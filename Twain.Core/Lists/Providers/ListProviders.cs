/*
Copyright (C) 2007 Martin Richards
(C) 2008 Stephen Kennedy, Sam Reed

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

using System.Xml;
using Twain.Core.Controls.Lists;

namespace Twain.Core.Lists.Providers;

// TODO: Consolidate category-name normalization helpers so list providers use
// one consistent implementation for decoding titles, removing fragments,
// adjusting capitalization, and stripping namespace prefixes.

/// <summary>
/// Retrieves pages contained directly in one or more named categories.
/// Subcategories are not traversed.
/// </summary>
public class CategoryListProvider : CategoryProviderBase
{
    /// <inheritdoc />
    public override List<Article> MakeList(
        params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string category in PrepareCategories(searchCriteria))
        {
            list.AddRange(
                GetListing(category, list.Count));
        }

        return list;
    }

    /// <inheritdoc />
    public override string DisplayText => "Category";
}

/// <summary>
/// Retrieves pages from one or more named categories and recursively
/// traverses their subcategories.
/// </summary>
public class CategoryRecursiveListProvider : CategoryProviderBase
{
    /// <summary>
    /// The maximum supported category recursion depth.
    /// </summary>
    public const int MaxDepth = 30;

    private int _depth = MaxDepth;

    /// <summary>
    /// Gets or sets the maximum number of subcategory levels traversed
    /// during a category scan.
    /// </summary>
    public int Depth
    {
        get => _depth;
        set => _depth = Math.Clamp(value, 0, MaxDepth);
    }

    /// <summary>
    /// Initializes a provider using the maximum recursion depth.
    /// </summary>
    public CategoryRecursiveListProvider()
        : this(MaxDepth)
    {
    }

    /// <summary>
    /// Initializes a provider using the specified recursion depth.
    /// </summary>
    /// <param name="depth">
    /// The number of subcategory levels to traverse.
    /// </param>
    public CategoryRecursiveListProvider(int depth)
    {
        Depth = depth;
        Limit = 200000;
    }

    /// <inheritdoc />
    public override List<Article> MakeList(
        params string[] searchCriteria)
    {
        List<Article> list = new();

        lock (Visited)
        {
            Visited.Clear();

            try
            {
                foreach (string category in
                         PrepareCategories(searchCriteria))
                {
                    list.AddRange(
                        RecurseCategory(
                            category,
                            list.Count,
                            Depth));
                }
            }
            finally
            {
                Visited.Clear();
            }
        }

        return list;
    }

    /// <inheritdoc />
    public override string DisplayText =>
        "Category (recursive)";
}

/// <summary>
/// Retrieves pages from named categories and traverses one level of
/// subcategories.
/// </summary>
public class CategoryRecursiveOneLevelListProvider
    : CategoryRecursiveListProvider
{
    /// <summary>
    /// Initializes a provider configured to traverse one subcategory level.
    /// </summary>
    public CategoryRecursiveOneLevelListProvider()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override string DisplayText =>
        "Category (recurse 1 level)";
}

/// <summary>
/// Retrieves pages from named categories using a recursion depth selected
/// by the user.
/// </summary>
public class CategoryRecursiveUserDefinedLevelListProvider
    : CategoryRecursiveListProvider
{
    /// <summary>
    /// Initializes a provider whose recursion depth is selected when the
    /// list is generated.
    /// </summary>
    public CategoryRecursiveUserDefinedLevelListProvider()
        : base(0)
    {
    }

    /// <inheritdoc />
    public override List<Article> MakeList(
        params string[] searchCriteria)
    {
        int userDepth =
            Tools.GetNumberFromUser(false, MaxDepth);

        if (userDepth < 0)
            return new();

        Depth = userDepth;

        return base.MakeList(searchCriteria);
    }

    /// <inheritdoc />
    public override string DisplayText =>
        "Category (recurse user defined level)";
}

/// <summary>
/// Gets a list of Categories on the specified pages
/// </summary>
public class CategoriesOnPageListProvider : ApiListProviderBase
{
    protected string clshow;

    #region Tags: <categories>/<cl>
    static readonly List<string> pe = new(["cl"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["categories"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url =
                "prop=categories&cllimit=max&titles=" +
                WebUtility.UrlEncode(page) +
                "&clshow=" +
                clshow;

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "Categories on page"; } }

    public override string UserInputTextBoxText
    { get { return "Pages:"; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }
    #endregion
}

/// <summary>
/// Gets a List of Categories on a page, excluding hidden categories, includes categories provided to page by a template
/// </summary>
public class CategoriesOnPageNoHiddenListProvider : CategoriesOnPageListProvider
{
    public CategoriesOnPageNoHiddenListProvider()
    {
        clshow = "!hidden";
    }

    public override string DisplayText
    { get { return base.DisplayText + " (no hidden cats)"; } }
}

/// <summary>
/// Gets a List of only hidden Categories on a page
/// </summary>
public class CategoriesOnPageOnlyHiddenListProvider : CategoriesOnPageListProvider
{
    public CategoriesOnPageOnlyHiddenListProvider()
    {
        clshow = "hidden";
    }

    public override string DisplayText
    { get { return base.DisplayText + " (only hidden cats)"; } }
}

/// <summary>
/// Gets a list of pages which link to the Named Pages
/// </summary>
public class WhatLinksHereListProvider : ApiListProviderBase, ISpecialPageProvider
{
    public WhatLinksHereListProvider()
    { }

    public WhatLinksHereListProvider(int limit)
    {
        Limit = limit;
    }

    #region Tags: <backlinks>/<bl>
    static readonly List<string> pe = new(["bl"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["backlinks"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    protected bool IncludeWhatLinksToRedirects;
    protected string Blfilterredir;
    public string ForceQueryLimit { get; set; }

    public List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        return MakeList(Namespace.ToString(), searchCriteria);
    }

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    protected List<Article> MakeList(
        string Namespace,
        params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url =
                "list=backlinks&bltitle=" +
                WebUtility.UrlEncode(page) +
                "&blnamespace=" +
                Namespace;

            if (!string.IsNullOrEmpty(ForceQueryLimit))
            {
                url += "&bllimit=" + ForceQueryLimit;
            }
            else
            {
                url += "&bllimit=max";
            }

            if (IncludeWhatLinksToRedirects)
            {
                url += "&blredirect";
            }

            if (!string.IsNullOrEmpty(Blfilterredir))
            {
                url += "&blfilterredir=" + Blfilterredir;
            }

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "What links here"; } }

    public override string UserInputTextBoxText
    { get { return "What links to:"; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }
    #endregion

    #region ISpecialPageProvider Members

    public bool PagesNeeded
    {
        get { return false; }
    }

    public bool NamespacesEnabled
    {
        get { return true; }
    }

    #endregion
}

/// <summary>
/// Gets a list of pages (all ns's) from which link to the Named Pages
/// </summary>
public class WhatLinksHereAllNSListProvider : WhatLinksHereListProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList("", searchCriteria);
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "What links here (all NS)"; } }
    #endregion
}

/// <summary>
/// Gets a list of pages (all ns's) from which link to the Named Pages
/// (If linking page is a redirect, get pages which link to that also)
/// </summary>
public class WhatLinksHereAndToRedirectsAllNSListProvider : WhatLinksHereAllNSListProvider
{
    public WhatLinksHereAndToRedirectsAllNSListProvider(int limit)
        : this()
    {
        Limit = limit;
    }

    public WhatLinksHereAndToRedirectsAllNSListProvider()
    {
        IncludeWhatLinksToRedirects = true;
    }

    public override string DisplayText
    { get { return base.DisplayText + " (and to redirects)"; } }
}

/// <summary>
/// Gets a list of pages which link to the Named Pages
/// (If linking page is a redirect, get pages which link to that also)
/// </summary>
public class WhatLinksHereAndToRedirectsListProvider : WhatLinksHereListProvider
{
    public WhatLinksHereAndToRedirectsListProvider(int limit)
        : this()
    {
        Limit = limit;
    }

    public WhatLinksHereAndToRedirectsListProvider()
    {
        IncludeWhatLinksToRedirects = true;
    }

    public override string DisplayText
    { get { return base.DisplayText + " (and to redirects)"; } }
}

/// <summary>
/// Gets a list of non redirect pages which link to the Named Pages
/// (If linking page is a redirect, get pages which link to that also)
/// </summary>
public class WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider : WhatLinksHereListProvider
{
    public WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider(int limit)
        : this()
    {
        Limit = limit;
    }

    public WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider()
    {
        Blfilterredir = "nonredirects";
        IncludeWhatLinksToRedirects = true;
    }

    public override string DisplayText
    { get { return base.DisplayText + " directly"; } }

    protected override bool EvaluateXmlElement(XmlReader xml)
    {
        return !xml.MoveToAttribute("redirect");
    }
}

/// <summary>
/// Gets a list of pages (excluding any redirects) which link to the Named Pages
/// </summary>
public class WhatLinksHereExcludingPageRedirectsListProvider : WhatLinksHereListProvider
{
    public WhatLinksHereExcludingPageRedirectsListProvider(int limit)
        : this()
    {
        Limit = limit;
    }

    public WhatLinksHereExcludingPageRedirectsListProvider()
    {
        Blfilterredir = "nonredirects";
    }

    public override string DisplayText
    { get { return base.DisplayText + " (no redirects)"; } }
}

/// <summary>
/// Gets a list of pages which redirect to the Named Pages
/// </summary>
public class RedirectsListProvider : WhatLinksHereListProvider
{
    public RedirectsListProvider()
    {
        Blfilterredir = "redirects";
    }

    public override string DisplayText
    { get { return "What redirects here"; } }

    public override string UserInputTextBoxText
    { get { return "Redirects to:"; } }
}

/// <summary>
/// Gets a list of pages which redirect to the Named Pages (in all NS's)
/// </summary>
public class RedirectsAllNSListProvider : WhatLinksHereAllNSListProvider
{
    public RedirectsAllNSListProvider()
    {
        Blfilterredir = "redirects";
    }

    public override string DisplayText
    { get { return "What redirects here (all NS)"; } }

    public override string UserInputTextBoxText
    { get { return "Redirects to:"; } }
}

/// <summary>
/// Gets a list of pages which transclude the Named Pages
/// </summary>
public class WhatTranscludesPageListProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <embeddedin>/<ei>
    static readonly List<string> pe = new(["ei"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["embeddedin"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public virtual List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        return MakeList(Namespace.ToString(), searchCriteria);
    }

    protected List<Article> MakeList(
        string Namespace,
        params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url =
                "list=embeddedin&eititle=" +
                WebUtility.UrlEncode(page) +
                "&eilimit=max&einamespace=" +
                Namespace;

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "What transcludes page"; } }

    public override string UserInputTextBoxText
    { get { return "What embeds:"; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    { }
    #endregion

    public virtual bool PagesNeeded
    { get { return true; } }

    public bool NamespacesEnabled
    { get { return true; } }
}

/// <summary>
/// Gets a list of pages (all ns's) which transclude the Named Pages
/// </summary>
public class WhatTranscludesPageAllNSListProvider : WhatTranscludesPageListProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList("", searchCriteria);
    }

    public override string DisplayText
    { get { return "What transcludes page (all NS)"; } }
}

/// <summary>
/// Gets a list of all (red && blue) links on the Named Pages
/// </summary>
public class LinksOnPageListProvider : ApiListProviderBase
{
    #region Tags: <links>/<pl>
    static readonly List<string> pe = new(["pl"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["links"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url = "prop=links&titles="
                         + WebUtility.UrlEncode(page) + "&pllimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "Links on page"; } }

    public override string UserInputTextBoxText
    { get { return "Links on:"; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }
    #endregion
}

/// <summary>
/// Gets a list of all red links on the Named Pages
/// </summary>
public class LinksOnPageOnlyRedListProvider : ApiListProviderBase
{
    public LinksOnPageOnlyRedListProvider()
    {
        Limit = 5000; // Cant imagine a page having more than 5000 links...
    }

    #region Tags: <pages>/<page>
    static readonly List<string> pe = new(["page"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["pages"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url = "generator=links&titles="
                         + WebUtility.UrlEncode(page) + "&gpllimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    protected override bool EvaluateXmlElement(XmlReader xml)
    {
        return xml.MoveToAttribute("missing");
    }

    public override string DisplayText
    {
        get { return "Links on page (only redlinks)"; }
    }

    public override string UserInputTextBoxText
    {
        get { return "Links on:"; }
    }

    public override bool UserInputTextBoxEnabled
    {
        get { return true; }
    }

    public override void Selected()
    {
    }
}

/// <summary>
/// Gets a list of all blue links on the Named Pages
/// </summary>
public class LinksOnPageOnlyBlueListProvider
    : LinksOnPageOnlyRedListProvider
{
    protected override bool EvaluateXmlElement(XmlReader xml)
    {
        return !base.EvaluateXmlElement(xml);
    }

    public override string DisplayText
    {
        get { return "Links on page (only bluelinks)"; }
    }
}

/// <summary>
/// Gets a list of all Images on the Named Pages
/// </summary>
public class FilesOnPageListProvider : ApiListProviderBase
{
    #region Tags: <images>/<im>
    static readonly List<string> pe = new(["im"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["images"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url = "prop=images&titles="
                         + WebUtility.UrlEncode(page) + "&imlimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "Files on page"; } }

    public override string UserInputTextBoxText
    { get { return "Files on:"; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }
    #endregion
}

/// <summary>
/// Gets a list of all the transclusions on the Named Pages
/// </summary>
public class TransclusionsOnPageListProvider : ApiListProviderBase
{
    #region Tags: <templates>/<tl>
    static readonly List<string> pe = new(["tl"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["templates"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url = "prop=templates&titles="
                         + WebUtility.UrlEncode(page) + "&tllimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "Transclusions on page"; } }

    public override string UserInputTextBoxText
    { get { return "Transclusions on:"; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }
    #endregion
}

/// <summary>
/// Gets the user contributions of the Named Users
/// </summary>
public class UserContribsListProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <usercontribs>/<item>
    static readonly List<string> pe = new(["item"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["usercontribs"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    protected string uclimit = "max";

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList("", searchCriteria);
    }

    public List<Article> MakeList(string @namespace, string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url = "list=usercontribs&ucuser=" +
                         Tools.WikiEncode(
                             Regex.Replace(page, Variables.NamespacesCaseInsensitive[Namespace.Category], ""))
                         + "&uclimit=" + uclimit
                         + "&ucnamespace=" + @namespace;

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "User contribs"; } }

    public override string UserInputTextBoxText
    { get { return Variables.Namespaces[Namespace.User]; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }

    public override bool RunOnSeparateThread
    { get { return true; } }
    #endregion

    #region ISpecialPageProvider Members

    public List<Article> MakeList(int @namespace, params string[] searchCriteria)
    {
        return MakeList(@namespace.ToString(), searchCriteria);
    }

    public bool PagesNeeded
    {
        get { return true; }
    }

    public bool NamespacesEnabled
    {
        get { return true; }
    }

    #endregion
}

/// <summary>
/// Gets the specified number of user contributions for the Named Users
/// </summary>
public class UserContribUserDefinedNumberListProvider : UserContribsListProvider
{
    public UserContribUserDefinedNumberListProvider()
    {
        UpperLimit = 25000;
    }

    protected int UpperLimit;
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        Limit = Tools.GetNumberFromUser(true, UpperLimit);
        uclimit = Limit.ToString();

        return base.MakeList(searchCriteria);
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "User contribs (user defined number)"; } }
    #endregion
}

/// <summary>
/// Gets a list of pages which link to the Named Images
/// </summary>
public class ImageFileLinksListProvider : ApiListProviderBase
{
    #region Tags: <imageusage>/<iu>
    static readonly List<string> pe = new(["iu"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["imageusage"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string image = Regex.Replace(page, "^" + Variables.Namespaces[Namespace.File],
                                         "", RegexOptions.IgnoreCase);
            image = WebUtility.UrlEncode(image);

            string url = "list=imageusage&iutitle=Image:"
                         + image + "&iulimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "Image file links"; } }

    public override string UserInputTextBoxText
    { get { return Variables.Namespaces[Namespace.File]; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }
    #endregion
}

/// <summary>
/// Gets a list of pages which are returned from a wiki search of the Named Pages
/// </summary>
/// <remarks>Slow query!!</remarks>
public class WikiSearchListProvider : ApiListProviderBase, ISpecialPageProvider
{
    protected string SearchType = "text", SearchPrefix = string.Empty;

    #region Tags: <search>/<p>
    static readonly List<string> pe = new(["p"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["search"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public WikiSearchListProvider()
    {
        Limit = 1000; // slow query
    }

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(0, searchCriteria);
    }

    public List<Article> MakeList(int @namespace, params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url;
            if (SearchPrefix.Equals("all:"))
            {
                url = string.Format("list=search&srwhat={0}&srnamespace=*&srsearch={1}&srlimit=max",
                    SearchType,
                    WebUtility.UrlEncode(page)
                );
            }
            else
            {
                url = string.Format("list=search&srwhat={0}&srnamespace={1}&srsearch={2}{3}&srlimit=max",
                    SearchType,
                    @namespace.ToString(),
                    SearchPrefix,
                    WebUtility.UrlEncode(page)
                    );
            }
            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "Wiki search (text)"; } }

    public override string UserInputTextBoxText
    { get { return "Wiki search:"; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public virtual bool PagesNeeded
    { get { return true; } }

    public bool NamespacesEnabled
    { get { return true; } }

    public override void Selected()
    {
    }
    #endregion
}

/// <summary>
/// Gets a list of pages which are returned from a title wiki search of the Named Pages, across all namespaces
/// </summary>
public class WikiSearchAllNSListProvider : WikiSearchListProvider
{
    public WikiSearchAllNSListProvider()
    {
        SearchPrefix = "all:";
    }

    public override string DisplayText
    { get { return base.DisplayText + " (all NS)"; } }
}

/// <summary>
/// Gets a list of pages which are returned from a title wiki search of the Named Pages
/// </summary>
public class WikiTitleSearchListProvider : WikiSearchListProvider
{
    public WikiTitleSearchListProvider()
    {
        // SearchType = "title";
        SearchPrefix = "intitle:";
    }

    public override string DisplayText
    { get { return "Wiki search (title)"; } }
}

/// <summary>
/// Gets a list of pages which are returned from a title wiki search of the Named Pages, across all namespaces
/// </summary>
public class WikiTitleSearchAllNSListProvider : WikiTitleSearchListProvider
{
    public WikiTitleSearchAllNSListProvider()
    {
        SearchPrefix = "all:" + SearchPrefix;
    }

    public override string DisplayText
    { get { return base.DisplayText + " (all NS)"; } }
}

/// <summary>
/// Gets all the pages from the current user's watchlist
/// </summary>
public class MyWatchlistListProvider : ApiListProviderBase
{
    #region Tags: <watchlistraw>/<wr>
    static readonly List<string> pe = new(["wr"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["watchlistraw"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return ApiMakeList("list=watchlistraw&wrlimit=max", 0);
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "My watchlist"; } }

    public override string UserInputTextBoxText
    { get { return string.Empty; } }

    public override bool UserInputTextBoxEnabled
    { get { return false; } }

    public override void Selected()
    {
    }
    #endregion
}

/// <summary>
/// Runs the Database Scanner
/// </summary>
public class DatabaseScannerListProvider : IListProvider
{
    private readonly ListMaker LMaker;

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="lm">ListMaker for DBScanner to add articles to</param>
    public DatabaseScannerListProvider(ListMaker lm)
    {
        LMaker = lm;
    }

    public List<Article> MakeList(params string[] searchCriteria)
    {
        new DBScanner.DatabaseScanner(LMaker).Show();
        return null;
    }

    public virtual bool StripUrl
    { get { return false; } }

    #region ListMaker properties
    public string DisplayText
    { get { return "Database dump"; } }

    public string UserInputTextBoxText
    { get { return string.Empty; } }

    public bool UserInputTextBoxEnabled
    { get { return false; } }

    public void Selected()
    {
    }

    public bool RunOnSeparateThread
    { get { return false; } }
    #endregion
}

/// <summary>
/// Gets 100 random articles
/// </summary>
public class RandomPagesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    protected string Extra;
    public RandomPagesSpecialPageProvider()
    {
        Limit = 100;
    }

    #region Tags: <random>/<page>
    static readonly List<string> pe = new(["page"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["random"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int Namespace, string[] searchCriteria)
    {
        List<Article> list = new();

        string url = "list=random&rnnamespace=" + Namespace +
                     "&rnlimit=max" + Extra;

        list.AddRange(ApiMakeList(url, list.Count));
        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "Random pages"; } }

    public override string UserInputTextBoxText
    { get { return string.Empty; } }

    public override bool UserInputTextBoxEnabled
    { get { return false; } }

    public override void Selected()
    {
    }
    #endregion

    public bool PagesNeeded
    { get { return false; } }

    public bool NamespacesEnabled
    { get { return true; } }
}

/// <summary>
/// Gets 100 random redirects
/// </summary>
public class RandomRedirectsSpecialPageProvider : RandomPagesSpecialPageProvider
{
    public RandomRedirectsSpecialPageProvider()
    {
        Extra = "&rnredirect";
    }

    public override string DisplayText
    { get { return "Random redirects"; } }
}

/// <summary>
/// Returns a list of "all pages" in a namespace
/// </summary>
public class AllPagesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <allpages>/<p>
    static readonly List<string> pe = new(["p"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["allpages"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    protected string From = "apfrom", Extra;

    #region ISpecialPageProvider Members

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public virtual List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url = "list=allpages&" + From + "=" +
                         WebUtility.UrlEncode(page) + "&apnamespace=" + Namespace + "&aplimit=max" + Extra;

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    public override string UserInputTextBoxText
    { get { return DisplayText; } }

    public virtual bool PagesNeeded
    { get { return false; } }
    #endregion

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }

    public override string DisplayText
    { get { return "All Pages"; } }

    public virtual bool NamespacesEnabled
    { get { return true; } }
}

/// <summary>
/// Returns a list of "all categories"
/// </summary>
public class AllCategoriesSpecialPageProvider : AllPagesSpecialPageProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Category, searchCriteria);
    }

    public override string DisplayText
    { get { return "All Categories"; } }

    public override string UserInputTextBoxText
    { get { return "Start Cat.:"; } }

    public override bool NamespacesEnabled
    { get { return false; } }
}

/// <summary>
/// Returns a list of "all files"
/// </summary>
public class AllFilesSpecialPageProvider : AllPagesSpecialPageProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.File, searchCriteria);
    }

    public override string DisplayText
    { get { return "All Files"; } }

    public override string UserInputTextBoxText
    { get { return "Start File:"; } }

    public override bool NamespacesEnabled
    { get { return false; } }
}

/// <summary>
/// Returns a list of "all redirects"
/// </summary>
public class AllRedirectsSpecialPageProvider : AllPagesSpecialPageProvider
{
    public AllRedirectsSpecialPageProvider()
    {
        Extra = "&apfilterredir=redirects";
    }

    public override string DisplayText
    { get { return "All Redirects"; } }

    public override string UserInputTextBoxText
    { get { return "Start Redirect:"; } }
}

/// <summary>
/// Returns a list of "all pages", without the redirects
/// </summary>
public class AllPagesNoRedirectsSpecialPageProvider : AllPagesSpecialPageProvider
{
    public AllPagesNoRedirectsSpecialPageProvider()
    {
        Extra = "&apfilterredir=nonredirects";
    }

    public override string DisplayText
    { get { return base.DisplayText + " (no redirects)"; } }

    public override string UserInputTextBoxText
    { get { return "Start page:"; } }
}

/// <summary>
/// Returns a list of protected pages
/// </summary>
public class ProtectedPagesSpecialPageProvider : AllPagesSpecialPageProvider
{
    private readonly ProtectionLevel Protlevel = new ProtectionLevel();

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public override List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        Protlevel.ShowDialog();
        Extra = "&apprtype=" + Protlevel.Type + "&apprlevel=" + Protlevel.Level;
        return base.MakeList(Namespace, searchCriteria);
    }

    public override string DisplayText
    { get { return "Protected Pages"; } }

    public override string UserInputTextBoxText
    { get { return "Pages:"; } }
}

/// <summary>
/// Returns a list of pages without language links
/// </summary>
public class PagesWithoutLanguageLinksSpecialPageProvider : AllPagesSpecialPageProvider
{
    public PagesWithoutLanguageLinksSpecialPageProvider()
    {
        Extra = "&apfilterlanglinks=withoutlanglinks";
    }

    public override string DisplayText
    { get { return "Pages without Language Links"; } }

    public override string UserInputTextBoxText
    { get { return "Pages:"; } }
}

/// <summary>
/// Returns a list of pages without language links, with no redirects
/// </summary>
public class PagesWithoutLanguageLinksNoRedirectsSpecialPageProvider : PagesWithoutLanguageLinksSpecialPageProvider
{
    public PagesWithoutLanguageLinksNoRedirectsSpecialPageProvider()
    {
        Extra += "&apfilterredir=nonredirects";
    }

    public override string DisplayText
    { get { return base.DisplayText + " (no redirects)"; } }
}

/// <summary>
/// Returns a list of subpages for the specified page
/// </summary>
public class PrefixIndexSpecialPageProvider : AllPagesSpecialPageProvider
{
    public PrefixIndexSpecialPageProvider()
    {
        From = "apprefix";
    }

    public override string DisplayText
    { get { return "All Pages with prefix (Prefixindex)"; } }

    public override bool PagesNeeded
    { get { return true; } }
}

/// <summary>
/// Returns a list of recent changes, by default in the 0 namespace
/// </summary>
public class RecentChangesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <recentchanges>/<rc>
    static readonly List<string> pe = new(["rc"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["recentchanges"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    #region ISpecialPageProvider Members
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string page in searchCriteria)
        {
            string url = "list=recentchanges&rctitles=" + WebUtility.UrlEncode(page) + "&rcnamespace=" + Namespace + "&rclimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    public override string DisplayText
    { get { return "Recent Changes"; } }

    public bool PagesNeeded
    { get { return false; } }
    #endregion

    public override string UserInputTextBoxText
    { get { return DisplayText; } }

    public override bool UserInputTextBoxEnabled
    { get { return false; } }

    public override void Selected()
    {
    }

    public bool NamespacesEnabled
    { get { return true; } }
}

/// <summary>
/// Returns a list of all users (their user pages) on the wiki
/// </summary>
public class AllUsersSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    public AllUsersSpecialPageProvider()
    {
        WantedAttribute = "name";
    }

    #region Tags: <allusers>/<u>
    static readonly List<string> pe = new(["u"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["allusers"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    #region ISpecialPageProvider Members
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int NamespaceIn, params string[] searchCriteria)
    {
        List<Article> list = new();

        list.AddRange(Tools.ConvertNamespace(ApiMakeList("list=allusers&aulimit=max", list.Count), Namespace.User));

        return list;
    }

    public override string DisplayText
    { get { return "All Users"; } }

    public bool PagesNeeded
    { get { return false; } }
    #endregion

    public override string UserInputTextBoxText
    { get { return string.Empty; } }

    public override bool UserInputTextBoxEnabled
    { get { return false; } }

    public override void Selected()
    {
    }

    public bool NamespacesEnabled
    { get { return false; } }
}

/// <summary>
/// Returns a list of new pages, by default in the 0 namespace
/// </summary>
/// <remarks>
/// Slow(ish) query! Api has:
/// ApiBase::PARAM_MAX => ApiBase::LIMIT_BIG1
/// ApiBase::PARAM_MAX2 => ApiBase::LIMIT_BIG2
/// </remarks>
public class NewPagesListProvider : ApiListProviderBase, ISpecialPageProvider
{
    public NewPagesListProvider()
    {
        Limit = 500;
    }

    #region Tags: <recentchanges>/<rc>
    static readonly List<string> pe = new(["rc"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["recentchanges"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        List<Article> list = new();

        string url = "list=recentchanges"
                     + "&rclimit=max&rctype=new&rcshow=!redirect&rcnamespace=" + Namespace;

        list.AddRange(ApiMakeList(url, list.Count));

        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "New pages"; } }

    public override string UserInputTextBoxText
    { get { return string.Empty; } }

    public override bool UserInputTextBoxEnabled
    { get { return false; } }

    public override void Selected()
    {
    }
    #endregion

    public bool PagesNeeded
    { get { return false; } }

    public bool NamespacesEnabled
    { get { return true; } }
}

/// <summary>
/// Returns a list of pages that contain the specified URL
/// </summary>
public class LinkSearchSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <exturlusage>/<eu>
    static readonly List<string> pe = new(["eu"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["exturlusage" ]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string searchUrl in searchCriteria)
        {
            int index = searchUrl.IndexOf("://", StringComparison.Ordinal);

            string protocol, urlEnd;

            if (index > -1)
            {
                protocol = searchUrl.Substring(0, index);
                urlEnd = searchUrl.Substring(index + 3);
            }
            else
            {
                protocol = string.Empty;
                urlEnd = searchUrl;
            }

            string url = "list=exturlusage&euquery=" +
                         WebUtility.UrlEncode(urlEnd) + "&eunamespace=" + Namespace +
                           "&euprotocol=" + protocol + "&eulimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "Link search"; } }

    public override string UserInputTextBoxText
    { get { return "URL:"; } }

    public override bool UserInputTextBoxEnabled
    { get { return true; } }

    public override void Selected()
    {
    }
    #endregion

    public bool PagesNeeded
    { get { return true; } }

    public bool NamespacesEnabled
    { get { return true; } }
}

/// <summary>
/// Returns a list of disambiguation pages
/// </summary>
public class DisambiguationPagesSpecialPageProvider : WhatTranscludesPageListProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return base.MakeList(Namespace.Article, new[] { "Template:Disambiguation" });
    }

    public override List<Article> MakeList(int @namespace, params string[] searchCriteria)
    {
        return base.MakeList(@namespace, new[] { "Template:Disambiguation" });
    }

    public override string DisplayText
    { get { return "Disambiguation Pages"; } }

    public override string UserInputTextBoxText
    { get { return string.Empty; } }

    public override bool UserInputTextBoxEnabled
    { get { return false; } }

    public override bool PagesNeeded
    { get { return false; } }
}

/// <summary>
/// Returns a list of new files
/// </summary>
/// <remarks>Slow query!</remarks>
public class GalleryNewFilesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <logevents>/<item>
    static readonly List<string> pe = new(["item"]);
    protected override ICollection<string> PageElements
    {
        get { return pe; }
    }

    static readonly List<string> ac = new(["logevents"]);
    protected override ICollection<string> Actions
    {
        get { return ac; }
    }
    #endregion

    public GalleryNewFilesSpecialPageProvider()
    {
        Limit = 1000; // slow query
    }

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        List<Article> list = new();

        list.AddRange(ApiMakeList("list=logevents&letype=upload&lelimit=max", list.Count));

        return list;
    }

    public List<Article> MakeList(int @namespace, string[] searchCriteria)
    {
        return MakeList(string.Empty);
    }

    #region ListMaker properties
    public override string DisplayText
    { get { return "New files"; } }

    public override string UserInputTextBoxText
    { get { return string.Empty; } }

    public override bool UserInputTextBoxEnabled
    { get { return false; } }

    public override void Selected()
    {
    }
    #endregion

    public bool PagesNeeded
    { get { return false; } }

    public bool NamespacesEnabled
    { get { return false; } }
}

public class PagesWithPropListProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <pageswithprop>/<page>
    protected override ICollection<string> PageElements
    {
        get { return new[] { "page" }; }
    }

    protected override ICollection<string> Actions
    {
        get { return new[] { "pageswithprop" }; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string prop in searchCriteria)
        {
            string url = "list=pageswithprop&pwppropname="
                         + WebUtility.UrlEncode(prop) + "&pwplimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    public List<Article> MakeList(int @namespace, params string[] searchCriteria)
    {
        return MakeList(searchCriteria);
    }

    #region ListMaker properties
    public override string DisplayText
    {
        get { return "Pages with a page property"; }
    }

    public override string UserInputTextBoxText
    {
        get { return "Property name:"; }
    }

    public override bool UserInputTextBoxEnabled
    {
        get { return true; }
    }

    public override void Selected()
    {
    }
    #endregion

    public bool PagesNeeded { get { return true; } }
    public bool NamespacesEnabled { get { return false; } }
}