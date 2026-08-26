namespace Twain.Core.Alerts;

/// <summary>
/// Contains the results of article-structure alert evaluation.
/// </summary>
public sealed class ArticleStructureAlertResult
{
    /// <summary>
    /// Gets the alert messages generated during structure analysis.
    /// </summary>
    public IReadOnlyList<string> Alerts { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Gets unbalanced-bracket positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> UnbalancedBrackets { get; init; } =
        new();

    /// <summary>
    /// Gets targetless-link positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> TargetlessLinks { get; init; } =
        new();

    /// <summary>
    /// Gets double-pipe-link positions keyed by character offset, with the
    /// associated highlight length.
    /// </summary>
    public Dictionary<int, int> DoublePipeLinks { get; init; } =
        new();

    /// <summary>
    /// Gets additional structure-error positions keyed by character offset,
    /// with the associated highlight length.
    /// </summary>
    public Dictionary<int, int> OtherErrors { get; init; } =
        new();
}