using Avalonia;
using System.Diagnostics;
using System.Text;
using Twain.Diagnostics;
using Twain.Desktop.Updates;
using Twain.UI.BotSettings;
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
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // TODO: Investigate Velopack Setup reporting a failed install hook even though
        // the application installs successfully and the install hook returns exit code
        // 0 when invoked manually.
        VelopackApp.Build().Run();

        InitializeDiagnostics();

        TwainDiagnostics.WriteAsync(
            DiagnosticCategory.System,
            "ApplicationStarted",
            "Twain application started.")
            .GetAwaiter()
            .GetResult();

        Twain.UI.App.UpdateServiceFactory =
            static () => new VelopackUpdateService();

        Twain.UI.App.SystemPowerAction =
           ExecuteSystemPowerAction;

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
    /// Performs the requested operating-system power action.
    /// </summary>
    /// <param name="shutdownAction">
    /// The power action requested by the Twain user interface.
    /// </param>
    private static void ExecuteSystemPowerAction(
        BotShutdownAction shutdownAction)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ProcessStartInfo startInfo =
            shutdownAction switch
            {
                BotShutdownAction.Shutdown =>
                    new ProcessStartInfo(
                        "shutdown.exe",
                        "/s /t 0"),

                BotShutdownAction.Restart =>
                    new ProcessStartInfo(
                        "shutdown.exe",
                        "/r /t 0"),

                BotShutdownAction.Standby =>
                    new ProcessStartInfo(
                        "rundll32.exe",
                        "powrprof.dll,SetSuspendState 0,1,0"),

                BotShutdownAction.Hibernate =>
                    new ProcessStartInfo(
                        "rundll32.exe",
                        "powrprof.dll,SetSuspendState Hibernate"),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(shutdownAction),
                    shutdownAction,
                    "Unsupported system power action.")
            };

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        try
        {
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            TwainDiagnostics.WriteAsync(
                DiagnosticCategory.System,
                "SystemPowerActionFailed",
                $"Unable to perform system power action '{shutdownAction}': {ex}")
                .GetAwaiter()
                .GetResult();
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