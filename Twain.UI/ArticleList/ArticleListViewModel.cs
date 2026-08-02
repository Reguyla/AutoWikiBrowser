using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace Twain.UI.ArticleList;

/// <summary>
/// Provides presentation state and commands for the article-list pane.
/// </summary>
/// <remarks>
/// This initial implementation establishes the article-list presentation
/// contract. Article acquisition, filtering, persistence, and processing
/// behavior will later be supplied by services in Twain.Core.
/// </remarks>
public sealed partial class ArticleListViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes the article-list pane with temporary sample entries.
    /// </summary>
    public ArticleListViewModel()
    {
        Articles =
        [
            "Example article",
            "Another article",
            "Sample page"
        ];

        SelectedArticle = Articles.FirstOrDefault();
    }

    /// <summary>
    /// Gets the article titles currently displayed in the pane.
    /// </summary>
    public ObservableCollection<string> Articles { get; }

    /// <summary>
    /// Gets or sets the currently selected article title.
    /// </summary>
    [ObservableProperty]
    private string? _selectedArticle;

    /// <summary>
    /// Adds a temporary article entry to the list.
    /// </summary>
    [RelayCommand]
    private void AddArticle()
    {
        string title = $"New article {Articles.Count + 1}";

        Articles.Add(title);
        SelectedArticle = title;
    }

    /// <summary>
    /// Removes the currently selected article from the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemoveSelectedArticle))]
    private void RemoveSelectedArticle()
    {
        if (SelectedArticle is null)
        {
            return;
        }

        int selectedIndex = Articles.IndexOf(SelectedArticle);

        Articles.Remove(SelectedArticle);

        if (Articles.Count == 0)
        {
            SelectedArticle = null;
            return;
        }

        int nextIndex = Math.Min(
            selectedIndex,
            Articles.Count - 1);

        SelectedArticle = Articles[nextIndex];
    }

    /// <summary>
    /// Determines whether a selected article can be removed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when an article is selected; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool CanRemoveSelectedArticle()
    {
        return SelectedArticle is not null;
    }

    partial void OnSelectedArticleChanged(
        string? value)
    {
        RemoveSelectedArticleCommand.NotifyCanExecuteChanged();
    }
}