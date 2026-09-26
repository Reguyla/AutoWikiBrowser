using System.ComponentModel;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.DiffHtml;
using Twain.UI.Editor;

namespace Twain.UI.Diff;

/// <summary>
/// Provides presentation state for the diff pane.
/// </summary>
public sealed class DiffViewModel : ViewModelBase
{
    private readonly ArticleDocumentViewModel _document;

    private readonly WikiDiff _diff = new();

    /// <summary>
    /// Gets or sets the action used to request navigation to a line
    /// in the article editor.
    /// </summary>
    public Func<int, Task>? GoToLineRequested { get; set; }

    /// <summary>
    /// Initializes the diff pane for the supplied article document.
    /// </summary>
    /// <param name="document">
    /// The article document displayed by the editor.
    /// </param>
    public DiffViewModel(
        ArticleDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;

        _document.PropertyChanged +=
            Document_PropertyChanged;
    }

    /// <summary>
    /// Gets the article text as originally loaded.
    /// </summary>
    public string OriginalText =>
        _document.OriginalText;

    /// <summary>
    /// Gets the current edited article text.
    /// </summary>
    public string UpdatedText =>
        _document.CurrentText;

    /// <summary>
    /// Updates the diff presentation when the current article text changes.
    /// </summary>
    private void Document_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
            nameof(ArticleDocumentViewModel.CurrentText))
        {
            return;
        }

        OnPropertyChanged(nameof(UpdatedText));
        OnPropertyChanged(nameof(DiffHtml));
    }

    /// <summary>
    /// Gets the rendered HTML representation of the current article diff.
    /// </summary>
    public string DiffHtml =>
        DiffHtmlBuilder.BuildDiffHtml(
            _diff,
            _document.OriginalText,
            _document.CurrentText,
            0);

    /// <summary>
    /// Requests navigation to the specified zero-based line in the
    /// article editor.
    /// </summary>
    public Task GoToLineAsync(
        int line)
    {
        if (line < 0 ||
            GoToLineRequested is null)
        {
            return Task.CompletedTask;
        }

        return GoToLineRequested(line);
    }

    /// <summary>
    /// Restores a changed line to its original contents.
    /// </summary>
    public void UndoChange(
        int leftLine,
        int rightLine)
    {
        _document.CurrentText =
            _diff.UndoChange(
                leftLine,
                rightLine);
    }

    /// <summary>
    /// Removes a line that was added to the edited article.
    /// </summary>
    public void UndoAddition(
        int rightLine)
    {
        _document.CurrentText =
            _diff.UndoAddition(
                rightLine);
    }

    /// <summary>
    /// Restores a line that was deleted from the edited article.
    /// </summary>
    public void UndoDeletion(
        int leftLine,
        int rightLine)
    {
        _document.CurrentText =
            _diff.UndoDeletion(
                leftLine,
                rightLine);
    }
}