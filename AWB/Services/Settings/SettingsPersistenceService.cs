using Twain.Core;
using Twain.Core.AWBSettings;

namespace AutoWikiBrowser.Services.Settings;

/// <summary>
/// Saves application settings and manages recovery from an existing settings
/// file backup.
/// </summary>
internal sealed class SettingsPersistenceService
{
    private const string BackupExtension = ".old";

    /// <summary>
    /// Saves the specified preferences to a settings file.
    /// </summary>
    /// <param name="preferences">
    /// The application preferences to save.
    /// </param>
    /// <param name="path">
    /// The destination settings file.
    /// </param>
    /// <returns>
    /// A result describing whether the settings were saved successfully and,
    /// when unsuccessful, the type of failure that occurred.
    /// </returns>
    /// <remarks>
    /// When the destination file already exists, it is copied to a temporary
    /// <c>.old</c> backup before the new settings are written. The backup is
    /// deleted after a successful save and restored after eligible failures.

    internal SettingsSaveResult Save(
        UserPrefs preferences,
        string path)
    {
        string backupPath = path + BackupExtension;

        try
        {
            CreateBackup(path, backupPath);

            UserPrefs.SavePrefs(
                preferences,
                path);

            DeleteBackup(backupPath);

            return new SettingsSaveResult(
                SettingsSaveFailure.None);
        }
        catch (Exception ex)
        {
            RestoreBackupWhenAppropriate(
                path,
                backupPath,
                ex);

            return new SettingsSaveResult(
                ClassifyFailure(ex),
                ex);
        }
    }

    // TODO: Track whether the current save operation successfully created the
    // backup so an unrelated stale .old file is not restored if backup creation
    // itself fails.
    /// <summary>
    /// Creates a backup of an existing settings file before it is replaced.
    /// </summary>
    /// <param name="path">
    /// The settings file that may be replaced.
    /// </param>
    /// <param name="backupPath">
    /// The destination path for the backup.
    /// </param>
    private static void CreateBackup(
        string path,
        string backupPath)
    {
        if (File.Exists(path))
        {
            File.Copy(
                path,
                backupPath,
                overwrite: true);
        }
    }

    /// <summary>
    /// Deletes the temporary backup after a successful settings save.
    /// </summary>
    /// <param name="backupPath">
    /// The path of the backup file.
    /// </param>
    private static void DeleteBackup(string backupPath)
    {
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }
    }

    /// <summary>
    /// Restores the previous settings file when the save failure permits a
    /// recovery attempt.
    /// </summary>
    /// <param name="path">
    /// The destination settings file.
    /// </param>
    /// <param name="backupPath">
    /// The path of the previous settings file backup.
    /// </param>
    /// <param name="exception">
    /// The exception raised while saving the settings.
    /// </param>
    /// <remarks>
    /// The existing behavior avoids writing additional data when the original
    /// failure was an input/output error, such as a full disk.
    /// </remarks>
    private static void RestoreBackupWhenAppropriate(
        string path,
        string backupPath,
        Exception exception)
    {
        if (exception is IOException ||
            !File.Exists(backupPath))
        {
            return;
        }

        File.Copy(
            backupPath,
            path,
            overwrite: true);
    }

    /// <summary>
    /// Converts a save exception into the corresponding settings failure
    /// category.
    /// </summary>
    /// <param name="exception">
    /// The exception raised while saving the settings.
    /// </param>
    /// <returns>
    /// The matching settings save failure category.
    /// </returns>
    private static SettingsSaveFailure ClassifyFailure(
        Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException =>
                SettingsSaveFailure.UnauthorizedAccess,

            IOException =>
                SettingsSaveFailure.IoError,

            _ =>
                SettingsSaveFailure.Unexpected
        };
    }

    /// <summary>
    /// Checks whether the current per-user application configuration can be
    /// opened and deletes the configuration file when it is corrupt.
    /// </summary>
    /// <remarks>
    /// A corrupt user configuration file can prevent the application from
    /// starting. When a <see cref="ConfigurationErrorsException"/> identifies
    /// an existing settings file, the file is deleted so that .NET can recreate
    /// it with default values.
    /// </remarks>
    public static void RemoveCorruptUserSettings()
    {
        try
        {
            ConfigurationManager.OpenExeConfiguration(
                ConfigurationUserLevel.PerUserRoamingAndLocal);
        }
        catch (ConfigurationErrorsException ex)
        {
            string settingsFilePath =
                ex.Filename;

            if (string.IsNullOrEmpty(settingsFilePath) &&
                ex.InnerException is ConfigurationErrorsException innerException)
            {
                settingsFilePath =
                    innerException.Filename;
            }

            if (string.IsNullOrEmpty(settingsFilePath) ||
                !File.Exists(settingsFilePath))
            {
                return;
            }

            FileInfo settingsFile =
                new(settingsFilePath);

            if (settingsFile.Directory == null)
            {
                return;
            }

            using FileSystemWatcher watcher = new(
                settingsFile.Directory.FullName,
                settingsFile.Name);

            Tools.WriteDebug(
                $"Deleting corrupt settings file {settingsFilePath}",
                ex.Message);

            File.Delete(settingsFilePath);

            if (File.Exists(settingsFilePath))
            {
                watcher.WaitForChanged(
                    WatcherChangeTypes.Deleted);
            }
        }
    }
}