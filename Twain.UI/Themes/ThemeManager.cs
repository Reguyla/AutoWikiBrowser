using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Twain.UI.Themes;

/// <summary>
/// Manages the active Twain application theme.
/// </summary>
public static class ThemeManager
{
    private static ResourceInclude? _activeThemeResources;

    /// <summary>
    /// Raised after the application theme changes successfully.
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// Gets the currently selected theme.
    /// </summary>
    public static TwainTheme CurrentTheme { get; private set; }
        = TwainTheme.ModernLight;

    /// <summary>
    /// Applies a theme to the running application.
    /// </summary>
    public static void ApplyTheme(TwainTheme theme)
    {
        Application? application = Application.Current;
        if (application is null)
            return;

        // Unsupported or retired themes fall back to Modern Light.
        if (theme != TwainTheme.ModernLight &&
            theme != TwainTheme.ModernDark)
        {
            theme = TwainTheme.ModernLight;
        }

        string resourcePath = theme switch
        {
            TwainTheme.ModernLight =>
                "avares://Twain.UI/Themes/ModernLightTheme.axaml",

            TwainTheme.ModernDark =>
                "avares://Twain.UI/Themes/ModernDarkTheme.axaml",

            _ => throw new ArgumentOutOfRangeException(nameof(theme))
        };

        var resources = application.Resources.MergedDictionaries;

        if (_activeThemeResources is not null)
            resources.Remove(_activeThemeResources);

        var newThemeResources = new ResourceInclude(
            new Uri("avares://Twain.UI/"))
        {
            Source = new Uri(resourcePath)
        };

        resources.Add(newThemeResources);

        _activeThemeResources = newThemeResources;
        CurrentTheme = theme;

        application.RequestedThemeVariant =
            theme == TwainTheme.ModernDark
                ? ThemeVariant.Dark
                : ThemeVariant.Light;

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }
}
