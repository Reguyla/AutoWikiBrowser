using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            control.Changed += DabControl_Changed;

            DisambiguationItemsPanel.Children.Add(control);
            _items.Add(control);
        }

        UpdateDoneButtonState();
    }

    /// <summary>
    /// Gets the disambiguation results currently represented by the
    /// controls in this window.
    /// </summary>
    public IReadOnlyList<DisambiguationProcessor.DisambiguationResult> Results =>
        _items
            .Select(
                item => new DisambiguationProcessor.DisambiguationResult(
                    item.NoChange,
                    item.Result))
            .ToList();

    /// <summary>
    /// Handles changes reported by an individual disambiguation control.
    /// </summary>
    private void DabControl_Changed(
        object? sender,
        EventArgs e)
        {
            UpdateDoneButtonState();
        }

    /// <summary>
    /// Updates whether the dialog can be completed based on the state
    /// of all disambiguation controls.
    /// </summary>
    private void UpdateDoneButtonState()
    {
        DoneButton.IsEnabled =
            _items.Count > 0 &&
            _items.All(item => item.CanSave);
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
    /// Restores the generated replacement for the current selection in every
    /// disambiguation control, discarding manual correction edits.
    /// </summary>
    private void UndoAllButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        foreach (DabControl item in _items)
        {
            item.Undo();
        }

        UndoAllRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Restores every disambiguation control to its default no-change state.
    /// </summary>
    private void ResetAllButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        foreach (DabControl item in _items)
        {
            item.Reset();
        }

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
    /// changes should be applied when all entries are saveable.
    /// </summary>
    private void DoneButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_items.Any(item => !item.CanSave))
            return;

        Close(DabDialogResult.Done);
    }

    /// <summary>
    /// Shows the disambiguation dialog and applies the selected results when
    /// the user completes the dialog.
    /// </summary>
    /// <param name="owner">
    /// The window that owns the disambiguation dialog.
    /// </param>
    /// <param name="articleText">
    /// The original article text.
    /// </param>
    /// <param name="preparation">
    /// The prepared disambiguation data for the article.
    /// </param>
    /// <returns>
    /// The dialog result together with the resulting article text.
    /// When the dialog is cancelled or aborted, the original article text
    /// is returned unchanged.
    /// </returns>
    public static async Task<(DabDialogResult DialogResult, string ArticleText)> ShowAsync(
        Window owner,
        string articleText,
        DisambiguationProcessor.DisambiguationPreparation preparation)
    {
        DabWindow window = new(preparation);

        DabDialogResult result =
            await window.ShowDialog<DabDialogResult>(owner);

        if (result != DabDialogResult.Done)
        {
            return (result, articleText);
        }

        string newText =
            DisambiguationProcessor.ApplyResults(
                articleText,
                preparation.Search,
                window.Results);

        return (result, newText);
    }
}