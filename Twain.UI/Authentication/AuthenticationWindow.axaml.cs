using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Twain.UI.Authentication;

/// <summary>
/// Prompts the user for HTTP authentication credentials.
/// </summary>
public partial class AuthenticationWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AuthenticationWindow"/> class.
    /// </summary>
    public AuthenticationWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the entered HTTP authentication username.
    /// </summary>
    public string Username => UsernameTextBox.Text ?? string.Empty;

    /// <summary>
    /// Gets the entered HTTP authentication password.
    /// </summary>
    public string Password => PasswordTextBox.Text ?? string.Empty;

    /// <summary>
    /// Closes the dialog after the user submits the credentials.
    /// </summary>
    private void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}