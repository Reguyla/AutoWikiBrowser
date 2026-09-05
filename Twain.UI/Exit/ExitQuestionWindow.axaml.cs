using System;
using Avalonia.Interactivity;

namespace Twain.UI.Exit;

/// <summary>
/// Displays a confirmation dialog when the user attempts to exit Twain.
/// </summary>
/// <remarks>
/// The dialog summarizes the current editing session and allows the user to
/// suppress future exit confirmations.
/// </remarks>
public partial class ExitQuestionWindow :
    Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ExitQuestionWindow"/> window for the Avalonia designer.
    /// </summary>
    public ExitQuestionWindow()
        : this(
            TimeSpan.Zero,
            0,
            string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ExitQuestionWindow"/> window.
    /// </summary>
    /// <param name="time">
    /// Total time spent during the current editing session.
    /// </param>
    /// <param name="edits">
    /// Number of edits completed during the current session.
    /// </param>
    /// <param name="message">
    /// Optional message displayed before the standard exit confirmation
    /// prompt.
    /// </param>
    public ExitQuestionWindow(
        TimeSpan time,
        int edits,
        string message)
    {
        InitializeComponent();

        PromptText.Text =
            (message ?? string.Empty) +
            "Are you sure you want to exit?";

        TimeAndEditsText.Text =
            string.Format(
                "You made {0} edits in {1}",
                edits,
                time);
    }

    /// <summary>
    /// Gets a value indicating whether the user chose not to display this
    /// confirmation dialog again.
    /// </summary>
    public bool CheckBoxDontAskAgain =>
        DontAskAgainCheckBox.IsChecked == true;

    /// <summary>
    /// Confirms the application exit.
    /// </summary>
    private void ExitButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Cancels the application exit.
    /// </summary>
    private void GoBackButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}