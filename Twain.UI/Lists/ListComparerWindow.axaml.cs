using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.Controls.Lists;
using Twain.Core.TwainSettings;
using Twain.UI.ViewModels.Lists;

namespace Twain.UI.Controls.Lists;

/// <summary>
/// Compares two article lists and displays the articles unique to each list
/// together with the articles common to both lists.
/// </summary>
public partial class ListComparerWindow : Avalonia.Controls.Window
{
    private readonly List<Article> _onlyInList1 = new();
    private readonly List<Article> _onlyInList2 = new();
    private readonly List<Article> _common = new();
    private readonly UserPrefs _prefs;
    private readonly MakeListViewModel _list1ViewModel;
    private readonly MakeListViewModel _list2ViewModel;

    /// <summary>
    /// Raised when the user requests that a comparison result be used
    /// as the application's article list.
    /// </summary>
    public event EventHandler<IReadOnlyList<Article>>? UseListRequested;

    /// <summary>
    /// Initializes a new empty list comparer window.
    /// </summary>
    public ListComparerWindow(
        Session session,
        UserPrefs prefs)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(prefs);

        InitializeComponent();

        _prefs = prefs;

        _list1ViewModel = new MakeListViewModel(session);
        _list2ViewModel = new MakeListViewModel(session);

        ListMaker1.DataContext = _list1ViewModel;
        ListMaker2.DataContext = _list2ViewModel;

        _list1ViewModel.Articles.CollectionChanged += (_, _) =>
        {
            UpdateControlState();
            UpdateInputListCounts();
        };

        _list2ViewModel.Articles.CollectionChanged += (_, _) =>
        {
            UpdateControlState();
            UpdateInputListCounts();
        };

