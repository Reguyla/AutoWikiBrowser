/*
Copyright (C) 2007 Martin Richards
(C) 2008 Sam Reed

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

namespace Twain.Core.Lists.Providers;

/// <summary>
/// Gets the list of pages on the Named Special Pages
/// </summary>
public partial class SpecialPageListProvider : Form, IListProvider
{
    private static readonly BindingList<IListProvider> ListItems = new();

    public SpecialPageListProvider()
    {
        InitializeComponent();

        if (ListItems.Count == 0)
        {
            ListItems.Add(new PrefixIndexSpecialPageProvider());
            ListItems.Add(new AllPagesSpecialPageProvider());
            ListItems.Add(new AllPagesNoRedirectsSpecialPageProvider());
            ListItems.Add(new AllCategoriesSpecialPageProvider());
            ListItems.Add(new AllFilesSpecialPageProvider());
            ListItems.Add(new AllRedirectsSpecialPageProvider());
            ListItems.Add(new RecentChangesSpecialPageProvider());
            ListItems.Add(new LinkSearchSpecialPageProvider());
            ListItems.Add(new RandomRedirectsSpecialPageProvider());
            ListItems.Add(new PagesWithoutLanguageLinksSpecialPageProvider());
            ListItems.Add(new PagesWithoutLanguageLinksNoRedirectsSpecialPageProvider());
            ListItems.Add(new ProtectedPagesSpecialPageProvider());
            ListItems.Add(new GalleryNewFilesSpecialPageProvider());
            ListItems.Add(new DisambiguationPagesSpecialPageProvider());
            ListItems.Add(new AllUsersSpecialPageProvider());
        }

        cmboSourceSelect.DataSource = ListItems;
        cmboSourceSelect.DisplayMember = "DisplayText";
        cmboSourceSelect.ValueMember = "DisplayText";
    }

    public SpecialPageListProvider(params IListProvider[] providers)
        : this()
    {
        if (!Globals.UsingMono)
        {
            foreach (IListProvider prov in providers)
            {
                if (prov is ISpecialPageProvider)
                    ListItems.Add(prov);
            }
        }
    }

    /// <summary>
    /// Displays the special-page list dialog and creates an article list using
    /// the selected provider and namespace.
    /// </summary>
    /// <param name="searchCriteria">
    /// Initial search criteria supplied by the caller. The current dialog
    /// implementation replaces these values with the page text entered by the
    /// user.
    /// </param>
    /// <returns>
    /// The articles created by the selected provider, or an empty list when the
    /// dialog is already visible, is cancelled, or cannot produce a list.
    /// </returns>
    public List<Article> MakeList(
        params string[] searchCriteria)
    {
        if (Visible)
        {
            return new List<Article>();
        }

        txtPages.Clear();

        if (ShowDialog() != DialogResult.OK)
        {
            return new List<Article>();
        }

        if (cmboSourceSelect.SelectedItem is not
            ISpecialPageProvider provider)
        {
            return new List<Article>();
        }

        int namespaceKey =
            Namespace.Determine(
                cboNamespace.Text);

        string[] enteredPages =
            txtPages.Text.Split(
                '|',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (enteredPages.Length > 0)
        {
            return provider.MakeList(
                namespaceKey,
                enteredPages);
        }

        if (provider.PagesNeeded)
        {
            MessageBox.Show(
                "Pages needed!",
                "Special page list",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return new List<Article>();
        }

        return provider.MakeList(
            namespaceKey,
            string.Empty);
    }

    /// <summary>
    /// Gets the display name shown for this list provider.
    /// </summary>
    public string DisplayText => "Special page";

    /// <summary>
    /// Gets the default text displayed in the user-input field.
    /// </summary>
    public string UserInputTextBoxText => string.Empty;

    /// <summary>
    /// Gets a value indicating whether the user-input field is enabled.
    /// </summary>
    public bool UserInputTextBoxEnabled => false;

    /// <summary>
    /// Handles selection of this list provider.
    /// </summary>
    /// <remarks>
    /// This provider does not require any additional action when selected.
    /// </remarks>
    public void Selected()
    {
    }

    /// <summary>
    /// Gets a value indicating whether list generation should run on a separate
    /// thread.
    /// </summary>
    public bool RunOnSeparateThread => true;

    /// <summary>
    /// Gets a value indicating whether URL prefixes should be removed from input
    /// values.
    /// </summary>
    public virtual bool StripUrl => false;

    private void SpecialPageListProvider_Load(object sender, EventArgs e)
    {
        int currentSelected = cboNamespace.SelectedIndex;
        cboNamespace.Items.Clear();
        cboNamespace.Items.Add("Main:");
        foreach (string name in Variables.Namespaces.Values)
        {
            cboNamespace.Items.Add(name);
        }
        cboNamespace.SelectedIndex = currentSelected;
    }

    private void cmboSourceSelect_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (DesignMode) return;

        ISpecialPageProvider prov = (ISpecialPageProvider)cmboSourceSelect.SelectedItem;

        txtPages.Enabled = prov.UserInputTextBoxEnabled;
        cboNamespace.Enabled = prov.NamespacesEnabled;
    }
}