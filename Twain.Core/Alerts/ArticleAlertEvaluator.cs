using Twain.Core.Parse;

namespace Twain.Core.Alerts;

/// <summary>
/// Evaluates configured alerts for an article.
/// </summary>
public static class ArticleAlertEvaluator
{
    /// <summary>
    /// Evaluates the configured alert conditions for the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article being analyzed.
    /// </param>
    /// <param name="articleText">
    /// The current article text.
    /// </param>
    /// <param name="templates">
    /// The template markup extracted from the article.
    /// </param>
    /// <param name="wordCount">
    /// The number of words in the article.
    /// </param>
    /// <param name="categoryCount">
    /// The number of categories found in the article.
    /// </param>
    /// <param name="allAlertsEnabled">
    /// <see langword="true"/> when all alerts are enabled.
    /// </param>
    /// <param name="enabledAlertIds">
    /// The individually enabled alert identifiers.
    /// </param>
    /// <param name="regexTypoFixEnabled">
    /// <see langword="true"/> when regular-expression typo fixing is enabled.
    /// </param>
    /// <returns>
    /// The generated alert messages and associated article-analysis results.
    /// </returns>
    public static ArticleAlertResult Evaluate(
        Article article,
        string articleText,
        string templates,
        int wordCount,
        int categoryCount,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds,
        bool regexTypoFixEnabled)
    {
        ArticleAlertResult result = new();

        EvaluateBasicAlerts(
            article,
            templates,
            wordCount,
            categoryCount,
            allAlertsEnabled,
            enabledAlertIds,
            result);

        EvaluateStructureAlerts(
            article,
            articleText,
            templates,
            allAlertsEnabled,
            enabledAlertIds,
            result);

        EvaluateCitationAndUrlAlerts(
            article,
            allAlertsEnabled,
            enabledAlertIds,
            result);

        EvaluateTalkAndUserAlerts(
            article,
            allAlertsEnabled,
            enabledAlertIds,
            result);

        EvaluateSicTagAlert(
            article,
            allAlertsEnabled,
            enabledAlertIds,
            regexTypoFixEnabled,
            result);

        return result;
    }

