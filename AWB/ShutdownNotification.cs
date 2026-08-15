namespace AutoWikiBrowser;

/// <summary>
/// Displays a countdown before a requested shutdown-related action occurs.
/// </summary>
public partial class ShutdownNotification : Form
{
    private const int InitialCountdownSeconds = 120;

    private readonly string _promptTemplate;

    private int _counter = InitialCountdownSeconds;
    private string _shutdownType = string.Empty;

    /// <summary>
    /// Initializes the shutdown notification dialog.
    /// </summary>
    public ShutdownNotification()
    {
        InitializeComponent();

        // Preserve the designer-provided text containing the format placeholder
        // so the shutdown type can be changed more than once safely.
        _promptTemplate = txtPrompt.Text;
    }

    /// <summary>
    /// Sets the shutdown action displayed by the notification dialog.
    /// </summary>
    /// <remarks>
    /// This write-only property updates the runtime shutdown state and refreshes
    /// the dialog text. It is not intended for Windows Forms designer
    /// serialization.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ShutdownType
    {
        set
        {
            _shutdownType = value ?? string.Empty;
            txtPrompt.Text = string.Format(
                _promptTemplate,
                _shutdownType);

            SetShutdownLabel(_counter);
        }
    }

    /// <summary>
    /// Updates the countdown label with the selected shutdown action and
    /// remaining time.
    /// </summary>
    /// <param name="time">
    /// The number of seconds remaining before the dialog closes.
    /// </param>
    private void SetShutdownLabel(int time)
    {
        lblTimer.Text =
            $"Time until {_shutdownType}: {time}";
    }

    /// <summary>
    /// Decrements the shutdown countdown and closes the dialog when it expires.
    /// </summary>
    /// <param name="sender">
    /// The timer that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void CountdownTimer_Tick(
        object sender,
        EventArgs e)
    {
        _counter--;

        // TODO (UI Modernization):
        // Replace the tick-count-based countdown with an elapsed-time calculation so
        // the displayed seconds remain accurate if the timer interval or UI scheduling
        // changes.
        if (_counter <= 0)
        {
            Close();
            return;
        }

        SetShutdownLabel(_counter);
    }
}