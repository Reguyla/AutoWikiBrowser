namespace Twain.UI.Logging;

/// <summary>
/// Displays a non-interactive wait window while a log is being uploaded.
/// </summary>
public partial class UploadingPleaseWaitWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="UploadingPleaseWaitWindow"/> class.
    /// </summary>
    public UploadingPleaseWaitWindow()
    {
        InitializeComponent();

        Cursor =
            new Avalonia.Input.Cursor(
                Avalonia.Input.StandardCursorType.Wait);
    }
}