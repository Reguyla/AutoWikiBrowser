using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Linq;
using System.Threading.Tasks;
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


}