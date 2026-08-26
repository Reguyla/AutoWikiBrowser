namespace Twain.Core.Alerts;

/// <summary>
/// Evaluates citation and URL-related article alerts that do not require
/// user-interface interaction.
/// </summary>
public static class CitationAndUrlAlertEvaluator
{
    /// <summary>
    /// Evaluates citation and URL-related alerts for the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article being analyzed.
    /// </param>
    /// <param name="allAlertsEnabled">
    /// <see langword="true"/> when all alerts are enabled.
    /// </param>
    /// <param name="enabledAlertIds">
    /// The individually enabled alert identifiers.
    /// </param>
    /// <returns>
    /// The generated citation and URL alert messages and associated analysis
    /// results.
    /// </returns>
    public static CitationAndUrlAlertResult Evaluate(
        Article article,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds)
    {
        List<string> alerts = new();

        Dictionary<int, int> deadLinks =
            new();

        Dictionary<int, int> ambiguousCiteDates =
            new();

        Dictionary<int, int> wikilinkedHeaders =
            new();

        Dictionary<int, int> unclosedTags =
            new();

        Dictionary<int, int> badCiteParameters =
            new();

        List<string> unknownMultipleIssuesParameters =
            new();

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                4))
        {
            deadLinks =
                article.DeadLinks();

            if (deadLinks.Any())
            {
                alerts.Add(
                    $"Dead links ({deadLinks.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                6) &&
            article.HasRefAfterReflist)
        {
            alerts.Add(
                @"Has a <ref> after <references/>");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                3) &&
            article.IsDisambiguationPageWithRefs)
        {
            alerts.Add(
                @"DAB page with <ref>s");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                19) &&
            article.HasBareReferences)
        {
            alerts.Add(
                "Unformatted references");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                1))
        {
            ambiguousCiteDates =
                article.AmbiguousCiteTemplateDates();

            if (ambiguousCiteDates.Count > 0)
            {
                alerts.Add(
                    $"Ambiguous citation dates ({ambiguousCiteDates.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                20))
        {
            unknownMultipleIssuesParameters =
                article.UnknownMultipleIssuesParameters();

            if (unknownMultipleIssuesParameters.Count > 0)
            {
                string warning =
                    $"Unknown parameters in Multiple issues ({unknownMultipleIssuesParameters.Count}): " +
                    string.Join(
                        ", ",
                        unknownMultipleIssuesParameters);

                alerts.Add(warning);
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                8))
        {
            wikilinkedHeaders =
                article.WikiLinkedHeaders();

            if (wikilinkedHeaders.Count > 0)
            {
                alerts.Add(
                    $"Header(s) with wikilinks ({wikilinkedHeaders.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                18))
        {
            unclosedTags =
                article.UnclosedTags();

            if (unclosedTags.Count > 0)
            {
                alerts.Add(
                    $"Unclosed tag(s) ({unclosedTags.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                9))
        {
            badCiteParameters =
                article.BadCiteParameters();

            if (badCiteParameters.Count > 0)
            {
                alerts.Add(
                    $"Invalid citation parameter(s) ({badCiteParameters.Count})");
            }
        }

        return new CitationAndUrlAlertResult
        {
            Alerts = alerts,
            DeadLinks = deadLinks,
            AmbiguousCiteDates = ambiguousCiteDates,
            WikilinkedHeaders = wikilinkedHeaders,
            UnclosedTags = unclosedTags,
            BadCiteParameters = badCiteParameters,
            UnknownMultipleIssuesParameters =
                unknownMultipleIssuesParameters
        };
    }
}