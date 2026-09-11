using Avalonia.Interactivity;

namespace Twain.UI.Controls;

/// <summary>
/// Prompts the user to apply or discard changes made in the regex tester.
/// </summary>
public partial class RegexApplyChangesWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new apply-changes dialog.
    /// </summary>
    public RegexApplyChangesWindow()
    {
        InitializeComponent();
    }

    private void ApplyButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    private void DiscardButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(null);
    }
}