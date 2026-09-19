namespace Twain.Core.Profiles;

/// <summary>
/// Defines persistent storage operations for user profiles.
/// </summary>
public interface IProfileStore
{
    /// <summary>
    /// Gets all saved profiles.
    /// </summary>
    IReadOnlyList<Profile> GetProfiles();

    /// <summary>
    /// Gets the profile with the specified identifier.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>
    /// The requested profile, or <see langword="null"/> if the profile
    /// does not exist or cannot be loaded.
    /// </returns>
    Profile? GetProfile(int id);

    /// <summary>
    /// Gets the profile with the specified username.
    /// </summary>
    /// <param name="username">The username to find.</param>
    /// <returns>
    /// The matching profile, or <see langword="null"/> if no matching
    /// profile exists.
    /// </returns>
    Profile? GetProfile(string username);

    /// <summary>
    /// Saves a new or modified profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    void SaveProfile(Profile profile);

    /// <summary>
    /// Deletes the profile with the specified identifier.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    void DeleteProfile(int id);

    /// <summary>
    /// Sets the password for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <param name="password">The password to store.</param>
    void SetPassword(int id, string password);

    /// <summary>
    /// Gets or sets the identifier or username of the last-used account.
    /// </summary>
    string LastUsedAccount { get; set; }
}