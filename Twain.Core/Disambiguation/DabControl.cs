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

using System.Windows.Forms;

namespace Twain.Core.Disambiguation;

public partial class DabControl : UserControl
{
    public DabControl()
    {
        InitializeComponent();
    }

    public DabControl(string articleText, Match match, List<string> variants, int contextChars)
    {
        try
        {
            ArticleText = articleText;
            Match = match;
            Variants = variants;
            ContextChars = contextChars;
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }

        InitializeComponent();
    }

    public event EventHandler Changed;

    // input data
    public readonly string ArticleText;
    public readonly Match Match;
    public readonly List<string> Variants;

    // output data
    public string Surroundings;
    public int SurroundingsStart;
    public string Result
    {
        get { return txtCorrection.Text; }
    }

    /// <summary>
    /// Returns whether this disambiguation makes a change
    /// </summary>
    public bool NoChange
    {
        get { return cmboChoice.SelectedIndex == 0 && txtCorrection.Text == Surroundings; }
    }

    //internal
    private readonly int ContextChars;
    private int PosStart, PosEnd;
    private bool StartOfSentence;

    private string VisibleLink, RealLink, CurrentLink, LinkTrail;

    private static readonly Regex UnpipeRegex = new Regex(@"\[\[\s*([^\|\]]*)\s*\|\s*[^\]]*\s*\]\](.*)", RegexOptions.Compiled);

    public bool CanSave
    {
        get { return !string.IsNullOrEmpty(txtCorrection.Text.Trim()); }
    }

    /// <summary>
    /// most preparations are done here
    /// </summary>
    private void DabControl_Load(object sender, EventArgs e)
    {
        try
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // prepare variants
            foreach (string s in Variants)
            {
                cmboChoice.Items.Add(s);
            }

            DisambiguationProcessor.DisambiguationItemPreparation preparation =
                DisambiguationProcessor.PrepareItem(
                    ArticleText,
                    Match,
                    ContextChars);

            PosStart = preparation.PositionStart;
            PosEnd = preparation.PositionEnd;

            VisibleLink = preparation.VisibleLink;
            RealLink = preparation.RealLink;
            LinkTrail = preparation.LinkTrail;

            SurroundingsStart = preparation.SurroundingsStart;
            Surroundings = preparation.Surroundings;

            StartOfSentence = preparation.StartOfSentence;

            // prepare text boxes
            // text editable by user is the new wikilink only, not the context
            // if user could edit context, could create conflicting changes for nearby links
            txtCorrection.Text = Match.Value;

            txtViewer.Text = ArticleText.Substring(PosStart, PosEnd - PosStart);
            // highlight link to disambiguate
            txtViewer.Select(Match.Index - PosStart, Match.Length);
            txtViewer.SelectionFont = new System.Drawing.Font(txtViewer.SelectionFont.FontFamily,
                txtViewer.SelectionFont.Size, System.Drawing.FontStyle.Bold);
            txtViewer.SelectionBackColor = System.Drawing.Color.FromArgb(0xFFD754);
            txtViewer.Select(SurroundingsStart - PosStart, 0);
            txtViewer.ScrollToCaret();
            txtViewer.Select(0, 0);

            cmboChoice.SelectedIndex = 0;
            cmboChoice.Select();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    private void ComboBoxChanged(int n)
    {
        try
        {
            switch (n)
            {
                case 0: //No change
                    CurrentLink = Match.Value;
                    break;

                case 1: //unlink
                    CurrentLink = VisibleLink + LinkTrail;
                    break;

                case 2: //{{Disambiguation needed}}
                    CurrentLink = Match.Value + "{{Disambiguation needed|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}";
                    break;

                default: //everything else
                    CurrentLink = "[[";
                    if (StartOfSentence || char.IsUpper(RealLink[0]))
                        CurrentLink += Tools.TurnFirstToUpper(Variants[n - 3]);
                    else
                        CurrentLink += Variants[n - 3];

                    CurrentLink += "|" + VisibleLink;
                    if (RealLink == VisibleLink)
                        CurrentLink += LinkTrail + "]]";
                    else
                        CurrentLink += "]]" + LinkTrail;

                    CurrentLink = Parse.Parsers.SimplifyLinks(CurrentLink);
                    break;
            }

            txtCorrection.Text = CurrentLink;

            btnUnpipe.Enabled = btnFlip.Enabled = CurrentLink.Contains("|");
            if (Changed != null) Changed(this, new EventArgs());
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    private void cmboChoice_SelectedIndexChanged(object sender, EventArgs e)
    {
        ComboBoxChanged(cmboChoice.SelectedIndex);
    }

    /// <summary>
    /// sets disambiguation back to 'no change' state
    /// </summary>
    public void Reset()
    {
        // to ensure that handler will be called
        if (cmboChoice.SelectedIndex != 0) cmboChoice.SelectedIndex = 0;
        else ComboBoxChanged(0);
    }

    private void btnReset_Click(object sender, EventArgs e)
    {
        Reset();
    }

    /// <summary>
    /// undoes all manual changes in edit box
    /// </summary>
    public void Undo()
    {
        ComboBoxChanged(cmboChoice.SelectedIndex);
    }

    private void btnUndo_Click(object sender, EventArgs e)
    {
        Undo();
    }

    private void btnUnpipe_Click(object sender, EventArgs e)
    {
        string newLink = UnpipeRegex.Replace(CurrentLink, "[[$1]]$2");
        txtCorrection.Text = txtCorrection.Text.Replace(CurrentLink, newLink);
        CurrentLink = newLink;
        if (Changed != null)
        {
            Changed(this, new EventArgs());
        }
    }

    private void txtCorrection_TextChanged(object sender, EventArgs e)
    {
        if (Changed != null)
        {
            Changed(this, new EventArgs());
        }
    }

    private void btnFlip_Click(object sender, EventArgs e)
    {
        string newLink = Regex.Replace(CurrentLink, @"\[\[(.*)\|(.*)\]\]", "[[$2|$1]]");
        txtCorrection.Text = txtCorrection.Text.Replace(CurrentLink, newLink);
        CurrentLink = newLink;
        if (Changed != null) Changed(this, new EventArgs());
    }
}