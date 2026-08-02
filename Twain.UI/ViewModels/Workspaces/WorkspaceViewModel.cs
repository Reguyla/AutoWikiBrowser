using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using Twain.Core.Workspaces;

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
                PanePlacement.DocumentArea,
                "documents",
                0),

            CreatePane(
                "article-list",
                "Article List",
                PaneKind.Navigation,
                PanePlacement.Left,
                "navigation",
                0),

            CreatePane(
                "diff",
                "Diff",
                PaneKind.Tool,
                PanePlacement.Right,
                "tools",
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

    private static PaneViewModel CreatePane(
        string id,
        string title,
        PaneKind kind,
        PanePlacement placement,
        string group,
        int order)
    {
        PaneDefinition definition = new(
            new PaneId(id),
            title,
            kind,
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