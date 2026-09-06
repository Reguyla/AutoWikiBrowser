using Avalonia.Threading;

namespace Twain.UI.Splash;

/// <summary>
/// Displays application version and startup progress while Twain is being
/// initialized.
/// </summary>
public partial class SplashWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes the splash window for the designer.
    /// </summary>
    public SplashWindow()
        : this(string.Empty)
    {
    }

    /// <summary>
    /// Initializes the splash window with the supplied application version.
    /// </summary>
    /// <param name="version">
    /// The application version to display.
    /// </param>
    public SplashWindow(string version)
    {
        InitializeComponent();

        VersionTextBlock.Text =
            string.IsNullOrWhiteSpace(version)
                ? "Twain"
                : $"Version {version}";

        SetProgress(0);
    }

    /// <summary>
    /// Updates the startup progress displayed by the splash window.
    /// </summary>
    /// <param name="percent">
    /// The requested completion percentage.
    /// </param>
    /// <param name="stage">
    /// The startup stage to display. When omitted, the currently displayed
    /// stage is retained.
    /// </param>
    public void SetProgress(
        int percent,
        string? stage = null)
    {
        int progress =
            Math.Clamp(percent, 0, 100);

        if (Dispatcher.UIThread.CheckAccess())
        {
            SetProgressCore(progress, stage);
            return;
        }

        Dispatcher.UIThread.Post(
            () => SetProgressCore(progress, stage));
    }

    /// <summary>
    /// Applies a startup progress update on the Avalonia UI thread.
    /// </summary>
    private void SetProgressCore(
        int percent,
        string? stage)
    {
        if (!string.IsNullOrWhiteSpace(stage))
        {
            StageTextBlock.Text =
                stage;
        }

        StartupProgressBar.Value =
            percent;
    }

    /// <summary>
    /// Closes the splash window when it is clicked.
    /// </summary>
    protected override void OnPointerPressed(
        Avalonia.Input.PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Close();
    }
}