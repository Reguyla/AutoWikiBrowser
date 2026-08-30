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

    /// <summary>
    /// Finds all non-empty matches of the specified search expression in article text.
    /// </summary>
    /// <param name="articleText">
    /// The article text to search.
    /// </param>
    /// <param name="searchText">
    /// The text or regular expression to search for.
    /// </param>
    /// <param name="isRegex">
    /// <see langword="true"/> when <paramref name="searchText"/> is already a
    /// regular expression; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="caseSensitive">
    /// <see langword="true"/> to perform a case-sensitive search; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="articleName">
    /// The current article name used when expanding AWB search keywords.
    /// </param>
    /// <returns>
    /// A dictionary containing the zero-based starting index and length of each
    /// non-empty match.
    /// </returns>
    public static Dictionary<int, int> FindAll(
        string articleText,
        string searchText,
        bool isRegex,
        bool caseSensitive,
        string articleName)
    {
        Dictionary<int, int> found = new();

        if (string.IsNullOrEmpty(searchText))
            return found;

        string pattern = FormatRegex(
            searchText,
            articleName,
            isRegex);

        Regex regex = new(
            pattern,
            caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

        foreach (Match match in regex.Matches(articleText))
        {
            if (match.Length > 0)
                found.Add(match.Index, match.Length);
        }

        return found;
    }
}