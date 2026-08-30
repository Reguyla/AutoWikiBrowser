namespace Twain.Core.Disambiguation;

/// <summary>
/// Provides non-UI processing used by the disambiguation workflow.
/// </summary>
public static class DisambiguationProcessor
{
    /// <summary>
    /// Matches an end of a wikilink followed by a
    /// {{Disambiguation needed}} template and punctuation.
    /// </summary>
    private static readonly Regex DisambiguationNeededPunctuation =
        new(@"(\]\])({{Disambiguation needed}})([.,'"":;]+)");

    /// <summary>
    /// Normalizes the supplied disambiguation variants by trimming values
    /// and removing empty entries.
    /// </summary>
    /// <param name="variants">
    /// The candidate disambiguation variants.
    /// </param>
    /// <returns>
    /// The normalized variants.
    /// </returns>
    public static List<string> NormalizeVariants(IEnumerable<string> variants)
    {
        return variants
            .Select(variant => variant.Trim())
            .Where(variant => variant.Length > 0)
            .ToList();
    }

    /// <summary>
    /// Creates the regular expression used to locate links requiring
    /// disambiguation.
    /// </summary>
    /// <param name="dabLink">
    /// The link, or pipe-separated link variants, to locate.
    /// </param>
    /// <returns>
    /// A regular expression matching the target wikilinks.
    /// </returns>
    public static Regex CreateSearchRegex(string dabLink)
    {
        string linkPattern = CreateLinkPattern(dabLink);

        return new Regex(
            @"\[\[\s*("
            + linkPattern
            + @")\s*(?:|#[^\|\]]*)(|\|[^\]]*)\]\]"
            + @"([\p{Ll}\p{Lu}\p{Lt}\p{Pc}\p{Lm}]*)");
    }

    /// <summary>
    /// Applies the selected disambiguation results to the article text.
    /// </summary>
    /// <param name="articleText">
    /// The original article text.
    /// </param>
    /// <param name="search">
    /// The regular expression identifying links requiring disambiguation.
    /// </param>
    /// <param name="results">
    /// The selected replacement results in the same order as the matches.
    /// </param>
    /// <returns>
    /// The article text with the selected disambiguation changes applied.
    /// </returns>
    public static string ApplyResults(
        string articleText,
        Regex search,
        IReadOnlyList<DisambiguationResult> results)
    {
        int index = 0;
        bool hasDisambiguationNeeded = false;

        string newText = search.Replace(
            articleText,
            match =>
            {
                DisambiguationResult result = results[index++];

                string replacement =
                    result.NoChange
                        ? match.Value
                        : result.Result;

                if (replacement.Contains(
                        "{{Disambiguation needed}}",
                        StringComparison.Ordinal))
                {
                    hasDisambiguationNeeded = true;
                }

                return replacement;
            });

        // Want ''[[link]]''{{Disambiguation needed}} rather than
        // ''[[link]]{{Disambiguation needed}}''.
        if (hasDisambiguationNeeded)
        {
            newText = DisambiguationNeededPunctuation.Replace(
                newText,
                "$1$3$2");
        }

        return Parse.Parsers.StickyLinks(newText);
    }

    /// <summary>
    /// Creates the regular-expression pattern for the supplied
    /// disambiguation link.
    /// </summary>
    private static string CreateLinkPattern(string dabLink)
    {
        if (!dabLink.Contains('|'))
        {
            return Tools.FirstLetterCaseInsensitive(
                Regex.Escape(dabLink.Trim()));
        }

        string pattern = string.Join(
            "|",
            dabLink
                .Split('|')
                .Select(link => link.Trim())
                .Where(link => link.Length > 0)
                .Select(
                    link => Tools.FirstLetterCaseInsensitive(
                        Regex.Escape(link))));

        if (pattern.Contains('|'))
            pattern = "(?:" + pattern + ")";

        return pattern;
    }

    /// <summary>
    /// Represents the selected result for one disambiguation occurrence.
    /// </summary>
    /// <param name="NoChange">
    /// Whether the original wikilink should remain unchanged.
    /// </param>
    /// <param name="Result">
    /// The replacement text selected for the occurrence.
    /// </param>
    public sealed record DisambiguationResult(
        bool NoChange,
        string Result);
}