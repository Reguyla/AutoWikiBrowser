using Microsoft.CodeAnalysis;

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
    /// Filters an article list using the specified set operation.
    /// </summary>
    /// <param name="articles">
    /// The original article collection.
    /// </param>
    /// <param name="filterArticles">
    /// The comparison article collection.
    /// </param>
    /// <param name="operation">
    /// The set operation to perform.
    /// </param>
    /// <returns>
    /// The resulting article collection.
    /// </returns>
    public static List<Article> FilterByArticleSet(
        IEnumerable<Article> articles,
        IEnumerable<Article> filterArticles,
        ArticleListSetOperation operation)
    {
        ArgumentNullException.ThrowIfNull(articles);
        ArgumentNullException.ThrowIfNull(filterArticles);

        HashSet<Article> result = new(articles);

        switch (operation)
        {
            case ArticleListSetOperation.Difference:
                result.ExceptWith(filterArticles);
                break;

            case ArticleListSetOperation.Intersection:
                result.IntersectWith(filterArticles);
                break;

            case ArticleListSetOperation.SymmetricDifference:
                result.SymmetricExceptWith(filterArticles);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(operation),
                    operation,
                    "Unsupported article-list set operation.");
        }

        return result.ToList();
    }

    /// <summary>
    /// Filters an article list using the legacy intersection flag.
    /// </summary>
    /// <remarks>
    /// Retained temporarily for compatibility with existing callers.
    /// </remarks>
    public static List<Article> FilterByArticleSet(
        IEnumerable<Article> articles,
        IEnumerable<Article> filterArticles,
        bool intersect)
    {
        return FilterByArticleSet(
            articles,
            filterArticles,
            intersect
                ? ArticleListSetOperation.Intersection
                : ArticleListSetOperation.Difference);
    }

    /// <summary>
    /// Applies the configured list filters to an article collection.
    /// </summary>
    /// <param name="articles">
    /// The articles to filter.
    /// </param>
    /// <param name="filterArticles">
    /// The articles used by the optional set operation.
    /// </param>
    /// <param name="selectedNamespaces">
    /// The namespaces to retain.
    /// </param>
    /// <param name="containsText">
    /// Text used to remove matching article titles.
    /// </param>
    /// <param name="doesNotContainText">
    /// Text used to retain matching article titles.
    /// </param>
    /// <param name="filterContains">
    /// Whether titles matching <paramref name="containsText"/> should be removed.
    /// </param>
    /// <param name="filterDoesNotContain">
    /// Whether titles not matching <paramref name="doesNotContainText"/> should be removed.
    /// </param>
    /// <param name="useRegex">
    /// Whether the title filter values are regular expressions.
    /// </param>
    /// <param name="removeDuplicates">
    /// Whether duplicate articles should be removed.
    /// </param>
    /// <param name="applySetFilter">
    /// Whether the article-set operation should be applied.
    /// </param>
    /// <param name="operation">
    /// The set operation to perform when comparing article collections.
    /// </param>
    /// <param name="sortAlphabetically">
    /// Whether the resulting articles should be sorted alphabetically.
    /// </param>
    /// <returns>
    /// The filtered article list.
    /// </returns>
    public static List<Article> Apply(
        IEnumerable<Article> articles,
        IEnumerable<Article> filterArticles,
        IEnumerable<int> selectedNamespaces,
        string containsText,
        string doesNotContainText,
        bool filterContains,
        bool filterDoesNotContain,
        bool useRegex,
        bool removeDuplicates,
        bool applySetFilter,
        ArticleListSetOperation operation,
        bool sortAlphabetically)
    {
        ArgumentNullException.ThrowIfNull(articles);
        ArgumentNullException.ThrowIfNull(filterArticles);
        ArgumentNullException.ThrowIfNull(selectedNamespaces);

        List<Article> result =
            articles.ToList();

        if (removeDuplicates)
        {
            result =
                result
                    .Distinct()
                    .ToList();
        }

        if (applySetFilter)
        {
            result =
                FilterByArticleSet(
                    result,
                    filterArticles,
                    operation);
        }

        result =
            FilterByTitle(
                result,
                containsText,
                doesNotContainText,
                filterContains,
                filterDoesNotContain,
                useRegex);

        result =
            FilterByNamespace(
                result,
                selectedNamespaces);

        if (sortAlphabetically)
        {
            result =
                result
                    .OrderBy(article => article.Name)
                    .ToList();
        }

        return result;
    }

    /// <summary>
    /// Applies article-list filters using the legacy intersection flag.
    /// </summary>
    /// <remarks>
    /// Retained temporarily while callers migrate to ArticleListSetOperation.
    /// </remarks>
    public static List<Article> Apply(
        IEnumerable<Article> articles,
        IEnumerable<Article> filterArticles,
        IEnumerable<int> selectedNamespaces,
        string containsText,
        string doesNotContainText,
        bool filterContains,
        bool filterDoesNotContain,
        bool useRegex,
        bool removeDuplicates,
        bool applySetFilter,
        bool intersect,
        bool sortAlphabetically)
    {
        return Apply(
            articles,
            filterArticles,
            selectedNamespaces,
            containsText,
            doesNotContainText,
            filterContains,
            filterDoesNotContain,
            useRegex,
            removeDuplicates,
            applySetFilter,
            intersect
                ? ArticleListSetOperation.Intersection
                : ArticleListSetOperation.Difference,
            sortAlphabetically);
    }
}