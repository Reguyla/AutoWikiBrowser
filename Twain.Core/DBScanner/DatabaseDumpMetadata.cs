namespace Twain.Core.DBScanner;

/// <summary>
/// Contains metadata read from a MediaWiki XML database dump.
/// </summary>
public sealed class DatabaseDumpMetadata
{
    /// <summary>
    /// Gets or sets the wiki site name.
    /// </summary>
    public string SiteName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base wiki URL recorded in the dump.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MediaWiki generator version recorded in the dump.
    /// </summary>
    public string Generator { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the wiki title case setting recorded in the dump.
    /// </summary>
    public string Case { get; set; } = string.Empty;
}