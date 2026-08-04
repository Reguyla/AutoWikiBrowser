using System.Globalization;
using System.Threading;

namespace Twain.Core;

/// <summary>
/// Retains a bounded, in-memory trail of sanitized application events.
///
/// Events are kept only for the lifetime of the current process and are
/// included in a local diagnostic report when an exception is handled.
/// This class does not write files, send network requests, or depend on
/// application UI, Tools, or ErrorHandler code.
/// </summary>
internal static class DiagnosticSession
{
    private const int MaximumEvents = 100;
    private const int MaximumCategoryLength = 40;
    private const int MaximumMessageLength = 300;

    private static readonly object SyncRoot = new object();

    private static readonly Queue<DiagnosticEvent> Events =
        new Queue<DiagnosticEvent>(MaximumEvents);

    private static int _discardedEventCount;

    private static readonly Regex SensitiveValuePattern = new Regex(
        @"(?ix)
            (
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
                    sessionid
                )
                \b
                \s* [:=] \s*
            )
            (
                ""[^""]*"" |
                '[^']*' |
                [^,\s;]+
            )",
        RegexOptions.Compiled);

    /// <summary>
    /// Records a concise, sanitized application event.
    ///
    /// Callers must provide only high-level, non-sensitive information.
    /// Do not pass article text, edit summaries, user names, full URLs,
    /// file paths, passwords, cookies, headers, or token values.
    /// </summary>
    /// <param name="category">
    /// A short event category, such as Startup, Settings, Login, API, or Error.
    /// </param>
    /// <param name="message">
    /// A concise, non-sensitive description of the event.
    /// </param>
    internal static void Record(string category, string message)
    {
        try
        {
            string safeCategory = SanitizeAndLimit(
                category,
                MaximumCategoryLength);

            string safeMessage = SanitizeAndLimit(
                message,
                MaximumMessageLength);

            if (string.IsNullOrEmpty(safeCategory))
                safeCategory = "General";

            if (string.IsNullOrEmpty(safeMessage))
                return;

            Thread thread = Thread.CurrentThread;

            DiagnosticEvent diagnosticEvent = new DiagnosticEvent(
                DateTime.Now,
                thread.ManagedThreadId,
                SanitizeAndLimit(thread.Name, 100),
                safeCategory,
                safeMessage);

            lock (SyncRoot)
            {
                if (Events.Count >= MaximumEvents)
                {
                    Events.Dequeue();
                    _discardedEventCount++;
                }

                Events.Enqueue(diagnosticEvent);
            }
        }
        catch
        {
            // Diagnostic recording must never interrupt normal application behavior.
        }
    }

    /// <summary>
    /// Returns a snapshot of recent events in oldest-to-newest order.
    /// </summary>
    /// <param name="discardedEventCount">
    /// The number of older events that were removed because the fixed-size
    /// event buffer reached its capacity.
    /// </param>
    /// <returns>Formatted, sanitized event descriptions.</returns>
    internal static string[] GetRecentEvents(out int discardedEventCount)
    {
        lock (SyncRoot)
        {
            discardedEventCount = _discardedEventCount;

            DiagnosticEvent[] snapshot = Events.ToArray();
            string[] results = new string[snapshot.Length];

            for (int index = 0; index < snapshot.Length; index++)
            {
                results[index] = snapshot[index].ToString();
            }

            return results;
        }
    }

    /// <summary>
    /// Removes line breaks, redacts common credential-style values, and limits
    /// the amount of data retained for one event field.
    /// </summary>
    private static string SanitizeAndLimit(string value, int maximumLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string sanitized = value
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        sanitized = SensitiveValuePattern.Replace(
            sanitized,
            "$1<removed>");

        if (sanitized.Length <= maximumLength)
            return sanitized;

        return sanitized.Substring(0, maximumLength) + " [truncated]";
    }

    /// <summary>
    /// Represents one retained diagnostic event.
    /// </summary>
    private sealed class DiagnosticEvent
    {
        private readonly DateTime _time;
        private readonly int _threadId;
        private readonly string _threadName;
        private readonly string _category;
        private readonly string _message;

        internal DiagnosticEvent(
            DateTime time,
            int threadId,
            string threadName,
            string category,
            string message)
        {
            _time = time;
            _threadId = threadId;
            _threadName = threadName;
            _category = category;
            _message = message;
        }

        public override string ToString()
        {
            string threadDescription =
                string.IsNullOrEmpty(_threadName)
                    ? "Thread " + _threadId
                    : "Thread " + _threadId + " (" + _threadName + ")";

            return _time.ToString(
                       "yyyy-MM-dd HH:mm:ss.fff",
                       CultureInfo.InvariantCulture)
                   + " | "
                   + threadDescription
                   + " | "
                   + _category
                   + " | "
                   + _message;
        }
    }
}