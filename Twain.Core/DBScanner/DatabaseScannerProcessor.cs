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
}