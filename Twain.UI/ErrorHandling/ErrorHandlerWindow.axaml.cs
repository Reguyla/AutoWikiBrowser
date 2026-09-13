using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Twain.Core;

namespace Twain.UI.ErrorHandling;

public partial class ErrorHandlerWindow : Window
{
    public ErrorHandlerWindow()
    {
        InitializeComponent();
    }

    public ErrorHandlerWindow(
        ErrorHandler.ErrorDialogContent content)
        : this()
    {
        ArgumentNullException.ThrowIfNull(content);

        ErrorTextBox.Text =
            content.Summary;

        SubjectTextBox.Text =
            content.Subject;

        DetailsTextBox.Text =
            content.Details;
    }

    private async void CopyButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        TopLevel? topLevel =
            TopLevel.GetTopLevel(this);

        if (topLevel?.Clipboard is null)
        {
            return;
        }

        await topLevel.Clipboard.SetTextAsync(
            DetailsTextBox.Text ?? string.Empty);
    }

    private void CloseButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}