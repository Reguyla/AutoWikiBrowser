namespace Twain.Core.Controls.Lists;

/// <summary>
/// Supported output formats for saved article lists.
/// </summary>
public enum ArticleListOutputFormat
{
    WikiText = 1,
    PlainText = 2,
    Csv = 3,
    CsvWikiText = 4
}

/// <summary>
/// Formats article collections for saving or exporting.
/// </summary>
public static class ArticleListOutputFormatter
{
    /// <summary>
    /// Formats the supplied articles using the requested output format.
    /// </summary>
    /// <param name="articles">
    /// The articles to format.
    /// </param>
    /// <param name="format">
    /// The output format to use.
    /// </param>
    /// <returns>
    /// The formatted article list.
    /// </returns>
    public static string Format(
        IEnumerable<Article> articles,
        ArticleListOutputFormat format)
    {
        ArgumentNullException.ThrowIfNull(articles);

        List<Article> articleList =
            articles.ToList();

        return format switch
        {
            ArticleListOutputFormat.WikiText =>
                FormatWikiText(articleList),

            ArticleListOutputFormat.PlainText =>
                FormatPlainText(articleList),

            ArticleListOutputFormat.Csv =>
                FormatCsv(articleList),

            ArticleListOutputFormat.CsvWikiText =>
                FormatCsvWikiText(articleList),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(format),
                    format,
                    "Unsupported article-list output format.")
        };
    }

    /// <summary>
    /// Formats the articles as a MediaWiki numbered list.
    /// </summary>
    private static string FormatWikiText(
        IEnumerable<Article> articles)
    {
        return string.Join(
            Environment.NewLine,
            articles.Select(
                article => $"# [[:{article}]]"));
    }

    /// <summary>
    /// Formats the articles as plain text with one title per line.
    /// </summary>
    private static string FormatPlainText(
        IEnumerable<Article> articles)
    {
        return string.Join(
            Environment.NewLine,
            articles.Select(
                article => article.ToString()));
    }

    /// <summary>
    /// Formats the articles as a comma-separated list of titles.
    /// </summary>
    private static string FormatCsv(
        IEnumerable<Article> articles)
    {
        return string.Join(
            ", ",
            articles.Select(
                article => article.ToString()));
    }

    /// <summary>
    /// Formats the articles as a comma-separated list of MediaWiki links.
    /// </summary>
    private static string FormatCsvWikiText(
        IEnumerable<Article> articles)
    {
        return string.Join(
            ", ",
            articles.Select(
                article => "[[:" + article + "]]"));
    }
}