using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Twain.Core.Updates;
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
        }

        base.OnFrameworkInitializationCompleted();
    }
}