namespace Twain.Core.Controls.Lists;

/// <summary>
/// Provides article-list splitting operations used by the list splitter.
/// </summary>
public static class ListSplitterProcessor
{
    private static readonly Regex BadCharacters =
        new(
            @"[""/:*?<>|.]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Removes characters that cannot be used in the generated output
    /// file name.
    /// </summary>
    /// <param name="sourceText">
    /// The source text used to derive the output file name.
    /// </param>
    /// <returns>
    /// The source text with unsupported file-name characters removed.
    /// </returns>
    public static string CreateFileName(string sourceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        return BadCharacters.Replace(sourceText, string.Empty);
    }

    /// <summary>
    /// Calculates the number of output groups produced by the legacy list
    /// splitter algorithm.
    /// </summary>
    /// <param name="articleCount">
    /// The number of articles to split.
    /// </param>
    /// <param name="articlesPerFile">
    /// The requested maximum number of articles per output file.
    /// </param>
    /// <returns>
    /// The number of output groups.
    /// </returns>
    public static int CalculateGroupCount(
        int articleCount,
        int articlesPerFile)
    {
        if (articleCount < 0)
            throw new ArgumentOutOfRangeException(nameof(articleCount));

        if (articlesPerFile <= 0)
            throw new ArgumentOutOfRangeException(nameof(articlesPerFile));

        int adjustedArticleCount = articleCount;
        int roundLimit = articlesPerFile / 2;

        if ((articleCount % articlesPerFile) <= roundLimit)
            adjustedArticleCount += roundLimit;

        return Convert.ToInt32(
            Math.Round(
                (decimal)adjustedArticleCount / articlesPerFile));
    }
}