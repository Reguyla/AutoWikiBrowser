using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Twain.Core.API;

//////////////////////////////////////////////////////////////////////////////////////////////
/* Don't use anything WikiFunctions-specific here, for source-compatibility with Updater  */
//////////////////////////////////////////////////////////////////////////////////////////////

namespace Twain.Core
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

        /// <summary>
        /// Detects exceptions that are expected or can be handled with a specific
        /// user-facing message without displaying the full error report dialog.
        /// </summary>
        /// <param name="ex">
        /// The exception to evaluate.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the exception was recognized, an appropriate
        /// message was displayed (or the exception was intentionally ignored), and no
        /// further error handling should occur; otherwise, <see langword="false"/> to
        /// allow normal error reporting.
        /// </returns>
        /// <remarks>
        /// This method provides fast-path handling for anticipated exceptions such as
        /// invalid regular expressions, unsupported cultures, network failures,
        /// out-of-memory conditions, I/O errors, and expected thread-abort scenarios.
        /// Exceptions that are not recognized are returned to the caller for standard
        /// diagnostic reporting.
        /// </remarks>
        private static bool HandleKnownExceptions(Exception ex)
        {
            string stackTrace = ex.StackTrace ?? string.Empty;

            TryWriteDebug(
                nameof(HandleKnownExceptions),
                stackTrace);

            if (IsInvalidRegularExpression(ex, stackTrace))
            {
                MessageBox.Show(
                    ex.Message,
                    "Invalid regular expression",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return true;
            }

            if (IsUnsupportedCulture(ex))
            {
                MessageBox.Show(
                    "Microsoft unfortunately doesn't support your locale culture. " +
                    "Please try a more commonly supported culture.",
                    "Unsupported culture",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return true;
            }

            if (IsNetworkException(ex))
            {
                MessageBox.Show(
                    GetNetworkErrorMessage(ex),
                    "Network access error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return true;
            }

            if (ex is OutOfMemoryException)
            {
                MessageBox.Show(
                    ex.Message,
                    "Out of Memory error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return true;
            }

            if (IsIoException(ex))
            {
                MessageBox.Show(
                    ex.Message,
                    "I/O error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return true;
            }

            if (IsExpectedThreadAbort(ex, stackTrace))
                return true;

            return false;
        }

        /// <summary>
        /// Determines whether an exception represents an invalid regular expression.
        /// </summary>
        private static bool IsInvalidRegularExpression(
            Exception ex,
            string stackTrace)
        {
            return ex is ArgumentException &&
                   (stackTrace.Contains(
                        "System.Text.RegularExpressions",
                        StringComparison.Ordinal) ||
                    ex.ToString().StartsWith(
                        "System.ArgumentException: parsing",
                        StringComparison.Ordinal));
        }

        /// <summary>
        /// Determines whether an exception was caused by an unsupported culture.
        /// </summary>
        private static bool IsUnsupportedCulture(Exception ex)
        {
            return ex is ArgumentException &&
                   Thrower(ex) == "CultureTableRecord.GetCultureTableRecord";
        }

        /// <summary>
        /// Gets the most useful user-facing message for a network exception.
        /// </summary>
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
        /// Determines whether an exception represents an I/O failure directly or
        /// through a configuration exception.
        /// </summary>
        private static bool IsIoException(Exception ex)
        {
            return ex is IOException ||
                   ex is ConfigurationErrorsException &&
                   ex.InnerException?.InnerException is IOException;
        }

        /// <summary>
        /// Determines whether a legacy thread-abort exception was caused by the user
        /// stopping article processing.
        /// </summary>
        private static bool IsExpectedThreadAbort(
            Exception ex,
            string stackTrace)
        {
            return ex is ThreadAbortException &&
                   (stackTrace.Contains(
                        "AutoWikiBrowser.MainForm.ProcessPage",
                        StringComparison.Ordinal) ||
                    stackTrace.Contains(
                        "Parsers.TagOrphans",
                        StringComparison.Ordinal));
        }

        /// <summary>
        /// Determines whether an exception or one of its inner exceptions represents
        /// a network request failure.
        /// </summary>
        /// <param name="exception">The exception to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the exception chain contains either a legacy
        /// <see cref="WebException"/> or a modern
        /// <see cref="HttpRequestException"/>; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsNetworkException(Exception exception)
        {
            return FindNetworkException(exception) != null;
        }

        /// <remarks>
        /// TODO (.NET 8 Modernization):
        /// Remove <see cref="WebException"/> support after all remaining
        /// <c>HttpWebRequest</c>-based request paths have been migrated to
        /// <c>HttpClient</c>.
        /// </remarks>
        /// <summary>
        /// Finds the first network request failure in an exception chain.
        /// </summary>
        /// <param name="exception">The exception chain to inspect.</param>
        /// <returns>
        /// The first matching network exception, or <see langword="null"/> when no
        /// network exception is present.
        /// </returns>
        private static Exception? FindNetworkException(Exception exception)
        {
            for (Exception? current = exception;
                 current != null;
                 current = current.InnerException)

                if (current is WebException or HttpRequestException)
                {
                    return current;
                }

            return null;
        }

        /// <summary>
        /// Displays exception information. Should be called from try...catch handlers
        /// </summary>
        /// <param name="ex">Exception object to handle</param>
        public static void HandleException(Exception ex)
        {
            if (ex == null)
                return;

            // Record a safe high-level event before writing the local report.
            DiagnosticSession.Record("Error", "Handling exception: " + ex.GetType().FullName);

            // Write the local report before older diagnostic code such as
            // HandleKnownExceptions or Tools.WriteDebug can fail.
            string diagnosticReportPath = LocalDiagnosticReport.TryWrite(ex);

            if (HandleKnownExceptions(ex))
                return;

            // Show a report-ready dialog for exceptions not handled as known conditions.
            // The user may review, copy, or manually submit the details as a bug report.

            ErrorHandler handler = new ErrorHandler
            {
                txtError =
                {
                    Text = BuildErrorSummary(ex, diagnosticReportPath)
                }
            };

            string errorMessage = PopulateErrorDialog(handler, ex);

            TryWriteDebug("HandleException", errorMessage);
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
                Duplicate,
                Site;

            /// <summary>
            /// Initializes a new instance of the <see cref="BugReport"/> class and
            /// gathers diagnostic information about an unhandled exception.
            /// </summary>
            /// <param name="ex">
            /// The exception for which diagnostic information will be collected.
            /// </param>
            /// <remarks>
            /// The constructor captures contextual information used to generate a
            /// comprehensive bug report, including the originating thread, stack trace,
            /// API-specific details, application and .NET version information, processing
            /// context, and the current wiki site. Optional diagnostic extensions are also
            /// queried to include any additional information they provide.
            /// </remarks>
            public BugReport(Exception ex)
            {
                ApiException? apiException = ex as ApiException;

                Thread = GetThrowingThreadName(apiException);

                StackTrace = BuildStackTrace(ex);

                ApiExtra = GetApiSpecificInformation(apiException);

                AppendedInfo = GetAppendedDiagnosticInformation();

                Version = BuildVersionInformation();
                DotNetVersion = Environment.Version.ToString();

                Duplicate = BuildProcessingContext();

                Site = GetCurrentSite();
            }

            /// <summary>
            /// Prints a wiki formatted bug report table
            /// </summary>
            /// <returns>String using {{AWB bug}} for reporting bugs</returns>
            public string PrintForWiki()
            {
                return Print(new WikiBugFormatter());
            }

            /// <summary>
            /// Formats the diagnostic information as a report suitable for submission to
            /// Wikimedia Phabricator.
            /// </summary>
            /// <returns>
            /// A formatted bug report containing the collected diagnostic information.
            /// </returns>
            public string PrintForPhabricator()
            {
                return Print(new PhabricatorBugFormatter());
            }

            /// <summary>
            /// Formats the collected diagnostic information using the specified bug report
            /// formatter.
            /// </summary>
            /// <param name="formatter">
            /// The formatter responsible for rendering the diagnostic report.
            /// </param>
            /// <returns>
            /// A formatted bug report containing the collected diagnostic information.
            /// </returns>
            /// <remarks>
            /// The generated report includes the exception stack trace, API-specific
            /// diagnostic information, thread details, operating system and application
            /// version information, duplicate-processing context, and any additional
            /// diagnostic information supplied by registered extensions.
            /// </remarks>
            public string Print(BugFormatter formatter)
            {
                StringBuilder errorMessage = new StringBuilder();

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

            /// <summary>
            /// Identifies the relationship of an exception to the primary exception being
            /// formatted.
            /// </summary>
            private enum ExceptionKind
            {
                /// <summary>
                /// The primary exception passed to the error-reporting system.
                /// </summary>
                TopLevel,

                /// <summary>
                /// An exception referenced through <see cref="Exception.InnerException"/>.
                /// </summary>
                Inner,

                /// <summary>
                /// An exception reported while loading a type or assembly.
                /// </summary>
                LoaderException
            }

            /// <summary>
            /// Returns the display label used for an exception category in a diagnostic
            /// report.
            /// </summary>
            /// <param name="ek">The exception category to describe.</param>
            /// <returns>
            /// A user-readable label for the specified exception category.
            /// </returns>
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

            /// <summary>
            /// Defines the formatting operations used to render a diagnostic bug report.
            /// </summary>
            public abstract class BugFormatter
            {
                /// <summary>
                /// Creates the text that appears before the main diagnostic report.
                /// </summary>
                /// <returns>The formatted report header.</returns>
                public abstract string PrintHeader();

                /// <summary>
                /// Creates the text that appears after the main diagnostic report.
                /// </summary>
                /// <returns>The formatted report footer.</returns>
                public abstract string PrintFooter();

                /// <summary>
                /// Formats a named diagnostic value.
                /// </summary>
                /// <param name="key">The diagnostic field name.</param>
                /// <param name="value">The diagnostic field value.</param>
                /// <returns>The formatted representation of the field.</returns>
                public abstract string PrintLine(string key, string value);

                /// <summary>
                /// Indicates whether this formatter provides report header and footer
                /// content.
                /// </summary>
                /// <returns>
                /// <see langword="true"/> when the report header and footer should be
                /// included; otherwise, <see langword="false"/>.
                /// </returns>
                public virtual bool HasHeaderFooter()
                {
                    return false;
                }
            }

            /// <summary>
            /// Formats diagnostic reports using the legacy AutoWikiBrowser wiki-template
            /// syntax.
            /// </summary>
            public class WikiBugFormatter : BugFormatter
            {
                /// <summary>
                /// Creates the opening portion of the AutoWikiBrowser bug-report template.
                /// </summary>
                /// <returns>The template opening and initial status field.</returns>
                public override string PrintHeader()
                {
                    return "{{AWB bug\r\n" +
                           PrintLine(
                               "status",
                               "new <!-- when fixed replace with \"fixed\" -->");
                }

                /// <summary>
                /// Creates the closing portion of the AutoWikiBrowser bug-report template.
                /// </summary>
                /// <returns>
                /// The fix-version field followed by the template closing braces.
                /// </returns>
                public override string PrintFooter()
                {
                    return
                        PrintLine(
                            "fix_version",
                            "<!-- Version of AWB the fix will be included in; " +
                            "AWB developer will complete when it's fixed -->") +
                        "\r\n}}";
                }

                /// <summary>
                /// Formats a diagnostic field as a parameter in the legacy AWB wiki
                /// bug-report template.
                /// </summary>
                /// <param name="key">The diagnostic field name.</param>
                /// <param name="value">The diagnostic field value.</param>
                /// <returns>The formatted template parameter.</returns>
                public override string PrintLine(string key, string value)
                {
                    return string.Format(" | {0,-14} = {1}", key, value);
                }

                /// <summary>
                /// Indicates that the wiki formatter produces both a report header and
                /// footer.
                /// </summary>
                /// <returns><see langword="true"/>.</returns>
                public override bool HasHeaderFooter()
                {
                    return true;
                }
            }

            /// <summary>
            /// Formats diagnostic reports using Wikimedia Phabricator markup.
            /// </summary>
            public class PhabricatorBugFormatter : BugFormatter
            {
                /// <summary>
                /// Returns the report header.
                /// </summary>
                /// <returns>
                /// An empty string, since Phabricator reports do not require a header.
                /// </returns>
                public override string PrintHeader()
                {
                    return string.Empty;
                }

                /// <summary>
                /// Returns the report footer.
                /// </summary>
                /// <returns>
                /// An empty string, since Phabricator reports do not require a footer.
                /// </returns>
                public override string PrintFooter()
                {
                    return string.Empty;
                }

                /// <summary>
                /// Formats a diagnostic field using Phabricator's bold-label syntax.
                /// </summary>
                /// <param name="key">The diagnostic field name.</param>
                /// <param name="value">The diagnostic field value.</param>
                /// <returns>
                /// The formatted diagnostic field.
                /// </returns>
                public override string PrintLine(string key, string value)
                {
                    return string.Format("**{0}**: {1}", key, value);
                }
            }

            /// <summary>
            /// Formats the supplied exception and its inner exceptions into the
            /// diagnostic stack trace used by the bug report.
            /// </summary>
            /// <param name="ex">The exception to format.</param>
            /// <returns>The formatted exception information.</returns>
            private static string BuildStackTrace(Exception ex)
            {
                StringBuilder stackTrace = new StringBuilder();

                FormatException(ex, stackTrace, ExceptionKind.TopLevel);

                return stackTrace.ToString();
            }

            /// <summary>
            /// Retrieves additional diagnostic information from an API exception.
            /// </summary>
            /// <param name="apiException">
            /// The API exception to inspect, or <see langword="null"/> when the reported
            /// exception is not API-related.
            /// </param>
            /// <returns>
            /// API-specific diagnostic information, a fallback message if collection
            /// fails, or <see langword="null"/> for a non-API exception.
            /// </returns>
            private static string GetApiSpecificInformation(ApiException apiException)
            {
                if (apiException == null)
                    return null;

                try
                {
                    return apiException.GetExtraSpecificInformation();
                }
                catch
                {
                    // Failure to collect optional API details must not prevent the main
                    // exception report from being generated.
                    return "API-specific diagnostic information could not be collected.";
                }
            }

            /// <summary>
            /// Builds the application and hosting assembly version information included
            /// in the diagnostic report.
            /// </summary>
            /// <returns>The formatted version description.</returns>
            private static string BuildVersionInformation()
            {
                AssemblyName hostingApp = Assembly.GetExecutingAssembly().GetName();

                string version = string.Format(
                    "{0} ({1}), {2} ({3})",
                    Application.ProductName,
                    Application.ProductVersion,
                    hostingApp.Name,
                    hostingApp.Version);

                // Suppress failures when Variables has not completed initialization.
                try
                {
                    version += ", revision " + Variables.Revision;
                }
                catch
                {
                }

                return version;
            }

            /// <summary>
            /// Retrieves the current wiki site URL for inclusion in the diagnostic report.
            /// </summary>
            /// <returns>
            /// The current site URL, or <see langword="null"/> when the wiki configuration
            /// is unavailable.
            /// </returns>
            private static string GetCurrentSite()
            {
                try
                {
                    return Variables.URL;
                }
                catch
                {
                    // Error reporting must continue even when wiki configuration
                    // initialization has failed.
                    return null;
                }
            }

            /// <summary>
            /// Builds diagnostic context for the page or ListMaker input being processed
            /// when the exception occurred.
            /// </summary>
            /// <returns>
            /// Page-processing context, ListMaker context, or <see langword="null"/> when
            /// no processing context is available.
            /// </returns>
            private static string BuildProcessingContext()
            {
                if (!string.IsNullOrEmpty(CurrentPage))
                {
                    try
                    {
                        // Use a plain URL because this context is included in Phabricator
                        // reports. Do not use Tools.WikiEncode here, to keep this code
                        // portable to AWBUpdater.
                        string pageUrl =
                            Variables.URLIndex +
                            "?title=" + WebUtility.UrlEncode(CurrentPage);

                        if (CurrentRevision > 0)
                        {
                            pageUrl += "&oldid=" + CurrentRevision;
                        }

                        return "Encountered while processing page: " + pageUrl;
                    }
                    catch
                    {
                        // The wiki configuration may be unavailable when Variables failed
                        // during startup or while processing an earlier exception.
                        return "Encountered while processing a page.";
                    }
                }

                if (!string.IsNullOrEmpty(ListMakerText))
                    return "ListMaker text was present.";

                return null;
            }

            /// <summary>
            /// Gets the name of the thread that raised the exception when it was not the
            /// application's main thread.
            /// </summary>
            /// <param name="apiException">
            /// The API exception containing its originating thread, or
            /// <see langword="null"/> for a non-API exception.
            /// </param>
            /// <returns>
            /// The throwing thread name, or <see langword="null"/> when the exception
            /// originated on the main thread or the thread has no name.
            /// </returns>
            private static string GetThrowingThreadName(ApiException apiException)
            {
                System.Threading.Thread thread = apiException != null
                    ? apiException.ThrowingThread
                    : System.Threading.Thread.CurrentThread;

                if (thread.Name != "Main thread")
                    return thread.Name;

                return null;
            }
        }

        /// <summary>
        /// Builds the user-facing exception summary displayed in the error dialog.
        /// </summary>
        /// <param name="ex">The exception being reported.</param>
        /// <param name="diagnosticReportPath">
        /// The path to the saved local diagnostic report, or an empty value when no
        /// report was created.
        /// </param>
        /// <returns>The exception summary to display to the user.</returns>
        private static string BuildErrorSummary(
            Exception ex,
            string diagnosticReportPath)
        {
            string errorSummary =
                ex.Message +
                Environment.NewLine +
                Environment.NewLine +
                "Before sharing this report publicly, review the details. " +
                "They may include wiki, page, ListMaker, or API-related context.";

            // Show the saved-file location in the normal error dialog, but do not add
            // it to txtDetails because that text can be copied into a public bug report.
            if (!string.IsNullOrEmpty(diagnosticReportPath))
            {
                errorSummary +=
                    Environment.NewLine +
                    Environment.NewLine +
                    "A local diagnostic report was saved to:" +
                    Environment.NewLine +
                    diagnosticReportPath;
            }

            return errorSummary;
        }

        /// <summary>
        /// Populates the error dialog with the formatted bug report and subject line.
        /// </summary>
        /// <param name="handler">The error dialog to populate.</param>
        /// <param name="ex">The exception being reported.</param>
        /// <returns>
        /// The formatted bug report text used for both the dialog details and debug output.
        /// </returns>
        private static string PopulateErrorDialog(
            ErrorHandler handler,
            Exception ex)
        {
            string errorMessage = new BugReport(ex).PrintForPhabricator();

            handler.txtDetails.Text = errorMessage;
            handler.txtSubject.Text = ex.GetType().Name + " in " + Thrower(ex);

            return errorMessage;
        }

        /// <summary>
        /// Collects additional diagnostic information from registered error-handler
        /// extensions.
        /// </summary>
        /// <returns>
        /// The combined diagnostic information, or <see langword="null"/> when no
        /// diagnostic extensions are registered.
        /// </returns>
        private static string GetAppendedDiagnosticInformation()
        {
            if (AppendToErrorHandler == null)
                return null;

            StringBuilder append = new StringBuilder();

            foreach (Delegate d in AppendToErrorHandler.GetInvocationList())
            {
                try
                {
                    object result = d.DynamicInvoke();
                    string value = result as string;

                    if (!string.IsNullOrEmpty(value))
                        append.AppendLine(value);
                }
                catch
                {
                    // A diagnostic extension must not prevent the main error report
                    // from being generated or displayed.
                }
            }

            return append.ToString();
        }
    
        #region Static helper functions

        // TODO: Review this pattern against modern .NET stack trace formats,
        // including generic methods, nested types, and async state machines.
        /// <summary>
        /// Matches fully qualified method names within exception stack traces for use
        /// when formatting diagnostic reports.
        /// </summary>
        private static readonly Regex StackTrace =
            new Regex(
                @"([a-zA-Z_0-9\.`]+)(?=\()",
                RegexOptions.Compiled);

        /// <summary>
        /// Returns names of functions in stack trace of an exception
        /// </summary>
        /// <param name="stackTrace">Exception's StackTrace</param>
        /// <returns>List of fully qualified function names</returns>
        public static string[] MethodNames(string? stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return Array.Empty<string>();

            MatchCollection matches = StackTrace.Matches(stackTrace);

            string[] result = new string[matches.Count];

            for (int i = 0; i < result.Length; i++)
                result[i] = matches[i].Groups[1].Value;

            return result;
        }

        // TODO: Review whether Tools.WriteDebug can expose a documented set of
        // expected exceptions so this catch can eventually be narrowed.
        /// <summary>
        /// Attempts to write diagnostic information without allowing a logging failure
        /// to interrupt exception handling.
        /// </summary>
        /// <param name="source">The component or operation producing the diagnostic entry.</param>
        /// <param name="text">The diagnostic text to write.</param>
        private static void TryWriteDebug(string source, string text)
        {
            try
            {
                Tools.WriteDebug(source, text);
            }
            catch
            {
                // LocalDiagnosticReport has already preserved the original exception.
                // Legacy debug logging must not mask or replace it.
            }
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
                return "unknown function";

            string thrower = trace[0];

            foreach (string methodName in trace)
            {
                if (!PresetNamespaces.Any(
                        namespacePrefix => methodName.StartsWith(
                            namespacePrefix,
                            StringComparison.Ordinal)))
                {
                    thrower = methodName;
                    break;
                }
            }

            // TODO: Add tests for nested types, generic methods, async state machines,
            // and compiler-generated stack trace names.
            // Strip namespace for clarity.
            string shortName = Regex.Match(
                thrower,
                @"\w+\.{1,2}\w+$").Value;

            if (shortName.Length > 0)
                return shortName;

            return thrower;
        }

        /// <summary>
        /// Namespace prefixes treated as framework or runtime code when selecting the
        /// most relevant method from an exception stack trace.
        /// </summary>
        private static readonly string[] PresetNamespaces =
        {
            "System.",
            "Microsoft.",
            "Mono."
        };

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandler"/> dialog.
        /// </summary>
        protected ErrorHandler()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the error dialog title when the form is loaded.
        /// </summary>
        /// <param name="sender">The source of the load event.</param>
        /// <param name="e">The event data.</param>
        private void ErrorHandler_Load(object sender, EventArgs e)
        {
            Text = Application.ProductName;
        }

        /// <summary>
        /// Copies the formatted diagnostic report to the system clipboard.
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">The event data.</param>
        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.Clear();

                // Allow the clipboard clear operation to complete before writing the
                // diagnostic report.
                // TODO: Replace the fixed UI-thread delay with bounded clipboard retry logic.
                Thread.Sleep(50);

                Clipboard.SetText(txtDetails.Text);
            }
            catch
            {
                // Clipboard access can fail when it is temporarily locked by another
                // process. Copy failure must not close or interrupt the error dialog.
            }
        }

        /// <summary>
        /// Opens the Wikimedia Phabricator task creation page for AutoWikiBrowser.
        /// </summary>
        /// <param name="sender">The source of the link-click event.</param>
        /// <param name="e">The event data.</param>
        private void linkLabel1_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.LinkVisited = true;

            try
            {
                OpenUrl(
                    "https://phabricator.wikimedia.org/maniphest/task/create/" +
                    "?projects=AutoWikiBrowser");
            }
            catch
            {
                // Failure to open the browser must not interrupt the error dialog.
            }
        }

        /// <summary>
        /// Opens a URL using the operating system's default browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        private static void OpenUrl(string url)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
        }

        // TODO: Rename generic designer-generated control and event-handler names
        // during a dedicated ErrorHandler UI cleanup.
        //
        // TODO: Consider confirming that the diagnostic details were reviewed before
        // inserting them into a public Phabricator task.
        //
        // TODO: Avoid placing large diagnostic reports entirely in the URL query.
        // Consider opening a blank task and relying on clipboard copy when the report
        // exceeds a safe URL length.
        /// <summary>
        /// Opens a new Wikimedia Phabricator task with the formatted diagnostic report
        /// included in the task description.
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">The event data.</param>
        private void btnPhab_Click(object sender, EventArgs e)
        {
            try
            {
                OpenUrl(
                    "https://phabricator.wikimedia.org/maniphest/task/create/" +
                    "?projects=AutoWikiBrowser&description=" +
                    Uri.EscapeDataString(txtDetails.Text));
            }
            catch
            {
                // Failure to open the browser must not interrupt the error dialog.
            }
        }
    }
}