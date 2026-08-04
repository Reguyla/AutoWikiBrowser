using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;

namespace Twain.Core;

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
    private const string ReportFormatVersion = "1.2";
    private const int MaximumLoaderExceptions = 25;

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

        report.AppendLine(
            "Report format version: " + ReportFormatVersion);

        report.AppendLine(
            "Created: " +
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture));

        report.AppendLine();

        report.AppendLine("Application and runtime:");
        AppendAssemblyInformation(
            report,
            "Application assembly",
            Assembly.GetEntryAssembly());

        AppendAssemblyInformation(
            report,
            "Twain.Core assembly",
            typeof(LocalDiagnosticReport).Assembly);

        report.AppendLine(
            "  OS: " + Environment.OSVersion.VersionString);

        report.AppendLine(
            "  .NET runtime: " + Environment.Version);

        report.AppendLine(
            "  Process architecture: " +
            (Environment.Is64BitProcess ? "64-bit" : "32-bit"));

        report.AppendLine(
            "  Operating-system architecture: " +
            (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));

        report.AppendLine();

        AppendProcessInformation(report);

        report.AppendLine();

        AppendThreadInformation(report); report.AppendLine();

        AppendRecentEvents(report); report.AppendLine();

        AppendException(report, exception, "Exception:");

        report.AppendLine();
        report.AppendLine("Privacy note:");
        report.AppendLine(
            "This report is created locally and is not automatically uploaded.");
        report.AppendLine(
            "Known password, token, cookie, session, and authorization values are redacted.");

        return report.ToString();
    }

    /// <summary>
    /// Adds an exception, its inner exceptions, and any loader exceptions exposed
    /// by <see cref="ReflectionTypeLoadException"/>.
    /// </summary>
    private static void AppendException(
        StringBuilder report,
        Exception exception,
        string heading)
    {
        if (exception == null)
            return;

        report.AppendLine(heading);

        report.AppendLine("  Type: " + exception.GetType().FullName);

        report.AppendLine(
            "  HResult: 0x" +
            exception.HResult.ToString("X8", CultureInfo.InvariantCulture) +
            " (" + exception.HResult + ")");

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

            AppendException(
                report,
                exception.InnerException,
                "Inner exception:");
        }

        ReflectionTypeLoadException typeLoadException =
            exception as ReflectionTypeLoadException;

        if (typeLoadException == null ||
            typeLoadException.LoaderExceptions == null)
        {
            return;
        }

        int loaderExceptionCount = 0;

        foreach (Exception loaderException in typeLoadException.LoaderExceptions)
        {
            if (loaderException == null)
                continue;

            loaderExceptionCount++;

            if (loaderExceptionCount > MaximumLoaderExceptions)
            {
                report.AppendLine();
                report.AppendLine(
                    "Additional loader exceptions were omitted after " +
                    MaximumLoaderExceptions +
                    " entries.");

                break;
            }

            report.AppendLine();

            AppendException(
                report,
                loaderException,
                "Loader exception " + loaderExceptionCount + ":");
        }
    }

    /// <summary>
    /// Adds identifying information about an assembly and its declared target
    /// framework, without depending on application-specific static state.
    /// </summary>
    private static void AppendAssemblyInformation(
        StringBuilder report,
        string label,
        Assembly assembly)
    {
        report.AppendLine(
            "  " + label + ": " + GetAssemblyDescription(assembly));

        report.AppendLine(
            "  " + label + " target framework: " +
            GetTargetFrameworkDescription(assembly));
    }

    /// <summary>
    /// Returns an assembly name and version suitable for a local diagnostic report.
    /// </summary>
    private static string GetAssemblyDescription(Assembly assembly)
    {
        if (assembly == null)
            return "Unavailable";

        try
        {
            AssemblyName assemblyName = assembly.GetName();

            return assemblyName.Name + " " + assemblyName.Version;
        }
        catch
        {
            return "Unavailable";
        }
    }

    /// <summary>
    /// Returns the target framework declared by an assembly when that metadata is
    /// available. Older or generated assemblies may not declare this value.
    /// </summary>
    private static string GetTargetFrameworkDescription(Assembly assembly)
    {
        if (assembly == null)
            return "Unavailable";

        try
        {
            object[] attributes = assembly.GetCustomAttributes(
                typeof(TargetFrameworkAttribute),
                false);

            if (attributes.Length == 0)
                return "Not declared";

            TargetFrameworkAttribute targetFramework =
                attributes[0] as TargetFrameworkAttribute;

            if (targetFramework == null ||
                string.IsNullOrEmpty(targetFramework.FrameworkName))
            {
                return "Not declared";
            }

            return targetFramework.FrameworkName;
        }
        catch
        {
            return "Unavailable";
        }
    }

    /// <summary>
    /// Adds basic process details that help distinguish one running instance from
    /// another and show how long the application had been running before failure.
    /// </summary>
    private static void AppendProcessInformation(StringBuilder report)
    {
        report.AppendLine("Process:");

        try
        {
            using (Process process = Process.GetCurrentProcess())
            {
                DateTime startTime = process.StartTime;
                TimeSpan uptime = DateTime.Now - startTime;

                report.AppendLine("  Process ID: " + process.Id);

                report.AppendLine(
                    "  Started: " +
                    startTime.ToString(
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture));

                report.AppendLine(
                    "  Uptime: " + FormatUptime(uptime));
            }
        }
        catch
        {
            report.AppendLine("  Process details: Unavailable");
        }
    }

    /// <summary>
    /// Formats an elapsed duration in a stable, readable form for a diagnostic
    /// report without depending on the user's current culture.
    /// </summary>
    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime < TimeSpan.Zero)
            return "Unavailable";

        return uptime.Days + "d " +
               uptime.Hours.ToString("00", CultureInfo.InvariantCulture) + "h " +
               uptime.Minutes.ToString("00", CultureInfo.InvariantCulture) + "m " +
               uptime.Seconds.ToString("00", CultureInfo.InvariantCulture) + "s";
    }

    /// <summary>
    /// Adds thread and culture information for the thread that reported the
    /// exception. This is useful for separating UI-thread and worker-thread faults.
    /// </summary>
    private static void AppendThreadInformation(StringBuilder report)
    {
        Thread thread = Thread.CurrentThread;

        report.AppendLine("Thread:");
        report.AppendLine("  Managed thread ID: " + thread.ManagedThreadId);

        report.AppendLine(
            "  Name: " +
            (string.IsNullOrEmpty(thread.Name)
                ? "<unnamed>"
                : SanitizeAndLimit(thread.Name, 200)));

        try
        {
            report.AppendLine(
                "  Apartment state: " + thread.GetApartmentState());
        }
        catch
        {
            report.AppendLine("  Apartment state: Unavailable");
        }

        report.AppendLine(
            "  Current culture: " + CultureInfo.CurrentCulture.Name);

        report.AppendLine(
            "  Current UI culture: " + CultureInfo.CurrentUICulture.Name);
    }

    /// <summary>
    /// Adds the bounded in-memory event trail that preceded the handled exception.
    /// </summary>
    private static void AppendRecentEvents(StringBuilder report)
    {
        int discardedEventCount;
        string[] events = DiagnosticSession.GetRecentEvents(
            out discardedEventCount);

        report.AppendLine("Recent diagnostic events:");

        if (events.Length == 0)
        {
            report.AppendLine("  No events were recorded.");
            return;
        }

        foreach (string diagnosticEvent in events)
        {
            report.AppendLine("  " + diagnosticEvent);
        }

        if (discardedEventCount > 0)
        {
            report.AppendLine(
                "  Earlier events omitted because the " +
                "in-memory buffer reached its limit: " +
                discardedEventCount);
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