using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace WikiFunctions
{
    /// <summary>
    /// Writes a sanitized local diagnostic report for an application exception.
    ///
    /// This class deliberately uses only framework APIs. It does not call
    /// <see cref="ErrorHandler"/>, <see cref="Tools"/>, network code, or UI code,
    /// so a failure while writing diagnostics cannot cause recursive error handling.
    /// </summary>
    internal static class LocalDiagnosticReport
    {
        private const int MaximumMessageLength = 4000;
        private const int MaximumStackTraceLength = 12000;

        private static int _isWriting;

        private static readonly Regex HeaderValuePattern = new Regex(
            @"(?im)^(\s*(?:authorization|cookie|set-cookie)\s*:\s*).*$",
            RegexOptions.Compiled);

        private static readonly Regex SensitiveValuePattern = new Regex(
            @"(?ix)
            (
                [""']?
                \b
                (?:
                    password |
                    passwd |
                    pwd |
                    token |
                    csrf |
                    authorization |
                    cookie |
                    session |
                    sessionid |
                    lgtoken |
                    lgpassword |
                    logintoken |
                    edittoken |
                    rollbacktoken
                )
                \b
                [""']?
                \s* [:=] \s*
            )
            (
                ""[^""]*"" |
                '[^']*' |
                [^&\s\r\n,;<>]+
            )",
            RegexOptions.Compiled);

        /// <summary>
        /// Attempts to create a local diagnostic report for an exception.
        /// Any failure is deliberately suppressed so diagnostic reporting never
        /// hides or replaces the original application error.
        /// </summary>
        /// <param name="exception">The exception to record.</param>
        /// <returns>
        /// The full report path when saved successfully; otherwise <c>null</c>.
        /// </returns>
        internal static string TryWrite(Exception exception)
        {
            if (exception == null)
                return null;

            if (Interlocked.CompareExchange(ref _isWriting, 1, 0) != 0)
                return null;

            try
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "AutoWikiBrowser",
                    "Diagnostics");

                    Directory.CreateDirectory(directory);

                string fileName = string.Format(
                    "error-{0:yyyyMMdd-HHmmss}-{1}-{2}.txt",
                    DateTime.Now,
                    Process.GetCurrentProcess().Id,
                    Guid.NewGuid().ToString("N").Substring(0, 8));

                    string reportPath = Path.Combine(directory, fileName);

                File.WriteAllText(
                    reportPath,
                    BuildReport(exception),
                    new UTF8Encoding(false));

                return reportPath;
            }
            catch
            {
                // Never allow diagnostic reporting to interrupt normal error handling.
                return null;
            }
            finally
            {
                Interlocked.Exchange(ref _isWriting, 0);
            }
        }

        /// <summary>
        /// Builds a concise, sanitized diagnostic report without serializing
        /// arbitrary exception properties, request bodies, cookies, or debug logs.
        /// </summary>
        private static string BuildReport(Exception exception)
        {
            StringBuilder report = new StringBuilder(4096);

            report.AppendLine("AutoWikiBrowser Local Diagnostic Report");
            report.AppendLine("======================================");
            report.AppendLine();

            report.AppendLine("Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("Machine OS: " + Environment.OSVersion.VersionString);
            report.AppendLine(".NET runtime: " + Environment.Version);
            report.AppendLine("Process architecture: " +
                (Environment.Is64BitProcess ? "64-bit" : "32-bit"));
            report.AppendLine("Operating-system architecture: " +
                (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));
            report.AppendLine("Application: " + GetApplicationDescription());
            report.AppendLine();

            AppendException(report, exception, 0);

            report.AppendLine();
            report.AppendLine("Privacy note:");
            report.AppendLine(
                "This report is created locally and is not automatically uploaded.");
            report.AppendLine(
                "Known password, token, cookie, session, and authorization values are redacted.");

            return report.ToString();
        }

        /// <summary>
        /// Adds an exception and its inner exceptions while restricting output
        /// to the type, sanitized message, and sanitized stack trace.
        /// </summary>
        private static void AppendException(
            StringBuilder report,
            Exception exception,
            int depth)
        {
            int exceptionNumber = depth + 1;

            report.AppendLine(
                depth == 0
                    ? "Exception:"
                    : "Inner exception " + exceptionNumber + ":");

            report.AppendLine("  Type: " + exception.GetType().FullName);
            report.AppendLine(
                "  Message: " +
                SanitizeAndLimit(exception.Message, MaximumMessageLength));

            string stackTrace = SanitizeAndLimit(
                exception.StackTrace,
                MaximumStackTraceLength);

            if (!string.IsNullOrEmpty(stackTrace))
            {
                report.AppendLine("  Stack trace:");
                AppendIndentedLines(report, stackTrace, "    ");
            }

            if (exception.InnerException != null)
            {
                report.AppendLine();
                AppendException(report, exception.InnerException, depth + 1);
            }
        }

        /// <summary>
        /// Returns basic application assembly information without depending on
        /// application-specific static state.
        /// </summary>
        private static string GetApplicationDescription()
        {
            try
            {
                Assembly entryAssembly = Assembly.GetEntryAssembly();

                if (entryAssembly == null)
                    return "Unavailable";

                AssemblyName assemblyName = entryAssembly.GetName();

                return assemblyName.Name + " " + assemblyName.Version;
            }
            catch
            {
                return "Unavailable";
            }
        }

        /// <summary>
        /// Removes common credential-bearing values and limits long text before
        /// it is written to a report.
        /// </summary>
        private static string SanitizeAndLimit(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            string sanitized = HeaderValuePattern.Replace(
                value,
                "$1<removed>");

            sanitized = SensitiveValuePattern.Replace(
                sanitized,
                "$1<removed>");

            if (sanitized.Length <= maximumLength)
                return sanitized;

            return sanitized.Substring(0, maximumLength) +
                   Environment.NewLine +
                   "[Text truncated]";
        }

        /// <summary>
        /// Appends text one line at a time with a fixed indentation prefix.
        /// </summary>
        private static void AppendIndentedLines(
            StringBuilder builder,
            string text,
            string indent)
        {
            using (StringReader reader = new StringReader(text))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    builder.Append(indent);
                    builder.AppendLine(line);
                }
            }
        }
    }
}