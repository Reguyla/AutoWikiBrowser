/*
Copyright (C) 2008 Max Semenik, Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

namespace Twain.Core.Lists.Providers;

/// <summary>
/// Provides shared MediaWiki category-listing and recursive category
/// traversal behavior for category-based list providers.
/// </summary>
public abstract class CategoryProviderBase : ApiListProviderBase
{
    private readonly List<string> _pageElements = new() { "cm" };
    private readonly List<string> _actions = new() { "categorymembers" };

    /// <summary>
    /// Gets the XML element names that represent category members.
    /// </summary>
    protected override ICollection<string> PageElements =>
        _pageElements;

    /// <summary>
    /// Gets the MediaWiki API list action used to retrieve category members.
    /// </summary>
    protected override ICollection<string> Actions =>
        _actions;

    /// <inheritdoc />
    public override string UserInputTextBoxText
    {
        get
        {
            if (Variables.Namespaces.TryGetValue(
                Namespace.Category,
                out string value))
            {
                return value;
            }

            return Variables.CanonicalNamespaces[Namespace.Category];
        }
    }

    /// <inheritdoc />
    public override void Selected()
    {
    }

    /// <inheritdoc />
    public override bool UserInputTextBoxEnabled => true;

    /// <summary>
    /// Gets the pages and subcategories contained in the specified category.
    /// </summary>
    /// <param name="category">
    /// The category name without the <c>Category:</c> prefix.
    /// </param>
    /// <param name="haveSoFar">
    /// The number of pages already retrieved by the current list operation.
    /// </param>
    /// <returns>The category members returned by the MediaWiki API.</returns>
    public List<Article> GetListing(
        string category,
        int haveSoFar)
    {
        string url =
            $"&list=categorymembers&cmtitle=Category:" +
            $"{WebUtility.UrlEncode(category)}&cmlimit=max";

        return ApiMakeList(url, haveSoFar);
    }

    /// <summary>
    /// Gets the pages and subcategories contained in the specified category.
    /// </summary>
    /// <param name="category">
    /// The category name without the <c>Category:</c> prefix.
    /// </param>
    /// <returns>The category members returned by the MediaWiki API.</returns>
    public List<Article> GetListing(string category) =>
        GetListing(category, 0);

    /// <summary>
    /// Tracks normalized category names already visited during recursive
    /// category traversal.
    /// </summary>
    protected readonly List<string> Visited = new();

    /// <summary>
    /// Recursively retrieves pages from a category and its subcategories.
    /// </summary>
    /// <param name="category">
    /// The category name without the <c>Category:</c> prefix.
    /// </param>
    /// <param name="haveSoFar">
    /// The number of pages already retrieved by the current list operation.
    /// </param>
    /// <param name="depth">
    /// The maximum number of subcategory levels to traverse.
    /// </param>
    /// <returns>The pages retrieved from the category tree.</returns>
    public List<Article> RecurseCategory(
        string category,
        int haveSoFar,
        int depth)
    {
        if (haveSoFar >= Limit || depth < 0)
            return new();

        category =
            Tools.TurnFirstToUpper(
                Tools.WikiDecode(category));

        if (Visited.Contains(category))
            return new();

        Visited.Add(category);

        List<Article> list =
            GetListing(category, haveSoFar);

        if (depth == 0 ||
            haveSoFar + list.Count >= Limit)
        {
            return list;
        }

        List<Article> fromSubcategories = new();

        foreach (Article page in list)
        {
            if (haveSoFar + list.Count +
                fromSubcategories.Count >= Limit)
            {
                break;
            }

            if (page.NameSpaceKey != Namespace.Category ||
                Visited.Contains(page.Name))
            {
                continue;
            }

            fromSubcategories.AddRange(
                RecurseCategory(
                    page.NamespacelessName,
                    haveSoFar +
                    list.Count +
                    fromSubcategories.Count,
                    depth - 1));
        }

        list.AddRange(fromSubcategories);

        return list;
    }

    /// <summary>
    /// Normalizes category names and removes the localized
    /// <c>Category:</c> prefix.
    /// </summary>
    /// <param name="source">The category names to normalize.</param>
    /// <returns>The normalized category names.</returns>
    public static IEnumerable<string> PrepareCategories(
        IEnumerable<string> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<string> categories = new();

        foreach (string category in source)
        {
            string normalizedCategory =
                Tools.RemoveHashFromPageTitle(
                    Tools.WikiDecode(category))
                .Trim();

            normalizedCategory = Regex.Replace(
                    normalizedCategory,
                    "^" +
                    Variables.NamespacesCaseInsensitive[
                        Namespace.Category],
                    string.Empty)
                .Trim();

            categories.Add(normalizedCategory);
        }

        return categories;
    }

    /// <inheritdoc />
    public override bool StripUrl => true;
}