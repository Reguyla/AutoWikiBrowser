namespace Twain.Diagnostics;

/// <summary>
/// Represents a single structured diagnostic event recorded by Twain.
/// </summary>
/// <remarks>
/// Diagnostic events contain a category, event name, optional message, and
/// structured data associated with the event. Storage and transmission are
/// handled separately by diagnostic sinks.
/// </remarks>
public sealed class DiagnosticEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticEvent"/> class.
    /// </summary>
    /// <param name="category">
    /// The diagnostic category associated with the event.
    /// </param>
    /// <param name="name">
    /// A short name identifying the event.
    /// </param>
    /// <param name="message">
    /// An optional human-readable description of the event.
    /// </param>
    /// <param name="data">
    /// Optional structured values associated with the event.
    /// </param>
    public DiagnosticEvent(
        DiagnosticCategory category,
        string name,
        string? message = null,
        IReadOnlyDictionary<string, object?>? data = null)
    {
        Timestamp = DateTimeOffset.UtcNow;
        Category = category;
        Name = name;
        Message = message;
        Data = data ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Gets the UTC timestamp at which the diagnostic event was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the category associated with the diagnostic event.
    /// </summary>
    public DiagnosticCategory Category { get; }

    /// <summary>
    /// Gets the short name identifying the diagnostic event.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional human-readable description of the event.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the structured data associated with the diagnostic event.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Data { get; }
}