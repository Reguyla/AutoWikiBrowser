namespace Twain.Core;

/// <summary>
/// Calculates basic statistics from article text.
/// </summary>
public static class ArticleStatisticsCalculator
{
    /// <summary>
    /// Calculates basic statistics for the supplied article text.
    /// </summary>
    /// <param name="articleText">
    /// The article text to analyze.
    /// </param>
    /// <returns>
    /// The calculated article statistics.
    /// </returns>
    public static ArticleStatistics Calculate(
        string articleText)
    {
        return new ArticleStatistics
        {
            WordCount =
                Tools.WordCount(articleText),

            CategoryCount =
                WikiRegexes.Category.Matches(articleText).Count,

            ImageCount =
                WikiRegexes.ImagesCountOnly.Matches(articleText).Count,

            LinkCount =
                Tools.LinkCount(articleText),

            InterwikiLinkCount =
                Tools.InterwikiCount(articleText)
        };
    }
}