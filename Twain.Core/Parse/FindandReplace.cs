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

using System.ComponentModel;
using System.Windows.Forms;
using Twain.Core.Controls;

namespace Twain.Core.Parse;

// TODO: Review title-related comparisons for use of IArticleComparer where
// wiki-specific title normalization is required. Do not apply it to article text.

/// <summary>
/// Provides a form and functions for setting and applying multiple find and replacements on a text string.
/// </summary>
public partial class FindandReplace : Form
{
    public FindandReplace()
    {
        InitializeComponent();
    }

    private readonly HideText _remove = new HideText(true, false, true);

    private List<Replacement> _replacementList = new();
    private List<Replacement> replacementBackup;
    private bool _ignoreLinks;
    private bool _ignoreMore;
    private bool _appendToSummary = true;

    private bool _applyDefault;
    private bool ApplyDefaultFormatting
    {
        get { return _applyDefault; }
        set
        {
            _applyDefault = value;
            dataGridView1.AllowUserToAddRows = value;
        }
    }

    private static Replacement RowToReplacement(
        DataGridViewRow dataGridRow)
    {
        FindReplace.EditorRow row =
            ReadEditorRow(
                dataGridRow);

        return FindReplace.CreateReplacement(
            row);
    }

    /// <summary>
    /// Builds backup list of find and replace entries in case user later wants to restore them.
    /// </summary>
    public void MakeList()
    {
        dataGridView1.EndEdit();

        RebuildReplacementListFromGrid();
    }

    /// <summary>
    /// Rebuilds the configured replacement list from the legacy editor grid.
    /// </summary>
    private void RebuildReplacementListFromGrid()
    {
        ClearReplacementList();

        foreach (DataGridViewRow row in dataGridView1.Rows)
        {
            if (!row.IsNewRow &&
                row.Cells["find"].Value is not null)
            {
                AddReplacement(
                    RowToReplacement(row));
            }
        }
    }

    /// <summary>
    /// Returns the number of replacements (enabled and disabled)
    /// </summary>
    public int NoOfReplacements { get { return _replacementList.Count; } }

    /// <summary>
    /// Returns whether there are any replacements (enabled and disabled)
    /// </summary>
    public bool HasReplacements { get { return NoOfReplacements != 0; } }

    /// <summary>
    /// Returns whether any of the enabled find & replace entries are specified to be run 'after processing'
    /// </summary>
    public bool HasAfterProcessingReplacements
    {
        get
        {
            return HasProcessingReplacements(true);
        }
    }

