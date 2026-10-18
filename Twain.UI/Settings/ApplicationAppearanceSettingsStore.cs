
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Twain.UI.Settings;

/// <summary>
/// Loads and saves application-wide appearance settings.
/// </summary>
public static class ApplicationAppearanceSettingsStore
{
    private static readonly string SettingsFilePath =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Twain",
            "appearance.json");

    /// <summary>
    /// Loads appearance settings, returning defaults if
    /// no valid settings file is available.
    /// </summary>
    public static ApplicationAppearanceSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new ApplicationAppearanceSettings();
            }

            string json = File.ReadAllText(SettingsFilePath);

            return JsonSerializer.Deserialize<ApplicationAppearanceSettings>(
                json) ?? new ApplicationAppearanceSettings();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Unable to load appearance settings: {ex}");

            return new ApplicationAppearanceSettings();
        }
    }

    /// <summary>
    /// Saves the supplied appearance settings.
    /// </summary>
    public static void Save(ApplicationAppearanceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? directory =
            Path.GetDirectoryName(SettingsFilePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(SettingsFilePath, json);
    }
}
