using Twain.UI.Controls;
using Twain.UI.Themes;

namespace Twain.UI.Settings;

/// <summary>
/// Stores application-wide appearance preferences.
/// These settings are independent of wiki-specific settings.
/// </summary>
public sealed class ApplicationAppearanceSettings
{
    /// <summary>
    /// Gets or sets the selected application theme.
    /// </summary>
    public TwainTheme Theme { get; set; } =
        TwainTheme.Classic;

    /// <summary>
    /// Gets or sets whether the processing toolbar
    /// should be displayed by default.
    /// </summary>
    public bool ShowProcessingToolbar { get; set; } = true;

    /// <summary>
    /// Gets or sets how processing toolbar buttons
    /// display their icons and text.
    /// </summary>
    public DisplayMode DisplayMode { get; set; } =
        DisplayMode.IconsAndText;

    /// <summary>
    /// Gets or sets how icons and labels appear in the navigation pane.
    /// </summary>
    public DisplayMode NavigationDisplayMode { get; set; } =
        DisplayMode.IconsAndText;
}