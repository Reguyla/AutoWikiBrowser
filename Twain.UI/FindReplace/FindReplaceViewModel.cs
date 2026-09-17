using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CoreFindReplace = Twain.Core.Parse.FindReplace;

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
    /// Adds a new empty find and replace rule.
    /// </summary>
    [RelayCommand]
    private void AddRule()
    {
        FindReplaceRowViewModel row = new();

        Rows.Add(row);
        SelectedRow = row;
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

        MoveRuleUpCommand.NotifyCanExecuteChanged();
        MoveRuleDownCommand.NotifyCanExecuteChanged();
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

        MoveRuleUpCommand.NotifyCanExecuteChanged();
        MoveRuleDownCommand.NotifyCanExecuteChanged();
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


}