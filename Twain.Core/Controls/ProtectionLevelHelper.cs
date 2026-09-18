namespace Twain.Core.Controls;

/// <summary>
/// Provides the protection levels available for the current wiki.
/// </summary>
public static class ProtectionLevelHelper
{
    /// <summary>
    /// Gets the protection levels available for the specified language.
    /// </summary>
    public static IReadOnlyList<ProtectionLevel> GetLevels(
        string languageCode)
    {
        List<ProtectionLevel> levels = [];

        levels.AddRange(
            ProtectionLevel.BasicLevels);

        ProtectionLevel? customLevel =
            GetCustomLevel(languageCode);

        if (customLevel is not null)
            levels.Add(customLevel);

        levels.AddRange(
            ProtectionLevel.Sysop);

        return levels;
    }

    /// <summary>
    /// Determines whether cascading protection is enabled using the
    /// legacy EditProtectControl selection behavior.
    /// </summary>
    public static bool IsCascadingEnabled(
        int editSelectedIndex,
        int moveSelectedIndex)
    {
        return editSelectedIndex == 2 &&
            moveSelectedIndex == 2;
    }

    private static ProtectionLevel? GetCustomLevel(
        string languageCode)
    {
        return languageCode switch
        {
            "en" =>
                new ProtectionLevel(
                    "templateeditor",
                    "Template editor"),

            "ar" =>
                new ProtectionLevel(
                    "autoreview",
                    "autoreview"),

            "ckb" or "he" =>
                new ProtectionLevel(
                    "autopatrol",
                    "autopatrol"),

            "pl" =>
                new ProtectionLevel(
                    "editor",
                    "editor"),

            "pt" =>
                new ProtectionLevel(
                    "autoreviewer",
                    "autoreviewer"),

            _ => null
        };
    }
}

/// <summary>
/// Represents a page-protection level.
/// </summary>
public sealed class ProtectionLevel
{
    public ProtectionLevel(
        string group,
        string display)
    {
        Group = group;
        Display = display;
    }

    public string Group { get; }

    public string Display { get; }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Display)
            ? string.Empty
            : Display;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ProtectionLevel protectionLevel)
            return protectionLevel.Group == Group;

        if (obj is string group)
            return Group == group;

        return false;
    }

    public override int GetHashCode()
    {
        return Group.GetHashCode();
    }

    public static readonly ProtectionLevel[] BasicLevels =
    [
        new ProtectionLevel(
            "",
            "Unprotected"),

        new ProtectionLevel(
            "autoconfirmed",
            "Semi-protected")
    ];

    public static readonly ProtectionLevel[] Sysop =
    [
        new ProtectionLevel(
            "sysop",
            "Fully protected")
    ];
}