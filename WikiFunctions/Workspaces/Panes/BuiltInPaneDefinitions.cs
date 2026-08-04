using Twain.Core.Workspaces;

namespace Twain.Core.Workspaces.Panes;

/// <summary>
/// Provides definitions for panes included with Twain.
/// </summary>
public static class BuiltInPaneDefinitions
{
    /// <summary>
    /// Gets the built-in article editor pane definition.
    /// </summary>
    public static PaneDefinition ArticleEditor { get; } = new(
        BuiltInPaneIds.ArticleEditor,
        "Article Editor",
        PaneKind.Document,
        WorkspaceRegion.Document,
        PaneCapabilities.Closable |
        PaneCapabilities.Movable |
        PaneCapabilities.Resizable |
        PaneCapabilities.Floatable,
        true);

    /// <summary>
    /// Gets the built-in article-list pane definition.
    /// </summary>
    public static PaneDefinition ArticleList { get; } = new(
        BuiltInPaneIds.ArticleList,
        "Article List",
        PaneKind.Navigation,
        WorkspaceRegion.Left,
        PaneCapabilities.Closable |
        PaneCapabilities.Movable |
        PaneCapabilities.Resizable |
        PaneCapabilities.Floatable,
        true);

    /// <summary>
    /// Gets the built-in diff pane definition.
    /// </summary>
    public static PaneDefinition Diff { get; } = new(
        BuiltInPaneIds.Diff,
        "Diff",
        PaneKind.Tool,
        WorkspaceRegion.Bottom,
        PaneCapabilities.Closable |
        PaneCapabilities.Movable |
        PaneCapabilities.Resizable |
        PaneCapabilities.Floatable,
        true);

    /// <summary>
    /// Gets the built-in options pane definition.
    /// </summary>
    public static PaneDefinition Options { get; } = new(
        BuiltInPaneIds.Options,
        "Options",
        PaneKind.Tool,
        WorkspaceRegion.Center,
        PaneCapabilities.Closable |
        PaneCapabilities.Movable |
        PaneCapabilities.Resizable |
        PaneCapabilities.Floatable,
        true);
}