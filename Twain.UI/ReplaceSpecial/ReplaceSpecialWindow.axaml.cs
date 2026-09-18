using Avalonia.Controls;
using System.Collections.Generic;
using Twain.Core.ReplaceSpecial;

namespace Twain.UI.ReplaceSpecial;

/// <summary>
/// Avalonia replacement for the Replace Special rule editor.
/// </summary>
public partial class ReplaceSpecialWindow :
    Window,
    IRuleControlOwner
{
    private IRule? _currentRule;
    private Control? _currentRuleControl;

    /// <summary>
    /// Initializes a new instance of the Replace Special window.
    /// </summary>
    public ReplaceSpecialWindow()
    {
        InitializeComponent();

        ViewModel =
            new ReplaceSpecialViewModel();

        DataContext = ViewModel;

        ViewModel.PropertyChanged +=
            ViewModel_PropertyChanged;
    }

    /// <summary>
    /// Gets the Replace Special view model used by this window.
    /// </summary>
    public ReplaceSpecialViewModel ViewModel { get; }

    /// <summary>
    /// Loads the supplied Replace Special rules into the editor.
    /// </summary>
    public void LoadRules(
        IEnumerable<IRule> rules)
    {
        ViewModel.LoadRules(rules);
    }

    /// <summary>
    /// Gets the rules currently configured in the editor.
    /// </summary>
    public List<IRule> GetRules()
    {
        return ViewModel.GetRules();
    }

    /// <summary>
    /// Updates the selected rule name when its editor changes the name.
    /// </summary>
    /// <param name="name">
    /// The updated rule name.
    /// </param>
    public void NameChanged(
        string name)
    {
        if (ViewModel.SelectedRule is null)
            return;

        ViewModel.SelectedRule.Name =
            name;
    }

    /// <summary>
    /// Responds to changes in the Replace Special view model.
    /// </summary>
    private void ViewModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(ReplaceSpecialViewModel.SelectedRule))
        {
            ShowSelectedRule();
        }
    }

    /// <summary>
    /// Saves the values displayed by the current rule editor.
    /// </summary>
    private void SaveCurrentRule()
    {
        switch (_currentRuleControl)
        {
            case RuleControl control
                when _currentRule is Rule rule:

                control.SaveToRule(rule);
                break;

            case InTemplateRuleControl control
                when _currentRule is InTemplateRule rule:

                control.SaveToRule(rule);
                break;

            case TemplateParamRuleControl control
                when _currentRule is TemplateParamRule rule:

                control.SaveToRule(rule);
                break;
        }
    }

    /// <summary>
    /// Saves the current editor and displays the editor for the selected rule.
    /// </summary>
    private void ShowSelectedRule()
    {
        SaveCurrentRule();

        RuleEditorHost.Children.Clear();

        _currentRule =
            ViewModel.SelectedRule?.Rule;

        _currentRuleControl =
            _currentRule switch
            {
                Rule rule =>
                    new RuleControl(
                        rule,
                        this),

                InTemplateRule rule =>
                    new InTemplateRuleControl(
                        rule,
                        this),

                TemplateParamRule rule =>
                    new TemplateParamRuleControl(
                        rule,
                        this),

                _ => null
            };

        if (_currentRuleControl is null)
        {
            NoRuleSelectedText.IsVisible = true;
            RuleEditorHost.Children.Add(
                NoRuleSelectedText);

            return;
        }

        NoRuleSelectedText.IsVisible = false;

        RuleEditorHost.Children.Add(
            _currentRuleControl);
    }
}