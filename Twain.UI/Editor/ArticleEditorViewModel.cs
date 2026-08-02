using CommunityToolkit.Mvvm.ComponentModel;

namespace Twain.UI.ViewModels.Editing;

/// <summary>
/// Provides presentation state for the article editor pane.
/// </summary>
/// <remarks>
/// The editor view model remains independent of the control used to edit the
/// article text. The initial Avalonia text editor can therefore be replaced
/// with Monaco without changing the workspace or pane-host infrastructure.
/// </remarks>
public sealed partial class ArticleEditorViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the article text displayed by the editor.
    /// </summary>
    [ObservableProperty]
    private string _articleText =
        "Article text will appear here.";
}