/*
AWB Profiles
Copyright (C) 2008 Sam Reed, Stephen Kennedy

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

namespace Twain.Core.Profiles;

/// <summary>
/// Represents a saved user profile.
/// </summary>
public class Profile
{
    /// <summary>
    /// Gets or sets the profile identifier.
    /// </summary>
    public int ID { get; set; } = -1;

    /// <summary>
    /// Gets or sets the username associated with the profile.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the saved password associated with the profile.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default settings profile associated with the account.
    /// </summary>
    public string DefaultSettings { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notes associated with the profile.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets whether the profile has a saved password.
    /// </summary>
    public bool HasSavedPassword =>
        !string.IsNullOrEmpty(Password);
}