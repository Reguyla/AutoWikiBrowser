using System.Collections.Generic;

namespace Twain.Core.Lists.Providers;

/// <summary>
/// Provides special-page source configuration and article-list generation
/// independently of the user interface.
/// </summary>
public sealed class SpecialPageListService
{
    private readonly List<ISpecialPageProvider> _providers = new();

    public SpecialPageListService(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _providers.Add(new PrefixIndexSpecialPageProvider());
        _providers.Add(new AllPagesSpecialPageProvider());
        _providers.Add(new AllPagesNoRedirectsSpecialPageProvider());
        _providers.Add(new AllCategoriesSpecialPageProvider());
        _providers.Add(new AllFilesSpecialPageProvider());
        _providers.Add(new AllRedirectsSpecialPageProvider());
        _providers.Add(new RecentChangesSpecialPageProvider());
        _providers.Add(new LinkSearchSpecialPageProvider());
        _providers.Add(new RandomRedirectsSpecialPageProvider());
        _providers.Add(new PagesWithoutLanguageLinksSpecialPageProvider());
        _providers.Add(new PagesWithoutLanguageLinksNoRedirectsSpecialPageProvider());
        _providers.Add(new ProtectedPagesSpecialPageProvider());
        _providers.Add(new GalleryNewFilesSpecialPageProvider());
        _providers.Add(new DisambiguationPagesSpecialPageProvider());
        _providers.Add(new AllUsersSpecialPageProvider());

        foreach (ISpecialPageProvider provider in _providers)
        {
            if (provider is ApiListProviderBase apiProvider)
            {
                apiProvider.SetSession(session);
            }
        }
    }

    /// <summary>
    /// Returns the available special-page providers and their
    /// supported input capabilities.
    /// </summary>
    public IReadOnlyList<SpecialPageProviderOption> GetProviderOptions()
    {
        return _providers
            .Select(
                (provider, index) =>
                    new SpecialPageProviderOption(
                        index,
                        provider.DisplayText,
                        provider.UserInputTextBoxEnabled,
                        provider.NamespacesEnabled))
            .ToList();
    }

    /// <summary>
    /// Returns the namespaces available for special-page queries.
    /// </summary>
    public IReadOnlyList<string> GetNamespaceItems()
    {
        List<string> namespaces =
        [
            "Main:"
        ];

        namespaces.AddRange(
            Variables.Namespaces.Values);

        return namespaces;
    }

    /// <summary>
    /// Contains the information needed to configure the
    /// Avalonia special-page selection dialog.
    /// </summary>
    public sealed record DialogRequest(
        IReadOnlyList<SpecialPageProviderOption> Providers,
        IReadOnlyList<string> Namespaces);

    /// <summary>
    /// Contains the user's selected special-page source,
    /// namespace, and page criteria.
    /// </summary>
    public sealed record DialogSelection(
        int ProviderIndex,
        string NamespaceText,
        string PagesText);

    /// <summary>
    /// Creates the configuration request for the special-page dialog.
    /// </summary>
    public DialogRequest CreateDialogRequest()
    {
        return new DialogRequest(
            GetProviderOptions(),
            GetNamespaceItems());
    }

    /// <summary>
    /// Generates an article list using the selected special-page provider.
    /// </summary>
    /// <param name="selection">
    /// The source, namespace, and page criteria selected by the user.
    /// </param>
    /// <returns>
    /// The generated articles, or an empty list if the selection
    /// cannot be resolved or required page criteria are missing.
    /// </returns>
    public List<Article> MakeList(DialogSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        ISpecialPageProvider? provider =
            _providers.ElementAtOrDefault(selection.ProviderIndex);

        if (provider is null)
        {
            return new List<Article>();
        }

        int namespaceKey =
            Namespace.Determine(selection.NamespaceText);

        string[] enteredPages =
            selection.PagesText.Split(
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
}