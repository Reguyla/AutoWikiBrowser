using CommunityToolkit.Mvvm.ComponentModel;

namespace Twain.UI.Diff;

/// <summary>
/// Provides presentation state for the diff pane.
/// </summary>
/// <remarks>
/// This initial implementation supplies temporary source and updated text.
/// Real diff generation will later be provided by Twain.Core.
/// </remarks>
public sealed partial class DiffViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the original article text.
    /// </summary>
    [ObservableProperty]
    private string _originalText =
        """
        This is the original article text.

        The sample demonstrates the source side of the diff.
        """;

    /// <summary>
    /// Gets or sets the updated article text.
    /// </summary>
    [ObservableProperty]
    private string _updatedText =
        """
        This is the updated article text.

        The sample demonstrates the revised side of the diff.
        """;
}