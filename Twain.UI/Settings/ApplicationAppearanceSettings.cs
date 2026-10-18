
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
}
