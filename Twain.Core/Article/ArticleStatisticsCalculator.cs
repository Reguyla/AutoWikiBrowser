using Twain.Core.Parse;

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
        MatchCollection images =
            WikiRegexes.ImagesCountOnly.Matches(articleText);

        string articleTextNoImagesUrls =
            WikiRegexes.ExternalLinksHTTPOnlyQuick.Replace(
                Tools.ReplaceWithSpaces(
                    articleText,
                    images),
                "");

        Dictionary<Parsers.DateLocale, int> dateCounts =
            Tools.DatesCount(articleTextNoImagesUrls);

        return new ArticleStatistics
        {
            WordCount =
                Tools.WordCount(articleText),

            CategoryCount =
                WikiRegexes.Category.Matches(articleText).Count,

            ImageCount =
                images.Count,

            LinkCount =
                Tools.LinkCount(articleText),

            InterwikiLinkCount =
                Tools.InterwikiCount(articleText),

            IsoDateCount =
                dateCounts[Parsers.DateLocale.ISO],

            InternationalDateCount =
                dateCounts[Parsers.DateLocale.International],

            AmericanDateCount =
                dateCounts[Parsers.DateLocale.American]
        };
    }
}