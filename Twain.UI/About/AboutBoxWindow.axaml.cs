using System;
using System.Reflection;
using Avalonia.Interactivity;
using Twain.Core.Controls;

namespace Twain.UI.About;

/// <summary>
/// Displays version, environment, licensing, and historical project
/// information for Twain.
/// </summary>
public partial class AboutBoxWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AboutBoxWindow"/> class.
    /// </summary>
    public AboutBoxWindow()
    {
        InitializeComponent();

        Assembly assembly =
            typeof(AboutBoxWindow).Assembly;

        Version? version =
            assembly.GetName().Version;

        VersionText.Text =
            version is null
                ? "Version information unavailable"
                : $"Version {version}";

        EnvironmentText.Text =
            $"""
            .NET version: {Environment.Version}
            Operating system: {Environment.OSVersion}
            """;

        DetailsText.Text =
            AboutInformation.GetDetailedMessage(
                assembly);
    }

    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private void AutoWikiBrowserButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        // The project-history link will be connected after the
        // Avalonia window itself is established.
    }
}