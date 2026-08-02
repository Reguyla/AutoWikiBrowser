using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using Twain.Core.Editing;
using Twain.Core.Workspaces;
using Twain.Core.Workspaces.Layouts;
using Twain.Core.Workspaces.Panes;
using Twain.UI.ArticleList;
using Twain.UI.Diff;
using Twain.UI.Editor;
using Twain.UI.Options;

namespace Twain.UI.ViewModels.Workspaces;

/// <summary>
/// Provides presentation state and commands for the active workspace.
/// </summary>
public sealed partial class WorkspaceViewModel : ObservableObject
{
    /// <summary>
    /// Initializes the standard Twain editing workspace.
    /// </summary>
    public WorkspaceViewModel()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        ArticleEditingSession editingSession = new(
            """
        This is the original article text.

        Edit this text to produce an updated version.
        """);

        ArticleDocumentViewModel document = new(
            editingSession);

        Panes =
        [
            CreatePane(
                BuiltInPaneDefinitions.ArticleEditor,
                FindState(
                    layout,
                    BuiltInPaneIds.ArticleEditor),
                new ArticleEditorViewModel(document)),

            CreatePane(
                BuiltInPaneDefinitions.ArticleList,
                FindState(
                    layout,
                    BuiltInPaneIds.ArticleList),
                new ArticleListViewModel()),

            CreatePane(
                BuiltInPaneDefinitions.Options,
                FindState(
                    layout,
                    BuiltInPaneIds.Options),
                new OptionsViewModel()),

            CreatePane(
                BuiltInPaneDefinitions.Diff,
                FindState(
                    layout,
                    BuiltInPaneIds.Diff),
                new DiffViewModel())
        ];
    }

    /// <summary>
    /// Gets the panes available in the current workspace.
    /// </summary>
    public ObservableCollection<PaneViewModel> Panes { get; }

    /// <summary>
    /// Gets the panes assigned to the left workspace region.
    /// </summary>
    public IEnumerable<PaneViewModel> LeftPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Left);

    /// <summary>
    /// Gets the panes assigned to the center workspace region.
    /// </summary>
    /// <remarks>
    /// The center region is intended for secondary tools such as job options,
    /// configuration panels, and other supporting functionality that complements
    /// the primary editing experience.
    /// </remarks>
    public IEnumerable<PaneViewModel> CenterPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Center);

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
    /// <remarks>
    /// The bottom region is intended for supporting output such as diffs,
    /// previews, validation results, and other information related to the
    /// active document.
    /// </remarks>
    public IEnumerable<PaneViewModel> BottomPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Bottom);

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

    /// <summary>
    /// Finds the initial state associated with a pane definition.
    /// </summary>
    /// <param name="layout">
    /// The workspace layout containing the pane state.
    /// </param>
    /// <param name="definitionId">
    /// The stable identifier of the pane definition to locate.
    /// </param>
    /// <returns>
    /// The pane state associated with the specified definition.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the layout does not contain exactly one matching pane state.
    /// </exception>
    private static PaneState FindState(
        WorkspaceLayout layout,
        PaneId definitionId)
    {
        return layout.Panes.Single(
            pane => pane.DefinitionId == definitionId);
    }

    /// <summary>
    /// Creates presentation state for a defined workspace pane.
    /// </summary>
    /// <param name="definition">
    /// The pane definition.
    /// </param>
    /// <param name="state">
    /// The initial workspace state of the pane.
    /// </param>
    /// <param name="content">
    /// The view model or temporary content displayed within the pane.
    /// </param>
    /// <returns>
    /// The corresponding pane view model.
    /// </returns>
    private static PaneViewModel CreatePane(
        PaneDefinition definition,
        PaneState state,
        object content)
    {
        return new PaneViewModel(
            definition,
            state,
            content);
    }
}