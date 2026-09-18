using Avalonia.Controls;

namespace Twain.UI.DBScanner;

/// <summary>
/// Provides the user interface for scanning MediaWiki XML database dumps.
/// </summary>
public partial class DatabaseScannerWindow : Window
{
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
}