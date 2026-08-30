using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Twain.UI.Disambiguation;

/// <summary>
/// Displays the interactive disambiguation editor.
/// </summary>
public partial class DabWindow : Window
{
    public DabWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the panel containing the dynamically generated
    /// disambiguation entries.
    /// </summary>
    internal StackPanel ItemsPanel => DisambiguationItemsPanel;

    /// <summary>
    /// Raised when the user requests the current page.
    /// </summary>
    public event EventHandler? PageRequested;

    /// <summary>
    /// Raised when the user requests that all changes be undone.
    /// </summary>
    public event EventHandler? UndoAllRequested;

    /// <summary>
    /// Raised when the user requests that all selections be reset.
    /// </summary>
    public event EventHandler? ResetAllRequested;

    private void PageButton_Click(object? sender, RoutedEventArgs e)
    {
        PageRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UndoAllButton_Click(object? sender, RoutedEventArgs e)
    {
        UndoAllRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ResetAllButton_Click(object? sender, RoutedEventArgs e)
    {
        ResetAllRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AbortButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(DabDialogResult.Abort);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(DabDialogResult.Cancel);
    }

    private void DoneButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(DabDialogResult.Done);
    }
}