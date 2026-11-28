using Twain.Core.Processing;

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
    /// Prepares the disambiguation workflow by normalizing the supplied
    /// variants, creating the search expression, and locating matching links.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="dabLink">
    /// The link, or pipe-separated link variants, to locate.
    /// </param>
    /// <param name="dabVariants">
    /// The candidate disambiguation variants.
    /// </param>
    /// <returns>
    /// The prepared disambiguation data.
    /// </returns>
    public static DisambiguationPreparation Prepare(
        string articleText,
        string dabLink,
        IEnumerable<string> dabVariants,
        int contextChars)
    {
        List<string> variants = NormalizeVariants(dabVariants);
        Regex search = CreateSearchRegex(dabLink);
        MatchCollection matches = search.Matches(articleText);

        List<DisambiguationItemPreparation> items =
            PrepareItems(
                articleText,
                matches,
                contextChars);

        return new DisambiguationPreparation(
            variants,
            search,
            items);
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
    /// Applies the selected disambiguation results and collects
    /// the individual corrections made to the article.
    /// </summary>
    public static DisambiguationProcessingResult
        ApplyResultsWithCorrections(
            string articleText,
            DisambiguationPreparation preparation,
            IReadOnlyList<DisambiguationResult> results)
    {
        IReadOnlyList<DisambiguationCorrection> corrections =
            CollectCorrections(preparation, results);

        string updatedText = ApplyResults(
            articleText,
            preparation.Search,
            results);

        return new DisambiguationProcessingResult(
            updatedText,
            corrections);
    }

    /// <summary>
    /// Contains the prepared data required to begin a disambiguation operation.
    /// </summary>
    /// <param name="Variants">
    /// The normalized candidate variants.
    /// </param>
    /// <param name="Search">
    /// The regular expression used to locate matching wikilinks.
    /// </param>
    /// <param name="Items">
    /// The prepared disambiguation occurrences found in the article.
    /// </param>
    public sealed record DisambiguationPreparation(
        IReadOnlyList<string> Variants,
        Regex Search,
        IReadOnlyList<DisambiguationItemPreparation> Items);

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

    /// <summary>
    /// Identifies the type of correction made to an ambiguous link.
    /// </summary>
    public enum DisambiguationCorrectionType
    {
        Resolved,
        Unlinked,
        Tagged,
        Removed,
        Modified
    }

    /// <summary>
    /// Describes an individual correction made during disambiguation.
    /// </summary>
    /// <remarks>
    /// Preserves the original and replacement text for edit-summary
    /// generation and future diagnostic reporting.
    /// </remarks>
    public sealed record DisambiguationCorrection(
        DisambiguationCorrectionType CorrectionType,
        string OriginalTarget,
        string? ReplacementTarget,
        string OriginalText,
        string ReplacementText);

    /// <summary>
    /// Contains the updated article text and the individual
    /// corrections produced by a disambiguation operation.
    /// </summary>
    public sealed record DisambiguationProcessingResult(
        string ArticleText,
        IReadOnlyList<DisambiguationCorrection> Corrections);

    /// <summary>
    /// Collects the individual corrections made during disambiguation.
    /// </summary>
    /// <param name="preparation">
    /// The original prepared disambiguation occurrences.
    /// </param>
    /// <param name="results">
    /// The final replacement results in occurrence order.
    /// </param>
    /// <returns>
    /// The corrections made to the original ambiguous links.
    /// </returns>
    public static IReadOnlyList<DisambiguationCorrection> CollectCorrections(
        DisambiguationPreparation preparation,
        IReadOnlyList<DisambiguationResult> results)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        ArgumentNullException.ThrowIfNull(results);

        if (preparation.Items.Count != results.Count)
        {
            throw new ArgumentException(
                "The number of results must match the number of prepared occurrences.",
                nameof(results));
        }

        var corrections = new List<DisambiguationCorrection>();

        for (int index = 0; index < results.Count; index++)
        {
            DisambiguationItemPreparation item =
                preparation.Items[index];

            DisambiguationResult result = results[index];

            if (result.NoChange ||
                string.Equals(
                    item.OriginalLink,
                    result.Result,
                    StringComparison.Ordinal))
            {
                continue;
            }

            corrections.Add(
                ClassifyCorrection(item, result.Result));
        }

        return corrections;
    }

    /// <summary>
    /// Classifies a correction by comparing the original ambiguous
    /// link with its final replacement text.
    /// </summary>
    private static DisambiguationCorrection ClassifyCorrection(
        DisambiguationItemPreparation item,
        string replacement)
    {
        DisambiguationCorrectionType correctionType =
            DisambiguationCorrectionType.Modified;

        string? replacementTarget = null;

        if (string.IsNullOrWhiteSpace(replacement))
        {
            correctionType =
                DisambiguationCorrectionType.Removed;
        }
        else if (string.Equals(
            replacement,
            item.VisibleLink + item.LinkTrail,
            StringComparison.Ordinal))
        {
            correctionType =
                DisambiguationCorrectionType.Unlinked;
        }
        else
        {
            MatchCollection links = Regex.Matches(
                replacement,
                @"\[\[\s*([^\[\]\|]+)(?:\|[^\[\]]*)?\]\]");

            if (links.Count == 1)
            {
                replacementTarget =
                    links[0].Groups[1].Value.Trim();

                if (string.Equals(
                    item.RealLink,
                    replacementTarget,
                    StringComparison.Ordinal))
                {
                    if (Regex.IsMatch(
                        replacement,
                        @"\{\{\s*Disambiguation needed\s*(?:\||\}\})",
                        RegexOptions.IgnoreCase))
                    {
                        correctionType =
                            DisambiguationCorrectionType.Tagged;
                    }
                }
                else
                {
                    correctionType =
                        DisambiguationCorrectionType.Resolved;
                }
            }
        }

        return new DisambiguationCorrection(
            correctionType,
            item.RealLink,
            replacementTarget,
            item.OriginalLink,
            replacement);
    }

    /// <summary>
    /// Creates edit-summary contributions from the collected
    /// disambiguation corrections.
    /// </summary>
    /// <remarks>
    /// Identical correction types and link targets are grouped
    /// together to provide accurate occurrence counts.
    /// </remarks>
    public static IReadOnlyList<EditSummaryContribution>
        CreateSummaryContributions(
            IReadOnlyList<DisambiguationCorrection> corrections)
    {
        ArgumentNullException.ThrowIfNull(corrections);

        var contributions = new List<EditSummaryContribution>();

        var groups = corrections.GroupBy(correction => new
        {
            correction.CorrectionType,
            correction.OriginalTarget,
            correction.ReplacementTarget
        });

        foreach (var group in groups)
        {
            int count = group.Count();

            string description = FormatCorrectionDescription(
                group.Key.CorrectionType,
                group.Key.OriginalTarget,
                group.Key.ReplacementTarget);

            if (count > 1)
            {
                description += $" (x{count})";
            }

            contributions.Add(
                new EditSummaryContribution
                {
                    Category = EditSummaryCategory.Disambiguation,
                    Description = description,
                    ShortDescription =
                        $"Disambiguation: {group.Key.OriginalTarget}"
                });
        }

        return contributions;
    }

    /// <summary>
    /// Formats a description of an individual disambiguation
    /// correction or group of identical corrections.
    /// </summary>
    private static string FormatCorrectionDescription(
        DisambiguationCorrectionType correctionType,
        string originalTarget,
        string? replacementTarget)
    {
        return correctionType switch
        {
            DisambiguationCorrectionType.Resolved =>
                $"Disambiguated: {originalTarget} → {replacementTarget}",

            DisambiguationCorrectionType.Unlinked =>
                $"Unlinked ambiguous link: {originalTarget}",

            DisambiguationCorrectionType.Tagged =>
                $"Tagged ambiguous link: {originalTarget}",

            DisambiguationCorrectionType.Removed =>
                $"Removed ambiguous link: {originalTarget}",

            _ =>
                $"Modified ambiguous link: {originalTarget}"
        };
    }

    /// <summary>
    /// Prepares the data required to display one disambiguation occurrence.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="match">
    /// The matched wikilink requiring disambiguation.
    /// </param>
    /// <param name="contextChars">
    /// The approximate number of characters to include on each side of the
    /// matched link when determining its surrounding context.
    /// </param>
    /// <returns>
    /// The prepared data for the disambiguation occurrence.
    /// </returns>
    public static DisambiguationItemPreparation PrepareItem(
        string articleText,
        Match match,
        int contextChars)
    {
        int posStart;

        // Find the beginning of the paragraph containing the link.
        for (posStart = match.Index; posStart > 0; posStart--)
        {
            if (!"\n\r".Contains(articleText[posStart] + ""))
                continue;

            posStart++;
            break;
        }

        int posEnd = match.Index + match.Value.Length;

        string visibleLink;
        string realLink;

        if (string.IsNullOrEmpty(match.Groups[2].Value))
        {
            visibleLink = match.Groups[1].Value.Trim();
            realLink = visibleLink;
        }
        else
        {
            visibleLink = match.Groups[2].Value.Trim();
            realLink = match.Groups[1].Value.Trim();
        }

        visibleLink = visibleLink.TrimStart('|');

        string linkTrail = match.Groups[3].Value;

        // Find the end of the paragraph containing the link.
        while (posEnd < articleText.Length - 1 &&
               !"\n\r".Contains(articleText[posEnd] + ""))
        {
            posEnd++;
        }

        // Find the surrounding context.
        int contextStart = match.Index - contextChars;

        if (contextStart < posStart)
            contextStart = posStart;

        for (; contextStart > posStart; contextStart--)
        {
            if (!char.IsSeparator(articleText[contextStart]))
                continue;

            contextStart++;
            break;
        }

        int contextEnd =
            match.Index + match.Length + contextChars;

        if (contextEnd > posEnd)
            contextEnd = posEnd;

        for (; contextEnd < posEnd; contextEnd++)
        {
            if (char.IsSeparator(articleText[contextEnd]))
                break;
        }

        string surroundings =
            articleText.Substring(
                contextStart,
                contextEnd - contextStart);

        // Determine whether the link occurs at the beginning of a sentence.
        bool startOfSentence = false;
        int position;

        for (position = match.Index - 1; position > posStart; --position)
        {
            if (articleText[position] == '.')
            {
                startOfSentence = true;
                break;
            }

            if (!char.IsWhiteSpace(articleText[position]))
                break;
        }

        if (position == posStart)
            startOfSentence = true;

        string paragraphText =
            articleText.Substring(
                posStart,
                posEnd - posStart);

        return new DisambiguationItemPreparation(
            visibleLink,
            realLink,
            linkTrail,
            surroundings,
            startOfSentence,
            match.Value,
            paragraphText,
            match.Index - posStart,
            match.Length,
            contextStart - posStart);
    }

    /// <summary>
    /// Contains the prepared data for one disambiguation occurrence.
    /// </summary>
    /// <param name="PositionStart">
    /// The beginning of the paragraph containing the matched link.
    /// </param>
    /// <param name="PositionEnd">
    /// The end of the paragraph containing the matched link.
    /// </param>
    /// <param name="VisibleLink">
    /// The text displayed by the wikilink.
    /// </param>
    /// <param name="RealLink">
    /// The actual target of the wikilink.
    /// </param>
    /// <param name="LinkTrail">
    /// Any link-trail characters following the wikilink.
    /// </param>
    /// <param name="SurroundingsStart">
    /// The starting position of the displayed surrounding context.
    /// </param>
    /// <param name="Surroundings">
    /// The surrounding article text used as context for the matched link.
    /// </param>
    /// <param name="StartOfSentence">
    /// Whether the matched link occurs at the beginning of a sentence.
    /// </param>
    public sealed record DisambiguationItemPreparation(
        string VisibleLink,
        string RealLink,
        string LinkTrail,
        string Surroundings,
        bool StartOfSentence,
        string OriginalLink,
        string ParagraphText,
        int MatchPosition,
        int MatchLength,
        int SurroundingsPosition);

    /// <summary>
    /// Creates the replacement wikilink for a selected disambiguation choice.
    /// </summary>
    /// <param name="selectedIndex">
    /// The selected disambiguation choice index.
    /// </param>
    /// <param name="originalMatch">
    /// The original matched wikilink.
    /// </param>
    /// <param name="visibleLink">
    /// The text displayed by the wikilink.
    /// </param>
    /// <param name="realLink">
    /// The actual target of the wikilink.
    /// </param>
    /// <param name="linkTrail">
    /// Any link-trail characters following the wikilink.
    /// </param>
    /// <param name="startOfSentence">
    /// Whether the link occurs at the beginning of a sentence.
    /// </param>
    /// <param name="variants">
    /// The available disambiguation target variants.
    /// </param>
    /// <returns>
    /// The replacement text for the selected choice.
    /// </returns>
    public static string CreateReplacement(
        int selectedIndex,
        string originalMatch,
        string visibleLink,
        string realLink,
        string linkTrail,
        bool startOfSentence,
        IReadOnlyList<string> variants)
    {
        switch (selectedIndex)
        {
            case 0:
                return originalMatch;

            case 1:
                return visibleLink + linkTrail;

            case 2:
                return originalMatch
                    + "{{Disambiguation needed|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}";

            default:
                string target = variants[selectedIndex - 3];

                if (startOfSentence || char.IsUpper(realLink[0]))
                    target = Tools.TurnFirstToUpper(target);

                string replacement =
                    "[[" + target + "|" + visibleLink;

                if (realLink == visibleLink)
                    replacement += linkTrail + "]]";
                else
                    replacement += "]]" + linkTrail;

                return Parse.Parsers.SimplifyLinks(replacement);
        }
    }

    /// <summary>
    /// Removes the displayed-text portion from a piped wikilink.
    /// </summary>
    /// <param name="link">
    /// The wikilink text to transform.
    /// </param>
    /// <returns>
    /// The unpiped wikilink text.
    /// </returns>
    public static string UnpipeLink(string link)
    {
        return Regex.Replace(
            link,
            @"\[\[\s*([^\|\]]*)\s*\|\s*[^\]]*\s*\]\](.*)",
            "[[$1]]$2");
    }

    /// <summary>
    /// Swaps the target and displayed-text portions of a piped wikilink.
    /// </summary>
    /// <param name="link">
    /// The wikilink text to transform.
    /// </param>
    /// <returns>
    /// The wikilink with its piped portions reversed.
    /// </returns>
    public static string FlipLink(string link)
    {
        return Regex.Replace(
            link,
            @"\[\[(.*)\|(.*)\]\]",
            "[[$2|$1]]");
    }

    /// <summary>
    /// Prepares the individual disambiguation items for all matched links.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="matches">
    /// The matched wikilinks requiring disambiguation.
    /// </param>
    /// <param name="contextChars">
    /// The approximate number of context characters to include on each side
    /// of each matched link.
    /// </param>
    /// <returns>
    /// The prepared disambiguation items.
    /// </returns>
    public static List<DisambiguationItemPreparation> PrepareItems(
        string articleText,
        IEnumerable<Match> matches,
        int contextChars)
    {
        return matches
            .Select(
                match => PrepareItem(
                    articleText,
                    match,
                    contextChars))
            .ToList();
    }

    /// <summary>
    /// Applies the configured disambiguation operation to the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="session">
    /// The active wiki session.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when disambiguation is aborted or the article
    /// is skipped.
    /// </returns>
    public static bool ApplyDisambiguation(
        Article article,
        MainProcessOptions options,
        Session session)
    {
        if (options.PreParseMode ||
            !options.DisambiguationEnabled ||
            options.DisambiguationLink.Length == 0 ||
            options.DisambiguationVariants.Length == 0)
        {
            return true;
        }

        bool noChange;

        DabForm dialog = new(session);

        string articleText =
            dialog.Disambiguate(
                article.ArticleText,
                article.Name,
                options.DisambiguationLink,
                options.DisambiguationVariants,
                options.DisambiguationContextCharacters,
                options.BotMode,
                out noChange);

        if (dialog.Abort)
            return false;

        article.ApplyDisambiguationResult(
            options.DisambiguationLink,
            articleText,
            noChange,
            options.SkipIfNoDisambiguation);

        return !article.SkipArticle;
    }

    /// <summary>
    /// Applies the configured disambiguation operation using an
    /// asynchronous application-owned dialog callback.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="callbacks">
    /// The application-owned processing callbacks.
    /// </param>
    /// <returns>
    /// The outcome of the disambiguation processing stage.
    /// </returns>
    public static async Task<DisambiguationProcessingOutcome>
        ApplyDisambiguationAsync(
            Article article,
            MainProcessOptions options,
            MainProcessCallbacks callbacks)
    {
        if (options.PreParseMode ||
            !options.DisambiguationEnabled ||
            options.DisambiguationLink.Length == 0 ||
            options.DisambiguationVariants.Length == 0)
        {
            return DisambiguationProcessingOutcome.Continue;
        }

        string originalText = article.ArticleText;

        DisambiguationPreparation preparation =
            Prepare(
                originalText,
                options.DisambiguationLink,
                options.DisambiguationVariants,
                options.DisambiguationContextCharacters);

        if (preparation.Variants.Count == 0 ||
            preparation.Items.Count == 0)
        {
            article.ApplyDisambiguationResult(
                options.DisambiguationLink,
                originalText,
                true,
                options.SkipIfNoDisambiguation);

            return article.SkipArticle
                ? DisambiguationProcessingOutcome.Skipped
                : DisambiguationProcessingOutcome.Continue;
        }

        var requestDisambiguation =
            callbacks.RequestDisambiguationAsync
            ?? throw new InvalidOperationException(
                "The disambiguation dialog callback has not been configured.");

        var (abort, processingResult) =
            await requestDisambiguation(article, preparation);

        if (abort)
        {
            return DisambiguationProcessingOutcome.Aborted;
        }

        string articleText = processingResult.ArticleText;

        bool noChange =
            string.Equals(
                originalText,
                articleText,
                StringComparison.Ordinal);

        article.ApplyDisambiguationResult(
            options.DisambiguationLink,
            articleText,
            noChange,
            options.SkipIfNoDisambiguation,
            processingResult.Corrections);

        return article.SkipArticle
            ? DisambiguationProcessingOutcome.Skipped
            : DisambiguationProcessingOutcome.Continue;
    }
}