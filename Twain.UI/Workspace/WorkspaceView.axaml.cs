using Avalonia.Controls;
using Avalonia.Interactivity;
using Twain.UI.Views.Shell;

namespace Twain.UI.Views.Workspaces;

/// <summary>
/// Displays the active Twain workspace.
/// </summary>
public partial class WorkspaceView : UserControl
{
    /// <summary>
    /// Initializes the workspace view.
    /// </summary>
    public WorkspaceView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Opens the application preferences window through the shell.
    /// </summary>
    private void PreferencesButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.OpenPreferences();
    }

    /// <summary>
    /// Opens the saved profiles and login window through the shell.
    /// </summary>
    private async void ProfilesButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        await shell.ShowLoginAsync();
    }

    /// <summary>
    /// Opens the custom module window through the shell.
    /// </summary>
    private void CustomModulesButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.OpenCustomModules();
    }

    /// <summary>
    /// Opens the regular expression tester through the shell.
    /// </summary>
    private void RegexTesterButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.OpenRegexTester();
    }

    /// <summary>
    /// Opens the database scanner through the shell.
    /// </summary>
    private void DatabaseScannerButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if(TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.OpenDatabaseScanner();
    }
}