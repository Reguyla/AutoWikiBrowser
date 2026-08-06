/*
AWB Profiles
Copyright (C) 2007 Sam Reed

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

using System.ComponentModel;
using System.Windows.Forms;

namespace Twain.Core.Profiles;

/// <summary>
/// Prompts the user to enter a password for a saved account profile.
/// </summary>
/// <remarks>
/// This dialog is used when a profile does not store a password or when the
/// user must supply credentials at runtime.
/// </remarks>
public partial class UserPassword : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPassword"/> class.
    /// </summary>
    public UserPassword()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the username displayed by the dialog.
    /// </summary>
    /// <remarks>
    /// This write-only property updates the text of the internal label at
    /// runtime. It is not a design-time property and must not be serialized
    /// by the Windows Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Username
    {
        set => lblText.Text = string.Format(lblText.Text, value);
    }

    /// <summary>
    /// Gets the password entered by the user.
    /// </summary>
    public string GetPassword => txtPassword.Text;
}