using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Twain.Core.ReplaceSpecial;

namespace Twain.UI.ReplaceSpecial;

/// <summary>
/// Provides the Avalonia state for the Replace Special rule editor.
/// </summary>
public partial class ReplaceSpecialViewModel : ObservableObject
{
    private readonly ReplaceSpecialHistory _history = new();

    /// <summary>
    /// Gets the top-level rules displayed in the rule tree.
    /// </summary>
    public ObservableCollection<ReplaceSpecialRuleViewModel> Rules { get; } =
        [];

    /// <summary>
    /// Gets or sets the currently selected rule.
    /// </summary>
    [ObservableProperty]
    private ReplaceSpecialRuleViewModel? _selectedRule;

    /// <summary>
    /// Replaces the current rule hierarchy with the supplied rules.
    /// </summary>
    /// <param name="rules">
    /// The top-level Replace Special rules to load.
    /// </param>
    public void LoadRules(
        IEnumerable<IRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        ReplaceRules(rules);

        _history.Clear();

        NotifyHistoryCommands();
    }

    /// <summary>
    /// Replaces the displayed rule hierarchy without changing history.
    /// </summary>
    private void ReplaceRules(
        IEnumerable<IRule> rules)
    {
        Rules.Clear();

        foreach (IRule rule in rules)
        {
            Rules.Add(
                new ReplaceSpecialRuleViewModel(
                    rule));
        }

        SelectedRule = null;
    }

    /// <summary>
    /// Gets whether an earlier rule-tree state is available.
    /// </summary>
    private bool CanUndo()
    {
        return _history.CanUndo;
    }

    /// <summary>
    /// Restores the previous rule-tree state.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void Undo()
    {
        List<IRule>? rules =
            _history.Undo(
                GetRules());

        if (rules is null)
            return;

        ReplaceRules(rules);

        NotifyHistoryCommands();
    }

    /// <summary>
    /// Gets whether a later rule-tree state is available.
    /// </summary>
    private bool CanRedo()
    {
        return _history.CanRedo;
    }

    /// <summary>
    /// Restores the next rule-tree state.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void Redo()
    {
        List<IRule>? rules =
            _history.Redo();

        if (rules is null)
            return;

        ReplaceRules(rules);

        NotifyHistoryCommands();
    }

    /// <summary>
    /// Updates commands whose availability depends on rule history.
    /// </summary>
    private void NotifyHistoryCommands()
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Gets the current top-level Replace Special rules.
    /// </summary>
    /// <returns>
    /// The rules in their current tree order.
    /// </returns>
    public List<IRule> GetRules()
    {
        List<IRule> rules = [];

        foreach (ReplaceSpecialRuleViewModel ruleViewModel in Rules)
        {
            SynchronizeChildren(ruleViewModel);

            rules.Add(ruleViewModel.Rule);
        }

        return rules;
    }

    /// <summary>
    /// Gets the currently selected replacement rule with its child hierarchy
    /// synchronized to the Avalonia rule tree.
    /// </summary>
    /// <returns>
    /// The selected rule, or <see langword="null"/> when no rule is selected.
    /// </returns>
    public IRule? GetSelectedRule()
    {
        if (SelectedRule is null)
            return null;

        SynchronizeChildren(
            SelectedRule);

        return SelectedRule.Rule;
    }

    /// <summary>
    /// Adds a deserialized replacement rule to the rule tree.
    /// </summary>
    /// <param name="rule">
    /// The replacement rule to add.
    /// </param>
    public void AddPastedRule(
        IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        _history.Save(
            GetRules());

        NotifyHistoryCommands();

        ReplaceSpecialRuleViewModel ruleViewModel =
            new(rule);

        if (SelectedRule is not null &&
            TryGetContainingCollection(
                SelectedRule,
                out ObservableCollection<ReplaceSpecialRuleViewModel>? collection))
        {
            int index =
                collection.IndexOf(
                    SelectedRule);

            collection.Insert(
                index + 1,
                ruleViewModel);
        }
        else
        {
            Rules.Add(
                ruleViewModel);
        }

        SelectedRule =
            ruleViewModel;
    }

