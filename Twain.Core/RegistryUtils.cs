/*
Copyright (C) 2008 Stephen Kennedy, Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using Microsoft.Win32;

namespace Twain.Core;

/// <summary>
/// Provides access to the local computer registry below
/// HKEY_CURRENT_USER\Software\AutoWikiBrowser only.
/// </summary>
/// <remarks>Clients should implement their own error handling.</remarks>
public static class RegistryUtils
{
    private const string KeyPrefix = "Software\\AutoWikiBrowser\\";

    /// <summary>
    /// Gets a string value from an AWB registry subkey.
    /// </summary>
    /// <param name="keyNameSuffix">
    /// The registry subkey and value name below the AWB registry area.
    /// </param>
    /// <param name="defaultValue">
    /// The value returned when the requested registry value does not exist.
    /// </param>
    /// <returns>The registry value converted to a string.</returns>
    public static string GetValue(
        string keyNameSuffix,
        object defaultValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyNameSuffix);

        int separatorIndex = keyNameSuffix.LastIndexOf('\\');

        if (separatorIndex < 0)
        {
            throw new ArgumentException(
                "The registry key suffix must include a value name.",
                nameof(keyNameSuffix));
        }

        string subKeyName = keyNameSuffix[..separatorIndex];
        string valueName = keyNameSuffix[(separatorIndex + 1)..];

        using var registryKey =
            Registry.CurrentUser.OpenSubKey(BuildKeyName(subKeyName));

        object value = registryKey?.GetValue(valueName, defaultValue);

        return value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Writes a string value to an AWB registry subkey.
    /// </summary>
    /// <param name="keyNameSuffix">The registry subkey below the AWB registry area.</param>
    /// <param name="valueName">The name of the registry value.</param>
    /// <param name="value">The value to write.</param>
    public static void SetValue(
        string keyNameSuffix,
        string valueName,
        string value)
    {
        using var registryKey = GetWritableKey(keyNameSuffix);
        registryKey.SetValue(valueName, value);
    }

    /// <summary>
    /// Opens or creates a writable registry key in the AWB registry area.
    /// </summary>
    /// <param name="keyNameSuffix">The registry subkey below the AWB registry area.</param>
    /// <returns>The writable registry key.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned registry key.
    /// </remarks>
    public static RegistryKey GetWritableKey(string keyNameSuffix)
    {
        // CreateSubKey creates a new subkey or opens an existing key
        // with write access.
        return Registry.CurrentUser.CreateSubKey(
            BuildKeyName(keyNameSuffix));
    }

    /// <summary>
    /// Opens a read-only registry key in the AWB registry area.
    /// </summary>
    /// <param name="keyNameSuffix">The registry subkey below the AWB registry area.</param>
    /// <returns>The registry key, or null if it does not exist.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned registry key.
    /// </remarks>
    public static RegistryKey OpenSubKey(string keyNameSuffix) =>
        Registry.CurrentUser.OpenSubKey(
            BuildKeyName(keyNameSuffix));

    /// <summary>
    /// Deletes a registry subkey.
    /// </summary>
    /// <param name="keyNameSuffix">The registry subkey below the AWB registry area.</param>
    /// <param name="throwOnMissingSubKey">
    /// Whether to throw an exception when the subkey does not exist.
    /// </param>
    public static void DeleteSubKey(
        string keyNameSuffix,
        bool throwOnMissingSubKey) =>
        Registry.CurrentUser.DeleteSubKey(
            BuildKeyName(keyNameSuffix),
            throwOnMissingSubKey);

    /// <summary>
    /// Deletes a registry subkey.
    /// </summary>
    /// <param name="keyNameSuffix">The registry subkey below the AWB registry area.</param>
    public static void DeleteSubKey(string keyNameSuffix) =>
        Registry.CurrentUser.DeleteSubKey(
            BuildKeyName(keyNameSuffix));

    private static string BuildKeyName(string keyNameSuffix) =>
        KeyPrefix + keyNameSuffix;
}