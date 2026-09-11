namespace Twain.Core.Controls.Lists;

/// <summary>
/// Contains the result of comparing two article lists.
/// </summary>
public sealed class ListComparisonResult
{
    public ListComparisonResult(
        List<Article> onlyInList1,
        List<Article> onlyInList2,
        List<Article> common)
    {
        ArgumentNullException.ThrowIfNull(onlyInList1);
        ArgumentNullException.ThrowIfNull(onlyInList2);
        ArgumentNullException.ThrowIfNull(common);

        OnlyInList1 = onlyInList1;
        OnlyInList2 = onlyInList2;
        Common = common;
    }

    /// <summary>
    /// Gets the articles that exist only in the first list.
    /// </summary>
    public List<Article> OnlyInList1 { get; }

    /// <summary>
    /// Gets the articles that exist only in the second list.
    /// </summary>
    public List<Article> OnlyInList2 { get; }

    /// <summary>
    /// Gets the articles that exist in both lists.
    /// </summary>
    public List<Article> Common { get; }
}