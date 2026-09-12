using Avalonia.Interactivity;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Twain.UI.Controls;

/// <summary>
/// Displays a simple modal message or confirmation dialog.
/// </summary>
public partial class MessageDialogWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a preview instance of the dialog.
    /// </summary>
    public MessageDialogWindow()
        : this(
            "Message",
            "Message",
            false)
    {
    }

    /// <summary>
    /// Initializes a new message dialog.
    /// </summary>
    public MessageDialogWindow(
        string message,
        string title,
        bool showYesNo)
    {
        InitializeComponent();

        MessageTextBlock.Text = message;
        Title = title;

        YesButton.IsVisible = showYesNo;
        NoButton.IsVisible = showYesNo;
        OkButton.IsVisible = !showYesNo;
    }

    private void YesButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    private void NoButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }
}