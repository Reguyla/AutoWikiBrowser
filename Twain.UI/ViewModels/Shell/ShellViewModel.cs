using Twain.UI.ViewModels.Workspaces;

namespace Twain.UI.ViewModels.Shell;

/// <summary>
/// Provides presentation state for the Twain application shell.
/// </summary>
public sealed class ShellViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the active workspace.
    /// </summary>
    public WorkspaceViewModel Workspace { get; } = new();
}