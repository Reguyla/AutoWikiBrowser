using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using Twain.Core;
using Twain.Core.Lists;
using Twain.Core.Lists.Providers;

namespace Twain.UI.Lists;

public partial class ListFilterWindow : Avalonia.Controls.Window
{

    private List<NamespaceFilterItem> _namespaceItems = new();
    private List<NamespaceFilterItem> _contentNamespaceItems = new();
    private List<NamespaceFilterItem> _talkNamespaceItems = new();

    private List<string> _comparisonArticleTitles = new();

    private string _project = Variables.URL;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListFilterWindow"/> class
    /// with a new filter configuration.
    /// </summary>
    public ListFilterWindow()
    {
        InitializeComponent();

        PopulateNamespaces();
    }

    /// <summary>
    /// Populates the content and talk namespace lists for the active wiki.
    /// </summary>
    private void PopulateNamespaces()
    {
        _namespaceItems =
            NamespaceFilterHelper.GetAvailableNamespaces();

        _contentNamespaceItems =
            _namespaceItems
                .Where(item => !item.IsTalk)
                .ToList();

        _talkNamespaceItems =
            _namespaceItems
                .Where(item => item.IsTalk)
                .ToList();

        ContentNamespaceList.ItemsSource =
            _contentNamespaceItems;

        TalkNamespaceList.ItemsSource =
            _talkNamespaceItems;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListFilterWindow"/> class
    /// using the supplied filter configuration.
    /// </summary>
    /// <param name="configuration">
    /// The filter configuration to display and edit.
    /// </param>
    public ListFilterWindow(
        ArticleListFilterConfiguration configuration)
        : this()
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Configuration = configuration;
        LoadConfiguration();
    }

    /// <summary>
    /// Gets the filter configuration represented by the current window state.
    /// </summary>
    public ArticleListFilterConfiguration Configuration { get; private set; } = new();

