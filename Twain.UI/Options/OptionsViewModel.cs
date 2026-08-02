using CommunityToolkit.Mvvm.ComponentModel;

namespace Twain.UI.Options;

/// <summary>
/// Provides presentation state for the editing-options pane.
/// </summary>
/// <remarks>
/// This initial implementation establishes the options-pane presentation
/// contract. Persistent settings and article-processing behavior will later
/// be provided by services in Twain.Core.
/// </remarks>
public sealed partial class OptionsViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets whether general text cleanup is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _enableGeneralCleanup = true;

    /// <summary>
    /// Gets or sets whether typo correction is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _enableTypoCorrection;

    /// <summary>
    /// Gets or sets whether unchanged articles should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipUnchangedArticles = true;

    /// <summary>
    /// Gets or sets whether edits should be marked as minor.
    /// </summary>
    [ObservableProperty]
    private bool _markEditsAsMinor;
}