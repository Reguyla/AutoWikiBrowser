using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Twain.UI.ErrorHandling;

/// <summary>
/// Displays a simple user-facing error message.
/// </summary>
public partial class ErrorMessageWindow : Window
{
    /// <summary>
    /// Initializes a new error message window.
    /// </summary>
    public ErrorMessageWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new error message window with the specified title and message.
    /// </summary>
    /// <param name="title">
    /// The title displayed by the window.
    /// </param>
    /// <param name="message">
    /// The error message displayed to the user.
    /// </param>
    public ErrorMessageWindow(
        string title,
        string message)
        : this()
    {
        Title =
            title ?? string.Empty;

        MessageTextBlock.Text =
            message ?? string.Empty;
    }

    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}