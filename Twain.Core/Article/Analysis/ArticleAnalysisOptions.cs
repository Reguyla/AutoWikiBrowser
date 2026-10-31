namespace Twain.Core.ArticleAnalysis;

/// <summary>
/// Configures how article structure and trailing metadata are analyzed.
/// </summary>
public sealed class ArticleAnalysisOptions
{
    /// <summary>
    /// Gets the names of templates recognized as trailing metadata.
    /// </summary>
    /// <remarks>
    /// Template names are compared without regard to case.
    /// Entries should not include the surrounding template braces.
    /// </remarks>
    public ISet<string> TrailingMetadataTemplates { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the interwiki prefixes recognized as trailing metadata.
    /// </summary>
    /// <remarks>
    /// Prefixes are compared without regard to case.
    /// Only prefixes explicitly configured here are recognized.
    /// </remarks>
    public ISet<string> TrailingInterwikiPrefixes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}