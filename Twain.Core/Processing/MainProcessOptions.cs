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

    /// <summary>
    /// Gets the image or file replacement operation to apply during processing.
    /// </summary>
    public int ImageOperation { get; init; }

    /// <summary>
    /// Gets the image or file name to replace, remove, or comment out.
    /// </summary>
    public string ImageReplace { get; init; } = string.Empty;

    /// <summary>
    /// Gets the replacement image or comment text used by the configured image
    /// operation.
    /// </summary>
    public string ImageWith { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the article should be skipped when the configured image
    /// operation makes no changes.
    /// </summary>
    public bool SkipIfNoImageChange { get; init; }

    /// <summary>
    /// Gets the category operation to apply during processing.
    /// </summary>
    public int CategorisationOperation { get; init; }

    /// <summary>
    /// Gets whether the article should be skipped when the configured category
    /// operation makes no changes.
    /// </summary>
    public bool SkipIfNoCategoryChange { get; init; }

    /// <summary>
    /// Gets the primary category used by the configured category operation.
    /// </summary>
    public string NewCategory { get; init; } = string.Empty;

    /// <summary>
    /// Gets the secondary category used by category operations that require an
    /// additional category value.
    /// </summary>
    public string NewCategory2 { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether an existing category sort key should be removed when the
    /// configured category operation is applied.
    /// </summary>
    public bool RemoveCategorySortKey { get; init; }

    /// <summary>
    /// Gets whether configured text should be appended or prepended to the
    /// article during processing.
    /// </summary>
    public bool AppendEnabled { get; init; }

    /// <summary>
    /// Gets the text to append or prepend to the article.
    /// </summary>
    public string AppendText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of newline characters inserted between the existing
    /// article text and the appended or prepended text.
    /// </summary>
    public int AppendNewLineCount { get; init; }

    /// <summary>
    /// Gets whether the configured text is appended to the article rather than
    /// prepended.
    /// </summary>
    public bool AppendInsteadOfPrepend { get; init; }

    /// <summary>
    /// Gets whether article metadata should be sorted after the append or
    /// prepend operation is applied.
    /// </summary>
    public bool SortMetadataAfterAppend { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when
    /// processing makes no changes.
    /// </summary>
    public bool SkipNoChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when only
    /// whitespace changes were made.
    /// </summary>
    public bool SkipWhitespaceChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when only
    /// casing changes were made.
    /// </summary>
    public bool SkipCasingChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when only
    /// minor general-fix changes were made.
    /// </summary>
    public bool SkipMinorGeneralFixChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when only
    /// general-fix changes were made.
    /// </summary>
    public bool SkipGeneralFixChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when it
    /// contains no wiki links.
    /// </summary>
    public bool SkipPagesWithNoLinks { get; init; }

    /// <summary>
    /// Gets a value indicating whether an article should be skipped when only
    /// cosmetic changes were made.
    /// </summary>
    public bool SkipCosmeticChanges { get; init; }
}