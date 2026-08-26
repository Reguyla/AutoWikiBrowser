namespace Twain.Core.Alerts;

/// <summary>
/// Contains the results of talk-page and user-namespace alert evaluation.
/// </summary>
public sealed class TalkAndUserAlertResult
{
    /// <summary>
    /// Gets the alert messages generated during talk-page and user-namespace
    /// analysis.
    /// </summary>
    public IReadOnlyList<string> Alerts { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Gets duplicate WikiProject banner-shell parameter positions keyed by
    /// character offset, with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> DuplicateBannerShellParameters { get; init; } =
        new();

    /// <summary>
    /// Gets unknown WikiProject banner-shell parameter names.
    /// </summary>
    public List<string> UnknownWikiProjectBannerShellParameters { get; init; } =
        new();

    /// <summary>
    /// Gets editor-signature or user-space-link positions keyed by character
    /// offset, with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> UserSignatures { get; init; } =
        new();
}