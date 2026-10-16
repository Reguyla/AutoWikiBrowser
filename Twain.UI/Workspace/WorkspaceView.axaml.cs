using Avalonia.Controls;
using Avalonia.Interactivity;
using Twain.UI.Profiles;
using Twain.UI.Shell;
using Twain.UI.ViewModels.Workspaces;
using Twain.UI.Views.Shell;

namespace Twain.UI.Views.Workspaces;

/// <summary>
/// Displays the active Twain workspace.
/// </summary>
public partial class WorkspaceView : UserControl
{
    /// <summary>
    /// Opens the application preferences window through the shell.
    /// </summary>
    private void PreferencesButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenEmbeddedPreferences();
    }

    /// <summary>
    /// Opens Preferences using the current application settings.
    /// </summary>
    public void OpenEmbeddedPreferences()
    {
        if (DataContext is not WorkspaceViewModel workspace)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not ShellWindow shell ||
            shell.DataContext is not ShellViewModel shellViewModel)
        {
            return;
        }

        PreferencesView.BeginEditing(shellViewModel.Preferences);

        workspace.ShowPreferencesCommand.Execute(null);
    }

    /// <summary>
    /// Opens the saved profiles and login window through the shell.
    /// </summary>
    private void ProfilesButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is WorkspaceViewModel viewModel)
        {
            viewModel.ShowProfilesCommand.Execute(null);
        }
    }

    public WorkspaceView()
    {
        InitializeComponent();

        ProfilesView.Completed += ProfilesView_Completed;
        PreferencesView.Completed += PreferencesView_Completed;

        DataContextChanged += (_, _) =>
        {
            if (DataContext is WorkspaceViewModel viewModel)
            {
                ProfilesView.SetSession(viewModel.Session);
            }
        };
    }

    /// <summary>
    /// Applies accepted preferences or discards canceled changes.
    /// </summary>
    private async void PreferencesView_Completed(
        object? sender,
        bool accepted)
    {
        if (DataContext is not WorkspaceViewModel workspace)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not ShellWindow shell ||
            shell.DataContext is not ShellViewModel shellViewModel)
        {
            return;
        }

        if (accepted)
        {
            PreferencesView.ApplyPreferences(
                shellViewModel.Preferences);

            shellViewModel.ApplySitePreferences();

            if (workspace.Editor.ApplyAppearanceRequested
                is { } applyAppearance)
            {
                await applyAppearance();
            }
        }
        else
        {
            PreferencesView.CancelChanges();
        }

        workspace.ShowEditingCommand.Execute(null);
    }

    private void ProfilesView_Completed(
        object? sender,
        bool loggedIn)
    {
        if (DataContext is not WorkspaceViewModel viewModel)
        {
            return;
        }

        if (loggedIn &&
            TopLevel.GetTopLevel(this) is ShellWindow shell)
        {
            shell.NotifyLoginCompleted();
        }

        viewModel.ShowEditingCommand.Execute(null);
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

    /// <summary>
    /// Opens the list comparer through the shell.
    /// </summary>
    private void ListComparerButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.OpenListComparer();
    }

    /// <summary>
    /// Opens the list splitter through the shell.
    /// </summary>
    private void ListSplitterButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.OpenListSplitter();
    }

    /// <summary>
    /// Opens the About Twain window through the shell.
    /// </summary>
    private void AboutButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.OpenAbout();
    }

    private async void OpenSettingsButton_Click(
        object? sender,
        RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is ShellWindow shell)
            {
                await shell.OpenSettingsAsync();
            }
        }

    /// <summary>
    /// Saves the current settings through the application shell.
    /// </summary>
    private async void SaveSettingsButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        await shell.SaveSettingsAsync();
    }

    /// <summary>
    /// Opens Save As through the application shell.
    /// </summary>
    private async void SaveSettingsAsButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        await shell.SaveSettingsAsAsync();
    }

    /// <summary>
    /// Opens the external processing window through the shell.
    /// </summary>
    private void ExternalProcessingButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.OpenExternalProcessing();
    }

    /// <summary>
    /// Exits Twain through the application shell.
    /// </summary>
    private void ExitButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        shell.ExitApplication();
    }

    private async void BotSettingsButton_Click(
    object? sender,
    RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
        {
            return;
        }

        await shell.OpenBotSettingsAsync();
    }
}