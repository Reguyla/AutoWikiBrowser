using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using System.Text.RegularExpressions;

//////////////////////////////////////////////////////////////////////////////////////////////
/* Don't use anything WikiFunctions-specific here, for source-compatibility with Updater  */
//////////////////////////////////////////////////////////////////////////////////////////////

namespace AWBUpdater
{
    public delegate string ErrorHandlerAddition();

    /// <summary>
    /// This class provides helper functions for handling errors and displaying them to users
    /// </summary>
    public partial class ErrorHandler : Form
    {
        public static event ErrorHandlerAddition AppendToErrorHandler;

        /// <summary>
        /// Title of the page currently being processed
        /// </summary>
        public static string CurrentPage;

        /// <summary>
        /// Revision of the page currently being processed
        /// </summary>
        public static long CurrentRevision;

        /// <summary>
        /// Current text that the list is being made from in ListMaker
        /// </summary>
        public static string ListMakerText;

        private static bool HandleKnownExceptions(Exception ex)
        {
            string stackTrace = ex.StackTrace ?? string.Empty;

            if (ex is ArgumentException &&
                (stackTrace.Contains("System.Text.RegularExpressions") ||
                 ex.ToString().StartsWith(
                     "System.ArgumentException: parsing",
                     StringComparison.Ordinal)))
            {
                MessageBox.Show(
                    ex.Message,
                    "Invalid regular expression",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            // Unsupported culture, possibly bn-BD
            else if (ex is ArgumentException &&
                     Thrower(ex) == "CultureTableRecord.GetCultureTableRecord")
            {
                MessageBox.Show(
                    "Microsoft unfortunately doesn't support your locale culture. " +
                    "Please try a more common one.",
                    "Unsupported culture",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            // Network access error
            else if (ex is System.Net.WebException ||
                     ex.InnerException is System.Net.WebException)
            {
                // If AWB starts offline, provide a clear network-related message.
                string message =
                    ex.Message.StartsWith(
                        "The type initializer for",
                        StringComparison.Ordinal) &&
                    ex.InnerException != null
                        ? ex.InnerException.Message
                        : ex.Message;

                MessageBox.Show(
                    message,
                    "Network access error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            // Out-of-memory error
            else if (ex is OutOfMemoryException)
            {
                MessageBox.Show(
                    ex.Message,
                    "Out of Memory error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            // Disk write error or full disk
            else if (ex is System.IO.IOException ||
                     ex is ConfigurationErrorsException &&
                     ex.InnerException != null &&
                     ex.InnerException.InnerException is System.IO.IOException)
            {
                MessageBox.Show(
                    ex.Message,
                    "I/O error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            // BackgroundRequest abort caused by the user pressing Stop
            else if (ex is ThreadAbortException &&
                     (stackTrace.Contains("AutoWikiBrowser.MainForm.ProcessPage") ||
                      stackTrace.Contains("Parsers.TagOrphans")))
            {
                return true;
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Displays exception information. Should be called from try...catch handlers
        /// </summary>
        /// <param name="ex">Exception object to handle</param>
        public static void HandleException(Exception ex)
        {
            if (ex == null || HandleKnownExceptions(ex)) return;

            ErrorHandler handler = new ErrorHandler
            {
                txtError =
                {
                    Text =
                        ex.Message +
                        Environment.NewLine +
                        Environment.NewLine +
                        "This error was not recognized as a known condition. " +
                        "Please review the details below and consider submitting a bug report."
                }
            };

            string errorMessage;

            try
            {
                errorMessage = new BugReport(ex).PrintForPhabricator();
            }
            catch
            {
                errorMessage =
                    "The formatted error report could not be generated." +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex;
            }

            handler.txtDetails.Text = errorMessage;

            handler.txtSubject.Text = ex.GetType().Name + " in " + Thrower(ex);

            handler.ShowDialog();
        }

        class BugReport
        {
            private string Thread,
                OS = Environment.OSVersion.ToString(),
                StackTrace,
                ApiExtra,
                AppendedInfo,
                Version,
                DotNetVersion,
                Duplicate;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="ex"></param>
            public BugReport(Exception ex)
            {
                System.Threading.Thread currentThread =
                    System.Threading.Thread.CurrentThread;

                if (!string.IsNullOrEmpty(currentThread.Name) &&
                    currentThread.Name != "Main thread")
                {
                    Thread = currentThread.Name;
                }

                StringBuilder stackTrace = new StringBuilder();
                FormatException(ex, stackTrace, ExceptionKind.TopLevel);
                StackTrace = stackTrace.ToString();

                ErrorHandlerAddition handlers = AppendToErrorHandler;

                if (handlers != null)
                {
                    StringBuilder append = new StringBuilder();

                    foreach (ErrorHandlerAddition handler in handlers.GetInvocationList())
                    {
                        try
                        {
                            string value = handler();

                            if (!string.IsNullOrEmpty(value))
                            {
                                append.AppendLine(value);
                            }
                        }
                        catch
                        {
                            // A diagnostic extension must not prevent the main
                            // error report from being generated or displayed.
                        }
                    }

                    AppendedInfo = append.ToString();
                }

                AssemblyName hostingApp = Assembly.GetExecutingAssembly().GetName();

                Version = string.Format(
                    "{0} ({1}), {2} ({3})",
                    Application.ProductName,
                    Application.ProductVersion,
                    hostingApp.Name,
                    hostingApp.Version);

                DotNetVersion = Environment.Version.ToString();
            }

            /// <summary>
            /// Prints a wiki formatted bug report table
            /// </summary>
            /// <returns>String using {{AWB bug}} for reporting bugs</returns>
            public string PrintForWiki()
            {
                return Print(new WikiBugFormatter());
            }

            public string PrintForPhabricator()
            {
                return Print(new PhabricatorBugFormatter());
            }

            public string Print(BugFormatter formatter)
            {
                StringBuilder errorMessage = new StringBuilder();

                if (formatter.HasHeaderFooter())
                {
                    errorMessage.AppendLine(formatter.PrintHeader());
                }
                errorMessage.AppendLine(formatter.PrintLine("description", ""));
                errorMessage.AppendLine(formatter.PrintLine("workaround", ""));

                errorMessage.AppendLine("--------------------------");

                errorMessage.Append("<table>");
                errorMessage.Append(StackTrace);
                errorMessage.AppendLine("</table>");

                if (!string.IsNullOrEmpty(ApiExtra))
                {
                    errorMessage.AppendLine(ApiExtra);
                }

                if (formatter is WikiBugFormatter)
                {
                    errorMessage.AppendLine("~~~~");
                }

                if (!string.IsNullOrEmpty(Thread))
                {
                    errorMessage.AppendLine(formatter.PrintLine("thread", Thread));
                }

                errorMessage.AppendLine(formatter.PrintLine("OS", OS));
                errorMessage.AppendLine(formatter.PrintLine("version", Version));
                errorMessage.AppendLine(formatter.PrintLine("net", DotNetVersion));
                errorMessage.AppendLine(formatter.PrintLine("duplicate", Duplicate));

                if (!string.IsNullOrEmpty(AppendedInfo))
                {
                    errorMessage.AppendLine(AppendedInfo);
                }

                if (formatter.HasHeaderFooter())
                {
                    errorMessage.AppendLine(formatter.PrintFooter());
                }

                return errorMessage.ToString();
            }

            /// <summary>
            /// Formats exception information for bug report
            /// </summary>
            /// <param name="ex">Exception to process</param>
            /// <param name="sb">StringBuilder used for output</param>
            /// <param name="kind">what kind of exception is this</param>
            private static void FormatException(Exception ex, StringBuilder sb, ExceptionKind kind)
            {
                sb.AppendFormat("<tr><th>{0}:</th><td>`{1}`</td></tr>\r\n", KindToString(kind), ex.GetType().Name);
                sb.AppendFormat("<tr><th>Message:</th><td>`{0}`</td></tr>\r\n", ex.Message);
                sb.AppendFormat("<tr><th>Call stack:</th><td><pre>{0}</pre></td></tr>\r\n", ex.StackTrace);

                if (ex.InnerException != null)
                {
                    FormatException(ex.InnerException, sb, ExceptionKind.Inner);
                }
                if (ex is ReflectionTypeLoadException)
                {
                    foreach (Exception e in ((ReflectionTypeLoadException)ex).LoaderExceptions)
                    {
                        FormatException(e, sb, ExceptionKind.LoaderException);
                    }
                }
            }

            private enum ExceptionKind
            {
                TopLevel,
                Inner,
                LoaderException
            };

            private static string KindToString(ExceptionKind ek)
            {
                switch (ek)
                {
                    case ExceptionKind.Inner:
                        return "Inner exception";
                    case ExceptionKind.LoaderException:
                        return "Loader exception";
                    default:
                        return "Exception";
                }
            }

            public abstract class BugFormatter
            {
                public abstract string PrintHeader();
                public abstract string PrintFooter();
                public abstract string PrintLine(string key, string value);

                public virtual bool HasHeaderFooter()
                {
                    return false;
                }
            }

            public class WikiBugFormatter : BugFormatter
            {
                public override string PrintHeader()
                {
                    return @"{{AWB bug\r\n" + PrintLine("status", "new <!-- when fixed replace with \"fixed\" -->");
                }

                public override string PrintFooter()
                {
                    return
                        PrintLine("fix_version",
                            "<!-- Version of AWB the fix will be included in; AWB developer will complete when it's fixed -->") +
                        "\r\n}}";
                }

                public override string PrintLine(string key, string value)
                {
                    return string.Format(" | {0,-14} = {1}", key, value);
                }

                public override bool HasHeaderFooter()
                {
                    return true;
                }
            }

            public class PhabricatorBugFormatter : BugFormatter
            {
                public override string PrintHeader()
                {
                    return "";
                }

                public override string PrintFooter()
                {
                    return "";
                }

                public override string PrintLine(string key, string value)
                {
                    return string.Format("**{0}**: {1}", key, value);
                }
            }
        }

        #region Static helper functions

        private static readonly Regex StackTraceMethodRegex =
            new Regex(@"([a-zA-Z_0-9\.`]+)(?=\()",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Returns names of functions in stack trace of an exception
        /// </summary>
        /// <param name="stackTrace">Exception's StackTrace</param>
        /// <returns>List of fully qualified function names</returns>
        public static string[] MethodNames(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
            {
                return new string[0];
            }

            MatchCollection matches =
                StackTraceMethodRegex.Matches(stackTrace);

            string[] result = new string[matches.Count];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = matches[i].Groups[1].Value;
            }

            return result;
        }

        /// <summary>
        /// Returns the name of our function where supposedly error resides;
        /// it's the last non-framework function in the stack
        /// </summary>
        /// <param name="ex">Exception to process</param>
        /// <returns>Function names without namespace</returns>
        public static string Thrower(Exception ex)
        {
            string[] trace = MethodNames(ex.StackTrace);

            if (trace.Length == 0)
            {
                return "unknown function";
            }

            string res = "";
            foreach (string t in trace)
            {
                if (PresetNamespaces.Any(ns => t.StartsWith(ns)))
                {
                    res = trace[0];
                }
                else
                {
                    res = t;
                    break;
                }
            }

            // strip namespace for clarity
            var res2 = Regex.Match(res, @"\w+\.{1,2}\w+$").Value;
            if (res2.Length > 0)
            {
                return res2;
            }

            return res;
        }

        private static readonly string[] PresetNamespaces = { "System.", "Microsoft.", "Mono." };

        #endregion

        protected ErrorHandler()
        {
            InitializeComponent();
        }

        private void ErrorHandler_Load(object sender, EventArgs e)
        {
            Text = Application.ProductName;
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtDetails.Text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(txtDetails.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The error report could not be copied to the clipboard." +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "Clipboard error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static void OpenUrl(string url)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
        }

        private void linkLabel1_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.LinkVisited = true;

            try
            {
                OpenUrl(
                    "https://phabricator.wikimedia.org/maniphest/task/create/?projects=AutoWikiBrowser");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The Phabricator page could not be opened." +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "Unable to open browser",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
