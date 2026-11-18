namespace Twain.Core.PreviewHtml;

/// <summary>
/// Builds HTML documents used to display rendered article previews.
/// </summary>
public static class PreviewHtmlBuilder
{
    private const string DarkPreviewStyles = """
        <style>
            html, body {
                background-color: #1B222B !important;
                color: #E7E9EC !important;
            }

            a, a:visited {
                color: #8DBCF0;
            }

            a:visited {
                color: #B5A0E6;
            }

            h1, h2, h3, h4, h5, h6 {
                color: #E7E9EC;
                border-color: #46505C;
            }

            table, th, td {
                color: inherit;
                border-color: #46505C;
            }

            th,
            .infobox,
            .wikitable {
                background-color: #252E38;
                color: #E7E9EC;
            }

            .wikitable > tr > th,
            .wikitable > * > tr > th {
                background-color: #303A46;
                color: #E7E9EC;
            }

            .wikitable > tr > td,
            .wikitable > * > tr > td {
                background-color: #252E38;
                color: #E7E9EC;
            }
        </style>
        """;

    /// <summary>
    /// Builds the HTML document displayed by the article preview browser.
    /// </summary>
    public static string BuildPreviewHtml(
        string htmlHeaders,
        string previewHtml,
        bool darkTheme = false)
    {
        string styles = darkTheme
            ? DarkPreviewStyles
            : string.Empty;

        string bodyStyle = darkTheme
            ? "background:#1B222B; color:#E7E9EC; margin:10px; text-align:left;"
            : "background:white; margin:10px; text-align:left;";

        return
            "<html><head>" +
            htmlHeaders +
            styles +
            "</head><body style=\"" +
            bodyStyle +
            "\">" +
            previewHtml +
            "</body></html>";
    }
}