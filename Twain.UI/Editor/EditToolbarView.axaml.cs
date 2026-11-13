
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Twain.UI.Editor;

public partial class EditToolbarView : UserControl
{
    public event EventHandler<string>? FormattingRequested;

    public EditToolbarView()
    {
        InitializeComponent();
    }

    private void FormattingButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not string actionId)
            return;

        FormattingRequested?.Invoke(this, actionId);
    }
}
