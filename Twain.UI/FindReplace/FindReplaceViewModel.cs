using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CoreFindReplace = Twain.Core.Parse.FindReplace;
using CoreReplacement = Twain.Core.Parse.Replacement;


namespace Twain.UI.FindReplace;

/// <summary>
/// Provides the state used by the Avalonia find and replace interface.
/// </summary>
public partial class FindReplaceViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindReplaceViewModel"/> class.
    /// </summary>
    public FindReplaceViewModel()
    {
    }

    /// <summary>
    /// Gets the editable find and replace rules.
    /// </summary>
    public ObservableCollection<FindReplaceRowViewModel> Rows { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected find and replace rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveRuleUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveRuleDownCommand))]
    private FindReplaceRowViewModel? _selectedRow;

    /// <summary>
    /// Gets or sets whether links should be ignored during replacement processing.
    /// </summary>
    [ObservableProperty]
    private bool _ignoreLinks;

    /// <summary>
    /// Gets or sets whether additional protected article content should be ignored
    /// during replacement processing.
    /// </summary>
    [ObservableProperty]
    private bool _ignoreMore;

    /// <summary>
    /// Gets or sets whether find and replace changes should be appended to the
    /// edit summary.
    /// </summary>
    [ObservableProperty]
    private bool _appendToSummary = true;

    /// <summary>
    /// Gets whether the Ignore Links option can be changed independently.
    /// </summary>
    public bool CanChangeIgnoreLinks =>
        !IgnoreMore;

    /// <summary>
    /// Adds a new empty find and replace rule.
    /// </summary>
    [RelayCommand]
    private void AddRule()
    {
        FindReplaceRowViewModel row = new();

        Rows.Add(row);
        SelectedRow = row;

        UpdateRuleCommandStates();
    }

    /// <summary>
    /// Removes the currently selected find and replace rule.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemoveRule))]
    private void RemoveRule()
    {
        if (SelectedRow is null)
        {
            return;
        }

        Rows.Remove(SelectedRow);
        SelectedRow = null;

        UpdateRuleCommandStates();
    }

    /// <summary>
    /// Updates dependent ignore options when Ignore More changes.
    /// </summary>
    partial void OnIgnoreMoreChanged(
        bool value)
    {
        if (value)
        {
            IgnoreLinks = true;
        }

        OnPropertyChanged(
            nameof(CanChangeIgnoreLinks));
    }

    /// <summary>
    /// Determines whether a find and replace rule can be removed.
    /// </summary>
    private bool CanRemoveRule()
    {
        return SelectedRow is not null;
    }

    /// <summary>
    /// Moves the currently selected find and replace rule up one position.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveRuleUp))]
    private void MoveRuleUp()
    {
        if (SelectedRow is null)
        {
            return;
        }

        int index = Rows.IndexOf(SelectedRow);

        if (index <= 0)
        {
            return;
        }

        Rows.Move(index, index - 1);

        UpdateRuleCommandStates();
    }

    /// <summary>
    /// Moves the currently selected find and replace rule down one position.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveRuleDown))]
    private void MoveRuleDown()
    {
        if (SelectedRow is null)
        {
            return;
        }

        int index = Rows.IndexOf(SelectedRow);

        if (index < 0 ||
            index >= Rows.Count - 1)
        {
            return;
        }

        Rows.Move(index, index + 1);

        UpdateRuleCommandStates();
    }

    /// <summary>
    /// Clears all find and replace rules.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanClearRules))]
    private void ClearRules()
    {
        Rows.Clear();
        SelectedRow = null;

        UpdateRuleCommandStates();
    }

    /// <summary>
    /// Determines whether any find and replace rules can be cleared.
    /// </summary>
    private bool CanClearRules()
    {
        return Rows.Count > 0;
    }

    /// <summary>
    /// Determines whether the selected rule can be moved down.
    /// </summary>
    private bool CanMoveRuleDown()
    {
        if (SelectedRow is null)
        {
            return false;
        }

        int index = Rows.IndexOf(SelectedRow);

        return index >= 0 &&
               index < Rows.Count - 1;
    }

    /// <summary>
    /// Determines whether the selected rule can be moved up.
    /// </summary>
    private bool CanMoveRuleUp()
    {
        return SelectedRow is not null &&
               Rows.IndexOf(SelectedRow) > 0;
    }

    /// <summary>
    /// Loads find and replace rules into the editable collection.
    /// </summary>
    /// <param name="rows">
    /// The Core editor rows to load.
    /// </param>
    public void LoadRows(
        IEnumerable<CoreFindReplace.EditorRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        Rows.Clear();

        foreach (CoreFindReplace.EditorRow row in rows)
        {
            Rows.Add(
                FindReplaceRowViewModel.FromEditorRow(row));
        }

        SelectedRow = null;
        UpdateRuleCommandStates();
    }

    /// <summary>
    /// Gets the current editable rules as Core editor rows.
    /// </summary>
    /// <returns>
    /// The current find and replace rules.
    /// </returns>
    public List<CoreFindReplace.EditorRow> GetRows()
    {
        return Rows
            .Select(row => row.ToEditorRow())
            .ToList();
    }

    /// <summary>
    /// Loads replacement rules into the editable collection.
    /// </summary>
    /// <param name="replacements">
    /// The replacement rules to load.
    /// </param>
    public void LoadReplacements(
        IEnumerable<CoreReplacement> replacements)
    {
        ArgumentNullException.ThrowIfNull(replacements);

        LoadRows(
            replacements.Select(
                replacement =>
                    CoreFindReplace.CreateEditorRow(
                        replacement,
                        false)));
    }

    /// <summary>
    /// Gets the current editable rules as replacement models.
    /// </summary>
    /// <returns>
    /// The current replacement rules.
    /// </returns>
    public List<CoreReplacement> GetReplacements()
    {
        return CoreFindReplace.CreateReplacements(
            GetRows());
    }

    /// <summary>
    /// Refreshes commands whose availability depends on the current rule collection
    /// or selected rule.
    /// </summary>
    private void UpdateRuleCommandStates()
    {
        RemoveRuleCommand.NotifyCanExecuteChanged();
        MoveRuleUpCommand.NotifyCanExecuteChanged();
        MoveRuleDownCommand.NotifyCanExecuteChanged();
        ClearRulesCommand.NotifyCanExecuteChanged();
    }
}