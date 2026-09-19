using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Twain.Core;
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
    private string _sourceText = string.Empty;

    /// <summary>
    /// Gets or sets the title entered for manual addition to the article list.
    /// </summary>
    [ObservableProperty]
    private string _manualArticleTitle = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether list generation is in progress.
    /// </summary>
    [ObservableProperty]
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
            return;
        }

        value.Selected();

        SourcePrompt = value.UserInputTextBoxText;
        IsSourceTextEnabled = value.UserInputTextBoxEnabled;
    }
}