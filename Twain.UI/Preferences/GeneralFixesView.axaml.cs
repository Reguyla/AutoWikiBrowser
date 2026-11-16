
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using Twain.Core.GeneralFixes;

namespace Twain.UI.Preferences.GeneralFixes;

/// <summary>
/// Allows editors to select which general fixes
/// Twain should apply during article processing.
/// </summary>
public partial class GeneralFixesView : UserControl
{
    private readonly Dictionary<GeneralFixId, CheckBox>
        _fixCheckBoxes = new();

    public GeneralFixesView()
    {
        InitializeComponent();
        PopulateFixControls();
    }

    /// <summary>
    /// Creates checkboxes for all registered general fixes.
    /// </summary>
    private void PopulateFixControls()
    {
        _fixCheckBoxes.Clear();

        FormattingFixesPanel.Children.Clear();
        LinksFixesPanel.Children.Clear();
        ReferencesFixesPanel.Children.Clear();
        StructureFixesPanel.Children.Clear();

        foreach (GeneralFixDefinition fix in GeneralFixCatalog.Fixes)
        {

            CheckBox checkBox = new()
            {
                Content = fix.Name,
                IsChecked = fix.EnabledByDefault
            };

            // Display the fix description when hovering.
            ToolTip.SetTip(checkBox, fix.Description);


            _fixCheckBoxes.Add(fix.Id, checkBox);

            StackPanel panel = fix.Category switch
            {
                GeneralFixCategory.DatesAndFormatting =>
                    FormattingFixesPanel,

                GeneralFixCategory.LinksAndTemplates =>
                    LinksFixesPanel,

                GeneralFixCategory.ReferencesAndCitations =>
                    ReferencesFixesPanel,

                GeneralFixCategory.ArticleStructureAndMetadata =>
                    StructureFixesPanel,

                _ => StructureFixesPanel
            };

            panel.Children.Add(checkBox);
        }
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
        foreach (CheckBox checkBox in _fixCheckBoxes.Values)
        {
            checkBox.IsChecked = enabled;
        }
    }

    /// <summary>
    /// Returns the identifiers of all enabled fixes.
    /// </summary>
    public List<GeneralFixId> GetEnabledFixes()
    {
        List<GeneralFixId> enabledFixes = new();

        foreach (KeyValuePair<GeneralFixId, CheckBox> fix
                 in _fixCheckBoxes)
        {
            if (fix.Value.IsChecked == true)
            {
                enabledFixes.Add(fix.Key);
            }
        }

        return enabledFixes;
    }

    /// <summary>
    /// Updates the controls from a collection of enabled fixes.
    /// </summary>
    public void SetEnabledFixes(
        IEnumerable<GeneralFixId> enabledFixes)
    {
        HashSet<GeneralFixId> enabled = new(enabledFixes);

        foreach (KeyValuePair<GeneralFixId, CheckBox> fix
                 in _fixCheckBoxes)
        {
            fix.Value.IsChecked = enabled.Contains(fix.Key);
        }
    }
}
