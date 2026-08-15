namespace Twain.Diagnostics;

/// <summary>
/// Provides the central entry point for recording diagnostic information in Twain.
/// </summary>
/// <remarks>
/// Application code should use this class to record diagnostic events rather than
/// interacting directly with individual diagnostic sinks.
/// </remarks>
public static class TwainDiagnostics
{
    private static IDiagnosticSink? _sink;

    /// <summary>
    /// Configures the diagnostic sink used to process diagnostic events.
    /// </summary>
    /// <param name="sink">
    /// The diagnostic sink to use.
    /// </param>
    public static void Configure(IDiagnosticSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        _sink = sink;
    }

    /// <summary>
    /// Records a diagnostic event.
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
    /// Optional structured data associated with the event.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous diagnostic write operation.
    /// </returns>
    public static Task WriteAsync(
        DiagnosticCategory category,
        string name,
        string? message = null,
        IReadOnlyDictionary<string, object?>? data = null,
        CancellationToken cancellationToken = default)
    {
        if (_sink is null)
        {
            return Task.CompletedTask;
        }

        DiagnosticEvent diagnosticEvent =
            new(
                category,
                name,
                message,
                data);

        return _sink.WriteAsync(
            diagnosticEvent,
            cancellationToken);
    }

    /// <summary>
    /// Records an exception as a diagnostic event.
    /// </summary>
    /// <param name="exception">
    /// The exception to record.
    /// </param>
    /// <param name="name">
    /// A short name identifying the operation or component that encountered
    /// the exception.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous diagnostic write operation.
    /// </returns>
    public static Task ReportExceptionAsync(
        Exception exception,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Dictionary<string, object?> data = new()
        {
            ["ExceptionType"] = exception.GetType().FullName,
            ["StackTrace"] = exception.StackTrace,
            ["Source"] = exception.Source,
            ["InnerException"] = exception.InnerException?.ToString()
        };

        return WriteAsync(
            DiagnosticCategory.Exception,
            name,
            exception.Message,
            data,
            cancellationToken);
    }
}