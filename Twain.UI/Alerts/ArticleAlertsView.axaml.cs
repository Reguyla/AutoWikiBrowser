using Avalonia.Controls;
using Avalonia.Input;

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
}
