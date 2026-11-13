using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Twain.UI.Editor;

public partial class EditToolbarView : UserControl
{
    public event EventHandler<string>? FormattingRequested;

    public EditToolbarView()
    {
        InitializeComponent();

        DataContext = ToolbarState;
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

    private readonly EditToolbarViewModel _toolbarState = new();

    public EditToolbarViewModel ToolbarState => _toolbarState;

    /// <summary>
    /// Updates the Edit Bar button display mode.
    /// </summary>
    public void SetDisplayMode(EditToolbarDisplayMode mode)
    {
        ToolbarState.DisplayMode = mode;
    }

}
