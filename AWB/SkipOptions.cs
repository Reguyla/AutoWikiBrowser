/*
Autowikibrowser
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

using Twain.Core;
using Twain.Core.Plugin;
using Twain.Core.Settings;

namespace AutoWikiBrowser;

// TODO (maintainability): Consider introducing a SkipOption model or enum
// to define the available skip options in one place. This would eliminate
// duplicated option IDs, descriptions, and property mappings, making it
// easier to add, remove, or reorder skip options while preserving their
// stable identifiers.
///
//TODO (optimization): If additional skip options are introduced, consider
// maintaining a mapping between option IDs and CheckedListBox indexes to
// avoid scanning the list each time IsOptionChecked() is called. The current
// linear search is appropriate for the small, fixed number of options.
/// <summary>
/// Provides options for skipping articles when selected automatic processing
/// operations did not make a change.
/// </summary>
/// <remarks>
/// The form is hidden rather than disposed when closed so that its selected
/// options remain available to the main AWB workflow.
/// </remarks>
internal sealed partial class SkipOptions : Form, ISkipOptions
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipOptions"/> form and
    /// populates the available skip conditions.
    /// </summary>
    public SkipOptions()
    {
        InitializeComponent();

        foreach ((int id, string description) in
                 SkipOptionsHelper.AvailableOptions)
        {
            skipListBox.Items.Add(
                new CheckedBoxItem
                {
                    ID = id,
                    Description = description
                });
        }
    }

    #region Properties

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoBoldTitle =>
    IsOptionChecked(
        SkipOptionsHelper.BoldTitleOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoBulletedLink =>
    IsOptionChecked(
        SkipOptionsHelper.BulletedExternalLinkOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoBadLink =>
    IsOptionChecked(
        SkipOptionsHelper.BadLinksOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoUnicode =>
    IsOptionChecked(
        SkipOptionsHelper.UnicodeOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoTag =>
    IsOptionChecked(
        SkipOptionsHelper.AutoTagOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoHeaderError =>
    IsOptionChecked(
        SkipOptionsHelper.HeaderErrorOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoDefaultSortAdded =>
    IsOptionChecked(
        SkipOptionsHelper.DefaultSortOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoUserTalkTemplatesSubstd =>
    IsOptionChecked(
        SkipOptionsHelper.UserTalkTemplatesOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoCiteTemplateDatesFixed =>
    IsOptionChecked(
        SkipOptionsHelper.CitationTemplateDatesOptionId);

    /// <inheritdoc />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipNoPeopleCategoriesFixed =>
    IsOptionChecked(
        SkipOptionsHelper.HumanCategoriesOptionId);

    // TODO (performance): If the number of skip options grows significantly,
    // consider converting the supplied List<int> to a HashSet<int> before
    // iterating through the checked list. This would reduce repeated
    // List.Contains() lookups from O(n) to O(1). With the current ten
    // options, the existing implementation is simple and sufficiently fast.

    /// <summary>
    /// Gets or sets the identifiers of the currently selected skip options.
    /// </summary>
    /// <remarks>
    /// This property exposes the checked state of the skip-options list for
    /// AWB's runtime settings. Setting this property updates the checked state
    /// of every displayed option and clears the transient <c>Check All</c> and
    /// <c>Check None</c> controls. It is not intended to be serialized
    /// independently by the Windows Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<int> SelectedItems
    {
        get
        {
            List<int> selectedItems = new();

            for (int index = 0;
                 index < skipListBox.Items.Count;
                 index++)
            {
                if (!skipListBox.GetItemChecked(index))
                {
                    continue;
                }

                CheckedBoxItem item =
                    (CheckedBoxItem)skipListBox.Items[index];

                selectedItems.Add(item.ID);
            }

            return selectedItems;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            CheckAll.Checked = false;
            CheckNone.Checked = false;

            for (int index = 0;
                 index < skipListBox.Items.Count;
                 index++)
            {
                CheckedBoxItem item =
                    (CheckedBoxItem)skipListBox.Items[index];

                skipListBox.SetItemChecked(
                    index,
                    value.Contains(item.ID));
            }
        }
    }

    #endregion

    #region Event handlers

    /// <summary>
    /// Prevents the form from being disposed and hides it instead.
    /// </summary>
    private void SkipOptions_FormClosing(
        object sender,
        FormClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// Hides the skip-options form.
    /// </summary>
    private void btnClose_Click(
        object sender,
        EventArgs e)
    {
        Hide();
    }

    /// <summary>
    /// Selects every available skip option when the Check All control is
    /// selected.
    /// </summary>
    private void CheckAll_CheckedChanged(
        object sender,
        EventArgs e)
    {
        if (!CheckAll.Checked)
        {
            return;
        }

        CheckNone.Checked = false;
        SetCheckboxes(true);
    }

    /// <summary>
    /// Clears every available skip option when the Check None control is
    /// selected.
    /// </summary>
    private void CheckNone_CheckedChanged(
        object sender,
        EventArgs e)
    {
        if (!CheckNone.Checked)
        {
            return;
        }

        CheckAll.Checked = false;
        SetCheckboxes(false);
    }

    #endregion

    /// <summary>
    /// Determines whether the skip option with the specified identifier is
    /// currently selected.
    /// </summary>
    /// <param name="optionId">The stable identifier of the option.</param>
    /// <returns>
    /// <see langword="true"/> when the option exists and is checked; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool IsOptionChecked(int optionId)
    {
        for (int index = 0;
             index < skipListBox.Items.Count;
             index++)
        {
            if (skipListBox.Items[index] is CheckedBoxItem item &&
                item.ID == optionId)
            {
                return skipListBox.GetItemChecked(index);
            }
        }

        return false;
    }

    /// <summary>
    /// Sets the checked state of every skip option.
    /// </summary>
    /// <param name="isChecked">
    /// The checked state to apply to every option.
    /// </param>
    private void SetCheckboxes(bool isChecked)
    {
        for (int index = 0;
             index < skipListBox.Items.Count;
             index++)
        {
            skipListBox.SetItemChecked(
                index,
                isChecked);
        }
    }
}