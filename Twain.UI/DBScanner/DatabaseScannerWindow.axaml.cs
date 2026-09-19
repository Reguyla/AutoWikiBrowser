using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Twain.Core;
using Twain.Core.Background;
using Twain.Core.DBScanner;
using Twain.Core.Lists;
using Twain.UI.Lists;

namespace Twain.UI.DBScanner;

/// <summary>
/// Provides the user interface for scanning MediaWiki XML database dumps.
/// </summary>
public partial class DatabaseScannerWindow : Window
{
    private MainProcess? _mainProcess;
    private bool _manualStop;
    private bool _resultLimitReached;

    private readonly CrossThreadQueue<string> _outputQueue =
        new();

    private readonly DispatcherTimer _progressTimer;

    private readonly ArticleListFilterConfiguration _filterConfiguration =
        new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseScannerWindow"/> class.
    /// </summary>
    public DatabaseScannerWindow()
    {
        InitializeComponent();

        LengthComparisonComboBox.SelectionChanged +=
            LengthComparisonComboBox_SelectionChanged;

        LinksComparisonComboBox.SelectionChanged +=
            LinksComparisonComboBox_SelectionChanged;

        WordsComparisonComboBox.SelectionChanged +=
            WordsComparisonComboBox_SelectionChanged;

        _progressTimer =
            new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

        _progressTimer.Tick += ProgressTimer_Tick;
    }

    private void ProgressTimer_Tick(
        object? sender,
        EventArgs e)
    {
        DrainOutputQueue();

        if (_mainProcess is null)
            return;

        int matchCount =
            ResultsListBox.ItemCount;

        int resultLimit =
            (int)(ResultLimitNumericUpDown.Value ?? 30000);

        double progress =
            DatabaseScannerProcessor.CalculateProgress(
                matchCount,
                resultLimit,
                _mainProcess.PercentageComplete);

        double percentage =
            progress * 100;

        ScanProgressBar.Value =
            percentage;

        ProgressTextBlock.Text =
            $"{percentage:0}%";

        if (matchCount >= resultLimit)
        {
            _resultLimitReached = true;
            _mainProcess.Stop();
        }
    }

    private void ArticleContainsCheckBox_IsCheckedChanged(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        ArticleContainsTextBox.IsEnabled =
            ArticleContainsCheckBox.IsChecked == true;
    }

    private void ArticleDoesNotContainCheckBox_IsCheckedChanged(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ArticleDoesNotContainTextBox.IsEnabled =
            ArticleDoesNotContainCheckBox.IsChecked == true;
    }

    private void ArticleRegexCheckBox_IsCheckedChanged(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool enabled =
            ArticleRegexCheckBox.IsChecked == true;

        ArticleSinglelineCheckBox.IsEnabled = enabled;
        ArticleMultilineCheckBox.IsEnabled = enabled;
    }

    private void TitleContainsCheckBox_IsCheckedChanged(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        TitleContainsTextBox.IsEnabled =
            TitleContainsCheckBox.IsChecked == true;
    }

    private void TitleDoesNotContainCheckBox_IsCheckedChanged(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        TitleDoesNotContainTextBox.IsEnabled =
            TitleDoesNotContainCheckBox.IsChecked == true;
    }

    private void SearchDatesCheckBox_IsCheckedChanged(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool enabled =
            SearchDatesCheckBox.IsChecked == true;

        DateFromPicker.IsEnabled = enabled;
        DateToPicker.IsEnabled = enabled;
    }

    private void LengthComparisonComboBox_SelectionChanged(
    object? sender,
    SelectionChangedEventArgs e)
    {
        LengthNumericUpDown.IsEnabled =
            LengthComparisonComboBox.SelectedIndex != 0;
    }

