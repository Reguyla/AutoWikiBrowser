using NUnit.Framework;
using Twain.Core.Workspaces;
using Twain.Core.Workspaces.Layouts;
using Twain.Core.Workspaces.Panes;

namespace Twain.Tests.Workspaces;

/// <summary>
/// Verifies the built-in Twain workspace layouts and pane definitions.
/// </summary>
[TestFixture]
public sealed class BuiltInWorkspaceLayoutsTests
{
    /// <summary>
    /// Verifies that the default editing workspace contains all expected
    /// built-in panes.
    /// </summary>
    [Test]
    public void CreateDefaultEditing_ContainsExpectedPaneDefinitions()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        PaneId[] paneIds =
        [
            .. layout.Panes.Select(
                pane => pane.DefinitionId)
        ];

        Assert.Multiple(() =>
        {
            Assert.That(
                paneIds,
                Does.Contain(BuiltInPaneIds.ArticleList));

            Assert.That(
                paneIds,
                Does.Contain(BuiltInPaneIds.Options));

            Assert.That(
                paneIds,
                Does.Contain(BuiltInPaneIds.ArticleEditor));

            Assert.That(
                paneIds,
                Does.Contain(BuiltInPaneIds.Diff));
        });
    }

    /// <summary>
    /// Verifies that the default editing workspace creates one state for
    /// each expected pane.
    /// </summary>
    [Test]
    public void CreateDefaultEditing_ContainsFourPaneStates()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        Assert.That(
            layout.Panes,
            Has.Count.EqualTo(4));
    }

    /// <summary>
    /// Verifies that every pane state in the default editing workspace has
    /// a unique instance identifier.
    /// </summary>
    [Test]
    public void CreateDefaultEditing_AssignsUniquePaneInstanceIds()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        Guid[] instanceIds =
        [
            .. layout.Panes.Select(
            pane => pane.InstanceId)
        ];

        Assert.That(
            instanceIds,
            Is.Unique);
    }

    /// <summary>
    /// Verifies that the article editor defaults to the primary document
    /// area.
    /// </summary>
    [Test]
    public void CreateDefaultEditing_PlacesArticleEditorInDocumentArea()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        PaneState editorState = FindPaneState(
            layout,
            BuiltInPaneIds.ArticleEditor);

        Assert.That(
            editorState.Placement,
            Is.EqualTo(PanePlacement.DocumentArea));
    }

    /// <summary>
    /// Verifies that the diff pane defaults to the bottom results area.
    /// </summary>
    [Test]
    public void CreateDefaultEditing_PlacesDiffInBottomArea()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        PaneState diffState = FindPaneState(
            layout,
            BuiltInPaneIds.Diff);

        Assert.That(
            diffState.Placement,
            Is.EqualTo(PanePlacement.Bottom));
    }

    /// <summary>
    /// Verifies that the article-list pane defaults to the left navigation
    /// area.
    /// </summary>
    [Test]
    public void CreateDefaultEditing_PlacesArticleListOnLeft()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        PaneState articleListState = FindPaneState(
            layout,
            BuiltInPaneIds.ArticleList);

        Assert.That(
            articleListState.Placement,
            Is.EqualTo(PanePlacement.Left));
    }

    /// <summary>
    /// Verifies that the options pane defaults to the center tools area.
    /// </summary>
    [Test]
    public void CreateDefaultEditing_PlacesOptionsInCenterArea()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        PaneState optionsState = FindPaneState(
            layout,
            BuiltInPaneIds.Options);

        Assert.That(
            optionsState.Placement,
            Is.EqualTo(PanePlacement.Center));
    }

    /// <summary>
    /// Verifies that the built-in pane identifiers remain stable.
    /// </summary>
    [Test]
    public void BuiltInPaneIds_HaveExpectedStableValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                BuiltInPaneIds.ArticleEditor.Value,
                Is.EqualTo("article-editor"));

            Assert.That(
                BuiltInPaneIds.ArticleList.Value,
                Is.EqualTo("article-list"));

            Assert.That(
                BuiltInPaneIds.Diff.Value,
                Is.EqualTo("diff"));

            Assert.That(
                BuiltInPaneIds.Options.Value,
                Is.EqualTo("options"));
        });
    }

    /// <summary>
    /// Verifies that the default editing workspace uses the expected
    /// display name.
    /// </summary>
    [Test]
    public void CreateDefaultEditing_UsesExpectedWorkspaceName()
    {
        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        Assert.That(
            layout.Name,
            Is.EqualTo("Default Editing"));
    }

    /// <summary>
    /// Finds the state associated with a built-in pane definition.
    /// </summary>
    /// <param name="layout">
    /// The workspace layout to inspect.
    /// </param>
    /// <param name="definitionId">
    /// The stable pane definition identifier.
    /// </param>
    /// <returns>
    /// The matching pane state.
    /// </returns>
    private static PaneState FindPaneState(
        WorkspaceLayout layout,
        PaneId definitionId)
    {
        return layout.Panes.Single(
            pane => pane.DefinitionId == definitionId);
    }
}