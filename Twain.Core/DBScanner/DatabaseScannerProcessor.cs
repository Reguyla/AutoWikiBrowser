using System.Xml;

namespace Twain.Core.DBScanner;

/// <summary>
/// Creates the database scan pipeline from scanner options.
/// </summary>
public static class DatabaseScannerProcessor
{
    /// <summary>
    /// Creates the scanners required for the supplied database scanner options.
    /// </summary>
    /// <param name="options">
    /// The configured database scanner options.
    /// </param>
    /// <returns>
    /// The scanners to apply to each article in the database dump.
    /// </returns>
    public static List<Scan> CreateScanners(DatabaseScannerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<Scan> scanners = [];

        Regex titleContainsRegex = CreateTitleRegex(
            options.TitleContains,
            options.TitleRegex,
            options.TitleCaseSensitive);

        Regex titleDoesNotContainRegex = CreateTitleRegex(
            options.TitleDoesNotContain,
            options.TitleRegex,
            options.TitleCaseSensitive);

        Regex articleContainsRegex = CreateArticleRegex(
            options.ArticleContains,
            options.ArticleRegex,
            options.ArticleCaseSensitive,
            options.ArticleRegexMultiline,
            options.ArticleRegexSingleline);

        Regex articleDoesNotContainRegex = CreateArticleRegex(
            options.ArticleDoesNotContain,
            options.ArticleRegex,
            options.ArticleCaseSensitive,
            options.ArticleRegexMultiline,
            options.ArticleRegexSingleline);

        // First set of checks: namespace and title.
        if (options.Namespaces.Count > 0)
            scanners.Add(new CheckNamespace(options.Namespaces));

        if (options.TitleContainsEnabled)
            scanners.Add(new TitleContains(titleContainsRegex));

        if (options.TitleDoesNotContainEnabled)
            scanners.Add(new TitleDoesNotContain(titleDoesNotContainRegex));

        // Second set of checks: redirects and article text.
        if (options.IgnoreRedirects)
            scanners.Add(new IsNotRedirect());

        if (options.ArticleContainsEnabled)
            scanners.Add(new TextContainsRegex(articleContainsRegex));

        if (options.ArticleDoesNotContainEnabled)
            scanners.Add(new TextDoesNotContainRegex(articleDoesNotContainRegex));

        if (options.SearchDates)
            scanners.Add(new DateRange(options.DateFrom, options.DateTo));

        if (options.CheckProtection)
        {
            scanners.Add(
                new Restriction(
                    options.EditProtectionLevel,
                    options.MoveProtectionLevel));
        }

        switch (options.LengthComparison)
        {
            case 1:
                scanners.Add(
                    new CountCharacters(
                        MoreLessThan.MoreThan,
                        options.Length));
                break;

            case 2:
                scanners.Add(
                    new CountCharacters(
                        MoreLessThan.LessThan,
                        options.Length));
                break;
        }

        switch (options.LinkComparison)
        {
            case 1:
                scanners.Add(
                    new CountLinks(
                        MoreLessThan.MoreThan,
                        options.Links));
                break;

            case 2:
                scanners.Add(
                    new CountLinks(
                        MoreLessThan.LessThan,
                        options.Links));
                break;
        }

        switch (options.WordComparison)
        {
            case 1:
                scanners.Add(
                    new CountWords(
                        MoreLessThan.MoreThan,
                        options.Words));
                break;

            case 2:
                scanners.Add(
                    new CountWords(
                        MoreLessThan.LessThan,
                        options.Words));
                break;
        }

        if (options.CheckBadLinks)
            scanners.Add(new HasBadLinks());

        if (options.CheckNoBoldTitle)
            scanners.Add(new HasNoBoldTitle());

        if (options.CheckCiteTemplateDates)
            scanners.Add(new CiteTemplateDates());

        if (options.CheckReorderReferences)
            scanners.Add(new ReorderReferences());

        if (options.CheckPeopleCategories)
            scanners.Add(new PeopleCategories());

        if (options.CheckUnbalancedBrackets)
            scanners.Add(new UnbalancedBrackets());

        if (options.CheckSimpleLinks)
            scanners.Add(new HasSimpleLinks());

        if (options.CheckHtmlEntities)
            scanners.Add(new HasHTMLEntities());

        if (options.CheckSectionErrors)
            scanners.Add(new HasSectionError());

        if (options.CheckUnbulletedLinks)
            scanners.Add(new HasUnbulletedLinks());

        if (options.CheckTypos)
            scanners.Add(new Typo());

        if (options.CheckMissingDefaultSort)
            scanners.Add(new MissingDefaultsort());

        return scanners;
    }

    private static Regex CreateTitleRegex(
        string pattern,
        bool isRegex,
        bool caseSensitive)
    {
        pattern = ConvertLineEndings(pattern);

        if (!isRegex)
            pattern = Regex.Escape(pattern);

        RegexOptions options = RegexOptions.Compiled;

        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;

        return new Regex(pattern, options);
    }

