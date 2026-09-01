namespace Twain.Core.Editing;

/// <summary>
/// Defines the editor operations required by article-editing workflows
/// independently of a specific user-interface editor implementation.
/// </summary>
/// <remarks>
/// Implementations are responsible for adapting editor-specific behavior,
/// such as caret positioning, selection handling, line-ending differences,
/// word wrapping, and undo availability, to the article-editing operations
/// exposed by this interface.
///
/// This abstraction allows article-editing workflows to operate without
/// depending directly on a particular editor control, such as the legacy
/// Windows Forms article editor or a future Monaco-based editor.
/// </remarks>
public interface IArticleEditor
{
    /// <summary>
    /// Gets or sets the complete article text displayed by the editor.
    /// </summary>
    string Text { get; set; }

    /// <summary>
    /// Gets or replaces the text currently selected in the editor.
    /// </summary>
    string SelectedText { get; set; }

    /// <summary>
    /// Gets the current zero-based caret position in editor coordinates.
    /// </summary>
    int CaretPosition { get; }

    /// <summary>
    /// Gets the current caret position expressed in normalized article-text
    /// coordinates.
    /// </summary>
    /// <remarks>
    /// Implementations are responsible for any conversion required between
    /// their native text representation and the normalized article-text
    /// representation used by processing code.
    /// </remarks>
    int ArticleTextCaretPosition { get; }

    /// <summary>
    /// Gets whether the editor currently has a text selection.
    /// </summary>
    bool HasSelection { get; }

    /// <summary>
    /// Gets whether the editor currently has an edit that can be undone.
    /// </summary>
    bool CanUndoEdit { get; }

    /// <summary>
    /// Gets or sets whether article text wraps within the editor.
    /// </summary>
    bool WrapText { get; set; }

    /// <summary>
    /// Attempts to give keyboard focus to the editor.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the editor receives input focus; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    bool Focus();

    /// <summary>
    /// Moves the editor caret to the specified position.
    /// </summary>
    /// <param name="position">
    /// The zero-based position in editor coordinates.
    /// </param>
    /// <param name="scrollToCaret">
    /// <see langword="true"/> to scroll the caret into view; otherwise,
    /// <see langword="false"/>.
    /// </param>
    void SetCaretPosition(
        int position,
        bool scrollToCaret);

    /// <summary>
    /// Selects a range specified in normalized article-text coordinates.
    /// </summary>
    /// <param name="inputIndex">
    /// The zero-based start position in normalized article text.
    /// </param>
    /// <param name="inputLength">
    /// The length of the range in normalized article text.
    /// </param>
    /// <param name="scrollToCaret">
    /// <see langword="true"/> to scroll the resulting selection into view;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// Implementations are responsible for translating article-text
    /// coordinates into their native editor coordinates when necessary.
    /// </remarks>
    void SetArticleTextSelection(
        int inputIndex,
        int inputLength,
        bool scrollToCaret);

    /// <summary>
    /// Clears the current text selection while preserving the caret
    /// position.
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Applies an edit-toolbar operation to the current editor selection.
    /// </summary>
    /// <param name="noSelection">
    /// The text to insert when no text is selected.
    /// </param>
    /// <param name="selectionStartOffset">
    /// The number of characters to move backward from the insertion point
    /// when establishing the resulting selection.
    /// </param>
    /// <param name="selectionLength">
    /// The length of the resulting selection when no text was initially
    /// selected.
    /// </param>
    /// <param name="selectionBefore">
    /// The text to insert before an existing selection.
    /// </param>
    /// <param name="selectionAfter">
    /// The text to insert after an existing selection.
    /// </param>
    void ApplyToolbarEdit(
        string noSelection,
        int selectionStartOffset,
        int selectionLength,
        string selectionBefore,
        string selectionAfter);

    /// <summary>
    /// Refreshes editor layout associated with the current word-wrap
    /// configuration.
    /// </summary>
    /// <remarks>
    /// Implementations may perform no work when their editor does not
    /// require an explicit word-wrap layout refresh.
    /// </remarks>
    void RefreshWordWrapLayout();
}