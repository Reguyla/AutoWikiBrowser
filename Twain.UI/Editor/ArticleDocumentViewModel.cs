using CommunityToolkit.Mvvm.ComponentModel;
using Twain.Core.Editing;

namespace Twain.UI.Editor;

/// <summary>
/// Provides observable presentation state for the active article document.
/// </summary>
/// <remarks>
/// This view model wraps a UI-independent
/// <see cref="ArticleEditingSession"/> and exposes its current text to
/// multiple workspace panes. The editor and diff panes can therefore observe
/// the same article document without directly depending on each other.
/// </remarks>
public sealed partial class ArticleDocumentViewModel : ObservableObject
{
    private readonly ArticleEditingSession _session;

    /// <summary>
    /// Initializes an article document using temporary design-time text.
    /// </summary>
    public ArticleDocumentViewModel()
        : this(
            new ArticleEditingSession(
                "Article text will appear here."))
    {
    }

    /// <summary>
    /// Initializes an article document for the specified editing session.
    /// </summary>
    /// <param name="session">
    /// The editing session represented by this view model.
    /// </param>
    public ArticleDocumentViewModel(
        ArticleEditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _currentText = session.CurrentText;
    }

    /// <summary>
    /// Gets the article text originally loaded into the editing session.
    /// </summary>
    public string OriginalText => _session.OriginalText;

    /// <summary>
    /// Gets or sets the article text currently being edited.
    /// </summary>
    [ObservableProperty]
    private string _currentText;

    /// <summary>
    /// Gets a value indicating whether the current text differs from the
    /// originally loaded article text.
    /// </summary>
    public bool HasChanges =>
        !string.Equals(
            OriginalText,
            CurrentText,
            StringComparison.Ordinal);

    partial void OnCurrentTextChanged(
        string value)
    {
        _session.CurrentText = value;

        OnPropertyChanged(nameof(HasChanges));
    }
}