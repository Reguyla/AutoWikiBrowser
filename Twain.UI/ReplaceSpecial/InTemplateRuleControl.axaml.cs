using Avalonia.Controls;
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
        : this()
    {
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
        ArgumentNullException.ThrowIfNull(rule);

        _rule = rule;
        _loadingRule = true;

        try
        {
            NameTextBox.Text =
                rule.Name;

            EnabledCheckBox.IsChecked =
                rule.enabled_;

            AliasesListBox.Items.Clear();

            foreach (string alias in rule.TemplateNames_)
            {
                AliasesListBox.Items.Add(alias);
            }

            ReplaceCheckBox.IsChecked =
                rule.DoReplace_;

            ReplaceWithTextBox.Text =
                rule.ReplaceWith_;

            UpdateEnabledStates();
        }
        finally
        {
            _loadingRule = false;
        }
    }

    private void ReplaceCheckBox_Changed(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_loadingRule ||
            _rule is null)
        {
            return;
        }

        _rule.DoReplace_ =
            ReplaceCheckBox.IsChecked == true;

        UpdateEnabledStates();
    }

    private void ReplaceWithTextBox_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (_loadingRule ||
            _rule is null)
        {
            return;
        }

        _rule.ReplaceWith_ =
            ReplaceWithTextBox.Text?.Trim() ??
            string.Empty;
    }

    private void AddButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_rule is null)
        {
            return;
        }

        string alias =
            AliasTextBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(alias))
        {
            return;
        }

        if (!AliasesListBox.Items.Contains(alias))
        {
            AliasesListBox.Items.Add(alias);
            _rule.TemplateNames_.Add(alias);
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
        if (_rule is null ||
            AliasesListBox.SelectedItem is not string alias)
        {
            return;
        }

        int selectedIndex =
            AliasesListBox.SelectedIndex;

        AliasesListBox.Items.Remove(alias);
        _rule.TemplateNames_.Remove(alias);

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
        if (_loadingRule ||
            _rule is null)
        {
            return;
        }

        _rule.Name =
            NameTextBox.Text?.Trim() ??
            string.Empty;
    }

    /// <summary>
    /// Updates the rule enabled state when the check box changes.
    /// </summary>
    private void EnabledCheckBox_Changed(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_loadingRule ||
            _rule is null)
        {
            return;
        }

        _rule.enabled_ =
            EnabledCheckBox.IsChecked == true;
    }
}