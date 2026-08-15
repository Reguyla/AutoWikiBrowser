using System.Threading;

namespace Twain.Core.Updates;

/// <summary>
/// Describes an available Twain application update.
/// </summary>
/// <param name="Version">
/// Version of the available update.
/// </param>
/// <param name="ReleaseNotes">
/// Optional release notes associated with the update.
/// </param>
public sealed record UpdateCheckResult(
    Version Version,
    string? ReleaseNotes);

/// <summary>
/// Defines application update operations used by Twain.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks whether a newer Twain release is available.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the update check.
    /// </param>
    /// <returns>
    /// Information about the available update, or <see langword="null"/> if
    /// no update is available.
    /// </returns>
    Task<UpdateCheckResult?> CheckForUpdatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the update found by the most recent update check.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the download.
    /// </param>
    Task DownloadUpdateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the downloaded update and restarts Twain.
    /// </summary>
    void ApplyUpdateAndRestart();
}