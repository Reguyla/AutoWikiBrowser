using Avalonia;
using System.Diagnostics;
using Twain.Diagnostics;
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
        // TODO: Investigate Velopack Setup reporting a failed install hook even though
        // the application installs successfully and the install hook returns exit code
        // 0 when invoked manually.
        VelopackApp.Build().Run();

        Twain.UI.App.UpdateServiceFactory =
            static () => new VelopackUpdateService();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Configures local diagnostic storage for the Twain desktop application.
    /// </summary>
    /// <remarks>
    /// Diagnostic events are stored in the user's local application data directory
    /// as newline-delimited JSON. This storage is local only and does not transmit
    /// diagnostic information to an external service.
    ///
    /// Diagnostic initialization is best-effort. A failure to initialize diagnostic
    /// storage must not prevent Twain from starting.
    /// </remarks>
    private static void InitializeDiagnostics()
    {
        try
        {
            string diagnosticsDirectory =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "Twain",
                    "Diagnostics");

            string diagnosticsFile =
                Path.Combine(
                    diagnosticsDirectory,
                    "diagnostics.jsonl");

            TwainDiagnostics.Configure(
                new LocalDiagnosticSink(diagnosticsFile));
        }
        catch (Exception ex)
        {
            // Diagnostics must never prevent Twain from starting.
            Debug.WriteLine(
                $"Unable to initialize Twain diagnostics: {ex}");
        }
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