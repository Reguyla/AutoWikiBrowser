namespace Twain.Core.Alerts;

/// <summary>
/// Evaluates article-structure alert conditions that do not require
/// user-interface interaction.
/// </summary>
public static class ArticleStructureAlertEvaluator
{
    /// <summary>
    /// Evaluates article-structure alerts for the supplied article.
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
    /// <param name="allAlertsEnabled">
    /// <see langword="true"/> when all alerts are enabled.
    /// </param>
    /// <param name="enabledAlertIds">
    /// The individually enabled alert identifiers.
    /// </param>
    /// <returns>
    /// The generated structure-alert messages and associated error positions.
    /// </returns>
    public static ArticleStructureAlertResult Evaluate(
        Article article,
        string articleText,
        string templates,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds)
    {
        List<string> alerts = new();

        Dictionary<int, int> unbalancedBrackets =
            new();

        Dictionary<int, int> targetlessLinks =
            new();

        Dictionary<int, int> doublePipeLinks =
            new();

        Dictionary<int, int> otherErrors =
            new();

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                16) &&
            article.NameSpaceKey == Namespace.Article &&
            articleText.StartsWith("=="))
        {
            alerts.Add(
                "Starts with heading");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                17))
        {
            unbalancedBrackets =
                article.UnbalancedBrackets();

            if (unbalancedBrackets.Count > 0)
            {
                alerts.Add(
                    $"Unbalanced brackets ({unbalancedBrackets.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                11))
        {
            targetlessLinks =
                article.TargetlessLinks();

            if (targetlessLinks.Count > 0)
            {
                alerts.Add(
                    $"Links with no target ({targetlessLinks.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                10))
        {
            doublePipeLinks =
                article.DoublepipeLinks();

            if (doublePipeLinks.Count > 0)
            {
                alerts.Add(
                    $"Links with double pipes ({doublePipeLinks.Count})");
            }

            // Preserve legacy behavior: these checks remain nested under alert 10.
            if (ArticleAlertHelper.IsAlertEnabled(
                    allAlertsEnabled,
                    enabledAlertIds,
                    13) &&
                WikiRegexes.Defaultsort.Matches(templates).Count > 1)
            {
                alerts.Add(
                    "Multiple DEFAULTSORTs");
            }

            if (ArticleAlertHelper.IsAlertEnabled(
                    allAlertsEnabled,
                    enabledAlertIds,
                    15) &&
                article.HasSeeAlsoAfterNotesReferencesOrExternalLinks)
            {
                alerts.Add(
                    "See also section out of place");

                AddSeeAlsoHeadingError(
                    articleText,
                    otherErrors);
            }
        }

        return new ArticleStructureAlertResult
        {
            Alerts = alerts,
            UnbalancedBrackets = unbalancedBrackets,
            TargetlessLinks = targetlessLinks,
            DoublePipeLinks = doublePipeLinks,
            OtherErrors = otherErrors
        };
    }

    /// <summary>
    /// Locates the See also heading in the supplied article text and records its
    /// position for editor highlighting.
    /// </summary>
    /// <param name="articleText">
    /// The article text to search.
    /// </param>
    /// <param name="otherErrors">
    /// The collection that receives the detected heading position.
    /// </param>
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