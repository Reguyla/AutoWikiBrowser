using System.ComponentModel;
using Twain.UI.Editor;

namespace Twain.UI.Diff;

/// <summary>
/// Provides presentation state for the diff pane.
/// </summary>
public sealed class DiffViewModel : ViewModelBase
{
    private readonly ArticleDocumentViewModel _document;

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
    }
}