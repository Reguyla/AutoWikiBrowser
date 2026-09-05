namespace Twain.Core.Settings;

/// <summary>
/// Defines the skip conditions available during automatic article processing.
/// </summary>
public static class SkipOptionsHelper
{
    public const int BoldTitleOptionId = 1;
    public const int BulletedExternalLinkOptionId = 2;
    public const int BadLinksOptionId = 3;
    public const int UnicodeOptionId = 4;
    public const int AutoTagOptionId = 5;
    public const int HeaderErrorOptionId = 6;
    public const int DefaultSortOptionId = 7;
    public const int UserTalkTemplatesOptionId = 8;
    public const int CitationTemplateDatesOptionId = 9;
    public const int HumanCategoriesOptionId = 10;

    /// <summary>
    /// Gets the available skip options in their intended display order.
    /// </summary>
    public static IReadOnlyList<(int Id, string Description)> AvailableOptions { get; } =
    [
        (BoldTitleOptionId, "Title boldened"),
        (BulletedExternalLinkOptionId, "External link bulleted"),
        (BadLinksOptionId, "Bad links fixed"),
        (UnicodeOptionId, "Unicodification"),
        (AutoTagOptionId, "Auto tag changes"),
        (HeaderErrorOptionId, "Header error fixed"),
        (DefaultSortOptionId, "{{defaultsort}} added"),
        (UserTalkTemplatesOptionId, "User talk templates subst'd"),
        (CitationTemplateDatesOptionId, "Citation templates dates fixed"),
        (HumanCategoriesOptionId, "Human category changes")
    ];

    /// <summary>
    /// Determines whether the specified skip option is selected.
    /// </summary>
    public static bool IsSelected(
        IEnumerable<int> selectedItems,
        int optionId)
    {
        ArgumentNullException.ThrowIfNull(selectedItems);

        return selectedItems.Contains(optionId);
    }
}