    /// <summary>
    /// Evaluates high-level article condition alerts.
    /// </summary>
    private static void EvaluateBasicAlerts(
        Article article,
        string templates,
        int wordCount,
        int categoryCount,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds,
        ArticleAlertResult result)
    {
        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                12) &&
            article.NameSpaceKey == Namespace.Article &&
            wordCount > Parsers.StubMaxWordCount &&
            WikiRegexes.Stub.IsMatch(templates))
        {
            result.Alerts.Add(
                "Long article with a stub tag.");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                14) &&
            categoryCount == 0 &&
            !Namespace.IsTalk(article.Name))
        {
            result.Alerts.Add(
                "No category (may be one in a template)");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                7) &&
            article.NameSpaceKey == Namespace.Article &&
            article.HasMorefootnotesAndManyReferences)
        {
            result.Alerts.Add(
                "Has 'No/More footnotes' template yet many references");
        }
    }

    /// <summary>
    /// Evaluates article-structure and reference-placement alerts.
    /// </summary>
    private static void EvaluateStructureAlerts(
        Article article,
        string articleText,
        string templates,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds,
        ArticleAlertResult result)
    {
        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                16) &&
            article.NameSpaceKey == Namespace.Article &&
            articleText.StartsWith("=="))
        {
            result.Alerts.Add(
                "Starts with heading");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                17))
        {
            result.UnbalancedBrackets =
                article.UnbalancedBrackets();

            if (result.UnbalancedBrackets.Count > 0)
            {
                result.Alerts.Add(
                    $"Unbalanced brackets ({result.UnbalancedBrackets.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                11))
        {
            result.TargetlessLinks =
                article.TargetlessLinks();

            if (result.TargetlessLinks.Count > 0)
            {
                result.Alerts.Add(
                    $"Links with no target ({result.TargetlessLinks.Count})");
            }
        }

        if (!ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                10))
        {
            return;
        }

        result.DoublePipeLinks =
            article.DoublepipeLinks();

        if (result.DoublePipeLinks.Count > 0)
        {
            result.Alerts.Add(
                $"Links with double pipes ({result.DoublePipeLinks.Count})");
        }

        // Preserve legacy behavior: DEFAULTSORT and See also checks remain
        // dependent on alert 10 being enabled.
        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                13) &&
            WikiRegexes.Defaultsort.Matches(templates).Count > 1)
        {
            result.Alerts.Add(
                "Multiple DEFAULTSORTs");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                15) &&
            article.HasSeeAlsoAfterNotesReferencesOrExternalLinks)
        {
            result.Alerts.Add(
                "See also section out of place");

            AddSeeAlsoHeadingError(
                articleText,
                result.OtherErrors);
        }
    }

    /// <summary>
    /// Evaluates citation and URL-related alerts.
    /// </summary>
    private static void EvaluateCitationAndUrlAlerts(
        Article article,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds,
        ArticleAlertResult result)
    {
        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                4))
        {
            result.DeadLinks =
                article.DeadLinks();

            if (result.DeadLinks.Any())
            {
                result.Alerts.Add(
                    $"Dead links ({result.DeadLinks.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                6) &&
            article.HasRefAfterReflist)
        {
            result.Alerts.Add(
                @"Has a <ref> after <references/>");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                3) &&
            article.IsDisambiguationPageWithRefs)
        {
            result.Alerts.Add(
                @"DAB page with <ref>s");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                19) &&
            article.HasBareReferences)
        {
            result.Alerts.Add(
                "Unformatted references");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                1))
        {
            result.AmbiguousCiteDates =
                article.AmbiguousCiteTemplateDates();

            if (result.AmbiguousCiteDates.Count > 0)
            {
                result.Alerts.Add(
                    $"Ambiguous citation dates ({result.AmbiguousCiteDates.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                20))
        {
            result.UnknownMultipleIssuesParameters =
                article.UnknownMultipleIssuesParameters();

            if (result.UnknownMultipleIssuesParameters.Count > 0)
            {
                result.Alerts.Add(
                    $"Unknown parameters in Multiple issues ({result.UnknownMultipleIssuesParameters.Count}): " +
                    string.Join(
                        ", ",
                        result.UnknownMultipleIssuesParameters));
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                8))
        {
            result.WikilinkedHeaders =
                article.WikiLinkedHeaders();

            if (result.WikilinkedHeaders.Count > 0)
            {
                result.Alerts.Add(
                    $"Header(s) with wikilinks ({result.WikilinkedHeaders.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                18))
        {
            result.UnclosedTags =
                article.UnclosedTags();

            if (result.UnclosedTags.Count > 0)
            {
                result.Alerts.Add(
                    $"Unclosed tag(s) ({result.UnclosedTags.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                9))
        {
            result.BadCiteParameters =
                article.BadCiteParameters();

            if (result.BadCiteParameters.Count > 0)
            {
                result.Alerts.Add(
                    $"Invalid citation parameter(s) ({result.BadCiteParameters.Count})");
            }
        }
    }

    /// <summary>
    /// Evaluates WikiProject banner-shell and user-namespace alerts.
    /// </summary>
    private static void EvaluateTalkAndUserAlerts(
        Article article,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds,
        ArticleAlertResult result)
    {
        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                5))
        {
            result.DuplicateBannerShellParameters =
                article.DuplicateWikiProjectBannerShellParameters();

            if (result.DuplicateBannerShellParameters.Count > 0)
            {
                result.Alerts.Add(
                    $"Duplicate parameter(s) in WPBannerShell ({result.DuplicateBannerShellParameters.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                21))
        {
            result.UnknownWikiProjectBannerShellParameters =
                article.UnknownWikiProjectBannerShellParameters();

            if (result.UnknownWikiProjectBannerShellParameters.Count > 0)
            {
                result.Alerts.Add(
                    $"Unknown parameters in WikiProject banner shell ({result.UnknownWikiProjectBannerShellParameters.Count}): " +
                    string.Join(
                        ", ",
                        result.UnknownWikiProjectBannerShellParameters));
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                22) &&
            article.NameSpaceKey == Namespace.Article)
        {
            result.UserSignatures =
                article.UserSignature();

            if (result.UserSignatures.Count > 0)
            {
                result.Alerts.Add(
                    $"Editor's signature or link to user space ({result.UserSignatures.Count})");
            }
        }
    }

    /// <summary>
    /// Evaluates the sic-tag alert.
    /// </summary>
    private static void EvaluateSicTagAlert(
        Article article,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds,
        bool regexTypoFixEnabled,
        ArticleAlertResult result)
    {
        bool shouldEvaluate =
            allAlertsEnabled ||
            enabledAlertIds.Contains(2) ||
            regexTypoFixEnabled;

        if (shouldEvaluate &&
            article.HasSicTag)
        {
            result.Alerts.Add(
                "Contains 'sic' tag");
        }
    }

    /// <summary>
    /// Locates the See also heading in the supplied article text and records its
    /// position for editor highlighting.
    /// </summary>
    private static void AddSeeAlsoHeadingError(
        string articleText,
        IDictionary<int, int> otherErrors)
    {
        Match seeAlsoHeading =
            WikiRegexes.Headings
                .Matches(articleText)
                .OfType<Match>()
                .FirstOrDefault(
                    heading =>
                        WikiRegexes.SeeAlso.IsMatch(
                            heading.Value));

        if (seeAlsoHeading != null &&
            !otherErrors.ContainsKey(
                seeAlsoHeading.Index))
        {
            otherErrors.Add(
                seeAlsoHeading.Index,
                seeAlsoHeading.Length);
        }
    }
}