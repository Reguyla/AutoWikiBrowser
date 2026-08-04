namespace Twain.Core.Workspaces.Panes;

/// <summary>
/// Uniquely identifies a pane within a Twain workspace.
/// </summary>
/// <param name="Value">The stable pane identifier.</param>
public readonly record struct PaneId(string Value)
{
    /// <summary>
    /// Returns the pane identifier as text.
    /// </summary>
    public override string ToString()
    {
        return Value;
    }
}