using Twain.Core.AWBSettings;

namespace Twain.Core.Profiles;

/// <summary>
/// Provides access to saved user profiles.
/// </summary>
public static class ProfileManager
{
    private static readonly RegistryProfileStore _profileStore =
        new();

    /// <summary>
    /// Gets all saved profiles.
    /// </summary>
    /// <returns>The saved profiles that could be loaded successfully.</returns>
    public static List<Profile> GetProfiles() =>
        _profileStore.GetProfiles().ToList();

    /// <summary>
    /// Gets the profile with the specified identifier.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>
    /// The requested profile, or <see langword="null"/> if it could not be loaded.
    /// </returns>
    public static Profile? GetProfile(int id) =>
        _profileStore.GetProfile(id);

    /// <summary>
    /// Gets a profile by username.
    /// </summary>
    /// <param name="userName">The profile username.</param>
    /// <returns>
    /// The matching profile, or <see langword="null"/> if no profile matches.
    /// </returns>
    public static Profile? GetProfile(string userName) =>
        _profileStore.GetProfile(userName);

    /// <summary>
    /// Sets the password for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <param name="password">The new password.</param>
    public static void SetPassword(
        int id,
        string password) =>
        _profileStore.SetPassword(
            id,
            password);

    /// <summary>
    /// Saves a new or modified profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    public static void SaveProfile(Profile profile) =>
        _profileStore.SaveProfile(profile);

    /// <summary>
    /// Gets or sets the username of the last-used account.
    /// </summary>
    internal static string LastUsedAccount
    {
        get => _profileStore.LastUsedAccount;
        set => _profileStore.LastUsedAccount = value;
    }

    /// <summary>
    /// Deletes the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    public static void DeleteProfile(int id) =>
        _profileStore.DeleteProfile(id);

    /// <summary>
    /// Gets the username for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>The profile username.</returns>
    public static string GetUsername(int id) =>
        _profileStore.GetUsername(id);

    /// <summary>
    /// Gets the password for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>The profile password.</returns>
    public static string GetPassword(int id) =>
        _profileStore.GetPassword(id);

    /// <summary>
    /// Applies the wiki connection settings associated with the specified profile.
    /// </summary>
    /// <param name="session">
    /// The wiki session to update after applying the profile settings.
    /// </param>
    /// <param name="profile">
    /// The profile whose default settings should be applied.
    /// </param>
    public static void ApplyDefaultSettings(
        Session session,
        Profile profile)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(profile.DefaultSettings))
            return;

        UserPrefs preferences =
            UserPrefs.LoadPrefs(profile.DefaultSettings);

        Variables.SetProject(
            preferences.LanguageCode,
            preferences.Project,
            preferences.CustomProject,
            preferences.Protocol);

        Variables.LoginDomain =
            preferences.LoginDomain;

        session.UpdateProject(true);
    }
}