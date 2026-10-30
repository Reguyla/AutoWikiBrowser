namespace Twain.Core.Lists;

/// <summary>
/// Describes the options used to filter an article list.
/// </summary>
public sealed class ArticleListFilterConfiguration
{
    /// <summary>
    /// Gets or sets the namespaces that should be retained.
    /// An empty collection means that no namespace filtering is applied.
    /// </summary>
    public List<int> NamespaceIds { get; set; } = new();

    /// <summary>
    /// Gets or sets whether titles matching <see cref="ContainsText"/>
    /// should be removed.
    /// </summary>
    public bool FilterTitlesThatContain { get; set; }

    /// <summary>
    /// Gets or sets the title text or expression used by the contains filter.
    /// </summary>
    public string ContainsText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether titles that do not match
    /// <see cref="DoesNotContainText"/> should be removed.
    /// </summary>
    public bool FilterTitlesThatDoNotContain { get; set; }

    /// <summary>
    /// Gets or sets the title text or expression used by the
    /// does-not-contain filter.
    /// </summary>
    public string DoesNotContainText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the title filter values are regular expressions.
    /// </summary>
    public bool UseRegex { get; set; }

    /// <summary>
    /// Gets or sets whether duplicate articles should be removed.
    /// </summary>
    public bool RemoveDuplicates { get; set; }

    /// <summary>
    /// Gets or sets whether the resulting article list should be sorted
    /// in ascending title order.
    /// </summary>
    public bool SortAscending { get; set; }

    /// <summary>
    /// Gets or sets the operation used when comparing article lists.
    /// </summary>
    public ArticleListSetOperation ComparisonOperation { get; set; }
        = ArticleListSetOperation.Difference;

    /// <summary>
    /// Gets or sets whether the comparison operation is an intersection.
    /// </summary>
    /// <remarks>
    /// Retained temporarily for compatibility with existing callers.
    /// </remarks>
    [Obsolete("Use ComparisonOperation instead.")]
    public bool IntersectComparisonList
    {
        get => ComparisonOperation == ArticleListSetOperation.Intersection;

        set => ComparisonOperation = value
            ? ArticleListSetOperation.Intersection
            : ArticleListSetOperation.Difference;
    }

    /// <summary>
    /// Gets or sets the article titles used by the comparison-list filter.
    /// </summary>
    public List<string> ComparisonArticleTitles { get; set; } = new();
}