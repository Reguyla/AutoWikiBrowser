namespace Twain.Core.DiffHtml;

/// <summary>
/// Builds HTML documents used to display article differences.
/// </summary>
public static class DiffHtmlBuilder
{
    /// <summary>
    /// Builds the HTML document displayed when the article text has not changed.
    /// </summary>
    /// <returns>
    /// A complete HTML document indicating that no article changes were detected.
    /// </returns>
    public static string BuildNoChangesHtml()
    {
        return
            @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<title>AWB Diff</title>
</head>
<body>
<h2 style='padding-top: .5em;
padding-bottom: .17em;
border-bottom: 1px solid #aaa;
font-size: 150%;'>No changes</h2>
<p>Press the ""Skip"" button below to skip to the next page.</p>
</body>
</html>";
    }

    /// <summary>
    /// Builds the HTML document containing a generated article diff.
    /// </summary>
    /// <param name="diff">
    /// The diff generator used to compare the original and current article text.
    /// </param>
    /// <param name="originalText">
    /// The original article text.
    /// </param>
    /// <param name="currentText">
    /// The current article text to compare with the original.
    /// </param>
    /// <param name="numberOfEdits">
    /// The number of edits completed during the current session.
    /// </param>
    /// <returns>
    /// A complete HTML document containing the generated article diff.
    /// </returns>
    public static string BuildArticleDiffHtml(
        WikiDiff diff,
        string originalText,
        string currentText,
        int numberOfEdits)
    {
        string tableHeader =
            numberOfEdits < 10
                ? WikiDiff.TableHeader
                : WikiDiff.TableHeaderNoMessages;

        return
            "<!DOCTYPE html>" +
            "<html>" +
            "<head>" +
            "<meta charset=\"utf-8\">" +
            WikiDiff.DiffHead() +
            "</head>" +
            "<body>" +
            tableHeader +
            diff.GetDiff(
                originalText,
                currentText,
                2) +
            "</table>" +
            "</body>" +
            "</html>";
    }

    /// <summary>
    /// Builds the appropriate HTML diff document for the supplied article text.
    /// </summary>
    /// <param name="diff">
    /// The diff generator used to compare the original and current article text.
    /// </param>
    /// <param name="originalText">
    /// The original article text.
    /// </param>
    /// <param name="currentText">
    /// The current article text to compare with the original.
    /// </param>
    /// <param name="numberOfEdits">
    /// The number of edits completed during the current session.
    /// </param>
    /// <returns>
    /// A complete HTML document containing either the generated diff or a
    /// no-changes message.
    /// </returns>
    public static string BuildDiffHtml(
        WikiDiff diff,
        string originalText,
        string currentText,
        int numberOfEdits)
    {
        if (string.Equals(
                originalText,
                currentText,
                StringComparison.Ordinal))
        {
            return BuildNoChangesHtml();
        }

        return BuildArticleDiffHtml(
            diff,
            originalText,
            currentText,
            numberOfEdits);
    }
}