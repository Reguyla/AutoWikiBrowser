/*
Copyright (C) 2007 Martin Richards
(C) 2008 Stephen Kennedy, Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
*/

using System.Windows.Forms;

namespace WikiFunctions.Lists.Providers;

/// <summary>
/// Retrieves article titles by scraping line-based content from the body
/// of one or more HTML pages.
/// </summary>
public class HTMLPageScraperListProvider : IListProvider
{
    /// <inheritdoc />
    public virtual List<Article> MakeList(
        params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string url in searchCriteria)
        {
            string urlBuilt =
                Uri.TryCreate(url, UriKind.Absolute, out Uri absoluteUri) &&
                (absoluteUri.Scheme == Uri.UriSchemeHttp ||
                 absoluteUri.Scheme == Uri.UriSchemeHttps)
                    ? absoluteUri.AbsoluteUri
                    : $"http://{url}";

            if (!WikiRegexes.UrlValidator.IsMatch(urlBuilt))
            {
                throw new ArgumentException(
                    $"URL \"{urlBuilt}\" is not valid.",
                    nameof(searchCriteria));
            }

            string pageBody = Tools.StringBetween(
                Tools.GetHTML(urlBuilt),
                "<body>",
                "</body>");

            string[] entries = pageBody.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in entries)
            {
                if (entry.Length > 0 && CheckExtra(entry))
                {
                    list.Add(
                        new Article(
                            ModifyArticleName(entry)));
                }
            }
        }

        return list;
    }

    /// <summary>
    /// Determines whether a scraped entry should be included.
    /// </summary>
    /// <param name="entry">The scraped HTML entry to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the entry should be included; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool CheckExtra(string entry) =>
        !entry.StartsWith(
            "<h1>",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a scraped HTML entry into an article title.
    /// </summary>
    /// <param name="title">The scraped title text.</param>
    /// <returns>The normalized article title.</returns>
    protected virtual string ModifyArticleName(string title)
    {
        Parse.Parsers parser = new();

        title = parser.Unicodify(title);
        title = title.Replace("&amp;", "&");
        title = title.Replace("&quot;", "\"");

        return title.Replace("<br />", string.Empty);
    }

    /// <inheritdoc />
    public virtual string DisplayText => "HTML Scraper";

    /// <inheritdoc />
    public virtual string UserInputTextBoxText => "URL:";

    /// <inheritdoc />
    public bool UserInputTextBoxEnabled => true;

    /// <inheritdoc />
    public void Selected()
    {
    }

    /// <inheritdoc />
    public bool RunOnSeparateThread => true;

    /// <inheritdoc />
    public virtual bool StripUrl => false;
}

/// <summary>
/// Retrieves article titles from an online CheckWiki output page.
/// </summary>
public class CheckWikiListProvider : HTMLPageScraperListProvider
{
    private static readonly Regex Apostrophe =
        new(
            @"&#0?39;|&#146;|&amp;#0?39;|&amp;#146;|[`’]",
            RegexOptions.Compiled);

    /// <inheritdoc />
    protected override bool CheckExtra(string entry) =>
        !entry.StartsWith(
            "<pre>",
            StringComparison.OrdinalIgnoreCase) &&
        !entry.EndsWith(
            "</pre>",
            StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    protected override string ModifyArticleName(string title)
    {
        title = Apostrophe.Replace(title, "'");
        title = title.Replace("&amp;", "&");
        title = title.Replace("&quot;", "\"");

        return title.Replace("<br />", string.Empty);
    }

    /// <inheritdoc />
    public override string DisplayText => "CheckWiki error";
}

/// <summary>
/// Retrieves article titles from a CheckWiki report selected by error number.
/// </summary>
public class CheckWikiWithNumberListProvider : CheckWikiListProvider
{
    /// <inheritdoc />
    public override List<Article> MakeList(
        params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string errorNumber in searchCriteria)
        {
            string url =
                "https://checkwiki.toolforge.org/cgi-bin/checkwiki.cgi" +
                $"?project={WebUtility.UrlEncode(Variables.LangCode)}wiki" +
                $"&view=bots&id={WebUtility.UrlEncode(errorNumber)}" +
                "&offset=0";

            list.AddRange(
                base.MakeList(url));
        }

