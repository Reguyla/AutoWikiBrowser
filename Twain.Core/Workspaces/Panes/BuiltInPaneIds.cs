namespace Twain.Core.Workspaces.Panes;

/// <summary>
/// Provides stable identifiers for panes included with Twain.
/// </summary>
public static class BuiltInPaneIds
{
    /// <summary>
    /// Identifies the article editor pane.
    /// </summary>
    public static PaneId ArticleEditor { get; } = new("article-editor");

    /// <summary>
    /// Identifies the article-list pane.
    /// </summary>
    public static PaneId ArticleList { get; } = new("article-list");

    /// <summary>
    /// Identifies the diff pane.
    /// </summary>
    public static PaneId Diff { get; } = new("diff");

    /// <summary>
    /// Identifies the options pane.
    /// </summary>
    public static PaneId Options { get; } = new("options");
}