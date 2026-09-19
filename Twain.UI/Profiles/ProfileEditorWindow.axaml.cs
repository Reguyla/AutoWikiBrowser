using Avalonia.Controls;
using Avalonia.Interactivity;
using Twain.Core;
using Twain.Core.Profiles;

namespace Twain.UI.Profiles;

/// <summary>
/// Collects the values used to create or edit a saved profile.
/// </summary>
public partial class ProfileEditorWindow : Window
{
    public ProfileEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the window with an existing profile.
    /// </summary>
    /// <param name="profile">The profile to edit.</param>
    public ProfileEditorWindow(Profile profile)
        : this()
    {
        ArgumentNullException.ThrowIfNull(profile);

        UsernameTextBox.Text = profile.Username;
        DefaultSettingsTextBox.Text = profile.DefaultSettings;
        NotesTextBox.Text = profile.Notes;
    }

    public string Username =>
        UsernameTextBox.Text ?? string.Empty;

    public string Password =>
        PasswordTextBox.Text ?? string.Empty;

    public string DefaultSettings =>
        DefaultSettingsTextBox.Text ?? string.Empty;

    public string Notes =>
        NotesTextBox.Text ?? string.Empty;

    private void SaveButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

}