    /// <summary>
    /// Creates a new top-level find and replace rule.
    /// </summary>
    [RelayCommand]
    public void AddRule()
    {
        AddTopLevelRule(
            new Rule());
    }

    /// <summary>
    /// Adds an existing replacement rule to the top-level rule collection.
    /// </summary>
    /// <param name="rule">
    /// The replacement rule to add.
    /// </param>
    public void AddRule(
        IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        AddTopLevelRule(rule);
    }

    /// <summary>
    /// Creates a new top-level in-template rule.
    /// </summary>
    [RelayCommand]
    public void AddInTemplateRule()
    {
        AddTopLevelRule(
            new InTemplateRule());
    }

    /// <summary>
    /// Creates a new top-level template-parameter rule.
    /// </summary>
    [RelayCommand]
    public void AddTemplateParamRule()
    {
        AddTopLevelRule(
            new TemplateParamRule());
    }

    /// <summary>
    /// Gets whether a new subrule can be added to the current selection.
    /// </summary>
    private bool CanAddSubrule()
    {
        return SelectedRule is not null;
    }

    /// <summary>
    /// Gets whether the currently selected rule can be deleted.
    /// </summary>
    private bool CanDeleteRule()
    {
        return SelectedRule is not null;
    }

    /// <summary>
    /// Deletes the currently selected rule from the rule hierarchy.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteRule))]
    public void DeleteRule()
    {
        if (SelectedRule is null)
            return;

        ReplaceSpecialRuleViewModel ruleToDelete =
            SelectedRule;

        _history.Save(
            GetRules());

        NotifyHistoryCommands();

        if (Rules.Remove(ruleToDelete))
        {
            SelectedRule = null;
            return;
        }

        if (RemoveChildRule(
            Rules,
            ruleToDelete))
        {
            SelectedRule = null;
        }
    }

