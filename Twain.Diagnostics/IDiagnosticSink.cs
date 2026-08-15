namespace Twain.Diagnostics;

/// <summary>
/// Defines a destination that receives diagnostic events produced by Twain.
/// </summary>
/// <remarks>
/// Diagnostic sinks are responsible for persisting, forwarding, or otherwise
/// processing diagnostic events. The diagnostics framework can use different
/// sink implementations without changing the code that creates the events.
/// </remarks>
public interface IDiagnosticSink
{
    /// <summary>
    /// Writes a diagnostic event to the sink.
    /// </summary>
    /// <param name="diagnosticEvent">
    /// The diagnostic event to write.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous write operation.
    /// </returns>
    Task WriteAsync(
        DiagnosticEvent diagnosticEvent,
        CancellationToken cancellationToken = default);
}