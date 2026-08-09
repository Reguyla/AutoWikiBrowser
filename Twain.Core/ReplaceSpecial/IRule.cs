/*
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

using System.Windows.Forms;

namespace Twain.Core.ReplaceSpecial;

// TODO: Separate rule behavior from WinForms control and TreeNode management.
// The rule model currently creates, owns, and manipulates UI controls directly,
// which prevents ReplaceSpecial rules from being UI-framework independent.
//
// TODO: Consider renaming IRule during a future compatibility-breaking cleanup.
// Despite the "I" prefix, this type is an abstract base class rather than an
// interface.
//
// TODO: Review public mutable fields such as enabled_ and Children. Prefer
// properties or encapsulated collections once XML serialization and existing
// callers have been verified.

/// <summary>
/// Defines the base behavior for a ReplaceSpecial rule, including rule
/// persistence, cloning, application, and its associated editor control.
/// </summary>
/// <remarks>
/// Derived rule types provide both the text-processing behavior of the rule and
/// the WinForms controls used to configure it.
/// </remarks>
[System.Xml.Serialization.XmlInclude(typeof(Rule))]
[System.Xml.Serialization.XmlInclude(typeof(TemplateParamRule))]
[System.Xml.Serialization.XmlInclude(typeof(InTemplateRule))]
public abstract class IRule : ICloneable
{
    /// <summary>
    /// Indicates whether the rule is enabled.
    /// </summary>
    /// <remarks>
    /// This legacy public field is retained for compatibility with existing
    /// serialization and callers.
    /// </remarks>
    public bool enabled_ = true;

    /// <summary>
    /// Stores child rules associated with this rule.
    /// </summary>
    /// <remarks>
    /// This legacy mutable collection field is retained for compatibility.
    /// </remarks>
    public List<IRule> Children;

    /// <summary>
    /// Gets or sets the display name of the rule.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the WinForms control currently associated with the rule.
    /// </summary>
    /// <returns>
    /// The rule's associated control, or <see langword="null"/> when no control
    /// has been created.
    /// </returns>
    public abstract Control GetControl();

    /// <summary>
    /// Removes the rule's reference to its associated control without
    /// disposing the control.
    /// </summary>
    public abstract void ForgetControl();

    /// <summary>
    /// Selects or focuses the portion of the rule editor used to edit the
    /// rule name.
    /// </summary>
    public abstract void SelectName();

    /// <summary>
    /// Saves the current editable state of the rule.
    /// </summary>
    public abstract void Save();

    /// <summary>
    /// Restores the rule to its previously saved state.
    /// </summary>
    public abstract void Restore();

    /// <summary>
    /// Creates the WinForms control used to configure the rule.
    /// </summary>
    /// <param name="owner">
    /// The object that owns and coordinates the rule control.
    /// </param>
    /// <param name="collection">
    /// The control collection to which the new rule control should be added.
    /// </param>
    /// <param name="pos">
    /// The position at which the control should be created.
    /// </param>
    /// <returns>
    /// The newly created rule control.
    /// </returns>
    public abstract Control CreateControl(
        IRuleControlOwner owner,
        Control.ControlCollection collection,
        System.Drawing.Point pos);

    /// <summary>
    /// Detaches and disposes the WinForms control associated with this rule.
    /// </summary>
    public void DisposeControl()
    {
        Control control = GetControl();

        if (control == null)
            return;

        ForgetControl();

        control.Hide();

        if (control.Parent != null)
        {
            control.Parent.Controls.Remove(control);
        }

        control.Dispose();
    }

    /// <summary>
    /// Applies the rule to the supplied text.
    /// </summary>
    /// <param name="tn">
    /// The tree node associated with the rule being applied.
    /// </param>
    /// <param name="text">
    /// The source text to process.
    /// </param>
    /// <param name="title">
    /// The title associated with the text being processed.
    /// </param>
    /// <returns>
    /// The text produced after applying the rule.
    /// </returns>
    public abstract string Apply(
        TreeNode tn,
        string text,
        string title);

    /// <summary>
    /// Creates a copy of the rule.
    /// </summary>
    /// <returns>
    /// A cloned rule instance.
    /// </returns>
    public abstract object Clone();

    /// <summary>
    /// Creates a deep clone of a rule tree node, including cloned rule objects
    /// stored in each node's <see cref="TreeNode.Tag"/> property.
    /// </summary>
    /// <param name="tn">
    /// The tree node to clone.
    /// </param>
    /// <returns>
    /// The cloned tree node, or <see langword="null"/> when
    /// <paramref name="tn"/> is <see langword="null"/>.
    /// </returns>
    public static TreeNode CloneTreeNode(TreeNode tn)
    {
        if (tn == null)
            return null;

        TreeNode clonedNode =
            (TreeNode)tn.Clone();

        CloneTags(clonedNode);

        return clonedNode;
    }

    /// <summary>
    /// Replaces each rule stored in a tree node's
    /// <see cref="TreeNode.Tag"/> property with a cloned instance and applies
    /// the same operation recursively to all child nodes.
    /// </summary>
    /// <param name="treeNode">
    /// The tree node whose rule tags should be cloned.
    /// </param>
    private static void CloneTags(TreeNode treeNode)
    {
        IRule rule =
            (IRule)treeNode.Tag;

        treeNode.Tag = rule.Clone();

        foreach (TreeNode childNode in treeNode.Nodes)
        {
            CloneTags(childNode);
        }
    }
}