    /// <summary>
    /// Returns whether any of the enabled find & replace entries are specified to be run at before/after as input
    /// </summary>
    public bool HasProcessingReplacements(bool after)
    {
        foreach (Replacement rep in _replacementList)
        {
            if (rep.Enabled && ((after && rep.BeforeOrAfter) || (!after && !rep.BeforeOrAfter)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Applies a series of defined find and replacements to the supplied article text.
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="editSummary"></param>
    /// <param name="strTitle"></param>
    /// <param name="beforeOrAfter">False if "before", true if "after"</param>
    /// <param name="majorChangesMade"></param>
    /// <returns>The modified article text.</returns>
    public string MultipleFindAndReplace(string articleText, string strTitle, bool beforeOrAfter, ref string editSummary, out bool majorChangesMade)
    {
        majorChangesMade = false;

        if (!HasReplacements)
            return articleText;

        ReplacedSummary = string.Empty;
        RemovedSummary = string.Empty;

        articleText =
            HideIgnoredText(
                articleText);

        foreach (Replacement rep in _replacementList)
        {
            if (!rep.Enabled || rep.BeforeOrAfter != beforeOrAfter)
                continue;

            bool changeMade;
            articleText = PerformFindAndReplace(rep, articleText, strTitle, out changeMade);

            if (changeMade && !rep.Minor)
            {
                majorChangesMade = true;
            }
        }

        articleText =
            RestoreIgnoredText(
                articleText);

        if (AppendToSummary)
        {
            editSummary =
                FindReplace.AppendEditSummary(
                    editSummary,
                    ReplacedSummary,
                    RemovedSummary,
                    Variables.LangCode);
        }

        return articleText;
    }

    /// <summary>
    /// Hides article content that should be excluded from find and replace processing.
    /// </summary>
    /// <param name="articleText">
    /// The article text to prepare.
    /// </param>
    /// <returns>
    /// The article text with configured content hidden.
    /// </returns>
    private string HideIgnoredText(
        string articleText)
    {
        if (IgnoreMore)
        {
            return _remove.HideMore(
                articleText);
        }

        if (IgnoreLinks)
        {
            return _remove.Hide(
                articleText);
        }

        return articleText;
    }

    /// <summary>
    /// Restores article content hidden before find and replace processing.
    /// </summary>
    /// <param name="articleText">
    /// The processed article text.
    /// </param>
    /// <returns>
    /// The article text with previously hidden content restored.
    /// </returns>
    private string RestoreIgnoredText(
        string articleText)
    {
        if (IgnoreMore)
        {
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_24#FormatException_in_HideText.AddBackMore
            // FIXME: Usages of IgnoreMore with number (or M) replacement done in the FindAndReplace can cause corruption
            // e.g. Replacing 2 with "" ⌊⌊⌊⌊M2⌋⌋⌋⌋ becomes ⌊⌊⌊⌊M⌋⌋⌋⌋
            // This cannot then be added back
            return _remove.AddBackMore(
                articleText);
        }

        if (IgnoreLinks)
        {
            return _remove.AddBack(
                articleText);
        }

        return articleText;
    }

    public string RemovedSummary { get; private set; }
    public string ReplacedSummary { get; private set; }

    /// <summary>
    /// Executes a single find &amp; replace rule against the article text.
    /// First applies keywords to the find and replace portions of the rule,
    /// then executes the rule.
    /// </summary>
    /// <param name="rep">
    /// The find and replace rule to execute.
    /// </param>
    /// <param name="articleText">
    /// The article text.
    /// </param>
    /// <param name="articleTitle">
    /// The article title.
    /// </param>
    /// <param name="changeMade">
    /// Whether the find and replace rule caused changes to the article text.
    /// </param>
    /// <returns>
    /// The updated article text.
    /// </returns>
    public string PerformFindAndReplace(
        Replacement rep,
        string articleText,
        string articleTitle,
        out bool changeMade)
    {
        ArgumentNullException.ThrowIfNull(rep);

        FindReplace.PreparedReplacement prepared =
            FindReplace.PrepareReplacement(
                rep,
                articleTitle);

        FindReplace.ReplacementResult result =
            FindReplace.ExecuteReplacement(
                articleText,
                prepared.Find,
                prepared.Replace,
                rep.RegularExpressionOptions);

        string summarySeparator =
            FindReplace.GetSummarySeparator(
                Variables.LangCode);

        FindReplace.SummaryResult summaries =
            FindReplace.UpdateSummaries(
                result,
                ReplacedSummary,
                RemovedSummary,
                summarySeparator,
                FindReplace.GetSummaryArrow(
                    Variables.RTL));

        ReplacedSummary =
            summaries.ReplacedSummary;

        RemovedSummary =
            summaries.RemovedSummary;

        changeMade =
            result.ChangeMade;

        return result.Text;
    }

    private void btnDone_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
    }

    private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        lnkWpRE.LinkVisited = true;
        Tools.OpenENArticleInBrowser("Regular_expression", false);
    }

    private void lblMsdn_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        lblMsdn.LinkVisited = true;
        Tools.OpenURLInBrowser("http://msdn.microsoft.com/en-us/library/hs600312.aspx");
    }

    private void btnClear_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show("Do you really want to clear the whole table?", "Really clear?", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
        {
            Clear();
        }
    }

    /// <summary>
    /// Clears the configured replacement entries and the legacy editor grid.
    /// </summary>
    public void Clear()
    {
        ClearReplacementList();
        dataGridView1.Rows.Clear();
    }

    /// <summary>
    /// Clears the configured replacement entries.
    /// </summary>
    private void ClearReplacementList()
    {
        _replacementList.Clear();
    }

    /// <summary>
    /// Adds a replacement entry to the configured replacement list.
    /// </summary>
    /// <param name="replacement">
    /// The replacement entry to add.
    /// </param>
    private void AddReplacement(
        Replacement replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);

        _replacementList.Add(replacement);
    }

