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
    private readonly BindingList<IListProvider> _listItems = new();

    private void SpecialPageListProvider_Load(
        object sender,
        EventArgs e)
    {
        int currentSelected =
            cboNamespace.SelectedIndex;

        cboNamespace.Items.Clear();

        foreach (string namespaceName in GetNamespaceItems())
        {
            cboNamespace.Items.Add(
                namespaceName);
        }

        if (currentSelected >= 0 &&
            currentSelected < cboNamespace.Items.Count)
        {
            cboNamespace.SelectedIndex =
                currentSelected;
        }
    }

    private void EnsureDefaultProviders()
    {
        if (_listItems.Count > 0)
        {
            return;
        }

        _listItems.Add(new PrefixIndexSpecialPageProvider());
        _listItems.Add(new AllPagesSpecialPageProvider());
        _listItems.Add(new AllPagesNoRedirectsSpecialPageProvider());
        _listItems.Add(new AllCategoriesSpecialPageProvider());
        _listItems.Add(new AllFilesSpecialPageProvider());
        _listItems.Add(new AllRedirectsSpecialPageProvider());
        _listItems.Add(new RecentChangesSpecialPageProvider());
        _listItems.Add(new LinkSearchSpecialPageProvider());
        _listItems.Add(new RandomRedirectsSpecialPageProvider());
        _listItems.Add(new PagesWithoutLanguageLinksSpecialPageProvider());
        _listItems.Add(new PagesWithoutLanguageLinksNoRedirectsSpecialPageProvider());
        _listItems.Add(new ProtectedPagesSpecialPageProvider());
        _listItems.Add(new GalleryNewFilesSpecialPageProvider());
        _listItems.Add(new DisambiguationPagesSpecialPageProvider());
        _listItems.Add(new AllUsersSpecialPageProvider());
    }

    public SpecialPageListProvider()
    {
        InitializeComponent();

        EnsureDefaultProviders();

        cmboSourceSelect.DataSource = _listItems;
        cmboSourceSelect.DisplayMember = "DisplayText";
        cmboSourceSelect.ValueMember = "DisplayText";
    }

    /// <summary>
    /// Creates presentation models for the available special-page providers.
    /// </summary>
    /// <returns>
    /// A read-only list containing the display name and input capabilities of each
    /// available special-page provider.
    /// </returns>
    public IReadOnlyList<SpecialPageProviderOption> GetProviderOptions()
    {
        return _listItems
            .OfType<ISpecialPageProvider>()
            .Select(
                (provider, index) =>
                    new SpecialPageProviderOption(
                        index,
                        provider.DisplayText,
                        provider.UserInputTextBoxEnabled,
                        provider.NamespacesEnabled))
            .ToList();
    }

    public SpecialPageListProvider(params IListProvider[] providers)
        : this()
    {
        if (!Globals.UsingMono)
        {
            foreach (IListProvider provider in providers)
            {
                if (provider is ISpecialPageProvider)
                {
                    _listItems.Add(provider);
                }
            }
        }
    }

    private static List<string> GetNamespaceItems()
    {
        List<string> namespaces =
            new()
            {
                "Main:"
            };

        namespaces.AddRange(
            Variables.Namespaces.Values);

        return namespaces;
    }

    /// <summary>
    /// Contains the data required to display the special-page list dialog.
    /// </summary>
    public sealed record DialogRequest(
        IReadOnlyList<SpecialPageProviderOption> Providers,
        IReadOnlyList<string> Namespaces);

    /// <summary>
    /// Contains the values selected in the special-page list dialog.
    /// </summary>
    public sealed record DialogSelection(
        int ProviderIndex,
        string NamespaceText,
        string PagesText);

    /// <summary>
    /// Creates the data required to display the special-page list dialog.
    /// </summary>
    public DialogRequest CreateDialogRequest()
    {
        return new DialogRequest(
            GetProviderOptions(),
            GetNamespaceItems());
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

        if (provider.PagesNeeded &&
            string.IsNullOrWhiteSpace(txtPages.Text))
        {
            MessageBox.Show(
                "Pages needed!",
                "Special page list",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return new List<Article>();
        }

        return MakeSpecialPageList(
            provider,
            cboNamespace.Text,
            txtPages.Text);
    }

    /// <summary>
    /// Creates an article list from the values selected in the special-page dialog.
    /// </summary>
    /// <param name="selection">
    /// The provider, namespace, and page criteria selected by the user.
    /// </param>
    /// <returns>
    /// The articles created by the selected provider, or an empty list when the
    /// selected provider cannot be resolved.
    /// </returns>
    private List<Article> MakeList(
        DialogSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        ISpecialPageProvider? provider =
            _listItems
                .OfType<ISpecialPageProvider>()
                .ElementAtOrDefault(
                    selection.ProviderIndex);

        if (provider is null)
        {
            return new List<Article>();
        }

        return MakeSpecialPageList(
            provider,
            selection.NamespaceText,
            selection.PagesText);
    }

    private static List<Article> MakeSpecialPageList(
    ISpecialPageProvider provider,
    string namespaceText,
    string pagesText)
    {
        int namespaceKey =
            Namespace.Determine(
                namespaceText);

        string[] enteredPages =
            pagesText.Split(
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

    private void cmboSourceSelect_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (DesignMode ||
            cmboSourceSelect.SelectedItem is not
                ISpecialPageProvider provider)
        {
            return;
        }

        (
            bool pagesEnabled,
            bool namespacesEnabled) =
            GetProviderCapabilities(provider);

        txtPages.Enabled =
            pagesEnabled;

        cboNamespace.Enabled =
            namespacesEnabled;
    }

    private static (
        bool PagesEnabled,
        bool NamespacesEnabled)
        GetProviderCapabilities(
            ISpecialPageProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            return (
                provider.UserInputTextBoxEnabled,
                provider.NamespacesEnabled);
        }
}