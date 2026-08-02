using Avalonia.Controls;

namespace Twain.UI.Editor;

/// <summary>
/// Displays the editable text of the active article.
/// </summary>
/// <remarks>
/// The initial implementation uses an Avalonia text box. A future
/// implementation will host Monaco while retaining the same pane and
/// view-model boundaries.
/// </remarks>
public partial class ArticleEditorView : UserControl
{
    /// <summary>
    /// Initializes the article editor view.
    /// </summary>
    public ArticleEditorView()
    {
        InitializeComponent();
    }
}