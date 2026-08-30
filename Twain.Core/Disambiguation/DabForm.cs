/*
Copyright (C) 2007 Max Semenik

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

namespace Twain.Core.Disambiguation;

public partial class DabForm : Form
{
    public DabForm(Session session)
    {
        InitializeComponent();
        Session = session;
    }

    /// <summary>
    /// if true, all processing should be immediately halted
    /// </summary>
    public bool Abort;

    private bool BotMode;

    private readonly List<string> Variants = new();
    private string ArticleTitle;
    private Regex Search;

    private readonly List<DabControl> Dabs = new();

    private static int SavedWidth, SavedHeight, SavedLeft, SavedTop;
    private bool NoSave = true;

    private readonly Session Session;

    /// <summary>
    /// Displays a form that prompts user for disambiguation
    /// if no disambiguation needed, immediately returns
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="dabLink">link to be disambiguated</param>
    /// <param name="dabVariants">variants of disambiguation</param>
    /// <param name="contextChars">number of chars each side from link in the context box</param>
    /// <param name="botMode">whether AWB saves pages automatically</param>
    /// <param name="skip">returns true when no disambiguation made</param>
    /// <returns></returns>
    public string Disambiguate(string articleText, string articleTitle, string dabLink,
        string[] dabVariants, int contextChars, bool botMode, out bool skip)
    {
        Variants.Clear();
        Dabs.Clear();

        skip = true;

        DisambiguationProcessor.DisambiguationPreparation preparation =
            DisambiguationProcessor.Prepare(
                articleText,
                dabLink,
                dabVariants);

        if (preparation.Variants.Count == 0)
            return articleText;

        if (preparation.Matches.Count == 0)
            return articleText;

        Variants.AddRange(preparation.Variants);

        BotMode = botMode;
        Search = preparation.Search;
        ArticleTitle = articleTitle;

        MatchCollection matches = preparation.Matches;

        List<DisambiguationProcessor.DisambiguationItemPreparation> items =
            DisambiguationProcessor.PrepareItems(
                articleText,
                matches,
                contextChars);

        foreach (DisambiguationProcessor.DisambiguationItemPreparation item in items)
        {
            DabControl c = new DabControl(
                articleText,
                item,
                Variants);

            c.Changed += OnUserInput;
            tableLayout.Controls.Add(c);
            Dabs.Add(c);
        }

        switch (ShowDialog(Variables.MainForm as Form))
        {
            case DialogResult.OK:
                break; // proceed further
            case DialogResult.Abort:
                Abort = true;
                goto default;
            default: //DialogResult.Cancel
                return articleText;
        }

        List<DisambiguationProcessor.DisambiguationResult> results = Dabs
            .Select(
                dab => new DisambiguationProcessor.DisambiguationResult(
                    dab.NoChange,
                    dab.Result))
            .ToList();

        string newText = DisambiguationProcessor.ApplyResults(
            articleText,
            Search,
            results);

        if (!newText.Equals(articleText))
            skip = false;

        return newText;
    }

    private void btnResetAll_Click(object sender, EventArgs e)
    {
        foreach (DabControl d in Dabs)
        {
            d.Reset();
        }
    }

    private void btnUndoAll_Click(object sender, EventArgs e)
    {
        foreach (DabControl d in Dabs)
        {
            d.Undo();
        }
    }

    private void OnUserInput(object sender, EventArgs e)
    {
        btnDone.Enabled = Dabs.Aggregate(true, (current, d) => current & d.CanSave);
    }

    private void DabForm_Load(object sender, EventArgs e)
    {
        Text += " — " + ArticleTitle;
        if (SavedWidth != 0)
        {
            Width = SavedWidth;
            Height = SavedHeight;
        }
        if (SavedLeft != 0)
        {
            Left = SavedLeft;
            Top = SavedTop;
        }
        NoSave = false;
        Dabs[0].Select();
    }

    private void DabForm_Move(object sender, EventArgs e)
    {
        if (!NoSave)
        {
            SavedLeft = Left;
            SavedTop = Top;
        }
    }

    private void DabForm_Resize(object sender, EventArgs e)
    {
        if (!NoSave)
        {
            SavedWidth = Width;
            SavedHeight = Height;
        }
    }

    private void DabForm_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == 27)
        {
            e.Handled = true;
            DialogResult = BotMode ? DialogResult.Abort : DialogResult.Cancel;
        }
    }

    private void btnArticle_Click(object sender, EventArgs e)
    {
        contextMenuStripOther.Show(this, new Point(btnArticle.Left, btnArticle.Top + btnArticle.Height),
            ToolStripDropDownDirection.BelowRight);
    }

    private void openInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Session.Site.OpenPageInBrowser(ArticleTitle);
    }

    private void editInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Tools.EditArticleInBrowser(ArticleTitle);
    }

    private void watchToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Session.Editor.Clone().Watch(ArticleTitle);
            MessageBox.Show("Page successfully added to your watchlist");
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    private void unwatchToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Session.Editor.Clone().Unwatch(ArticleTitle);
            MessageBox.Show("Page successfully removed from your watchlist");
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }
}