namespace Twain.UI.Editor;

/// <summary>
/// Provides presentation state for the article editor pane.
/// </summary>
/// <remarks>
/// The editor view model remains independent of the control used to edit the
/// article text. The initial Avalonia text box can therefore be replaced with
/// Monaco without changing the workspace or pane-host infrastructure.
/// </remarks>
public sealed class ArticleEditorViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes the article editor with a temporary standalone document.
    /// </summary>
    /// <remarks>
    /// This constructor supports design-time previewing and conventional view
    /// creation. The active workspace supplies a shared document through the
    /// other constructor at runtime.
    /// </remarks>
    public ArticleEditorViewModel()
        : this(
            new ArticleDocumentViewModel())
    {
    }

    /// <summary>
    /// Initializes the article editor for the specified document.
    /// </summary>
    /// <param name="document">
    /// The article document edited by this pane.
    /// </param>
    public ArticleEditorViewModel(
        ArticleDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Document = document;
    }

    /// <summary>
    /// Gets the article document edited by this pane.
    /// </summary>
    public ArticleDocumentViewModel Document { get; }
}