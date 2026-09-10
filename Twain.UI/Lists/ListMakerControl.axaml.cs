using Avalonia.Controls;
using System.Collections.Generic;
using System.Linq;
using Twain.Core;
using Twain.Core.Lists;

namespace Twain.UI.Lists;

/// <summary>
/// Displays and manages an article list in the Avalonia user interface.
/// </summary>
public partial class ListMakerControl : UserControl
{
    private readonly Twain.Core.Lists.ArticleList _articleList = new();
    private readonly ArticleListFilterConfiguration _filterConfiguration = new();

    private string _sourceText = string.Empty;
    private bool _makeListEnabled = true;

    /// <summary>
    /// Gets or sets the source text used when generating an article list.
    /// </summary>
    public string SourceText
    {
        get => _sourceText;
        set => _sourceText = value ?? string.Empty;
    }

    /// <summary>
    /// Sets whether list generation is enabled.
    /// </summary>
    public bool MakeListEnabled
    {
        set => _makeListEnabled = value;
    }

    /// <summary>
    /// Occurs when the number of articles in the list is updated.
    /// </summary>
    public event EventHandler? NoOfArticlesChanged;

    public ListMakerControl()
    {
        InitializeComponent();
        RefreshArticleList();
    }

    /// <summary>
    /// Gets the number of articles currently in the list.
    /// </summary>
    public int Count => _articleList.Count;

    /// <summary>
    /// Adds an article to the list.
    /// </summary>
    public void AddArticle(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        _articleList.Add(article);
        RefreshArticleList();
    }

    /// <summary>
    /// Adds multiple articles to the list.
    /// </summary>
    public void AddArticles(IEnumerable<Article> articles)
    {
        ArgumentNullException.ThrowIfNull(articles);

        _articleList.AddRange(articles);
        RefreshArticleList();
    }

    /// <summary>
    /// Removes all articles from the list.
    /// </summary>
    public void Clear()
    {
        _articleList.Clear();
        RefreshArticleList();
    }

    /// <summary>
    /// Returns a snapshot of the current article list.
    /// </summary>
    public List<Article> GetArticleList()
    {
        return _articleList.ToList();
    }

    /// <summary>
    /// Sorts the list alphabetically by article title.
    /// </summary>
    public void AlphaSortList()
    {
        _articleList.SortAscending();
        RefreshArticleList();
    }

    /// <summary>
    /// Updates the Remove button when the article selection changes.
    /// </summary>
    private void ArticleListBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        RemoveButton.IsEnabled =
            ArticleListBox.SelectedItems?.Count > 0;
    }

    /// <summary>
    /// Refreshes the displayed article list and related control state.
    /// </summary>
    private void RefreshArticleList()
    {
        ArticleListBox.ItemsSource = null;
        ArticleListBox.ItemsSource = _articleList.ToList();

        ArticleCountText.Text =
            _articleList.Count == 1
                ? "1 page"
                : $"{_articleList.Count} pages";

        FilterButton.IsEnabled =
            _articleList.Count > 0;

        RemoveButton.IsEnabled =
            ArticleListBox.SelectedItems?.Count > 0;

        NoOfArticlesChanged?.Invoke(
            this,
            EventArgs.Empty);
    }

    /// <summary>
    /// Removes the selected articles from the list.
    /// </summary>
    private void RemoveButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        List<int> selectedIndexes =
            ArticleListBox.Selection.SelectedIndexes.ToList();

        selectedIndexes.Sort();
        selectedIndexes.Reverse();

        foreach (int index in selectedIndexes)
        {
            _articleList.RemoveAt(index);
        }

        RefreshArticleList();
    }

    /// <summary>
    /// Opens the article-list filter configuration window and applies the
    /// selected filters to the current article list.
    /// </summary>
    private async void FilterButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        ListFilterWindow filterWindow =
            new(_filterConfiguration);

        bool apply =
            await filterWindow.ShowDialog<bool>(owner);

        if (!apply)
            return;

        ApplyFilterConfiguration();
    }

    /// <summary>
    /// Applies the current article-list filter configuration.
    /// </summary>
    private void ApplyFilterConfiguration()
    {
        List<Article> articles =
            _articleList.ToList();

        articles =
            ArticleListFilterProcessor.FilterByNamespace(
                articles,
                _filterConfiguration.NamespaceIds);

        articles =
            ArticleListFilterProcessor.FilterByTitle(
                articles,
                _filterConfiguration.ContainsText,
                _filterConfiguration.DoesNotContainText,
                _filterConfiguration.FilterTitlesThatContain,
                _filterConfiguration.FilterTitlesThatDoNotContain,
                _filterConfiguration.UseRegex);

        if (_filterConfiguration.ComparisonArticleTitles.Count > 0)
        {
            List<Article> comparisonArticles = new();

            foreach (string title in
                     _filterConfiguration.ComparisonArticleTitles)
            {
                comparisonArticles.Add(
                    new Article(title));
            }

            articles =
                ArticleListFilterProcessor.FilterByArticleSet(
                    articles,
                    comparisonArticles,
                    _filterConfiguration.IntersectComparisonList);
        }

        _articleList.ReplaceWith(articles);

        if (_filterConfiguration.RemoveDuplicates)
            _articleList.RemoveDuplicates();

        if (_filterConfiguration.SortAscending)
            _articleList.SortAscending();

        RefreshArticleList();
    }
}