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

namespace WikiFunctions.Encryption;

/// <summary>
/// Provides a friendly wrapper around the RijndaelSimple class.
/// </summary>
public sealed class EncryptionUtils
{
    private readonly string _initializationVector;
    private readonly string _passPhrase;
    private readonly string _salt;

    public EncryptionUtils(
        string initializationVector,
        string passPhrase,
        string salt)
    {
        _initializationVector = initializationVector;
        _passPhrase = passPhrase;
        _salt = salt;
    }

    /// <summary>
    /// Encrypts a string.
    /// </summary>
    /// <param name="text">The string to encrypt.</param>
    /// <returns>
    /// The encrypted string, or the original value if encryption fails.
    /// </returns>
    public string Encrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        try
        {
            return RijndaelSimple.Encrypt(
                text,
                _passPhrase,
                _salt,
                "SHA1",
                2,
                _initializationVector,
                256);
        }
        catch
        {
            // Preserve the existing behavior if encryption fails.
            return text;
        }
    }

    /// <summary>
    /// Decrypts a string.
    /// </summary>
    /// <param name="text">The string to decrypt.</param>
    /// <returns>
    /// The decrypted string, or the original value if decryption fails.
    /// </returns>
    public string Decrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        try
        {
            return RijndaelSimple.Decrypt(
                text,
                _passPhrase,
                _salt,
                "SHA1",
                2,
                _initializationVector,
                256);
        }
        catch
        {
            // Preserve the existing behavior if decryption fails.
            return text;
        }
    }

    /// <summary>
    /// Reads an encrypted registry value and decrypts it.
    /// </summary>
    /// <param name="keyNameSuffix">
    /// The registry subkey and value name below the AWB registry area.
    /// </param>
    /// <param name="defaultValue">
    /// The value returned when the registry value does not exist.
    /// </param>
    /// <returns>The decrypted registry value.</returns>
    public string RegistryGetValueAndDecrypt(
        string keyNameSuffix,
        object defaultValue) =>
        Decrypt(RegistryUtils.GetValue(keyNameSuffix, defaultValue));
}