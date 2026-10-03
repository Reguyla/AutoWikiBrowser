using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

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

    /// <summary>
    /// Gets or sets the action used to request navigation to the next
    /// search match in the article editor.
    /// </summary>
    public Func<
        string,
        bool,
        bool,
        string,
        Task>? FindNextRequested { get; set; }

    /// <summary>
    /// Gets or sets the action used to navigate the article editor
    /// to a zero-based line number.
    /// </summary>
    public Func<int, Task>? GoToLineRequested { get; set; }

    private string _editSummary = string.Empty;

    /// <summary>
    /// Gets or sets the edit summary used when saving the current article.
    /// </summary>
    public string EditSummary
    {
        get => _editSummary;

        set
        {
            if (_editSummary == value)
            {
                return;
            }

            _editSummary = value;
            OnPropertyChanged();
        }
    }
    private bool _isMinorEdit;

    /// <summary>
    /// Gets or sets whether the current article should be saved as a minor edit.
    /// </summary>
    public bool IsMinorEdit
    {
        get => _isMinorEdit;

        set
        {
            if (_isMinorEdit == value)
            {
                return;
            }

            _isMinorEdit = value;
            OnPropertyChanged();
        }
    }
}