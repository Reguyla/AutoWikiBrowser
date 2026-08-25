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
}