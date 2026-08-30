using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using Twain.Core.Disambiguation;

namespace Twain.UI.Disambiguation;

/// <summary>
/// Displays the interactive disambiguation editor for the prepared
/// occurrences in an article.
/// </summary>
public partial class DabWindow : Window
{
    /// <summary>
    /// Contains the disambiguation controls created for the current article.
    /// </summary>
    private readonly List<DabControl> _items = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DabWindow"/> class.
    /// </summary>
    public DabWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DabWindow"/> class
    /// using prepared disambiguation data.
    /// </summary>
    /// <param name="preparation">
    /// The prepared disambiguation data for the current article.
    /// </param>
    public DabWindow(
        DisambiguationProcessor.DisambiguationPreparation preparation)
        : this()
    {
        PopulateItems(preparation);
    }

    /// <summary>
    /// Gets the panel containing the dynamically generated
    /// disambiguation controls.
    /// </summary>
    internal StackPanel ItemsPanel => DisambiguationItemsPanel;

    /// <summary>
    /// Raised when the user requests the current page.
    /// </summary>
    public event EventHandler? PageRequested;

    /// <summary>
    /// Raised when the user requests that all manual changes be undone.
    /// </summary>
    public event EventHandler? UndoAllRequested;

    /// <summary>
    /// Raised when the user requests that all disambiguation selections
    /// be reset.
    /// </summary>
    public event EventHandler? ResetAllRequested;

    /// <summary>
    /// Creates a disambiguation control for each prepared occurrence
    /// and adds it to the window.
    /// </summary>
    /// <param name="preparation">
    /// The prepared disambiguation data for the current article.
    /// </param>
    private void PopulateItems(
        DisambiguationProcessor.DisambiguationPreparation preparation)
    {
        foreach (DisambiguationProcessor.DisambiguationItemPreparation item
                 in preparation.Items)
        {
            DabControl control =
                new DabControl(
                    item,
                    preparation.Variants);

            DisambiguationItemsPanel.Children.Add(control);
            _items.Add(control);
        }
    }

    /// <summary>
    /// Handles a request to open or display the current page.
    /// </summary>
    private void PageButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        PageRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles a request to undo changes for all disambiguation occurrences.
    /// </summary>
    private void UndoAllButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        UndoAllRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles a request to reset all disambiguation occurrences.
    /// </summary>
    private void ResetAllButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ResetAllRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Closes the dialog and indicates that article processing should abort.
    /// </summary>
    private void AbortButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(DabDialogResult.Abort);
    }

    /// <summary>
    /// Closes the dialog without applying the disambiguation changes.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(DabDialogResult.Cancel);
    }

    /// <summary>
    /// Closes the dialog and indicates that the selected disambiguation
    /// changes should be applied.
    /// </summary>
    private void DoneButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(DabDialogResult.Done);
    }
}