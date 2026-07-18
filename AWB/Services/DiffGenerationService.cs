using WikiFunctions;

namespace AutoWikiBrowser.Services.Diff;

internal sealed class DiffGenerationService
{
    private readonly WikiDiff _diff = new();

    internal string Generate(
        string originalText,
        string currentText,
        int numberOfEdits)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(currentText);

        if (originalText.Equals(currentText, StringComparison.Ordinal))
        {
            return BuildNoChangesHtml();
        }

        return BuildDiffHtml(
            originalText,
            currentText,
            numberOfEdits);
    }

    private static string BuildNoChangesHtml()
    {
        return
            @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<title>AWB Diff</title>
</head>
<body>
<h2 style=""padding-top: .5em;
padding-bottom: .17em;
border-bottom: 1px solid #aaa;
font-size: 150%;"">No changes</h2>
<p>Press the ""Skip"" button below to skip to the next page.</p>
</body>
</html>";
    }

    private string BuildDiffHtml(
        string originalText,
        string currentText,
        int numberOfEdits)
    {
        string tableHeader = numberOfEdits < 10
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
            _diff.GetDiff(
                originalText,
                currentText,
                2) +
            "</table>" +
            "</body>" +
            "</html>";
    }
}