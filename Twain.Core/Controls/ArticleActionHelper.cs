namespace Twain.Core.Controls;

/// <summary>
/// Provides shared behavior for article move, delete, and protection actions.
/// </summary>
public static class ArticleActionHelper
{
    /// <summary>
    /// Gets the default edit-summary choices for an article action.
    /// </summary>
    public static IReadOnlyList<string> GetDefaultSummaries(
        ArticleAction action)
    {
        return action switch
        {
            ArticleAction.Move =>
            [
                "Typo in page title",
                "Wikipedia naming convention",
                "Reverting vandalism page move",
                "Facilitating concordance with a category's name"
            ],

            ArticleAction.Delete =>
            [
                "tagged for [[WP:PROD|proposed deletion]] for 7 days",
                "[[WP:CSD#G1|G1]]: [[WP:PN|Patent nonsense]], meaningless, or incomprehensible",
                "[[WP:CSD#G2|G2]]: Test page",
                "[[WP:CSD#G3|G3]]: [[WP:Vandalism|Vandalism]]",
                "[[WP:CSD#G3|G3]]: Blatant [[WP:Do not create hoaxes|hoax]]",
                "[[WP:CSD#G4|G4]]: Recreation of a page that was [[WP:DEL|deleted]] per a [[WP:XFD|deletion discussion]]",
                "[[WP:CSD#G5|G5]]: Creation by a [[WP:BLOCK|blocked]] or [[WP:BAN|banned]] user in violation of block or ban",
                "[[WP:CSD#G6|G6]]: Housekeeping and routine (non-controversial) cleanup",
                "[[WP:CSD#G7|G7]]: One author who has requested deletion or blanked the page",
                "[[WP:CSD#G8|G8]]: Page dependent on a deleted or nonexistent page",
                "[[WP:CSD#G10|G10]]: [[WP:ATP|Attack page]] or negative unsourced [[WP:BLP|BLP]]",
                "[[WP:CSD#G11|G11]]: Unambiguous [[WP:NOTADVERTISING|advertising]] or promotion",
                "[[WP:CSD#G12|G12]]: Unambiguous [[WP:CV|copyright infringement]]",
                "[[WP:CSD#G13|G13]]: Abandoned [[WP:AFC|Article for creation]] – to retrieve it, see [[WP:REFUND/G13]]",
                "[[WP:CSD#A1|A1]]: Short article without enough context to identify the subject",
                "[[WP:CSD#A2|A2]]: Article in a foreign language that exists on another project",
                "[[WP:CSD#A3|A3]]: Article that has no meaningful, substantive content",
                "[[WP:CSD#A5|A5]]: Article that has been transwikied to another project",
                "[[WP:CSD#A7|A7]]: No credible indication of importance (individuals, animals, organizations, web content, events)",
                "[[WP:CSD#A9|A9]]: Music recording by redlinked artist and no indication of importance or significance",
                "[[WP:CSD#A10|A10]]: Recently created article that duplicates an existing topic",
                "[[WP:CSD#A11|A11]]: Made up by article creator or an associate, and no indication of importance/significance",
                "[[WP:CSD#R1|Redirect to non-existent page]]",
                "[[WP:CSD#R2|Cross namespace redirect from mainspace]]",
                "[[WP:CSD#R3|Recently created, implausible redirect]]",
                "[[WP:CSD#C1|C1]]: Empty category",
                "[[WP:CSD#C2|C2]]: Speedy renaming",
                "[[WP:CSD#U1|U1]]: User request to delete page in own userspace",
                "[[WP:CSD#U2|U2]]: Userpage or subpage of a nonexistent user",
                "[[WP:CSD#U3|U3]]: [[WP:NFC|Non-free]] [[Help:Gallery|gallery]]",
                "[[WP:CSD#U5|U5]]: [[WP:NOTWEBHOST|Misuse of Wikipedia as a web host]]",
                "[[WP:CSD#T2|T2]]: Template that unambiguously misrepresents established policy",
                "[[WP:CSD#T3|T3]]: Unused, redundant template",
                "[[WP:CSD#G8|G8]]: Component or documentation of a deleted template"
            ],

            ArticleAction.Protect =>
            [
                "Excessive vandalism",
                "High traffic page",
                "Excessive spamming",
                "Edit warring"
            ],

            _ => []
        };
    }

    /// <summary>
    /// Validates the values supplied for an article action.
    /// </summary>
    /// <returns>
    /// The validation error text, or an empty string when the values are valid.
    /// </returns>
    public static string Validate(
        ArticleAction action,
        string summary,
        string newTitle,
        string expiry,
        string editProtectionLevel,
        string moveProtectionLevel)
    {
        StringBuilder errorMessage =
            new();

        switch (action)
        {
            case ArticleAction.Move:
                if (string.IsNullOrEmpty(summary))
                    errorMessage.AppendLine(
                        "Please enter/select a move reason.");

                if (string.IsNullOrEmpty(newTitle))
                    errorMessage.AppendLine(
                        "Please enter a new/target title.");

                break;

            case ArticleAction.Protect:
                if (!string.IsNullOrEmpty(expiry) &&
                    Tools.DateBeforeToday(expiry))
                {
                    errorMessage.AppendLine(
                        "Please enter an expiry date in the future");
                }

                if (string.IsNullOrEmpty(summary))
                {
                    errorMessage.AppendLine(
                        "Please enter/select a protection reason.");
                }

                if ((!string.IsNullOrEmpty(moveProtectionLevel) ||
                     !string.IsNullOrEmpty(editProtectionLevel)) &&
                    string.IsNullOrEmpty(expiry))
                {
                    errorMessage.AppendLine(
                        "Please enter an expiry time.");
                }

                break;

            case ArticleAction.Delete:
                if (string.IsNullOrEmpty(summary))
                {
                    errorMessage.AppendLine(
                        "Please enter/select a deletion reason");
                }

                break;
        }

        return errorMessage.ToString();
    }

    /// <summary>
    /// Gets the title used when displaying validation errors.
    /// </summary>
    public static string GetValidationErrorTitle(
        ArticleAction action)
    {
        return action switch
        {
            ArticleAction.Move => "Move error",
            ArticleAction.Protect => "Protection error",
            ArticleAction.Delete => "Deletion reason required",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets the protection levels available for the current wiki.
    /// </summary>
    public static IReadOnlyList<ProtectionLevel> GetProtectionLevels()
    {
        return ProtectionLevelHelper.GetLevels(
            Variables.LangCode);
    }

    /// <summary>
    /// Determines whether cascading protection is available for the
    /// selected edit and move protection levels.
    /// </summary>
    public static bool IsCascadingProtectionAvailable(
        int editSelectedIndex,
        int moveSelectedIndex)
    {
        return ProtectionLevelHelper.IsCascadingEnabled(
            editSelectedIndex,
            moveSelectedIndex);
    }
}