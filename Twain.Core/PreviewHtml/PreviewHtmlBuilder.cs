namespace Twain.Core.PreviewHtml;

/// <summary>
/// Builds HTML documents used to display rendered article previews.
/// </summary>
public static class PreviewHtmlBuilder
{
    /// <summary>
    /// Builds the HTML document displayed by the article preview browser.
    /// </summary>
    /// <param name="htmlHeaders">
    /// The HTML header content required by the rendered wiki preview.
    /// </param>
    /// <param name="previewHtml">
    /// The rendered article HTML returned by the wiki API.
    /// </param>
    /// <returns>
    /// A complete HTML document containing the rendered article preview.
    /// </returns>
    public static string BuildPreviewHtml(
        string htmlHeaders,
        string previewHtml)
    {
        return
            "<html><head>" +
            htmlHeaders +
            "</head><body style=\"background:white; margin:10px; text-align:left;\">" +
            previewHtml +
            "</body></html>";
    }
}