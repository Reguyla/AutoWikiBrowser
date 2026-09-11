using System.Diagnostics;

namespace Twain.Core.Controls;

/// <summary>
/// Performs the regular-expression operations used by the regex tester.
/// </summary>
public static class RegexTesterProcessor
{
    private static readonly TimeSpan MatchTimeout =
        TimeSpan.FromSeconds(10);

    /// <summary>
    /// Finds all matches for a regular expression in the supplied input.
    /// </summary>
    /// <param name="input">
    /// The text to search.
    /// </param>
    /// <param name="pattern">
    /// The regular-expression pattern.
    /// </param>
    /// <param name="options">
    /// The regular-expression options to apply.
    /// </param>
    /// <returns>
    /// The matches and execution time for the operation.
    /// </returns>
    public static RegexTestResult Find(
        string input,
        string pattern,
        RegexOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(pattern);

        return Execute(
            input,
            pattern,
            null,
            options);
    }

    /// <summary>
    /// Replaces all matches for a regular expression in the supplied input.
    /// </summary>
    /// <param name="input">
    /// The text to process.
    /// </param>
    /// <param name="pattern">
    /// The regular-expression pattern.
    /// </param>
    /// <param name="replacement">
    /// The replacement expression.
    /// </param>
    /// <param name="options">
    /// The regular-expression options to apply.
    /// </param>
    /// <returns>
    /// The matches, replacement text, and execution time for the operation.
    /// </returns>
    public static RegexTestResult Replace(
        string input,
        string pattern,
        string replacement,
        RegexOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(replacement);

        return Execute(
            input,
            pattern,
            replacement,
            options);
    }

    /// <summary>
    /// Executes a find or replace operation using the regex tester's
    /// standard timeout.
    /// </summary>
    private static RegexTestResult Execute(
        string input,
        string pattern,
        string? replacement,
        RegexOptions options)
    {
        Regex regex =
            new(
                pattern,
                options,
                MatchTimeout);

        string normalizedInput =
            input.Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal);

        Stopwatch stopwatch =
            Stopwatch.StartNew();

        MatchCollection matches =
            regex.Matches(
                normalizedInput);

        // MatchCollection evaluates lazily. Accessing Count ensures that
        // execution time includes the complete matching operation.
        _ = matches.Count;

        string? replacementResult = null;

        if (replacement is not null)
        {
            replacementResult =
                regex.Replace(
                    normalizedInput,
                    replacement);
        }

        stopwatch.Stop();

        return new RegexTestResult(
            matches,
            replacementResult,
            stopwatch.ElapsedMilliseconds);
    }
}