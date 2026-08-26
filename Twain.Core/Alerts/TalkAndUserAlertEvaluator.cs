namespace Twain.Core.Alerts;

/// <summary>
/// Evaluates talk-page and user-namespace article alerts that do not require
/// user-interface interaction.
/// </summary>
public static class TalkAndUserAlertEvaluator
{
    /// <summary>
    /// Evaluates talk-page and user-namespace alerts for the supplied article.
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
    /// The generated alert messages and associated analysis results.
    /// </returns>
    public static TalkAndUserAlertResult Evaluate(
        Article article,
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds)
    {
        List<string> alerts = new();

        Dictionary<int, int> duplicateBannerShellParameters =
            new();

        List<string> unknownWikiProjectBannerShellParameters =
            new();

        Dictionary<int, int> userSignatures =
            new();

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                5))
        {
            duplicateBannerShellParameters =
                article.DuplicateWikiProjectBannerShellParameters();

            if (duplicateBannerShellParameters.Count > 0)
            {
                alerts.Add(
                    $"Duplicate parameter(s) in WPBannerShell ({duplicateBannerShellParameters.Count})");
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                21))
        {
            unknownWikiProjectBannerShellParameters =
                article.UnknownWikiProjectBannerShellParameters();

            if (unknownWikiProjectBannerShellParameters.Count > 0)
            {
                string warning =
                    $"Unknown parameters in WikiProject banner shell ({unknownWikiProjectBannerShellParameters.Count}): " +
                    string.Join(
                        ", ",
                        unknownWikiProjectBannerShellParameters);

                alerts.Add(warning);
            }
        }

        if (ArticleAlertHelper.IsAlertEnabled(
                allAlertsEnabled,
                enabledAlertIds,
                22) &&
            article.NameSpaceKey == Namespace.Article)
        {
            userSignatures =
                article.UserSignature();

            if (userSignatures.Count > 0)
            {
                alerts.Add(
                    $"Editor's signature or link to user space ({userSignatures.Count})");
            }
        }

        return new TalkAndUserAlertResult
        {
            Alerts = alerts,
            DuplicateBannerShellParameters =
                duplicateBannerShellParameters,
            UnknownWikiProjectBannerShellParameters =
                unknownWikiProjectBannerShellParameters,
            UserSignatures =
                userSignatures
        };
    }
}