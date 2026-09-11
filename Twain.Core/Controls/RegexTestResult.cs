namespace Twain.Core.Controls;

/// <summary>
/// Contains the result of a regular-expression test operation.
/// </summary>
public sealed class RegexTestResult
{
    /// <summary>
    /// Initializes a new regular-expression test result.
    /// </summary>
    /// <param name="matches">
    /// The matches produced by the regular expression.
    /// </param>
    /// <param name="replacementResult">
    /// The replacement result, or <see langword="null"/> for a find-only
    /// operation.
    /// </param>
    /// <param name="executionTimeMilliseconds">
    /// The elapsed execution time in milliseconds.
    /// </param>
    public RegexTestResult(
        MatchCollection matches,
        string? replacementResult,
        long executionTimeMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(matches);

        Matches = matches;
        ReplacementResult = replacementResult;
        ExecutionTimeMilliseconds = executionTimeMilliseconds;
    }

    /// <summary>
    /// Gets the matches produced by the regular expression.
    /// </summary>
    public MatchCollection Matches { get; }

    /// <summary>
    /// Gets the replacement result, or <see langword="null"/> when the
    /// operation was find-only.
    /// </summary>
    public string? ReplacementResult { get; }

    /// <summary>
    /// Gets the elapsed execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMilliseconds { get; }
}