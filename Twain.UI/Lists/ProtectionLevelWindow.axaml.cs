using Avalonia.Interactivity;

namespace Twain.UI.Controls.Lists;

/// <summary>
/// Allows the user to select a protection type and protection level.
/// </summary>
public partial class ProtectionLevelWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ProtectionLevelWindow"/> class.
    /// </summary>
    public ProtectionLevelWindow()
    {
        InitializeComponent();

        TypeListBox.SelectedIndex = 0;
        LevelListBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Gets the selected protection type.
    /// </summary>
    public string Type =>
        TypeListBox.SelectedIndex switch
        {
            0 => "edit",
            1 => "move",
            2 => "edit|move",
            _ => string.Empty
        };

    /// <summary>
    /// Gets the selected protection level.
    /// </summary>
    public string Level =>
        LevelListBox.SelectedIndex switch
        {
            0 => "autoconfirmed",
            1 => "sysop",
            2 => "autoconfirmed|sysop",
            _ => string.Empty
        };

    /// <summary>
    /// Accepts the selected protection settings and closes the dialog.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Closes the dialog without accepting the selected values.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}