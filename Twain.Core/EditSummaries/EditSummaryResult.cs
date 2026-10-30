using System;
using System.Collections.Generic;

namespace Twain.Core.EditSummaries;

/// <summary>
/// Represents the result of assembling an article's edit summary.
/// </summary>
/// <remarks>
/// Includes the generated summary and information about any
/// compression performed to satisfy the configured length limit.
/// </remarks>
public sealed record EditSummaryResult
{
    /// <summary>
    /// Gets the final assembled edit-summary text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the length of the summary in UTF-8 bytes.
    /// </summary>
    public required int ByteLength { get; init; }

    /// <summary>
    /// Gets the maximum permitted summary length in UTF-8 bytes.
    /// </summary>
    public required int MaximumBytes { get; init; }

    /// <summary>
    /// Gets whether the summary was shortened during assembly.
    /// </summary>
    public bool WasCompressed { get; init; }

    /// <summary>
    /// Gets whether the assembled summary satisfies validation rules.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets contributions omitted because of length restrictions.
    /// </summary>
    public IReadOnlyList<EditSummaryContribution> OmittedContributions
    { get; init; } = Array.Empty<EditSummaryContribution>();

    /// <summary>
    /// Gets contributions whose abbreviated descriptions were used.
    /// </summary>
    public IReadOnlyList<EditSummaryContribution> AbbreviatedContributions
    { get; init; } = Array.Empty<EditSummaryContribution>();
}