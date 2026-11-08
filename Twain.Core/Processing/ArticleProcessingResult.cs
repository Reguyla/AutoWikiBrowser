namespace Twain.Core.Processing;

/// <summary>
/// Describes the result of processing an article.
/// </summary>
public sealed record ArticleProcessingResult
{
    /// <summary>
    /// The overall processing outcome.
    /// </summary>
    public required ArticleProcessingOutcome Outcome { get; init; }

    /// <summary>
    /// Whether the article text differs from its original text.
    /// </summary>
    public bool HasTextChanges { get; init; }

    /// <summary>
    /// Whether an existing processing rule requested a skip.
    /// </summary>
    public bool WasSkipped { get; init; }

    /// <summary>
    /// Whether the processing pipeline reached its normal completion.
    /// </summary>
    public bool Completed { get; init; }

    /// <summary>
    /// The reason for skipping, when available.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// The exception associated with a processing failure, when available.
    /// </summary>
    public Exception? Error { get; init; }
}