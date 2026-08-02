namespace Twain.Core.Workspaces.Panes;

/// <summary>
/// Describes the general workspace location of a pane.
/// </summary>
public enum PanePlacement
{
    /// <summary>
    /// The pane belongs in the primary document area.
    /// </summary>
    DocumentArea,

    /// <summary>
    /// The pane belongs on the left side of the workspace.
    /// </summary>
    Left,

    /// <summary>
    /// The pane belongs on the right side of the workspace.
    /// </summary>
    Right,

    /// <summary>
    /// The pane belongs at the bottom of the workspace.
    /// </summary>
    Bottom,

    /// <summary>
    /// The pane is hosted in a separate desktop window.
    /// </summary>
    Floating
}