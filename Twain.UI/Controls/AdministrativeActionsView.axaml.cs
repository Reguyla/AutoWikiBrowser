using Avalonia.Controls;

namespace Twain.UI.Controls.Processing;

/// <summary>
/// Displays administrative actions available for the current wiki.
/// </summary>
/// <remarks>
/// Administrative capabilities will eventually be determined by
/// the permissions granted to the authenticated wiki user.
/// </remarks>
public partial class AdministrativeActionsView : UserControl
{
    public AdministrativeActionsView()
    {
        InitializeComponent();
    }
}