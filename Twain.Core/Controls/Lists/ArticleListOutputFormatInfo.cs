
using Twain.Core.Controls.Lists;

namespace Twain.Core.Controls.Lists;

/// <summary>
/// Provides shared metadata for article-list export formats.
/// </summary>
public static class ArticleListOutputFormatInfo
{
    /// <summary>
    /// Returns the appropriate file extension for an output format.
    /// </summary>
    public static string GetExtension(
        ArticleListOutputFormat format)
    {
        return format switch
        {
            ArticleListOutputFormat.WikiText => ".txt",
            ArticleListOutputFormat.PlainText => ".txt",
            ArticleListOutputFormat.Csv => ".csv",
            ArticleListOutputFormat.CsvWikiText => ".csv",
            ArticleListOutputFormat.TwainSettingsXml => ".xml",

            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unsupported article-list output format.")
        };
    }

    /// <summary>
    /// Indicates whether the format requires application preferences.
    /// </summary>
    public static bool RequiresPreferences(
        ArticleListOutputFormat format)
    {
        return format switch
        {
            ArticleListOutputFormat.WikiText => false,
            ArticleListOutputFormat.PlainText => false,
            ArticleListOutputFormat.Csv => false,
            ArticleListOutputFormat.CsvWikiText => false,
            ArticleListOutputFormat.TwainSettingsXml => true,

            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unsupported article-list output format.")
        };
    }
}
