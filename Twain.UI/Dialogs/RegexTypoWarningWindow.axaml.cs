using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Twain.UI.Dialogs;

/// <summary>
/// Warns the editor before downloading and enabling
/// regex typo correction rules.
/// </summary>
public partial class RegexTypoWarningWindow : Window
{
    /// <summary>
    /// Initializes the regex typo warning dialog.
    /// </summary>
    public RegexTypoWarningWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the dialog with the configured typo-list source.
    /// </summary>
    /// <param name="sourceUrl">
    /// URL from which the typo rules will be downloaded.
    /// </param>
    public RegexTypoWarningWindow(string sourceUrl)
        : this()
    {
        SourceUrlText.Text = sourceUrl;
    }

    /// <summary>
    /// Confirms that typo rule loading may proceed.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Cancels typo rule loading.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}