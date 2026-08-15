using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Twain.Core.Updates;
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
    public ShellViewModel(IUpdateService? updateService = null)
    {
        _updateService = updateService;
    }

    /// <summary>
    /// Gets the active workspace.
    /// </summary>
    public WorkspaceViewModel Workspace { get; } = new();

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
}