namespace Twain.Core.Links;

/// <summary>
/// Provides helpers for working with wiki-link markup.
/// </summary>
public static class WikiLinkHelper
{
    /// <summary>
    /// Attempts to remove wiki-link markup from the supplied text while
    /// preserving the most appropriate display text.
    /// </summary>
    /// <param name="selectedText">
    /// The selected wiki-link text to process.
    /// </param>
    /// <param name="replacementText">
    /// The text that should replace the original selection.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the supplied text represents a wiki link;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryRemoveMarkup(
        string selectedText,
        out string replacementText)
    {
        replacementText = selectedText;

        if (!selectedText.StartsWith("[[") ||
            !selectedText.EndsWith("]]"))
        {
            return false;
        }

        replacementText =
            selectedText.Trim('[').Trim(']');

        if (replacementText.EndsWith("|"))
        {
            if (replacementText.Contains("(") &&
                replacementText.Contains(")"))
            {
                replacementText =
                    replacementText.Substring(
                        0,
                        replacementText.IndexOf(
                            "(",
                            StringComparison.Ordinal));
            }

            if (replacementText.Contains(":"))
            {
                replacementText =
                    replacementText.Substring(
                        replacementText.IndexOf(
                            ":",
                            StringComparison.Ordinal))
                    .TrimEnd('|');
            }

            if (selectedText ==
                "[[" + replacementText + "]]")
            {
                replacementText = selectedText;
            }
        }
        else if (replacementText.Contains("|"))
        {
            replacementText =
                replacementText.Substring(
                    replacementText.IndexOf(
                        "|",
                        StringComparison.Ordinal) + 1);
        }

        return true;
    }
}