        UpdateControlState();
        UpdateResultCounts();
        UpdateInputListCounts();
    }


    /// <summary>
    /// Releases the temporary list view models when
    /// the List Comparer window closes.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _list1ViewModel.Dispose();
        _list2ViewModel.Dispose();

        base.OnClosed(e);
    }

    /// <summary>
    /// Initializes a list comparer with articles in the first list.
    /// </summary>
    public ListComparerWindow(
        Session session,
        UserPrefs prefs,
        IEnumerable<Article> articles)
        : this(session, prefs)
    {
        ArgumentNullException.ThrowIfNull(articles);

        _list1ViewModel.ReplaceArticles(articles);
    }

    /// <summary>
    /// Compares the two input article lists and displays the articles unique
    /// to each list together with the articles common to both lists.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void CompareButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ListComparisonResult result =
            ListComparerProcessor.Compare(
                _list1ViewModel.Articles.ToList(),
                _list2ViewModel.Articles.ToList());

        _onlyInList1.Clear();
        _onlyInList1.AddRange(
            result.OnlyInList1);

        _onlyInList2.Clear();
        _onlyInList2.AddRange(
            result.OnlyInList2);

        _common.Clear();
        _common.AddRange(
            result.Common);

        RefreshResultLists();
    }

    private void UpdateInputListCounts()
    {
        List1CountText.Text =
            $"{_list1ViewModel.Articles.Count:N0} pages";

        List2CountText.Text =
            $"{_list2ViewModel.Articles.Count:N0} pages";
    }

    /// <summary>
    /// Clears all comparison results.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void ClearButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ClearResults();
    }

    /// <summary>
    /// Removes all articles from the comparison-result collections and
    /// refreshes the displayed result lists.
    /// </summary>
    private void ClearResults()
    {
        _onlyInList1.Clear();
        _onlyInList2.Clear();
        _common.Clear();

        RefreshResultLists();
    }

    /// <summary>
    /// Refreshes the three displayed comparison-result lists and their
    /// associated counts and button states.
    /// </summary>
    private void RefreshResultLists()
    {
        OnlyList1ListBox.ItemsSource = null;
        OnlyList1ListBox.ItemsSource = _onlyInList1;

        OnlyList2ListBox.ItemsSource = null;
        OnlyList2ListBox.ItemsSource = _onlyInList2;

        CommonListBox.ItemsSource = null;
        CommonListBox.ItemsSource = _common;

        UpdateResultCounts();
    }

    /// <summary>
    /// Updates controls whose enabled state depends on the contents of
    /// the two comparison input lists.
    /// </summary>
    private void UpdateControlState()
    {
        CompareButton.IsEnabled =
            _list1ViewModel.Articles.Count > 0 &&
            _list2ViewModel.Articles.Count > 0;
    }

    /// <summary>
    /// Updates the displayed comparison-result counts and button states.
    /// </summary>
    private void UpdateResultCounts()
    {
        OnlyList1CountText.Text =
            $"{_onlyInList1.Count} pages";

        OnlyList2CountText.Text =
            $"{_onlyInList2.Count} pages";

        CommonCountText.Text =
            $"{_common.Count} pages";

        bool hasOnlyList1 =
            _onlyInList1.Count > 0;

        bool hasOnlyList2 =
            _onlyInList2.Count > 0;

        bool hasCommon =
            _common.Count > 0;

        SaveOnlyList1Button.IsEnabled = hasOnlyList1;
        UseOnlyList1Button.IsEnabled = hasOnlyList1;

        SaveOnlyList2Button.IsEnabled = hasOnlyList2;
        UseOnlyList2Button.IsEnabled = hasOnlyList2;

        SaveCommonButton.IsEnabled = hasCommon;
        UseCommonButton.IsEnabled = hasCommon;

        ClearButton.IsEnabled =
            hasOnlyList1 ||
            hasOnlyList2 ||
            hasCommon;
    }

    /// <summary>
    /// Requests that the articles unique to List 1 be used as the
    /// application's article list.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void UseOnlyList1Button_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RequestUseList(
            _onlyInList1);
    }

    /// <summary>
    /// Requests that the articles common to both lists be used as the
    /// application's article list.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void UseCommonButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RequestUseList(
            _common);
    }

    /// <summary>
    /// Requests that the articles unique to List 2 be used as the
    /// application's article list.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void UseOnlyList2Button_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RequestUseList(
            _onlyInList2);
    }

    /// <summary>
    /// Prompts the user to save the articles unique to List 1.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private async void SaveOnlyList1Button_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveResultListAsync(
            _onlyInList1,
            "List 1 unique articles.txt");
    }

    /// <summary>
    /// Prompts the user to save the articles common to both lists.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private async void SaveCommonButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveResultListAsync(
            _common,
            "Common articles.txt");
    }

    /// <summary>
    /// Prompts the user to save the articles unique to List 2.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private async void SaveOnlyList2Button_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveResultListAsync(
            _onlyInList2,
            "List 2 unique articles.txt");
    }

    /// <summary>
    /// Prompts the user for a destination and saves the supplied articles
    /// using the selected article-list output format.
    /// </summary>
    /// <param name="articles">
    /// The articles to save.
    /// </param>
    /// <param name="suggestedFileName">
    /// The file name initially suggested by the save dialog.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous save operation.
    /// </returns>

    private async Task SaveResultListAsync(
        IEnumerable<Article> articles,
        string suggestedFileName)
    {
        ArticleListOutputFormat format =
            GetSelectedOutputFormat();

        string extension =
            ArticleListOutputFormatInfo.GetExtension(format)
                .TrimStart('.');

        string baseFileName =
            Path.GetFileNameWithoutExtension(suggestedFileName);

        IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save article list",
                    SuggestedFileName =
                        $"{baseFileName}.{extension}",
                    DefaultExtension = extension,
                    FileTypeChoices =
                    [
                        new FilePickerFileType(
                        $"{extension.ToUpperInvariant()} files")
                    {
                        Patterns = [$"*.{extension}"]
                    }
                    ]
                });

        if (file is null)
            return;

        string? path = file.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            ArticleListExporter.Save(
                articles,
                path,
                format,
                _prefs);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }


    /// <summary>
    /// Gets the article-list output format selected in the save-format control.
    /// </summary>
    /// <returns>
    /// The selected article-list output format.
    /// </returns>

    private ArticleListOutputFormat GetSelectedOutputFormat()
    {
        return SaveFormatComboBox.SelectedIndex switch
        {
            0 => ArticleListOutputFormat.WikiText,
            1 => ArticleListOutputFormat.PlainText,
            2 => ArticleListOutputFormat.Csv,
            3 => ArticleListOutputFormat.CsvWikiText,
            4 => ArticleListOutputFormat.TwainSettingsXml,
            _ => ArticleListOutputFormat.WikiText
        };
    }


    /// <summary>
    /// Removes the selected articles from the List 1 unique results.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void RemoveOnlyList1Selected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RemoveSelectedArticles(
            OnlyList1ListBox,
            _onlyInList1);
    }

    /// <summary>
    /// Removes the selected articles from the common results.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void RemoveCommonSelected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RemoveSelectedArticles(
            CommonListBox,
            _common);
    }

    /// <summary>
    /// Removes the selected articles from the List 2 unique results.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void RemoveOnlyList2Selected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RemoveSelectedArticles(
            OnlyList2ListBox,
            _onlyInList2);
    }

    /// <summary>
    /// Removes the selected entries from an article collection using their
    /// displayed list indexes and refreshes the comparison results.
    /// </summary>
    /// <param name="listBox">
    /// The result list containing the selected entries.
    /// </param>
    /// <param name="articles">
    /// The backing article collection from which the entries are removed.
    /// </param>
    private void RemoveSelectedArticles(
        ListBox listBox,
        List<Article> articles)
    {
        List<int> selectedIndexes =
            listBox.Selection.SelectedIndexes.ToList();

        selectedIndexes.Sort();
        selectedIndexes.Reverse();

        foreach (int index in selectedIndexes)
        {
            if (index >= 0 &&
                index < articles.Count)
            {
                articles.RemoveAt(index);
            }
        }

        RefreshResultLists();
    }

    /// <summary>
    /// Selects every article in the List 1 unique results.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void SelectAllOnlyList1_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SelectAllArticles(
            OnlyList1ListBox);
    }

    /// <summary>
    /// Selects every article in the common results.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void SelectAllCommon_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SelectAllArticles(
            CommonListBox);
    }

    /// <summary>
    /// Selects every article in the List 2 unique results.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void SelectAllOnlyList2_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SelectAllArticles(
            OnlyList2ListBox);
    }

    /// <summary>
    /// Selects every item in the supplied result list.
    /// </summary>
    /// <param name="listBox">
    /// The result list whose items are selected.
    /// </param>
    private static void SelectAllArticles(
        ListBox listBox)
    {
        listBox.Selection.SelectAll();
    }

    /// <summary>
    /// Copies the selected List 1 unique article titles to the clipboard.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private async void CopyOnlyList1Selected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CopySelectedArticlesAsync(
            OnlyList1ListBox);
    }

    /// <summary>
    /// Copies the selected common article titles to the clipboard.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private async void CopyCommonSelected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CopySelectedArticlesAsync(
            CommonListBox);
    }

    /// <summary>
    /// Copies the selected List 2 unique article titles to the clipboard.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private async void CopyOnlyList2Selected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CopySelectedArticlesAsync(
            OnlyList2ListBox);
    }

    /// <summary>
    /// Copies the titles of the selected articles to the system clipboard,
    /// with one title per line.
    /// </summary>
    /// <param name="listBox">
    /// The result list containing the articles to copy.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous clipboard operation.
    /// </returns>
    private static async Task CopySelectedArticlesAsync(
        ListBox listBox)
    {
        if (TopLevel.GetTopLevel(listBox)?.Clipboard is not { } clipboard)
            return;

        if (listBox.SelectedItems is null ||
            listBox.SelectedItems.Count == 0)
        {
            return;
        }

        List<string> titles = new();

        foreach (object? item in listBox.SelectedItems)
        {
            if (item is Article article)
                titles.Add(article.Name);
        }

        if (titles.Count == 0)
            return;

        await clipboard.SetTextAsync(
            string.Join(
                Environment.NewLine,
                titles));
    }

    /// <summary>
    /// Opens the selected List 1 unique articles in the browser.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void OpenOnlyList1InBrowser_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenSelectedArticlesInBrowser(
            OnlyList1ListBox);
    }

    /// <summary>
    /// Opens the selected common articles in the browser.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void OpenCommonInBrowser_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenSelectedArticlesInBrowser(
            CommonListBox);
    }

    /// <summary>
    /// Opens the selected List 2 unique articles in the browser.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void OpenOnlyList2InBrowser_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenSelectedArticlesInBrowser(
            OnlyList2ListBox);
    }

    /// <summary>
    /// Opens each selected article from the supplied result list in the browser.
    /// </summary>
    /// <param name="listBox">
    /// The result list containing the articles to open.
    /// </param>
    private static void OpenSelectedArticlesInBrowser(
        ListBox listBox)
    {
        if (listBox.SelectedItems is null)
            return;

        foreach (object? item in listBox.SelectedItems)
        {
            if (item is Article article)
            {
                Tools.OpenArticleInBrowser(
                    article.Name);
            }
        }
    }

    /// <summary>
    /// Raises <see cref="UseListRequested"/> with a snapshot of the supplied
    /// comparison result.
    /// </summary>
    /// <param name="articles">
    /// The comparison-result articles requested for use by the application.
    /// </param>
    private void RequestUseList(
        IEnumerable<Article> articles)
    {
        List<Article> requestedArticles =
            articles.ToList();

        if (requestedArticles.Count == 0)
            return;

        UseListRequested?.Invoke(
            this,
            requestedArticles);
    }

    /// <summary>
    /// Transfers all articles from the selected comparison-result list
    /// to the first input ListMaker.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void TransferToListMaker1_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ListBox? sourceListBox =
            GetResultListBoxFromMenuItem(sender);

        if (sourceListBox is null)
            return;

        AddResultToListMaker(
            _list1ViewModel,
            sourceListBox);
    }

    /// <summary>
    /// Transfers all articles from the selected comparison-result list
    /// to the second input ListMaker.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void TransferToListMaker2_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ListBox? sourceListBox =
            GetResultListBoxFromMenuItem(sender);

        if (sourceListBox is null)
            return;

        AddResultToListMaker(
            _list2ViewModel,
            sourceListBox);
    }

    /// <summary>
    /// Gets the result ListBox associated with a context-menu item.
    /// </summary>
    /// <param name="sender">
    /// The menu item whose context menu was opened for a result list.
    /// </param>
    /// <returns>
    /// The owning result ListBox, or <see langword="null"/> when it
    /// cannot be determined.
    /// </returns>
    private static ListBox? GetResultListBoxFromMenuItem(
        object? sender)
    {
        if (sender is not MenuItem menuItem ||
            menuItem.Parent is not ContextMenu contextMenu)
        {
            return null;
        }

        return contextMenu.PlacementTarget as ListBox;
    }

    private void CloseButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Adds all articles displayed in a comparison-result list
    /// to the destination list view model.
    /// </summary>
    private static void AddResultToListMaker(
        MakeListViewModel destination,
        ListBox sourceListBox)
    {
        foreach (object? item in sourceListBox.Items)
        {
            if (item is Article article)
            {
                destination.Articles.Add(article);
            }
        }
    }

}