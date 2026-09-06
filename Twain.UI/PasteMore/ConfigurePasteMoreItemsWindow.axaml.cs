using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Twain.Core.Editing;

namespace Twain.UI.PasteMore;

public partial class ConfigurePasteMoreItemsWindow : Avalonia.Controls.Window
{
    public ConfigurePasteMoreItemsWindow()
        : this([])
    {
    }

    public ConfigurePasteMoreItemsWindow(
        IEnumerable<string> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Rows =
            new ObservableCollection<PasteMoreItemRow>(
                items.Select(
                    (text, index) =>
                        new PasteMoreItemRow(
                            index + 1,
                            text ?? string.Empty)));

        InitializeComponent();

        DataContext = this;
    }

    /// <summary>
    /// Gets the editable Paste More rows displayed by the window.
    /// </summary>
    public ObservableCollection<PasteMoreItemRow> Rows { get; }

    /// <summary>
    /// Gets the configured Paste More text values.
    /// </summary>
    public IReadOnlyList<string> Items =>
        Rows
            .Select(row => row.Text)
            .ToArray();

    /// <summary>
    /// Accepts the configured Paste More values and closes the dialog.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void OkButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Cancels the Paste More configuration and closes the dialog.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void CancelButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    /// <summary>
    /// Initializes the Paste More configuration window from an existing
    /// Paste More configuration.
    /// </summary>
    /// <param name="configuration">
    /// The Paste More configuration whose current items are displayed for editing.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public ConfigurePasteMoreItemsWindow(
        PasteMoreConfiguration configuration)
        : this(configuration?.Items
            ?? throw new ArgumentNullException(nameof(configuration)))
    {
    }
}

/// <summary>
/// Represents one editable Paste More entry.
/// </summary>
public sealed class PasteMoreItemRow
{
    public PasteMoreItemRow(
        int number,
        string text)
    {
        Number = number;
        Text = text;
    }

    /// <summary>
    /// Gets the one-based display number of the entry.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// Gets or sets the text associated with the entry.
    /// </summary>
    public string Text { get; set; }
}