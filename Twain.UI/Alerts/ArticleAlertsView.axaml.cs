using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Twain.UI.Alerts;

/// <summary>
/// Displays alerts detected in the current article.
/// </summary>
public partial class ArticleAlertsView : UserControl
{
    /// <summary>
    /// Raised when the user double-clicks an alert.
    /// </summary>
    public event EventHandler<string>? AlertActivated;

    /// <summary>
    /// Raised when the user requests alert configuration.
    /// </summary>
    public event EventHandler? SettingsRequested;

    public ArticleAlertsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles double-clicking an alert entry.
    /// </summary>
    private void AlertItem_PointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2)
        {
            return;
        }

        if (sender is not Border border ||
            border.DataContext is not string alertText)
        {
            return;
        }

        AlertActivated?.Invoke(this, alertText);

        e.Handled = true;
    }

    /// <summary>
    /// Requests navigation to the Alerts preferences.
    /// </summary>
    private void AlertsSettingsButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }
}
