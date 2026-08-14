using Avalonia;
using Velopack;
using Velopack.Sources;

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
    private static async Task CheckForTestUpdateAsync()
    {
        var source = new GithubSource(
            "https://github.com/Reguyla/AutoWikiBrowser", null, false);

        var updateManager = new UpdateManager(source);

        var update = await updateManager.CheckForUpdatesAsync();

        if (update == null)
        {
            return;
        }

        await updateManager.DownloadUpdatesAsync(update);

        updateManager.ApplyUpdatesAndRestart(update);
    }
}