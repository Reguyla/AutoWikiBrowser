using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twain.Core.Profiles;
using Twain.UI.Controls;

namespace Twain.UI.Profiles;

/// <summary>
/// Provides the user interface for creating or editing a saved account
/// profile.
/// </summary>
public partial class AWBProfileAddWindow : Avalonia.Controls.Window
{
    private readonly int _editId;

    /// <summary>
    /// Initializes a new instance of the profile window for creating a profile.
    /// </summary>
    public AWBProfileAddWindow()
    {
        InitializeComponent();

        _editId = -1;
        Title = "Add New Profile";
    }

    /// <summary>
    /// Initializes a new instance of the profile window for editing an existing
    /// profile.
    /// </summary>
    /// <param name="profile">
    /// The profile whose values should be displayed for editing.
    /// </param>
    public AWBProfileAddWindow(
        AWBProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        InitializeComponent();

        _editId = profile.ID;

        UsernameTextBox.Text =
            profile.Username;

        PasswordTextBox.Text =
            profile.Password;

        DefaultSettingsPathTextBox.Text =
            profile.DefaultSettings;

        NotesTextBox.Text =
            profile.Notes;

        SavePasswordCheckBox.IsChecked =
            !string.IsNullOrEmpty(
                profile.Password);

        DefaultSettingsCheckBox.IsChecked =
            !string.IsNullOrEmpty(
                profile.DefaultSettings);

        UpdatePasswordState();
        UpdateDefaultSettingsState();

        Title = "Edit Profile";
    }

    private void SavePasswordCheckBox_Click(
        object? sender,
        RoutedEventArgs e)
    {
        UpdatePasswordState();
    }

    private void DefaultSettingsCheckBox_Click(
        object? sender,
        RoutedEventArgs e)
    {
        UpdateDefaultSettingsState();
    }

    private void UpdatePasswordState()
    {
        PasswordTextBox.IsEnabled =
            SavePasswordCheckBox.IsChecked == true;
    }

    private void UpdateDefaultSettingsState()
    {
        bool enabled =
            DefaultSettingsCheckBox.IsChecked == true;

        DefaultSettingsPathTextBox.IsEnabled =
            enabled;

        BrowseButton.IsEnabled =
            enabled;
    }

    private async void BrowseButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DefaultSettingsCheckBox.IsChecked != true)
            return;

        IReadOnlyList<IStorageFile> files =
            await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Select default settings file",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType(
                            "XML settings files")
                        {
                            Patterns = ["*.xml"]
                        },
                        FilePickerFileTypes.All
                    ]
                });

        if (files.Count == 0)
            return;

        string? path =
            files[0].TryGetLocalPath();

        if (!string.IsNullOrEmpty(path))
        {
            DefaultSettingsPathTextBox.Text =
                path;
        }
    }

    private async void SaveButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string username =
            UsernameTextBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            await ShowMessageAsync(
                "The username cannot be blank.",
                "Username required");

            return;
        }

        if (_editId == -1 &&
            AWBProfiles.GetProfile(username) is not null)
        {
            bool useDuplicate =
                await ConfirmDuplicateUsernameAsync(
                    username);

            if (!useDuplicate)
                return;
        }

        AWBProfile profile = new()
        {
            ID = _editId,
            Username = username,
            Password =
                SavePasswordCheckBox.IsChecked == true &&
                !string.IsNullOrEmpty(
                    PasswordTextBox.Text)
                    ? PasswordTextBox.Text
                    : string.Empty,
            DefaultSettings =
                DefaultSettingsPathTextBox.Text ??
                string.Empty,
            Notes =
                NotesTextBox.Text ??
                string.Empty
        };

        AWBProfiles.SaveProfile(
            profile);

        Close(true);
    }

    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    private async Task<bool> ConfirmDuplicateUsernameAsync(
        string username)
    {
        MessageDialogWindow dialog =
            new(
                $"Username \"{username}\" is already used by another profile. " +
                "Are you sure you want to use this username again?",
                "Username already used",
                true);

        bool result =
            await dialog.ShowDialog<bool>(this);

        return result;
    }

    private async Task ShowMessageAsync(
        string message,
        string title)
    {
        MessageDialogWindow dialog =
            new(
                message,
                title,
                false);

        await dialog.ShowDialog<bool>(this);
    }
}