    /// <summary>
    /// Removes the specified rule from a child-rule collection.
    /// </summary>
    private static bool RemoveChildRule(
        IEnumerable<ReplaceSpecialRuleViewModel> rules,
        ReplaceSpecialRuleViewModel ruleToRemove)
    {
        foreach (ReplaceSpecialRuleViewModel rule in rules)
        {
            if (rule.Children.Remove(ruleToRemove))
                return true;

            if (RemoveChildRule(
                rule.Children,
                ruleToRemove))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the collection that directly contains the specified rule.
    /// </summary>
    private bool TryGetContainingCollection(
        ReplaceSpecialRuleViewModel ruleToFind,
        out ObservableCollection<ReplaceSpecialRuleViewModel>? collection)
    {
        if (Rules.Contains(ruleToFind))
        {
            collection = Rules;
            return true;
        }

        return TryGetContainingCollection(
            Rules,
            ruleToFind,
            out collection);
    }

    /// <summary>
    /// Recursively searches child-rule collections for the specified rule.
    /// </summary>
    private static bool TryGetContainingCollection(
        IEnumerable<ReplaceSpecialRuleViewModel> rules,
        ReplaceSpecialRuleViewModel ruleToFind,
        out ObservableCollection<ReplaceSpecialRuleViewModel>? collection)
    {
        foreach (ReplaceSpecialRuleViewModel rule in rules)
        {
            if (rule.Children.Contains(ruleToFind))
            {
                collection = rule.Children;
                return true;
            }

            if (TryGetContainingCollection(
                rule.Children,
                ruleToFind,
                out collection))
            {
                return true;
            }
        }

        collection = null;
        return false;
    }

    /// <summary>
    /// Creates a new find and replace rule beneath the currently selected rule.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddSubrule))]
    public void AddSubrule()
    {
        AddChildRule(
            new Rule());
    }

    /// <summary>
    /// Creates a new in-template rule beneath the currently selected rule.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddSubrule))]
    public void AddInTemplateSubrule()
    {
        AddChildRule(
            new InTemplateRule());
    }

    /// <summary>
    /// Creates a new template-parameter rule beneath the currently selected rule.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddSubrule))]
    public void AddTemplateParamSubrule()
    {
        AddChildRule(
            new TemplateParamRule());
    }

    /// <summary>
    /// Moves the currently selected rule one position upward.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveRuleUp))]
    public void MoveRuleUp()
    {
        if (SelectedRule is null)
            return;

        if (!TryGetContainingCollection(
            SelectedRule,
            out ObservableCollection<ReplaceSpecialRuleViewModel>? collection))
        {
            return;
        }

        int index = collection.IndexOf(SelectedRule);

        if (index <= 0)
            return;

        _history.Save(
            GetRules());

        NotifyHistoryCommands();

        collection.Move(
            index,
            index - 1);

        MoveRuleUpCommand.NotifyCanExecuteChanged();
        MoveRuleDownCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Moves the currently selected rule one position downward.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveRuleDown))]
    public void MoveRuleDown()
    {
        if (SelectedRule is null)
            return;

        if (!TryGetContainingCollection(
            SelectedRule,
            out ObservableCollection<ReplaceSpecialRuleViewModel>? collection))
        {
            return;
        }

        int index = collection.IndexOf(SelectedRule);

        if (index < 0 ||
            index >= collection.Count - 1)
        {
            return;
        }

        _history.Save(
           GetRules());

        NotifyHistoryCommands();

        collection.Move(
            index,
            index + 1);

        MoveRuleUpCommand.NotifyCanExecuteChanged();
        MoveRuleDownCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Adds a rule to the top-level rule collection and selects it.
    /// </summary>
    private void AddTopLevelRule(
        IRule rule)
    {
        _history.Save(
            GetRules());

        NotifyHistoryCommands();

        ReplaceSpecialRuleViewModel ruleViewModel =
            new(rule);

        Rules.Add(ruleViewModel);

        SelectedRule = ruleViewModel;
    }

    /// <summary>
    /// Adds a rule beneath the currently selected rule and selects it.
    /// </summary>
    private void AddChildRule(
        IRule rule)
    {
        if (SelectedRule is null)
            return;

        _history.Save(
            GetRules());


        ReplaceSpecialRuleViewModel ruleViewModel =
            new(rule);

        SelectedRule.Children.Add(ruleViewModel);

        SelectedRule = ruleViewModel;
    }

    /// <summary>
    /// Synchronizes the underlying rule hierarchy with the Avalonia tree.
    /// </summary>
    private static void SynchronizeChildren(
        ReplaceSpecialRuleViewModel ruleViewModel)
    {
        if (ruleViewModel.Children.Count == 0)
        {
            ruleViewModel.Rule.Children = null;
            return;
        }

        List<IRule> children = [];

        foreach (ReplaceSpecialRuleViewModel childViewModel
                 in ruleViewModel.Children)
        {
            SynchronizeChildren(childViewModel);

            children.Add(childViewModel.Rule);
        }

        ruleViewModel.Rule.Children = children;
    }

    /// <summary>
    /// Updates commands whose availability depends on the selected rule.
    /// </summary>
    partial void OnSelectedRuleChanged(
        ReplaceSpecialRuleViewModel? value)
    {
        AddSubruleCommand.NotifyCanExecuteChanged();
        AddInTemplateSubruleCommand.NotifyCanExecuteChanged();
        AddTemplateParamSubruleCommand.NotifyCanExecuteChanged();
        DeleteRuleCommand.NotifyCanExecuteChanged();
        MoveRuleUpCommand.NotifyCanExecuteChanged();
        MoveRuleDownCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Gets whether the currently selected rule can be moved up.
    /// </summary>
    private bool CanMoveRuleUp()
    {
        if (SelectedRule is null)
            return false;

        if (!TryGetContainingCollection(
            SelectedRule,
            out ObservableCollection<ReplaceSpecialRuleViewModel>? collection))
        {
            return false;
        }

        return collection.IndexOf(SelectedRule) > 0;
    }

    /// <summary>
    /// Gets whether the currently selected rule can be moved down.
    /// </summary>
    private bool CanMoveRuleDown()
    {
        if (SelectedRule is null)
            return false;

        if (!TryGetContainingCollection(
            SelectedRule,
            out ObservableCollection<ReplaceSpecialRuleViewModel>? collection))
        {
            return false;
        }

        int index = collection.IndexOf(SelectedRule);

        return index >= 0 &&
               index < collection.Count - 1;
    }
}