    private static Regex CreateArticleRegex(
        string pattern,
        bool isRegex,
        bool caseSensitive,
        bool multiline,
        bool singleline)
    {
        pattern = ConvertLineEndings(pattern);

        RegexOptions options = RegexOptions.Compiled;

        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;

        if (!isRegex)
        {
            pattern = Regex.Escape(pattern);
        }
        else
        {
            if (multiline)
                options |= RegexOptions.Multiline;

            if (singleline)
                options |= RegexOptions.Singleline;
        }

        return new Regex(pattern, options);
    }

    private static string ConvertLineEndings(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    /// <summary>
    /// Creates a wiki-formatted list from database scanner results.
    /// </summary>
    /// <param name="articles">
    /// The articles to include in the list.
    /// </param>
    /// <param name="useNumberedList">
    /// Whether to use numbered list items instead of bullet points.
    /// </param>
    /// <param name="useNumberedHeadings">
    /// Whether to divide the results into numbered sections.
    /// </param>
    /// <param name="articlesPerSection">
    /// The number of articles to include in each numbered section.
    /// </param>
    /// <param name="useAlphabeticalHeadings">
    /// Whether to divide the results using alphabetical headings.
    /// </param>
    /// <returns>
    /// The wiki-formatted article list.
    /// </returns>
    public static string CreateWikiList(
        IEnumerable<Article> articles,
        bool useNumberedList,
        bool useNumberedHeadings,
        int articlesPerSection,
        bool useAlphabeticalHeadings)
    {
        ArgumentNullException.ThrowIfNull(articles);

        List<Article> articleList = articles.ToList();

        StringBuilder result = new();
        string bullet = useNumberedList ? "#" : "*";

        if (useNumberedHeadings)
        {
            int sectionCount = 0;
            int sectionNumber = 0;
            int articleCount = 0;

            result.AppendLine("==0==");
            sectionNumber++;

            foreach (Article article in articleList)
            {
                articleCount++;

                string title = article
                    .ToString()
                    .Replace("&amp;", "&");

                if (article.NameSpaceKey == Namespace.File)
                    title = ":" + title;

                result.AppendLine($"{bullet} [[{title}]]");

                sectionCount++;

                if (sectionCount == articlesPerSection &&
                    articleCount != articleList.Count)
                {
                    result.AppendLine($"\r\n=={sectionNumber}==");

                    sectionNumber++;
                    sectionCount = 0;
                }
            }
        }
        else if (useAlphabeticalHeadings)
        {
            string previousLetter = string.Empty;

            foreach (Article article in articleList)
            {
                string title = article
                    .ToString()
                    .Replace("&amp;", "&");

                string currentLetter =
                    title.Length > 1
                        ? title.Remove(1)
                        : title;

                currentLetter =
                    Tools.RemoveDiacritics(currentLetter);

                if (currentLetter != previousLetter)
                    result.AppendLine($"\r\n== {currentLetter} ==");

                result.AppendLine($"{bullet} [[{title}]]");

                previousLetter = currentLetter;
            }
        }
        else
        {
            foreach (Article article in articleList)
            {
                string title = article
                    .ToString()
                    .Replace("&amp;", "&");

                result.AppendLine($"{bullet} [[{title}]]");
            }
        }

        return result.ToString().Trim();
    }

    /// <summary>
    /// Reads the site metadata from a MediaWiki XML database dump.
    /// </summary>
    /// <param name="fileName">
    /// The path to the database dump.
    /// </param>
    /// <returns>
    /// The metadata stored in the dump header.
    /// </returns>
    public static DatabaseDumpMetadata ReadDumpMetadata(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        DatabaseDumpMetadata metadata = new();

        using XmlTextReader reader = new(fileName);

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "sitename":
                    metadata.SiteName = reader.ReadString();
                    break;

                case "base":
                    metadata.BaseUrl = reader.ReadString();
                    break;

                case "generator":
                    metadata.Generator = reader.ReadString();
                    break;

                case "case":
                    metadata.Case = reader.ReadString();
                    return metadata;
            }
        }

        return metadata;
    }

    /// <summary>
    /// Calculates the overall progress of a database scan.
    /// </summary>
    /// <param name="matches">
    /// The number of matching articles found so far.
    /// </param>
    /// <param name="resultLimit">
    /// The maximum number of matching articles requested.
    /// </param>
    /// <param name="scanCompletion">
    /// The fraction of the database dump that has been scanned, ranging from 0 to 1.
    /// </param>
    /// <returns>
    /// The overall scan progress as a fraction ranging from 0 to 1.
    /// </returns>
    public static double CalculateProgress(
        int matches,
        int resultLimit,
        double scanCompletion)
    {
        double matchesByLimit =
            resultLimit > 0
                ? (double)matches / resultLimit
                : 0;

        return Math.Min(
            1,
            Math.Max(matchesByLimit, scanCompletion));
    }
}