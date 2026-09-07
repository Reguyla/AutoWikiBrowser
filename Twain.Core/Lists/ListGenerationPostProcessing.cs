namespace Twain.Core.Lists;

/// <summary>
/// Describes the post-processing operations to apply after list generation.
/// </summary>
public sealed class ListGenerationPostProcessing
{
    /// <summary>
    /// Gets or sets a value indicating whether non-mainspace articles
    /// should be removed.
    /// </summary>
    public bool FilterNonMainArticles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether duplicate articles
    /// should be removed.
    /// </summary>
    public bool RemoveDuplicates { get; set; }
}