using Avalonia.Controls;
using Twain.Core.ReplaceSpecial;

namespace Twain.UI.ReplaceSpecial;

/// <summary>
/// Provides the Avalonia editor used to configure a
/// <see cref="TemplateParamRule"/>.
/// </summary>
public partial class TemplateParamRuleControl : UserControl
{
    private readonly IRuleControlOwner? _owner;
    private bool _loadingRule;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="TemplateParamRuleControl"/> class.
    /// </summary>
    public TemplateParamRuleControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="TemplateParamRuleControl"/> class for the specified rule.
    /// </summary>
    /// <param name="rule">
    /// The rule edited by this control.
    /// </param>
    public TemplateParamRuleControl(
        TemplateParamRule rule)
        : this(
            rule,
            null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="TemplateParamRuleControl"/> class for the specified rule
    /// and owner.
    /// </summary>
    /// <param name="rule">
    /// The rule edited by this control.
    /// </param>
    /// <param name="owner">
    /// The owner that receives notifications when the rule name changes.
    /// </param>
    public TemplateParamRuleControl(
        TemplateParamRule rule,
        IRuleControlOwner? owner)
        : this()
    {
        ArgumentNullException.ThrowIfNull(rule);

        _owner = owner;

        RestoreFromRule(rule);
    }

    /// <summary>
    /// Sets the name displayed in the rule name text box.
    /// </summary>
    /// <param name="name">
    /// The rule name to display.
    /// </param>
    public void SetName(
        string name)
    {
        NameTextBox.Text =
            name;
    }

    /// <summary>
    /// Selects all text in the rule name text box.
    /// </summary>
    public void SelectName()
    {
        NameTextBox.Focus();
        NameTextBox.SelectAll();
    }

    /// <summary>
    /// Saves the values currently displayed by the control to the specified
    /// rule.
    /// </summary>
    /// <param name="rule">
    /// The rule to update.
    /// </param>
    public void SaveToRule(
        TemplateParamRule rule)
    {
        if (rule is null)
        {
            return;
        }

        rule.enabled_ =
            EnabledCheckBox.IsChecked == true;

        rule.Name =
            NameTextBox.Text?.Trim() ??
            string.Empty;

        rule.ParamName =
            ParamNameTextBox.Text?.Trim() ??
            string.Empty;

        rule.NewParamName =
            ChangeNameToTextBox.Text?.Trim() ??
            string.Empty;
    }

    /// <summary>
    /// Restores the specified rule values to the editor.
    /// </summary>
    /// <param name="rule">
    /// The rule whose values should be displayed.
    /// </param>
    public void RestoreFromRule(
        TemplateParamRule rule)
    {
        if (rule is null)
        {
            return;
        }

        _loadingRule = true;

        try
        {
            EnabledCheckBox.IsChecked =
                rule.enabled_;

            NameTextBox.Text =
                rule.Name;

            ParamNameTextBox.Text =
                rule.ParamName;

            ChangeNameToTextBox.Text =
                rule.NewParamName;
        }
        finally
        {
            _loadingRule = false;
        }
    }

    /// <summary>
    /// Notifies the control owner when the rule name changes.
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
}