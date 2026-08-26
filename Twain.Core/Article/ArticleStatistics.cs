namespace Twain.Core;

/// <summary>
/// Contains basic statistics calculated from article text.
/// </summary>
public sealed class ArticleStatistics
{
    /// <summary>
    /// Gets the number of words in the article.
    /// </summary>
    public int WordCount { get; init; }

    /// <summary>
    /// Gets the number of categories in the article.
    /// </summary>
    public int CategoryCount { get; init; }

    /// <summary>
    /// Gets the number of images in the article.
    /// </summary>
    public int ImageCount { get; init; }

    /// <summary>
    /// Gets the number of wikilinks in the article.
    /// </summary>
    public int LinkCount { get; init; }

    /// <summary>
    /// Gets the number of interwiki links in the article.
    /// </summary>
    public int InterwikiLinkCount { get; init; }
}