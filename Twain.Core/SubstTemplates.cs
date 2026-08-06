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

namespace Twain.Core
{
    public partial class SubstTemplates : Form
    {
        public SubstTemplates()
        {
            InitializeComponent();
        }

        private string[] LocTemplateList = new string[0];

        private readonly Dictionary<Regex, string> Regexes = new Dictionary<Regex, string>();

        private readonly Parse.HideText RemoveUnformatted = new Parse.HideText(true, false, true);

        /// <summary>
        /// Gets or sets the list of templates to substitute.
        /// </summary>
        /// <remarks>
        /// This property synchronizes the runtime template list with the internal
        /// template text box and refreshes the generated regular expressions. It is
        /// not intended to be serialized independently by the Windows Forms designer.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string[] TemplateList
        {
            get { return LocTemplateList; }
            set
            {
                textBoxTemplates.Lines = LocTemplateList = value;
                textBoxTemplates.Select(0, 0);
                RefreshRegexes();
            }
        }

        /// <summary>
        /// Gets or sets whether template substitution should expand templates
        /// recursively.
        /// </summary>
        /// <remarks>
        /// This property exposes the state of the internal expand-templates checkbox
        /// for runtime use and should not be serialized independently by the Windows
        /// Forms designer.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ExpandRecursively
        {
            get { return chkUseExpandTemplates.Checked; }
            set { chkUseExpandTemplates.Checked = value; }
        }

        /// <summary>
        /// Gets or sets whether unformatted templates should be ignored.
        /// </summary>
        /// <remarks>
        /// This property exposes the state of the internal ignore-unformatted
        /// checkbox for runtime use and should not be serialized independently by
        /// the Windows Forms designer.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IgnoreUnformatted
        {
            get { return chkIgnoreUnformatted.Checked; }
            set { chkIgnoreUnformatted.Checked = value; }
        }

        /// <summary>
        /// Gets or sets whether comments should be included during template
        /// substitution.
        /// </summary>
        /// <remarks>
        /// This property exposes the state of the internal include-comments checkbox
        /// for runtime use and should not be serialized independently by the Windows
        /// Forms designer.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IncludeComments
        {
            get { return chkIncludeComment.Checked; }
            set { chkIncludeComment.Checked = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public void Clear()
        {
            LocTemplateList = new string[0];
            Regexes.Clear();
        }

        /// <summary>
        /// Generates regexes to match the templates from the template list.
        /// Supports templates with Template: or Msg: at the start
        /// Does not process nested templates
        /// </summary>
        private void RefreshRegexes()
        {
            Regexes.Clear();

            // derive optional template namespace prefixes to allow
            string templ = Variables.NamespacesCaseInsensitive[Namespace.Template];
            if (templ[0] == '(')
                templ = "(?:" + templ.Insert(templ.IndexOf(')'), "|[Mm]sg") + @")?\s*";
            else
                templ = @"(?:" + templ + @"|[Mm]sg:|)\s*";

            foreach (string s in TemplateList)
            {
                if (string.IsNullOrEmpty(s.Trim()))
                    continue;

                Regexes.Add(new Regex(@"\{\{\s*" + templ + Tools.FirstLetterCaseInsensitive(Regex.Escape(s)) + @"\s*(\|[^\}]*|)}}",
                    RegexOptions.Singleline), @"{{subst:" + s + "$1}}");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            textBoxTemplates.Text = string.Empty;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            TemplateList = textBoxTemplates.Lines;
            Close();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            textBoxTemplates.Lines = TemplateList;
        }

        /// <summary>
        /// Returns the number of templates in the substitution list
        /// </summary>
        public int NoOfRegexes { get { return Regexes.Count; } }

        /// <summary>
        /// Returns whether there are any templates in the substitution list
        /// </summary>
        public bool HasSubstitutions { get { return NoOfRegexes != 0; } }

        /// <summary>
        /// Substitutes templates in the given article text
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <returns></returns>
        public string SubstituteTemplates(string articleText, string articleTitle)
        {
            if (!HasSubstitutions)
                return articleText; // nothing to substitute

            if (chkIgnoreUnformatted.Checked)
                articleText = RemoveUnformatted.HideUnformatted(articleText);

            if (!chkUseExpandTemplates.Checked)
            {
                foreach (KeyValuePair<Regex, string> p in Regexes)
                {
                    articleText = p.Key.Replace(articleText, p.Value);
                }
            }
            else
                articleText = Tools.ExpandTemplate(articleText, articleTitle, Regexes, chkIncludeComment.Checked);

            if (chkIgnoreUnformatted.Checked)
                articleText = RemoveUnformatted.AddBackUnformatted(articleText);

            return articleText;
        }

        private void chkUseExpandTemplates_CheckedChanged(object sender, EventArgs e)
        {
            chkIncludeComment.Enabled = chkUseExpandTemplates.Checked;
        }
    }
}