namespace Twain.Core.Lists;

/// <summary>
/// Provides article-list filtering operations that are independent of the UI.
/// </summary>
public static class ArticleListFilterProcessor
{
    /// <summary>
    /// Returns only articles whose namespace is included in the supplied
    /// namespace selection.
    /// </summary>
    /// <remarks>
    /// An empty namespace selection leaves the article list unchanged.
    /// </remarks>
    public static List<Article> FilterByNamespace(
        IEnumerable<Article> articles,
        IEnumerable<int> selectedNamespaces)
    {
        ArgumentNullException.ThrowIfNull(articles);
        ArgumentNullException.ThrowIfNull(selectedNamespaces);

        HashSet<int> namespaces =
            new(selectedNamespaces);

        if (namespaces.Count == 0)
            return articles.ToList();

        return articles
            .Where(article =>
                namespaces.Contains(article.NameSpaceKey))
            .ToList();
    }

    /// <summary>
    /// Filters articles by title content.
    /// </summary>
    /// <param name="articles">
    /// The articles to filter.
    /// </param>
    /// <param name="containsText">
    /// Text that article titles must not contain when filtering is enabled.
    /// </param>
    /// <param name="doesNotContainText">
    /// Text that article titles must contain when exclusion filtering is enabled.
    /// </param>
    /// <param name="filterContains">
    /// Whether to remove titles that match <paramref name="containsText"/>.
    /// </param>
    /// <param name="filterDoesNotContain">
    /// Whether to remove titles that do not match <paramref name="doesNotContainText"/>.
    /// </param>
    /// <param name="useRegex">
    /// Whether the supplied text values should be interpreted as regular expressions.
    /// </param>
    /// <returns>
    /// The filtered article list.
    /// </returns>
    public static List<Article> FilterByTitle(
        IEnumerable<Article> articles,
        string containsText,
        string doesNotContainText,
        bool filterContains,
        bool filterDoesNotContain,
        bool useRegex)
    {
        ArgumentNullException.ThrowIfNull(articles);

        List<Article> result =
            articles.ToList();

        if (!filterContains &&
            !filterDoesNotContain)
        {
            return result;
        }

        Regex containsRegex = null;
        Regex doesNotContainRegex = null;

        if (filterContains)
        {
            containsRegex =
                new Regex(
                    useRegex
                        ? containsText
                        : Regex.Escape(containsText),
                    RegexOptions.Compiled);
        }

        if (filterDoesNotContain)
        {
            doesNotContainRegex =
                new Regex(
                    useRegex
                        ? doesNotContainText
                        : Regex.Escape(doesNotContainText),
                    RegexOptions.Compiled);
        }

        result.RemoveAll(
            article =>
                filterContains &&
                containsRegex.IsMatch(article.Name) ||
                filterDoesNotContain &&
                !doesNotContainRegex.IsMatch(article.Name));

        return result;
    }

    /// <summary>
    /// Filters an article list against another article collection.
    /// </summary>
    /// <param name="articles">
    /// The source articles to filter.
    /// </param>
    /// <param name="filterArticles">
    /// The articles used as the comparison set.
    /// </param>
    /// <param name="intersect">
    /// When <see langword="true"/>, keeps only articles found in both collections.
    /// When <see langword="false"/>, removes articles found in
    /// <paramref name="filterArticles"/>.
    /// </param>
    /// <returns>
    /// The filtered article list.
    /// </returns>
    public static List<Article> FilterByArticleSet(
        IEnumerable<Article> articles,
        IEnumerable<Article> filterArticles,
        bool intersect)
    {
        ArgumentNullException.ThrowIfNull(articles);
        ArgumentNullException.ThrowIfNull(filterArticles);

        HashSet<Article> result =
            new(articles);

        if (intersect)
            result.IntersectWith(filterArticles);
        else
            result.ExceptWith(filterArticles);

        return result.ToList();
    }
}