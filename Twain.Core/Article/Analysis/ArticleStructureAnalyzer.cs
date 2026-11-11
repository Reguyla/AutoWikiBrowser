namespace Twain.Core.ArticleAnalysis;

/// <summary>
/// Provides read-only analysis of MediaWiki article sections.
/// </summary>
public static class ArticleStructureAnalyzer
{
    /// <summary>
    /// Analyzes an article and identifies its lead and named sections
    /// without modifying the original wikitext.
    /// </summary>
    /// <param name="articleText">The original article wikitext.</param>
    /// <returns>The article's structural representation.</returns>
    public static ArticleStructure Analyze(
        string articleText,
        ArticleAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(articleText);

        options ??= new ArticleAnalysisOptions();

        List<Match> headings = WikiRegexes.Headings
            .Matches(articleText)
            .Cast<Match>()
            .Where(match =>
            {
                int level = GetHeadingLevel(match.Value);

                return level >= 1 && level <= 6;
            })
            .ToList();

        List<ArticleSection> sections = new();

        int leadLength = headings.Count > 0
            ? headings[0].Index
            : articleText.Length;

        ArticleSection lead = new(
            title: string.Empty,
            level: 0,
            startIndex: 0,
            length: leadLength,
            heading: string.Empty,
            content: articleText.Substring(0, leadLength));

        for (int i = 0; i < headings.Count; i++)
        {
            Match headingMatch = headings[i];

            int startIndex = headingMatch.Index;

            int endIndex = i + 1 < headings.Count
                ? headings[i + 1].Index
                : articleText.Length;

            string heading = headingMatch.Value;

            string sectionText = articleText.Substring(
                startIndex,
                endIndex - startIndex);

            // Include the complete line ending in the heading rather than
            // leaving part of it at the beginning of the section content.
            int contentStart = heading.Length;

            if (contentStart < sectionText.Length)
            {
                if (sectionText[contentStart] == '\r')
                {
                    contentStart++;

                    if (contentStart < sectionText.Length &&
                        sectionText[contentStart] == '\n')
                    {
                        contentStart++;
                    }
                }
                else if (sectionText[contentStart] == '\n')
                {
                    contentStart++;
                }
            }

            heading = sectionText.Substring(0, contentStart);
            string content = sectionText.Substring(contentStart);

            string title = ExtractHeadingTitle(heading);

            int level = GetHeadingLevel(heading);

            sections.Add(new ArticleSection(
                title,
                level,
                startIndex,
                sectionText.Length,
                heading,
                content));
        }

        List<ArticleMetadata> trailingMetadata =
            FindTrailingMetadata(articleText, options);

        if (trailingMetadata.Count > 0)
        {
            ArticleSection lastSection = sections.Count > 0
                ? sections[^1]
                : lead;

            int metadataStart = trailingMetadata[0].StartIndex;

            int contentStart = lastSection.StartIndex + lastSection.Heading.Length;

            if (metadataStart >= contentStart &&
                metadataStart <= lastSection.EndIndex)
            {
                int logicalEnd = metadataStart;

                // Exclude line breaks separating the section content
                // from the trailing metadata region.
                while (logicalEnd > contentStart &&
                       (articleText[logicalEnd - 1] == '\r' ||
                        articleText[logicalEnd - 1] == '\n'))
                {
                    logicalEnd--;
                }

                lastSection.LogicalContent = articleText.Substring(
                    contentStart,
                    logicalEnd - contentStart);
            }
        }

        return new ArticleStructure(
            articleText,
            lead,
            sections,
            trailingMetadata);
    }

    /// <summary>
    /// Extracts the displayed title from a MediaWiki section heading.
    /// </summary>
    private static string ExtractHeadingTitle(string heading)
    {
        return heading.Trim()
            .Trim('=')
            .Trim();
    }

    /// <summary>
    /// Determines the heading level from its opening equals signs.
    /// </summary>
    private static int GetHeadingLevel(string heading)
    {
        int level = 0;

        foreach (char character in heading)
        {
            if (character != '=')
                break;

            level++;
        }

        return level;
    }

