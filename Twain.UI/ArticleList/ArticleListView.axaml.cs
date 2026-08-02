using Avalonia.Controls;

namespace Twain.UI.ArticleList;

/// <summary>
/// Displays the article titles available to the active workspace.
/// </summary>
/// <remarks>
/// The initial implementation displays an observable list of sample titles.
/// Future implementations will bind to article-list services supplied by
/// Twain.Core.
/// </remarks>
public partial class ArticleListView : UserControl
{
    /// <summary>
    /// Initializes the article-list view.
    /// </summary>
    public ArticleListView()
    {
        InitializeComponent();
    }
}