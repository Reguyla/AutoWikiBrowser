namespace Twain.Core.Lists;

/// <summary>
/// Provides namespace information used by article-list filtering.
/// </summary>
public static class NamespaceFilterHelper
{
    /// <summary>
    /// Gets the namespaces available for article-list filtering.
    /// </summary>
    /// <returns>
    /// The available non-negative namespaces, including the main article
    /// namespace.
    /// </returns>
    public static List<NamespaceFilterItem> GetAvailableNamespaces()
    {
        List<NamespaceFilterItem> namespaces = new()
        {
            new NamespaceFilterItem(
                0,
                "Main/Article",
                false)
        };

        foreach (KeyValuePair<int, string> namespaceItem in Variables.Namespaces)
        {
            if (namespaceItem.Key < 0)
                continue;

            namespaces.Add(
                new NamespaceFilterItem(
                    namespaceItem.Key,
                    namespaceItem.Value,
                    Namespace.IsTalk(namespaceItem.Key)));
        }

        return namespaces;
    }
}