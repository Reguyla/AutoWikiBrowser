using Twain.Core.Lists.Providers;

namespace Twain.Core.Disambiguation;

/// <summary>
/// Provides helpers for parsing disambiguation-page input and formatting
/// disambiguation link results.
/// </summary>
public static class DisambiguationLinkHelper
{
    /// <summary>
    /// Splits disambiguation-page input into individual page titles.
    /// </summary>
    /// <param name="text">
    /// The disambiguation-page input text.
    /// </param>
    /// <returns>
    /// The parsed page titles.
    /// </returns>
    public static string[] ParseLinkTitles(
        string text)
    {
        return text.Split(
            new[] { '|' },
            StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Builds disambiguation variant text from the supplied articles,
    /// excluding likely year articles.
    /// </summary>
    /// <param name="articles">
    /// The articles to process.
    /// </param>
    /// <returns>
    /// The article titles formatted as newline-separated text.
    /// </returns>
    public static string BuildVariantsText(
        IEnumerable<Article> articles)
    {
        StringBuilder builder = new();

        foreach (Article article in articles)
        {
            // Exclude likely year articles.
            if (uint.TryParse(
                    article.Name,
                    out uint year) &&
                year < 2100)
            {
                continue;
            }

            builder.AppendLine(article.Name);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Loads disambiguation links from the supplied page input and formats the
    /// resulting article titles as variant text.
    /// </summary>
    /// <param name="text">
    /// The disambiguation-page input text.
    /// </param>
    /// <returns>
    /// The resulting article titles formatted as newline-separated text,
    /// excluding likely year articles.
    /// </returns>
    public static string LoadVariantsText(
        string text)
    {
        string[] linkTitles =
            ParseLinkTitles(text);

        IEnumerable<Article> articles =
            new LinksOnPageListProvider().MakeList(linkTitles);

        return BuildVariantsText(articles);
    }
}