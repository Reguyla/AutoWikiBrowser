using System.Reflection;

namespace Twain.Core.Controls;

/// <summary>
/// Provides application and assembly information used by About dialogs.
/// </summary>
public static class AboutInformation
{
    /// <summary>
    /// Gets the GPL notice used by Twain and legacy AutoWikiBrowser components.
    /// </summary>
    public static string GPLNotice =>
        Resources.GPL;

    /// <summary>
    /// Gets the description associated with the specified assembly.
    /// </summary>
    public static string AssemblyDescription(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        AssemblyDescriptionAttribute? attribute =
            assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();

        return attribute?.Description ?? string.Empty;
    }

    /// <summary>
    /// Gets the copyright notice associated with the specified assembly.
    /// </summary>
    public static string AssemblyCopyright(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        AssemblyCopyrightAttribute? attribute =
            assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();

        return attribute?.Copyright ?? string.Empty;
    }

    /// <summary>
    /// Builds the detailed information displayed by About dialogs.
    /// </summary>
    public static string GetDetailedMessage(Assembly assembly)
    {
        return
            AssemblyDescription(assembly) +
            Environment.NewLine +
            Environment.NewLine +
            GPLNotice;
    }
}