namespace Twain.Core.Editing;

/// <summary>
/// Tracks counters and rates for the current editing session.
/// </summary>
public sealed class SessionCounters
{
    /// <summary>
    /// Gets or sets the number of edits completed during the current session.
    /// </summary>
    public int NumberOfEdits { get; set; }

    /// <summary>
    /// Gets or sets the number of new pages processed during the current session.
    /// </summary>
    public int NumberOfNewPages { get; set; }

    /// <summary>
    /// Gets or sets the number of edits skipped during the current session.
    /// </summary>
    public int NumberOfIgnoredEdits { get; set; }

    /// <summary>
    /// Gets or sets the current number of edits completed per minute.
    /// </summary>
    public int NumberOfEditsPerMinute { get; set; }

    /// <summary>
    /// Gets or sets the current number of pages processed per minute.
    /// </summary>
    public int NumberOfPagesPerMinute { get; set; }

    /// <summary>
    /// Gets or sets the number of pages parsed during pre-parse mode.
    /// </summary>
    public int NumberOfPagesParsed { get; set; }

    /// <summary>
    /// Resets all session activity and rate counters.
    /// </summary>
    public void Reset()
    {
        NumberOfEdits = 0;
        NumberOfIgnoredEdits = 0;
        NumberOfNewPages = 0;
        NumberOfPagesParsed = 0;
        NumberOfEditsPerMinute = 0;
        NumberOfPagesPerMinute = 0;
    }
}