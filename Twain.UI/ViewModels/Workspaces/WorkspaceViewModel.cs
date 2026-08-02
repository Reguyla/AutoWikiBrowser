using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using Twain.Core.Workspaces;
using Twain.Core.Workspaces.Panes;

namespace Twain.UI.ViewModels.Workspaces;

/// <summary>
/// Provides presentation state and commands for the active workspace.
/// </summary>
public sealed partial class WorkspaceViewModel : ObservableObject
{
    /// <summary>
    /// Initializes the initial Twain workspace.
    /// </summary>
    public WorkspaceViewModel()
    {
        Panes =
        [
        CreatePane(
            "editor",
            "Article Editor",
            PaneKind.Document,
            WorkspaceRegion.Document,
            PanePlacement.DocumentArea,
            "documents",
            0),

        CreatePane(
            "article-list",
            "Article List",
            PaneKind.Navigation,
            WorkspaceRegion.Left,
            PanePlacement.Left,
            "navigation",
            0),

        CreatePane(
            "diff",
            "Diff",
            PaneKind.Tool,
            WorkspaceRegion.Bottom,
            PanePlacement.Bottom,
            "results",
            0)
         ];
    }

    /// <summary>
    /// Gets the panes available in the current workspace.
    /// </summary>
    public ObservableCollection<PaneViewModel> Panes { get; }

    /// <summary>
    /// Shows all panes in the current workspace.
    /// </summary>
    [RelayCommand]
    private void ShowAllPanes()
    {
        foreach (PaneViewModel pane in Panes)
        {
            pane.IsVisible = true;
        }
    }

    /// <summary>
    /// Gets the panes assigned to the left workspace region.
    /// </summary>
    public IEnumerable<PaneViewModel> LeftPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Left);

    /// <summary>
    /// Gets the panes assigned to the primary document region.
    /// </summary>
    public IEnumerable<PaneViewModel> DocumentPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Document);

    /// <summary>
    /// Gets the panes assigned to the lower results region.
    /// </summary>
    public IEnumerable<PaneViewModel> BottomPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Bottom);

    /// <summary>
    /// Hides the specified pane.
    /// </summary>
    /// <param name="pane">The pane to hide.</param>
    [RelayCommand]
    private static void HidePane(PaneViewModel? pane)
    {
        if (pane is not null)
        {
            pane.IsVisible = false;
        }
    }

    /// <summary>
    /// Creates a workspace pane using the supplied definition and initial state.
    /// </summary>
    /// <param name="id"> The stable identifier assigned to the pane.</param>
    /// <param name="title"> The user-facing title displayed for the pane.</param>
    /// <param name="kind"> The general role performed by the pane.</param>
    /// <param name="preferredRegion"> The logical workspace region in which the pane should initially appear.</param>
    /// <param name="placement"> The pane's initial placement within the workspace.</param>
    /// <param name="group"> The logical docking or tab group that initially contains the pane.</param>
    /// <param name="order"> The pane's initial ordering within its group.</param>
    /// <returns>
    /// A <see cref="PaneViewModel"/> initialized with the specified pane
    /// definition and initial workspace state.
    /// </returns>
    private static PaneViewModel CreatePane(
        string id,
        string title,
        PaneKind kind,
        WorkspaceRegion preferredRegion,
        PanePlacement placement,
        string group,
        int order)
    {
        PaneDefinition definition = new(
            new PaneId(id),
            title,
            kind,
            preferredRegion,
            PaneCapabilities.Closable |
            PaneCapabilities.Movable |
            PaneCapabilities.Resizable |
            PaneCapabilities.Floatable,
            true);

        PaneState state = new(
            Guid.NewGuid(),
            definition.Id,
            true,
            placement,
            group,
            order);

        return new PaneViewModel(definition, state);
    }
}