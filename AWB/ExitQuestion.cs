namespace AutoWikiBrowser;

/// <summary>
/// Displays a confirmation dialog when the user attempts to exit
/// Twain.
/// </summary>
/// <remarks>
/// The dialog summarizes the current editing session and allows the user to
/// suppress future exit confirmations.
/// </remarks>
internal sealed partial class ExitQuestion : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExitQuestion"/> class.
    /// </summary>
    /// <param name="time">
    /// Total time spent during the current editing session.
    /// </param>
    /// <param name="edits">
    /// Number of edits completed during the current session.
    /// </param>
    /// <param name="msg">
    /// Optional message displayed before the standard exit confirmation
    /// prompt.
    /// </param>
    public ExitQuestion(TimeSpan time, int edits, string msg)
    {
        InitializeComponent();

        lblPrompt.Text = msg + "Are you sure you want to exit?";

        lblTimeAndEdits.Text =
            string.Format(
                "You made {0} edits in {1}",
                edits,
                time);
    }

    /// <summary>
    /// Gets a value indicating whether the user chose not to display this
    /// confirmation dialog again.
    /// </summary>
    public bool CheckBoxDontAskAgain => chkDontAskAgain.Checked;
}