namespace Twain.Core;

/// <summary>
/// Identifies the processing operation that contributed to an edit summary.
/// </summary>
public enum EditSummaryCategory
{
    General,
    GeneralFixes,
    MetadataCleanup,
    CategoryChanges,
    CustomModule,
    TypoCorrections,
    FindReplace,
    AdvancedFindReplace,
    Disambiguation,
    Other
}

/// <summary>
/// Describes an individual contribution to an article's edit summary.
/// </summary>
/// <remarks>
/// Contributions are collected independently of processing order.
/// The edit-summary builder determines their final presentation.
/// </remarks>
public sealed record EditSummaryContribution
{
    /// <summary>
    /// Gets the category responsible for the contribution.
    /// </summary>
    public required EditSummaryCategory Category { get; init; }

    /// <summary>
    /// Gets the complete description of the changes.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets an optional abbreviated description used when
    /// the edit summary approaches its length limit.
    /// </summary>
    public string? ShortDescription { get; init; }

}