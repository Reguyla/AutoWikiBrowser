using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using Twain.Core;
using Twain.Core.Lists;
using Twain.Core.Lists.Providers;

namespace Twain.UI.ViewModels.Lists;

/// <summary>
/// Provides presentation state for the Make List workspace region.
/// </summary>
public partial class MakeListViewModel : ObservableObject
{
    /// <summary>
    /// Gets the articles currently displayed in the Make List region.
    /// </summary>
    public ObservableCollection<Article> Articles { get; } = new();

    /// <summary>
    /// Gets or sets the article currently selected in the Make List region.
    /// </summary>
    [ObservableProperty]
    private Article? _selectedArticle;

    /// <summary>
    /// Gets or sets the source text used by the selected list provider.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MakeListCommand))]
    private string _sourceText = string.Empty;

    /// <summary>
    /// Gets or sets the title entered for manual addition to the article list.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddArticleCommand))]
    private string _manualArticleTitle = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether list generation is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MakeListCommand))]
    private bool _isBusy;

    /// <summary>
    /// Gets or sets the current list-generation status text.
    /// </summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>
    /// Gets the number of articles currently displayed.
    /// </summary>
    public int ArticleCount => Articles.Count;

    /// <summary>
    /// Gets the list providers available to the Make List region.
    /// </summary>
    public ObservableCollection<IListProvider> Providers { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected list provider.
    /// </summary>
    [ObservableProperty]
    private IListProvider? _selectedProvider;

    /// <summary>
    /// Gets or sets the label describing the input expected by the selected
    /// list provider.
    /// </summary>
    [ObservableProperty]
    private string _sourcePrompt = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the selected list provider
    /// accepts source text.
    /// </summary>
    [ObservableProperty]
    private bool _isSourceTextEnabled;

    private readonly Session _session;

    [ObservableProperty]
    private bool _formatDisplayTitles;

    /// <summary>
    /// Gets the list providers that can generate additional articles
    /// from selected article titles.
    /// </summary>
    public IEnumerable<IListProvider> AddFromSelectedProviders =>
        Providers.Where(
            provider => provider.UserInputTextBoxEnabled);

    public void RefreshArticleDisplay()
    {
        OnPropertyChanged(nameof(FormatDisplayTitles));
    }

    partial void OnSelectedProviderChanged(IListProvider? value)
    {
        if (value is null)
        {
            SourcePrompt = string.Empty;
            IsSourceTextEnabled = false;
            MakeListCommand.NotifyCanExecuteChanged();
            return;
        }

        value.Selected();

        SourcePrompt = value.UserInputTextBoxText;
        IsSourceTextEnabled = value.UserInputTextBoxEnabled;

        MakeListCommand.NotifyCanExecuteChanged();
    }

    public MakeListViewModel()
        : this(new Session())
    {
    }

    public MakeListViewModel(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;

        foreach (IListProvider provider in
                 ListProviderRegistry.Providers
                     .OrderBy(provider => provider.DisplayText,
                         StringComparer.CurrentCultureIgnoreCase))
        {
            ConfigureProvider(provider);
            Providers.Add(provider);
        }

        ListProviderRegistry.ProviderAdded += OnProviderAdded;

        SelectedProvider = Providers.FirstOrDefault(
            provider => provider.GetType().Name == "CategoryListProvider");
    }

    private void ConfigureProvider(IListProvider provider)
    {
        if (provider is ApiListProviderBase apiProvider)
        {
            apiProvider.SetSession(_session);
        }
    }

    private void OnProviderAdded(IListProvider provider)
    {
        ConfigureProvider(provider);

        if (!Providers.Contains(provider))
            Providers.Add(provider);

        OnPropertyChanged(nameof(AddFromSelectedProviders));
    }

    [RelayCommand(CanExecute = nameof(CanAddArticle))]
    private void AddArticle()
    {
        AddArticleTitles(
            [ManualArticleTitle]);

        ManualArticleTitle = string.Empty;
    }

    private bool CanAddArticle()
    {
        return !string.IsNullOrWhiteSpace(ManualArticleTitle);
    }

    /// <summary>
    /// Adds the supplied article titles to the current article list.
    /// </summary>
    /// <param name="titles">
    /// The article titles to add.
    /// </param>
    public void AddArticleTitles(
        IEnumerable<string> titles)
    {
        ArgumentNullException.ThrowIfNull(titles);

        foreach (string value in titles)
        {
            string title =
                ListGenerationProcessor.PrepareArticleTitle(
                    value);

            if (title.Length == 0)
            {
                continue;
            }

            Articles.Add(
                new Article(title));
        }
    }

    [RelayCommand]
    private void RemoveArticles(IEnumerable<Article>? articles)
    {
        if (articles is null)
            return;

        foreach (Article article in articles.ToList())
            Articles.Remove(article);
    }

    /// <summary>
    /// Removes all articles from the current list.
    /// </summary>
    [RelayCommand]
    private void RemoveAllArticles()
    {
        Articles.Clear();
        SelectedArticle = null;
    }

    /// <summary>
    /// Removes duplicate article titles while preserving
    /// the first occurrence and original list order.
    /// </summary>
    [RelayCommand]
    private void RemoveDuplicateArticles()
    {
        HashSet<string> seen = new(
            StringComparer.OrdinalIgnoreCase);

        Article[] uniqueArticles = Articles
            .Where(article => seen.Add(article.Name))
            .ToArray();

        int removedCount = Articles.Count - uniqueArticles.Length;

        if (removedCount == 0)
            return;

        ReplaceArticles(uniqueArticles);

        StatusText =
            $"{removedCount} duplicate article(s) removed.";
    }

    /// <summary>
    /// Removes articles outside the main namespace.
    /// </summary>
    [RelayCommand]
    private void RemoveNonMainSpaceArticles()
    {
        Article[] mainSpaceArticles = Articles
            .Where(article =>
                article.NameSpaceKey == Namespace.Article)
            .ToArray();

        int removedCount =
            Articles.Count - mainSpaceArticles.Length;

        if (removedCount == 0)
            return;

        ReplaceArticles(mainSpaceArticles);

        StatusText =
            $"{removedCount} non-main space article(s) removed.";
    }

    /// <summary>
    /// Removes the specified article from the current article list.
    /// </summary>
    /// <param name="article">The article to remove.</param>
    /// <returns>
    /// <see langword="true"/> when the article was removed; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool RemoveArticle(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        return Articles.Remove(article);
    }

    /// <summary>
    /// Removes the article with the specified title from the current article list.
    /// </summary>
    /// <param name="articleName">
    /// The title of the article to remove.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the article was found and removed; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool RemoveArticle(
        string articleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleName);

        Article? article =
            Articles.FirstOrDefault(
                item =>
                    string.Equals(
                        item.Name,
                        articleName,
                        StringComparison.Ordinal));

        return article is not null &&
               Articles.Remove(article);
    }

    [RelayCommand(CanExecute = nameof(CanMakeList))]
    private void MakeList()
    {
        if (SelectedProvider is null)
            return;

        IsBusy = true;
        StatusText = "Generating list...";

        try
        {
            string[] sourceValues =
                ListGenerationProcessor.ParseSourceValues(SourceText);

            sourceValues =
                ListGenerationProcessor.PrepareSourceValues(
                    SelectedProvider,
                    sourceValues);

            ListGenerationRequest request =
                new(
                    SelectedProvider,
                    sourceValues);

            ListGenerationResult result =
                ListGenerationProcessor.Generate(request);

            if (!result.Succeeded)
            {
                StatusText =
                    string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? $"Unable to generate list: {result.Failure}"
                        : result.ErrorMessage;

                return;
            }

            Articles.Clear();

            foreach (Article article in result.Articles)
                Articles.Add(article);

            StatusText =
                $"{Articles.Count} article{(Articles.Count == 1 ? string.Empty : "s")}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Generates additional articles from the supplied source articles
    /// using the specified list provider and appends the results to the
    /// current article list.
    /// </summary>
    public void AddFromSelectedArticles(
        IListProvider provider,
        IEnumerable<Article> sourceArticles)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(sourceArticles);

        string[] sourceValues =
            sourceArticles
                .Select(article => article.Name)
                .ToArray();

        if (sourceValues.Length == 0)
            return;

        IsBusy = true;
        StatusText = "Generating list...";

        try
        {
            ConfigureProvider(provider);

            sourceValues =
                ListGenerationProcessor.PrepareSourceValues(
                    provider,
                    sourceValues);

            ListGenerationRequest request =
                new(
                    provider,
                    sourceValues);

            ListGenerationResult result =
                ListGenerationProcessor.Generate(request);

            if (!result.Succeeded)
            {
                StatusText =
                    string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? $"Unable to generate list: {result.Failure}"
                        : result.ErrorMessage;

                return;
            }

            foreach (Article article in result.Articles)
                Articles.Add(article);

            StatusText =
                $"{Articles.Count} article{(Articles.Count == 1 ? string.Empty : "s")}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanMakeList()
    {
        if (IsBusy || SelectedProvider is null)
            return false;

        return !SelectedProvider.UserInputTextBoxEnabled ||
               !string.IsNullOrWhiteSpace(SourceText);
    }

    /// <summary>
    /// Opens the currently selected article in the default web browser.
    /// </summary>
    [RelayCommand]
    private void OpenSelectedArticle()
    {
        if (SelectedArticle is null)
        {
            return;
        }

        _session.Site.OpenPageInBrowser(
            SelectedArticle.Name);
    }

    /// <summary>
    /// Opens the revision history of the currently selected article
    /// in the default web browser.
    /// </summary>
    [RelayCommand]
    private void OpenSelectedArticleHistory()
    {
        if (SelectedArticle is null)
        {
            return;
        }

        _session.Site.OpenPageHistoryInBrowser(
            SelectedArticle.Name);
    }

    /// <summary>
    /// Opens the talk page associated with the currently selected
    /// article in the default web browser.
    /// </summary>
    [RelayCommand]
    private void OpenSelectedArticleTalk()
    {
        if (SelectedArticle is null)
        {
            return;
        }

        _session.Site.OpenPageInBrowser(
            Tools.ConvertToTalk(SelectedArticle));
    }

    [RelayCommand]
    private void MoveArticlesToTop(
    IEnumerable<Article>? articles)
    {
        if (articles is null)
        {
            return;
        }

        List<Article> selectedArticles =
            articles
                .Where(Articles.Contains)
                .OrderBy(Articles.IndexOf)
                .ToList();

        if (selectedArticles.Count == 0)
        {
            return;
        }

        foreach (Article article in selectedArticles)
        {
            Articles.Remove(article);
        }

        for (int index = 0;
             index < selectedArticles.Count;
             index++)
        {
            Articles.Insert(
                index,
                selectedArticles[index]);
        }
    }

    [RelayCommand]
    private void MoveArticlesToBottom(
        IEnumerable<Article>? articles)
    {
        if (articles is null)
        {
            return;
        }

        List<Article> selectedArticles =
            articles
                .Where(Articles.Contains)
                .OrderBy(Articles.IndexOf)
                .ToList();

        if (selectedArticles.Count == 0)
        {
            return;
        }

        foreach (Article article in selectedArticles)
        {
            Articles.Remove(article);
        }

        foreach (Article article in selectedArticles)
        {
            Articles.Add(article);
        }
    }

    /// <summary>
    /// Converts the current article list to the corresponding talk pages.
    /// </summary>
    [RelayCommand]
    private void ConvertToTalkPages()
    {
        if (Articles.Count == 0)
        {
            return;
        }

        List<Article> convertedArticles =
            Tools.ConvertToTalk(
                Articles.ToList());

        Articles.Clear();

        foreach (Article article in convertedArticles)
        {
            Articles.Add(article);
        }
    }

    /// <summary>
    /// Converts talk pages in the current list to their corresponding
    /// non-talk pages.
    /// </summary>
    [RelayCommand]
    private void ConvertFromTalkPages()
    {
        if (Articles.Count == 0)
        {
            return;
        }

        List<Article> convertedArticles =
            Tools.ConvertFromTalk(
                Articles.ToList());

        Articles.Clear();

        foreach (Article article in convertedArticles)
        {
            Articles.Add(article);
        }
    }

    /// <summary>
    /// Sorts the current article list alphabetically by page title.
    /// </summary>
    [RelayCommand]
    private void SortAscending()
    {
        List<Article> sortedArticles =
            Articles
                .OrderBy(
                    article => article.Name,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        Articles.Clear();

        foreach (Article article in sortedArticles)
        {
            Articles.Add(article);
        }
    }

    /// <summary>
    /// Sorts the current article list in reverse alphabetical order
    /// by page title.
    /// </summary>
    [RelayCommand]
    private void SortDescending()
    {
        List<Article> sortedArticles =
            Articles
                .OrderByDescending(
                    article => article.Name,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        Articles.Clear();

        foreach (Article article in sortedArticles)
        {
            Articles.Add(article);
        }
    }

    public string GetArticleDisplayText(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        if (!FormatDisplayTitles ||
            string.IsNullOrWhiteSpace(article.DisplayTitle))
        {
            return article.Name;
        }

        return article.DisplayTitle;
    }

    public void ReplaceArticles(
    IEnumerable<Article> articles)
    {
        ArgumentNullException.ThrowIfNull(articles);

        Article? selectedArticle = SelectedArticle;

        Articles.Clear();

        foreach (Article article in articles)
            Articles.Add(article);

        if (selectedArticle is not null)
        {
            SelectedArticle =
                Articles.FirstOrDefault(
                    article =>
                        string.Equals(
                            article.Name,
                            selectedArticle.Name,
                            StringComparison.Ordinal));
        }
    }
}