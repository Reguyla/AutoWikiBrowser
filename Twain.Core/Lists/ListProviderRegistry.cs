using Twain.Core.Lists.Providers;

namespace Twain.Core.Lists;

/// <summary>
/// Provides the built-in list providers available to Twain.
/// </summary>
public static class ListProviderRegistry
{
    private static readonly List<IListProvider> RegisteredProviders =
        CreateBuiltInProviders();

    /// <summary>
    /// Gets the currently registered list providers.
    /// </summary>
    public static IReadOnlyList<IListProvider> Providers =>
        RegisteredProviders;

    /// <summary>
    /// Registers an additional list provider.
    /// </summary>
    /// <param name="provider">
    /// The provider to register.
    /// </param>
    public static void Add(IListProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        RegisteredProviders.Add(provider);
        ProviderAdded?.Invoke(provider);
    }

    private static List<IListProvider> CreateBuiltInProviders()
    {
        RedirectsListProvider redirectProvider = new();
        WhatLinksHereListProvider whatLinksHereProvider = new();
        WhatTranscludesPageListProvider whatTranscludesProvider = new();
        CategoriesOnPageListProvider categoriesOnPageProvider = new();
        NewPagesListProvider newPagesProvider = new();
        RandomPagesSpecialPageProvider randomPagesProvider = new();
        UserContribsListProvider userContribProvider = new();
        PagesWithPropListProvider pagesWithProvider = new();
        WikiSearchListProvider wikiSearchProvider = new();

        return
        [
            new PagesWithPropJsonListProvider(),
            new CategoryListProvider(),
            new CategoryRecursiveListProvider(),
            new CategoryRecursiveOneLevelListProvider(),
            new CategoryRecursiveUserDefinedLevelListProvider(),
            categoriesOnPageProvider,
            new CategoriesOnPageOnlyHiddenListProvider(),
            new CategoriesOnPageNoHiddenListProvider(),
            whatLinksHereProvider,
            new WhatLinksHereAllNSListProvider(),
            new WhatLinksHereAndToRedirectsListProvider(),
            new WhatLinksHereAndToRedirectsAllNSListProvider(),
            new WhatLinksHereExcludingPageRedirectsListProvider(),
            new WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider(),
            whatTranscludesProvider,
            new WhatTranscludesPageAllNSListProvider(),
            new LinksOnPageListProvider(),
            new LinksOnPageOnlyBlueListProvider(),
            new LinksOnPageOnlyRedListProvider(),
            new FilesOnPageListProvider(),
            new TransclusionsOnPageListProvider(),
            new TextFileListProviderUFT8(),
            new TextFileListProviderWindows1252(),
            new GoogleSearchListProvider(),
            userContribProvider,
            new UserContribUserDefinedNumberListProvider(),
            new SpecialPageListProvider(
                whatLinksHereProvider,
                newPagesProvider,
                categoriesOnPageProvider,
                randomPagesProvider,
                whatTranscludesProvider,
                redirectProvider,
                userContribProvider,
                pagesWithProvider,
                wikiSearchProvider),
            new ImageFileLinksListProvider(),
            new MyWatchlistListProvider(),
            wikiSearchProvider,
            new WikiSearchAllNSListProvider(),
            new WikiTitleSearchListProvider(),
            new WikiTitleSearchAllNSListProvider(),
            randomPagesProvider,
            redirectProvider,
            new RedirectsAllNSListProvider(),
            newPagesProvider,
            pagesWithProvider
        ];
    }

    /// <summary>
    /// Occurs when a list provider is registered at runtime.
    /// </summary>
    public static event Action<IListProvider>? ProviderAdded;
}