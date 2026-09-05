using Twain.Core.Parse;

namespace Twain.Core.Settings;

/// <summary>
/// Provides UI-independent behavior used by the user-settings interface.
/// </summary>
public static class UserSettingsHelper
{
    private static readonly Regex CustomProjectRegex = new(
        @"^.*?://(?:([\w/\.-]+?)/(?:index|api)\.php|([\w/\.-]+)).*$",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Normalizes a wiki language code for selection.
    /// </summary>
    public static string NormalizeLanguageCode(
        string language)
    {
        ArgumentNullException.ThrowIfNull(language);

        return language.ToLowerInvariant();
    }

    /// <summary>
    /// Parses the pipe-delimited custom wiki setting.
    /// </summary>
    public static IReadOnlyList<string> ParseCustomWikis(
        string? storedCustomWikis)
    {
        if (string.IsNullOrWhiteSpace(storedCustomWikis))
        {
            return Array.Empty<string>();
        }

        return storedCustomWikis
            .Split(
                '|',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Converts the persisted privacy value to the checkbox representation
    /// used by the settings interface.
    /// </summary>
    public static bool GetPrivacyCheckboxState(
        bool privacyEnabled)
    {
        return !privacyEnabled;
    }

    /// <summary>
    /// Converts the privacy checkbox state to the persisted setting value.
    /// </summary>
    public static bool GetPrivacySetting(
        bool checkboxState)
    {
        return !checkboxState;
    }

    /// <summary>
    /// Gets the protocol selector index for the supplied protocol.
    /// </summary>
    public static int GetProtocolSelectionIndex(
        string protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        return string.Equals(
            protocol,
            "http://",
            StringComparison.Ordinal)
            ? 1
            : 0;
    }

    /// <summary>
    /// Determines whether AWB attribution suppression is available for the
    /// specified project.
    /// </summary>
    public static bool SupportsAwbAttributionSuppression(
        ProjectEnum project)
    {
        return project == ProjectEnum.custom ||
               project == ProjectEnum.wikia ||
               project == ProjectEnum.fandom;
    }

    /// <summary>
    /// Normalizes a custom wiki project value.
    /// </summary>
    public static string NormalizeCustomProject(
        string? customProject,
        ProjectEnum project)
    {
        string normalized =
            CustomProjectRegex.Replace(
                (customProject ?? string.Empty).Trim(),
                "$1$2");

        normalized =
            normalized.TrimEnd('/');

        if (normalized.Length > 0 &&
            project == ProjectEnum.custom)
        {
            normalized += "/";
        }

        return normalized;
    }

    /// <summary>
    /// Builds the pipe-delimited value used to persist custom wiki entries.
    /// </summary>
    public static string BuildCustomWikisSetting(
        IEnumerable<string?> customWikis)
    {
        ArgumentNullException.ThrowIfNull(customWikis);

        return string.Join(
            "|",
            customWikis
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item))
                .Select(item =>
                    item!.Trim()));
    }

    /// <summary>
    /// Gets the available wiki languages for the specified project.
    /// </summary>
    /// <param name="project">
    /// The wiki project whose language list should be returned.
    /// </param>
    /// <returns>
    /// The languages supported by the selected project.
    /// </returns>
    public static IReadOnlyList<string> GetLanguagesForProject(
        ProjectEnum project)
    {
        return project switch
        {
            ProjectEnum.wikipedia =>
                SiteMatrix.WikipediaLanguages,

            ProjectEnum.wiktionary =>
                SiteMatrix.WiktionaryLanguages,

            ProjectEnum.wikibooks =>
                SiteMatrix.WikibooksLanguages,

            ProjectEnum.wikinews =>
                SiteMatrix.WikinewsLanguages,

            ProjectEnum.wikiquote =>
                SiteMatrix.WikiquoteLanguages,

            ProjectEnum.wikisource =>
                SiteMatrix.WikisourceLanguages,

            ProjectEnum.wikiversity =>
                SiteMatrix.WikiversityLanguages,

            _ =>
                SiteMatrix.Languages
        };
    }

    /// <summary>
    /// Determines whether the specified project uses custom-project controls.
    /// </summary>
    public static bool UsesCustomProjectControls(
        ProjectEnum project)
    {
        return project == ProjectEnum.custom ||
               project == ProjectEnum.wikia ||
               project == ProjectEnum.fandom;
    }

    /// <summary>
    /// Determines whether the specified project allows custom connection settings.
    /// </summary>
    public static bool SupportsCustomConnectionSettings(
        ProjectEnum project)
    {
        return project == ProjectEnum.custom;
    }

    /// <summary>
    /// Determines whether the specified project requires HTTPS.
    /// </summary>
    public static bool RequiresHttps(
        ProjectEnum project)
    {
        return project == ProjectEnum.wikia ||
               project == ProjectEnum.fandom;
    }

    /// <summary>
    /// Determines whether the specified project requires a custom project value.
    /// </summary>
    public static bool RequiresCustomProject(
        ProjectEnum project)
    {
        return project == ProjectEnum.custom ||
               project == ProjectEnum.wikia ||
               project == ProjectEnum.fandom;
    }

    /// <summary>
    /// Normalizes the persisted article-load action selection.
    /// </summary>
    /// <remarks>
    /// Legacy selection index 2 represented showing the edit page and is no
    /// longer supported. It is mapped to the default action at index 0.
    /// </remarks>
    /// <param name="selectionIndex">
    /// The stored or selected article-load action index.
    /// </param>
    /// <returns>
    /// The supported article-load action index.
    /// </returns>
    public static int NormalizeOnLoadSelection(
        int selectionIndex)
    {
        return selectionIndex == 2
            ? 0
            : selectionIndex;
    }

    /// <summary>
    /// Determines whether edit-box autosave can remain enabled for the
    /// specified file path.
    /// </summary>
    /// <param name="isEnabled">
    /// Whether autosave is currently enabled.
    /// </param>
    /// <param name="filePath">
    /// The configured autosave file path.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when autosave should remain enabled;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool NormalizeAutoSaveEnabled(
        bool isEnabled,
        string? filePath)
    {
        return isEnabled &&
            !string.IsNullOrWhiteSpace(filePath);
    }

    /// <summary>
    /// Determines whether a custom wiki should be added to the stored
    /// custom-wiki collection.
    /// </summary>
    /// <param name="customWiki">
    /// The normalized custom wiki value to consider adding.
    /// </param>
    /// <param name="existingCustomWikis">
    /// The custom wiki values that are already stored.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the value is nonempty and does not already
    /// exist in the collection; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool ShouldAddCustomWiki(
        string? customWiki,
        IEnumerable<string?> existingCustomWikis)
    {
        ArgumentNullException.ThrowIfNull(existingCustomWikis);

        if (string.IsNullOrWhiteSpace(customWiki))
        {
            return false;
        }

        return !existingCustomWikis.Any(
            item => string.Equals(
                item,
                customWiki,
                StringComparison.Ordinal));
    }

    /// <summary>
    /// Determines whether the specified project supports selection from the
    /// standard wiki language list.
    /// </summary>
    /// <param name="project">
    /// The wiki project to evaluate.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the language selector should be enabled;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool SupportsLanguageSelection(
        ProjectEnum project)
    {
        return project < ProjectEnum.species;
    }
}