using Avalonia.Controls;

namespace Twain.UI.Options;

/// <summary>
/// Displays configurable options for the active editing workspace.
/// </summary>
/// <remarks>
/// The initial implementation presents temporary option controls. Future
/// implementations will bind these controls to persistent settings and
/// processing services supplied by Twain.Core.
/// </remarks>
public partial class OptionsView : UserControl
{
    /// <summary>
    /// Initializes the options view.
    /// </summary>
    public OptionsView()
    {
        InitializeComponent();
    }
}