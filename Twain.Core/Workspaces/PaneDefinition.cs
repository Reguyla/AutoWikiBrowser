namespace Twain.Core.Workspaces;

/// <summary>
/// Defines a pane that can participate in a Twain workspace.
/// </summary>
/// <param name="Id">The stable pane identifier.</param>
/// <param name="Title">The user-facing pane title.</param>
/// <param name="Kind">The general role of the pane.</param>
/// <param name="Capabilities">The operations supported by the pane.</param>
/// <param name="IsVisibleByDefault">
/// Whether the pane is visible in a newly created workspace.
/// </param>
public sealed record PaneDefinition(
    PaneId Id,
    string Title,
    PaneKind Kind,
    PaneCapabilities Capabilities,
    bool IsVisibleByDefault);