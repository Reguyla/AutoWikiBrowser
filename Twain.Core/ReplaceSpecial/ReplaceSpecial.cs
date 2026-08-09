/*
Derived from Autowikibrowser
Copyright (C) 2007 Martin Richards

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Twain.Core.ReplaceSpecial;

// TODO: Replace applicable comparison/replacement logic with existing
// IArticleComparer implementations where doing so preserves ReplaceSpecial
// behavior and reduces duplicated comparison logic.
//
// TODO: Separate rule-tree editing operations from WinForms event handling so
// rule creation, movement, copy/paste, history, and selection behavior can be
// tested independently of the form.

/// <summary>
/// Provides the WinForms editor used to create, organize, configure, and apply
/// ReplaceSpecial rules.
/// </summary>
/// <remarks>
/// The form coordinates the rule tree, rule-specific editor controls, command
/// history, and selection state.
/// </remarks>
public partial class ReplaceSpecial : Form, IRuleControlOwner
{
    #region contextmenu

    /// <summary>
    /// Creates a new top-level rule when the corresponding context-menu command is
    /// selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void NewRuleContextMenuItem_Click(
        object sender,
        EventArgs e)
    {
        NewRule();
    }

    /// <summary>
    /// Creates a new child rule beneath the currently selected rule when the
    /// corresponding context-menu command is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void NewSubruleContextMenuItem_Click(
        object sender,
        EventArgs e)
    {
        NewSubrule();
    }

    /// <summary>
    /// Cuts the currently selected rule when the Cut menu command is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void CutMenuItem_Click(
        object sender,
        EventArgs e)
    {
        CutCmd();
    }

    /// <summary>
    /// Copies the currently selected rule when the Copy menu command is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void CopyMenuItem_Click(
        object sender,
        EventArgs e)
    {
        CopyCmd();
    }

    /// <summary>
    /// Pastes the previously copied rule when the Paste menu command is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void PasteMenuItem_Click(
        object sender,
        EventArgs e)
    {
        PasteCmd();
    }

    #endregion

    /// <summary>
    /// Stores the rule currently selected and displayed in the rule editor.
    /// </summary>
    private IRule _currentRule;

    /// <summary>
    /// Stores the rule-specific WinForms control currently displayed in the rule
    /// editor area.
    /// </summary>
    private Control _ruleControl;

    /// <summary>
    /// Tracks rule-tree changes to support undo and redo operations.
    /// </summary>
    private readonly RuleTreeHistory _history;

    /// <summary>
    /// Initializes a new ReplaceSpecial rule editor.
    /// </summary>
    /// <remarks>
    /// Initializes the form controls, creates the rule-tree history manager, and
    /// updates command availability for the initial selection state.
    /// </remarks>
    public ReplaceSpecial()
    {
        InitializeComponent();

        _history =
            new RuleTreeHistory(
                RulesTreeView);

        UpdateEnabledStates();
    }

    /// <summary>
    /// Removes all rules from the rule tree when rules are currently present.
    /// </summary>
    public void Clear()
    {
        if (NoOfRules > 0)
        {
            RulesTreeView.Nodes.Clear();
        }
    }

    /// <summary>
    /// Displays the ReplaceSpecial form with the specified window title.
    /// </summary>
    /// <param name="titleText">
    /// The title to display in the form caption.
    /// </param>
    public void Show(string titleText)
    {
        Text = titleText;
        base.Show();
    }

    /// <summary>
    /// Saves the currently active rule and hides the ReplaceSpecial form when the
    /// OK button is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the button click.
    /// </param>
    private void OkButton_Click(
        object sender,
        EventArgs e)
    {
        SaveCurrentRule();
        Hide();
    }

    /// <summary>
    /// Saves the active rule and hides the form instead of allowing the form to
    /// close and be disposed.
    /// </summary>
    /// <param name="sender">
    /// The form that raised the closing event.
    /// </param>
    /// <param name="e">
    /// The form-closing event data used to cancel the close operation.
    /// </param>
    /// <remarks>
    /// ReplaceSpecial is intentionally retained for reuse. The close operation is
    /// therefore cancelled and the form is hidden instead of being disposed.
    /// </remarks>
    private void ReplaceSpecial_FormClosing(
        object sender,
        FormClosingEventArgs e)
    {
        SaveCurrentRule();

        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// Saves the previously active rule and restores the editor state for the newly
    /// selected rule.
    /// </summary>
    /// <param name="sender">
    /// The tree view that raised the selection-change event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the selected tree node.
    /// </param>
    /// <remarks>
    /// No action is taken when the tree has no selected node.
    /// </remarks>
    private void RulesTreeView_AfterSelect(
        object sender,
        TreeViewEventArgs e)
    {
        if (RulesTreeView.SelectedNode == null)
            return;

        SaveCurrentRule();
        RestoreSelectedRule();

        // TODO: Confirm whether this call is still required. RestoreSelectedRule
        // already updates enabled states after restoring the selection.
        UpdateEnabledStates();
    }

    /// <summary>
    /// Saves the currently active rule and refreshes its tree-view appearance.
    /// </summary>
    private void SaveCurrentRule()
    {
        if (_currentRule == null)
            return;

        _currentRule.Save();
        SetTreeViewColours();
    }

    /// <summary>
    /// Restores the rule associated with the currently selected tree node and
    /// updates the rule editor UI.
    /// </summary>
    private void RestoreSelectedRule()
    {
        TreeNode selectedNode =
            RulesTreeView.SelectedNode;

        if (selectedNode == null)
        {
            ClearSelectedRule();
        }
        else
        {
            ShowSelectedRule(selectedNode);
        }

        UpdateEnabledStates();
        SetTreeViewColours();
    }

    /// <summary>
    /// Clears the active rule and displays the no-selection placeholder.
    /// </summary>
    private void ClearSelectedRule()
    {
        if (_currentRule == null)
            return;

        _currentRule.DisposeControl();
        _currentRule = null;

        NoRuleSelectedLabel.Show();
    }

    /// <summary>
    /// Displays and restores the rule associated with the specified tree node.
    /// </summary>
    /// <param name="selectedNode">
    /// The selected rule tree node.
    /// </param>
    private void ShowSelectedRule(
        TreeNode selectedNode)
    {
        NoRuleSelectedLabel.Hide();

        IRule previousRule =
            _currentRule;

        // TODO: Validate or encapsulate the TreeNode.Tag -> IRule invariant so
        // rule-tree operations do not rely on repeated unchecked casts.
        _currentRule =
            (IRule)selectedNode.Tag;

        SuspendLayout();

        try
        {
            _ruleControl =
                _currentRule.CreateControl(
                    this,
                    RuleControlSpace.Controls,
                    Point.Empty);

            _ruleControl.Size =
                RuleControlSpace.Size;

            _currentRule.Name =
                selectedNode.Text;

            _currentRule.Restore();

            if (previousRule != null &&
                !_currentRule.Equals(previousRule))
            {
                previousRule.DisposeControl();
            }

            _ruleControl.Visible = true;
        }
        finally
        {
            ResumeLayout();
        }
    }

    /// <summary>
    /// Moves the currently selected rule one position upward within its owning
    /// node collection.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the button click.
    /// </param>
    private void UpButton_Click(
        object sender,
        EventArgs e)
    {
        MoveSelectedUp();
    }

    /// <summary>
    /// Moves the currently selected rule one position upward within its owning
    /// node collection.
    /// </summary>
    private void MoveSelectedUp()
    {
        MoveSelectedNode(moveUp: true);
    }

    /// <summary>
    /// Moves the currently selected rule one position downward within its owning
    /// node collection when the Down button is clicked.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the button click.
    /// </param>
    private void DownButton_Click(
        object sender,
        EventArgs e)
    {
        MoveSelectedDown();
    }

    /// <summary>
    /// Moves the currently selected rule one position downward within its owning
    /// node collection.
    /// </summary>
    private void MoveSelectedDown()
    {
        MoveSelectedNode(moveUp: false);
    }

    /// <summary>
    /// Moves the currently selected rule one position within its owning node
    /// collection.
    /// </summary>
    /// <param name="moveUp">
    /// <see langword="true"/> to move the selected rule upward; otherwise,
    /// <see langword="false"/> to move it downward.
    /// </param>
    /// <remarks>
    /// The selected node remains selected after the move, and the rule editor is
    /// restored so the UI remains synchronized with the reordered tree.
    /// </remarks>
    private void MoveSelectedNode(bool moveUp)
    {
        TreeNode selectedNode =
            RulesTreeView.SelectedNode;

        if (selectedNode == null)
            return;

        RulesTreeView.Select();

        TreeNodeCollection owningNodes =
            GetOwningNodes(selectedNode);

        if (owningNodes.Count < 2)
            return;

        TreeNode adjacentNode =
            moveUp
                ? selectedNode.PrevNode
                : selectedNode.NextNode;

        if (adjacentNode == null)
            return;

        _history.Save();

        int targetIndex =
            owningNodes.IndexOf(adjacentNode);

        owningNodes.Remove(selectedNode);
        owningNodes.Insert(
            targetIndex,
            selectedNode);

        RulesTreeView.SelectedNode =
            selectedNode;

        if (moveUp)
        {
            RulesTreeView.Select();
        }

        RestoreSelectedRule();
    }

    /// <summary>
    /// Gets the node collection that directly contains the specified tree node.
    /// </summary>
    /// <param name="treeNode">
    /// The tree node whose owning collection should be returned.
    /// </param>
    /// <returns>
    /// The parent node's child collection when <paramref name="treeNode"/> is
    /// nested; otherwise, the root node collection of the rule tree.
    /// </returns>
    private TreeNodeCollection GetOwningNodes(
        TreeNode treeNode)
    {
        TreeNode parentNode =
            treeNode.Parent;

        return parentNode != null
            ? parentNode.Nodes
            : RulesTreeView.Nodes;
    }

    /// <summary>
    /// Creates a new top-level rule when the New Rule button is clicked and
    /// refreshes the rule tree display.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the button click.
    /// </param>
    private void NewRuleButton_Click(
        object sender,
        EventArgs e)
    {
        NewRule();
        SetTreeViewColours();
    }

    /// <summary>
    /// Creates a new child rule beneath the currently selected rule when the
    /// New Subrule button is clicked and refreshes the rule tree display.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the button click.
    /// </param>
    private void NewSubruleButton_Click(
        object sender,
        EventArgs e)
    {
        NewSubrule();
        SetTreeViewColours();
    }

    /// <summary>
    /// Updates the selected tree node when a rule control reports a name change.
    /// </summary>
    /// <param name="rc">
    /// The rule control reporting the change.
    /// </param>
    /// <param name="name">
    /// The new display name for the selected rule.
    /// </param>
    /// <remarks>
    /// Empty names and values matching the current node text are ignored.
    /// </remarks>
    // TODO: Determine whether the rule-control parameter is required by
    // IRuleControlOwner implementations. Remove it from the interface during a
    // future API cleanup if no implementation uses it.
    public void NameChanged(
        Control rc,
        string name)
    {
        if (RulesTreeView.SelectedNode == null ||
            string.IsNullOrEmpty(name) ||
            RulesTreeView.SelectedNode.Text == name)
        {
            return;
        }

        RulesTreeView.SelectedNode.Text =
            name;
    }

    /// <summary>
    /// Updates the enabled state of ReplaceSpecial commands and controls based on
    /// the current rule selection and undo/redo history.
    /// </summary>
    /// <remarks>
    /// Commands that operate on a rule are disabled when no rule is selected.
    /// Undo and redo availability is determined by <see cref="RuleTreeHistory"/>.
    /// </remarks>
    private void UpdateEnabledStates()
    {
        bool hasSelection =
            RulesTreeView.SelectedNode != null;

        if (_ruleControl != null)
        {
            _ruleControl.Enabled =
                hasSelection;
        }

        DeleteButton.Enabled = hasSelection;
        UpButton.Enabled = hasSelection;
        DownButton.Enabled = hasSelection;
        NewSubruleButton.Enabled = hasSelection;

        DeleteMenuItem.Enabled = hasSelection;
        DeleteContextMenuItem.Enabled = hasSelection;

        NewSubruleMenu.Enabled = hasSelection;
        NewSubruleContextMenuItem.Enabled = hasSelection;

        NewSubruleMenuItem.Enabled = hasSelection;
        NewSubruleInTemplateCallMenuItem.Enabled = hasSelection;
        NewSubruleTemplateParameterMenuItem.Enabled = hasSelection;

        // TODO: Determine whether Paste should be enabled only when the
        // ReplaceSpecial clipboard contains a valid rule.
        PasteMenuItem.Enabled =
            PasteContextMenuItem.Enabled =
                true;

        CutMenuItem.Enabled = hasSelection;
        CutContextMenuItem.Enabled = hasSelection;

        CopyMenuItem.Enabled = hasSelection;
        CopyContextMenuItem.Enabled = hasSelection;

        UndoMenuItem.Enabled = _history.CanUndo;
        RedoMenuItem.Enabled = _history.CanRedo;
    }

    /// <summary>
    /// Deletes the currently selected rule when the Delete button is clicked.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the button click.
    /// </param>
    private void DeleteButton_Click(
        object sender,
        EventArgs e)
    {
        DeleteCmd();
    }

    /// <summary>
    /// Deletes the currently selected rule from the rule tree.
    /// </summary>
    /// <remarks>
    /// Before removing the node, the current rule state is saved and the tree
    /// history is captured for undo support. If the selected rule is a child rule,
    /// its rule object is also removed from the parent rule's
    /// <see cref="IRule.Children"/> collection.
    /// </remarks>
    private void DeleteCmd()
    {
        TreeNode selectedNode =
            RulesTreeView.SelectedNode;

        if (selectedNode == null)
            return;

        SaveCurrentRule();

        _history.Save();

        TreeNode nextNode =
            selectedNode.NextNode;

        TreeNode parentNode =
            selectedNode.Parent;

        if (parentNode != null)
        {
            IRule parentRule =
                (IRule)parentNode.Tag;

            if (parentRule.Children != null)
            {
                parentRule.Children.Remove(
                    (IRule)selectedNode.Tag);
            }
        }

        // TODO: Verify deletion of nested rules. The current implementation removes
        // the selected node through RulesTreeView.Nodes even when the node belongs
        // to a parent's Nodes collection.
        RulesTreeView.Nodes.Remove(
            selectedNode);

        RulesTreeView.SelectedNode =
            nextNode;

        RulesTreeView.Select();

        RestoreSelectedRule();
        SetTreeViewColours();
    }

    /// <summary>
    /// Saves the current rule whenever the form's visibility changes.
    /// </summary>
    private void ReplaceSpecial_VisibleChanged(
        object sender,
        EventArgs e)
    {
        SaveCurrentRule();
    }

    /// <summary>
    /// Saves the current rule when the form loses input focus.
    /// </summary>
    private void ReplaceSpecial_Leave(
        object sender,
        EventArgs e)
    {
        SaveCurrentRule();
    }

    /// <summary>
    /// Saves the current rule when the form becomes inactive.
    /// </summary>
    private void ReplaceSpecial_Deactivate(
        object sender,
        EventArgs e)
    {
        SaveCurrentRule();
    }

    /// <summary>
    /// Gets the number of top-level rules currently defined in the rule tree.
    /// </summary>
    /// <remarks>
    /// Child rules are not included in this count.
    /// </remarks>
    public int NoOfRules =>
        RulesTreeView.Nodes.Count;

    /// <summary>
    /// Gets a value indicating whether at least one top-level rule is currently
    /// defined.
    /// </summary>
    public bool HasRules =>
        NoOfRules != 0;

    /// <summary>
    /// Applies all configured top-level ReplaceSpecial rules to the supplied
    /// article text.
    /// </summary>
    /// <param name="text">
    /// The article text to process.
    /// </param>
    /// <param name="title">
    /// The title of the article being processed.
    /// </param>
    /// <returns>
    /// The article text after all configured rules have been applied.
    /// </returns>
    /// <remarks>
    /// Each rule is applied in tree order, and the output of one rule becomes the
    /// input to the next rule.
    /// </remarks>
    public string ApplyRules(
        string text,
        string title)
    {
        foreach (TreeNode treeNode in RulesTreeView.Nodes)
        {
            IRule rule =
                (IRule)treeNode.Tag;

            text =
                rule.Apply(
                    treeNode,
                    text,
                    title);
        }

        return text;
    }

    /// <summary>
    /// Deletes the currently selected rule when the Delete menu command is
    /// selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void DeleteMenuItem_Click(
        object sender,
        EventArgs e)
    {
        DeleteCmd();
    }

    /// <summary>
    /// Cuts the currently selected rule by copying it and then deleting it from the
    /// rule tree.
    /// </summary>
    /// <remarks>
    /// The delete operation already restores the selected-rule UI after removing
    /// the node.
    /// </remarks>
    private void CutCmd()
    {
        if (RulesTreeView.SelectedNode == null)
            return;

        CopyCmd();
        DeleteCmd();

        // TODO: Confirm whether this additional restore is necessary.
        // DeleteCmd already restores the selected-rule UI after deletion.
        RestoreSelectedRule();
    }


    /// <summary>
    /// Copies the currently selected replacement rule to the clipboard.
    /// </summary>
    /// <remarks>
    /// The selected rule is saved, serialized to its text representation, and
    /// placed on the clipboard so it can be pasted into another rules tree.
    /// </remarks>
    private void CopyCmd()
    {
        if (RulesTreeView.SelectedNode == null)
            return;

        SaveCurrentRule();
       _history.Save();

        string serializedRule = Serialize(GetSelectedRule());
        Tools.CopyToClipboard(serializedRule, true);

        UpdateEnabledStates();
    }

    /// <summary>
    /// Pastes a serialized replacement rule from the clipboard into the rules tree.
    /// </summary>
    /// <remarks>
    /// Clipboard data is accepted only when it contains text. The serialized rule
    /// is parsed before the tree enters its bulk-update state so invalid clipboard
    /// contents leave the current rules tree unchanged.
    /// </remarks>
    private void PasteCmd()
    {
        IDataObject clipboardData = Clipboard.GetDataObject();

        if (clipboardData?.GetDataPresent(typeof(string)) != true)
            return;

        if (clipboardData.GetData(typeof(string)) is not string serializedRule ||
            string.IsNullOrWhiteSpace(serializedRule))
        {
            return;
        }

        try
        {
            var rule = Deserialize(serializedRule);

            RulesTreeView.BeginUpdate();

            try
            {
                SaveCurrentRule();
                _history.Save();

                AddNewRule(rule);

                RulesTreeView.Select();
                RestoreSelectedRule();
                RulesTreeView.ExpandAll();
            }
            finally
            {
                RulesTreeView.EndUpdate();
            }
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show(
                this,
                "The clipboard does not contain a valid replacement rule.",
                "Unable to paste rule",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Creates a new standard ReplaceSpecial rule and adds it to the rule tree.
    /// </summary>
    private void NewRule()
    {
        AddNewRule(
            RuleFactory.CreateRule());
    }

    /// <summary>
    /// Creates a new in-template rule when the corresponding menu command is
    /// selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void NewInTemplateRuleMenuItem_Click(
        object sender,
        EventArgs e)
    {
        AddNewRule(
            RuleFactory.CreateInTemplateRule());
    }

    /// <summary>
    /// Creates a new template-parameter rule when the corresponding menu command
    /// is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void NewTemplateParameterRuleMenuItem_Click(
        object sender,
        EventArgs e)
    {
        AddNewRule(
            RuleFactory.CreateTemplateParamRule());
    }

    /// <summary>
    /// Rebuilds the child-rule hierarchy for the specified rule from the
    /// corresponding tree-node hierarchy.
    /// </summary>
    /// <param name="treeNode">
    /// The tree node whose child nodes should be converted into child rules.
    /// </param>
    /// <param name="rule">
    /// The rule associated with <paramref name="treeNode"/>.
    /// </param>
    /// <remarks>
    /// When the node has children, the rule's existing
    /// <see cref="IRule.Children"/> collection is replaced with a new collection
    /// reflecting the current tree structure.
    /// </remarks>
    private static void RecurseNode(
        TreeNode treeNode,
        IRule rule)
    {
        if (treeNode.Nodes.Count == 0)
            return;

        rule.Children =
            new List<IRule>();

        foreach (TreeNode childNode in treeNode.Nodes)
        {
            IRule childRule =
                (IRule)childNode.Tag;

            if (childNode.Nodes.Count > 0)
            {
                RecurseNode(
                    childNode,
                    childRule);
            }

            rule.Children.Add(childRule);
        }
    }

    /// <summary>
    /// Gets the complete set of top-level ReplaceSpecial rules represented by the
    /// current rule tree.
    /// </summary>
    /// <returns>
    /// A list containing the top-level rules in tree order. Each returned rule's
    /// child-rule collection is synchronized with the current tree hierarchy.
    /// </returns>
    /// <remarks>
    /// This method rebuilds nested <see cref="IRule.Children"/> collections from
    /// the WinForms tree before returning the rules.
    /// </remarks>
    public List<IRule> GetRules()
    {
        List<IRule> rules =
            new();

        foreach (TreeNode treeNode in RulesTreeView.Nodes)
        {
            IRule rule =
                (IRule)treeNode.Tag;

            if (treeNode.Nodes.Count > 0)
            {
                RecurseNode(
                    treeNode,
                    rule);
            }

            rules.Add(rule);
        }

        return rules;
    }

    /// <summary>
    /// Gets the rule associated with the currently selected tree node.
    /// </summary>
    /// <returns>
    /// The selected rule with its child-rule hierarchy synchronized to the current
    /// tree structure.
    /// </returns>
    /// <remarks>
    /// The current implementation assumes that a rule tree node is selected and
    /// that its <see cref="TreeNode.Tag"/> contains an <see cref="IRule"/>.
    /// </remarks>
    // TODO: Review whether callers can request the selected rule while no tree node
    // is selected. If so, make the no-selection contract explicit rather than
    // relying on the current TreeNode/IRule assumptions.
    public IRule GetSelectedRule()
    {
        TreeNode selectedNode =
            RulesTreeView.SelectedNode;

        IRule selectedRule =
            (IRule)selectedNode.Tag;

        if (selectedNode.Nodes.Count > 0)
        {
            RecurseNode(
                selectedNode,
                selectedRule);
        }

        return selectedRule;
    }

    /// <summary>
    /// Replaces the current rule tree with the supplied rules.
    /// </summary>
    /// <param name="rules">
    /// The top-level rules to load into the rule tree.
    /// </param>
    /// <remarks>
    /// Child rules are added recursively by <c>AppendRule</c>, after which the
    /// complete tree is expanded.
    /// </remarks>
    public void AddNewRule(
        List<IRule> rules)
    {
        RulesTreeView.BeginUpdate();

        try
        {
            RulesTreeView.Nodes.Clear();

            foreach (IRule rule in rules)
            {
                AppendRule(rule);
            }

            RulesTreeView.ExpandAll();
        }
        finally
        {
            RulesTreeView.EndUpdate();
        }
    }

    /// <summary>
    /// Adds a rule to the rule tree immediately after the currently selected rule,
    /// or at the root level when no rule is selected.
    /// </summary>
    /// <param name="rule">
    /// The rule to add.
    /// </param>
    /// <remarks>
    /// Existing child rules are added recursively. When the rule has no children,
    /// the new node becomes the current tree selection and its editor is restored.
    /// </remarks>
    private void AddNewRule(
        IRule rule)
    {
        if (rule == null)
            return;

        SaveCurrentRule();
        _history.Save();

        TreeNode newNode =
            new(rule.Name)
            {
                Tag = rule
            };

        InsertRuleNode(newNode);
        AddChildRulesOrSelectNode(rule, newNode);

        RestoreSelectedRule();

        _currentRule.SelectName();
    }

    /// <summary>
    /// Inserts the specified rule node immediately after the currently selected
    /// node, or at the root level when no node is selected.
    /// </summary>
    /// <param name="newNode">
    /// The rule tree node to insert.
    /// </param>
    private void InsertRuleNode(
        TreeNode newNode)
    {
        TreeNode selectedNode =
            RulesTreeView.SelectedNode;

        if (selectedNode == null)
        {
            RulesTreeView.Nodes.Add(newNode);
            return;
        }

        TreeNode parentNode =
            selectedNode.Parent;

        if (parentNode == null)
        {
            RulesTreeView.Nodes.Insert(
                RulesTreeView.Nodes.IndexOf(selectedNode) + 1,
                newNode);

            return;
        }

        parentNode.Nodes.Insert(
            parentNode.Nodes.IndexOf(selectedNode) + 1,
            newNode);
    }

    /// <summary>
    /// Adds the supplied rule's child rules beneath the specified node, or selects
    /// the node when the rule has no children.
    /// </summary>
    /// <param name="rule">
    /// The rule whose child rules should be added.
    /// </param>
    /// <param name="newNode">
    /// The tree node associated with the rule.
    /// </param>
    private void AddChildRulesOrSelectNode(
        IRule rule,
        TreeNode newNode)
    {
        if (rule.Children != null &&
            rule.Children.Count > 0)
        {
            foreach (IRule childRule in rule.Children)
            {
                AddNewRule(
                    childRule,
                    newNode);
            }

            return;
        }

        RulesTreeView.SelectedNode =
            newNode;

        RulesTreeView.Select();
    }

    /// <summary>
    /// Adds the specified rule as a child of the supplied parent tree node and
    /// recursively adds any nested child rules.
    /// </summary>
    /// <param name="rule">
    /// The rule to add.
    /// </param>
    /// <param name="parentNode">
    /// The tree node beneath which the rule should be added.
    /// </param>
    /// <remarks>
    /// When the rule has no child rules, the newly created node becomes the current
    /// selection.
    /// </remarks>
    private void AddNewRule(
        IRule rule,
        TreeNode parentNode)
    {
        TreeNode newNode =
            new(rule.Name)
            {
                Tag = rule
            };

        parentNode.Nodes.Add(newNode);

        if (rule.Children != null &&
            rule.Children.Count > 0)
        {
            foreach (IRule childRule in rule.Children)
            {
                AddNewRule(
                    childRule,
                    newNode);
            }
        }
        else
        {
            RulesTreeView.SelectedNode =
                newNode;

            RulesTreeView.Select();
        }
    }

    /// <summary>
    /// Appends the specified rule to the root of the rule tree and recursively adds
    /// any nested child rules.
    /// </summary>
    /// <param name="rule">
    /// The top-level rule to append.
    /// </param>
    /// <remarks>
    /// When the rule has no child rules, the newly created node becomes the current
    /// selection.
    /// </remarks>
    private void AppendRule(IRule rule)
    {
        TreeNode newNode =
            new(rule.Name)
            {
                Tag = rule
            };

        RulesTreeView.Nodes.Add(newNode);

        if (rule.Children != null &&
            rule.Children.Count > 0)
        {
            foreach (IRule childRule in rule.Children)
            {
                AddNewRule(
                    childRule,
                    newNode);
            }
        }
        else
        {
            RulesTreeView.SelectedNode =
                newNode;

            RulesTreeView.Select();
        }
    }

    /// <summary>
    /// Creates a new standard rule as a child of the currently selected rule.
    /// </summary>
    private void NewSubrule()
    {
        AddNewSubrule(
            RuleFactory.CreateRule());
    }

    /// <summary>
    /// Adds the specified rule as a child of the currently selected rule.
    /// </summary>
    /// <param name="rule">
    /// The rule to add as a child.
    /// </param>
    /// <remarks>
    /// The current rule is saved before the tree is modified. The new child rule
    /// becomes selected and its editor is restored after insertion.
    /// </remarks>
    private void AddNewSubrule(IRule rule)
    {
        SaveCurrentRule();

        TreeNode selectedNode =
            RulesTreeView.SelectedNode;

        if (selectedNode == null)
            return;

        _history.Save();

        TreeNode newNode =
            new(rule.Name)
            {
                Tag = rule
            };

        selectedNode.Nodes.Add(newNode);

        RulesTreeView.SelectedNode =
            newNode;

        RulesTreeView.Select();

        RestoreSelectedRule();
        _currentRule.SelectName();
    }

    /// <summary>
    /// Creates a new in-template rule as a child of the currently selected rule
    /// when the corresponding menu command is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void NewSubruleInTemplateCallMenuItem_Click(
        object sender,
        EventArgs e)
    {
        AddNewSubrule(
            RuleFactory.CreateInTemplateRule());
    }

    /// <summary>
    /// Creates a new template-parameter rule as a child of the currently selected
    /// rule when the corresponding menu command is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void NewSubruleTemplateParameterMenuItem_Click(
        object sender,
        EventArgs e)
    {
        AddNewSubrule(
            RuleFactory.CreateTemplateParamRule());
    }

    /// <summary>
    /// Creates a new top-level rule when the corresponding menu command is
    /// selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void NewRuleMenuItem_Click(
        object sender,
        EventArgs e)
    {
        NewRule();
    }

    /// <summary>
    /// Creates a new child rule beneath the currently selected rule when the
    /// corresponding menu command is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void NewSubruleMenuItem_Click(
        object sender,
        EventArgs e)
    {
        NewSubrule();
    }

    /// <summary>
    /// Saves the current rule, restores the previous rule-tree state, and refreshes
    /// the selected-rule editor.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void UndoMenuItem_Click(
        object sender,
        EventArgs e)
    {
        SaveCurrentRule();
        _history.Undo();
        RestoreSelectedRule();
    }

    /// <summary>
    /// Saves the current rule, reapplies the next rule-tree state, and refreshes
    /// the selected-rule editor.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void RedoMenuItem_Click(
        object sender,
        EventArgs e)
    {
        SaveCurrentRule();
        _history.Redo();
        RestoreSelectedRule();
    }

    /// <summary>
    /// Refreshes the visual coloring of the rule tree when the corresponding menu
    /// command is selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void refreshColoursToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        SetTreeViewColours();
    }

    /// <summary>
    /// Handles keyboard shortcuts used by the replacement-rules tree.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Information about the pressed key.</param>
    private void ReplaceSpecial_KeyDown(object sender, KeyEventArgs e)
    {
        if (!RulesTreeView.Focused)
            return;

        if (e.KeyCode == Keys.Delete)
        {
            e.Handled = true;
            DeleteCmd();
            return;
        }

        if (!e.Control)
            return;

        e.Handled = true;

        switch (e.KeyCode)
        {
            case Keys.C:
                CopyCmd();
                break;

            case Keys.V:
                PasteCmd();
                break;

            case Keys.X:
                CutCmd();
                break;

            default:
                e.Handled = false;
                break;
        }
    }

    /// <summary>
    /// Starts a move operation when a node is dragged from the rules tree.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// Information about the item being dragged.
    /// </param>
    private void RulesTreeView_ItemDrag(object sender, ItemDragEventArgs e)
    {
        if (e.Item is TreeNode draggedNode)
            RulesTreeView.DoDragDrop(draggedNode, DragDropEffects.Move);
    }

    /// <summary>
    /// Determines whether incoming drag data can be accepted by the rules tree.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// Information about the current drag-and-drop operation.
    /// </param>
    private void RulesTreeView_DragEnter(object sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(typeof(TreeNode)) == true
            ? DragDropEffects.Move
            : DragDropEffects.None;
    }

    /// <summary>
    /// Updates the prospective drop target and prevents invalid tree-node moves.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// Information about the current drag position and transferred data.
    /// </param>
    private void RulesTreeView_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(TreeNode)) is not TreeNode draggedNode)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        Point targetPoint =
            RulesTreeView.PointToClient(new Point(e.X, e.Y));

        TreeNode targetNode = RulesTreeView.GetNodeAt(targetPoint);

        if (targetNode == null ||
            ReferenceEquals(draggedNode, targetNode) ||
            Tools.IsSubnodeOf(draggedNode, targetNode))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        RulesTreeView.SelectedNode = targetNode;
        e.Effect = DragDropEffects.Move;
    }

    /// <summary>
    /// Moves the dragged rule node to the selected location in the rules tree.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// Information about the completed drag-and-drop operation.
    /// </param>
    private void RulesTreeView_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(TreeNode)) is not TreeNode draggedNode)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        Point targetPoint =
            RulesTreeView.PointToClient(new Point(e.X, e.Y));

        TreeNode targetNode = RulesTreeView.GetNodeAt(targetPoint);

        if (ReferenceEquals(draggedNode, targetNode) ||
            targetNode != null && Tools.IsSubnodeOf(draggedNode, targetNode))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        RulesTreeView.BeginUpdate();

        try
        {
            draggedNode.Remove();

            if (targetNode == null)
                RulesTreeView.Nodes.Add(draggedNode);
            else
                targetNode.Nodes.Insert(0, draggedNode);

            RulesTreeView.SelectedNode = draggedNode;
            RestoreSelectedRule();

            e.Effect = DragDropEffects.Move;
        }
        finally
        {
            RulesTreeView.EndUpdate();
        }
    }

    /// <summary>
    /// Refreshes the background color of every rule node in the tree based on the
    /// enabled state of its associated rule.
    /// </summary>
    private void SetTreeViewColours()
    {
        RulesTreeView.BeginUpdate();

        try
        {
            foreach (TreeNode treeNode in RulesTreeView.Nodes)
            {
                SetColours(treeNode);
            }
        }
        finally
        {
            RulesTreeView.EndUpdate();
        }
    }

    /// <summary>
    /// Applies enabled-state coloring to the specified rule node and all of its
    /// descendants.
    /// </summary>
    /// <param name="treeNode">
    /// The rule tree node whose color and descendant colors should be refreshed.
    /// </param>
    private static void SetColours(TreeNode treeNode)
    {
        IRule rule =
            (IRule)treeNode.Tag;

        SetNodeColour(
            treeNode,
            rule);

        foreach (TreeNode childNode in treeNode.Nodes)
        {
            SetColours(childNode);
        }
    }

    /// <summary>
    /// Applies the appropriate background color to a rule tree node based on
    /// whether the associated rule is enabled.
    /// </summary>
    /// <param name="treeNode">
    /// The tree node whose background color should be updated.
    /// </param>
    /// <param name="rule">
    /// The rule associated with the tree node.
    /// </param>
    // TODO: Replace hard-coded enabled/disabled colors with theme-aware and
    // accessibility-friendly visual states that do not rely on color alone.
    private static void SetNodeColour(
        TreeNode treeNode,
        IRule rule)
    {
        treeNode.BackColor =
            rule.enabled_
                ? Color.White
                : Color.Red;
    }

    /// <summary>
    /// Refreshes rule-tree coloring when the ReplaceSpecial form is loaded.
    /// </summary>
    /// <param name="sender">
    /// The form that raised the load event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the form load.
    /// </param>
    private void ReplaceSpecial_Load(
        object sender,
        EventArgs e)
    {
        SetTreeViewColours();
    }

    /// <summary>
    /// Expands all rule-tree nodes when the corresponding menu command is
    /// selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void expandAllToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        Collapsed(false);
    }

    /// <summary>
    /// Collapses all rule-tree nodes when the corresponding menu command is
    /// selected.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu-item click.
    /// </param>
    private void collapseAllToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        Collapsed(true);
    }

    /// <summary>
    /// Expands or collapses all top-level rule-tree nodes.
    /// </summary>
    /// <param name="collapsed">
    /// <see langword="true"/> to collapse each top-level node; otherwise,
    /// <see langword="false"/> to expand each node and all of its descendants.
    /// </param>
    private void Collapsed(bool collapsed)
    {
        RulesTreeView.BeginUpdate();

        try
        {
            foreach (TreeNode treeNode in RulesTreeView.Nodes)
            {
                if (collapsed)
                {
                    treeNode.Collapse();
                }
                else
                {
                    treeNode.ExpandAll();
                }
            }
        }
        finally
        {
            RulesTreeView.EndUpdate();
        }
    }

    #region Serialize/Deserialize for Clipboard work
    //Base code from http://www.dotnetjohn.com/articles.aspx?articleid=173

    /// <summary>
    /// Serializes a replacement rule to its XML clipboard representation.
    /// </summary>
    /// <param name="rule">The replacement rule to serialize.</param>
    /// <returns>
    /// An XML string containing the serialized replacement rule.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rule"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the rule cannot be serialized.
    /// </exception>
    private static string Serialize(IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var serializer = new XmlSerializer(typeof(IRule));

        using var stream = new MemoryStream();

        using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
        {
            serializer.Serialize(writer, rule);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Deserializes a replacement rule from its XML clipboard representation.
    /// </summary>
    /// <param name="serializedRule">
    /// The XML representation of the replacement rule.
    /// </param>
    /// <returns>The deserialized replacement rule.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serializedRule"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied text is not valid replacement-rule XML.
    /// </exception>
    private static IRule Deserialize(string serializedRule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializedRule);

        if (!serializedRule.Contains(
                "<?xml",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The supplied text is not an XML replacement rule.");
        }

        var serializer = new XmlSerializer(typeof(IRule));

        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(serializedRule));

        return serializer.Deserialize(stream) as IRule
            ?? throw new InvalidOperationException(
                "The supplied XML does not contain a valid replacement rule.");
    }
    #endregion
}