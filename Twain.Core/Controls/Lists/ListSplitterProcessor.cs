using Twain.Core.AWBSettings;

namespace Twain.Core.Controls.Lists;

/// <summary>
/// Provides article-list splitting operations used by the list splitter.
/// </summary>
public static class ListSplitterProcessor
{
    private static readonly Regex BadCharacters =
        new(
            @"[""/:*?<>|.]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Removes characters that cannot be used in the generated output
    /// file name.
    /// </summary>
    /// <param name="sourceText">
    /// The source text used to derive the output file name.
    /// </param>
    /// <returns>
    /// The source text with unsupported file-name characters removed.
    /// </returns>
    public static string CreateFileName(string sourceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        return BadCharacters.Replace(sourceText, string.Empty);
    }

    /// <summary>
    /// Calculates the number of output groups required for the supplied
    /// article count and group size.
    /// </summary>
    /// <param name="articleCount">
    /// The number of articles to split.
    /// </param>
    /// <param name="articlesPerFile">
    /// The maximum number of articles per output file.
    /// </param>
    /// <returns>
    /// The number of output groups required.
    /// </returns>
    public static int CalculateGroupCount(
        int articleCount,
        int articlesPerFile)
    {
        if (articleCount < 0)
            throw new ArgumentOutOfRangeException(nameof(articleCount));

        if (articlesPerFile <= 0)
            throw new ArgumentOutOfRangeException(nameof(articlesPerFile));

        if (articleCount == 0)
            return 0;

        return (articleCount + articlesPerFile - 1) / articlesPerFile;
    }

    /// <summary>
    /// Creates the text content for a group of articles from a split list.
    /// </summary>
    /// <param name="articles">
    /// The complete article list.
    /// </param>
    /// <param name="startIndex">
    /// The zero-based index at which the group begins.
    /// </param>
    /// <param name="maximumCount">
    /// The maximum number of articles to include in the group.
    /// </param>
    /// <returns>
    /// The article titles for the group, separated by line breaks.
    /// </returns>
    public static string CreateTextGroup(
        List<Article> articles,
        int startIndex,
        int maximumCount)
    {
        ArgumentNullException.ThrowIfNull(articles);

        if (startIndex < 0 || startIndex > articles.Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (maximumCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCount));

        int count =
            Math.Min(
                articles.Count - startIndex,
                maximumCount);

        StringBuilder text = new();

        foreach (Article article in articles.GetRange(startIndex, count))
        {
            text.AppendLine(article.ToString());
        }

        return text.ToString().TrimEnd();
    }

    /// <summary>
    /// Creates an article group from a split list.
    /// </summary>
    /// <param name="articles">
    /// The complete article list.
    /// </param>
    /// <param name="startIndex">
    /// The zero-based index at which the group begins.
    /// </param>
    /// <param name="maximumCount">
    /// The maximum number of articles to include in the group.
    /// </param>
    /// <returns>
    /// The requested article group.
    /// </returns>
    public static List<Article> CreateArticleGroup(
        List<Article> articles,
        int startIndex,
        int maximumCount)
    {
        ArgumentNullException.ThrowIfNull(articles);

        if (startIndex < 0 || startIndex > articles.Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (maximumCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCount));

        int count =
            Math.Min(
                articles.Count - startIndex,
                maximumCount);

        return articles.GetRange(startIndex, count);
    }

    /// <summary>
    /// Creates a numbered output path for a split file.
    /// </summary>
    /// <param name="path">
    /// The original output path selected by the user.
    /// </param>
    /// <param name="index">
    /// The one-based output file index.
    /// </param>
    /// <returns>
    /// The numbered output path.
    /// </returns>
    public static string CreateNumberedPath(
        string path,
        int index)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (index <= 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        string extension = Path.GetExtension(path);
        string directory = Path.GetDirectoryName(path) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(path);

        string numberedFileName =
            $"{fileName} {index}{extension}";

        return string.IsNullOrEmpty(directory)
            ? numberedFileName
            : Path.Combine(directory, numberedFileName);
    }

    /// <summary>
    /// Saves the supplied articles to numbered text files.
    /// </summary>
    /// <param name="articles">
    /// The articles to save.
    /// </param>
    /// <param name="path">
    /// The base output path.
    /// </param>
    /// <param name="articlesPerFile">
    /// The maximum number of articles written to each file.
    /// </param>
    public static void SaveTextFiles(
        List<Article> articles,
        string path,
        int articlesPerFile)
    {
        ArgumentNullException.ThrowIfNull(articles);
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (articlesPerFile <= 0)
            throw new ArgumentOutOfRangeException(nameof(articlesPerFile));

        int groupCount =
            CalculateGroupCount(
                articles.Count,
                articlesPerFile);

        int baseIndex = 0;

        for (int i = 1; i <= groupCount; i++)
        {
            string groupText =
                CreateTextGroup(
                    articles,
                    baseIndex,
                    articlesPerFile);

            Tools.WriteTextFileAbsolutePath(
                groupText,
                CreateNumberedPath(path, i),
                false);

            baseIndex += articlesPerFile;
        }
    }

    /// <summary>
    /// Saves the supplied articles to numbered AWB settings files.
    /// </summary>
    /// <param name="prefs">
    /// The user preferences used as the basis for each settings file.
    /// </param>
    /// <param name="articles">
    /// The articles to save.
    /// </param>
    /// <param name="path">
    /// The base output path.
    /// </param>
    /// <param name="articlesPerFile">
    /// The maximum number of articles written to each settings file.
    /// </param>
    public static void SaveSettingsFiles(
        UserPrefs prefs,
        List<Article> articles,
        string path,
        int articlesPerFile)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        ArgumentNullException.ThrowIfNull(articles);
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (articlesPerFile <= 0)
            throw new ArgumentOutOfRangeException(nameof(articlesPerFile));

        int groupCount =
            CalculateGroupCount(
                articles.Count,
                articlesPerFile);

        int baseIndex = 0;

        for (int i = 1; i <= groupCount; i++)
        {
            prefs.List.ArticleList =
                CreateArticleGroup(
                    articles,
                    baseIndex,
                    articlesPerFile);

            UserPrefs.SavePrefs(
                prefs,
                CreateNumberedPath(path, i));

            baseIndex += articlesPerFile;
        }
    }
}