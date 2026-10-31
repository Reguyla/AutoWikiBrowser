namespace Twain.Core.ArticleAnalysis;

/// <summary>
/// Represents the analyzed structure of a MediaWiki article.
/// This model does not modify or normalize the original wikitext.
/// </summary>
public sealed class ArticleStructure
{
    /// <summary>
    /// Initializes an article structure.
    /// </summary>
    public ArticleStructure(
        string originalText,
        ArticleSection lead,
        IEnumerable<ArticleSection> sections,
        IEnumerable<ArticleMetadata> trailingMetadata)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(lead);
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(trailingMetadata);

        OriginalText = originalText;
        Lead = lead;

        Sections = Array.AsReadOnly(sections.ToArray());
        TrailingMetadata = Array.AsReadOnly(trailingMetadata.ToArray());
    }

    /// <summary>
    /// Gets the original, unmodified article text.
    /// </summary>
    public string OriginalText { get; }

    /// <summary>
    /// Gets the article's lead section.
    /// </summary>
    public ArticleSection Lead { get; }

    /// <summary>
    /// Gets the named sections in their original order.
    /// </summary>
    public IReadOnlyList<ArticleSection> Sections { get; }

    /// <summary>
    /// Gets all sections, including the lead.
    /// </summary>
    public IEnumerable<ArticleSection> AllSections
    {
        get
        {
            yield return Lead;

            foreach (ArticleSection section in Sections)
                yield return section;
        }
    }

    /// <summary>
    /// Gets whether the article contains named sections.
    /// </summary>
    public bool HasSections => Sections.Count > 0;

    /// <summary>
    /// Gets the number of named sections.
    /// </summary>
    public int SectionCount => Sections.Count;

    /// <summary>
    /// Gets the trailing metadata declarations recognized in the article.
    /// </summary>
    public IReadOnlyList<ArticleMetadata> TrailingMetadata { get; }

    /// <summary>
    /// Gets the starting position of the trailing metadata region.
    /// Returns the article length when no trailing metadata is recognized.
    /// </summary>
    public int TrailingMetadataStartIndex =>
        TrailingMetadata.Count > 0
            ? TrailingMetadata[0].StartIndex
            : OriginalText.Length;

    /// <summary>
    /// Gets the original article text preceding the first recognized
    /// trailing metadata declaration, including any separating whitespace.
    /// </summary>
    public string TextBeforeTrailingMetadata =>
        OriginalText.Substring(0, TrailingMetadataStartIndex);
}