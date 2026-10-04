using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Twain.UI.BotSettings;

public partial class ShutdownNotificationWindow : Window
{
    public ShutdownNotificationWindow()
    {
        InitializeComponent();
    }

    public ShutdownNotificationWindow(
        BotShutdownAction shutdownAction)
        : this()
    {
        MessageTextBlock.Text =
            $"Article processing has completed.\n\n" +
            $"Continue with {GetActionName(shutdownAction)}?";
    }

    private static string GetActionName(
        BotShutdownAction shutdownAction)
    {
        return shutdownAction switch
        {
            BotShutdownAction.Shutdown => "shutdown",
            BotShutdownAction.Standby => "standby",
            BotShutdownAction.Restart => "restart",
            BotShutdownAction.Hibernate => "hibernate",
            _ => "the configured action"
        };
    }

    private void ContinueButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}