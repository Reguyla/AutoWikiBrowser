using System.Windows.Forms;

namespace Twain.Core.ReplaceSpecial;

// TODO: Verify that history snapshots clone the IRule instances stored in
// TreeNode.Tag rather than sharing mutable rule objects between snapshots.
// Add regression tests covering changes to rule properties across undo/redo.
/// <summary>
/// Maintains undo and redo history for a rule tree displayed in a
/// <see cref="TreeView"/>.
/// </summary>
/// <remarks>
/// History entries are stored as cloned collections of tree nodes. The
/// current history position is tracked separately to support undo and redo
/// navigation.
/// </remarks>
public class RuleTreeHistory
{
    private readonly List<List<TreeNode>> History = new();
    private int index_ = -1;

    private readonly TreeView treeView_;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleTreeHistory"/> class.
    /// </summary>
    /// <param name="tv">
    /// The tree view whose rule hierarchy is tracked.
    /// </param>
    public RuleTreeHistory(TreeView tv)
    {
        ArgumentNullException.ThrowIfNull(tv);

        treeView_ = tv;
    }

    /// <summary>
    /// Clears all saved history and resets the current history position.
    /// </summary>
    public void Clear()
    {
        History.Clear();
        index_ = -1;
    }

    /// <summary>
    /// Saves the current rule tree state to the history.
    /// </summary>
    /// <remarks>
    /// When called after an undo operation, the existing history is cleared
    /// before the current state is saved.
    /// </remarks>
    public void Save()
    {
        if (index_ != -1)
        {
            Clear();
        }

        InternalSave();
    }

    /// <summary>
    /// Saves a cloned snapshot of the current rule tree.
    /// </summary>
    private void InternalSave()
    {
        List<TreeNode> copy = Copy(treeView_.Nodes);
        History.Insert(0, copy);
    }

    /// <summary>
    /// Gets a value indicating whether an earlier rule tree state is
    /// available.
    /// </summary>
    public bool CanUndo =>
        History.Count > 0 &&
        (index_ == -1 || index_ + 1 < History.Count);

    // TODO: Review history behavior when a new change is saved after Undo.
    // Consider discarding only redo states rather than clearing the entire
    // undo/redo history.
    /// <summary>
    /// Restores the previous rule tree state when one is available.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        if (index_ == -1)
        {
            InternalSave();
            index_ = 1;
        }
        else
        {
            ++index_;
        }

        Restore();
    }

    /// <summary>
    /// Gets a value indicating whether a later rule tree state is available
    /// after an undo operation.
    /// </summary>
    public bool CanRedo =>
        History.Count > 0 &&
        index_ > 0;

    /// <summary>
    /// Restores the next rule tree state when one is available.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        --index_;
        Restore();
    }

    /// <summary>
    /// Replaces the current tree contents with the history entry at the
    /// current history position.
    /// </summary>
    private void Restore()
    {
        treeView_.Nodes.Clear();

        List<TreeNode> historyEntry = History[index_];

        foreach (TreeNode treeNode in historyEntry)
        {
            TreeNode copy = (TreeNode)treeNode.Clone();

            treeView_.Nodes.Add(copy);
            UpdateNames(copy);
        }
    }

    /// <summary>
    /// Updates the displayed rule name for the specified node and all of its
    /// child nodes.
    /// </summary>
    /// <param name="t">
    /// The tree node whose displayed rule name should be refreshed.
    /// </param>
    private static void UpdateNames(TreeNode t)
    {
        if (t == null)
        {
            return;
        }

        IRule rule = (IRule)t.Tag;
        t.Text = rule.Name;

        foreach (TreeNode sub in t.Nodes)
        {
            UpdateNames(sub);
        }
    }

    /// <summary>
    /// Creates a cloned list of the supplied tree nodes.
    /// </summary>
    /// <param name="col">
    /// The tree node collection to copy.
    /// </param>
    /// <returns>
    /// A list containing clones of the supplied tree nodes.
    /// </returns>
    private static List<TreeNode> Copy(TreeNodeCollection col)
    {
        return col
            .Cast<TreeNode>()
            .Select(t => (TreeNode)t.Clone())
            .ToList();
    }
}