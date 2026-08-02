namespace Twain.Core.Workspaces;

/// <summary>
/// Describes the general role of a pane within a workspace.
/// </summary>
public enum PaneKind
{
    /// <summary>
    /// A primary document or editing surface.
    /// </summary>
    Document,

    /// <summary>
    /// A supporting tool used alongside the primary document.
    /// </summary>
    Tool,

    /// <summary>
    /// A navigation surface used to select or locate content.
    /// </summary>
    Navigation,

    /// <summary>
    /// A status or informational surface.
    /// </summary>
    Status
}