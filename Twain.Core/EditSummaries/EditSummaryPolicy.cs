using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Twain.Core.EditSummaries;

/// <summary>
/// Defines the rules used to assemble and shorten edit summaries.
/// </summary>
/// <remarks>
/// Policies are immutable after construction so that summary formatting
/// remains consistent throughout an article-processing operation.
/// </remarks>
public sealed class EditSummaryPolicy
{
    private static readonly IReadOnlyDictionary<
        EditSummaryCategory,
        EditSummaryCategoryPolicy> DefaultCategories =
        new ReadOnlyDictionary<
            EditSummaryCategory,
            EditSummaryCategoryPolicy>(
            new Dictionary<
                EditSummaryCategory,
                EditSummaryCategoryPolicy>
            {
                [EditSummaryCategory.General] =
                    new(10, 100),

                [EditSummaryCategory.GeneralFixes] =
                    new(20, 90),

                [EditSummaryCategory.CustomModule] =
                    new(30, 95),

                [EditSummaryCategory.TypoCorrections] =
                    new(40, 70),

                [EditSummaryCategory.FindReplace] =
                    new(50, 60),

                [EditSummaryCategory.AdvancedFindReplace] =
                    new(60, 60),

                [EditSummaryCategory.Disambiguation] =
                    new(70, 80),

                [EditSummaryCategory.Other] =
                    new(80, 50)
            });

    /// <summary>
    /// Initializes a policy using Twain's default summary conventions.
    /// </summary>
    public EditSummaryPolicy()
    {
        Categories = DefaultCategories;
    }

    /// <summary>
    /// Gets the separator inserted between summary contributions.
    /// </summary>
    public string Separator { get; } = "; ";

    /// <summary>
    /// Gets the maximum supported summary length in UTF-8 bytes.
    /// </summary>
    public int MaximumBytes { get; } = Summary.MaxLength;

    /// <summary>
    /// Gets whether abbreviated descriptions may be used
    /// when the summary exceeds the available length.
    /// </summary>
    public bool AllowAbbreviations { get; } = true;

    /// <summary>
    /// Gets whether duplicate contributions should be excluded.
    /// </summary>
    public bool RemoveDuplicates { get; } = true;

    /// <summary>
    /// Gets the default ordering and retention rules for each category.
    /// </summary>
    public IReadOnlyDictionary<
        EditSummaryCategory,
        EditSummaryCategoryPolicy> Categories
    { get; }

    /// <summary>
    /// Gets the rules for the specified contribution category.
    /// </summary>
    public EditSummaryCategoryPolicy GetCategoryPolicy(
        EditSummaryCategory category)
    {
        return Categories.TryGetValue(category, out var policy)
            ? policy
            : Categories[EditSummaryCategory.Other];
    }
}

/// <summary>
/// Defines the ordering and retention priority of a summary category.
/// </summary>
/// <param name="DisplayOrder">
/// Lower values appear earlier in the summary.
/// </param>
/// <param name="RetentionPriority">
/// Higher values are retained preferentially when space is limited.
/// </param>
public sealed record EditSummaryCategoryPolicy(
    int DisplayOrder,
    int RetentionPriority);