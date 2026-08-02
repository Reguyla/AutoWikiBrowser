namespace Twain.Core.Workspaces;

/// <summary>
/// Identifies a logical region within a workspace layout.
/// </summary>
public enum WorkspaceRegion
{
    /// <summary>
    /// The left-side navigation and queue region.
    /// </summary>
    Left,

    /// <summary>
    /// The secondary tools and options region.
    /// </summary>
    Center,

    /// <summary>
    /// The primary document or editing region.
    /// </summary>
    Document,

    /// <summary>
    /// The lower results and preview region.
    /// </summary>
    Bottom,

    /// <summary>
    /// The optional right-side tool region.
    /// </summary>
    Right
}