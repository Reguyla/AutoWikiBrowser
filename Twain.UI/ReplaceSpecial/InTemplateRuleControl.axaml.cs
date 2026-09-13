using Avalonia.Controls;
using System.Collections.Generic;
using System.Linq;
using Twain.Core.ReplaceSpecial;

namespace Twain.UI.ReplaceSpecial;

/// <summary>
/// Provides the Avalonia editor used to configure an
/// <see cref="InTemplateRule"/>.
/// </summary>
public partial class InTemplateRuleControl : UserControl
{
    private InTemplateRule? _rule;
    private bool _loadingRule;
    private readonly IRuleControlOwner? _owner;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InTemplateRuleControl"/> class.
    /// </summary>
    public InTemplateRuleControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InTemplateRuleControl"/> class for the specified rule.
    /// </summary>
    /// <param name="rule">
    /// The rule edited by this control.
    /// </param>
    public InTemplateRuleControl(
        InTemplateRule rule)
        : this(
            rule,
            null)
    {
    }

    /// <summary>
    /// Selects the complete rule name when the name text box is double-clicked.
    /// </summary>
    private void NameTextBox_DoubleTapped(
        object? sender,
        Avalonia.Input.TappedEventArgs e)
    {
        NameTextBox.SelectAll();
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InTemplateRuleControl"/> class for the specified rule
    /// and owner.
    /// </summary>
    /// <param name="rule">
    /// The rule edited by this control.
    /// </param>
    /// <param name="owner">
    /// The owner that receives notifications when the rule name changes.
    /// </param>
    public InTemplateRuleControl(
        InTemplateRule rule,
        IRuleControlOwner? owner)
        : this()
    {
        ArgumentNullException.ThrowIfNull(rule);

        _owner = owner;

        LoadRule(rule);
    }

    /// <summary>
    /// Loads the specified rule into the editor.
    /// </summary>
    /// <param name="rule">
    /// The rule to edit.
    /// </param>
    public void LoadRule(
        InTemplateRule rule)
    {
        RestoreFromRule(rule);
    }

    private void ReplaceCheckBox_Changed(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateEnabledStates();
    }

    private void AddButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        string alias =
            AliasTextBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(alias))
        {
            return;
        }

        if (!AliasesListBox.Items.Contains(alias))
        {
            List<string> aliases =
                AliasesListBox.Items
                    .OfType<string>()
                    .Append(alias)
                    .OrderBy(
                        value => value,
                        StringComparer.CurrentCulture)
                    .ToList();

            AliasesListBox.Items.Clear();

            foreach (string value in aliases)
            {
                AliasesListBox.Items.Add(value);
            }
        }

        AliasTextBox.Text =
            string.Empty;

        AliasTextBox.Focus();

        UpdateEnabledStates();
    }

    private void DeleteButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (AliasesListBox.SelectedItem is not string alias)
        {
            return;
        }

        int selectedIndex =
            AliasesListBox.SelectedIndex;

        AliasesListBox.Items.Remove(alias);

        int count =
            AliasesListBox.ItemCount;

        if (count > 0)
        {
            if (selectedIndex >= count)
            {
                selectedIndex =
                    count - 1;
            }

            AliasesListBox.SelectedIndex =
                selectedIndex;
        }

        UpdateEnabledStates();
    }

    private void AliasesListBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        UpdateEnabledStates();
    }

    private void UpdateEnabledStates()
    {
        DeleteButton.IsEnabled =
            AliasesListBox.SelectedItem is not null;

        ReplaceWithTextBox.IsEnabled =
            ReplaceCheckBox.IsChecked == true;
    }

    /// <summary>
    /// Updates the rule name when the displayed name changes.
    /// </summary>
    private void NameTextBox_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (_loadingRule)
        {
            return;
        }

        _owner?.NameChanged(
            NameTextBox.Text?.Trim() ??
            string.Empty);
    }

    /// <summary>
    /// Saves the values currently displayed by the control to the specified rule.
    /// </summary>
    /// <param name="rule">
    /// The rule to update.
    /// </param>
    public void SaveToRule(
        InTemplateRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        rule.enabled_ =
            EnabledCheckBox.IsChecked == true;

        rule.Name =
            NameTextBox.Text?.Trim() ??
            string.Empty;

        rule.ReplaceWith_ =
            ReplaceWithTextBox.Text?.Trim() ??
            string.Empty;

        rule.DoReplace_ =
            ReplaceCheckBox.IsChecked == true;

        rule.TemplateNames_.Clear();

        foreach (object? item in AliasesListBox.Items)
        {
            if (item is string alias)
            {
                rule.TemplateNames_.Add(alias);
            }
        }
    }

    /// <summary>
    /// Restores the specified rule values to the editor.
    /// </summary>
    /// <param name="rule">
    /// The rule whose values should be displayed.
    /// </param>
    public void RestoreFromRule(
        InTemplateRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        _rule = rule;
        _loadingRule = true;

        try
        {
            NameTextBox.Text =
                rule.Name;

            EnabledCheckBox.IsChecked =
                rule.enabled_;

            ReplaceWithTextBox.Text =
                rule.ReplaceWith_;

            ReplaceCheckBox.IsChecked =
                rule.DoReplace_;

            AliasesListBox.Items.Clear();

            foreach (string alias in rule.TemplateNames_.OrderBy(
                         alias => alias,
                         StringComparer.CurrentCulture))
            {
                AliasesListBox.Items.Add(alias);
            }

            UpdateEnabledStates();
        }
        finally
        {
            _loadingRule = false;
        }
    }
}