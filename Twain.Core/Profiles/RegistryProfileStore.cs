using Microsoft.Win32;
using Twain.Core.Encryption;

namespace Twain.Core.Profiles;

/// <summary>
/// Provides access to legacy AWB profiles stored in the Windows Registry.
/// </summary>
public sealed class RegistryProfileStore : IProfileStore
{
    private const string ProfileRegistryString = "Profiles\\";

    // These values are retained solely for compatibility with profiles
    // encrypted by earlier AWB versions. Changing them would prevent
    // existing usernames and passwords from being decrypted.
    private const string LegacyInitializationVector =
        "tnf47bgfdwlp9,.q";

    private const string LegacyPassPhrase =
        "oi frjweopi 4r390%^($%%^$HJKJNMHJGY 2`';'[#";

    private const string LegacySalt =
        "SH1ew yuhn gxe$�$%^y HNKLHWEQ JEW`b";

    private readonly EncryptionUtils _encryptionUtils =
        new(
            LegacyInitializationVector,
            LegacyPassPhrase,
            LegacySalt);

    /// <inheritdoc />
    public IReadOnlyList<Profile> GetProfiles() =>
        GetProfileIDs()
            .Select(GetProfile)
            .Where(profile => profile != null)
            .ToList()!;

    /// <inheritdoc />
    public Profile? GetProfile(int id)
    {
        Profile profile = new()
        {
            ID = id
        };

        try
        {
            profile.Username =
                RegistryGetAndDecryptValue(
                    $"{id}\\User",
                    string.Empty);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(GetProfile),
                ex.ToString());

            return null;
        }

        if (string.IsNullOrEmpty(profile.Username))
            return null;

        try
        {
            profile.Password =
                RegistryGetAndDecryptValue(
                    $"{id}\\Pass",
                    string.Empty);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(GetProfile),
                ex.ToString());

            profile.Password = string.Empty;
        }

        try
        {
            profile.DefaultSettings =
                RegistryGetValue(
                    $"{id}\\Settings",
                    string.Empty);

            profile.Notes =
                RegistryGetValue(
                    $"{id}\\Notes",
                    string.Empty);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(GetProfile),
                ex.ToString());
        }

        return profile;
    }

    /// <inheritdoc />
    public Profile? GetProfile(string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        return GetProfiles().FirstOrDefault(
            profile => string.Equals(
                profile.Username,
                username,
                StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets the decrypted password for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>The decrypted password.</returns>
    public string GetPassword(int id) =>
        RegistryGetAndDecryptValue(
            $"{id}\\Pass",
            string.Empty);

    /// <summary>
    /// Gets the decrypted username for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>The decrypted username.</returns>
    public string GetUsername(int id) =>
        RegistryGetAndDecryptValue(
            $"{id}\\User",
            string.Empty);

    /// <inheritdoc />
    public void SetPassword(
        int id,
        string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        SetProfilePassword(
            id,
            _encryptionUtils.Encrypt(password));
    }

    /// <summary>
    /// Writes an encrypted profile password to the registry.
    /// </summary>
    private void SetProfilePassword(
        int id,
        string password)
    {
        try
        {
            RegistrySetValue(
                id,
                "Pass",
                password);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(SetProfilePassword),
                ex.ToString());
        }
    }

    /// <inheritdoc />
    public void SaveProfile(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.ID == -1)
        {
            profile.ID = GetFirstFreeID();
        }

        try
        {
            using RegistryKey key =
                RegistryGetWritableKey(profile.ID);

            key.SetValue(
                "User",
                _encryptionUtils.Encrypt(
                    profile.Username));

            key.SetValue(
                "Pass",
                _encryptionUtils.Encrypt(
                    profile.Password));

            key.SetValue(
                "Settings",
                profile.DefaultSettings);

            key.SetValue(
                "Notes",
                profile.Notes);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(SaveProfile),
                ex.ToString());
        }
    }

    /// <inheritdoc />
    public string LastUsedAccount
    {
        get
        {
            try
            {
                return RegistryUtils.GetValue(
                    ProfileRegistryString +
                    "LastUsedAccount",
                    string.Empty);
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(
                    nameof(LastUsedAccount),
                    ex.ToString());

                return string.Empty;
            }
        }
        set
        {
            try
            {
                RegistryUtils.SetValue(
                    ProfileRegistryString,
                    "LastUsedAccount",
                    value);
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(
                    nameof(LastUsedAccount),
                    ex.ToString());
            }
        }
    }

    /// <inheritdoc />
    public void DeleteProfile(int id)
    {
        try
        {
            RegistryUtils.DeleteSubKey(
                ProfileRegistryString + id);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(DeleteProfile),
                ex.ToString());
        }
    }

    /// <summary>
    /// Gets all numeric profile identifiers stored in the registry.
    /// </summary>
    private List<int> GetProfileIDs()
    {
        List<int> profileIds = new();

        try
        {
            using RegistryKey key =
                RegistryUtils.OpenSubKey(
                    ProfileRegistryString);

            if (key is null)
                return profileIds;

            foreach (string subKeyName in
                     key.GetSubKeyNames())
            {
                if (int.TryParse(
                        subKeyName,
                        out int profileId))
                {
                    profileIds.Add(profileId);
                }
            }
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(GetProfileIDs),
                ex.ToString());
        }

        return profileIds;
    }

    /// <summary>
    /// Gets the first unused positive profile identifier.
    /// </summary>
    private int GetFirstFreeID()
    {
        HashSet<int> profileIds =
            GetProfileIDs().ToHashSet();

        int candidateId = 1;

        while (profileIds.Contains(candidateId))
        {
            candidateId++;
        }

        return candidateId;
    }

    /// <summary>
    /// Reads a profile registry value.
    /// </summary>
    private string RegistryGetValue(
        string suffix,
        object defaultValue) =>
        RegistryUtils.GetValue(
            ProfileRegistryString + suffix,
            defaultValue);

    /// <summary>
    /// Reads and decrypts a profile registry value.
    /// </summary>
    private string RegistryGetAndDecryptValue(
        string suffix,
        object defaultValue) =>
        _encryptionUtils.RegistryGetValueAndDecrypt(
            ProfileRegistryString + suffix,
            defaultValue);

    /// <summary>
    /// Writes a profile registry value.
    /// </summary>
    private void RegistrySetValue(
        int keyNameSuffix,
        string valueName,
        string value) =>
        RegistryUtils.SetValue(
            ProfileRegistryString + keyNameSuffix,
            valueName,
            value);

    /// <summary>
    /// Opens or creates a writable profile registry key.
    /// The caller is responsible for disposing the returned key.
    /// </summary>
    private RegistryKey RegistryGetWritableKey(
        int keyNameSuffix) =>
        RegistryUtils.GetWritableKey(
            ProfileRegistryString + keyNameSuffix);
}