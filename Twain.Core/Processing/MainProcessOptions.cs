namespace Twain.Core.Processing;

/// <summary>
/// Represents a snapshot of the processing options used when an article is
/// passed through the main processing pipeline.
/// </summary>
/// <remarks>
/// This type separates processing configuration from the user-interface
/// controls that provide those values. Instances should contain the option
/// values that were active when processing began and should not depend on
/// user-interface components.
/// </remarks>
public sealed class MainProcessOptions
{
    /// <summary>
    /// Gets a value indicating whether {{bots}} and {{nobots}} restrictions
    /// should be ignored.
    /// </summary>
    public bool IgnoreNoBots { get; init; }

    /// <summary>
    /// Gets a value indicating whether find-and-replace processing is enabled.
    /// </summary>
    public bool FindAndReplaceEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when
    /// find-and-replace processing makes no changes.
    /// </summary>
    public bool SkipWhenNoFindAndReplace { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when
    /// find-and-replace processing makes only minor changes.
    /// </summary>
    public bool SkipOnlyMinorFindAndReplace { get; init; }

    /// <summary>
    /// Gets a value indicating whether general fixes are enabled.
    /// </summary>
    public bool GeneralFixesEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether legacy reference-list markup should be
    /// replaced with the supported reference-list form during general fixes.
    /// </summary>
    public bool ReplaceReferenceTags { get; init; }

    /// <summary>
    /// Gets a value indicating whether automatic default-sort changes should
    /// be restricted during general fixes.
    /// </summary>
    public bool RestrictDefaultSortChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether Manual of Style compliance fixes should
    /// be excluded from general fixes.
    /// </summary>
    public bool NoMosComplianceFixes { get; init; }

    /// <summary>
    /// Gets a value indicating whether regular-expression typo fixing is enabled.
    /// </summary>
    public bool RegexTypoFixEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when
    /// regular-expression typo fixing makes no changes.
    /// </summary>
    public bool SkipIfNoRegexTypo { get; init; }

    /// <summary>
    /// Gets a value indicating whether processing is running in bot mode.
    /// </summary>
    public bool BotMode { get; init; }

    /// <summary>
    /// Gets a value indicating whether whole-article Unicode conversion is
    /// enabled.
    /// </summary>
    public bool UnicodifyWholeArticle { get; init; }

    /// <summary>
    /// Gets a value indicating whether automatic article tagging is enabled.
    /// </summary>
    public bool AutoTaggerEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether automatic orphan tagging should be
    /// restricted.
    /// </summary>
    public bool RestrictOrphanTagging { get; init; }

    /// <summary>
    /// Gets a value indicating whether processing is running in pre-parse mode.
    /// </summary>
    public bool PreParseMode { get; init; }

    /// <summary>
    /// Gets a value indicating whether disambiguation processing is enabled.
    /// </summary>
    public bool DisambiguationEnabled { get; init; }

    /// <summary>
    /// Gets the link targeted by the disambiguation operation.
    /// </summary>
    public string DisambiguationLink { get; init; } = string.Empty;

    /// <summary>
    /// Gets the replacement variants available to the disambiguation operation.
    /// </summary>
    public string[] DisambiguationVariants { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Gets the number of surrounding characters used to provide context during
    /// disambiguation.
    /// </summary>
    public int DisambiguationContextCharacters { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when
    /// disambiguation makes no changes.
    /// </summary>
    public bool SkipIfNoDisambiguation { get; init; }

    public int ImageOperation { get; init; }

    public string ImageReplace { get; init; } = string.Empty;

    public string ImageWith { get; init; } = string.Empty;

    public bool SkipIfNoImageChange { get; init; }

    public int CategorisationOperation { get; init; }

    public bool SkipIfNoCategoryChange { get; init; }

    public string NewCategory { get; init; } = string.Empty;

    public string NewCategory2 { get; init; } = string.Empty;

    public bool RemoveCategorySortKey { get; init; }

}