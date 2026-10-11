namespace Twain.Core.Plugin;

/// <summary>
/// Provides custom modules with access to Twain application
/// services without depending on a particular user interface.
/// </summary>
public sealed class TwainHost : ITwainHost
{
    private readonly Action _stopProcessing;
    private readonly Action<string> _reportMessage;
    private readonly Func<bool> _isProcessing;

    /// <summary>
    /// Initializes a new Twain application host.
    /// </summary>
    public TwainHost(
        Session session,
        Func<bool> isProcessing,
        Action stopProcessing,
        Action<string> reportMessage)
    {
        Session = session
            ?? throw new ArgumentNullException(nameof(session));

        _isProcessing = isProcessing
            ?? throw new ArgumentNullException(nameof(isProcessing));

        _stopProcessing = stopProcessing
            ?? throw new ArgumentNullException(nameof(stopProcessing));

        _reportMessage = reportMessage
            ?? throw new ArgumentNullException(nameof(reportMessage));
    }

    /// <inheritdoc />
    public Session Session { get; }

    /// <inheritdoc />
    public Version ApplicationVersion =>
        typeof(TwainHost).Assembly.GetName().Version
        ?? new Version(1, 0);

    /// <inheritdoc />
    public string LanguageCode => Variables.LangCode;

    /// <inheritdoc />
    public bool IsProcessing => _isProcessing();

    /// <inheritdoc />
    public void StopProcessing()
    {
        _stopProcessing();
    }

    /// <inheritdoc />
    public void ReportMessage(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _reportMessage(message);
    }
}