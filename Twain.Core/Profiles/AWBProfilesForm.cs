/*
AWB Profiles
Copyright (C) 2007 Sam Reed, Stephen Kennedy

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

using System.Windows.Forms;
using Twain.Core.API;

namespace Twain.Core.Profiles;

/// <summary>
/// Provides the user interface for selecting, loading, and logging in with
/// saved AutoWikiBrowser account profiles.
/// </summary>
public partial class AWBProfilesForm : Form
{
    /// <summary>
    /// Stores the name or identifier of the currently selected settings
    /// profile.
    /// </summary>
    protected string CurrentSettingsProfile = string.Empty;

    private readonly Session TheSession;

    /// <summary>
    /// Occurs after the user has logged in successfully.
    /// </summary>
    public event EventHandler? LoggedIn;

    /// <summary>
    /// Occurs when the selected account's default settings must be loaded.
    /// </summary>
    public event EventHandler? UserDefaultSettingsLoadRequired;

    /// <summary>
    /// Initializes a new instance of the <see cref="AWBProfilesForm"/> class.
    /// </summary>
    /// <param name="session">
    /// The session used to authenticate and manage the selected account.
    /// </param>
    public AWBProfilesForm(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        InitializeComponent();

        TheSession = session;

        loginAsThisAccountToolStripMenuItem.Visible = true;
        loginAsThisAccountToolStripMenuItem.Click += lvAccounts_DoubleClick;
        btnLogin.Visible = true;

        UsernameOrPasswordChanged(this, EventArgs.Empty);
    }

    // TODO: Move profile lookup and last-used-account resolution into a
    // UI-independent service so the behavior can be reused by Twain.

    /// <summary>
    /// Loads saved account profiles and restores the most recently used
    /// account selection when the form opens.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void AWBProfiles_Load(object sender, EventArgs e)
    {
        if (!DesignMode)
        {
            LoadProfiles();
        }

        string lastUsedAccount = AWBProfiles.LastUsedAccount;

        if (string.IsNullOrEmpty(lastUsedAccount))
        {
            return;
        }

        // TODO: Store the last-used profile ID and fallback username in separate
        // settings so numeric usernames cannot be mistaken for profile identifiers.
        if (!int.TryParse(lastUsedAccount, out int id))
        {
            txtUsername.Text = lastUsedAccount;
            return;
        }

        AWBProfile? profile = AWBProfiles.GetProfile(id);

        if (profile is null)
        {
            txtUsername.Text = lastUsedAccount;
            return;
        }

        txtUsername.Text =
            id > 0
                ? profile.Username
                : lastUsedAccount;
    }

    /// <summary>
    /// Gets the profile identifier of the first selected account.
    /// </summary>
    /// <value>
    /// The selected profile identifier, or <c>-1</c> if no valid profile is
    /// selected.
    /// </value>
    protected int SelectedItem
    {
        get
        {
            if (lvAccounts.SelectedIndices.Count == 0)
            {
                return -1;
            }

            return int.TryParse(
                lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text,
                out int profileId)
                ? profileId
                : -1;
        }
    }

    /// <summary>
    /// Updates the enabled state of controls that require a selected account.
    /// </summary>
    private void UpdateUI()
    {
        bool accountSelected = lvAccounts.SelectedItems.Count > 0;

        btnLogin.Enabled = accountSelected;
        btnDelete.Enabled = accountSelected;
        BtnEdit.Enabled = accountSelected;
        loginAsThisAccountToolStripMenuItem.Enabled = accountSelected;
        editThisAccountToolStripMenuItem.Enabled = accountSelected;
        changePasswordToolStripMenuItem.Enabled = accountSelected;
        deleteThisAccountToolStripMenuItem.Enabled = accountSelected;
    }

    // TODO: Move profile retrieval and ListView population into a
    // UI-independent presenter or service to simplify future Avalonia migration.
    /// <summary>
    /// Loads the saved account profiles into the account list and refreshes the
    /// associated user interface.
    /// </summary>
    private void LoadProfiles()
    {
        lvAccounts.BeginUpdate();

        try
        {
            lvAccounts.Items.Clear();

            foreach (AWBProfile profile in AWBProfiles.GetProfiles())
            {
                ListViewItem item = new(profile.ID.ToString());
                item.SubItems.Add(profile.Username);
                item.SubItems.Add(
                    !string.IsNullOrEmpty(profile.Password)
                        ? "Yes"
                        : "No");
                item.SubItems.Add(profile.DefaultSettings);
                item.SubItems.Add(profile.Notes);

                lvAccounts.Items.Add(item);
            }

            UpdateUI();
            lvAccounts.ResizeColumns();
        }
        finally
        {
            lvAccounts.EndUpdate();
        }
    }

    /// <summary>
    /// Opens the dialog for adding a saved account.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void btnAdd_Click(object sender, EventArgs e)
    {
        AddProfile();
    }

    /// <summary>
    /// Opens the dialog for adding a saved account from the context menu.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void addNewAccountToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        AddProfile();
    }

    /// <summary>
    /// Displays the account-creation dialog and reloads the saved profiles when
    /// a new account is added successfully.
    /// </summary>
    private void AddProfile()
    {
        using AWBProfileAdd add = new();

        if (add.ShowDialog(this) == DialogResult.Yes)
        {
            LoadProfiles();
        }
    }

    /// <summary>
    /// Deletes the currently selected saved account.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void btnDelete_Click(object sender, EventArgs e)
    {
        Delete();
    }

    /// <summary>
    /// Deletes the currently selected saved account from the context menu.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void deleteThisSavedAccountToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        Delete();
    }

    private void Delete()
    {
        try
        {
            if (SelectedItem < 0)
            {
                return;
            }
            AWBProfiles.DeleteProfile(SelectedItem);
            LoadProfiles();
        }
        finally
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// Opens the password-change dialog for the selected saved account.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void changePasswordToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (!TryGetSelectedProfileId(out int profileId))
        {
            return;
        }

        string username =
            lvAccounts.Items[lvAccounts.SelectedIndices[0]]
                .SubItems[1]
                .Text;

        using UserPassword password = new()
        {
            Username = username
        };

        if (password.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        AWBProfiles.SetPassword(
            profileId,
            password.GetPassword);

        LoadProfiles();
    }

    /// <summary>
    /// Opens the editor for the selected saved account from the context menu.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void editThisAccountToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        Edit();
    }

    /// <summary>
    /// Opens the selected account profile for editing and reloads the profile
    /// list when the changes are saved.
    /// </summary>
    private void Edit()
    {
        if (!TryGetSelectedProfileId(out int profileId))
        {
            return;
        }

        AWBProfile? profile = AWBProfiles.GetProfile(profileId);

        if (profile is null)
        {
            return;
        }

        using AWBProfileAdd add = new(profile);

        if (add.ShowDialog(this) == DialogResult.Yes)
        {
            LoadProfiles();
        }
    }

    /// <summary>
    /// Attempts to retrieve the identifier of the currently selected account
    /// profile.
    /// </summary>
    /// <param name="profileId">
    /// When this method returns successfully, contains the selected profile
    /// identifier.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a selected profile has a valid numeric
    /// identifier; otherwise, <see langword="false"/>.
    /// </returns>
    private bool TryGetSelectedProfileId(out int profileId)
    {
        profileId = 0;

        if (lvAccounts.SelectedIndices.Count == 0)
        {
            return false;
        }

        string profileIdText =
            lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text;

        return int.TryParse(profileIdText, out profileId);
    }

    /// <summary>
    /// Closes the profiles form.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void btnExit_Click(object sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Gets the settings profile selected for loading.
    /// </summary>
    public string SettingsToLoad =>
        CurrentSettingsProfile;

    /// <summary>
    /// Updates the available profile actions when the account selection changes.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void lvAccounts_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateUI();
    }

    /// <summary>
    /// Logs in using the selected saved account when the account is
    /// double-clicked.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    protected virtual void lvAccounts_DoubleClick(object sender, EventArgs e)
    {
        Login();
    }

    /// <summary>
    /// Opens the editor for the selected saved account.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void BtnEdit_Click(object sender, EventArgs e)
    {
        Edit();
    }

    /// <summary>
    /// Logs in to the selected saved account using the specified password.
    /// </summary>
    /// <param name="password">
    /// The password to use for authentication.
    /// </param>
    private void PerformLogin(string password)
    {
        if (lvAccounts.SelectedIndices.Count == 0)
        {
            return;
        }

        string profileIdText =
            lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text;

        if (!int.TryParse(profileIdText, out int profileId))
        {
            return;
        }

        string username = AWBProfiles.GetUsername(profileId);

        PerformLogin(username, password);
    }

    /// <summary>
    /// Attempts to log in using the specified credentials and updates the form
    /// when authentication succeeds.
    /// </summary>
    /// <param name="username">
    /// The username to use for authentication.
    /// </param>
    /// <param name="password">
    /// The password to use for authentication.
    /// </param>
    private void PerformLogin(string username, string password)
    {
        if (TheSession.IsBusy)
        {
            MessageBox.Show(
                this,
                "Cannot log in because the session is busy.\r\n\r\n" +
                "Please wait for the current page-saving operation to complete.",
                "Session busy",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        bool loginSucceeded = false;

        try
        {
            // TODO: Review after the AsyncApiEdit migration. This profile-login
            // path remains synchronous because it depends on local login exception
            // handling and the LoginDomain overload.
            TheSession.Editor.SynchronousEditor.Login(
                username,
                password,
                Variables.LoginDomain);

            loginSucceeded = true;
        }
        catch (UriChangedException ex)
        {
            // TODO: Offer to change the configured protocol to match the response
            // URI scheme and retry the login after user confirmation.
            MessageBox.Show(
                this,
                ex.Message,
                ex.Header,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (LoginException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Login failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }

        if (loginSucceeded)
        {
            LoggedIn?.Invoke(this, EventArgs.Empty);
        }

        // Do not close when login was initiated through the /u command-line
        // argument and the form was never displayed to the user.
        if (TheSession.User.IsLoggedIn && Visible)
        {
            Close();
        }
    }

    /// <summary>
    /// Attempts to log in using the currently selected account.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void btnLogin_Click(object sender, EventArgs e)
    {
        Login();
    }

    /// <summary>
    /// Logs in using the account currently selected in the saved profiles list.
    /// </summary>
    /// <remarks>
    /// When the selected profile specifies default settings, those settings are
    /// loaded before authentication so the session uses the profile's configured
    /// wiki and project.
    /// </remarks>
    private void Login()
    {
        int selectedProfileId = SelectedItem;

        if (selectedProfileId < 0)
        {
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;

            ListViewItem item =
                lvAccounts.Items[lvAccounts.SelectedIndices[0]];

            CurrentSettingsProfile =
                string.IsNullOrEmpty(item.SubItems[3].Text)
                    ? string.Empty
                    : item.SubItems[3].Text;

            // Load the profile settings before login so authentication uses the
            // wiki and project configured for the selected account.
            if (CurrentSettingsProfile.Length > 0)
            {
                UserDefaultSettingsLoadRequired?.Invoke(
                    this,
                    EventArgs.Empty);
            }

            if (item.SubItems[2].Text == "Yes")
            {
                string password =
                    AWBProfiles.GetPassword(selectedProfileId);

                PerformLogin(password);
            }
            else
            {
                using UserPassword password = new()
                {
                    Username = item.SubItems[1].Text
                };

                if (password.ShowDialog(this) == DialogResult.OK)
                {
                    PerformLogin(password.GetPassword);
                }
            }

            AWBProfiles.LastUsedAccount = item.Text;
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    // TODO: Replace the ambiguous ID-or-name startup argument with an explicit
    // profile identifier format so numeric profile names are handled reliably.
    /// <summary>
    /// Logs in using a saved profile identified by its numeric profile ID or
    /// profile name.
    /// </summary>
    /// <param name="profileIdOrName">
    /// The saved profile ID or profile name supplied through the application
    /// startup arguments.
    /// </param>
    public void Login(string profileIdOrName)
    {
        if (string.IsNullOrEmpty(profileIdOrName))
        {
            return;
        }

        try
        {
            AWBProfile? startupProfile;

            if (int.TryParse(profileIdOrName, out int profileId))
            {
                startupProfile = AWBProfiles.GetProfile(profileId);
            }
            else
            {
                startupProfile = AWBProfiles.GetProfile(profileIdOrName);
            }

            if (startupProfile is null)
            {
                MessageBox.Show(
                    this,
                    $"Cannot find user profile '{profileIdOrName}'.",
                    "Command-line error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);

                return;
            }

            if (!string.IsNullOrEmpty(startupProfile.Password))
            {
                PerformLogin(
                    startupProfile.Username,
                    startupProfile.Password);

                return;
            }

            using UserPassword password = new()
            {
                Username = startupProfile.Username
            };

            if (password.ShowDialog(this) == DialogResult.OK)
            {
                PerformLogin(
                    startupProfile.Username,
                    password.GetPassword);
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Updates the availability of the quick login button when the username or
    /// password changes.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void UsernameOrPasswordChanged(object sender, EventArgs e)
    {
        btnQuickLogin.Enabled =
            txtUsername.TextLength > 0 &&
            txtPassword.TextLength > 0;
    }

    /// <summary>
    /// Initiates a quick login when the Enter key is pressed in the password
    /// field.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The keyboard event data.
    /// </param>
    private void txtPassword_KeyUp(object sender, KeyEventArgs e)
    {
        if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return) &&
            btnQuickLogin.Enabled)
        {
            btnQuickLogin.PerformClick();
        }
    }

    // TODO: Move profile creation and persistence into a profile service so the
    // form only gathers user input and displays results.
    /// <summary>
    /// Logs in using the supplied credentials and optionally saves the account as
    /// a new profile.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void btnQuickLogin_Click(object sender, EventArgs e)
    {
        string user = txtUsername.Text;
        string password = txtPassword.Text;

        if (chkSaveProfile.Checked)
        {
            if (AWBProfiles.GetProfile(user) is not null)
            {
                MessageBox.Show(
                    this,
                    $"Username \"{user}\" already exists.",
                    "Username exists",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            AWBProfile profile = new()
            {
                Username = user
            };

            if (chkSavePassword.Checked)
            {
                profile.Password = password;
            }

            AWBProfiles.AddEditProfile(profile);
        }

        AWBProfiles.LastUsedAccount = user;
        PerformLogin(user, password);
    }

    /// <summary>
    /// Enables or disables password saving based on whether the profile will be
    /// saved.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void chkSaveProfile_CheckedChanged(object sender, EventArgs e)
    {
        chkSavePassword.Enabled = chkSaveProfile.Checked;
    }
}