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

namespace WikiFunctions.ReplaceSpecial;

//TODO: Use IArticleComparer derivatives where possible

public partial class ReplaceSpecial : Form, IRuleControlOwner
{
    #region contextmenu
    private void NewRuleContextMenuItem_Click(object sender, EventArgs e)
    {
        NewRule();
    }

    private void NewSubruleContextMenuItem_Click(object sender, EventArgs e)
    {
        NewSubrule();
    }

    private void CutMenuItem_Click(object sender, EventArgs e)
    {
        CutCmd();
    }

    private void CopyMenuItem_Click(object sender, EventArgs e)
    {
        CopyCmd();
    }

    private void PasteMenuItem_Click(object sender, EventArgs e)
    {
        PasteCmd();
    }

    #endregion

    IRule CurrentRule;
    Control ruleControl_;
    private readonly RuleTreeHistory History;

    public void Clear()
    {
        if (NoOfRules > 0)
            RulesTreeView.Nodes.Clear();
    }

    public ReplaceSpecial()
    {
        InitializeComponent();

        History = new RuleTreeHistory(RulesTreeView);

        UpdateEnabledStates();
    }

    public void Show(string titleText)
    {
        Text = titleText;
        base.Show();
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
        SaveCurrentRule();
        Hide();
    }

    private void ReplaceSpecial_FormClosing(object sender, FormClosingEventArgs e)
    {
        SaveCurrentRule();
        e.Cancel = true;
        Hide();
    }

    private void RulesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
    {
        if (RulesTreeView.SelectedNode == null)
            return;

        SaveCurrentRule();
        RestoreSelectedRule();
        UpdateEnabledStates();
    }

    private void SaveCurrentRule()
    {
        if (CurrentRule == null)
            return;

        CurrentRule.Save();
        SetTreeViewColours();
    }

    private void RestoreSelectedRule()
    {
        if (RulesTreeView.SelectedNode == null)
        {
            if (CurrentRule != null)
            {
                CurrentRule.DisposeControl();
                CurrentRule = null;
                NoRuleSelectedLabel.Show();
            }
        }
        else
        {
            NoRuleSelectedLabel.Hide();

            IRule oldrule = CurrentRule;

            CurrentRule = (IRule)RulesTreeView.SelectedNode.Tag;

            SuspendLayout();

            ruleControl_ = CurrentRule.CreateControl(this, RuleControlSpace.Controls, new Point());
            ruleControl_.Size = RuleControlSpace.Size;

            CurrentRule.Name = RulesTreeView.SelectedNode.Text;

            CurrentRule.Restore();

            if (oldrule != null)
            {
                if (!CurrentRule.Equals(oldrule))
                    oldrule.DisposeControl();
            }

            ruleControl_.Visible = true;

            ResumeLayout();
        }
        UpdateEnabledStates();
        SetTreeViewColours();
    }

    private void UpButton_Click(object sender, EventArgs e)
    {
        MoveSelectedUp();
    }

    private void MoveSelectedUp()
    {
        TreeNode tn = RulesTreeView.SelectedNode;

        if (tn == null)
            return;

        RulesTreeView.Select();

        TreeNodeCollection col = GetOwningNodes(tn);

        if (col.Count < 2)
            return;

        TreeNode p = tn.PrevNode;
        if (p == null)
            return;

        History.Save();

        col.Remove(tn);
        int i = col.IndexOf(p);
        col.Insert(i, tn);

        RulesTreeView.SelectedNode = tn;
        RulesTreeView.Select();
        //RulesTreeView.ExpandAll();
        RestoreSelectedRule();
    }

    private TreeNodeCollection GetOwningNodes(TreeNode t)
    {
        TreeNode p = t.Parent;
        return p != null ? p.Nodes : RulesTreeView.Nodes;
    }

    private void DownButton_Click(object sender, EventArgs e)
    {
        TreeNode tn = RulesTreeView.SelectedNode;

        if (tn == null)
            return;

        RulesTreeView.Select();

        TreeNodeCollection col = GetOwningNodes(tn);

        if (col.Count < 2)
            return;

        TreeNode p = tn.NextNode;
        if (p == null)
            return;

        History.Save();

        int i = col.IndexOf(p);
        col.Remove(tn);
        col.Insert(i, tn);

        RulesTreeView.SelectedNode = tn;
        //RulesTreeView.ExpandAll();
        RestoreSelectedRule();
    }

    private void NewRuleButton_Click(object sender, EventArgs e)
    {
        NewRule();
        SetTreeViewColours();
    }

    private void NewSubruleButton_Click(object sender, EventArgs e)
    {
        NewSubrule();
        SetTreeViewColours();
    }

