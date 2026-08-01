using WikiFunctions.AWBSettings;

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
    /// If a backup file already exists, it is deleted after a successful save.
    /// For failures other than <see cref="IOException"/>, the backup is copied
    /// back over the destination file when available.
    /// </remarks>
    internal SettingsSaveResult Save(
        UserPrefs preferences,
        string path)
    {
        string backupPath = path + BackupExtension;

        try
        {
            UserPrefs.SavePrefs(preferences, path);

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
}