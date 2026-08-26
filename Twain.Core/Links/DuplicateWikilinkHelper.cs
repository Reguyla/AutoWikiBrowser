namespace Twain.Core.Links;

/// <summary>
/// Provides helpers for working with duplicate wikilink display values and
/// editor search patterns.
/// </summary>
public static class DuplicateWikilinkHelper
{
    /// <summary>
    /// Removes the appended duplicate count from a displayed duplicate wikilink.
    /// </summary>
    /// <param name="selectedItem">
    /// The duplicate-wikilink display text.
    /// </param>
    /// <returns>
    /// The wikilink text without the appended duplicate count.
    /// </returns>
    public static string ExtractWikilink(
        string selectedItem)
    {
        return Regex.Replace(
            selectedItem,
            @" \(\d+\)$",
            string.Empty);
    }

    /// <summary>
    /// Builds the regular expression used to locate a duplicate wikilink while
    /// allowing the first character of the link target to differ by case.
    /// </summary>
    /// <param name="link">
    /// The wikilink target to locate.
    /// </param>
    /// <returns>
    /// A regular expression matching the corresponding wikilink.
    /// </returns>
    public static string BuildSearchPattern(
        string link)
    {
        string firstCharacter =
            Regex.Escape(link[0].ToString());

        string remainingCharacters =
            Regex.Escape(link[1..]);

        return
            "\\[\\[(?i)" +
            firstCharacter +
            "(?-i)" +
            remainingCharacters +
            "(\\|.*?)?\\]\\]";
    }
}