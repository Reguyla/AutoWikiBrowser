namespace Twain.Core.Workspaces;

/// <summary>
/// Represents a named arrangement of panes within a Twain workspace.
/// </summary>
/// <param name="Name">The user-facing layout name.</param>
/// <param name="Panes">The pane states contained in the layout.</param>
public sealed record WorkspaceLayout(
    string Name,
    IReadOnlyList<PaneState> Panes);