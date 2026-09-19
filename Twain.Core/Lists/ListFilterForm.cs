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

using System.ComponentModel;
using System.Windows.Forms;
using Twain.Core.Controls.Lists;
using Twain.Core.Lists.Providers;

namespace Twain.Core.Lists
{
    public partial class ListFilterForm : Form
    {
        private readonly ListBoxArticle _destListBox;

        string _project = Variables.URL;

        public ListFilterForm(ListBoxArticle lb)
        {
            InitializeComponent();

            if (lb == null)
                throw new ArgumentNullException("lb");

            _destListBox = lb;

            if (_prefs != null)
                Settings = _prefs;
        }

        private List<Article> _list = new();
        private static AWBSettings.SpecialFilterPrefs _prefs;

        private void btnApply_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                bool filterContains =
                    chkContains.Checked &&
                    !string.IsNullOrEmpty(txtContains.Text);

                bool filterDoesNotContain =
                    chkNotContains.Checked &&
                    !string.IsNullOrEmpty(txtDoesNotContain.Text);

                List<Article> result =
                    ArticleListFilterProcessor.Apply(
                        _destListBox.Cast<Article>(),
                        lbRemove.Cast<Article>(),
                        pageNamespaces.GetSelectedNamespaces(),
                        txtContains.Text,
                        txtDoesNotContain.Text,
                        filterContains,
                        filterDoesNotContain,
                        chkIsRegex.Checked,
                        chkRemoveDups.Checked,
                        lbRemove.Items.Count > 0,
                        cbOpType.SelectedIndex != 0,
                        chkSortAZ.Checked);

                _list.Clear();
                _list.AddRange(result);

                if (_list.Count != _destListBox.Items.Count ||
                    !_list.SequenceEqual(_destListBox.Cast<Article>()))
                {
                    _destListBox.BeginUpdate();
                    _destListBox.Items.Clear();
                    _destListBox.Items.AddRange(
                        _list.ToArray());
                    _destListBox.EndUpdate();
                }

                // Only try to update number of articles using ListMaker
                // when the parent is actually a ListMaker.
                if (_destListBox.Parent is ListMaker listMaker)
                {
                    listMaker.UpdateNumberOfArticles();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex);
            }

            DialogResult =
                DialogResult.OK;
        }

        /// <summary>
        /// Loads articles from a UTF-8 text file and adds them to the removal list.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data.
        /// </param>
        private void btnGetList_Click(object sender, EventArgs e)
        {
            Article[] items = new TextFileListProviderUFT8()
                .MakeList()
                .ToArray();

            lbRemove.Items.AddRange(items);
        }

        /// <summary>
        /// Removes all items from the removal list.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data.
        /// </param>
        private void btnClear_Click(object sender, EventArgs e)
        {
            lbRemove.Items.Clear();
        }

        /// <summary>
        /// Cancels the dialog without applying the current filter settings.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data.
        /// </param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Updates the content-filter controls when the contains option changes.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data.
        /// </param>
        private void chkContains_CheckedChanged(object sender, EventArgs e)
        {
            UpdateContainsControls();
        }

        /// <summary>
        /// Updates the content-filter controls when the does-not-contain option
        /// changes.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data.
        /// </param>
        private void chkNotContains_CheckedChanged(object sender, EventArgs e)
        {
            UpdateContainsControls();
        }

        /// <summary>
        /// Initializes the default operation type when the filter dialog loads.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data.
        /// </param>
        private void specialFilter_Load(object sender, EventArgs e)
        {
            cbOpType.SelectedIndex = 0;
        }

        /// <summary>
        /// Clears the current collection of filtered articles.
        /// </summary>
        internal void Clear()
        {
            _list.Clear();
        }

        /// <summary>
        /// Updates the enabled state of the content-filter controls based on the
        /// selected options.
        /// </summary>
        private void UpdateContainsControls()
        {
            txtContains.Enabled = chkContains.Checked;
            txtDoesNotContain.Enabled = chkNotContains.Checked;
            chkIsRegex.Enabled =
                chkContains.Checked ||
                chkNotContains.Checked;
        }

        /// <summary>
        /// Refreshes the namespace list when the active wiki project changes and the
        /// form becomes visible.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data.
        /// </param>
        private void SpecialFilter_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible || _project == Variables.URL)
            {
                return;
            }

            _project = Variables.URL;
            pageNamespaces.Populate();
        }

        /// <summary>
        /// Gets or sets the special-filter settings represented by this form.
        /// </summary>
        /// <remarks>
        /// This property builds or applies an AWB settings object from the current
        /// state of the form's child controls. It is used by AWB's own configuration
        /// handling and must not be serialized independently by the Windows Forms
        /// designer.
        /// </remarks>
        [Browsable(false)]
        [Localizable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AWBSettings.SpecialFilterPrefs Settings
        {
            get
            {
                _prefs = new AWBSettings.SpecialFilterPrefs
                {
                    namespaceValues = pageNamespaces.GetSelectedNamespaces(),
                    filterTitlesThatContain = chkContains.Checked,
                    filterTitlesThatContainText = txtContains.Text,
                    filterTitlesThatDontContain = chkNotContains.Checked,
                    filterTitlesThatDontContainText = txtDoesNotContain.Text,
                    areRegex = chkIsRegex.Checked,
                    remDupes = chkRemoveDups.Checked,
                    sortAZ = chkSortAZ.Checked,
                    opType = cbOpType.SelectedIndex
                };

                foreach (Article a in lbRemove.Items)
                {
                    _prefs.remove.Add(a.Name);
                }

                return _prefs;
            }
            set
            {
                if (value == null || DesignMode)
                    return;

                _prefs = value;

                if (_prefs.namespaceValues == null)
                {
                    _prefs.namespaceValues = new List<int>(
                        new[] { 0, 1, 2, 3, 4, 5, 6, 7, 10, 11, 14, 15 });
                }

                if (!_prefs.namespaceValues.Any())
                    pageNamespaces.SetSelectedNamespaces(_prefs.namespaceValues);

                chkContains.Checked = _prefs.filterTitlesThatContain;
                txtContains.Text = _prefs.filterTitlesThatContainText;
                chkNotContains.Checked = _prefs.filterTitlesThatDontContain;
                txtDoesNotContain.Text = _prefs.filterTitlesThatDontContainText;
                chkIsRegex.Checked = _prefs.areRegex;

                chkRemoveDups.Checked = _prefs.remDupes;
                chkSortAZ.Checked = _prefs.sortAZ;

                cbOpType.SelectedIndex = _prefs.opType;

                lbRemove.Items.Clear();
                lbRemove.Items.AddRange(
                    _prefs.remove.Select(s => new Article(s)).ToArray());
            }
        }
    }
}