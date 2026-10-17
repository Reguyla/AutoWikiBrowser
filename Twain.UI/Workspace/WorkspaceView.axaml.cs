using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.ComponentModel;
using Twain.CustomModules;
using Twain.UI.BotSettings;
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
    private BotSettingsViewModel? _editingBotSettings;
    private CustomModule? _customModuleView;
    private WorkspaceViewModel? _navigationViewModel;

    /// <summary>
    /// Opens the application preferences window through the shell.
    /// </summary>
    private void PreferencesButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenEmbeddedPreferences();
    }

    public WorkspaceView()
    {
        InitializeComponent();

        ProfilesView.Completed += ProfilesView_Completed;
        PreferencesView.Completed += PreferencesView_Completed;
        BotSettingsView.Completed += BotSettingsView_Completed;

        PreferencesView.PropertyChanged += PreferencesView_PropertyChanged;
        BotSettingsView.PropertyChanged += BotSettingsView_PropertyChanged;

        DataContextChanged += (_, _) =>
        {
            if (_navigationViewModel is not null)
            {
                _navigationViewModel.PropertyChanged -= Navigation_PropertyChanged;
            }

            _navigationViewModel = DataContext as WorkspaceViewModel;

            if (_navigationViewModel is not null)
            {
                ProfilesView.SetSession(_navigationViewModel.Session);
                _navigationViewModel.PropertyChanged += Navigation_PropertyChanged;
            }

            UpdateNavigationLayout();
        };
    }

    private void Navigation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceViewModel.IsNavigationCollapsed))
        {
            UpdateNavigationLayout();
        }
    }

    private void UpdateNavigationLayout()
    {
        bool collapsed =
            _navigationViewModel?.IsNavigationCollapsed == true;

        WorkspaceLayoutGrid.ColumnDefinitions[0].Width =
            new GridLength(collapsed ? 48 : 220);

        NavigationBorder.Padding =
            new Thickness(collapsed ? 4 : 12);
    }

    /// <summary>
    /// Initializes the embedded Custom Modules editor using the
    /// active workspace state, host, and shared preferences.
    /// </summary>
    private void InitializeCustomModules()
    {
        if (_customModuleView is not null)
        {
            return;
        }

        if (DataContext is not WorkspaceViewModel workspace)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not ShellWindow shell ||
            shell.DataContext is not ShellViewModel shellViewModel)
        {
            return;
        }

        _customModuleView = new CustomModule(
            workspace.CustomModuleState,
            workspace.TwainHost,
            shellViewModel.Preferences);

        _customModuleView.CloseRequested += (_, _) =>
        {
            workspace.ShowEditingCommand.Execute(null);
        };

        CustomModulesHost.Content = _customModuleView;
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

    /// <summary>
    /// Applies accepted bot settings or discards canceled changes.
    /// </summary>
    private void BotSettingsView_Completed(
        object? sender,
        bool accepted)
    {
        if (DataContext is not WorkspaceViewModel workspace)
        {
            return;
        }

        if (accepted && _editingBotSettings is not null)
        {
            BotSettingsViewModel source = _editingBotSettings;
            BotSettingsViewModel target = workspace.BotSettings;

            target.AutoSaveEnabled = source.AutoSaveEnabled;
            target.AutoSaveDelay = source.AutoSaveDelay;
            target.MaximumEdits = source.MaximumEdits;
            target.SuppressUsingTwain = source.SuppressUsingTwain;
            target.NudgeEnabled = source.NudgeEnabled;
            target.SkipAfterRepeatedNudge = source.SkipAfterRepeatedNudge;
            target.AutomaticShutdownEnabled = source.AutomaticShutdownEnabled;
            target.ShutdownAction = source.ShutdownAction;
        }

        _editingBotSettings = null;
        BotSettingsView.DataContext = null;

        workspace.ShowEditingCommand.Execute(null);
    }

    /// <summary>
    /// Discards unaccepted bot settings when leaving the embedded view.
    /// </summary>
    private void BotSettingsView_PropertyChanged(
        object? sender,
        Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Avalonia.Visual.IsVisibleProperty &&
            !BotSettingsView.IsVisible)
        {
            _editingBotSettings = null;
            BotSettingsView.DataContext = null;
        }
    }

    /// <summary>
    /// Opens an editable copy of the current bot settings.
    /// </summary>
    public void OpenEmbeddedBotSettings()
    {
        if (DataContext is not WorkspaceViewModel workspace)
        {
            return;
        }

        BotSettingsViewModel current = workspace.BotSettings;

        _editingBotSettings = new BotSettingsViewModel
        {
            AutoSaveEnabled = current.AutoSaveEnabled,
            AutoSaveDelay = current.AutoSaveDelay,
            MaximumEdits = current.MaximumEdits,
            SuppressUsingTwain = current.SuppressUsingTwain,
            NudgeEnabled = current.NudgeEnabled,
            SkipAfterRepeatedNudge = current.SkipAfterRepeatedNudge,
            NudgeCount = current.NudgeCount,
            CurrentArticleNudgeCount = current.CurrentArticleNudgeCount,
            AutomaticShutdownEnabled = current.AutomaticShutdownEnabled,
            ShutdownAction = current.ShutdownAction,
            CanUseBotMode = current.CanUseBotMode
        };

        // Reset the actual processing counters, not just the editable copy.
        _editingBotSettings.ResetLiveNudgeCounters = () =>
        {
            current.ResetNudgeCountCommand.Execute(null);

            _editingBotSettings.NudgeCount = current.NudgeCount;
            _editingBotSettings.CurrentArticleNudgeCount =
                current.CurrentArticleNudgeCount;
        };

        BotSettingsView.DataContext = _editingBotSettings;

        workspace.ShowBotSettingsCommand.Execute(null);
    }

    /// <summary>
    /// Restores the original theme when Preferences is hidden
    /// without the user accepting the changes.
    /// </summary>
    private void PreferencesView_PropertyChanged(
        object? sender,
        AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty &&
            !PreferencesView.IsVisible)
        {
            PreferencesView.CancelChanges();
        }
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
    /// Opens the embedded Custom Modules editor.
    /// </summary>
    private void CustomModulesButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenEmbeddedCustomModules();
    }

    /// <summary>
    /// Opens the embedded Custom Modules editor.
    /// </summary>
    public void OpenEmbeddedCustomModules()
    {
        if (DataContext is not WorkspaceViewModel workspace)
        {
            return;
        }

        InitializeCustomModules();

        if (_customModuleView is null)
        {
            return;
        }

        workspace.ShowCustomModulesCommand.Execute(null);
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
        if (TopLevel.GetTopLevel(this) is not ShellWindow shell)
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