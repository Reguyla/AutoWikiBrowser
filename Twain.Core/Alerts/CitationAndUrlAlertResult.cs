namespace Twain.Core.Alerts;

/// <summary>
/// Contains the results of citation and URL alert evaluation.
/// </summary>
public sealed class CitationAndUrlAlertResult
{
    /// <summary>
    /// Gets the alert messages generated during citation and URL analysis.
    /// </summary>
    public IReadOnlyList<string> Alerts { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Gets dead-link positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> DeadLinks { get; init; } =
        new();

    /// <summary>
    /// Gets ambiguous citation-date positions keyed by character offset,
    /// with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> AmbiguousCiteDates { get; init; } =
        new();

    /// <summary>
    /// Gets wikilinked-header positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> WikilinkedHeaders { get; init; } =
        new();

    /// <summary>
    /// Gets unclosed-tag positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> UnclosedTags { get; init; } =
        new();

    /// <summary>
    /// Gets invalid citation-parameter positions keyed by character offset,
    /// with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> BadCiteParameters { get; init; } =
        new();

    /// <summary>
    /// Gets unknown parameters found in Multiple issues templates.
    /// </summary>
    public List<string> UnknownMultipleIssuesParameters { get; init; } =
        new();
}