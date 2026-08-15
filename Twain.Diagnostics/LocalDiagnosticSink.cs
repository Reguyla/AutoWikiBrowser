using System.Text.Json;

namespace Twain.Diagnostics;

/// <summary>
/// Writes diagnostic events to a local JSON Lines file.
/// </summary>
/// <remarks>
/// Each diagnostic event is serialized as a single JSON object on its own line.
/// The sink performs local storage only and does not transmit diagnostic data
/// to an external service.
/// </remarks>
public sealed class LocalDiagnosticSink : IDiagnosticSink
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDiagnosticSink"/> class.
    /// </summary>
    /// <param name="filePath">
    /// The path of the JSON Lines file used to store diagnostic events.
    /// </param>
    public LocalDiagnosticSink(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        DiagnosticEvent diagnosticEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvent);

        string? directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(
            diagnosticEvent,
            JsonOptions);

        await _writeLock.WaitAsync(cancellationToken);

        try
        {
            await File.AppendAllTextAsync(
                _filePath,
                json + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}