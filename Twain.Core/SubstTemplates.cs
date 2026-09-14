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

        private readonly Parse.TemplateSubstitutionProcessor _processor = new();

        /// <summary>
        /// Describes the template-substitution settings presented to a user interface.
        /// </summary>
        public sealed record DialogRequest(
            string[] TemplateList,
            bool ExpandRecursively,
            bool IgnoreUnformatted,
            bool IncludeComments);

        /// <summary>
        /// Describes template-substitution settings returned by a user interface.
        /// </summary>
        public sealed record DialogSelection(
            string[] TemplateList,
            bool ExpandRecursively,
            bool IgnoreUnformatted,
            bool IncludeComments);

        /// <summary>
        /// Gets or sets the application-provided presenter used to edit template
        /// substitution settings.
        /// </summary>
        public static Func<
            DialogRequest,
            Task<DialogSelection?>>?
            ShowDialogAsync  { get; set; }

        /// <summary>
        /// Creates a snapshot of the current template-substitution settings for
        /// presentation by a user interface.
        /// </summary>
        private DialogRequest CreateDialogRequest()
        {
            return new DialogRequest(
                [.. TemplateList],
                ExpandRecursively,
                IgnoreUnformatted,
                IncludeComments);
        }

        /// <summary>
        /// Applies template-substitution settings returned by a user interface.
        /// </summary>
        /// <param name="selection">
        /// The settings selected by the user.
        /// </param>
        private void ApplyDialogSelection(
            DialogSelection selection)
        {
            ArgumentNullException.ThrowIfNull(selection);

            TemplateList =
                [.. selection.TemplateList];

            ExpandRecursively =
                selection.ExpandRecursively;

            IgnoreUnformatted =
                selection.IgnoreUnformatted;

            IncludeComments =
                selection.IncludeComments;
        }

        /// <summary>
        /// Displays the application-provided template-substitution settings editor.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the user accepts the edited settings;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public async Task<bool> ShowConfigurationDialogAsync()
        {
            if (ShowDialogAsync is null)
            {
                return false;
            }

            DialogSelection? selection =
                await ShowDialogAsync(
                    CreateDialogRequest());

            if (selection is null)
            {
                return false;
            }

            ApplyDialogSelection(
                selection);

            return true;
        }

        /// <summary>
        /// Gets or sets the list of templates to substitute.
        /// </summary>
        /// <remarks>
        /// This property synchronizes the runtime template list with the internal
        /// template text box and updates the template substitution processor.
        /// It is not intended to be serialized independently by the Windows Forms
        /// designer.
        /// </remarks>
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public string[] TemplateList
        {
            get => _processor.TemplateList;

            set
            {
                ArgumentNullException.ThrowIfNull(value);

                _processor.TemplateList = value;
                textBoxTemplates.Lines = value;
                textBoxTemplates.Select(0, 0);
            }
        }

        /// <summary>
        /// Gets or sets whether template substitution should expand templates
        /// recursively.
        /// </summary>
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public bool ExpandRecursively
        {
            get => _processor.ExpandRecursively;

            set
            {
                _processor.ExpandRecursively = value;
                chkUseExpandTemplates.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets whether unformatted templates should be ignored.
        /// </summary>
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public bool IgnoreUnformatted
        {
            get => _processor.IgnoreUnformatted;

            set
            {
                _processor.IgnoreUnformatted = value;
                chkIgnoreUnformatted.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets whether comments should be included during template
        /// substitution.
        /// </summary>
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public bool IncludeComments
        {
            get => _processor.IncludeComments;

            set
            {
                _processor.IncludeComments = value;
                chkIncludeComment.Checked = value;
            }
        }

        /// <summary>
        /// Clears all configured template substitutions.
        /// </summary>
        public void Clear()
        {
            _processor.Clear();
            textBoxTemplates.Clear();
        }

        /// <summary>
        /// Clears the editable template list without changing the currently
        /// committed template substitution configuration.
        /// </summary>
        private void btnClear_Click(
            object sender,
            EventArgs e)
        {
            textBoxTemplates.Text = string.Empty;
        }

        /// <summary>
        /// Commits the template list currently displayed in the editor and closes
        /// the dialog.
        /// </summary>
        private void btnOk_Click(
            object sender,
            EventArgs e)
        {
            TemplateList = textBoxTemplates.Lines;
            Close();
        }

        /// <summary>
        /// Discards uncommitted changes in the template editor and restores the
        /// currently committed template list.
        /// </summary>
        private void btnReset_Click(
            object sender,
            EventArgs e)
        {
            textBoxTemplates.Lines = TemplateList;
        }

        /// <summary>
        /// Gets the number of configured template substitution expressions.
        /// </summary>
        public int NoOfRegexes =>
            _processor.NoOfRegexes;

        /// <summary>
        /// Gets whether any template substitutions are configured.
        /// </summary>
        public bool HasSubstitutions =>
            _processor.HasSubstitutions;

        /// <summary>
        /// Substitutes configured templates in the supplied article text.
        /// </summary>
        /// <param name="articleText">
        /// The wiki text of the article.
        /// </param>
        /// <param name="articleTitle">
        /// The title of the article.
        /// </param>
        /// <returns>
        /// The article text after configured template substitutions have been
        /// applied.
        /// </returns>
        public string SubstituteTemplates(
            string articleText,
            string articleTitle)
        {
            _processor.ExpandRecursively =
                chkUseExpandTemplates.Checked;

            _processor.IgnoreUnformatted =
                chkIgnoreUnformatted.Checked;

            _processor.IncludeComments =
                chkIncludeComment.Checked;

            return _processor.SubstituteTemplates(
                articleText,
                articleTitle);
        }

        /// <summary>
        /// Updates the availability of the include-comments option when recursive
        /// template expansion is enabled or disabled.
        /// </summary>
        /// <remarks>
        /// Including comments only applies when templates are expanded recursively,
        /// so the option is disabled when recursive expansion is not selected.
        /// </remarks>
        private void chkUseExpandTemplates_CheckedChanged(
            object sender,
            EventArgs e)
        {
            chkIncludeComment.Enabled =
                chkUseExpandTemplates.Checked;
        }
    }
}