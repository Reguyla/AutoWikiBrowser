using System.Collections.Generic;
using System.Linq;
using Twain.Core.Lists;

namespace Twain.UI.Lists;

public partial class ListFilterWindow : Avalonia.Controls.Window
{

    private List<NamespaceFilterItem> _namespaceItems = new();
    private List<NamespaceFilterItem> _contentNamespaceItems = new();
    private List<NamespaceFilterItem> _talkNamespaceItems = new();

    private List<string> _comparisonArticleTitles = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ListFilterWindow"/> class
    /// with a new filter configuration.
    /// </summary>
    public ListFilterWindow()
    {
        InitializeComponent();

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

        ContentNamespaceList.SelectedItems.Clear();
        TalkNamespaceList.SelectedItems.Clear();

        foreach (NamespaceFilterItem item in _contentNamespaceItems)
        {
            if (Configuration.NamespaceIds.Contains(item.Id))
                ContentNamespaceList.SelectedItems.Add(item);
        }

        foreach (NamespaceFilterItem item in _talkNamespaceItems)
        {
            if (Configuration.NamespaceIds.Contains(item.Id))
                TalkNamespaceList.SelectedItems.Add(item);
        }

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

        foreach (NamespaceFilterItem item in
            ContentNamespaceList.SelectedItems.Cast<NamespaceFilterItem>())
        {
            Configuration.NamespaceIds.Add(item.Id);
        }

        foreach (NamespaceFilterItem item in
            TalkNamespaceList.SelectedItems.Cast<NamespaceFilterItem>())
        {
            Configuration.NamespaceIds.Add(item.Id);
        }

        Configuration.NamespaceIds.Sort();

        Configuration.ComparisonArticleTitles.Clear();
        Configuration.ComparisonArticleTitles.AddRange(
            _comparisonArticleTitles);
    }

    /// <summary>
    /// Adds the entered article title to the comparison list.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void AddComparisonTitleButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        string title =
            ComparisonTitleTextBox.Text?.Trim()
            ?? string.Empty;

        if (title.Length == 0)
            return;

        _comparisonArticleTitles.Add(title);

        ComparisonList.ItemsSource = null;
        ComparisonList.ItemsSource =
            _comparisonArticleTitles;

        ComparisonTitleTextBox.Clear();
        ComparisonTitleTextBox.Focus();
    }

    /// <summary>
    /// Removes the selected article titles from the comparison list.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void RemoveComparisonTitleButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        List<string> selectedTitles =
            ComparisonList.SelectedItems
                .Cast<string>()
                .ToList();

        if (selectedTitles.Count == 0)
            return;

        foreach (string title in selectedTitles)
            _comparisonArticleTitles.Remove(title);

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
}