namespace Twain.Core.Alerts;

/// <summary>
/// Identifies the article alerts supported by the article alert system.
/// </summary>
/// <remarks>
/// Numeric values are explicitly assigned to preserve compatibility with
/// existing alert preference values.
/// </remarks>
public enum ArticleAlertId
{
    AmbiguousCitationDates = 1,
    SicTag = 2,
    DisambiguationPageWithReferences = 3,
    DeadLinks = 4,
    DuplicateBannerShellParameters = 5,
    ReferenceAfterReferences = 6,
    FootnotesTemplateWithManyReferences = 7,
    WikilinkedHeaders = 8,
    InvalidCitationParameters = 9,
    DoublePipeLinks = 10,
    TargetlessLinks = 11,
    LongArticleWithStubTag = 12,
    MultipleDefaultSort = 13,
    NoCategory = 14,
    SeeAlsoOutOfPlace = 15,
    StartsWithHeading = 16,
    UnbalancedBrackets = 17,
    UnclosedTags = 18,
    UnformattedReferences = 19,
    UnknownMultipleIssuesParameters = 20,
    UnknownWikiProjectBannerShellParameters = 21,
    UserSignatureOrUserSpaceLink = 22
}

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
        ArticleAlertId alertId)
    {
        return allAlertsEnabled ||
            enabledAlertIds.Contains((int)alertId);
    }

    /// <summary>
    /// Gets the article-alert identifiers and their user-facing descriptions.
    /// </summary>
    /// <remarks>
    /// The numeric identifiers must remain synchronized with the alert values
    /// used by the article-checking and preferences logic.
    ///
    /// TODO: Move user-facing alert descriptions to application resources if
    /// alert text is localized in the future.
    /// </remarks>
    public static IReadOnlyDictionary<ArticleAlertId, string> AlertDescriptions { get; } =
        new Dictionary<ArticleAlertId, string>
        {
        { ArticleAlertId.AmbiguousCitationDates, "Ambiguous citation dates" },
        { ArticleAlertId.SicTag, "Contains 'sic' tag" },
        { ArticleAlertId.DisambiguationPageWithReferences, "DAB page with <ref>s" },
        { ArticleAlertId.DeadLinks, "Dead links" },
        { ArticleAlertId.DuplicateBannerShellParameters, "Duplicate parameters in WPBannerShell" },
        { ArticleAlertId.ReferenceAfterReferences, "Has <ref> after </references>" },
        { ArticleAlertId.FootnotesTemplateWithManyReferences, "Has 'No/More footnotes' template yet many references" },
        { ArticleAlertId.WikilinkedHeaders, "Headers with wikilinks" },
        { ArticleAlertId.InvalidCitationParameters, "Invalid citation parameters" },
        { ArticleAlertId.DoublePipeLinks, "Links with double pipes" },
        { ArticleAlertId.TargetlessLinks, "Links with no target" },
        { ArticleAlertId.LongArticleWithStubTag, "Long article with stub tag" },
        { ArticleAlertId.MultipleDefaultSort, "Multiple DEFAULTSORT" },
        { ArticleAlertId.NoCategory, "No category (may be one in a template)" },
        { ArticleAlertId.SeeAlsoOutOfPlace, "See also section out of place" },
        { ArticleAlertId.StartsWithHeading, "Starts with heading" },
        { ArticleAlertId.UnbalancedBrackets, "Unbalanced brackets" },
        { ArticleAlertId.UnclosedTags, "Unclosed tags" },
        { ArticleAlertId.UnformattedReferences, "Unformatted references" },
        { ArticleAlertId.UnknownMultipleIssuesParameters, "Unknown parameters in multiple issues" },
        { ArticleAlertId.UnknownWikiProjectBannerShellParameters, "Unknown parameters in WikiProject banner shell" },
        { ArticleAlertId.UserSignatureOrUserSpaceLink, "Editor's signature or link to user space" }
        };

    /// <summary>
    /// Resolves the enabled alert identifiers from the available alerts and
    /// the currently selected identifiers.
    /// </summary>
    /// <remarks>
    /// An empty selection represents the legacy default in which all available
    /// alerts are enabled.
    /// </remarks>
    /// <param name="availableAlertIds">
    /// The identifiers of all available alerts.
    /// </param>
    /// <param name="selectedAlertIds">
    /// The identifiers explicitly selected by the user.
    /// </param>
    /// <returns>
    /// The identifiers that should be treated as enabled.
    /// </returns>
    public static List<int> ResolveEnabledAlertIds(
        IEnumerable<int> availableAlertIds,
        IEnumerable<int> selectedAlertIds)
    {
        ArgumentNullException.ThrowIfNull(availableAlertIds);
        ArgumentNullException.ThrowIfNull(selectedAlertIds);

        List<int> selected =
            selectedAlertIds.ToList();

        return selected.Count == 0
            ? availableAlertIds.ToList()
            : selected;
    }

    /// <summary>
    /// Determines whether an alert should be selected from the stored alert
    /// preference identifiers.
    /// </summary>
    /// <remarks>
    /// An empty stored selection represents the legacy default in which every
    /// available alert is enabled.
    /// </remarks>
    public static bool IsAlertEnabled(
        IReadOnlyCollection<int> enabledAlertIds,
        int alertId)
    {
        ArgumentNullException.ThrowIfNull(enabledAlertIds);

        return enabledAlertIds.Count == 0 ||
            enabledAlertIds.Contains(alertId);
    }
}