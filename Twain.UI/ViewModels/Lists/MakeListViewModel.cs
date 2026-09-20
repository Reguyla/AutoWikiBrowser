using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    {
        foreach (IListProvider provider in
                 ListProviderRegistry.Providers
                     .OrderBy(provider => provider.DisplayText,
                         StringComparer.CurrentCultureIgnoreCase))
        {
            Providers.Add(provider);
        }

        ListProviderRegistry.ProviderAdded += OnProviderAdded;

        SelectedProvider = Providers.FirstOrDefault(
            provider => provider.GetType().Name == "CategoryListProvider");
    }

    private void OnProviderAdded(IListProvider provider)
    {
        if (!Providers.Contains(provider))
            Providers.Add(provider);
    }

    [RelayCommand(CanExecute = nameof(CanAddArticle))]
    private void AddArticle()
    {
        string title =
            ListGenerationProcessor.PrepareArticleTitle(
                ManualArticleTitle);

        if (title.Length == 0)
            return;

        Articles.Add(new Article(title));

        ManualArticleTitle = string.Empty;
    }

    private bool CanAddArticle()
    {
        return !string.IsNullOrWhiteSpace(ManualArticleTitle);
    }

    [RelayCommand]
    private void RemoveArticles(IEnumerable<Article>? articles)
    {
        if (articles is null)
            return;

        foreach (Article article in articles.ToList())
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

    private bool CanMakeList()
    {
        if (IsBusy || SelectedProvider is null)
            return false;

        return !SelectedProvider.UserInputTextBoxEnabled ||
               !string.IsNullOrWhiteSpace(SourceText);
    }
}