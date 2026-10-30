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
    /// Gets or sets the action invoked by the pane's optional header button.
    /// </summary>
    public Action? HeaderAction { get; set; }

    /// <summary>
    /// Gets or sets the tooltip displayed for the pane's header action.
    /// </summary>
    public string HeaderActionToolTip { get; set; } = "Editing settings";

    /// <summary>
    /// Gets or sets the icon displayed for the pane's header action.
    /// </summary>
    public PaneHeaderActionIcon HeaderActionIcon { get; set; } =
        PaneHeaderActionIcon.Settings;

    /// <summary>
    /// Gets whether the pane header should display a settings icon.
    /// </summary>
    public bool ShowSettingsHeaderIcon =>
        HeaderActionIcon == PaneHeaderActionIcon.Settings;

    /// <summary>
    /// Gets whether the pane header should display a filter icon.
    /// </summary>
    public bool ShowFilterHeaderIcon =>
        HeaderActionIcon == PaneHeaderActionIcon.Filter;

    /// <summary>
    /// Gets whether the pane has an available header action.
    /// </summary>
    public bool HasHeaderAction => HeaderAction is not null;

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

    /// <summary>
    /// Executes the optional pane header action.
    /// </summary>
    [RelayCommand]
    private void ExecuteHeaderAction()
    {
        HeaderAction?.Invoke();
    }
}

    /// <summary>
    /// Identifies the icon displayed by a workspace pane header action.
    /// </summary>
    public enum PaneHeaderActionIcon
{
    /// <summary>
    /// Displays a settings icon.
    /// </summary>
    Settings,

    /// <summary>
    /// Displays a filtering icon.
    /// </summary>
    Filter
}