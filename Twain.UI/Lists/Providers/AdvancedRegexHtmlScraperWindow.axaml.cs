using Avalonia.Controls;
using Avalonia.Interactivity;
using Twain.Core.Lists.Providers;
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