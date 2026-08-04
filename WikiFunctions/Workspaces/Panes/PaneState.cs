namespace Twain.Core.Workspaces.Panes;

/// <summary>
/// Represents the persisted workspace state of one pane instance.
/// </summary>
/// <param name="InstanceId">
/// The identifier of this pane instance.
/// </param>
/// <param name="DefinitionId">
/// The identifier of the pane definition represented by this instance.
/// </param>
/// <param name="IsVisible">
/// Whether the pane is currently visible.
/// </param>
/// <param name="Placement">
/// The pane's general workspace placement.
/// </param>
/// <param name="Group">
/// The optional tab or docking group containing the pane.
/// </param>
/// <param name="Order">
/// The pane's relative order within its group.
/// </param>
public sealed record PaneState(
    Guid InstanceId,
    PaneId DefinitionId,
    bool IsVisible,
    PanePlacement Placement,
    string? Group,
    int Order);