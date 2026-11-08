namespace Twain.Core.Processing;

/// <summary>
/// Describes the outcome of an article-processing operation.
/// </summary>
public enum ArticleProcessingOutcome
{
    ChangesMade,
    NoChanges,
    Skipped,
    Failed
}