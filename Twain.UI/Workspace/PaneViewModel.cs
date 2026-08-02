using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Twain.Core.Workspaces.Panes;

namespace Twain.UI.ViewModels.Workspaces;

/// <summary>
/// Provides presentation state and commands for one pane in the active
/// workspace.
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
    /// The initial workspace state of the pane.
    /// </param>
    /// <param name="content">
    /// The content presented within the pane.
    /// </param>
    public PaneViewModel(
        PaneDefinition definition,
        PaneState state,
        object content)
    {
        Definition = definition;
        State = state;
        Content = content;
        _isVisible = state.IsVisible;
    }

    /// <summary>
    /// Gets the pane definition.
    /// </summary>
    public PaneDefinition Definition { get; }

    /// <summary>
    /// Gets the initial persisted state of the pane.
    /// </summary>
    /// <remarks>
    /// The view model currently tracks visibility separately from this
    /// immutable initial state. Workspace persistence will synchronize
    /// presentation changes with a new pane state in a later implementation.
    /// </remarks>
    public PaneState State { get; }

    /// <summary>
    /// Gets the content presented within the pane.
    /// </summary>
    /// <remarks>
    /// The content may initially be text and may later be replaced by a
    /// pane-specific view model rendered through an Avalonia data template.
    /// </remarks>
    public object Content { get; }

    /// <summary>
    /// Gets the stable pane identifier.
    /// </summary>
    public PaneId Id => Definition.Id;

    /// <summary>
    /// Gets the user-facing pane title.
    /// </summary>
    public string Title => Definition.Title;

    /// <summary>
    /// Gets the general role performed by the pane.
    /// </summary>
    public PaneKind Kind => Definition.Kind;

    /// <summary>
    /// Gets a value indicating whether the pane may be closed by the user.
    /// </summary>
    public bool CanClose =>
        Definition.Capabilities.HasFlag(
            PaneCapabilities.Closable);

    /// <summary>
    /// Gets or sets whether the pane is currently visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Hides the pane when the pane definition permits it to be closed.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        if (CanClose)
        {
            IsVisible = false;
        }
    }
}