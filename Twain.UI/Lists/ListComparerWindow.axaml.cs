using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.Controls.Lists;

namespace Twain.UI.Controls.Lists;

/// <summary>
/// Compares two article lists and displays unique and common results.
/// </summary>
public partial class ListComparerWindow : Avalonia.Controls.Window
{
    private readonly List<Article> _onlyInList1 = new();
    private readonly List<Article> _onlyInList2 = new();
    private readonly List<Article> _common = new();

    /// <summary>
    /// Initializes a new empty list comparer window.
    /// </summary>
    public ListComparerWindow()
    {
        InitializeComponent();

        ListMaker1.MakeListEnabled = true;
        ListMaker2.MakeListEnabled = true;

        UpdateControlState();
        UpdateResultCounts();
    }

    /// <summary>
    /// Initializes a new list comparer window with articles in the first list.
    /// </summary>
    public ListComparerWindow(
        IEnumerable<Article> articles)
        : this()
    {
        ArgumentNullException.ThrowIfNull(articles);

        ListMaker1.AddArticles(articles);
    }

    private void ListMaker_NoOfArticlesChanged(
        object? sender,
        EventArgs e)
    {
        UpdateControlState();
    }

    /// <summary>
    /// Compares the two article lists and displays the resulting
    /// unique and common articles.
    /// </summary>
    private void CompareButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ListComparisonResult result =
            ListComparerProcessor.Compare(
                ListMaker1.GetArticleList(),
                ListMaker2.GetArticleList());

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

    private void ClearButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ClearResults();
    }

    private void ClearResults()
    {
        _onlyInList1.Clear();
        _onlyInList2.Clear();
        _common.Clear();

        RefreshResultLists();
    }

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

    private void UpdateControlState()
    {
        CompareButton.IsEnabled =
            ListMaker1.Count > 0 &&
            ListMaker2.Count > 0;
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

    private void UseOnlyList1Button_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ListMaker1.AddArticles(
            _onlyInList1);
    }

    private void UseCommonButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ListMaker1.AddArticles(
            _common);
    }

    private void UseOnlyList2Button_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ListMaker2.AddArticles(
            _onlyInList2);
    }

    private async void SaveOnlyList1Button_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveResultListAsync(
            _onlyInList1,
            "List 1 unique articles.txt");
    }

    private async void SaveCommonButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveResultListAsync(
            _common,
            "Common articles.txt");
    }

    private async void SaveOnlyList2Button_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveResultListAsync(
            _onlyInList2,
            "List 2 unique articles.txt");
    }

    private async Task SaveResultListAsync(
        IEnumerable<Article> articles,
        string suggestedFileName)
    {
        IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    SuggestedFileName = suggestedFileName,
                    FileTypeChoices =
                    [
                        new FilePickerFileType("Text files")
                        {
                            Patterns = ["*.txt"]
                        }
                    ]
                });

        if (file is null)
            return;

        string? path =
            file.TryGetLocalPath();

        if (string.IsNullOrEmpty(path))
            return;

        string text =
            string.Join(
                Environment.NewLine,
                articles.Select(article => article.Name));

        Tools.WriteTextFileAbsolutePath(
            text,
            path,
            false);
    }

    private void RemoveOnlyList1Selected_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        RemoveSelectedArticles(
            OnlyList1ListBox,
            _onlyInList1);
    }

    private void RemoveCommonSelected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RemoveSelectedArticles(
            CommonListBox,
            _common);
    }

    private void RemoveOnlyList2Selected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RemoveSelectedArticles(
            OnlyList2ListBox,
            _onlyInList2);
    }

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

    private void SelectAllOnlyList1_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        SelectAllArticles(
            OnlyList1ListBox);
    }

    private void SelectAllCommon_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SelectAllArticles(
            CommonListBox);
    }

    private void SelectAllOnlyList2_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SelectAllArticles(
            OnlyList2ListBox);
    }

    private static void SelectAllArticles(
    ListBox listBox)
    {
        listBox.Selection.SelectAll();
    }

    private async void CopyOnlyList1Selected_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CopySelectedArticlesAsync(
            OnlyList1ListBox);
    }

    private async void CopyCommonSelected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CopySelectedArticlesAsync(
            CommonListBox);
    }

    private async void CopyOnlyList2Selected_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CopySelectedArticlesAsync(
            OnlyList2ListBox);
    }

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

    private void OpenOnlyList1InBrowser_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenSelectedArticlesInBrowser(
            OnlyList1ListBox);
    }

    private void OpenCommonInBrowser_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenSelectedArticlesInBrowser(
            CommonListBox);
    }

    private void OpenOnlyList2InBrowser_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenSelectedArticlesInBrowser(
            OnlyList2ListBox);
    }

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
}