using Twain.Core.Workspaces;
using Twain.Core.Workspaces.Panes;

namespace Twain.Tests.Workspaces;

[TestFixture]
public sealed class WorkspaceLayoutTests
{
    [Test]
    public void PaneId_ToString_ReturnsStoredValue()
    {
        PaneId paneId = new("editor");

        Assert.That(paneId.ToString(), Is.EqualTo("editor"));
    }

    [Test]
    public void WorkspaceLayout_CanContainMultipleInstancesOfSamePaneDefinition()
    {
        PaneId editorPaneId = new("editor");

        PaneState firstEditor = new(
            Guid.NewGuid(),
            editorPaneId,
            true,
            PanePlacement.DocumentArea,
            "documents",
            0);

        PaneState secondEditor = new(
            Guid.NewGuid(),
            editorPaneId,
            true,
            PanePlacement.Floating,
            null,
            0);

        WorkspaceLayout layout = new(
            "Editing",
            [firstEditor, secondEditor]);

        Assert.That(layout.Panes, Has.Count.EqualTo(2));
        Assert.That(
            layout.Panes.Select(pane => pane.DefinitionId),
            Has.All.EqualTo(editorPaneId));
    }
}