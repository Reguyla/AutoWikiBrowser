using Twain.Core.Parse;

namespace Twain.Core.Alerts;

/// <summary>
/// Evaluates basic article alert conditions that do not require user-interface
/// interaction.
/// </summary>
public static class BasicArticleAlertEvaluator
{
    /// <summary>
    /// Evaluates basic article alerts for the supplied article state.
    /// </summary>
    /// <param name="article">
    /// The article being analyzed.
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
    /// <returns>
    /// The alert messages generated for the supplied article.
    /// </returns>
    public static IReadOnlyList<string> Evaluate(
        Article article,
        string templates,
        int wordCount,
        int categoryCount,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds)
    {
        List<string> alerts = new();

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                12) &&
            article.NameSpaceKey == Namespace.Article &&
            wordCount > Parsers.StubMaxWordCount &&
            WikiRegexes.Stub.IsMatch(templates))
        {
            alerts.Add(
                "Long article with a stub tag.");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                14) &&
            categoryCount == 0 &&
            !Namespace.IsTalk(article.Name))
        {
            alerts.Add(
                "No category (may be one in a template)");
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                7) &&
            article.NameSpaceKey == Namespace.Article &&
            article.HasMorefootnotesAndManyReferences)
        {
            alerts.Add(
                "Has 'No/More footnotes' template yet many references");
        }

        return alerts;
    }
}