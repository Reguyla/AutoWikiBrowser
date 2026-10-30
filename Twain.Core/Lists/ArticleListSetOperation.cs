namespace Twain.Core.Lists;

/// <summary>
/// Defines the supported operations for comparing article lists.
/// </summary>
public enum ArticleListSetOperation
{
    /// <summary>
    /// Keeps articles from the original list that are not
    /// present in the comparison list.
    /// </summary>
    Difference,

    /// <summary>
    /// Keeps only articles appearing in both lists.
    /// </summary>
    Intersection,

    /// <summary>
    /// Keeps articles appearing in either list,
    /// but not in both.
    /// </summary>
    SymmetricDifference
}