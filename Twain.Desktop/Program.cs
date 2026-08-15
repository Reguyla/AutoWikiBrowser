using Avalonia;
using Twain.Desktop.Updates;
using Velopack;

namespace Twain.Desktop;

/// <summary>
/// Provides the desktop application entry point for Twain.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Starts the Twain desktop application.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments supplied to the application.
    /// </param>
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        Twain.UI.App.UpdateServiceFactory =
            static () => new VelopackUpdateService();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Configures Avalonia for supported desktop platforms.
    /// </summary>
    /// <returns>
    /// The configured Avalonia application builder.
    /// </returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<Twain.UI.App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}