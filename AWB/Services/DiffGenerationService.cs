using Twain.Core;

namespace AutoWikiBrowser.Services.Diff;

/// <summary>
/// Generates complete HTML documents for displaying the differences between
/// an article's original text and its current edited text.
/// </summary>
/// <remarks>
/// The service delegates the actual text comparison and diff markup generation
/// to <see cref="WikiDiff"/> and wraps the result in the HTML required by the
/// AWB diff viewer.
/// </remarks>
internal sealed class DiffGenerationService
{
    /// <summary>
    /// Number of unchanged context lines displayed around each difference.
    /// </summary>
    private const int DiffContextLines = 2;

    /// <summary>
    /// Edit count below which the standard diff table header, including its
    /// informational messages, is displayed.
    /// </summary>
    private const int MessageDisplayEditThreshold = 10;

    private readonly WikiDiff _diff = new();

    // TODO (validation): Confirm whether a negative numberOfEdits value can occur
    // during normal operation. If it always indicates a caller error, add
    // ArgumentOutOfRangeException.ThrowIfNegative(numberOfEdits) at the start of
    // Generate(). Avoid adding the guard until this is verified because the current
    // behavior accepts negative values and displays the standard diff table header.
    /// <summary>
    /// Generates a complete HTML document describing the differences between
    /// the original and current article text.
    /// </summary>
    /// <param name="originalText">
    /// The article text before the current editing operation.
    /// </param>
    /// <param name="currentText">
    /// The current article text after processing or manual editing.
    /// </param>
    /// <param name="numberOfEdits">
    /// The number of edits processed during the current run. This determines
    /// whether informational messages are included in the diff table header.
    /// </param>
    /// <returns>
    /// A complete HTML document containing either the generated diff or a
    /// message indicating that no changes were made.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="originalText"/> or
    /// <paramref name="currentText"/> is <see langword="null"/>.
    /// </exception>
    internal string Generate(
        string originalText,
        string currentText,
        int numberOfEdits)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(currentText);

        if (string.Equals(
                originalText,
                currentText,
                StringComparison.Ordinal))
        {
            return BuildNoChangesHtml();
        }

        return BuildDiffHtml(
            originalText,
            currentText,
            numberOfEdits);
    }

    /// <summary>
    /// Builds the HTML displayed when the article text has not changed.
    /// </summary>
    private static string BuildNoChangesHtml()
    {
        return """
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <title>AWB Diff</title>
            </head>
            <body>
            <h2 style="padding-top: .5em;
            padding-bottom: .17em;
            border-bottom: 1px solid #aaa;
            font-size: 150%;">No changes</h2>
            <p>Press the "Skip" button below to skip to the next page.</p>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Builds a complete HTML document containing the generated diff table.
    /// </summary>
    private string BuildDiffHtml(
        string originalText,
        string currentText,
        int numberOfEdits)
    {
        string tableHeader =
            numberOfEdits < MessageDisplayEditThreshold
                ? WikiDiff.TableHeader
                : WikiDiff.TableHeaderNoMessages;

        string diffMarkup = _diff.GetDiff(
            originalText,
            currentText,
            DiffContextLines);

        return string.Concat(
            "<!DOCTYPE html>",
            "<html>",
            "<head>",
            "<meta charset=\"utf-8\">",
            WikiDiff.DiffHead(),
            "</head>",
            "<body>",
            tableHeader,
            diffMarkup,
            "</table>",
            "</body>",
            "</html>");
    }
}