    /// <summary>
    /// Updates the title-filter controls when the contains option changes.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void ContainsCheckBox_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateTitleFilterControls();
    }

    /// <summary>
    /// Updates the title-filter controls when the does-not-contain option changes.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void DoesNotContainCheckBox_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateTitleFilterControls();
    }

    /// <summary>
    /// Enables or disables the title-filter inputs according to the selected
    /// filtering options.
    /// </summary>
    private void UpdateTitleFilterControls()
    {
        ContainsTextBox.IsEnabled =
            ContainsCheckBox.IsChecked == true;

        DoesNotContainTextBox.IsEnabled =
            DoesNotContainCheckBox.IsChecked == true;

        RegexCheckBox.IsEnabled =
            ContainsCheckBox.IsChecked == true ||
            DoesNotContainCheckBox.IsChecked == true;
    }

    /// <summary>
    /// Saves the current filter configuration and closes the window with a
    /// successful result.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void ApplyButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SaveConfiguration();
        Close(true);
    }

    /// <summary>
    /// Closes the window without applying the current filter configuration.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void CancelButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    /// <summary>
    /// Loads the current filter configuration into the window controls.
    /// </summary>
    private void LoadConfiguration()
    {
        ContainsCheckBox.IsChecked =
            Configuration.FilterTitlesThatContain;

        ContainsTextBox.Text =
            Configuration.ContainsText;

        DoesNotContainCheckBox.IsChecked =
            Configuration.FilterTitlesThatDoNotContain;

        DoesNotContainTextBox.Text =
            Configuration.DoesNotContainText;

        RegexCheckBox.IsChecked =
            Configuration.UseRegex;

        RemoveDuplicatesCheckBox.IsChecked =
            Configuration.RemoveDuplicates;

        SortCheckBox.IsChecked =
            Configuration.SortAscending;

        OperationComboBox.SelectedIndex =
            Configuration.IntersectComparisonList
                ? 1
                : 0;

        _comparisonArticleTitles =
            new List<string>(
                Configuration.ComparisonArticleTitles);

        ComparisonList.ItemsSource =
            _comparisonArticleTitles;

        SelectNamespaces(
            Configuration.NamespaceIds);

        UpdateTitleFilterControls();
    }

    /// <summary>
    /// Captures the current window controls in the filter configuration.
    /// </summary>
    private void SaveConfiguration()
    {
        Configuration.FilterTitlesThatContain =
            ContainsCheckBox.IsChecked == true;

        Configuration.ContainsText =
            ContainsTextBox.Text ?? string.Empty;

        Configuration.FilterTitlesThatDoNotContain =
            DoesNotContainCheckBox.IsChecked == true;

        Configuration.DoesNotContainText =
            DoesNotContainTextBox.Text ?? string.Empty;

        Configuration.UseRegex =
            RegexCheckBox.IsChecked == true;

        Configuration.RemoveDuplicates =
            RemoveDuplicatesCheckBox.IsChecked == true;

        Configuration.SortAscending =
            SortCheckBox.IsChecked == true;

        Configuration.IntersectComparisonList =
            OperationComboBox.SelectedIndex != 0;


        Configuration.NamespaceIds.Clear();
        Configuration.NamespaceIds.AddRange(
            GetSelectedNamespaceIds());

        Configuration.NamespaceIds.Sort();

        Configuration.ComparisonArticleTitles.Clear();
        Configuration.ComparisonArticleTitles.AddRange(
            _comparisonArticleTitles);
    }

    /// <summary>
    /// Removes all article titles from the comparison list.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void ClearComparisonListButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        _comparisonArticleTitles.Clear();

        ComparisonList.ItemsSource = null;
        ComparisonList.ItemsSource =
            _comparisonArticleTitles;
    }

    /// <summary>
    /// Opens a file picker and adds article titles from the selected text files
    /// to the comparison list.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private async void GetComparisonListButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files =
            await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Open article list",
                    AllowMultiple = true,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Text files")
                    {
                        Patterns = ["*.txt"]
                    },
                    FilePickerFileTypes.All
                    ]
                });

        if (files.Count == 0)
            return;

        List<string> fileNames = new();

        foreach (IStorageFile file in files)
        {
            string? fileName =
                file.TryGetLocalPath();

            if (!string.IsNullOrEmpty(fileName))
                fileNames.Add(fileName);
        }

        if (fileNames.Count == 0)
            return;

        TextFileListProviderUFT8 provider = new();

        List<Article> articles =
            provider.MakeList(
                fileNames,
                validateTitles:
                    ValidateComparisonTitlesCheckBox.IsChecked == true);

        foreach (Article article in articles)
            _comparisonArticleTitles.Add(article.Name);

        ComparisonList.ItemsSource = null;
        ComparisonList.ItemsSource =
            _comparisonArticleTitles;
    }

    private void ContentNamespacesCheckBox_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentNamespaceList.SelectedItems.Clear();

        if (ContentNamespacesCheckBox.IsChecked != true)
            return;

        foreach (NamespaceFilterItem item in _contentNamespaceItems)
            ContentNamespaceList.SelectedItems.Add(item);
    }

    private void TalkNamespacesCheckBox_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        TalkNamespaceList.SelectedItems.Clear();

        if (TalkNamespacesCheckBox.IsChecked != true)
            return;

        foreach (NamespaceFilterItem item in _talkNamespaceItems)
            TalkNamespaceList.SelectedItems.Add(item);
    }

    /// <summary>
    /// Refreshes the available namespaces when the active wiki has changed.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void ListFilterWindow_Opened(
        object? sender,
        EventArgs e)
    {
        if (_project == Variables.URL)
            return;

        List<int> selectedNamespaceIds =
            GetSelectedNamespaceIds();

        _project = Variables.URL;

        PopulateNamespaces();
        SelectNamespaces(selectedNamespaceIds);
    }

    /// <summary>
    /// Gets the currently selected namespace identifiers.
    /// </summary>
    /// <returns>
    /// The selected namespace identifiers in ascending order.
    /// </returns>
    private List<int> GetSelectedNamespaceIds()
    {
        List<int> namespaceIds = new();

        foreach (NamespaceFilterItem item in
            ContentNamespaceList.SelectedItems.Cast<NamespaceFilterItem>())
        {
            namespaceIds.Add(item.Id);
        }

        foreach (NamespaceFilterItem item in
            TalkNamespaceList.SelectedItems.Cast<NamespaceFilterItem>())
        {
            namespaceIds.Add(item.Id);
        }

        namespaceIds.Sort();

        return namespaceIds;
    }

    /// <summary>
    /// Selects the namespaces having the specified identifiers.
    /// </summary>
    /// <param name="namespaceIds">
    /// The namespace identifiers to select.
    /// </param>
    private void SelectNamespaces(
        ICollection<int> namespaceIds)
    {
        ContentNamespaceList.SelectedItems.Clear();
        TalkNamespaceList.SelectedItems.Clear();

        foreach (NamespaceFilterItem item in _contentNamespaceItems)
        {
            if (namespaceIds.Contains(item.Id))
                ContentNamespaceList.SelectedItems.Add(item);
        }

        foreach (NamespaceFilterItem item in _talkNamespaceItems)
        {
            if (namespaceIds.Contains(item.Id))
                TalkNamespaceList.SelectedItems.Add(item);
        }
    }
}