    public void NameChanged(Control rc, string name)
    {
        if (RulesTreeView.SelectedNode == null
            || string.IsNullOrEmpty(name)
            || RulesTreeView.SelectedNode.Text == name)
            return;

        RulesTreeView.SelectedNode.Text = name;
    }

    private void UpdateEnabledStates()
    {
        bool hasSelection = RulesTreeView.SelectedNode != null;

        if (ruleControl_ != null)
            ruleControl_.Enabled = hasSelection;

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

        PasteMenuItem.Enabled = PasteContextMenuItem.Enabled = true;

        CutMenuItem.Enabled = hasSelection;
        CutContextMenuItem.Enabled = hasSelection;

        CopyMenuItem.Enabled = hasSelection;
        CopyContextMenuItem.Enabled = hasSelection;

        UndoMenuItem.Enabled = History.CanUndo;
        RedoMenuItem.Enabled = History.CanRedo;
    }

    private void DeleteButton_Click(object sender, EventArgs e)
    {
        DeleteCmd();
    }

    private void DeleteCmd()
    {
        TreeNode st = RulesTreeView.SelectedNode;
        if (st == null)
            return;

        SaveCurrentRule();

        History.Save();

        TreeNode nt = st.NextNode;

        TreeNode parent = st.Parent;

        if (parent != null)
        {
            IRule rule = (IRule)parent.Tag;
            if (rule.Children != null)
            {
                rule.Children.Remove((IRule)st.Tag);
            }
        }

        RulesTreeView.Nodes.Remove(st);

        RulesTreeView.SelectedNode = nt;
        RulesTreeView.Select();
        RestoreSelectedRule();
        SetTreeViewColours();
    }

    private void ReplaceSpecial_VisibleChanged(object sender, EventArgs e)
    {
        SaveCurrentRule();
    }

    private void ReplaceSpecial_Leave(object sender, EventArgs e)
    {
        SaveCurrentRule();
    }

    private void ReplaceSpecial_Deactivate(object sender, EventArgs e)
    {
        SaveCurrentRule();
    }

    /// <summary>
    ///
    /// </summary>
    public int NoOfRules { get { return RulesTreeView.Nodes.Count; } }

    /// <summary>
    ///
    /// </summary>
    public bool HasRules { get { return NoOfRules != 0; } }

    /// <summary>
    /// Applys the Replace Special Rules
    /// </summary>
    /// <param name="text">Article title</param>
    /// <param name="title">Article text</param>
    /// <returns>Amended text</returns>
    public string ApplyRules(string text, string title)
    {
        foreach (TreeNode tn in RulesTreeView.Nodes)
        {
            IRule r = (IRule)tn.Tag;
            text = r.Apply(tn, text, title);
        }

        return text;
    }

    private void DeleteMenuItem_Click(object sender, EventArgs e)
    {
        DeleteCmd();
    }

