namespace Twain.Core.Editing;

/// <summary>
/// Provides editor-independent helpers for preparing article search expressions.
/// </summary>
internal static class ArticleSearchHelper
{
    /// <summary>
    /// Prepares a search expression for matching against article text.
    /// </summary>
    /// <param name="searchText">
    /// The text or regular expression to prepare.
    /// </param>
    /// <param name="articleName">
    /// The current article name used when expanding AWB search keywords.
    /// </param>
    /// <param name="isRegex">
    /// <see langword="true"/> when <paramref name="searchText"/> is already a
    /// regular expression; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The search expression after keyword expansion and any required escaping.
    /// </returns>
    /// <remarks>
    /// AWB keywords are expanded before the expression is processed. Literal
    /// searches are escaped for use as regular expressions while preserving
    /// <c>\n</c> as a newline search sequence.
    /// </remarks>
    public static string FormatRegex(
        string searchText,
        string articleName,
        bool isRegex)
    {
        searchText = Tools.ApplyKeyWords(articleName, searchText);

        if (!isRegex)
        {
            bool newlines = searchText.Contains("\\n");
            searchText = Regex.Escape(searchText);

            if (newlines)
                searchText = searchText.Replace(@"\\n", "\n");
        }

        return searchText;
    }
}