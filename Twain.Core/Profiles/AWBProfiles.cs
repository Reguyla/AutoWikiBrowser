/*
AWB Profiles
Copyright (C) 2008 Sam Reed, Stephen Kennedy

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
*/

using Microsoft.Win32;
using System.Windows.Forms;
using Twain.Core.Encryption;

namespace Twain.Core.Profiles;

/// <summary>
/// Provides registry-backed storage and retrieval for AWB user profiles.
/// </summary>
public static class AWBProfiles
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

    private static readonly EncryptionUtils _encryptionUtils =
        new(
            LegacyInitializationVector,
            LegacyPassPhrase,
            LegacySalt);

    static AWBProfiles()
    {
        ResetTempPassword();
    }

    /// <summary>
    /// Gets all saved profiles from the registry.
    /// </summary>
    /// <returns>The saved profiles that could be loaded successfully.</returns>
    public static List<AWBProfile> GetProfiles() =>
        GetProfileIDs()
            .Select(GetProfile)
            .Where(profile => profile != null)
            .ToList();

    /// <summary>
    /// Gets the profile with the specified identifier.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>
    /// The requested profile, or <c>null</c> if it could not be loaded.
    /// </returns>
    public static AWBProfile GetProfile(int id)
    {
        AWBProfile profile = new()
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

            DialogResult result = MessageBox.Show(
                "Profile corrupt. Would you like to delete this profile?",
                "Delete corrupt profile?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                DeleteProfile(id);
            }

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

    /// <summary>
    /// Gets a profile by username.
    /// </summary>
    /// <param name="userName">The profile username.</param>
    /// <returns>
    /// The matching profile, or <c>null</c> if no profile matches.
    /// </returns>
    public static AWBProfile GetProfile(string userName)
    {
        ArgumentNullException.ThrowIfNull(userName);

        return GetProfiles().FirstOrDefault(
            profile => string.Equals(
                profile.Username,
                userName,
                StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets the decrypted password for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>The decrypted password.</returns>
    public static string GetPassword(int id) =>
        RegistryGetAndDecryptValue(
            $"{id}\\Pass",
            string.Empty);

    /// <summary>
    /// Gets the decrypted username for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>The decrypted username.</returns>
    public static string GetUsername(int id) =>
        RegistryGetAndDecryptValue(
            $"{id}\\User",
            string.Empty);

    /// <summary>
    /// Sets the password for the specified profile.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <param name="password">The new password.</param>
    public static void SetPassword(
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
    private static void SetProfilePassword(
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

    /// <summary>
    /// Writes a new or modified profile to the registry.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    internal static void AddEditProfile(
        AWBProfile profile)
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
                nameof(AddEditProfile),
                ex.ToString());
        }
    }

    /// <summary>
    /// Gets or sets the username of the last account used by AWB.
    /// </summary>
    internal static string LastUsedAccount
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

    /// <summary>
    /// Gets or sets the temporary password entered by the user.
    /// </summary>
    private static string TempPassword
    {
        get
        {
            try
            {
                return RegistryGetAndDecryptValue(
                    "TempPassword",
                    string.Empty);
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(
                    nameof(TempPassword),
                    ex.ToString());

                return string.Empty;
            }
        }
        set
        {
            try
            {
                using RegistryKey key =
                    RegistryUtils.GetWritableKey(
                        ProfileRegistryString);

                key.SetValue(
                    "TempPassword",
                    _encryptionUtils.Encrypt(value));
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(
                    nameof(TempPassword),
                    ex.ToString());
            }
        }
    }

    /// <summary>
    /// Clears the temporary password.
    /// </summary>
    public static void ResetTempPassword()
    {
        TempPassword = string.Empty;
    }

    /// <summary>
    /// Deletes the specified profile from the registry.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    public static void DeleteProfile(int id)
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
    /// <returns>The profile identifiers.</returns>
    private static List<int> GetProfileIDs()
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
    /// <returns>The first available profile identifier.</returns>
    private static int GetFirstFreeID()
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
    private static string RegistryGetValue(
        string suffix,
        object defaultValue) =>
        RegistryUtils.GetValue(
            ProfileRegistryString + suffix,
            defaultValue);

    /// <summary>
    /// Reads and decrypts a profile registry value.
    /// </summary>
    private static string RegistryGetAndDecryptValue(
        string suffix,
        object defaultValue) =>
        _encryptionUtils.RegistryGetValueAndDecrypt(
            ProfileRegistryString + suffix,
            defaultValue);

    /// <summary>
    /// Writes a profile registry value.
    /// </summary>
    private static void RegistrySetValue(
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
    private static RegistryKey RegistryGetWritableKey(
        int keyNameSuffix) =>
        RegistryUtils.GetWritableKey(
            ProfileRegistryString + keyNameSuffix);
}