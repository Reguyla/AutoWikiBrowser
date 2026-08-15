/*
Copyright (C) 2007 Martin Richards

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
*/

using System.Windows.Forms;

namespace Twain.Core.ReplaceSpecial;

/// <summary>
/// Provides the WinForms editor used to configure an
/// <see cref="InTemplateRule"/>.
/// </summary>
public partial class InTemplateRuleControl : UserControl
{
    private readonly IRuleControlOwner Owner;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InTemplateRuleControl"/> class.
    /// </summary>
    /// <param name="owner">
    /// The owner that receives notifications when the rule name changes.
    /// </param>
    public InTemplateRuleControl(IRuleControlOwner owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        InitializeComponent();

        Owner = owner;

        Anchor =
            AnchorStyles.Bottom |
            AnchorStyles.Left |
            AnchorStyles.Right |
            AnchorStyles.Top;

        UpdateEndabledStates();
    }

    /// <summary>
    /// Sets the name displayed in the rule name text box.
    /// </summary>
    /// <param name="name">
    /// The rule name to display.
    /// </param>
    public void SetName(string name)
    {
        NameTextbox.Text = name;
    }

    /// <summary>
    /// Selects all text in the rule name text box.
    /// </summary>
    public void SelectName()
    {
        NameTextbox.Select();
        NameTextbox.SelectAll();
    }

    /// <summary>
    /// Saves the values currently displayed by the control to the specified
    /// rule.
    /// </summary>
    /// <param name="rule">
    /// The rule to update.
    /// </param>
    public void SaveToRule(InTemplateRule rule)
    {
        if (rule is null)
        {
            return;
        }

        rule.enabled_ = RuleEnabledCheckBox.Checked;
        rule.Name = NameTextbox.Text.Trim();
        rule.ReplaceWith_ = ReplaceWithTextBox.Text.Trim();
        rule.DoReplace_ = ReplaceCheckBox.Checked;

        rule.TemplateNames_.Clear();

        foreach (string alias in AliasesListBox.Items)
        {
            rule.TemplateNames_.Add(alias);
        }
    }

    /// <summary>
    /// Restores the specified rule values to the editor control.
    /// </summary>
    /// <param name="rule">
    /// The rule whose values should be displayed.
    /// </param>
    public void RestoreFromRule(InTemplateRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        NameTextbox.Text = rule.Name;
        RuleEnabledCheckBox.Checked = rule.enabled_;
        ReplaceWithTextBox.Text = rule.ReplaceWith_;
        ReplaceCheckBox.Checked = rule.DoReplace_;

        AliasesListBox.BeginUpdate();

        try
        {
            AliasesListBox.Items.Clear();

            foreach (string alias in rule.TemplateNames_)
            {
                AliasesListBox.Items.Add(alias);
            }
        }
        finally
        {
            AliasesListBox.EndUpdate();
        }

        UpdateEndabledStates();
    }

    /// <summary>
    /// Notifies the control owner when the rule name changes.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void NameTextbox_TextChanged(object sender, EventArgs e)
    {
        Owner.NameChanged(
            this,
            NameTextbox.Text.Trim());
    }

    /// <summary>
    /// Selects the complete rule name when the name text box is
    /// double-clicked.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void NameTextbox_DoubleClick(object sender, EventArgs e)
    {
        NameTextbox.SelectAll();
    }

    /// <summary>
    /// Updates the enabled state of replacement controls when replacement is
    /// enabled or disabled.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void ReplaceCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        UpdateEndabledStates();
    }

    /// <summary>
    /// Updates the enabled state of controls based on the current rule
    /// settings and alias selection.
    /// </summary>
    private void UpdateEndabledStates()
    {
        ReplaceWithTextBox.Enabled = ReplaceCheckBox.Checked;
        DeleteButton.Enabled = AliasesListBox.SelectedItem is not null;
    }

    /// <summary>
    /// Adds the entered template alias when it is non-empty and has not
    /// already been added.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void AddButton_Click(object sender, EventArgs e)
    {
        string alias = AliasTextBox.Text;

        if (string.IsNullOrEmpty(alias))
        {
            return;
        }

        if (!AliasesListBox.Items.Contains(alias))
        {
            AliasesListBox.Items.Add(alias);
        }

        AliasTextBox.Text = string.Empty;
        AliasTextBox.Select();

        UpdateEndabledStates();
    }

    /// <summary>
    /// Removes the selected template alias and selects the nearest remaining
    /// alias when possible.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void DeleteButton_Click(object sender, EventArgs e)
    {
        if (AliasesListBox.SelectedItem is null)
        {
            return;
        }

        int selectedIndex = AliasesListBox.SelectedIndex;

        AliasesListBox.Items.Remove(
            AliasesListBox.SelectedItem);

        int count = AliasesListBox.Items.Count;

        if (count > 0)
        {
            if (selectedIndex >= count)
            {
                selectedIndex = count - 1;
            }

            AliasesListBox.SelectedIndex = selectedIndex;
        }

        UpdateEndabledStates();
    }

    /// <summary>
    /// Updates the available alias actions when the selected alias changes.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void AliasesListBox_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        UpdateEndabledStates();
    }
}