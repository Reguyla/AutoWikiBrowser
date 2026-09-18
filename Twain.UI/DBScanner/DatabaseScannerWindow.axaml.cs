using Avalonia.Controls;
using Twain.Core.DBScanner;

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

    private DatabaseScannerOptions CreateOptions()
    {
        return new DatabaseScannerOptions
        {
            FileName =
                DumpLocationTextBox.Text ?? string.Empty,

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
}