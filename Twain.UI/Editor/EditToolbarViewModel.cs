
namespace Twain.UI.Editor;

public enum EditToolbarDisplayMode
{
    Icons,
    Text,
    IconsAndText
}

public sealed class EditToolbarViewModel
{
    public bool IsVisible { get; set; } = false;

    public EditToolbarDisplayMode DisplayMode { get; set; }
        = EditToolbarDisplayMode.Text;
}
