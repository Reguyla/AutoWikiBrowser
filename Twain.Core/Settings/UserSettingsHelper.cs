using System.Text.RegularExpressions;

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
}