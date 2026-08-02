using Avalonia.Controls;

namespace Twain.UI.Controls.Workspaces;

/// <summary>
/// Provides the shared visual frame used to host a workspace pane.
/// </summary>
/// <remarks>
/// The host presents common pane chrome, including the title and close
/// command, while leaving the pane's functional content independent of the
/// workspace layout.
/// </remarks>
public partial class WorkspacePaneHost : UserControl
{
    /// <summary>
    /// Initializes the workspace pane host.
    /// </summary>
    public WorkspacePaneHost()
    {
        InitializeComponent();
    }
}