    #region loading/saving


    /// <summary>
    /// Loads a single replacement entry into the find and replace data grid.
    /// </summary>
    /// <param name="r">The replacement entry.</param>
    /// <param name="decodeRequired">
    /// Whether decoding of newlines in find and replace text is required.
    /// Required when loading settings from XML and not required when restoring
    /// from backup after Cancel.
    /// </param>
    public void AddNew(
        Replacement r,
        bool decodeRequired)
    {
        FindReplace.EditorRow row =
            FindReplace.CreateEditorRow(
                r,
                decodeRequired);

        AddEditorRowToGrid(row);

        AddReplacement(r);
    }

    /// <summary>
    /// Loads a single replacement entry into the find and replace data grid.
    /// </summary>
    /// <param name="r">The replacement entry.</param>
    public void AddNew(
        Replacement r)
    {
        AddNew(
            r,
            true);
    }

    /// <summary>
    /// Loads list of replacement entries into the find and replace data grid
    /// </summary>
    /// <param name="rList"></param>
    public void AddNew(List<Replacement> rList)
    {
        foreach (Replacement r in rList)
        {
            AddNew(r);
        }
    }

    /// <summary>
    /// Adds editable replacement values to the legacy data grid.
    /// </summary>
    /// <param name="row">
    /// The editable replacement values to add.
    /// </param>
    private void AddEditorRowToGrid(
        FindReplace.EditorRow row)
    {
        dataGridView1.Rows.Add(
            row.Find,
            row.Replace,
            row.CaseSensitive,
            row.IsRegex,
            row.Multiline,
            row.Singleline,
            row.Minor,
            row.BeforeOrAfter,
            row.Enabled,
            row.Comment);
    }


    /// <summary>
    /// Reads editable replacement values from a legacy data grid row.
    /// </summary>
    private static FindReplace.EditorRow ReadEditorRow(
        DataGridViewRow dataGridRow)
    {
        if (dataGridRow.Cells["replace"].Value == null)
        {
            dataGridRow.Cells["replace"].Value =
                string.Empty;
        }

        return new FindReplace.EditorRow(
            dataGridRow.Cells["find"].Value.ToString(),
            dataGridRow.Cells["replace"].Value.ToString(),
            (bool)dataGridRow.Cells["casesensitive"].FormattedValue,
            (bool)dataGridRow.Cells["regex"].FormattedValue,
            (bool)dataGridRow.Cells["multi"].FormattedValue,
            (bool)dataGridRow.Cells["single"].FormattedValue,
            (bool)dataGridRow.Cells["minor"].FormattedValue,
            (bool)dataGridRow.Cells["BeforeOrAfter"].FormattedValue,
            (bool)dataGridRow.Cells["enabled"].FormattedValue,
            (string)dataGridRow.Cells["comment"].FormattedValue ?? "");
    }

    /// <summary>
    /// Gets a copy of the configured find and replace entries.
    /// </summary>
    /// <returns>
    /// A new list containing the configured replacements.
    /// </returns>
    public List<Replacement> GetList()
    {
        return [.. _replacementList];
    }

