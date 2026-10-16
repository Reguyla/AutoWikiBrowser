using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Twain.Core.AWBSettings;
using Twain.CustomModules;
using Twain.UI.About;
using Twain.UI.BotSettings;
using Twain.UI.Controls;
using Twain.UI.Controls.Lists;
using Twain.UI.DBScanner;
using Twain.UI.ExternalPrograms;
using Twain.UI.Preferences;
using Twain.UI.Profiles;
using Twain.UI.Shell;
using Twain.UI.Views.Workspaces;

namespace Twain.UI.Views.Shell;

/// <summary>
/// Hosts the top-level Twain user interface.
/// </summary>
public partial class ShellWindow : Window
{
    /// <summary>
    /// Initializes the shell window.
    /// </summary>
    public ShellWindow()
    {
        InitializeComponent();

        DataContextChanged += ShellWindow_DataContextChanged;
    }

    /// <summary>
    /// Connects shell-owned UI services to the active workspace.
    /// </summary>
    private void ShellWindow_DataContextChanged(
        object? sender,
        EventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        viewModel.Workspace.ConfirmShutdownRequested =
            ConfirmShutdownAsync;
    }

    /// <summary>
    /// Displays the profile login workflow for the active session.
    /// </summary>
    public Task ShowLoginAsync()
    {
        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.Workspace.ShowProfilesCommand.Execute(null);
        }

        return Task.CompletedTask;
    }

    public void NotifyLoginCompleted()
    {
        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.LoginCompleted();
        }
    }

    private async void LoginProfilesMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await ShowLoginAsync();
    }

    /// <summary>
    /// Closes the Twain application window.
    /// </summary>
    public void ExitApplication()
    {
        Close();
    }

    private void ExitMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ExitApplication();
    }

    /// <summary>
    /// Opens the custom module window using the active Twain workspace.
    /// </summary>
    public async void OpenCustomModules()
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        CustomModule window =
            new(
                viewModel.Workspace.CustomModuleState,
                viewModel.Workspace.TwainHost,
                viewModel.Preferences);

        await window.ShowDialog(this);
    }

    private void MakeModuleMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenCustomModules();
    }

    /// <summary>
    /// Opens the external processing window.
    /// </summary>
    public async void OpenExternalProcessing()
    {
        ExternalProgramWindow window = new();
        await window.ShowDialog<bool>(this);
    }

    private void ExternalProcessingMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenExternalProcessing();
    }

    /// <summary>
    /// Opens the regular expression tester window.
    /// </summary>
    public async void OpenRegexTester()
    {
        RegexTesterWindow window = new();

        await window.ShowDialog(this);
    }

    private void RegexTesterMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenRegexTester();
    }

    /// <summary>
    /// Opens the database scanner window.
    /// </summary>
    public async void OpenDatabaseScanner()
    {
        DatabaseScannerWindow window = new();

        await window.ShowDialog(this);
    }

    private void DatabaseScannerMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenDatabaseScanner();
    }

    /// <summary>
    /// Opens the list comparer window.
    /// </summary>
    public async void OpenListComparer()
    {
        ListComparerWindow window = new();

        await window.ShowDialog(this);
    }

    private void ListComparerMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenListComparer();
    }

    /// <summary>
    /// Opens the list splitter window.
    /// </summary>
    public async void OpenListSplitter()
    {
        ListSplitterWindow window = new();

        await window.ShowDialog(this);
    }

    private void ListSplitterMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenListSplitter();
    }

    /// <summary>
    /// Opens the bot processing settings window.
    /// </summary>
    public async Task OpenBotSettingsAsync()
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        var window = new BotSettingsWindow
        {
            DataContext = viewModel.Workspace.BotSettings
        };

        await window.ShowDialog<bool>(this);
    }

    /// <summary>
    /// Confirms the configured action after article processing completes.
    /// </summary>
    /// <param name="shutdownAction">
    /// The configured action to confirm.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the user confirms the action;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public async Task<bool> ConfirmShutdownAsync(
        BotShutdownAction shutdownAction)
    {
        var window =
            new ShutdownNotificationWindow(
                shutdownAction);

        return await window.ShowDialog<bool>(this);
    }

    /// <summary>
    /// Opens application preferences inside the workspace.
    /// </summary>
    public void OpenPreferences()
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        viewModel.Workspace.ShowPreferencesCommand.Execute(null);
    }

    /// <summary>
    /// Opens the application preferences window from the main menu.
    /// </summary>
    private void PreferencesMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenPreferences();
    }

    /// <summary>
    /// Opens the About Twain window.
    /// </summary>
    public async void OpenAbout()
    {
        AboutBoxWindow window = new();

        await window.ShowDialog(this);
    }

    private void AboutMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenAbout();
    }

    /// <summary>
    /// Opens an AWB-compatible XML settings file.
    /// </summary>
    public async Task OpenSettingsAsync()
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open Twain Settings",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("AWB/Twain Settings")
                {
                    Patterns = ["*.xml", "*.awb"]
                }
                ]
            });

        if (files.Count == 0)
        {
            return;
        }

        string? path = files[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            viewModel.LoadSettings(path);
        }
        catch (Exception ex)
        {
            // Temporary until the shared Avalonia error dialog is wired here.
            System.Diagnostics.Debug.WriteLine(
                $"Unable to open settings: {ex}");
        }
    }

    /// <summary>
    /// Saves the current AWB-compatible settings file.
    /// Opens Save As if no settings file has been selected.
    /// </summary>
    public async Task SaveSettingsAsync()
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.CurrentSettingsFile))
        {
            await SaveSettingsAsAsync();
            return;
        }

        try
        {
            viewModel.SaveSettings();
        }
        catch (Exception ex)
        {
            // Temporary until the shared Avalonia error dialog is wired here.
            System.Diagnostics.Debug.WriteLine(
                $"Unable to save settings: {ex}");
        }
    }

    /// <summary>
    /// Prompts for a destination and saves the current
    /// AWB-compatible XML settings.
    /// </summary>
    public async Task SaveSettingsAsAsync()
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save Twain Settings As",
                SuggestedFileName = "TwainSettings.xml",
                DefaultExtension = "xml",
                FileTypeChoices =
                [
                    new FilePickerFileType("AWB/Twain Settings")
                {
                    Patterns = ["*.xml"]
                }
                ]
            });

        if (file == null)
        {
            return;
        }

        string? path = file.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            viewModel.SaveSettingsAs(path);
        }
        catch (Exception ex)
        {
            // Temporary until the shared Avalonia error dialog is wired here.
            System.Diagnostics.Debug.WriteLine(
                $"Unable to save settings: {ex}");
        }
    }
}