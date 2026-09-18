using System.Collections.Generic;
using System.Linq;
using Twain.Core.ReplaceSpecial;

namespace Twain.UI.ReplaceSpecial;

/// <summary>
/// Maintains undo and redo snapshots for the Avalonia Replace Special editor.
/// </summary>
public sealed class ReplaceSpecialHistory
{
    private readonly List<List<IRule>> _history = [];

    private int _index = -1;

    /// <summary>
    /// Gets whether an earlier rule-tree state is available.
    /// </summary>
    public bool CanUndo =>
        _history.Count > 0 &&
        (_index == -1 || _index + 1 < _history.Count);

    /// <summary>
    /// Gets whether a later rule-tree state is available.
    /// </summary>
    public bool CanRedo =>
        _history.Count > 0 &&
        _index > 0;

    /// <summary>
    /// Clears all saved history.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _index = -1;
    }

    /// <summary>
    /// Saves a snapshot of the supplied rule hierarchy.
    /// </summary>
    public void Save(
        IEnumerable<IRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (_index != -1)
        {
            _history.RemoveRange(
                0,
                _index);

            _index = -1;
        }

        _history.Insert(
            0,
            CloneRules(rules));
    }

    /// <summary>
    /// Moves to the previous rule-tree state.
    /// </summary>
    public List<IRule>? Undo(
        IEnumerable<IRule> currentRules)
    {
        ArgumentNullException.ThrowIfNull(currentRules);

        if (!CanUndo)
            return null;

        if (_index == -1)
        {
            _history.Insert(
                0,
                CloneRules(currentRules));

            _index = 1;
        }
        else
        {
            ++_index;
        }

        return CloneRules(
            _history[_index]);
    }

    /// <summary>
    /// Moves to the next rule-tree state.
    /// </summary>
    public List<IRule>? Redo()
    {
        if (!CanRedo)
            return null;

        --_index;

        return CloneRules(
            _history[_index]);
    }

    /// <summary>
    /// Creates an independent copy of a rule hierarchy.
    /// </summary>
    private static List<IRule> CloneRules(
        IEnumerable<IRule> rules)
    {
        return rules
            .Select(CloneRule)
            .ToList();
    }

    /// <summary>
    /// Creates an independent copy of a rule and its child hierarchy.
    /// </summary>
    private static IRule CloneRule(
        IRule rule)
    {
        IRule clone =
            (IRule)rule.Clone();

        clone.Children =
            rule.Children is null
                ? null
                : CloneRules(rule.Children);

        return clone;
    }
}