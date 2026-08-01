#nullable enable

using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using ThreadingThread = System.Threading.Thread;
using System.Windows.Forms;

//////////////////////////////////////////////////////////////////////////////////////////////
/* Don't use anything WikiFunctions-specific here, for source-compatibility with Updater  */
//////////////////////////////////////////////////////////////////////////////////////////////

namespace AWBUpdater;

public delegate string ErrorHandlerAddition();

/// <summary>
/// This class provides helper functions for handling errors and displaying them to users
/// </summary>
public partial class ErrorHandler : Form
{
    public static event ErrorHandlerAddition? AppendToErrorHandler;

    /// <summary>
    /// Revision of the page currently being processed
    /// </summary>
    public static long CurrentRevision;

    /// <summary>
    /// Current text that the list is being made from in ListMaker
    /// </summary>
    public static string ListMakerText = string.Empty;

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
        else if (IsNetworkException(ex))
        {
            // If AWB starts offline, provide a clear network-related message.
            string message = GetNetworkErrorMessage(ex);

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
    /// Gets the most useful user-facing message for a network exception.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>The message that should be displayed to the user.</returns>
    private static string GetNetworkErrorMessage(Exception ex)
    {
        Exception? networkException = FindNetworkException(ex);

        return ex.Message.StartsWith(
                   "The type initializer for",
                   StringComparison.Ordinal) &&
               networkException != null
            ? networkException.Message
            : ex.Message;
    }

    /// <summary>
    /// Determines whether an exception or one of its inner exceptions represents
    /// a network request failure.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when a network exception is present; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool IsNetworkException(Exception exception)
    {
        return FindNetworkException(exception) != null;
    }

    // TODO (.NET 8 Modernization):
    // Remove legacy WebException support after all remaining
    // HttpWebRequest-based code has been migrated to HttpClient.

    /// <summary>
    /// Finds the first network request failure in an exception chain.
    /// </summary>
    /// <param name="exception">The exception chain to inspect.</param>
    /// <returns>
    /// The first matching network exception, or <see langword="null"/> when none
    /// is present.
    /// </returns>
    private static Exception? FindNetworkException(Exception exception)
    {
        for (Exception? current = exception;
             current != null;
             current = current.InnerException)
        {
            if (current is System.Net.WebException or HttpRequestException)
            {
                return current;
            }
        }

        return null;
    }

    /// <summary>
    /// Displays exception information. Should be called from try...catch handlers
    /// </summary>
    /// <param name="ex">Exception object to handle</param>
    public static void HandleException(Exception ex)
    {
        if (ex == null || HandleKnownExceptions(ex)) return;

        ErrorHandler handler = new()
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
        private readonly string Thread = string.Empty;
        private readonly string OS = Environment.OSVersion.ToString();
        private readonly string StackTrace;
        private readonly string AppendedInfo = string.Empty;
        private readonly string Version;
        private readonly string DotNetVersion;

        /// <summary>
        /// Initializes a diagnostic bug report for the supplied exception.
        /// </summary>
        /// <param name="ex">The exception to include in the report.</param>
        public BugReport(Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            ThreadingThread currentThread =
                ThreadingThread.CurrentThread;

            if (!string.IsNullOrEmpty(currentThread.Name) &&
                currentThread.Name != "Main thread")
            {
                Thread = currentThread.Name;
            }

            StringBuilder stackTrace = new();
            FormatException(
                ex,
                stackTrace,
                ExceptionKind.TopLevel);

            StackTrace = stackTrace.ToString();

            ErrorHandlerAddition? handlers = AppendToErrorHandler;

            if (handlers != null)
            {
                StringBuilder append = new();

                foreach (Delegate invocation in handlers.GetInvocationList())
                {
                    try
                    {
                        ErrorHandlerAddition handler =
                            (ErrorHandlerAddition)invocation;

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

            AssemblyName hostingApp =
                Assembly.GetExecutingAssembly().GetName();

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
    public string PrintForPhabricator()
        {
            return Print(new PhabricatorBugFormatter());
        }

        public string Print(BugFormatter formatter)
        {
            StringBuilder errorMessage = new();

            errorMessage.AppendLine(formatter.PrintLine("description", ""));
            errorMessage.AppendLine(formatter.PrintLine("workaround", ""));

            errorMessage.AppendLine("--------------------------");

            errorMessage.Append("<table>");
            errorMessage.Append(StackTrace);
            errorMessage.AppendLine("</table>");

            if (!string.IsNullOrEmpty(Thread))
            {
                errorMessage.AppendLine(formatter.PrintLine("thread", Thread));
            }

            errorMessage.AppendLine(formatter.PrintLine("OS", OS));
            errorMessage.AppendLine(formatter.PrintLine("version", Version));
            errorMessage.AppendLine(formatter.PrintLine("net", DotNetVersion));
            errorMessage.AppendLine(formatter.PrintLine("duplicate", ""));

            if (!string.IsNullOrEmpty(AppendedInfo))
            {
                errorMessage.AppendLine(AppendedInfo);
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
            if (ex is ReflectionTypeLoadException reflectionTypeLoadException)
            {
                foreach (Exception? loaderException
                         in reflectionTypeLoadException.LoaderExceptions)
                {
                    if (loaderException != null)
                    {
                        FormatException(
                            loaderException,
                            sb,
                            ExceptionKind.LoaderException);
                    }
                }
            }
        }

        /// <summary>
        /// Identifies an exception's role within the reported exception hierarchy.
        /// </summary>
        private enum ExceptionKind
        {
            /// <summary>
            /// The primary exception being reported.
            /// </summary>
            TopLevel,

            /// <summary>
            /// An exception contained within another exception.
            /// </summary>
            Inner,

            /// <summary>
            /// An exception encountered while loading a type or assembly.
            /// </summary>
            LoaderException
        }

        /// <summary>
        /// Gets the display label for an exception's role in the exception hierarchy.
        /// </summary>
        /// <param name="kind">The exception kind to describe.</param>
        /// <returns>A label suitable for diagnostic output.</returns>
        private static string KindToString(ExceptionKind kind) =>
            kind switch
            {
                ExceptionKind.Inner => "Inner exception",
                ExceptionKind.LoaderException => "Loader exception",
                _ => "Exception"
            };

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
    public static string[] MethodNames(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return [];
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

    /// <summary>
    /// Contains namespace prefixes treated as framework or runtime namespaces
    /// when identifying the likely source of an exception.
    /// </summary>
    private static readonly string[] PresetNamespaces =
    {
    "System.",
    "Microsoft.",
    "Mono."
};

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandler"/> form.
    /// </summary>
    protected ErrorHandler()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the form title to the current application product name when the form loads.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
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
        Process.Start(
            new ProcessStartInfo
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