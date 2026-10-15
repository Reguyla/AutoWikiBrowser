
using System;
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
    /// Gets the currently selected theme.
    /// </summary>
    public static TwainTheme CurrentTheme { get; private set; }
        = TwainTheme.Classic;

    /// <summary>
    /// Applies a theme to the running application.
    /// </summary>
    public static void ApplyTheme(TwainTheme theme)
    {
        Application? application = Application.Current;

        if (application is null)
        {
            return;
        }

        string resourcePath = theme switch
        {
            TwainTheme.Classic =>
                "avares://Twain.UI/Themes/ClassicTheme.axaml",

            TwainTheme.Legacy =>
                "avares://Twain.UI/Themes/LegacyTheme.axaml",

            TwainTheme.ModernLight =>
                throw new NotSupportedException(
                    "Modern Light has not been implemented yet."),

            TwainTheme.ModernDark =>
                throw new NotSupportedException(
                    "Modern Dark has not been implemented yet."),

            _ => throw new ArgumentOutOfRangeException(nameof(theme))
        };

        var resources = application.Resources.MergedDictionaries;

        if (_activeThemeResources is not null)
        {
            resources.Remove(_activeThemeResources);
        }

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
    }
}
