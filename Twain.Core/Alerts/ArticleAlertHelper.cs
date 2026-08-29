namespace Twain.Core.Alerts;

/// <summary>
/// Provides shared helpers for evaluating article alert configuration.
/// </summary>
public static class ArticleAlertHelper
{
    /// <summary>
    /// Determines whether the specified article alert is enabled.
    /// </summary>
    /// <param name="allAlertsEnabled">
    /// <see langword="true"/> when all article alerts are enabled.
    /// </param>
    /// <param name="enabledAlertIds">
    /// The individually enabled article alert identifiers.
    /// </param>
    /// <param name="alertId">
    /// The alert identifier to evaluate.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the alert should be evaluated; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool IsAlertEnabled(
        bool allAlertsEnabled,
        ICollection<int> enabledAlertIds,
        int alertId)
    {
        return allAlertsEnabled ||
            enabledAlertIds.Contains(alertId);
    }

    /// <summary>
    /// Gets the article-alert identifiers and their user-facing descriptions.
    /// </summary>
    /// <remarks>
    /// The numeric identifiers must remain synchronized with the alert values
    /// used by the article-checking and preferences logic.
    ///
    /// TODO: Replace the numeric alert identifiers with a named enum after
    /// confirming whether these values are persisted or used externally.
    ///
    /// TODO: Move user-facing alert descriptions to application resources if
    /// alert text is localized in the future.
    /// </remarks>
    public static IReadOnlyDictionary<int, string> AlertDescriptions { get; } =
        new Dictionary<int, string>
        {
            { 1, "Ambiguous citation dates" },
            { 2, "Contains 'sic' tag" },
            { 3, "DAB page with <ref>s" },
            { 4, "Dead links" },
            { 5, "Duplicate parameters in WPBannerShell" },
            { 6, "Has <ref> after </references>" },
            { 7, "Has 'No/More footnotes' template yet many references" },
            { 8, "Headers with wikilinks" },
            { 9, "Invalid citation parameters" },
            { 10, "Links with double pipes" },
            { 11, "Links with no target" },
            { 12, "Long article with stub tag" },
            { 13, "Multiple DEFAULTSORT" },
            { 14, "No category (may be one in a template)" },
            { 15, "See also section out of place" },
            { 16, "Starts with heading" },
            { 17, "Unbalanced brackets" },
            { 18, "Unclosed tags" },
            { 19, "Unformatted references" },
            { 20, "Unknown parameters in multiple issues" },
            { 21, "Unknown parameters in WikiProject banner shell" },
            { 22, "Editor's signature or link to user space" }
        };
}