    private void CutCmd()
    {
        if (RulesTreeView.SelectedNode == null)
            return;

        CopyCmd();
        DeleteCmd();
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
        History.Save();

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
                History.Save();

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

    private void NewRule()
    {
        AddNewRule(RuleFactory.CreateRule());
    }

    private void NewInTemplateRuleMenuItem_Click(object sender, EventArgs e)
    {
        AddNewRule(RuleFactory.CreateInTemplateRule());
    }

    private void NewTemplateParameterRuleMenuItem_Click(object sender, EventArgs e)
    {
        AddNewRule(RuleFactory.CreateTemplateParamRule());
    }

    private static void RecurseNode(TreeNode n, IRule r)
    {
        if (n.Nodes.Count == 0) return;

        r.Children = new List<IRule>();
        foreach (TreeNode n1 in n.Nodes)
        {
            IRule r1 = (IRule)n1.Tag;
            if (n1.Nodes.Count > 0) RecurseNode(n1, r1);
            r.Children.Add(r1);
        }
    }

    public List<IRule> GetRules()
    {
        List<IRule> l = new List<IRule>();

        foreach (TreeNode tn in RulesTreeView.Nodes)
        {
            IRule r = (IRule)tn.Tag;
            if (tn.Nodes.Count > 0) RecurseNode(tn, r);
            l.Add(r);
        }

        return l;
    }

    public IRule GetSelectedRule()
    {
        TreeNode tn = RulesTreeView.SelectedNode;
        IRule r = (IRule)tn.Tag;
        if (tn.Nodes.Count > 0) RecurseNode(tn, r);

        return r;
    }

    public void AddNewRule(List<IRule> rules)
    {
        RulesTreeView.BeginUpdate();
        RulesTreeView.Nodes.Clear();

        foreach (IRule r in rules)
        {
            AppendRule(r);
        }

        RulesTreeView.ExpandAll();
        RulesTreeView.EndUpdate();
    }

    private void AddNewRule(IRule r)
    {
        if (r == null)
            return;

        SaveCurrentRule();
        History.Save();

        TreeNode n = new TreeNode(r.Name) { Tag = r };

        TreeNode s = RulesTreeView.SelectedNode;
        if (s != null)
        {
            TreeNode p = s.Parent;
            if (p == null)
                RulesTreeView.Nodes.Insert(RulesTreeView.Nodes.IndexOf(s) + 1, n);
            else
                p.Nodes.Insert(p.Nodes.IndexOf(s) + 1, n);
        }
        else
        {
            RulesTreeView.Nodes.Add(n);
        }

        if (r.Children != null && r.Children.Count > 0)
        {
            foreach (IRule rnew in r.Children)
                AddNewRule(rnew, n);
        }
        else
        {
            RulesTreeView.SelectedNode = n;
            RulesTreeView.Select();
        }

        RestoreSelectedRule();
        CurrentRule.SelectName();
    }

    private void AddNewRule(IRule r, TreeNode tn)
    {
        TreeNode n = new TreeNode(r.Name) { Tag = r };

        tn.Nodes.Add(n);

        if (r.Children != null && r.Children.Count > 0)
        {
            foreach (IRule rnew in r.Children)
                AddNewRule(rnew, n);
        }
        else
        {
            RulesTreeView.SelectedNode = n;
            RulesTreeView.Select();
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="r"></param>
    private void AppendRule(IRule r)
    {
        TreeNode n = new TreeNode(r.Name) { Tag = r };

        RulesTreeView.Nodes.Add(n);

        if (r.Children != null && r.Children.Count > 0)
        {
            foreach (IRule rnew in r.Children)
            {
                AddNewRule(rnew, n);
            }
        }
        else
        {
            RulesTreeView.SelectedNode = n;
            RulesTreeView.Select();
        }
    }

    private void NewSubrule()
    {
        AddNewSubrule(RuleFactory.CreateRule());
    }

    private void AddNewSubrule(IRule r)
    {
        SaveCurrentRule();

        TreeNode s = RulesTreeView.SelectedNode;
        if (s == null)
            return;

        History.Save();

        TreeNode n = new TreeNode(r.Name) { Tag = r };

        s.Nodes.Add(n);
        RulesTreeView.SelectedNode = n;
        RulesTreeView.Select();

        RestoreSelectedRule();
        CurrentRule.SelectName();
    }

    private void NewSubruleInTemplateCallMenuItem_Click(object sender, EventArgs e)
    {
        AddNewSubrule(RuleFactory.CreateInTemplateRule());
    }

    private void NewSubruleTemplateParameterMenuItem_Click(object sender, EventArgs e)
    {
        AddNewSubrule(RuleFactory.CreateTemplateParamRule());
    }

    private void NewRuleMenuItem_Click(object sender, EventArgs e)
    {
        NewRule();
    }

    private void NewSubruleMenuItem_Click(object sender, EventArgs e)
    {
        NewSubrule();
    }

    private void UndoMenuItem_Click(object sender, EventArgs e)
    {
        SaveCurrentRule();
        History.Undo();
        RestoreSelectedRule();
    }

    private void RedoMenuItem_Click(object sender, EventArgs e)
    {
        SaveCurrentRule();
        History.Redo();
        RestoreSelectedRule();
    }

    private void refreshColoursToolStripMenuItem_Click(object sender, EventArgs e)
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

    private void SetTreeViewColours()
    {
        RulesTreeView.BeginUpdate();
        foreach (TreeNode node in RulesTreeView.Nodes)
        {
            SetColours(node);
        }
        RulesTreeView.EndUpdate();
    }

    private static void SetColours(TreeNode rnode)
    {
        IRule temp = (IRule)rnode.Tag;
        SetNodeColour(rnode, temp);

        foreach (TreeNode node in rnode.Nodes)
        {
            IRule temp2 = (IRule)node.Tag;

            SetNodeColour(node, temp2);
            SetColours(node);
        }
    }

    private static void SetNodeColour(TreeNode node, IRule rule)
    {
        node.BackColor = rule.enabled_ ? Color.White : Color.Red;
    }

    private void ReplaceSpecial_Load(object sender, EventArgs e)
    {
        SetTreeViewColours();
    }

    private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Collapsed(false);
    }

    private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Collapsed(true);
    }

    private void Collapsed(bool collapsed)
    {
        RulesTreeView.BeginUpdate();
        foreach (TreeNode node in RulesTreeView.Nodes)
        {
            if (collapsed) node.Collapse();
            else node.ExpandAll();
        }
        RulesTreeView.EndUpdate();
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