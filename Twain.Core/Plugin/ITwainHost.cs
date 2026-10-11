namespace Twain.Core.Plugin;

/// <summary>
/// Provides custom modules and plugins with access to Twain
/// application information and processing services without
/// exposing user interface controls.
/// </summary>
public interface ITwainHost
{
    /// <summary>
    /// Gets the active wiki session.
    /// </summary>
    Session Session { get; }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    Version ApplicationVersion { get; }

    /// <summary>
    /// Gets the current wiki language code.
    /// </summary>
    string LanguageCode { get; }

    /// <summary>
    /// Gets a value indicating whether Twain is currently
    /// processing articles.
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Requests that article processing stop.
    /// </summary>
    void StopProcessing();

    /// <summary>
    /// Reports an informational message to the application.
    /// </summary>
    void ReportMessage(string message);
}