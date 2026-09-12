using Avalonia.Interactivity;

namespace Twain.UI.Controls;

public partial class ArticleActionValidationWindow : Avalonia.Controls.Window
{
    public ArticleActionValidationWindow()
    {
        InitializeComponent();
    }

    public ArticleActionValidationWindow(
        string title,
        string message)
        : this()
    {
        Title = title;
        MessageTextBlock.Text = message;
    }

    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}