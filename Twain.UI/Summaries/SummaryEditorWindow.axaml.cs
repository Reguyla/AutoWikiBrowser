using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Linq;

namespace Twain.UI.Summaries;

/// <summary>
/// Provides an editor for managing the collection of predefined edit summaries.
/// </summary>
public partial class SummaryEditorWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryEditorWindow"/> window.
    /// </summary>
    public SummaryEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the current edit-summary text.
    /// </summary>
    public string SummaryText
    {
        get => SummariesTextBox.Text ?? string.Empty;
        set => SummariesTextBox.Text = value ?? string.Empty;
    }

    /// <summary>
    /// Sorts the non-empty summary entries using the current culture's
    /// default string comparison rules.
    /// </summary>
    private void SortButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string[] lines =
            (SummariesTextBox.Text ?? string.Empty)
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries);

        List<string> summaries =
            new(lines);

        summaries.Sort(
            StringComparer.CurrentCulture);

        SummariesTextBox.Text =
            string.Join(
                Environment.NewLine,
                summaries);
    }

    /// <summary>
    /// Closes the dialog and accepts the edited summaries.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Closes the dialog without accepting changes.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}