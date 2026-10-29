using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.AWBSettings;
using Twain.Core.Updates;
using Twain.UI.Settings;
using Twain.UI.ViewModels.Workspaces;

namespace Twain.UI.Shell;

/// <summary>
/// Provides presentation state for the Twain application shell.
/// </summary>
public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly IUpdateService? _updateService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel"/> class.
    /// </summary>
    /// <param name="updateService">
    /// Optional service used to check for, download, and apply Twain updates.
    /// </param>
    public ShellViewModel(
        IUpdateService? updateService = null,
        ApplicationAppearanceSettings? appearanceSettings = null)
    {
        _updateService = updateService;

        AppearanceSettings =
           appearanceSettings ?? new ApplicationAppearanceSettings();

        IsToolbarExpanded =
           AppearanceSettings.ShowProcessingToolbar;

        Session = new Session();

        Session.LoggedOff += _ =>
        {
            Dispatcher.UIThread.Post(LoginCompleted);
        };

        Preferences = new UserPrefs();

        Workspace =
            new WorkspaceViewModel(
                Session,
                Preferences);

        // Restore the saved processing toolbar display mode.
        Workspace.Options.ToolbarDisplayMode =
            AppearanceSettings.ToolbarDisplayMode;

        Workspace.Options.IsWikiAuthenticated = IsLoggedIn;
    }

    /// <summary>
    /// Gets the application-wide appearance settings.
    /// </summary>
    public ApplicationAppearanceSettings AppearanceSettings { get; }

    /// <summary>
    /// Updates application state after the active wiki session
    /// has successfully authenticated.
    /// </summary>
    public void LoginCompleted()
    {
        Workspace.BotSettings.CanUseBotMode =
            Session.IsBot || Session.IsSysop;

        Workspace.Options.IsWikiAuthenticated = IsLoggedIn;

        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(LoggedInUsername));
        OnPropertyChanged(nameof(LoginStatusText));
        OnPropertyChanged(nameof(ActiveWikiText));
        OnPropertyChanged(nameof(LoginButtonText));
    }

    /// <summary>
    /// Gets whether the active wiki session is authenticated.
    /// </summary>
    public bool IsLoggedIn =>
        Session.User.IsLoggedIn;

    /// <summary>
    /// Gets the username of the authenticated wiki user.
    /// </summary>
    public string LoggedInUsername =>
        IsLoggedIn
            ? Session.User.Name
            : string.Empty;

    /// <summary>
    /// Gets the authentication status displayed in the application shell.
    /// </summary>
    public string LoginStatusText =>
        IsLoggedIn
            ? $"Logged in as {LoggedInUsername}"
            : "Not logged in";

    /// <summary>
    /// Gets the label for the authentication button.
    /// </summary>
    public string LoginButtonText =>
        IsLoggedIn ? "Logout" : "Login";

    /// <summary>
    /// Logs out of the active wiki and refreshes authentication status.
    /// </summary>
    public async Task LogoutAsync()
    {
        if (!IsLoggedIn)
        {
            return;
        }

        var editor = Session.Editor;

        if (editor.IsActive)
        {
            await Task.Run(editor.Abort);
        }

        editor.Logout();

        await Task.Run(editor.Wait);

        if (editor.State !=
            Twain.Core.API.AsyncApiEdit.EditState.Ready)
        {
            throw new InvalidOperationException(
                "The wiki logout operation did not complete successfully.");
        }

        if (Session.User.IsLoggedIn)
        {
            throw new InvalidOperationException(
                "The wiki still reports an authenticated session after logout.");
        }

        LoginCompleted();
    }

    /// <summary>
    /// Gets the wiki associated with the authenticated session.
    /// </summary>
    public string ActiveWikiText
    {
        get
        {
            if (!IsLoggedIn)
            {
                return string.Empty;
            }

            string? wikiUrl = Session.Editor?.URL;

            if (string.IsNullOrWhiteSpace(wikiUrl))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(
                    wikiUrl,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                return uri.Authority;
            }

            return wikiUrl;
        }
    }

    /// <summary>
    /// Gets the active wiki session for the application.
    /// </summary>
    public Session Session { get; }

    /// <summary>
    /// Gets the active workspace.
    /// </summary>
    public WorkspaceViewModel Workspace { get; }

    /// <summary>
    /// Gets the active application preferences.
    /// </summary>
    public UserPrefs Preferences { get; }

    /// <summary>
    /// Gets the path of the currently opened or saved settings file.
    /// </summary>
    public string? CurrentSettingsFile { get; private set; }

    /// <summary>
    /// Loads an AWB-compatible XML settings file into
    /// the existing shared preferences instance.
    /// </summary>
    /// <param name="file">The settings file to load.</param>
    public void LoadSettings(string file)
    {
        UserPrefs loaded = UserPrefs.LoadPrefs(file);

        Preferences.Project = loaded.Project;
        Preferences.LanguageCode = loaded.LanguageCode;
        Preferences.CustomProject = loaded.CustomProject;
        Preferences.Protocol = loaded.Protocol;
        Preferences.LoginDomain = loaded.LoginDomain;

        Preferences.List = loaded.List;
        Preferences.FindAndReplace = loaded.FindAndReplace;
        Preferences.Editprefs = loaded.Editprefs;
        Preferences.General = loaded.General;
        Preferences.SkipOptions = loaded.SkipOptions;
        Preferences.Module = loaded.Module;
        Preferences.ExternalProgram = loaded.ExternalProgram;
        Preferences.Disambiguation = loaded.Disambiguation;
        Preferences.Special = loaded.Special;
        Preferences.Tool = loaded.Tool;
        Preferences.Plugin = loaded.Plugin;

        // Refresh the workspace controls from the loaded preferences.
        Workspace.ApplyLoadedSettings();

        // Apply the loaded wiki configuration to the active session.
        ApplySitePreferences();

        CurrentSettingsFile = file;
    }

    /// <summary>
    /// Saves the current workspace preferences to the active
    /// AWB-compatible XML settings file.
    /// </summary>
    public void SaveSettings()
    {
        if (string.IsNullOrWhiteSpace(CurrentSettingsFile))
        {
            throw new InvalidOperationException(
                "No settings file is currently selected. Use Save As.");
        }

        SaveSettingsAs(CurrentSettingsFile);
    }

    /// <summary>
    /// Saves the current workspace preferences to the specified
    /// AWB-compatible XML settings file.
    /// </summary>
    /// <param name="file">The destination settings file.</param>
    public void SaveSettingsAs(string file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);

        // Copy the current Avalonia workspace settings into UserPrefs.
        Workspace.UpdatePreferencesForSave();

        // Serialize the shared preferences using the existing AWB format.
        UserPrefs.SavePrefs(Preferences, file);

        // Only update the active filename after saving succeeds.
        CurrentSettingsFile = file;
    }

    /// <summary>
    /// Gets or sets whether an application update is currently available.
    /// </summary>
    [ObservableProperty]
    private bool _isUpdateAvailable;

    /// <summary>
    /// Gets or sets the message describing the current update state.
    /// </summary>
    [ObservableProperty]
    private string _updateStatusMessage = string.Empty;

    /// <summary>
    /// Checks the configured release source for a newer Twain version.
    /// </summary>
    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (_updateService == null)
        {
            UpdateStatusMessage =
                "Update checking is not available in this application host.";

            return;
        }

        UpdateStatusMessage = "Checking for updates...";
        IsUpdateAvailable = false;

        try
        {
            UpdateCheckResult? update =
                await _updateService.CheckForUpdatesAsync();

            if (update == null)
            {
                UpdateStatusMessage = "Twain is up to date.";
                return;
            }

            UpdateStatusMessage =
                $"Twain {update.Version} is available.";

            IsUpdateAvailable = true;
        }
        catch (Exception ex)
        {
            UpdateStatusMessage =
                $"Unable to check for updates: {ex.Message}";
        }
    }

    /// <summary>
    /// Downloads the available update, applies it, and restarts Twain.
    /// </summary>
    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        if (_updateService == null || !IsUpdateAvailable)
        {
            return;
        }

        try
        {
            UpdateStatusMessage = "Downloading update...";

            await _updateService.DownloadUpdateAsync();

            UpdateStatusMessage = "Installing update...";

            _updateService.ApplyUpdateAndRestart();
        }
        catch (Exception ex)
        {
            UpdateStatusMessage =
                $"Unable to install the update: {ex.Message}";
        }
    }

    /// <summary>
    /// Applies the active site preferences to the current wiki session.
    /// </summary>
    public void ApplySitePreferences()
    {
        Variables.LoginDomain =
            Preferences.LoginDomain;

        Variables.SetProject(
            Session,
            Preferences.LanguageCode,
            Preferences.Project,
            Preferences.CustomProject,
            Preferences.Protocol);

        Session.UpdateProject(true);

        OnPropertyChanged(nameof(ActiveWikiText));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(LoggedInUsername));
        OnPropertyChanged(nameof(LoginStatusText));
        OnPropertyChanged(nameof(LoginButtonText));
    }

    /// <summary>
    /// Controls the temporary visibility of the six processing buttons.
    /// The Login control is independent of this setting.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToolbarToggleText))]
    private bool _isToolbarExpanded = true;

    /// <summary>
    /// Text for the always-visible toolbar toggle.
    /// </summary>
    public string ToolbarToggleText =>
        IsToolbarExpanded ? "Hide toolbar" : "Show toolbar";

    [RelayCommand]
    private void ToggleToolbar()
    {
        IsToolbarExpanded = !IsToolbarExpanded;
    }
}