namespace Twain.Core.Workspaces;

/// <summary>
/// Describes the operations supported by a workspace pane.
/// </summary>
[Flags]
public enum PaneCapabilities
{
    /// <summary>
    /// The pane does not support optional workspace operations.
    /// </summary>
    None = 0,

    /// <summary>
    /// The user may close or remove the pane from the workspace.
    /// </summary>
    Closable = 1,

    /// <summary>
    /// The pane may be moved to another location.
    /// </summary>
    Movable = 2,

    /// <summary>
    /// The pane may be resized.
    /// </summary>
    Resizable = 4,

    /// <summary>
    /// The pane may be hosted in a separate desktop window.
    /// </summary>
    Floatable = 8,

    /// <summary>
    /// More than one instance of the pane may exist.
    /// </summary>
    SupportsMultipleInstances = 16
}