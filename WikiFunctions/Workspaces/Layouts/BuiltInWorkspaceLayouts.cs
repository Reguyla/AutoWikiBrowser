using Twain.Core.Workspaces.Panes;

namespace Twain.Core.Workspaces.Layouts;

/// <summary>
/// Creates workspace layouts included with Twain.
/// </summary>
public static class BuiltInWorkspaceLayouts
{
    /// <summary>
    /// Creates the standard article-editing workspace layout.
    /// </summary>
    /// <returns>
    /// A new default editing workspace layout.
    /// </returns>
    public static WorkspaceLayout CreateDefaultEditing()
    {
        return new WorkspaceLayout(
            "Default Editing",
            [
                new PaneState(
                    Guid.NewGuid(),
                    BuiltInPaneIds.ArticleList,
                    true,
                    PanePlacement.Left,
                    "navigation",
                    0),

                new PaneState(
                    Guid.NewGuid(),
                    BuiltInPaneIds.Options,
                    true,
                    PanePlacement.Center,
                    "options",
                    0),

                new PaneState(
                    Guid.NewGuid(),
                    BuiltInPaneIds.ArticleEditor,
                    true,
                    PanePlacement.DocumentArea,
                    "documents",
                    0),

                new PaneState(
                    Guid.NewGuid(),
                    BuiltInPaneIds.Diff,
                    true,
                    PanePlacement.Bottom,
                    "results",
                    0)
            ]);
    }
}