        return list;
    }

    /// <inheritdoc />
    public override string UserInputTextBoxText => "Error number:";

    /// <inheritdoc />
    public override string DisplayText =>
        "CheckWiki error (number)";
}

/// <summary>
/// Retrieves wiki article titles by scraping Google search results.
/// </summary>
/// <remarks>
/// This provider depends on Google's current HTML response structure and may
/// require maintenance when that structure changes.
/// </remarks>
public class GoogleSearchListProvider : IListProvider
{
    private static readonly Regex RegexGoogle =
        new(
            @"href\s*=\s*(?:""(?:/url\?q=)?(?<title>[^""]*)""|(?<title>\S+) class=l)",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled);

    /// <inheritdoc />
    public List<Article> MakeList(
        params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string searchTerm in searchCriteria)
        {
            int start = 0;
            string encodedSearchTerm =
                WebUtility.UrlEncode(searchTerm);

            while (true)
            {
                string url =
                    "https://www.google.com/search" +
                    $"?q={encodedSearchTerm}+site:{Variables.URL}" +
                    $"&num=100&hl=en&lr=&start={start}" +
                    "&sa=N&filter=0";

                string googleText =
                    Tools.GetHTML(url, Encoding.Default);

                foreach (Match match in
                         RegexGoogle.Matches(googleText))
                {
                    string searchResult =
                        match.Groups["title"].Value;

                    int encodedParameterIndex =
                        searchResult.IndexOf(
                            "&amp;",
                            StringComparison.Ordinal);

                    if (encodedParameterIndex >= 0)
                    {
                        searchResult =
                            searchResult[..encodedParameterIndex];
                    }

                    string title =
                        Tools.GetTitleFromURL(searchResult);

                    if (string.IsNullOrEmpty(title))
                        continue;

                    // Some Google results are double encoded, so decode
                    // the title again before creating the article.
                    string decodedTitle =
                        Tools.WikiDecode(title);

                    decodedTitle = Regex.Replace(
                        decodedTitle,
                        @"\?\w+=.*",
                        string.Empty);

                    list.Add(
                        new Article(decodedTitle));
                }

                if (!googleText.Contains(
                        "img src=\"nav_next.gif\"",
                        StringComparison.Ordinal))
                {
                    break;
                }

                start += 100;
            }
        }

        return Tools.FilterSomeArticles(list);
    }

    /// <inheritdoc />
    public string DisplayText => "Google search";

    /// <inheritdoc />
    public string UserInputTextBoxText => "Google search:";

    /// <inheritdoc />
    public bool UserInputTextBoxEnabled => true;

    /// <inheritdoc />
    public void Selected()
    {
    }

    /// <inheritdoc />
    public bool RunOnSeparateThread => true;

    /// <inheritdoc />
    public virtual bool StripUrl => false;
}

/// <summary>
/// Retrieves article titles from one or more UTF-8 encoded text files.
/// </summary>
/// <remarks>
/// The class name retains the historic <c>UFT8</c> spelling for compatibility
/// with existing callers.
/// </remarks>
public class TextFileListProviderUFT8 : IListProvider
{
    private static readonly Regex RegexFromFile =
        new(
            "(^[a-z]{2,3}:)|(simple:)",
            RegexOptions.Compiled);

    private static readonly Regex LoadWikiLink =
        new(
            @"\[\[:?([^\|[\]]+)(?:\]\]|\|)",
            RegexOptions.Compiled);

    private static readonly OpenFileDialog OpenListDialog = new();

    protected Encoding TargetEncoding = Encoding.UTF8;

    static TextFileListProviderUFT8()
    {
        OpenListDialog.Filter =
            "Text files|*.txt|" +
            "Text files (no validation)|*.txt|" +
            "All files|*.*";

        OpenListDialog.Multiselect = true;
    }

