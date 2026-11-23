
using Twain.Core.TwainSettings;

namespace Twain.Core.Controls.Lists;

/// <summary>
/// Saves article lists using the supported export formats.
/// </summary>
public static class ArticleListExporter
{
    /// <summary>
    /// Saves an article list to the specified path.
    /// </summary>
    public static void Save(
        IEnumerable<Article> articles,
        string path,
        ArticleListOutputFormat format,
        UserPrefs? prefs = null)
    {
        ArgumentNullException.ThrowIfNull(articles);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<Article> articleList = articles.ToList();

        if (format == ArticleListOutputFormat.TwainSettingsXml)
        {
            if (prefs is null)
            {
                throw new ArgumentNullException(
                    nameof(prefs),
                    "Twain settings export requires user preferences.");
            }

            // Preserve the original article-list reference.
            var originalArticleList = prefs.List.ArticleList;

            try
            {
                prefs.List.ArticleList = articleList;
                UserPrefs.SavePrefs(prefs, path);
            }
            finally
            {
                prefs.List.ArticleList = originalArticleList;
            }

            return;
        }

        string text =
            ArticleListOutputFormatter.Format(
                articleList,
                format);

        Tools.WriteTextFileAbsolutePath(
            text,
            path,
            false);
    }
}
