/*
AWB Profiles
Copyright (C) 2008 Sam Reed

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

using System.Windows.Forms;

namespace Twain.Core.Profiles;

/// <summary>
/// Provides the user interface for creating or editing a saved account
/// profile.
/// </summary>
public partial class AWBProfileAdd : Form
{
    private readonly int Editid;

    /// <summary>
    /// Initializes a new instance of the <see cref="AWBProfileAdd"/> class
    /// for creating a profile.
    /// </summary>
    public AWBProfileAdd()
    {
        InitializeComponent();

        Editid = -1;
        Text = "Add New Profile";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AWBProfileAdd"/> class
    /// for editing an existing profile.
    /// </summary>
    /// <param name="profile">
    /// The profile whose values should be displayed for editing.
    /// </param>
    public AWBProfileAdd(AWBProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        InitializeComponent();

        Editid = profile.ID;

        txtUsername.Text = profile.Username;
        txtPassword.Text = profile.Password;
        txtPath.Text = profile.DefaultSettings;
        txtNotes.Text = profile.Notes;

        chkDefaultSettings.Checked =
            !string.IsNullOrEmpty(txtPath.Text);

        chkSavePassword.Checked =
            !string.IsNullOrEmpty(txtPassword.Text);

        Text = "Edit Profile";
    }

    /// <summary>
    /// Updates the availability of the password field when password saving is
    /// enabled or disabled.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void chkSavePassword_CheckedChanged(
        object sender,
        EventArgs e)
    {
        txtPassword.Enabled = chkSavePassword.Checked;
    }

    /// <summary>
    /// Initializes the default settings file dialog when the form loads.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void AWBProfileAdd_Load(object sender, EventArgs e)
    {
        openDefaultFile.InitialDirectory =
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);
    }

    /// <summary>
    /// Updates the availability of the default settings path controls.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void chkDefaultSettings_CheckedChanged(
        object sender,
        EventArgs e)
    {
        bool enabled = chkDefaultSettings.Checked;

        txtPath.Enabled = enabled;
        btnBrowse.Enabled = enabled;
    }

    /// <summary>
    /// Opens a file dialog for selecting the profile's default settings file.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void btnBrowse_Click(object sender, EventArgs e)
    {
        if (!chkDefaultSettings.Checked)
        {
            return;
        }

        if (openDefaultFile.ShowDialog(this) == DialogResult.OK)
        {
            txtPath.Text = openDefaultFile.FileName;
        }
    }

    /// <summary>
    /// Validates the entered profile details and saves the profile.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            MessageBox.Show(
                this,
                "The username cannot be blank.",
                "Username required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        // Warn when a new profile uses a username that is already assigned
        // to another saved profile.
        if (Editid == -1 &&
            AWBProfiles.GetProfile(txtUsername.Text) is not null)
        {
            DialogResult result = MessageBox.Show(
                this,
                $"Username \"{txtUsername.Text}\" is already used by another " +
                "profile. Are you sure you want to use this username again?",
                "Username already used",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return;
            }
        }

        AWBProfile profile = new()
        {
            ID = Editid,
            Username = txtUsername.Text,
            Password =
                chkSavePassword.Checked &&
                !string.IsNullOrEmpty(txtPassword.Text)
                    ? txtPassword.Text
                    : string.Empty,
            DefaultSettings = txtPath.Text,
            Notes = txtNotes.Text
        };

        AWBProfiles.AddEditProfile(profile);

        DialogResult = DialogResult.Yes;
    }
}