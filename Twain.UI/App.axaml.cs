using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.Lists.Providers;
using Twain.Core.Updates;
using Twain.UI.ErrorHandling;
using Twain.UI.Lists.Providers;
using Twain.UI.Shell;
using Twain.UI.Views.Shell;

namespace Twain.UI;

/// <summary>
/// Represents the Twain Avalonia application and configures application-wide
/// startup behavior.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets or sets the factory used to create the application update service.
    /// </summary>
    /// <remarks>
    /// The desktop host supplies the platform-specific implementation so the UI
    /// depends only on the Twain update abstraction and not on Velopack.
    /// </remarks>
    public static Func<IUpdateService>? UpdateServiceFactory { get; set; }

    /// <summary>
    /// Loads the application's XAML resources and initializes the Avalonia
    /// application infrastructure.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Completes application startup by creating the main shell window and
    /// assigning its root view model.
    /// </summary>
    /// <remarks>
    /// For desktop application lifetimes, the application creates a single
    /// <see cref="ShellWindow"/> and assigns a corresponding
    /// <see cref="ShellViewModel"/> as its data context. Future platform-
    /// specific application lifetimes, such as mobile, may provide different
    /// startup behavior.
    /// </remarks>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow =
                new ShellWindow
                {
                    DataContext =
                        new ShellViewModel()
                };

            ErrorHandler.ShowErrorDialog =
                content =>
                    ShowErrorDialog(
                        desktop,
                        content);

            ErrorHandler.ShowErrorMessage =
                (message, title) =>
                    ShowErrorMessage(
                        desktop,
                        message,
                        title);

            SpecialPageListProvider.ShowDialogAsync =
                request =>
                    ShowSpecialPageDialogAsync(
                        desktop,
                        request);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Displays the Special Page list-provider dialog using Avalonia.
    /// </summary>
    /// <param name="desktop">
    /// The active desktop application lifetime.
    /// </param>
    /// <param name="request">
    /// The provider and namespace data to display in the dialog.
    /// </param>
    /// <returns>
    /// The values selected by the user, or <see langword="null"/> when the dialog
    /// is cancelled or no visible owner window is available.
    /// </returns>
    private static Task<SpecialPageListProvider.DialogSelection?>
        ShowSpecialPageDialogAsync(
            IClassicDesktopStyleApplicationLifetime desktop,
            SpecialPageListProvider.DialogRequest request)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowSpecialPageDialogOnUiThreadAsync(
                desktop,
                request);
        }

        TaskCompletionSource<
            SpecialPageListProvider.DialogSelection?> completion =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        Dispatcher.UIThread.Post(
            async () =>
            {
                try
                {
                    SpecialPageListProvider.DialogSelection? selection =
                        await ShowSpecialPageDialogOnUiThreadAsync(
                            desktop,
                            request);

                    completion.SetResult(
                        selection);
                }
                catch (Exception exception)
                {
                    completion.SetException(
                        exception);
                }
            });

        return completion.Task;
    }

    /// <summary>
    /// Displays the Special Page dialog on the Avalonia UI thread.
    /// </summary>
    private static async Task<
        SpecialPageListProvider.DialogSelection?>
        ShowSpecialPageDialogOnUiThreadAsync(
            IClassicDesktopStyleApplicationLifetime desktop,
            SpecialPageListProvider.DialogRequest request)
    {
        if (desktop.MainWindow is not { IsVisible: true } owner)
        {
            return null;
        }

        SpecialPageListProviderWindow window =
            new(request);

        bool accepted =
            await window.ShowDialog<bool>(
                owner);

        return accepted
            ? window.CreateSelection()
            : null;
    }

    /// <summary>
    /// Displays unhandled exception information using the Avalonia error window.
    /// </summary>
    /// <param name="desktop">
    /// The active desktop application lifetime.
    /// </param>
    /// <param name="content">
    /// The error information to display.
    /// </param>
    private static void ShowErrorDialog(
        IClassicDesktopStyleApplicationLifetime desktop,
        ErrorHandler.ErrorDialogContent content)
    {
        void Show()
        {
            ErrorHandlerWindow window =
                new(content);

            if (desktop.MainWindow is { IsVisible: true })
            {
                _ = window.ShowDialog(
                    desktop.MainWindow);

                return;
            }

            window.Show();
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Show();
            return;
        }

        Dispatcher.UIThread.Post(Show);
    }

    /// <summary>
    /// Displays a simple user-facing error message using Avalonia.
    /// </summary>
    /// <param name="desktop">
    /// The active desktop application lifetime.
    /// </param>
    /// <param name="message">
    /// The message to display.
    /// </param>
    /// <param name="title">
    /// The window title.
    /// </param>
    private static void ShowErrorMessage(
        IClassicDesktopStyleApplicationLifetime desktop,
        string message,
        string title)
    {
        void Show()
        {
            ErrorMessageWindow window =
                new(
                    title,
                    message);

            if (desktop.MainWindow is { IsVisible: true })
            {
                _ = window.ShowDialog(
                    desktop.MainWindow);

                return;
            }

            window.Show();
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Show();
            return;
        }

        Dispatcher.UIThread.Post(Show);
    }
}