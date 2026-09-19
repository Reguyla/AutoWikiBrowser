using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Linq;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.API;
using Twain.Core.Profiles;

namespace Twain.UI.Profiles;

/// <summary>
/// Displays and manages saved user profiles.
/// </summary>
public partial class ProfilesWindow : Window
{
    public ProfilesWindow()
    {
        InitializeComponent();
        LoadProfiles();
    }

    private readonly Session? _session;

    /// <summary>
    /// Gets the currently selected profile list item.
    /// </summary>
    private ProfileListItem? SelectedProfile =>
        ProfilesDataGrid.SelectedItem as ProfileListItem;

    /// <summary>
    /// Loads the saved profiles into the profile list.
    /// </summary>
    private void LoadProfiles()
    {
        ProfilesDataGrid.ItemsSource =
            ProfileManager
                .GetProfiles()
                .Select(profile => new ProfileListItem
                {
                    ID = profile.ID,
                    Username = profile.Username,
                    PasswordSaved =
                        profile.HasSavedPassword
                            ? "Yes"
                            : "No",
                    DefaultSettings = profile.DefaultSettings,
                    Notes = profile.Notes
                })
                .ToList();
    }

    private void ProfilesDataGrid_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        bool hasSelection =
            ProfilesDataGrid.SelectedItem is ProfileListItem;

        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
        LoginButton.IsEnabled = hasSelection;
    }

    private void CloseButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private async void DeleteButton_Click(
    object? sender,
    RoutedEventArgs e)
    {
        ProfileListItem? selectedProfile = SelectedProfile;

        if (selectedProfile is null)
            return;

        bool confirmed =
            await ConfirmDeleteAsync(selectedProfile.Username);

        if (!confirmed)
            return;

        ProfileManager.DeleteProfile(selectedProfile.ID);

        LoadProfiles();
    }

    private async Task<bool> ConfirmDeleteAsync(
    string username)
    {
        Window confirmationWindow = new()
        {
            Title = "Delete profile?",
            Width = 400,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        bool confirmed = false;

        Button deleteButton = new()
        {
            Content = "Delete",
            MinWidth = 80
        };

        Button cancelButton = new()
        {
            Content = "Cancel",
            MinWidth = 80
        };

        deleteButton.Click += (_, _) =>
        {
            confirmed = true;
            confirmationWindow.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            confirmationWindow.Close();
        };

        confirmationWindow.Content =
            new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 16,
                Children =
                {
                new TextBlock
                {
                    Text =
                        $"Delete the saved profile for \"{username}\"?",
                    TextWrapping = TextWrapping.Wrap
                },

                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment =
                        Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        cancelButton,
                        deleteButton
                    }
                }
                }
            };

        await confirmationWindow.ShowDialog(this);

        return confirmed;
    }

    private async void AddButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ProfileEditorWindow editor = new()
        {
            Title = "Add Profile"
        };

        bool saved =
            await editor.ShowDialog<bool>(this);

        if (!saved)
            return;

        Profile profile = new()
        {
            Username = editor.Username,
            Password = editor.Password,
            DefaultSettings = editor.DefaultSettings,
            Notes = editor.Notes
        };

        ProfileManager.SaveProfile(profile);

        LoadProfiles();
    }

    private async void EditButton_Click(
    object? sender,
    RoutedEventArgs e)
    {
        ProfileListItem? selectedProfile = SelectedProfile;

        if (selectedProfile is null)
            return;

        Profile? profile =
            ProfileManager.GetProfile(selectedProfile.ID);

        if (profile is null)
            return;

        ProfileEditorWindow editor = new(profile)
        {
            Title = "Edit Profile"
        };

        bool saved =
            await editor.ShowDialog<bool>(this);

        if (!saved)
            return;

        profile.Username = editor.Username;
        profile.DefaultSettings = editor.DefaultSettings;
        profile.Notes = editor.Notes;

        if (!string.IsNullOrEmpty(editor.Password))
        {
            profile.Password = editor.Password;
        }

        ProfileManager.SaveProfile(profile);

        LoadProfiles();
    }

    /// <summary>
    /// Initializes the profiles window for the specified wiki session.
    /// </summary>
    /// <param name="session">
    /// The wiki session used for authentication.
    /// </param>
    public ProfilesWindow(Session session)
        : this()
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
    }

    private async void LoginButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_session is null)
            return;

        ProfileListItem? selectedProfile = SelectedProfile;

        if (selectedProfile is null)
            return;

        Profile? profile =
            ProfileManager.GetProfile(selectedProfile.ID);

        if (profile is null)
            return;

        ProfileLoginService.LoginResult result =
            ProfileLoginService.Login(
                _session,
                profile.Username,
                profile.Password,
                Variables.LoginDomain);

        switch (result.Status)
        {
            case ProfileLoginService.LoginStatus.Success:
                Close(true);
                break;

            case ProfileLoginService.LoginStatus.SessionBusy:
            case ProfileLoginService.LoginStatus.UriChanged:
            case ProfileLoginService.LoginStatus.LoginFailed:
                await ShowMessageAsync(
                    result.Title,
                    result.Message);
                break;
        }
    }

    private async Task ShowMessageAsync(
    string title,
    string message)
    {
        Window dialog = new()
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        Button okButton = new()
        {
            Content = "OK",
            MinWidth = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        okButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 16,
            Children =
        {
            new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            },
            okButton
        }
        };

        await dialog.ShowDialog(this);
    }
}