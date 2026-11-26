using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Linq;
using System.Threading.Tasks;
using Twain.Core;
using Twain.UI.About;
using Twain.UI.BotSettings;
using Twain.UI.Controls;
using Twain.UI.Controls.Lists;
using Twain.UI.DBScanner;
using Twain.UI.ExternalPrograms;
using Twain.UI.Shell;
using Twain.UI.Talk;

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

    private ShellViewModel? _subscribedShellViewModel;

    private void ShellWindow_DataContextChanged(
        object? sender,
        EventArgs e)
    {
        if (_subscribedShellViewModel is not null)
        {
            _subscribedShellViewModel.NewTalkMessagesDetected -=
                ShellViewModel_NewTalkMessagesDetected;

            _subscribedShellViewModel.Workspace
                .NewTalkMessagesNotificationRequested = null;
        }

        _subscribedShellViewModel = DataContext as ShellViewModel;

        if (_subscribedShellViewModel is not ShellViewModel viewModel)
        {
            return;
        }

        viewModel.Workspace.ConfirmShutdownRequested =
            ConfirmShutdownAsync;

        viewModel.Workspace.ConfirmSpamFilterRetryRequested =
            ConfirmSpamFilterRetryAsync;

        viewModel.NewTalkMessagesDetected +=
            ShellViewModel_NewTalkMessagesDetected;

        viewModel.Workspace.NewTalkMessagesNotificationRequested =
            () => ShellViewModel_NewTalkMessagesDetected(
                viewModel,
                EventArgs.Empty);
    }

    private bool _talkMessageDialogVisible;

    /// <summary>
    /// Displays the talk-message notification when requested
    /// by the active shell view model.
    /// </summary>
    private async void ShellViewModel_NewTalkMessagesDetected(
        object? sender,
        EventArgs e)
    {
        if (_talkMessageDialogVisible)
        {
            return;
        }

        if (DataContext is not ShellViewModel viewModel ||
            !viewModel.IsLoggedIn)
        {
            return;
        }

        _talkMessageDialogVisible = true;

        try
        {
            // Match legacy AWB behavior: stop processing before
            // allowing the user to view new talk-page messages.
            viewModel.Workspace.StopProcessingCommand.Execute(null);

            // Require the wiki session to refresh its status.
            viewModel.Session.RequireUpdate();

            Activate();


            TalkMessageWindow dialog = new();

            bool viewMessages =
                await dialog.ShowDialog<bool>(this);

            if (!viewMessages)
            {
                return;
            }

            Tools.OpenUserTalkInBrowser(viewModel.Session.User.Name);

            viewModel.Session.Editor.SynchronousEditor.ClearNewMessages();
        }

        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to display or process talk-message notification: {ex}");

            ErrorHandler.HandleException(ex);
        }
        finally
        {
            _talkMessageDialogVisible = false;
        }
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

    /// <summary>
    /// Opens Profiles or logs out of the active wiki session.
    /// </summary>
    private async void LoginProfilesMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        if (!viewModel.IsLoggedIn)
        {
            await ShowLoginAsync();
            return;
        }

        try
        {
            await viewModel.LogoutAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to log out: {ex}");

            viewModel.LoginCompleted();
        }
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
    /// Opens Custom Modules inside the workspace.
    /// </summary>
    public Task OpenCustomModuleAsync()
    {
        MainWorkspaceView.OpenEmbeddedCustomModules();

        return Task.CompletedTask;
    }

    private async void MakeModuleMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await OpenCustomModuleAsync();
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
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        var workspace = viewModel.Workspace;
        var articles = workspace.MakeList.Articles.ToList();

        int preference =
            workspace.Preferences.Tool.ListComparerUseCurrentArticleList;

        bool useCurrentList = false;

        switch (preference)
        {
            case 0: // Ask
                if (articles.Count > 0)
                {
                    var yesButton = new Button
                    {
                        Content = "Yes",
                        MinWidth = 90
                    };

                    var noButton = new Button
                    {
                        Content = "No",
                        MinWidth = 90
                    };

                    var dialog = new Window
                    {
                        Title = "Copy Article List?",
                        Width = 440,
                        SizeToContent = SizeToContent.Height,
                        CanResize = false,
                        WindowStartupLocation =
                            WindowStartupLocation.CenterOwner,
                        Content = new StackPanel
                        {
                            Margin = new Avalonia.Thickness(20),
                            Spacing = 16,
                            Children =
                        {
                            new TextBlock
                            {
                                Text =
                                    "Would you like to copy your current " +
                                    "Article List to the List Comparer?",
                                TextWrapping =
                                    Avalonia.Media.TextWrapping.Wrap
                            },
                            new StackPanel
                            {
                                Orientation =
                                    Avalonia.Layout.Orientation.Horizontal,
                                HorizontalAlignment =
                                    Avalonia.Layout.HorizontalAlignment.Right,
                                Spacing = 10,
                                Children =
                                {
                                    yesButton,
                                    noButton
                                }
                            }
                        }
                        }
                    };

                    yesButton.Click += (_, _) => dialog.Close(true);
                    noButton.Click += (_, _) => dialog.Close(false);

                    useCurrentList =
                        await dialog.ShowDialog<bool>(this);
                }
                break;

            case 1: // Always
                useCurrentList = true;
                break;

            case 2: // Never
            default:
                useCurrentList = false;
                break;
        }

        ListComparerWindow window =
            useCurrentList
                ? new ListComparerWindow(
                    workspace.Session,
                    workspace.Preferences,
                    articles)
                : new ListComparerWindow(
                    workspace.Session,
                    workspace.Preferences);

        window.UseListRequested += async (_, selectedArticles) =>
        {
            if (workspace.Options.IsProcessing)
            {
                var okButton = new Button
                {
                    Content = "OK",
                    MinWidth = 90
                };

                var dialog = new Window
                {
                    Title = "Article Processing Active",
                    Width = 440,
                    SizeToContent = SizeToContent.Height,
                    CanResize = false,
                    WindowStartupLocation =
                        WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Spacing = 16,
                        Children =
                {
                    new TextBlock
                    {
                        Text =
                            "The article list cannot be replaced " +
                            "while Twain is processing articles. " +
                            "Stop processing before using this list.",
                        TextWrapping =
                            Avalonia.Media.TextWrapping.Wrap
                    },
                    new StackPanel
                    {
                        Orientation =
                            Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment =
                            Avalonia.Layout.HorizontalAlignment.Right,
                        Children = { okButton }
                    }
                }
                    }
                };

                okButton.Click += (_, _) => dialog.Close();

                await dialog.ShowDialog(window);
                return;
            }

            workspace.MakeList.ReplaceArticles(selectedArticles);
            window.Close();
        };


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
        if (DataContext is not ShellViewModel viewModel)
            return;

        var workspace = viewModel.Workspace;

        ListSplitterWindow window = new(
            workspace.Session,
            workspace.Preferences);

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
    /// <summary>
    /// Opens bot processing settings inside the workspace.
    /// </summary>
    public Task OpenBotSettingsAsync()
    {
        MainWorkspaceView.OpenEmbeddedBotSettings();

        return Task.CompletedTask;
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
    /// Asks whether to retry an edit rejected by the wiki's
    /// spam blacklist.
    /// </summary>
    public async Task<bool> ConfirmSpamFilterRetryAsync(
        string blockedUrl)
    {
        var retryButton = new Button
        {
            Content = "Retry",
            MinWidth = 90
        };

        var skipButton = new Button
        {
            Content = "Skip Article",
            MinWidth = 90
        };

        var dialog = new Window
        {
            Title = "Spam Blacklist",
            Width = 440,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 16,
                Children =
            {
                new TextBlock
                {
                    Text =
                        "The wiki rejected this edit because " +
                        "of its spam blacklist.\n\n" +
                        $"Blocked URL: {blockedUrl}\n\n" +
                        "Would you like to retry the edit?",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation =
                        Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment =
                        Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10,
                    Children =
                    {
                        retryButton,
                        skipButton
                    }
                }
            }
            }
        };

        retryButton.Click += (_, _) => dialog.Close(true);
        skipButton.Click += (_, _) => dialog.Close(false);

        return await dialog.ShowDialog<bool>(this);
    }

    /// <summary>
    /// Opens application preferences inside the workspace.
    /// </summary>
    public void OpenPreferences()
    {
        MainWorkspaceView.OpenEmbeddedPreferences();
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
    /// Opens a compatible XML settings file.
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
    /// Saves the current settings file.
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
    /// Twain-compatible XML settings.
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