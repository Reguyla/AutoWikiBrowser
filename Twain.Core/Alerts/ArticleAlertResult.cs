namespace Twain.Core.Alerts;

/// <summary>
/// Contains the results produced by article alert evaluation.
/// </summary>
public sealed class ArticleAlertResult
{
    /// <summary>
    /// Gets the alert messages generated for the article.
    /// </summary>
    public List<string> Alerts { get; } = new();

    /// <summary>
    /// Gets unbalanced-bracket positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> UnbalancedBrackets { get; internal set; } =
        new();

    /// <summary>
    /// Gets targetless-link positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> TargetlessLinks { get; internal set; } =
        new();

    /// <summary>
    /// Gets double-pipe-link positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> DoublePipeLinks { get; internal set; } =
        new();

    /// <summary>
    /// Gets additional article-structure error positions keyed by character
    /// offset, with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> OtherErrors { get; internal set; } =
        new();

    /// <summary>
    /// Gets dead-link positions keyed by character offset, with the associated
    /// highlight length.
    /// </summary>
    public Dictionary<int, int> DeadLinks { get; internal set; } =
        new();

    /// <summary>
    /// Gets ambiguous citation-date positions keyed by character offset, with
    /// the associated highlight length.
    /// </summary>
    public Dictionary<int, int> AmbiguousCiteDates { get; internal set; } =
        new();

    /// <summary>
    /// Gets wikilinked-header positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> WikilinkedHeaders { get; internal set; } =
        new();

    /// <summary>
    /// Gets unclosed-tag positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> UnclosedTags { get; internal set; } =
        new();

    /// <summary>
    /// Gets invalid citation-parameter positions keyed by character offset,
    /// with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> BadCiteParameters { get; internal set; } =
        new();

    /// <summary>
    /// Gets duplicate WikiProject banner-shell parameter positions keyed by
    /// character offset, with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> DuplicateBannerShellParameters { get; internal set; } =
        new();

    /// <summary>
    /// Gets editor-signature or user-space-link positions keyed by character
    /// offset, with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> UserSignatures { get; internal set; } =
        new();

    /// <summary>
    /// Gets unknown parameters found in Multiple issues templates.
    /// </summary>
    public List<string> UnknownMultipleIssuesParameters { get; internal set; } =
        new();

    /// <summary>
    /// Gets unknown parameters found in WikiProject banner-shell templates.
    /// </summary>
    public List<string> UnknownWikiProjectBannerShellParameters { get; internal set; } =
        new();
}