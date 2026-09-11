namespace Twain.Core.Controls.Lists;

/// <summary>
/// Provides article-list comparison operations.
/// </summary>
public static class ListComparerProcessor
{
    /// <summary>
    /// Compares two article lists and separates unique and common articles.
    /// </summary>
    public static ListComparisonResult Compare(
        IEnumerable<Article> list1,
        IEnumerable<Article> list2)
    {
        ArgumentNullException.ThrowIfNull(list1);
        ArgumentNullException.ThrowIfNull(list2);

        List<Article> first =
            list1.ToList();

        List<Article> second =
            list2.ToList();

        return new ListComparisonResult(
            first.Except(second).ToList(),
            second.Except(first).ToList(),
            first.Intersect(second).ToList());
    }
}