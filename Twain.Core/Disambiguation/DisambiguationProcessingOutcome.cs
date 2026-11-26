namespace Twain.Core.Disambiguation;

/// <summary>
/// Represents the outcome of the disambiguation
/// processing stage.
/// </summary>
public enum DisambiguationProcessingOutcome
{
    /// <summary>
    /// Article processing may continue.
    /// </summary>
    Continue,

    /// <summary>
    /// The current article should be skipped.
    /// </summary>
    Skipped,

    /// <summary>
    /// The current processing workflow should stop.
    /// </summary>
    Aborted
}