    /// <summary>
    /// Identifies recognized standalone metadata declarations at the end
    /// of an article without modifying its original wikitext.
    /// </summary>
    private static List<ArticleMetadata> FindTrailingMetadata(
        string articleText,
        ArticleAnalysisOptions options)
    {
        List<ArticleMetadata> metadata = new();

        int position = articleText.Length;

        while (position > 0)
        {
            // Skip trailing whitespace and blank lines.
            while (position > 0 &&
                   char.IsWhiteSpace(articleText[position - 1]))
            {
                position--;
            }

            if (position == 0)
                break;

            int lineStart = articleText.LastIndexOf(
                '\n',
                position - 1) + 1;

            string line = articleText.Substring(
                lineStart,
                position - lineStart);

            string trimmedLine = line.Trim();

            bool isCategory = IsCompleteMatch(
                WikiRegexes.Category,
                trimmedLine);

            bool isDefaultSort = IsCompleteMatch(
                WikiRegexes.Defaultsort,
                trimmedLine);

            bool isConfiguredTemplate =
                IsConfiguredMetadataTemplate(trimmedLine, options);

            bool isInterwiki =
                IsConfiguredInterwikiLink(trimmedLine, options);

            if (!isCategory &&
                !isDefaultSort &&
                !isConfiguredTemplate &&
                !isInterwiki)
            {
                break;
            }

            int declarationStart =
                lineStart + line.IndexOf(
                    trimmedLine,
                    StringComparison.Ordinal);

            ArticleMetadataType metadataType;

            if (isCategory)
            {
                metadataType = ArticleMetadataType.Category;
            }
            else if (isDefaultSort)
            {
                metadataType = ArticleMetadataType.DefaultSort;
            }
            else if (isConfiguredTemplate)
            {
                metadataType = ArticleMetadataType.Template;
            }
            else
            {
                metadataType = ArticleMetadataType.InterwikiLink;
            }

            metadata.Add(new ArticleMetadata(
                metadataType,
                trimmedLine,
                declarationStart,
                trimmedLine.Length));

            position = lineStart;
        }

        metadata.Reverse();

        return metadata;
    }

    /// <summary>
    /// Determines whether a regular expression matches an entire string.
    /// </summary>
    /// <param name="expression">The regular expression to evaluate.</param>
    /// <param name="text">The text to evaluate.</param>
    /// <returns>
    /// True when the expression matches the entire text; otherwise, false.
    /// </returns>
    private static bool IsCompleteMatch(Regex expression, string text)
    {
        Match match = expression.Match(text);

        return match.Success &&
               match.Index == 0 &&
               match.Length == text.Length;
    }

    /// <summary>
    /// Determines whether a standalone template declaration is configured
    /// as trailing article metadata.
    /// </summary>
    private static bool IsConfiguredMetadataTemplate(
        string text,
        ArticleAnalysisOptions options)
    {
        if (options.TrailingMetadataTemplates.Count == 0)
            return false;

        if (!text.StartsWith("{{", StringComparison.Ordinal) ||
            !text.EndsWith("}}", StringComparison.Ordinal))
        {
            return false;
        }

        string templateBody = text.Substring(2, text.Length - 4);

        // Reject nested templates for now. These require a proper
        // template parser rather than simple string matching.
        if (templateBody.Contains("{{", StringComparison.Ordinal) ||
            templateBody.Contains("}}", StringComparison.Ordinal))
        {
            return false;
        }

        int parameterSeparator = templateBody.IndexOf('|');

        string templateName = parameterSeparator >= 0
            ? templateBody.Substring(0, parameterSeparator)
            : templateBody;

        templateName = templateName.Trim();

        // MediaWiki treats spaces and underscores equivalently
        // in template titles.
        templateName = templateName.Replace('_', ' ');

        foreach (string configuredName in options.TrailingMetadataTemplates)
        {
            string normalizedName = configuredName
                .Trim()
                .Replace('_', ' ');

            if (string.Equals(
                templateName,
                normalizedName,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a standalone interwiki link is configured
    /// as trailing article metadata.
    /// </summary>
    private static bool IsConfiguredInterwikiLink(
        string text,
        ArticleAnalysisOptions options)
    {
        if (options.TrailingInterwikiPrefixes.Count == 0)
            return false;

        // Interwiki declarations use ordinary wiki-link syntax.
        // A leading colon indicates a visible link instead.
        if (!text.StartsWith("[[", StringComparison.Ordinal) ||
            !text.EndsWith("]]", StringComparison.Ordinal) ||
            text.StartsWith("[[:", StringComparison.Ordinal))
        {
            return false;
        }

        string linkBody = text.Substring(2, text.Length - 4);

        // Reject multiple or nested links.
        if (linkBody.Contains("[[", StringComparison.Ordinal) ||
            linkBody.Contains("]]", StringComparison.Ordinal))
        {
            return false;
        }

        int colonIndex = linkBody.IndexOf(':');

        if (colonIndex <= 0)
            return false;

        string prefix = linkBody.Substring(0, colonIndex);

        // Only recognize explicitly configured prefixes.
        return options.TrailingInterwikiPrefixes.Contains(prefix);
    }

    /// <summary>
    /// Normalizes a category declaration missing its final closing bracket
    /// so section comparisons can recognize the surrounding metadata block.
    /// </summary>
    private static string NormalizeMalformedCategories(string articleText)
    {
        return Regex.Replace(
            articleText,
            @"(?m)^([ \t]*\[\[\s*Category\s*:[^\[\]\r\n]*\])(?=\r?$)",
            "$1]",
            RegexOptions.IgnoreCase);
    }
}