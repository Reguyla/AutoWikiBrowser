namespace Twain.Core.ArticleAnalysis;

/// <summary>
/// Represents a section of a MediaWiki article without modifying
/// its original wikitext.
/// </summary>
public sealed class ArticleSection
{
    /// <summary>
    /// Initializes an article section.
    /// </summary>
    public ArticleSection(
        string title,
        int level,
        int startIndex,
        int length,
        string heading,
        string content)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(heading);
        ArgumentNullException.ThrowIfNull(content);

        if (level < 0 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level));

        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        Title = title;
        Level = level;
        StartIndex = startIndex;
        Length = length;
        Heading = heading;
        Content = content;
        LogicalContent = content;
    }

    /// <summary>
    /// Gets the section title, or an empty string for the lead section.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the heading level. Zero identifies the lead section.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets the section's starting character position in the original article.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// Gets the total section length, including its heading.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the original heading markup, excluding the section body.
    /// </summary>
    public string Heading { get; }

    /// <summary>
    /// Gets the section body without its heading.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the exclusive ending character position.
    /// </summary>
    public int EndIndex => StartIndex + Length;

    /// <summary>
    /// Gets whether this represents the article's lead section.
    /// </summary>
    public bool IsLead => Level == 0;

    /// <summary>
    /// Gets the original section text, including its heading.
    /// </summary>
    public string Text => Heading + Content;

    /// <summary>
    /// Gets the section content excluding recognized trailing metadata.
    /// Defaults to the original content when no metadata is excluded.
    /// </summary>
    public string LogicalContent { get; internal set; }
}