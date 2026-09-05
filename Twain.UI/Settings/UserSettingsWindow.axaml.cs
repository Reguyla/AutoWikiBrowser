using Avalonia.Interactivity;

namespace Twain.UI.Settings;

/// <summary>
/// Displays and edits user-configurable application settings.
/// </summary>
public partial class UserSettingsWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsWindow"/> class.
    /// </summary>
    public UserSettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Accepts the current settings and closes the window.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Discards changes and closes the window.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}