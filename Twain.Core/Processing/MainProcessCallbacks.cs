namespace Twain.Core.Processing;

/// <summary>
/// Contains application-owned callbacks used by the Core article-processing
/// orchestrator.
/// </summary>
/// <remarks>
/// These callbacks provide explicit boundaries for behavior that is still owned
/// by the application layer, such as extension execution, resource preparation,
/// statistics updates, workflow abort handling, and exception handling.
/// </remarks>
public sealed class MainProcessCallbacks
{
    /// <summary>
    /// Gets or initializes the callback that runs configured custom modules,
    /// external programs, and plugins for an article.
    /// </summary>
    public required Func<Article, bool> RunExtensionProcessing { get; init; }

    /// <summary>
    /// Gets or initializes the callback that prepares wiki-backed resources
    /// required by the general-fix processing path.
    /// </summary>
    public required Action<Article, MainProcessOptions>
        PrepareGeneralFixResources
    { get; init; }

    /// <summary>
    /// Gets or initializes the callback that applies regular-expression typo
    /// processing and updates application-owned statistics and UI state.
    /// </summary>
    public required Action<Article, bool, MainProcessOptions>
        ApplyRegexTypoProcessing
    { get; init; }

    /// <summary>
    /// Gets or initializes the callback that aborts the current application
    /// processing workflow.
    /// </summary>
    public required Action AbortProcessing { get; init; }

    /// <summary>
    /// Gets or initializes the callback that handles exceptions raised by the
    /// article-processing pipeline.
    /// </summary>
    public required Action<Article, Exception>
        HandleProcessingException
    { get; init; }
}