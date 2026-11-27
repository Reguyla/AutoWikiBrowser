using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.Disambiguation;

namespace Twain.UI.Disambiguation;

/// <summary>
/// Displays the interactive disambiguation editor for the prepared
/// occurrences in an article.
/// </summary>
public partial class DabWindow : Window
{
    /// <summary>
    /// Contains the disambiguation controls created for the current article.
    /// </summary>
    private readonly List<DabControl> _items = [];

    /// <summary>
    /// The title of the article currently being disambiguated.
    /// </summary>
    private readonly string _articleTitle = string.Empty;

    private Func<string, bool, Task<bool>>? _setWatchlistStatusAsync;
    private bool _hasWatchlistStatus;
    private bool _isArticleWatched;
    private bool _isWatchOperationRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="DabWindow"/> class.
    /// </summary>
    public DabWindow()
    {
        InitializeComponent();

        Closing += DabWindow_Closing;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DabWindow"/> class
    /// using prepared disambiguation data.
    /// </summary>
    /// <param name="articleTitle">
    /// The title of the article being disambiguated.
    /// </param>
    /// <param name="preparation">
    /// The prepared disambiguation data for the current article.
    /// </param>
    public DabWindow(
        string articleTitle,
        DisambiguationProcessor.DisambiguationPreparation preparation)
        : this()
    {
        _articleTitle = articleTitle;

        Title = $"Disambiguation — {articleTitle}";

        PopulateItems(preparation);
    }

    /// <summary>
    /// Configures the watchlist status and operation for the current article.
    /// </summary>
    public void ConfigureWatchlist(
        bool hasWatchlistStatus,
        bool isArticleWatched,
        Func<string, bool, Task<bool>> setWatchlistStatusAsync)
    {
        _hasWatchlistStatus = hasWatchlistStatus;
        _isArticleWatched = isArticleWatched;
        _setWatchlistStatusAsync = setWatchlistStatusAsync;

        UpdateWatchlistMenuState();
    }

    /// <summary>
    /// Gets the panel containing the dynamically generated
    /// disambiguation controls.
    /// </summary>
    internal StackPanel ItemsPanel => DisambiguationItemsPanel;

    /// <summary>
    /// Raised when the user requests that all manual changes be undone.
    /// </summary>
    public event EventHandler? UndoAllRequested;

    /// <summary>
    /// Raised when the user requests that all disambiguation selections
    /// be reset.
    /// </summary>
    public event EventHandler? ResetAllRequested;

    /// <summary>
    /// Creates a disambiguation control for each prepared occurrence
    /// and adds it to the window.
    /// </summary>
    /// <param name="preparation">
    /// The prepared disambiguation data for the current article.
    /// </param>
    private void PopulateItems(
        DisambiguationProcessor.DisambiguationPreparation preparation)
    {
        foreach (DisambiguationProcessor.DisambiguationItemPreparation item
                 in preparation.Items)
        {
            DabControl control =
                new DabControl(
                    item,
                    preparation.Variants);

            control.Changed += DabControl_Changed;

            DisambiguationItemsPanel.Children.Add(control);
            _items.Add(control);
        }

        UpdateDoneButtonState();
    }

    /// <summary>
    /// Gets the disambiguation results currently represented by the
    /// controls in this window.
    /// </summary>
    public IReadOnlyList<DisambiguationProcessor.DisambiguationResult> Results =>
        _items
            .Select(
                item => new DisambiguationProcessor.DisambiguationResult(
                    item.NoChange,
                    item.Result))
            .ToList();

    /// <summary>
    /// Handles changes reported by an individual disambiguation control.
    /// </summary>
    private void DabControl_Changed(
        object? sender,
        EventArgs e)
    {
        UpdateDoneButtonState();
    }

    /// <summary>
    /// Updates the availability of the Done button.
    /// </summary>
    private void UpdateDoneButtonState()
    {
        DoneButton.IsEnabled =
            !_isWatchOperationRunning &&
            _items.Count > 0 &&
            _items.All(item => item.CanSave);
    }

    /// <summary>
    /// Opens the current article in the default web browser.
    /// </summary>
    private void OpenInBrowser_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_articleTitle))
            return;

        Tools.OpenArticleInBrowser(_articleTitle);
    }

    /// <summary>
    /// Opens the current article's editing page in the default web browser.
    /// </summary>
    private void EditInBrowser_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_articleTitle))
            return;

        Tools.EditArticleInBrowser(_articleTitle);
    }

    private async void WatchPage_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await SetWatchlistStatusAsync(true);
    }

    private async void UnwatchPage_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await SetWatchlistStatusAsync(false);
    }

    /// <summary>
    /// Updates the article's watchlist status through the configured
    /// workspace operation.
    /// </summary>
    private async Task SetWatchlistStatusAsync(bool watch)
    {
        if (_isWatchOperationRunning ||
            !_hasWatchlistStatus ||
            _setWatchlistStatusAsync is null ||
            _isArticleWatched == watch)
        {
            return;
        }

        _isWatchOperationRunning = true;
        UpdateWatchlistMenuState();

        try
        {
            bool succeeded = await _setWatchlistStatusAsync(
                _articleTitle,
                watch);

            if (succeeded)
            {
                _isArticleWatched = watch;
            }
        }
        finally
        {
            _isWatchOperationRunning = false;
            UpdateWatchlistMenuState();
        }
    }

    /// <summary>
    /// Updates the availability of the watchlist menu actions.
    /// </summary>
    private void UpdateWatchlistMenuState()
    {
        bool canChangeWatchlist =
            _hasWatchlistStatus &&
            !_isWatchOperationRunning &&
            _setWatchlistStatusAsync is not null;

        WatchPageMenuItem.IsEnabled =
            canChangeWatchlist && !_isArticleWatched;

        UnwatchPageMenuItem.IsEnabled =
            canChangeWatchlist && _isArticleWatched;

        UpdateDoneButtonState();
    }

    /// <summary>
    /// Prevents the disambiguation dialog from closing while
    /// a watchlist operation is in progress.
    /// </summary>
    private void DabWindow_Closing(
        object? sender,
        WindowClosingEventArgs e)
    {
        if (_isWatchOperationRunning)
        {
            e.Cancel = true;
        }
    }

    /// <summary>
    /// Restores the generated replacement for the current selection in every
    /// disambiguation control, discarding manual correction edits.
    /// </summary>
    private void UndoAllButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        foreach (DabControl item in _items)
        {
            item.Undo();
        }

        UndoAllRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Restores every disambiguation control to its default no-change state.
    /// </summary>
    private void ResetAllButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        foreach (DabControl item in _items)
        {
            item.Reset();
        }

        ResetAllRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Closes the dialog and indicates that article processing should abort.
    /// </summary>
    private void AbortButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(DabDialogResult.Abort);
    }

    /// <summary>
    /// Closes the dialog without applying the disambiguation changes.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(DabDialogResult.Cancel);
    }

    /// <summary>
    /// Closes the dialog and indicates that the selected disambiguation
    /// changes should be applied when all entries are saveable.
    /// </summary>
    private void DoneButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_items.Any(item => !item.CanSave))
            return;

        Close(DabDialogResult.Done);
    }

    /// <summary>
    /// Shows the disambiguation dialog and applies the selected results when
    /// the user completes the dialog.
    /// </summary>
    /// <param name="owner">
    /// The window that owns the disambiguation dialog.
    /// </param>
    /// <param name="articleTitle">
    /// The title of the article being disambiguated.
    /// </param>
    /// <param name="articleText">
    /// The original article text.
    /// </param>
    /// <param name="preparation">
    /// The prepared disambiguation data for the article.
    /// </param>
    /// <returns>
    /// The dialog result together with the resulting article text.
    /// When the dialog is cancelled or aborted, the original article text
    /// is returned unchanged.
    /// </returns>
    public static async Task<(
        DabDialogResult DialogResult,
        DisambiguationProcessor.DisambiguationProcessingResult ProcessingResult)>
        ShowWithCorrectionsAsync(
            Window owner,
            string articleTitle,
            string articleText,
            DisambiguationProcessor.DisambiguationPreparation preparation,
            bool hasWatchlistStatus,
            bool isArticleWatched,
            Func<string, bool, Task<bool>> setWatchlistStatusAsync)
    {
        DabWindow window = new(articleTitle, preparation);

        window.ConfigureWatchlist(
            hasWatchlistStatus,
            isArticleWatched,
            setWatchlistStatusAsync);

        DabDialogResult result =
            await window.ShowDialog<DabDialogResult>(owner);

        if (result != DabDialogResult.Done)
        {
            return (
                result,
                new DisambiguationProcessor.DisambiguationProcessingResult(
                    articleText,
                    Array.Empty<
                        DisambiguationProcessor.DisambiguationCorrection>()));
        }

        var processingResult =
            DisambiguationProcessor.ApplyResultsWithCorrections(
                articleText,
                preparation,
                window.Results);

        return (result, processingResult);
    }

    public static async Task<(
    DabDialogResult DialogResult,
    string ArticleText)> ShowAsync(
        Window owner,
        string articleTitle,
        string articleText,
        DisambiguationProcessor.DisambiguationPreparation preparation,
        bool hasWatchlistStatus,
        bool isArticleWatched,
        Func<string, bool, Task<bool>> setWatchlistStatusAsync)
    {
        var (dialogResult, processingResult) =
            await ShowWithCorrectionsAsync(
                owner,
                articleTitle,
                articleText,
                preparation,
                hasWatchlistStatus,
                isArticleWatched,
                setWatchlistStatusAsync);

        return (
            dialogResult,
            processingResult.ArticleText);
    }
}