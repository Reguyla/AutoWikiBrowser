
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;

namespace Twain.UI.Preferences.GeneralFixes;

/// <summary>
/// Allows editors to select which general fixes
/// Twain should apply during article processing.
/// </summary>
public partial class GeneralFixesView : UserControl
{
    private readonly List<CheckBox> _fixCheckBoxes = new();

    public GeneralFixesView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Selects every available general fix.
    /// </summary>
    private void SelectAllButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        SetAllFixes(true);
    }

    /// <summary>
    /// Deselects every available general fix.
    /// </summary>
    private void SelectNoneButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        SetAllFixes(false);
    }

    /// <summary>
    /// Updates the checked state of all registered fixes.
    /// </summary>
    private void SetAllFixes(bool enabled)
    {
        foreach (CheckBox checkBox in _fixCheckBoxes)
        {
            checkBox.IsChecked = enabled;
        }
    }
}