    private void LinksComparisonComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        LinksNumericUpDown.IsEnabled =
            LinksComparisonComboBox.SelectedIndex != 0;
    }

    private void WordsComparisonComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        WordsNumericUpDown.IsEnabled =
            WordsComparisonComboBox.SelectedIndex != 0;
    }

    private void CheckProtectionCheckBox_IsCheckedChanged(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        ProtectionControl.IsEnabled =
            CheckProtectionCheckBox.IsChecked == true;
    }

    private DatabaseScannerOptions CreateOptions()
    {
        return new DatabaseScannerOptions
        {
            FileName =
                DumpLocationTextBox.Text ?? string.Empty,

            StartFrom =
                StartFromTextBox.Text ?? string.Empty,

            IgnoreRedirects =
                IgnoreRedirectsCheckBox.IsChecked == true,

            IgnoreComments =
                IgnoreCommentsCheckBox.IsChecked == true,

             Priority =
                PriorityComboBox.SelectedIndex switch
                {
                    0 => ThreadPriority.Highest,
                    1 => ThreadPriority.AboveNormal,
                    3 => ThreadPriority.BelowNormal,
                    4 => ThreadPriority.Lowest,
                    _ => ThreadPriority.Normal
                },

            ResultLimit =
                (int)(ResultLimitNumericUpDown.Value ?? 30000),

            Namespaces =
                NamespacesControl.GetSelectedNamespaces(),

            TitleContainsEnabled =
                TitleContainsCheckBox.IsChecked == true,

            TitleContains =
                TitleContainsTextBox.Text ?? string.Empty,

            TitleDoesNotContainEnabled =
                TitleDoesNotContainCheckBox.IsChecked == true,

            TitleDoesNotContain =
                TitleDoesNotContainTextBox.Text ?? string.Empty,

            TitleRegex =
                TitleRegexCheckBox.IsChecked == true,

            TitleCaseSensitive =
                TitleCaseSensitiveCheckBox.IsChecked == true,

            ArticleContainsEnabled =
                ArticleContainsCheckBox.IsChecked == true,

            ArticleContains =
                ArticleContainsTextBox.Text ?? string.Empty,

            ArticleDoesNotContainEnabled =
                ArticleDoesNotContainCheckBox.IsChecked == true,

            ArticleDoesNotContain =
                ArticleDoesNotContainTextBox.Text ?? string.Empty,

            ArticleRegex =
                ArticleRegexCheckBox.IsChecked == true,

            ArticleCaseSensitive =
                ArticleCaseSensitiveCheckBox.IsChecked == true,

            ArticleRegexMultiline =
                ArticleMultilineCheckBox.IsChecked == true,

            ArticleRegexSingleline =
                ArticleSinglelineCheckBox.IsChecked == true,

            SearchDates =
                SearchDatesCheckBox.IsChecked == true,

            DateFrom =
               DateFromPicker.SelectedDate ?? DateTime.MinValue,

            DateTo =
               DateToPicker.SelectedDate ?? DateTime.MaxValue,

            CheckProtection =
                CheckProtectionCheckBox.IsChecked == true,

            EditProtectionLevel =
                ProtectionControl.EditProtectionLevel,

            MoveProtectionLevel =
                ProtectionControl.MoveProtectionLevel,

            LengthComparison =
                LengthComparisonComboBox.SelectedIndex,

            Length =
                (int)(LengthNumericUpDown.Value ?? 1000),

            LinkComparison =
                LinksComparisonComboBox.SelectedIndex,

            Links =
                (int)(LinksNumericUpDown.Value ?? 5),

            WordComparison =
                WordsComparisonComboBox.SelectedIndex,

            Words =
                (int)(WordsNumericUpDown.Value ?? 200),

            CheckBadLinks =
                BadLinksCheckBox.IsChecked == true,

            CheckNoBoldTitle =
                NoBoldTitleCheckBox.IsChecked == true,

            CheckCiteTemplateDates =
                CiteTemplateDatesCheckBox.IsChecked == true,

            CheckReorderReferences =
                ReorderReferencesCheckBox.IsChecked == true,

            CheckPeopleCategories =
                PeopleCategoriesCheckBox.IsChecked == true,

            CheckUnbalancedBrackets =
                UnbalancedBracketsCheckBox.IsChecked == true,

            CheckSimpleLinks =
                SimpleLinksCheckBox.IsChecked == true,

            CheckHtmlEntities =
                HtmlEntitiesCheckBox.IsChecked == true,

            CheckSectionErrors =
                SectionErrorsCheckBox.IsChecked == true,

            CheckUnbulletedLinks =
                UnbulletedLinksCheckBox.IsChecked == true,

            CheckTypos =
               TypoCheckBox.IsChecked == true,

            CheckMissingDefaultSort =
                MissingDefaultSortCheckBox.IsChecked == true
        };
    }

    private MainProcess CreateMainProcess(
    DatabaseScannerOptions options)
    {
        List<Scan> scanners =
            DatabaseScannerProcessor.CreateScanners(options);

        MainProcess mainProcess =
            new(
                scanners,
                options.FileName,
                options.Priority,
                options.IgnoreComments,
                options.StartFrom)
            {
                OutputQueue = _outputQueue
            };

        return mainProcess;
    }

    private async void BrowseButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        IReadOnlyList<Avalonia.Platform.Storage.IStorageFile> files =
            await StorageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Open MediaWiki XML database dump",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new Avalonia.Platform.Storage.FilePickerFileType(
                        "XML database dump")
                    {
                        Patterns = ["*.xml"]
                    }
                    ]
                });

        if (files.Count == 0)
            return;

        string? fileName =
            files[0].TryGetLocalPath();

        if (string.IsNullOrEmpty(fileName))
            return;

        FileInfo fileInfo =
            new(fileName);

        if (!fileInfo.Exists ||
            fileInfo.Length == 0)
        {
            return;
        }

        DumpLocationTextBox.Text = fileName;

        DatabaseDumpMetadata metadata =
            DatabaseScannerProcessor.ReadDumpMetadata(fileName);

        DumpSiteNameTextBox.Text =
            metadata.SiteName;

        DumpBaseUrlTextBox.Text =
            metadata.BaseUrl;

        DumpGeneratorTextBox.Text =
            metadata.Generator;

        DumpCaseTextBox.Text =
            metadata.Case;
    }

    private void MainProcess_Stopped()
    {
        _progressTimer.Stop();

        DrainOutputQueue();

        if (!_manualStop &&
            !_resultLimitReached)
        {
            ScanProgressBar.Value = 100;
            ProgressTextBlock.Text = "100%";
        }

        _mainProcess = null;

        SetScanningState(false);
    }

    private void DrainOutputQueue()
    {
        while (_outputQueue.Count > 0)
        {
            string articleTitle =
                _outputQueue.Remove();

            ResultsListBox.Items.Add(articleTitle);
        }

        int matchCount =
            ResultsListBox.ItemCount;

        ResultCountTextBlock.Text =
            $"{matchCount} matches";
    }

    private void StartButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_mainProcess is not null)
        {
            _manualStop = true;
            _mainProcess.Stop();
            return;
        }

        DatabaseScannerOptions options =
            CreateOptions();

        if (string.IsNullOrWhiteSpace(options.FileName))
            return;

        ResultsListBox.Items.Clear();
        ResultCountTextBlock.Text = "0 matches";
        ScanProgressBar.Value = 0;
        ProgressTextBlock.Text = "0%";

        _mainProcess =
            CreateMainProcess(options);

        _mainProcess.StoppedEvent += MainProcess_Stopped;

        SetScanningState(true);

        _progressTimer.Start();
        _mainProcess.Start();
    }

    private void SetScanningState(bool isScanning)
    {
        ScannerTabs.IsEnabled = !isScanning;

        DumpLocationTextBox.IsEnabled = !isScanning;
        BrowseButton.IsEnabled = !isScanning;
        StartFromTextBox.IsEnabled = !isScanning;
        IgnoreRedirectsCheckBox.IsEnabled = !isScanning;
        PriorityComboBox.IsEnabled = !isScanning;
        ResultLimitNumericUpDown.IsEnabled = !isScanning;

        StartButton.Content =
            isScanning ? "Stop" : "Start";
    }

    private void ResetButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        ArticleContainsCheckBox.IsChecked = false;
        ArticleContainsTextBox.Text = string.Empty;

        ArticleDoesNotContainCheckBox.IsChecked = false;
        ArticleDoesNotContainTextBox.Text = string.Empty;

        ArticleRegexCheckBox.IsChecked = false;
        ArticleCaseSensitiveCheckBox.IsChecked = false;
        ArticleSinglelineCheckBox.IsChecked = false;
        ArticleMultilineCheckBox.IsChecked = false;

        TitleContainsCheckBox.IsChecked = false;
        TitleContainsTextBox.Text = string.Empty;

        TitleDoesNotContainCheckBox.IsChecked = false;
        TitleDoesNotContainTextBox.Text = string.Empty;

        TitleRegexCheckBox.IsChecked = false;
        TitleCaseSensitiveCheckBox.IsChecked = false;

        SearchDatesCheckBox.IsChecked = false;
        DateFromPicker.SelectedDate = null;
        DateToPicker.SelectedDate = null;

        CheckProtectionCheckBox.IsChecked = false;
        ProtectionControl.Reset();

        LengthComparisonComboBox.SelectedIndex = 0;
        LengthNumericUpDown.Value = 1000;

        LinksComparisonComboBox.SelectedIndex = 0;
        LinksNumericUpDown.Value = 5;

        WordsComparisonComboBox.SelectedIndex = 0;
        WordsNumericUpDown.Value = 200;

        NoBoldTitleCheckBox.IsChecked = false;
        PeopleCategoriesCheckBox.IsChecked = false;
        UnbalancedBracketsCheckBox.IsChecked = false;
        SimpleLinksCheckBox.IsChecked = false;
        ReorderReferencesCheckBox.IsChecked = false;
        BadLinksCheckBox.IsChecked = false;
        HtmlEntitiesCheckBox.IsChecked = false;
        SectionErrorsCheckBox.IsChecked = false;
        UnbulletedLinksCheckBox.IsChecked = false;
        CiteTemplateDatesCheckBox.IsChecked = false;
        TypoCheckBox.IsChecked = false;
        MissingDefaultSortCheckBox.IsChecked = false;

        NamespacesControl.Reset();

        WikiHeadingCheckBox.IsChecked = false;
        WikiHeadingSpacingNumericUpDown.Value = 25;
        WikiAlphabeticalHeaderCheckBox.IsChecked = false;
        WikiNumberedListCheckBox.IsChecked = true;

        WikiListTextBox.Text = string.Empty;

        StartFromTextBox.Text = string.Empty;
        IgnoreRedirectsCheckBox.IsChecked = true;
        IgnoreCommentsCheckBox.IsChecked = false;
        PriorityComboBox.SelectedIndex = 2;

        DumpLocationTextBox.Text = string.Empty;
        DumpSiteNameTextBox.Text = string.Empty;
        DumpBaseUrlTextBox.Text = string.Empty;
        DumpGeneratorTextBox.Text = string.Empty;
        DumpCaseTextBox.Text = string.Empty;

        ResultLimitNumericUpDown.Value = 30000;
    }

    private void ClearResultsButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        ResultsListBox.Items.Clear();

        ResultCountTextBlock.Text = "0 matches";
    }

    private async void SaveResultsButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ResultsListBox.ItemCount == 0)
            return;

        Avalonia.Platform.Storage.IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Save article list",
                    SuggestedFileName = "DatabaseScannerResults.txt",
                    DefaultExtension = "txt",
                    FileTypeChoices =
                    [
                        new Avalonia.Platform.Storage.FilePickerFileType(
                        "Text file")
                    {
                        Patterns = ["*.txt"]
                    }
                    ]
                });

        if (file is null)
            return;

        string? fileName =
            file.TryGetLocalPath();

        if (string.IsNullOrEmpty(fileName))
            return;

        IEnumerable<string> results =
            ResultsListBox.Items
                .OfType<string>();

        File.WriteAllLines(
            fileName,
            results);
    }

    private void RemoveSelectedButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        List<object?> selectedItems =
            ResultsListBox.SelectedItems?
                .Cast<object?>()
                .ToList()
            ?? [];

        foreach (object? item in selectedItems)
        {
            ResultsListBox.Items.Remove(item);
        }

        int matchCount =
            ResultsListBox.ItemCount;

        ResultCountTextBlock.Text =
            $"{matchCount} matches";
    }

    private async void CopySelectedButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        List<string> selectedItems =
            ResultsListBox.SelectedItems?
                .OfType<string>()
                .ToList()
            ?? [];

        if (selectedItems.Count == 0)
            return;

        string text =
            string.Join(
                Environment.NewLine,
                selectedItems);

        Avalonia.Input.Platform.IClipboard? clipboard =
            TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is null)
            return;

        await clipboard.SetTextAsync(text);
    }

    private void GenerateWikiListButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        List<Article> articles =
            ResultsListBox.Items
                .OfType<string>()
                .Select(title => new Article(title))
                .ToList();

        WikiListTextBox.Text =
            DatabaseScannerProcessor.CreateWikiList(
                articles,
                WikiNumberedListCheckBox.IsChecked == true,
                WikiHeadingCheckBox.IsChecked == true,
                (int)(WikiHeadingSpacingNumericUpDown.Value ?? 25),
                WikiAlphabeticalHeaderCheckBox.IsChecked == true);
    }

    private void WikiHeadingCheckBox_IsCheckedChanged(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        WikiHeadingSpacingNumericUpDown.IsEnabled =
            WikiHeadingCheckBox.IsChecked == true;
    }

    private void ClearWikiListButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        WikiListTextBox.Text = string.Empty;
    }

    private async void CopyWikiListButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        string text =
            WikiListTextBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(text))
            return;

        var clipboard =
            TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is null)
            return;

        await clipboard.SetTextAsync(text);
    }

    private async void SaveWikiListButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        string text =
            WikiListTextBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(text))
            return;

        Avalonia.Platform.Storage.IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Save wiki list",
                    SuggestedFileName = "DatabaseScannerWikiList.txt",
                    DefaultExtension = "txt",
                    FileTypeChoices =
                    [
                        new Avalonia.Platform.Storage.FilePickerFileType(
                        "Text file")
                    {
                        Patterns = ["*.txt"]
                    }
                    ]
                });

        if (file is null)
            return;

        string? fileName =
            file.TryGetLocalPath();

        if (string.IsNullOrEmpty(fileName))
            return;

        await File.WriteAllTextAsync(
            fileName,
            text);
    }

    private async void FilterButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!ResultsListBox.Items.OfType<string>().Any())
            return;

        ListFilterWindow filterWindow =
            new(_filterConfiguration);

        bool? result =
            await filterWindow.ShowDialog<bool?>(this);

        if (result != true)
            return;

        List<Article> articles =
            ResultsListBox.Items
                .OfType<string>()
                .Select(title => new Article(title))
                .ToList();

        List<Article> comparisonArticles =
            _filterConfiguration.ComparisonArticleTitles
                .Select(title => new Article(title))
                .ToList();

        List<Article> filteredArticles =
            ArticleListFilterProcessor.Apply(
                articles,
                comparisonArticles,
                _filterConfiguration.NamespaceIds,
                _filterConfiguration.ContainsText,
                _filterConfiguration.DoesNotContainText,
                _filterConfiguration.FilterTitlesThatContain &&
                    !string.IsNullOrEmpty(_filterConfiguration.ContainsText),
                _filterConfiguration.FilterTitlesThatDoNotContain &&
                    !string.IsNullOrEmpty(_filterConfiguration.DoesNotContainText),
                _filterConfiguration.UseRegex,
                _filterConfiguration.RemoveDuplicates,
                comparisonArticles.Count > 0,
                _filterConfiguration.IntersectComparisonList,
                _filterConfiguration.SortAscending);

        ResultsListBox.ItemsSource =
            filteredArticles
                .Select(article => article.Name)
                .ToList();

        ResultCountTextBlock.Text =
            $"{filteredArticles.Count} matches";
    }
}