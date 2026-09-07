using Avalonia.Threading;

namespace Twain.UI.Background;

/// <summary>
/// Displays status and progress information for a background operation.
/// </summary>
public partial class PleaseWaitWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Occurs when the user requests cancellation of the background
    /// operation.
    /// </summary>
    public event EventHandler? CancelRequested;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PleaseWaitWindow"/> class.
    /// </summary>
    public PleaseWaitWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the status text displayed for the current operation.
    /// </summary>
    public string Status
    {
        get => StatusTextBlock.Text ?? string.Empty;

        set
        {
            string status =
                value ?? string.Empty;

            if (Dispatcher.UIThread.CheckAccess())
            {
                StatusTextBlock.Text = status;
                return;
            }

            Dispatcher.UIThread.Post(
                () => StatusTextBlock.Text = status);
        }
    }

    /// <summary>
    /// Updates the progress displayed for the current operation.
    /// </summary>
    /// <param name="completed">
    /// The number of completed items.
    /// </param>
    /// <param name="total">
    /// The total number of items.
    /// </param>
    public void SetProgress(
        int completed,
        int total)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            SetProgressCore(
                completed,
                total);

            return;
        }

        Dispatcher.UIThread.Post(
            () => SetProgressCore(
                completed,
                total));
    }

    /// <summary>
    /// Applies progress information to the window controls.
    /// </summary>
    private void SetProgressCore(
        int completed,
        int total)
    {
        int safeTotal =
            Math.Max(total, 1);

        int safeCompleted =
            Math.Clamp(
                completed,
                0,
                safeTotal);

        ProgressBar.Maximum =
            safeTotal;

        ProgressBar.Value =
            safeCompleted;

        ProgressGroupBox.Header =
            $"{completed}/{total} complete";
    }

    /// <summary>
    /// Raises the cancellation request and closes the window.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelRequested?.Invoke(
            this,
            EventArgs.Empty);

        Close();
    }
}