    /// <summary>
    /// Creates a list from pipe-delimited text-file paths.
    /// </summary>
    /// <param name="searchCriteria">
    /// Pipe-delimited paths to the files to load.
    /// </param>
    /// <returns>The articles loaded from the specified files.</returns>
    public List<Article> MakeList(string searchCriteria)
    {
        ArgumentNullException.ThrowIfNull(searchCriteria);

        return MakeList(
            searchCriteria.Split(
                '|',
                StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Opens the file-selection dialog and creates an article list from
    /// the selected files.
    /// </summary>
    /// <returns>The articles loaded from the selected files.</returns>
    public List<Article> MakeList() =>
        MakeList(Array.Empty<string>());

    /// <inheritdoc />
    public List<Article> MakeList(
        params string[] searchCriteria)
    {
        List<Article> list = new();

        try
        {
            if (searchCriteria.Length == 0 &&
                OpenListDialog.ShowDialog() == DialogResult.OK)
            {
                searchCriteria = OpenListDialog.FileNames;
            }

            foreach (string fileName in searchCriteria)
            {
                string pageText =
                    File.ReadAllText(
                        fileName,
                        TargetEncoding);

                switch (OpenListDialog.FilterIndex)
                {
                    case 2:
                        AddUnvalidatedLines(
                            list,
                            pageText);
                        break;

                    default:
                        AddValidatedTitles(
                            list,
                            pageText);
                        break;
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            // Preserve the existing behavior of reporting the error and
            // returning any articles that were loaded successfully.
            ErrorHandler.HandleException(ex);
            return list;
        }
    }

    private static void AddUnvalidatedLines(
        List<Article> list,
        string pageText)
    {
        IEnumerable<Article> articles =
            pageText
                .Split(
                    new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                    new Article(
                        Tools.RemoveSyntax(
                            Tools.TurnFirstToUpper(
                                line.Trim()))));

        list.AddRange(articles);
    }

    private static void AddValidatedTitles(
        List<Article> list,
        string pageText)
    {
        if (LoadWikiLink.IsMatch(pageText))
        {
            IEnumerable<Article> linkedArticles =
                LoadWikiLink
                    .Matches(pageText)
                    .Cast<Match>()
                    .Select(match =>
                        match.Groups[1].Value)
                    .Where(title =>
                        !RegexFromFile.IsMatch(title) &&
                        !title.StartsWith(
                            "#",
                            StringComparison.Ordinal))
                    .Select(title =>
                        new Article(
                            Tools.RemoveSyntax(
                                Tools.TurnFirstToUpper(title))));

            list.AddRange(linkedArticles);
            return;
        }

        IEnumerable<Article> lineArticles =
            pageText
                .Split(
                    new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line =>
                    line.Length > 0 &&
                    Tools.IsValidTitle(line))
                .Select(line =>
                    new Article(
                        Tools.RemoveSyntax(
                            Tools.TurnFirstToUpper(line))));

        list.AddRange(lineArticles);
    }

    /// <inheritdoc />
    public virtual string DisplayText => "Text file (UTF-8)";

    /// <inheritdoc />
    public string UserInputTextBoxText => string.Empty;

    /// <inheritdoc />
    public bool UserInputTextBoxEnabled => false;

    /// <inheritdoc />
    public void Selected()
    {
    }

    /// <inheritdoc />
    public bool RunOnSeparateThread => false;

    /// <inheritdoc />
    public virtual bool StripUrl => false;
}

/// <summary>
/// Retrieves article titles from one or more Windows-1252 encoded text files.
/// </summary>
public class TextFileListProviderWindows1252
    : TextFileListProviderUFT8
{
    public TextFileListProviderWindows1252()
    {
        TargetEncoding =
            Encoding.GetEncoding("windows-1252");
    }

    /// <inheritdoc />
    public override string DisplayText =>
        "Text file (Windows 1252 / ANSI)";
}