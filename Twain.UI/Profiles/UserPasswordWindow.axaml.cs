using Avalonia.Interactivity;

namespace Twain.UI.Profiles;

/// <summary>
/// Prompts the user to enter a password for a saved account profile.
/// </summary>
public partial class UserPasswordWindow : Avalonia.Controls.Window
{
    private string _username = string.Empty;

    /// <summary>
    /// Initializes a new instance of the password dialog.
    /// </summary>
    public UserPasswordWindow()
    {
        InitializeComponent();

        Opened += UserPasswordWindow_Opened;
        UpdatePrompt();
    }

    /// <summary>
    /// Initializes a new instance of the password dialog for the
    /// supplied username.
    /// </summary>
    public UserPasswordWindow(
        string username)
        : this()
    {
        Username = username;
    }

    /// <summary>
    /// Gets or sets the username displayed by the dialog.
    /// </summary>
    public string Username
    {
        get => _username;

        set
        {
            _username =
                value ?? string.Empty;

            UpdatePrompt();
        }
    }

    /// <summary>
    /// Gets the password entered by the user.
    /// </summary>
    public string GetPassword =>
        PasswordTextBox.Text ?? string.Empty;

    private void UpdatePrompt()
    {
        PromptTextBlock.Text =
            string.IsNullOrEmpty(_username)
                ? "Please enter the password for this profile."
                : $"Please enter the password for {_username}.";
    }

    private void UserPasswordWindow_Opened(
        object? sender,
        EventArgs e)
    {
        PasswordTextBox.Focus();
    }

    private void OkButton_Click(
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