    /// <summary>
    /// Gets or sets whether the replacements ignore external links and images.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IgnoreLinks
    {
        get => _ignoreLinks;
        set
        {
            _ignoreLinks = value;
            chkIgnoreLinks.Checked = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the replacements ignore headings, internal link
    /// targets, templates, and refs.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IgnoreMore
    {
        get => _ignoreMore;
        set
        {
            _ignoreMore = value;
            chkIgnoreMore.Checked = value;
        }
    }

    private void chkIgnoreLinks_CheckedChanged(
    object sender,
    EventArgs e)
    {
        _ignoreLinks =
            chkIgnoreLinks.Checked;
    }

    private void chkIgnoreMore_CheckedChanged(
        object sender,
        EventArgs e)
    {
        _ignoreMore =
            chkIgnoreMore.Checked;

        if (chkIgnoreMore.Checked)
        {
            chkIgnoreLinks.Checked = true;
            chkIgnoreLinks.Enabled = false;
        }
        else
        {
            chkIgnoreLinks.Enabled = true;
        }
    }

    private void chkAddToSummary_CheckedChanged(
        object sender,
        EventArgs e)
    {
        _appendToSummary =
            chkAddToSummary.Checked;
    }

    /// <summary>
    /// Gets or sets whether the summary should be used.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AppendToSummary
    {
        get => _appendToSummary;
        set
        {
            _appendToSummary = value;
            chkAddToSummary.Checked = value;
        }
    }

    #endregion

    #region Events

    private void ChangeChecked(string col, bool value)
    {
        dataGridView1.EndEdit();
        foreach (DataGridViewRow r in dataGridView1.Rows)
        {
            if (r.IsNewRow)
                break;

            r.Cells[col].Value = value;
        }
    }

    private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
    {
        if (!ApplyDefaultFormatting)
            return;

        dataGridView1.Rows[e.RowIndex].Cells["enabled"].Value = true;
    }

    private void FindandReplace_Shown(object sender, EventArgs e)
    {
        DialogResult = DialogResult.None;
        ApplyDefaultFormatting = true;
    }

    private void FindandReplace_FormClosing(object sender, FormClosingEventArgs e)
    {
        ApplyDefaultFormatting = false;
        Hide();
        if (replacementBackup != null && DialogResult == DialogResult.Cancel)
        {
            dataGridView1.Rows.Clear();
            foreach (Replacement r in replacementBackup)
            {
                AddNew(r, false);
            }
            _replacementList.Clear();
            _replacementList = replacementBackup;
            replacementBackup = null;
            return;
        }
        MakeList();
    }

    #endregion

    #region Context menu

    private void allCaseSensitiveToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("casesensitive", true);
    }

    private void uncheckAllCaseSensitiveToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("casesensitive", false);
    }

