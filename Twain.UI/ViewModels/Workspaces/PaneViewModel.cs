using CommunityToolkit.Mvvm.ComponentModel;
using Twain.Core.Workspaces;

namespace Twain.UI.ViewModels.Workspaces;

/// <summary>
/// Provides presentation state for one pane in the active workspace.
/// </summary>
public sealed partial class PaneViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a workspace pane view model.
    /// </summary>
    /// <param name="definition">
    /// The pane definition represented by this view model.
    /// </param>
    /// <param name="state">
    /// The current workspace state of the pane.
    /// </param>
    public PaneViewModel(
        PaneDefinition definition,
        PaneState state)
    {
        Definition = definition;
        State = state;
        _isVisible = state.IsVisible;
    }

    /// <summary>
    /// Gets the pane definition.
    /// </summary>
    public PaneDefinition Definition { get; }

    /// <summary>
    /// Gets the persisted pane state.
    /// </summary>
    public PaneState State { get; }

    /// <summary>
    /// Gets the stable pane identifier.
    /// </summary>
    public PaneId Id => Definition.Id;

    /// <summary>
    /// Gets the user-facing pane title.
    /// </summary>
    public string Title => Definition.Title;

    /// <summary>
    /// Gets the general pane role.
    /// </summary>
    public PaneKind Kind => Definition.Kind;

    /// <summary>
    /// Gets or sets whether the pane is currently visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;
}