using Avalonia.Interactivity;

namespace Twain.UI.Talk;

/// <summary>
/// Notifies the user that new talk-page messages are available.
/// </summary>
public partial class TalkMessageWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes the talk-message window.
    /// </summary>
    public TalkMessageWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Closes the dialog and indicates that the user chose to view the messages.
    /// </summary>
    private void ViewButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }
}