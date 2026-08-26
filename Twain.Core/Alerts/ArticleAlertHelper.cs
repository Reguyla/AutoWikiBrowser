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
}