using Avalonia.Controls;
using Avalonia.Interactivity;
using Twain.Core.Lists.Providers;
using Twain.UI.Controls;
using Twain.UI.Lists.Providers;

namespace Twain.UI.Lists.Providers;

public partial class AdvancedRegexHtmlScraperWindow : Window
{
    public AdvancedRegexHtmlScraperWindow()
    {
        InitializeComponent();
    }

    public AdvancedRegexHtmlScraperWindow(
        AdvancedRegexHtmlScraper.ScraperSettings? settings)
        : this()
    {
        if (settings is null)
        {
            return;
        }

        RegexTextBox.Text =
            settings.Pattern;

        GroupNumberInput.Value =
            settings.GroupNumber;

        CaseSensitiveCheckBox.IsChecked =
            settings.CaseSensitive;

        SingleLineCheckBox.IsChecked =
            settings.SingleLine;

        MultiLineCheckBox.IsChecked =
            settings.MultiLine;
    }

    public AdvancedRegexHtmlScraper.ScraperSettings
        CreateSettings()
    {
        return new AdvancedRegexHtmlScraper.ScraperSettings(
            RegexTextBox.Text ?? string.Empty,
            (int)(GroupNumberInput.Value ?? 0),
            CaseSensitiveCheckBox.IsChecked == true,
            SingleLineCheckBox.IsChecked == true,
            MultiLineCheckBox.IsChecked == true);
    }

    private async void OpenRegexTesterButton_Click(
    object? sender,
    RoutedEventArgs e)
    {
        RegexTesterWindow tester =
            new(true)
            {
                Find =
                    RegexTextBox.Text ??
                    string.Empty,

                IgnoreCase =
                    CaseSensitiveCheckBox.IsChecked != true,

                Multiline =
                    MultiLineCheckBox.IsChecked == true,

                Singleline =
                    SingleLineCheckBox.IsChecked == true
            };

        await tester.ShowDialog(
            this);

        if (tester.ApplyChangesResult != true)
        {
            return;
        }

        RegexTextBox.Text =
            tester.Find;

        CaseSensitiveCheckBox.IsChecked =
            !tester.IgnoreCase;

        MultiLineCheckBox.IsChecked =
            tester.Multiline;

        SingleLineCheckBox.IsChecked =
            tester.Singleline;
    }

    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        AdvancedRegexHtmlScraper.ScraperSettings settings =
            CreateSettings();

        if (!AdvancedRegexHtmlScraper.TryValidateSettings(
                settings,
                out string errorMessage))
        {
            ValidationMessageTextBlock.Text =
                errorMessage;

            ValidationMessageTextBlock.IsVisible =
                true;

            return;
        }

        ValidationMessageTextBlock.Text =
            string.Empty;

        ValidationMessageTextBlock.IsVisible =
            false;

        Close(true);
    }

    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}