using Avalonia.Controls;

namespace Twain.UI.FindReplace;

/// <summary>
/// Provides the Avalonia interface for configuring find and replace rules.
/// </summary>
public partial class FindReplaceWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindReplaceWindow"/> class.
    /// </summary>
    public FindReplaceWindow()
        : this(new FindReplaceViewModel())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FindReplaceWindow"/> class
    /// using the supplied view model.
    /// </summary>
    /// <param name="viewModel">
    /// The view model used by the window.
    /// </param>
    public FindReplaceWindow(
        FindReplaceViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        DataContext = viewModel;
    }
}