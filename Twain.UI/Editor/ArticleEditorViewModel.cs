using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using Twain.Core.AWBSettings;

namespace Twain.UI.Editor;

/// <summary>
/// Provides presentation state for the article editor pane.
/// </summary>
/// <remarks>
/// The editor view model remains independent of the control used to edit the
/// article text. The initial Avalonia text box can therefore be replaced with
/// Monaco without changing the workspace or pane-host infrastructure.
/// </remarks>
public sealed partial class ArticleEditorViewModel : ViewModelBase
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
    /// Gets or sets the calculated edit summary displayed above the article editor.
    /// </summary>
    [ObservableProperty]
    private string _calculatedEditSummary = string.Empty;

    /// <summary>
    /// Initializes the article editor for the specified document.
    /// </summary>
    /// <param name="document">
    /// The article document edited by this pane.
    /// </param>
    public ArticleEditorViewModel(
        ArticleDocumentViewModel document,
        UserPrefs? preferences = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        Document = document;
        Preferences = preferences;
    }

    /// <summary>
    /// Gets the article document edited by this pane.
    /// </summary>
    public ArticleDocumentViewModel Document { get; }

    /// <summary>
    /// Requests that the active Monaco editor refresh its appearance.
    /// </summary>
    public Func<Task>? ApplyAppearanceRequested { get; set; }

    /// <summary>
    /// Gets the application preferences used by the editor.
    /// </summary>
    public UserPrefs? Preferences { get; }

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