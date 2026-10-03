using Avalonia.Interactivity;

namespace Twain.UI.BotSettings;

/// <summary>
/// Displays settings used for automatic bot processing.
/// </summary>
public partial class BotSettingsWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes the bot settings window.
    /// </summary>
    public BotSettingsWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            UpdateShutdownActionSelection();
    }

    private void OkButton_Click(
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

    private void UpdateShutdownActionSelection()
    {
        if (DataContext is not BotSettingsViewModel viewModel)
        {
            return;
        }

        ShutdownRadioButton.IsChecked =
            viewModel.ShutdownAction == BotShutdownAction.Shutdown;

        StandbyRadioButton.IsChecked =
            viewModel.ShutdownAction == BotShutdownAction.Standby;

        RestartRadioButton.IsChecked =
            viewModel.ShutdownAction == BotShutdownAction.Restart;

        HibernateRadioButton.IsChecked =
            viewModel.ShutdownAction == BotShutdownAction.Hibernate;
    }

    private void ShutdownRadioButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        SetShutdownAction(BotShutdownAction.Shutdown);
    }

    private void StandbyRadioButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        SetShutdownAction(BotShutdownAction.Standby);
    }

    private void RestartRadioButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        SetShutdownAction(BotShutdownAction.Restart);
    }

    private void HibernateRadioButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        SetShutdownAction(BotShutdownAction.Hibernate);
    }

    private void SetShutdownAction(
        BotShutdownAction action)
    {
        if (DataContext is BotSettingsViewModel viewModel)
        {
            viewModel.ShutdownAction = action;
        }
    }
}