    private void checkAllRegularExpressionsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("regex", true);
    }

    private void uncheckAllRegularExpressionsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("regex", false);
    }

    private void checkAllMultlineToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("multi", true);
    }

    private void uncheckAllMultilineToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("multi", false);
    }

    private void enableAllToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("enabled", true);
    }

    private void disableAllToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("enabled", false);
    }

    private void checkAllSinglelineToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("single", true);
    }

    private void uncheckAllSinglelineToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("single", false);
    }

    private void checkAllBeforeOrAfterToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("beforeorafter", true);
    }

    private void uncheckAllBeforeOrAfterToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("beforeorafter", false);
    }

    private void deleteRowToolStripMenuItem_Click(object sender, EventArgs e)
    {
        while (dataGridView1.SelectedRows.Count > 0)
        {
            if (dataGridView1.Rows[dataGridView1.SelectedRows[0].Index].IsNewRow)
            {
                dataGridView1.SelectedRows[0].Selected = false;
                continue;
            }

            dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
        }
    }

    private void FindAndReplaceContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
        deleteRowToolStripMenuItem.Enabled = (dataGridView1.SelectedRows.Count > 0);
        testRegexToolStripMenuItem.Enabled = createRetfRuleToolStripMenuItem.Enabled =
            ((dataGridView1.CurrentRow != null)
            && ((bool)dataGridView1.CurrentRow.Cells["regex"].FormattedValue));
    }

    private void testRegexToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DataGridViewRow row = dataGridView1.CurrentRow;

        if (row == null) return;

        using (RegexTester t = new RegexTester(true))
        {
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            t.Find = (string)row.Cells["find"].Value;
            t.Replace = (string)row.Cells["replace"].Value;
            t.Multiline = (bool)row.Cells["multi"].FormattedValue;
            t.Singleline = (bool)row.Cells["single"].FormattedValue;
            t.IgnoreCase = !(bool)row.Cells["casesensitive"].FormattedValue;

            if (Variables.MainForm != null && Variables.MainForm.EditBox.Enabled)
                t.ArticleText = Variables.MainForm.EditBox.Text;

            if (t.ShowDialog(this) != DialogResult.OK) return;
            row.Cells["find"].Value = t.Find;
            row.Cells["replace"].Value = t.Replace;
            row.Cells["multi"].Value = t.Multiline;
            row.Cells["single"].Value = t.Singleline;
            row.Cells["casesensitive"].Value = !t.IgnoreCase;
            dataGridView1.RefreshEdit();
        }
    }
    #endregion

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        btnSearch.Enabled = txtSearch.Text.Length > 0;
    }

    // performs a search for next match after current cell, or first match if no matches after current cell (last match found)
    private void btnSearch_Click(object sender, EventArgs e)
    {
        // row index of currently highlighted cell (if any)
        int currentRowIndex = dataGridView1.CurrentCell == null
            ? -1
            : dataGridView1.CurrentCell.RowIndex;

        // get list of all cells with text match
        List<DataGridViewCell> cells = new();

        // search by row then column
        for (int j = 0; j < dataGridView1.Rows.Count; j++)
        {
            for (int i = 0; i < 2; i++)
            {
                DataGridViewCell c = dataGridView1.Rows[j].Cells[i];
                if (c.Value != null && c.Value.ToString().Contains(txtSearch.Text))
                    cells.Add(c);
            }
        }

        // nothing to do if no matches
        if (cells.Count == 0)
            return;

        DataGridViewCell c2;

        // if no current grid focus, pick first match
        if (currentRowIndex < 0)
        {
            c2 = cells[0];
        }
        else
        {
            // look for a match on a row after current grid focus
            // if no match, go back to start of grid i.e. first match
            List<DataGridViewCell> cells2 = cells.FindAll(x => x.RowIndex > currentRowIndex);
            if (cells2.Count > 0)
                c2 = cells2[0];
            else
                c2 = cells[0];
        }

        // select / focus / scroll to selected match
        dataGridView1.ClearSelection();
        dataGridView1.EndEdit();
        dataGridView1.CurrentCell = c2;
        if (!c2.Displayed)
        {
            dataGridView1.FirstDisplayedScrollingRowIndex = c2.RowIndex;
        }
        dataGridView1.Focus();
        dataGridView1.BeginEdit(false);
    }

    private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == 13)
        {
            e.Handled = true;
            btnSearch_Click(null, null);
        }
    }

    private void addRowToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (dataGridView1.SelectedRows.Count > 0)
            dataGridView1.Rows.Insert(dataGridView1.SelectedRows[0].Index);
        else
            dataGridView1.Rows.Add();
    }

    private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
    {
        dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        foreach (DataGridViewRow row in dataGridView1.SelectedRows)
        {
            if (row.Index > 0 && row.Index < (dataGridView1.Rows.Count - 1))
            {
                int index = row.Index;
                DataGridViewRow tmp = row;
                dataGridView1.Rows.Remove(row);
                ApplyDefaultFormatting = false;
                dataGridView1.Rows.Insert(index - 1, tmp);
                ApplyDefaultFormatting = true;
                SelectRowAndFocusColumn(index - 1);
            }
        }
    }

    private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
    {
        dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        foreach (DataGridViewRow row in dataGridView1.SelectedRows)
        {
            if (row.Index < (dataGridView1.Rows.Count - 2))
            {
                int index = row.Index;
                DataGridViewRow tmp = row;
                dataGridView1.Rows.Remove(row);
                ApplyDefaultFormatting = false;
                dataGridView1.Rows.Insert(index + 1, tmp);
                ApplyDefaultFormatting = true;
                SelectRowAndFocusColumn(index + 1);
            }
        }
    }

    private void moveToTopToolStripMenuItem_Click(object sender, EventArgs e)
    {
        dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        foreach (DataGridViewRow row in dataGridView1.SelectedRows)
        {
            if (row.Index > 0 && row.Index < (dataGridView1.Rows.Count - 1))
            {
                DataGridViewRow tmp = row;
                dataGridView1.Rows.Remove(row);
                ApplyDefaultFormatting = false;
                dataGridView1.Rows.Insert(0, tmp);
                ApplyDefaultFormatting = true;
                SelectRowAndFocusColumn(0);
            }
        }
    }

    private void moveToBottomToolStripMenuItem_Click(object sender, EventArgs e)
    {
        dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        foreach (DataGridViewRow row in dataGridView1.SelectedRows)
        {
            if (row.Index < (dataGridView1.Rows.Count - 2))
            {
                DataGridViewRow tmp = row;
                dataGridView1.Rows.Remove(row);
                ApplyDefaultFormatting = false;
                dataGridView1.Rows.Add(tmp);
                ApplyDefaultFormatting = true;
                SelectRowAndFocusColumn(dataGridView1.Rows.Count - 2); // zero based row and always one blank at end
            }
        }
    }

    /// <summary>
    /// After a user has just moved a row: selects the moved row and returns focus to the previously selected column
    /// </summary>
    /// <param name='row'>
    /// Moved row number
    /// </param>
    private void SelectRowAndFocusColumn(int row)
    {
        dataGridView1.Rows[row].Selected = true;
        dataGridView1.CurrentCell = dataGridView1[dataGridView1.CurrentCell.ColumnIndex, row]; // focus on column that currently has focus, but in the moved row
    }

    private void createRetfRuleToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DataGridViewRow row = dataGridView1.CurrentRow;

        if (row == null)
            return;

        string typoName = (string)row.Cells["Comment"].Value;
        if (string.IsNullOrEmpty(typoName))
            typoName = "<enter a name>";

        Tools.CopyToClipboard(RegExTypoFix.CreateRule((string)row.Cells["find"].Value,
                                                      (string)row.Cells["replace"].Value, typoName));
    }

    private void checkAllMinorToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("minor", true);
    }

    private void uncheckAllMinorToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ChangeChecked("minor", false);
    }

    private void FindandReplace_VisibleChanged(object sender, EventArgs e)
    {
        if (Visible)
        {
            replacementBackup = _replacementList.ConvertAll(repl => new Replacement(repl));
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
    }
}

/// <summary>
///
/// </summary>
public class Replacement
{
    public Replacement()
    {
        RegularExpressionOptions = RegexOptions.None;
    }

    public Replacement(string find, string replace, bool isRegex, bool enabled, bool minor, bool beforeOrAfter,
                       RegexOptions regularExpressionOptions, string comment)
    {
        Find = find;
        Replace = replace;
        IsRegex = isRegex;
        Enabled = enabled;
        Minor = minor;
        BeforeOrAfter = beforeOrAfter;
        RegularExpressionOptions = regularExpressionOptions;
        Comment = comment;
    }

    public Replacement(Replacement repl)
        : this(
            repl.Find, repl.Replace, repl.IsRegex, repl.Enabled, repl.Minor,
            repl.BeforeOrAfter, repl.RegularExpressionOptions, repl.Comment)
    {
    }

    public string Find,
        Replace, Comment;

    public bool IsRegex,
        Enabled, Minor, BeforeOrAfter;

    public RegexOptions RegularExpressionOptions;
}