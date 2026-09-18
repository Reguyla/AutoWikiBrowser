using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        SaveCurrentRule();

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

    /// <summary>
    /// Copies the currently selected replacement rule to the clipboard.
    /// </summary>
    private async void Copy_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CopySelectedRuleAsync();
    }

    /// <summary>
    /// Copies the currently selected replacement rule to the clipboard.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the rule was copied; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private async Task<bool> CopySelectedRuleAsync()
    {
        if (ViewModel.SelectedRule is null)
            return false;

        SaveCurrentRule();

        IRule? rule =
            ViewModel.GetSelectedRule();

        if (rule is null)
            return false;

        string serializedRule =
            RuleSerialization.Serialize(
                rule);

        var clipboard =
            TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is null)
            return false;

        await clipboard.SetTextAsync(
            serializedRule);

        return true;
    }

    /// <summary>
    /// Expands all rule nodes in the rule tree.
    /// </summary>
    private void ExpandAll_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetAllRuleNodesExpanded(
            true);
    }

    /// <summary>
    /// Collapses all rule nodes in the rule tree.
    /// </summary>
    private void CollapseAll_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetAllRuleNodesExpanded(
            false);
    }

    /// <summary>
    /// Sets the expansion state of all rule nodes.
    /// </summary>
    /// <param name="isExpanded">
    /// Whether the rule nodes should be expanded.
    /// </param>
    private void SetAllRuleNodesExpanded(
        bool isExpanded)
    {
        SetRuleNodesExpanded(
            RulesTreeView,
            isExpanded);
    }

    /// <summary>
    /// Recursively sets the expansion state of realized tree items.
    /// </summary>
    /// <param name="control">
    /// The control whose visual children should be searched.
    /// </param>
    /// <param name="isExpanded">
    /// Whether tree items should be expanded.
    /// </param>
    private static void SetRuleNodesExpanded(
        Control control,
        bool isExpanded)
    {
        foreach (Control child in
                 control.GetVisualChildren()
                     .OfType<Control>())
        {
            if (child is TreeViewItem treeViewItem)
            {
                treeViewItem.IsExpanded =
                    isExpanded;
            }

            SetRuleNodesExpanded(
                child,
                isExpanded);
        }
    }

    /// <summary>
    /// Cuts the currently selected replacement rule to the clipboard.
    /// </summary>
    private async void Cut_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!await CopySelectedRuleAsync())
            return;

        ViewModel.DeleteRule();
    }

    /// <summary>
    /// Pastes a replacement rule from the clipboard.
    /// </summary>
    private async void Paste_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var clipboard =
            TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is null)
            return;

        string? serializedRule =
            await clipboard.TryGetTextAsync();

        if (string.IsNullOrWhiteSpace(
            serializedRule))
        {
            return;
        }

        IRule rule;

        try
        {
            rule =
                RuleSerialization.Deserialize(
                    serializedRule);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        SaveCurrentRule();

        ViewModel.AddPastedRule(
            rule);
    }

    /// <summary>
    /// Closes the Replace Special window.
    /// </summary>
    private void Close_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SaveCurrentRule();
        Close();
    }

}