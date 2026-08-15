using Twain.Core.Updates;
using Velopack;
using Velopack.Sources;

namespace Twain.Desktop.Updates;

/// <summary>
/// Provides Twain application update operations using Velopack and GitHub Releases.
/// </summary>
internal sealed class VelopackUpdateService : IUpdateService
{
    private const string RepositoryUrl =
        "https://github.com/Reguyla/AutoWikiBrowser";

    private readonly UpdateManager _updateManager;

    private UpdateInfo? _availableUpdate;

    /// <summary>
    /// Initializes the Velopack-backed Twain update service.
    /// </summary>
    public VelopackUpdateService()
    {
        var source = new GithubSource(
            RepositoryUrl,
            null,
            false);

        _updateManager = new UpdateManager(source);
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult?> CheckForUpdatesAsync(
        CancellationToken cancellationToken = default)
    {
        _availableUpdate =
            await _updateManager.CheckForUpdatesAsync();

        if (_availableUpdate == null)
        {
            return null;
        }

        return new UpdateCheckResult(
            new Version(_availableUpdate.TargetFullRelease.Version.ToString()),
            _availableUpdate.TargetFullRelease.NotesMarkdown);
    }

    /// <inheritdoc />
    public async Task DownloadUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        if (_availableUpdate == null)
        {
            throw new InvalidOperationException(
                "No update is available to download. Check for updates first.");
        }

        await _updateManager.DownloadUpdatesAsync(_availableUpdate);
    }

    /// <inheritdoc />
    public void ApplyUpdateAndRestart()
    {
        if (_availableUpdate == null)
        {
            throw new InvalidOperationException(
                "No update is available to apply. Check for updates first.");
        }

        _updateManager.ApplyUpdatesAndRestart(_availableUpdate);
    }
}