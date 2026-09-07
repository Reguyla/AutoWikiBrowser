using Avalonia.Interactivity;
using Twain.Core.Lists.Providers;

namespace Twain.UI.Controls;

/// <summary>
/// Allows the user to select a numeric level or contribution count.
/// </summary>
public partial class LevelNumberWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="LevelNumberWindow"/> class for the XAML previewer.
    /// </summary>
    public LevelNumberWindow()
        : this(
            false,
            CategoryRecursiveListProvider.MaxDepth)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="LevelNumberWindow"/> class.
    /// </summary>
    /// <param name="edits">
    /// <see langword="true"/> when selecting a contribution count;
    /// otherwise, when selecting a recursive category depth.
    /// </param>
    /// <param name="max">
    /// The maximum contribution count when <paramref name="edits"/> is
    /// <see langword="true"/>.
    /// </param>
    public LevelNumberWindow(
        bool edits,
        int max)
    {
        InitializeComponent();

        if (edits)
        {
            PromptTextBlock.Text =
                "Number of contribs:";

            LevelNumericUpDown.Minimum =
                1;

            LevelNumericUpDown.Maximum =
                max;
        }
        else
        {
            PromptTextBlock.Text =
                "Number of levels:";

            LevelNumericUpDown.Maximum =
                CategoryRecursiveListProvider.MaxDepth;
        }

        LevelNumericUpDown.Value =
            1;
    }

    /// <summary>
    /// Gets the selected numeric value.
    /// </summary>
    public int Levels =>
        (int)(LevelNumericUpDown.Value ?? 1);

    /// <summary>
    /// Accepts the selected value and closes the dialog.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Closes the dialog without accepting the selected value.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}