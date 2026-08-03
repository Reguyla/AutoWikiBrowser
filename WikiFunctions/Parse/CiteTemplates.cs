/*

Copyright (C) 2007 Martin Richards

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

namespace WikiFunctions.Parse;

/// <summary>
/// Provides functions for editing wiki text, such as formatting and re-categorization.
/// </summary>
public partial class Parsers
{
    #region FixCitationTemplates
    // TODO: Review regex field naming for consistency during the planned
    // CiteTemplates cleanup. Members such as rpTemplate currently use legacy
    // naming conventions.
    //
    /// <summary>
    /// Matches the value of a citation template's <c>url</c> parameter when the
    /// value is an unquoted, whitespace-free URL.
    /// </summary>
    /// <remarks>
    /// The capture group contains the URL value. Values containing square
    /// brackets, angle brackets, quotation marks, or whitespace are excluded.
    /// This is used to avoid modifying text inside malformed URL parameters.
    /// </remarks>
    private static readonly Regex CiteUrl =
        new(
            @"\|\s*url\s*=\s*([^\[\]<>""\s]+)",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches a citation template's <c>work</c> parameter when its entire value
    /// is enclosed in wiki italics.
    /// </summary>
    /// <remarks>
    /// Group 1 contains the parameter name and assignment text. Group 2 contains
    /// the work value without the surrounding apostrophes. Values containing
    /// apostrophes, braces, or template pipes are deliberately excluded.
    /// </remarks>
    private static readonly Regex WorkInItalics =
        new(
            @"(\|\s*work\s*=\s*)''([^'{}\|]+)''(?=\s*(?:\||}}))",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches redundant page prefixes such as <c>p.</c>, <c>pp.</c>,
    /// <c>pg</c>, and <c>pgs</c> at the start of a citation
    /// <c>page</c> or <c>pages</c> parameter value.
    /// </summary>
    /// <remarks>
    /// Optional whitespace or <c>&amp;nbsp;</c> following the prefix is included
    /// in the match. The lookarounds ensure that only the parameter value is
    /// modified and that the value remains inside the citation template.
    /// </remarks>
    private static readonly Regex CiteTemplatePagesPP =
        new(
            @"(?<=\|\s*pages?\s*=\s*)p(?:p|gs?)?(?:\.|\b)(?:&nbsp;|\s*)(?=[^{}\|]+(?:\||}}))",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches a redundant volume label at the start of a citation journal's
    /// <c>volume</c> parameter.
    /// </summary>
    /// <remarks>
    /// Recognizes forms such as <c>vol</c>, <c>vol.</c>, <c>volume</c>, and
    /// <c>volumes</c>, optionally followed by a colon or <c>&amp;nbsp;</c>.
    /// Matching is case-insensitive.
    /// </remarks>
    private static readonly Regex CiteTemplatesJournalVolume =
        new(
            @"(?<=\|\s*volume\s*=\s*)vol(?:umes?|\.)?(?:&nbsp;|:)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches a redundant issue or number label embedded at the end of a citation
    /// journal's <c>volume</c> parameter.
    /// </summary>
    /// <remarks>
    /// The lookbehind requires a numeric or Roman-numeral volume value. The match
    /// includes labels such as <c>no.</c>, <c>nos.</c>, <c>number</c>,
    /// <c>issue</c>, and <c>iss</c>, together with their preceding separator.
    /// Matching is case-insensitive.
    /// </remarks>
    private static readonly Regex CiteTemplatesJournalVolumeAndIssue =
        new(
            @"(?<=\|\s*volume\s*=\s*[0-9VXMILC]+?)(?:[;,]?\s*(?:nos?[\.:; ]|(?:numbers?|issues?|iss)\s*[:; ]))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches a redundant issue or number label at the start of a citation
    /// journal's <c>issue</c> parameter.
    /// </summary>
    /// <remarks>
    /// Recognizes labels such as <c>issue</c>, <c>no.</c>, <c>iss.</c>, and
    /// <c>number</c>, optionally followed by punctuation or <c>&amp;nbsp;</c>.
    /// Matching is case-insensitive.
    /// </remarks>
    private static readonly Regex CiteTemplatesJournalIssue =
        new(
            @"(?<=\|\s*issue\s*=\s*)(?:issues?|(?:nos?|iss)(?:[\.,;:]|\b)|numbers?[\.,;:]?)(?:&nbsp;)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches a citation <c>page</c> parameter whose value appears to contain
    /// a page range or page list.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the parameter delimiter and surrounding whitespace.
    /// Group 2 captures the parameter name, assignment, and numeric value.
    /// Recognized separators are an en dash or a comma followed by a space.
    /// </remarks>
    private static readonly Regex CiteTemplatesPageRangeName =
        new(
            @"(\|\s*)page(\s*=\s*[0-9]+\s*(?:–|, )\s*[0-9])",
            RegexOptions.Compiled);

    // TODO: Review AccessDateYear year-range matching. The current pattern only
    // recognizes years through 2029 and may require extension for future dates.
    /// <summary>
    /// Matches a separate <c>accessyear</c> parameter following an access date
    /// that contains a day and named month.
    /// </summary>
    /// <remarks>
    /// Group 1 captures spacing before the <c>accessyear</c> parameter, group 2
    /// captures the year, and group 3 captures the following template delimiter.
    /// The year pattern currently recognizes years from 2000 through 2029.
    /// </remarks>
    private static readonly Regex AccessDateYear =
        new(
            @"(?<=\|\s*access\-?date\s*=\s*(?:[1-3]?\d\s+" +
            WikiRegexes.MonthsNoGroup +
            @"|\s*" +
            WikiRegexes.MonthsNoGroup +
            @"\s+[1-3]?\d))(\s*)\|\s*accessyear\s*=\s*(20[012]\d)\s*(\||}})",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches an empty legacy access-date component parameter.
    /// </summary>
    /// <remarks>
    /// Recognizes empty <c>accessdaymonth</c>, <c>accessmonth</c>,
    /// <c>accessmonthday</c>, and <c>accessyear</c> parameters.
    /// </remarks>
    private static readonly Regex AccessDayMonthDay =
        new(
            @"\|\s*access(?:daymonth|month(?:day)?|year)\s*=\s*(?=\||}})",
            RegexOptions.Compiled);

    // TODO: Review DateLeadingZero year-range matching. The current expression
    // limits recognized years to 1800–2029 and should be evaluated for long-term
    // maintainability.
    /// <summary>
    /// Matches a leading zero in the day portion of a citation date parameter.
    /// </summary>
    /// <remarks>
    /// Applies to <c>date</c>, <c>accessdate</c>, <c>access-date</c>,
    /// <c>archivedate</c>, and <c>archive-date</c> parameters using recognized
    /// named-month date formats. The optional year pattern currently supports
    /// years from 1800 through 2029.
    /// </remarks>
    private static readonly Regex DateLeadingZero =
        new(
            @"(?<=\|\s*(?:access|archive)?\-?date\s*=\s*)(?:0([1-9]\s+[A-Z][a-z]{2,})|(\s*[A-Z][a-z]{2,}\s)+0([1-9],?))(\s+(?:20[012]|1[89]\d)\d)?(\s*(?:\||}}))",
            RegexOptions.Compiled);

    // TODO: Evaluate LangTemplate support for modern language identifiers.
    // The current pattern only recognizes two-character language codes.
    /// <summary>
    /// Matches a two-letter language icon template used as the complete value of
    /// a citation <c>language</c> parameter.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the parameter assignment, group 2 captures the complete
    /// icon template, and group 3 captures the two-letter language code.
    /// </remarks>
    private static readonly Regex LangTemplate =
        new(
            @"(\|\s*language\s*=\s*)({{(\w{2}) icon}}\s*)(?=\||}})",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches a comma-separated numeric page range or page list that lacks
    /// whitespace after the comma.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the first page number and its preceding boundary.
    /// Group 2 captures the following page number and its trailing boundary.
    /// </remarks>
    private static readonly Regex UnspacedCommaPageRange =
        new(
            @"((?:[ ,–]|^)[0-9]+),([0-9]+(?:[ ,–]|$))",
            RegexOptions.Compiled);

    // TODO: Consider replacing ParametersToDequote with a read-only collection
    // (for example, ReadOnlyCollection<string>, ImmutableArray<string>, or
    // FrozenSet<string>) since the values are constant after initialization.
    /// <summary>
    /// Identifies citation parameters whose values may have unnecessary outer
    /// quotation marks removed.
    /// </summary>
    private static readonly List<string> ParametersToDequote =
        new(new[] { "title", "trans_title" });

    /// <summary>
    /// Matches an <c>rp</c> template, including supported nested template content.
    /// </summary>
    private static readonly Regex rpTemplate =
        Tools.NestedTemplateRegex("rp");

    /// <summary>
    /// Contains characters that mark the end of a template name.
    /// </summary>
    private static readonly char[] TemplateNameEndChars =
        "}|".ToCharArray();

    /// <summary>
    /// Applies supported formatting corrections to citation-related templates.
    /// </summary>
    /// <param name="articleText">
    /// The article wikitext to process.
    /// </param>
    /// <returns>
    /// The updated article wikitext.
    /// </returns>
    /// <remarks>
    /// Citation-template cleanup is currently implemented only for English-language
    /// wikis. For other languages, the supplied article text is returned unchanged.
    /// </remarks>
    public static string FixCitationTemplates(string articleText)
    {
        if (!Variables.LangCode.Equals("en"))
            return articleText;

        List<string> allTemplates = GetAllTemplates(articleText);
        List<string> allTemplateDetails = GetAllTemplateDetail(articleText);

        articleText = FixCiteTemplateCalls(
            articleText,
            allTemplates,
            allTemplateDetails);

        articleText = FixHarvAndSfnTemplateCalls(
            articleText,
            allTemplates);

        articleText = FixRpTemplateCalls(
            articleText,
            allTemplates,
            allTemplateDetails);

        return articleText;
    }

    /// <summary>
    /// Applies citation-template formatting fixes to the detailed template calls
    /// found in an article.
    /// </summary>
    /// <param name="articleText">
    /// The complete article wikitext.
    /// </param>
    /// <param name="allTemplates">
    /// The template names found in the article.
    /// </param>
    /// <param name="allTemplateDetails">
    /// The complete template calls found in the article.
    /// </param>
    /// <returns>
    /// The article text after citation-template formatting fixes have been
    /// applied.
    /// </returns>
    private static string FixCiteTemplateCalls(
        string articleText,
        List<string> allTemplates,
        List<string> allTemplateDetails)
    {
        List<string> citeTemplatesUsed = allTemplates.FindAll(
            templateName =>
                WikiRegexes.CiteTemplate.IsMatch(
                    "{{" + templateName + "|}}"));

        if (!citeTemplatesUsed.Any())
            return articleText;

        // Filter template calls by name before applying the more expensive
        // citation-template regular expression.
        IEnumerable<string> citeTemplateCalls =
            allTemplateDetails.Where(
                templateCall =>
                    citeTemplatesUsed.Any(
                        citeTemplateName =>
                            GetTemplateNamePortion(templateCall).IndexOf(
                                citeTemplateName,
                                StringComparison.OrdinalIgnoreCase) >= 0));

        foreach (string templateCall in citeTemplateCalls)
        {
            string result = ApplyCitationFixesUntilStable(templateCall);

            if (!result.Equals(templateCall))
                articleText = articleText.Replace(templateCall, result);
        }

        return articleText;
    }

    /// <summary>
    /// Repeatedly applies citation-template corrections until an additional pass
    /// produces no further changes.
    /// </summary>
    /// <param name="templateCall">
    /// The template call to process.
    /// </param>
    /// <returns>
    /// The stabilized template call.
    /// </returns>
    private static string ApplyCitationFixesUntilStable(string templateCall)
    {
        string current = templateCall;
        string previous;

        do
        {
            previous = current;
            current = WikiRegexes.CiteTemplate.Replace(
                current,
                FixCitationTemplatesME);
        }
        while (!current.Equals(previous));

        return current;
    }

    // TODO (validation): Confirm that every value returned by
    // GetAllTemplateDetail contains at least one TemplateNameEndChars character.
    // If malformed or incomplete template calls are possible, handle an
    // IndexOfAny result of -1 instead of passing it to Substring.
    /// <summary>
    /// Gets the leading portion of a template call containing its template name.
    /// </summary>
    /// <param name="templateCall">
    /// The complete template call.
    /// </param>
    /// <returns>
    /// The portion of the template call preceding the first template-name
    /// terminator.
    /// </returns>
    private static string GetTemplateNamePortion(string templateCall)
    {
        int nameEndIndex =
            templateCall.IndexOfAny(TemplateNameEndChars);

        return templateCall.Substring(
            0,
            nameEndIndex);
    }

    /// <summary>
    /// Applies shared formatting corrections to Harvard-reference and shortened
    /// footnote templates.
    /// </summary>
    /// <param name="articleText">
    /// The complete article wikitext.
    /// </param>
    /// <param name="allTemplates">
    /// The template names found in the article.
    /// </param>
    /// <returns>
    /// The article text after supported Harvard-reference and shortened-footnote
    /// corrections have been applied.
    /// </returns>
    private static string FixHarvAndSfnTemplateCalls(
        string articleText,
        List<string> allTemplates)
    {
        if (TemplateExists(
                allTemplates,
                WikiRegexes.HarvTemplate))
        {
            articleText = WikiRegexes.HarvTemplate.Replace(
                articleText,
                FixHarvSfnTemplatesME);
        }

        if (TemplateExists(
                allTemplates,
                WikiRegexes.SfnTemplate))
        {
            articleText = WikiRegexes.SfnTemplate.Replace(
                articleText,
                FixHarvSfnTemplatesME);
        }

        return articleText;
    }

    /// <summary>
    /// Normalizes page-range parameters in a Harvard-reference or shortened
    /// footnote template.
    /// </summary>
    /// <param name="match">
    /// The matched <c>harv</c> or <c>sfn</c> template call.
    /// </param>
    /// <returns>
    /// The updated template call with normalized page ranges. When the
    /// <c>p</c> parameter contains a page range and no <c>pp</c> parameter is
    /// already present, the parameter is renamed from <c>p</c> to <c>pp</c>.
    /// </returns>
    /// <remarks>
    /// Parenthetical text in the <c>p</c> value is ignored when determining
    /// whether the value represents multiple pages.
    /// </remarks>
    private static string FixHarvSfnTemplatesME(Match match)
    {
        string updatedTemplate = FixPageRanges(
            match.Value,
            Tools.GetTemplateParameterValues(match.Value));

        string page = Tools.GetTemplateParameterValue(
            updatedTemplate,
            "p");

        // Ignore parenthetical text when determining whether the value
        // represents a page range.
        int openingParenthesisIndex = page.IndexOf(
            '(',
            StringComparison.Ordinal);

        if (openingParenthesisIndex >= 0)
        {
            page = page.Substring(
                0,
                openingParenthesisIndex);
        }

        // TODO (maintainability): Consider extracting the page-range detection
        // expression into a named, shared regex if the same range syntax is checked
        // elsewhere. Preserve the current pattern until all related citation and page
        // range processing has been compared.
        bool containsPageRange =
            Regex.IsMatch(
                page,
                @"\d+\s*(?:–|&ndash;|, )\s*\d");

        bool hasPluralPageParameter =
            Tools.GetTemplateParameterValue(
                updatedTemplate,
                "pp").Length > 0;

        if (containsPageRange &&
            !hasPluralPageParameter)
        {
            updatedTemplate = Tools.RenameTemplateParameter(
                updatedTemplate,
                "p",
                "pp");
        }

        return updatedTemplate;
    }

    /// <summary>
    /// Normalizes page ranges in <c>rp</c> templates.
    /// </summary>
    /// <param name="articleText">
    /// The complete article wikitext.
    /// </param>
    /// <param name="allTemplates">
    /// The template names found in the article.
    /// </param>
    /// <param name="allTemplateDetails">
    /// The complete template calls found in the article.
    /// </param>
    /// <returns>
    /// The article text after page-range corrections have been applied.
    /// </returns>
    private static string FixRpTemplateCalls(
        string articleText,
        List<string> allTemplates,
        List<string> allTemplateDetails)
    {
        if (!TemplateExists(allTemplates, rpTemplate))
            return articleText;

        // Use the cached template details instead of scanning the entire article
        // with the rp-template expression.
        List<string> rpTemplates = allTemplateDetails.FindAll(
            templateCall =>
                GetTemplateNamePortion(templateCall).IndexOf(
                    "rp",
                    StringComparison.OrdinalIgnoreCase) >= 0);

        foreach (string templateCall in rpTemplates)
        {
            string result = rpTemplate.Replace(
                templateCall,
                FixRpTemplatePageRange);

            if (!result.Equals(templateCall))
                articleText = articleText.Replace(templateCall, result);
        }

        return articleText;
    }

    /// <summary>
    /// Normalizes a page range in an <c>rp</c> template match.
    /// </summary>
    /// <param name="match">
    /// The matched <c>rp</c> template.
    /// </param>
    /// <returns>
    /// The template with its page range normalized, or the original matched text
    /// when no page range is present.
    /// </returns>
    private static string FixRpTemplatePageRange(Match match)
    {
        // rp templates may use either an unnamed page range, such as
        // {{rp|1-7}}, or a named page-range parameter, such as {{rp|pp=1-7}}.
        Dictionary<string, string> parameters =
            Tools.GetTemplateParameterValues(match.Value);

        if (parameters.Any())
            return FixPageRanges(match.Value, parameters);

        string pageRange =
            Tools.GetTemplateArgument(match.Value, 1);

        if (pageRange.Length > 0)
        {
            return match.Value.Replace(
                pageRange,
                FixPageRangesValue(pageRange));
        }

        return match.Value;
    }

    // TODO: Review identifier validation. The current ISBN, ASIN, and ISSN
    // patterns recognize formatting but do not validate identifier checksums.
    //
    /// <summary>
    /// Matches an <c>id</c> parameter containing an ISBN identifier.
    /// </summary>
    /// <remarks>
    /// Captures the ISBN value without the leading <c>ISBN</c> label. Optional
    /// whitespace and <c>:</c> or <c>=</c> separators are permitted.
    /// </remarks>
    private static readonly Regex IdISBN =
        new(
            @"^ISBN ?[:=]?\s*([\d \-]+X?)$",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches an <c>id</c> parameter containing an ASIN identifier.
    /// </summary>
    /// <remarks>
    /// Captures the ASIN value without the leading <c>ASIN</c> label. Optional
    /// whitespace and <c>:</c> or <c>=</c> separators are permitted.
    /// </remarks>
    private static readonly Regex IdASIN =
        new(
            @"^ASIN ?[:=]?\s*([\d \-]+X?)$",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches an <c>id</c> parameter containing an ISSN identifier.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the first four digits and group 2 captures the final
    /// four-character component, allowing either a digit or <c>X</c> as the
    /// checksum character.
    /// </remarks>
    private static readonly Regex IdISSN =
        new(
            @"^ISSN ?[:=]?\s*([0-9]{4}) *[- –]? *([0-9]{3}[0-9X])$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches a four-digit year with no surrounding text.
    /// </summary>
    private static readonly Regex YearOnly =
        new(
            @"^[12]\d{3}$",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches an en dash separating adjacent ISBN components.
    /// </summary>
    /// <remarks>
    /// Used to normalize ISBN separators to standard hyphens.
    /// </remarks>
    private static readonly Regex ISBNDash =
        new(
            @"(\d)[–](\d|X$)",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches text enclosed by single angle quotation marks.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the enclosed text without the quotation marks.
    /// </remarks>
    private static readonly Regex BalancedArrows =
        new(
            @"(?:‹([^›]+)›)",
            RegexOptions.Compiled);

    // TODO: Review ArchiveOrgURL support for additional archive services and URL
    // formats (for example, archive.ph and future Internet Archive variants).
    /// <summary>
    /// Matches supported Internet Archive snapshot URLs.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the eight-digit archive date (YYYYMMDD).
    /// Recognizes both <c>web.archive.org</c> and
    /// <c>archive.today</c> URL formats.
    /// </remarks>
    private static readonly Regex ArchiveOrgURL =
        new(
            @"^https?://(?:web\.archive\.org|archive\.today)/(?:web/)?(\d{8})(?:\d{6}/)",
            RegexOptions.Compiled);

    /// <summary>
    /// Performs fixes to a given citation template call
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private static string FixCitationTemplatesME(Match m)
    {
        string newValue = Tools.RemoveExcessTemplatePipes(m.Value);
        string templatename = m.Groups[2].Value;

        Dictionary<string, string> paramsFound = new();
        // remove duplicated fields, ensure the URL is not touched (may have pipes in)
        newValue = Tools.RemoveDuplicateTemplateParameters(newValue, paramsFound);

        // fix cite params not in lower case, allowing for ISBN, DOI identifiers being uppercase, avoiding changing text within malformed URL
        newValue = NormalizeCitationParameterNames(
            newValue,
            paramsFound);

        CitationParameterValues parameterValues =
            GetCitationParameterValues(paramsFound);

        string theURL = parameterValues.Url;
        string id = parameterValues.Id;
        string format = parameterValues.Format;
        string theTitle = parameterValues.Title;
        string TheYear = parameterValues.Year;
        string lang = parameterValues.Language;
        string TheDate = parameterValues.Date;
        string TheMonth = parameterValues.Month;
        string TheWork = parameterValues.Work;
        string nopp = parameterValues.NoPagePrefix;
        string TheIssue = parameterValues.Issue;
        string TheVolume = parameterValues.Volume;
        string accessdate = parameterValues.AccessDate;
        string pages = parameterValues.Pages;
        string page = parameterValues.Page;
        string ISBN = parameterValues.Isbn;
        string ISSN = parameterValues.Issn;
        string origyear = parameterValues.OriginalYear;
        string origdate = parameterValues.OriginalDate;
        string archiveurl = parameterValues.ArchiveUrl;
        string contributionurl = parameterValues.ContributionUrl;
        string website = parameterValues.Website;

        string theURLoriginal = theURL;

        // remove the unneeded 'format=HTML' field
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Remove_.22format.3DHTML.22_in_citation_templates
        // remove format= field with null value when URL is HTML page
        if (paramsFound.ContainsKey("format"))
        {
            if (format.TrimStart("[]".ToCharArray()).ToUpper().StartsWith("HTM")
                ||
                (format.Length == 0 &&
                 theURL.ToUpper().TrimEnd('L').EndsWith("HTM")))
                newValue = Tools.RemoveTemplateParameter(newValue, "format");
        }

        // Remove Empty Citation Original Date
        newValue = RemoveEmptyCitationOriginalDate(newValue, paramsFound, origdate);

        // newlines to spaces in all parameters
        newValue = NormalizeCitationParameterNewlines(newValue, paramsFound);

        // {{sv icon}} -> sv in language=
        newValue = NormalizeCitationLanguageTemplate(newValue, lang);

        if (lang.Contains("{{"))
            lang = Tools.GetTemplateParameterValue(newValue, "language");

        // remove italics for work field for book/periodical, but not website -- auto italicized by template
        newValue = RemoveRedundantCitationWorkItalics(newValue, TheWork);

        // page= and pages= fields don't need p. or pp. in them when nopp not set
        if ((pages.Contains("p") || page.Contains("p")) &&
            !templatename.Equals("cite journal", StringComparison.OrdinalIgnoreCase) && nopp.Length == 0)
        {
            newValue = CiteTemplatePagesPP.Replace(newValue, "");
            pages = Tools.GetTemplateParameterValue(newValue, "pages");
            paramsFound.Remove("pages");
            paramsFound.Add("pages", pages);
        }

        // with Lua no need to rename date to year when date = YYYY, just remove year and date duplicating each other
        if (TheDate.Length == 4 && TheYear.Equals(TheDate))
            newValue = Tools.RemoveTemplateParameter(newValue, "date");

        // year = full date --> date = full date
        if (TheYear.Length > 5)
        {
            newValue = MoveFullDateFromCitationYear(newValue, ref TheYear, ref TheDate);
        }

        // year=YYYY and date=...YYYY -> remove year; not for year=YYYYa
        else if (TheYear.Length == 4 && TheDate.Contains(TheYear) && YearOnly.IsMatch(TheYear))
        {
            Parsers p = new Parsers();
            TheDate = p.FixDatesAInternal(TheDate);

            if (WikiRegexes.InternationalDates.IsMatch(TheDate) || WikiRegexes.AmericanDates.IsMatch(TheDate)
                || WikiRegexes.ISODates.IsMatch(TheDate))
            {
                TheYear = "";
                newValue = Tools.RemoveTemplateParameter(newValue, "year");
            }
        }

        // month=Month and date=...Month... OR month=Month and date=same month (by conversion from ISO format) Or month=nn and date=same month (by conversion to ISO format)
        int num;
        if ((TheMonth.Length > 2 && TheDate.Contains(TheMonth)) // named month within date
            || (TheMonth.Length > 2 && Tools.ConvertDate(TheDate, DateLocale.International).Contains(TheMonth))
            ||
            (int.TryParse(TheMonth, out num) &&
             Regex.IsMatch(Tools.ConvertDate(TheDate, DateLocale.ISO), @"\-0?" + TheMonth + @"\-")))
        {
            newValue = Tools.RemoveTemplateParameter(newValue, "month");
        }

        // date = Month DD and year = YYYY --> date = Month DD, YYYY
        if (!YearOnly.IsMatch(TheDate) && YearOnly.IsMatch(TheYear))
        {
            if (!WikiRegexes.AmericanDates.IsMatch(TheDate) &&
                WikiRegexes.AmericanDates.IsMatch(TheDate + ", " + TheYear))
            {
                if (!TheDate.Contains(TheYear))
                {
                    newValue = Tools.SetTemplateParameterValue(newValue, "date", TheDate + ", " + TheYear);
                }
                newValue = Tools.RemoveTemplateParameter(newValue, "year");
            }
            else if (!WikiRegexes.InternationalDates.IsMatch(TheDate) &&
                     WikiRegexes.InternationalDates.IsMatch(TheDate + " " + TheYear))
            {
                if (!TheDate.Contains(TheYear))
                {
                    newValue = Tools.SetTemplateParameterValue(newValue, "date", TheDate + " " + TheYear);
                }
                newValue = Tools.RemoveTemplateParameter(newValue, "year");
            }
        }

        // correct volume=vol 7... and issue=no. 8 for {{cite journal}} only
        if (templatename.Equals("cite journal", StringComparison.OrdinalIgnoreCase))
        {
            newValue = NormalizeCitationJournalVolumeAndIssue(newValue, TheVolume, TheIssue);
        }
        // {{cite web}} for Google books -> {{Cite book}}
        else if (templatename.Contains("web") &&
                 newValue.Contains("http://books.google.") &&
                 TheWork.Length == 0)
        {
            newValue = Tools.RenameTemplate(newValue, templatename, "Cite book");
        }

        // remove leading zero in day of month
        if (paramsFound.Any(p => p.Key.Contains("date") && Regex.IsMatch(p.Value, @"\b0[1-9]")))
        {
            newValue = DateLeadingZero.Replace(newValue, @"$1$2$3$4$5");
            newValue = DateLeadingZero.Replace(newValue, @"$1$2$3$4$5");
            TheDate = Tools.GetTemplateParameterValue(newValue, "date");
            accessdate = Tools.GetTemplateParameterValue(newValue, "accessdate");
            if (accessdate.Length == 0)
                accessdate = Tools.GetTemplateParameterValue(newValue, "access-date");
        }

        if (paramsFound.Any(s => s.Key.Contains("access") && !s.Key.Contains("date")))
        {
            string accessyear;
            if (!paramsFound.TryGetValue("accessyear", out accessyear))
                accessyear = "";

            if (Regex.IsMatch(templatename, @"[Cc]ite(?: ?web| book| news)"))
            {
                // remove any empty accessdaymonth, accessmonthday, accessmonth and accessyear
                newValue = AccessDayMonthDay.Replace(newValue, "");

                // merge accessdate of 'D Month' or 'Month D' and accessyear of 'YYYY' in cite web
                if (accessyear.Length == 4)
                    newValue = AccessDateYear.Replace(newValue, @" $2$1$3");
            }

            // remove accessyear where accessdate is present and contains said year
            if (accessyear.Length > 0 && accessdate.Contains(accessyear))
                newValue = Tools.RemoveTemplateParameter(newValue, "accessyear");
        }

        // fix unspaced comma ranges, avoid pages=12,345 as could be valid page number
        if (pages.Contains(",") && !Regex.IsMatch(pages, @"\b[0-9]{1,2},[0-9]{3}\b"))
        {
            while (UnspacedCommaPageRange.IsMatch(pages))
            {
                pages = UnspacedCommaPageRange.Replace(pages, "$1, $2");
            }
            newValue = Tools.UpdateTemplateParameterValue(newValue, "pages", pages);
            paramsFound.Remove("pages");
            paramsFound.Add("pages", pages);
        }

        // page range should have unspaced en-dash; validate that page is range not section link
        newValue = FixPageRanges(newValue, paramsFound);

        // page range or list should use 'pages' parameter not 'page'
        if (page.Length > 0 && CiteTemplatesPageRangeName.IsMatch(newValue))
        {
            newValue = CiteTemplatesPageRangeName.Replace(newValue, @"$1pages$2");
            newValue = Tools.RemoveDuplicateTemplateParameters(newValue);
        }

        // remove ordinals from dates
        newValue = RemoveCitationDateOrdinals(newValue, TheDate, accessdate);

        // catch after any other fixes
        if (!IncorrectCommaAmericanDates.IsMatch(theURLoriginal))
            newValue = IncorrectCommaAmericanDates.Replace(newValue, @"$1 $2, $3");

        // URL starting www needs http://
        if (theURL.StartsWith("www", StringComparison.OrdinalIgnoreCase))
            theURL = "http://" + theURL;

        // URL http format fix
        if (theURL.StartsWith("http") && !theURL.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !theURL.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            theURL = FixSyntax(" " + theURL).Trim();

        if (archiveurl.StartsWith("www", StringComparison.OrdinalIgnoreCase))
        {
            newValue = Tools.UpdateTemplateParameterValue(newValue, "archiveurl", "http://" + archiveurl);
            newValue = Tools.UpdateTemplateParameterValue(newValue, "archive-url", "http://" + archiveurl);
        }
        if (contributionurl.StartsWith("www", StringComparison.OrdinalIgnoreCase))
            newValue = Tools.UpdateTemplateParameterValue(newValue, "contribution-url", "http://" + contributionurl);

        // (part) wikilinked/external linked URL in cite template, don't change when named external link format
        if (!theURL.Contains(" "))
            theURL = theURL.Trim('[').Trim(']');

        if (!theURLoriginal.Equals(theURL))
            newValue = Tools.UpdateTemplateParameterValue(newValue, "url", theURL);

        // url=...archive to url=original, archive-url=archive
        if (ArchiveOrgURL.IsMatch(theURL) && archiveurl.Length == 0)
        {
            newValue = Tools.SetTemplateParameterValue(newValue, "archive-url", theURL);
            newValue = Tools.SetTemplateParameterValue(newValue, "archive-date", Regex.Replace(ArchiveOrgURL.Match(theURL).Groups[1].Value, @"(\d{4})(\d\d)(\d\d)", "$1-$2-$3"));
            if (website.ToLower().StartsWith("archive") || website.ToLower().StartsWith("web.archive"))
                newValue = Tools.RemoveTemplateParameter(newValue, "website");
            theURL = ArchiveOrgURL.Replace(theURL, "");
            newValue = Tools.UpdateTemplateParameterValue(newValue, "url", theURL);
        }

        newValue = MoveDeadLinkOutsideCitation(newValue, format, theURL);

        if (id.Length > 0)
        {
            // get id param name, id or ID
            string idParamName = paramsFound.FirstOrDefault(p => p.Key == "ID" || p.Key == "id").Key;

            //id=ISBN fix
            if (IdISBN.IsMatch(id) && ISBN.Length == 0)
            {
                newValue = Tools.RenameTemplateParameter(newValue, idParamName, "isbn");
                newValue = Tools.SetTemplateParameterValue(newValue, "isbn", IdISBN.Match(id).Groups[1].Value.Trim());
            }

            //id=ASIN fix
            if (IdASIN.IsMatch(id) && Tools.GetTemplateParameterValue(newValue, "asin", true).Length == 0)
            {
                newValue = Tools.RenameTemplateParameter(newValue, idParamName, "asin");
                newValue = Tools.SetTemplateParameterValue(newValue, "asin", IdASIN.Match(id).Groups[1].Value.Trim());
            }

            //id=ISSN fix
            Match IdISSNMatch = IdISSN.Match(id);
            if (IdISSNMatch.Success && ISSN.Length == 0)
            {
                string newIssn = IdISSNMatch.Groups[1].Value + "-" + IdISSNMatch.Groups[2].Value; // 1234-5678 using standard hyphen
                newValue = Tools.RenameTemplateParameter(newValue, idParamName, "issn");
                newValue = Tools.SetTemplateParameterValue(newValue, "issn", newIssn);
            }
        }

        // format ISSN: 1234-5678 with hyphen
        if (ISSN.Length > 0)
        {
            string newISSN = Regex.Replace(ISSN, @"^([0-9]{4}) *[- –]* *([0-9]{3}[0-9X])$", "$1-$2");

            if (!newISSN.Equals(ISSN))
                newValue = Tools.UpdateTemplateParameterValue(newValue, paramsFound.FirstOrDefault(p => p.Key == "ISSN" || p.Key == "issn").Key, newISSN);
        }

        if (ISBN.Length > 0)
        {
            string ISBNbefore = ISBN;
            // remove ISBN at start, but not if multiple ISBN
            if (ISBN.IndexOf("isbn", StringComparison.OrdinalIgnoreCase) > -1
                && ISBN.Substring(4).IndexOf("isbn", StringComparison.OrdinalIgnoreCase) == -1)
                ISBN = Regex.Replace(ISBN, @"^(?i)ISBN\s*", "");

            // trim unneeded characters
            ISBN = ISBN.Trim(".;,:".ToCharArray()).Trim();

            // fix dashes: only hyphens allowed
            while (ISBNDash.IsMatch(ISBN))
                ISBN = ISBNDash.Replace(ISBN, @"$1-$2");
            ISBN = ISBN.Replace('\x2010', '-');
            ISBN = ISBN.Replace('\x2012', '-');

            if (!ISBN.Equals(ISBNbefore))
            {
                if (paramsFound.ContainsKey("ISBN"))
                    newValue = Tools.UpdateTemplateParameterValue(newValue, "ISBN", ISBN);
                else
                    newValue = Tools.UpdateTemplateParameterValue(newValue, "isbn", ISBN);
            }
        }

        // origyear --> year when no year/date
        if (origyear.Length == 4 && TheYear.Length == 0 && TheDate.Length == 0)
        {
            newValue = Tools.RenameTemplateParameter(newValue, "origyear", "year");
            newValue = Tools.RemoveDuplicateTemplateParameters(newValue);
        }

        return newValue;
    }

    /// <summary>
    /// Normalizes quotation marks in citation title parameters and removes
    /// unmatched outer quotation marks when it is safe to do so.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="parameters">
    /// The citation parameters captured from the template.
    /// </param>
    /// <returns>
    /// The citation template with normalized title quotation marks.
    /// </returns>
    private static string NormalizeCitationTitleQuotes(
        string template,
        IReadOnlyDictionary<string, string> parameters)
    {
        foreach (string parameterName in ParametersToDequote)
        {
            if (!parameters.TryGetValue(parameterName, out string title))
                continue;

            string normalizedTitle = title;

            // Convert curly quotation marks to straight quotation marks.
            normalizedTitle =
                WikiRegexes.CurlyDoubleQuotes.Replace(normalizedTitle, @"""");
            normalizedTitle =
                BalancedArrows.Replace(normalizedTitle, @"""$1""");
            normalizedTitle = normalizedTitle.Replace("’", "'");
            normalizedTitle = normalizedTitle.Replace("‘", "'");

            // Do not alter hidden text because its quotation marks cannot be
            // reliably evaluated at this stage.
            if (!normalizedTitle.Trim('"').Contains(@"""") &&
                !normalizedTitle.Contains("⌊⌊⌊⌊"))
            {
                if (normalizedTitle.StartsWith(@"""") &&
                    !normalizedTitle.EndsWith(@""""))
                {
                    normalizedTitle = normalizedTitle.TrimStart('"');
                }
                else if (normalizedTitle.EndsWith(@"""") &&
                         !normalizedTitle.StartsWith(@""""))
                {
                    normalizedTitle = normalizedTitle.TrimEnd('"');
                }
            }

            if (!title.Equals(normalizedTitle))
            {
                template = Tools.SetTemplateParameterValue(
                    template,
                    parameterName,
                    normalizedTitle);
            }
        }

        return template;
    }

    /// <summary>
    /// Moves a dead-link template from the citation's format parameter to
    /// immediately after the citation template.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="format">
    /// The current value of the format parameter.
    /// </param>
    /// <param name="url">
    /// The current value of the URL parameter.
    /// </param>
    /// <returns>
    /// The citation with any embedded dead-link template moved outside it.
    /// </returns>
    private static string MoveDeadLinkOutsideCitation(
        string template,
        string format,
        string url)
    {
        Match deadLinkMatch = WikiRegexes.DeadLink.Match(format);

        if (!deadLinkMatch.Success)
            return template;

        string deadLink = deadLinkMatch.Value;

        if (url.ToUpper().TrimEnd('L').EndsWith("HTM") &&
            format.Equals(deadLink))
        {
            template = Tools.RemoveTemplateParameter(template, "format");
        }
        else
        {
            template = Tools.UpdateTemplateParameterValue(
                template,
                "format",
                format.Replace(deadLink, ""));
        }

        return template + " " + deadLink;
    }

    /// <summary>
    /// Replaces a language icon template in the citation language parameter
    /// with its corresponding language code.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="language">
    /// The current value of the language parameter.
    /// </param>
    /// <returns>
    /// The citation template with any supported language icon converted to
    /// its language code.
    /// </returns>
    private static string NormalizeCitationLanguageTemplate(
        string template,
        string language)
    {
        if (!language.Contains("{{"))
            return template;

        return LangTemplate.Replace(template, "$1$3");
    }

    /// <summary>
    /// Removes manually applied italics from a citation work parameter when
    /// the citation template will apply the formatting automatically.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="work">
    /// The current value of the work parameter.
    /// </param>
    /// <returns>
    /// The citation template with unnecessary work-field italics removed.
    /// </returns>
    private static string RemoveRedundantCitationWorkItalics(
        string template,
        string work)
    {
        // Book and periodical work names are italicized automatically by the
        // citation template. Preserve the existing website-related exception.
        if (work.Contains("''") && !work.Contains("."))
            return WorkInItalics.Replace(template, "$1$2");

        return template;
    }

    /// <summary>
    /// Removes an empty <c>origdate</c> parameter from a citation template.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="parameters">
    /// The citation parameters captured from the template.
    /// </param>
    /// <param name="originalDate">
    /// The current value of the <c>origdate</c> parameter.
    /// </param>
    /// <returns>
    /// The citation template with an empty <c>origdate</c> parameter removed,
    /// or the original template when the parameter is absent or has a value.
    /// </returns>
    private static string RemoveEmptyCitationOriginalDate(
        string template,
        IReadOnlyDictionary<string, string> parameters,
        string originalDate)
    {
        if (parameters.ContainsKey("origdate") && originalDate.Length == 0)
            return Tools.RemoveTemplateParameter(template, "origdate");

        return template;
    }

    /// <summary>
    /// Replaces Windows-style line breaks within citation parameter values
    /// with spaces.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="parameters">
    /// The citation parameters and their current values.
    /// </param>
    /// <returns>
    /// The citation template with line breaks removed from parameter values.
    /// </returns>
    private static string NormalizeCitationParameterNewlines(
        string template,
        IReadOnlyDictionary<string, string> parameters)
    {
        foreach (KeyValuePair<string, string> parameter in
                 parameters.Where(parameter =>
                     parameter.Value.Contains("\r\n")))
        {
            template = Tools.UpdateTemplateParameterValue(
                template,
                parameter.Key,
                parameter.Value.Replace("\r\n", " "));
        }

        return template;
    }

    /// <summary>
    /// Removes redundant volume and issue labels from a
    /// <c>cite journal</c> template.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="volume">
    /// The current value of the journal volume parameter.
    /// </param>
    /// <param name="issue">
    /// The current value of the journal issue parameter.
    /// </param>
    /// <returns>
    /// The citation template with journal volume and issue formatting
    /// normalized.
    /// </returns>
    private static string NormalizeCitationJournalVolumeAndIssue(
        string template,
        string volume,
        string issue)
    {
        if (volume.Length > 0)
            template = CiteTemplatesJournalVolume.Replace(template, "");

        if (issue.Length > 0)
        {
            template = CiteTemplatesJournalIssue.Replace(template, "");
        }
        else
        {
            template = CiteTemplatesJournalVolumeAndIssue.Replace(
                template,
                @"| issue = ");
        }

        return template;
    }

    /// <summary>
    /// Corrects punctuation in a full date stored in the citation
    /// <c>year</c> parameter and moves recognized full dates to
    /// the <c>date</c> parameter.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="year">
    /// The current value of the citation <c>year</c> parameter.
    /// This value is updated when its punctuation is corrected and
    /// cleared when the parameter is renamed to <c>date</c>.
    /// </param>
    /// <param name="date">
    /// The current value of the citation <c>date</c> parameter.
    /// This value is updated when a full date is moved from
    /// <c>year</c>.
    /// </param>
    /// <returns>
    /// The citation template with the year value corrected and,
    /// when applicable, moved to the <c>date</c> parameter.
    /// </returns>
    private static string MoveFullDateFromCitationYear(
        string template,
        ref string year,
        ref string date)
    {
        string correctedYear =
            IncorrectCommaInternationalDates.Replace(
                year,
                @"$1 $2");

        correctedYear =
            IncorrectCommaAmericanDates.Replace(
                correctedYear,
                @"$1 $2, $3");

        if (!correctedYear.Equals(year))
        {
            template = Tools.UpdateTemplateParameterValue(
                template,
                "year",
                correctedYear);

            year = correctedYear;
        }

        if (WikiRegexes.ISODates.IsMatch(year) ||
            WikiRegexes.InternationalDates.IsMatch(year) ||
            WikiRegexes.AmericanDates.IsMatch(year))
        {
            date = year;
            year = "";

            template = Tools.RenameTemplateParameter(
                template,
                "year",
                "date");
        }

        return template;
    }

    /// <summary>
    /// Removes ordinal suffixes from citation publication and access dates.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="date">
    /// The current value of the citation <c>date</c> parameter.
    /// </param>
    /// <param name="accessDate">
    /// The current value of the citation access-date parameter.
    /// </param>
    /// <returns>
    /// The citation template with recognized ordinal suffixes removed from
    /// publication and access dates.
    /// </returns>
    private static string RemoveCitationDateOrdinals(
        string template,
        string date,
        string accessDate)
    {
        if (!Ordinal.IsMatch(date) && !Ordinal.IsMatch(accessDate))
            return template;

        if (OrdinalsInDatesInt.IsMatch(date))
        {
            template = Tools.UpdateTemplateParameterValue(
                template,
                "date",
                OrdinalsInDatesInt.Replace(date, "$1$2$3 $4"));
        }
        else if (OrdinalsInDatesAm.IsMatch(date))
        {
            template = Tools.UpdateTemplateParameterValue(
                template,
                "date",
                OrdinalsInDatesAm.Replace(date, "$1 $2$3"));
        }

        if (OrdinalsInDatesInt.IsMatch(accessDate))
        {
            string normalizedAccessDate =
                OrdinalsInDatesInt.Replace(
                    accessDate,
                    "$1$2$3 $4");

            template = Tools.UpdateTemplateParameterValue(
                template,
                "accessdate",
                normalizedAccessDate);

            template = Tools.UpdateTemplateParameterValue(
                template,
                "access-date",
                normalizedAccessDate);
        }
        else if (OrdinalsInDatesAm.IsMatch(accessDate))
        {
            string normalizedAccessDate =
                OrdinalsInDatesAm.Replace(
                    accessDate,
                    "$1 $2$3");

            template = Tools.UpdateTemplateParameterValue(
                template,
                "accessdate",
                normalizedAccessDate);

            template = Tools.UpdateTemplateParameterValue(
                template,
                "access-date",
                normalizedAccessDate);
        }

        return template;
    }

    /// <summary>
    /// Renames citation parameters that are not lowercase while preserving
    /// recognized uppercase identifier names and text occurring inside a
    /// malformed URL.
    /// </summary>
    /// <param name="template">
    /// The citation template being processed.
    /// </param>
    /// <param name="parameters">
    /// The citation parameters captured before normalization.
    /// </param>
    /// <returns>
    /// The citation template with eligible parameter names converted to
    /// lowercase.
    /// </returns>
    private static string NormalizeCitationParameterNames(
        string template,
        IReadOnlyDictionary<string, string> parameters)
    {
        foreach (string parameterName in parameters.Keys.Where(
                     parameterName =>
                         parameterName.ToLower() != parameterName &&
                         !Regex.IsMatch(
                             parameterName,
                             @"(?:IS[BS]N|DOI|PMID|OCLC|PMC|LCCN|ASIN|ARXIV|ASIN\-TLD|BIBCODE|ID|ISBN13|JFM|JSTOR|MR|OL|OSTI|RFC|SSRN|URL|ZBL)") &&
                         !CiteUrl.Match(template).Value.Contains(parameterName)))
        {
            template = Tools.RenameTemplateParameter(
                template,
                parameterName,
                parameterName.ToLower());
        }

        return template;
    }

    /// <summary>
    /// Retrieves the citation parameter values used by the remaining citation
    /// cleanup rules.
    /// </summary>
    /// <param name="parameters">
    /// The citation parameters captured from the template.
    /// </param>
    /// <returns>
    /// An object containing the current values of the citation parameters used
    /// during subsequent processing.
    /// </returns>
    private static CitationParameterValues GetCitationParameterValues(
        IReadOnlyDictionary<string, string> parameters)
    {
        return new CitationParameterValues
        {
            Url = GetCitationParameterValue(parameters, "url"),
            Id = GetCitationParameterValue(parameters, "id", "ID"),
            Format = GetCitationParameterValue(parameters, "format"),
            Title = GetCitationParameterValue(parameters, "title"),
            Year = GetCitationParameterValue(parameters, "year"),
            Date = GetCitationParameterValue(parameters, "date"),
            Language = GetCitationParameterValue(parameters, "language"),
            Month = GetCitationParameterValue(parameters, "month"),
            Work = GetCitationParameterValue(parameters, "work"),
            Website = GetCitationParameterValue(parameters, "website"),
            NoPagePrefix = GetCitationParameterValue(parameters, "nopp"),
            Issue = GetCitationParameterValue(parameters, "issue"),
            Volume = GetCitationParameterValue(parameters, "volume"),
            AccessDate = GetCitationParameterValue(
                parameters,
                "accessdate",
                "access-date"),
            Pages = GetCitationParameterValue(parameters, "pages"),
            Page = GetCitationParameterValue(parameters, "page"),
            OriginalYear = GetCitationParameterValue(parameters, "origyear"),
            OriginalDate = GetCitationParameterValue(parameters, "origdate"),
            ArchiveUrl = GetCitationParameterValue(
                parameters,
                "archiveurl",
                "archive-url"),
            ContributionUrl = GetCitationParameterValue(
                parameters,
                "contribution-url"),
            Isbn = GetCitationParameterValue(parameters, "isbn", "ISBN"),
            Issn = GetCitationParameterValue(parameters, "issn", "ISSN")
        };
    }

    /// <summary>
    /// Returns the value of the first matching citation parameter name.
    /// </summary>
    /// <param name="parameters">
    /// The citation parameters to search.
    /// </param>
    /// <param name="parameterNames">
    /// The parameter names to check in priority order.
    /// </param>
    /// <returns>
    /// The first matching parameter value, or an empty string when none of
    /// the supplied parameter names are present.
    /// </returns>
    private static string GetCitationParameterValue(
        IReadOnlyDictionary<string, string> parameters,
        params string[] parameterNames)
    {
        foreach (string parameterName in parameterNames)
        {
            if (parameters.TryGetValue(parameterName, out string value))
                return value;
        }

        return "";
    }

    /// <summary>
    /// Contains citation parameter values used by the citation cleanup rules.
    /// </summary>
    private sealed class CitationParameterValues
    {
        public string Url { get; set; } = "";
        public string Id { get; set; } = "";

        public string Format { get; set; } = "";

        public string Title { get; set; } = "";

        public string Year { get; set; } = "";

        public string Date { get; set; } = "";

        public string Language { get; set; } = "";

        public string Month { get; set; } = "";

        public string Work { get; set; } = "";

        public string Website { get; set; } = "";

        public string NoPagePrefix { get; set; } = "";

        public string Issue { get; set; } = "";

        public string Volume { get; set; } = "";

        public string AccessDate { get; set; } = "";

        public string Pages { get; set; } = "";

        public string Page { get; set; } = "";

        public string OriginalYear { get; set; } = "";

        public string OriginalDate { get; set; } = "";

        public string ArchiveUrl { get; set; } = "";

        public string ContributionUrl { get; set; } = "";

        public string Isbn { get; set; } = "";

        public string Issn { get; set; } = "";
    }
    #endregion

    #region PageRanges

    // TODO: Consider replacing PageFields with a read-only collection because
    // the set of recognized page parameter names is constant after initialization.
    /// <summary>
    /// Identifies citation parameters that may contain a page number or page range.
    /// </summary>
    /// <remarks>
    /// Includes both the full parameter names and their short aliases.
    /// </remarks>
    private static readonly List<string> PageFields =
        new(new[] { "page", "pages", "p", "pp" });

    // TODO: Review PageRange separator handling. The current expression accepts
    // hyphens and em dashes but does not include en dashes or the &ndash; entity.
    //
    // TODO: Review the eight-digit page-number limit in PageRange and document
    // whether it reflects an intentional validation rule or a legacy safeguard.
    /// <summary>
    /// Matches a numeric page range separated by one or more hyphens or em dashes.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the starting page and group 2 captures the ending page.
    /// Each page number may contain between one and eight digits.
    /// </remarks>
    private static readonly Regex PageRange =
        new(
            @"\b([0-9]{1,8})\s*[-—]+\s*([0-9]{1,8})",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches a page range containing spaces around an en dash or
    /// <c>&amp;ndash;</c> entity.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the starting page, group 2 captures the separator, and
    /// group 3 captures the first digit of the ending page.
    /// </remarks>
    private static readonly Regex SpacedPageRange =
        new(
            @"(\d+) +(–|&ndash;) +(\d)",
            RegexOptions.Compiled);

    // TODO: Document the lifecycle of HiddenRegex placeholders and verify that
    // malformed or unmatched placeholder markers are handled safely.
    /// <summary>
    /// Matches an internal placeholder used to temporarily hide protected text.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the numeric placeholder identifier between the marker
    /// characters.
    /// </remarks>
    private static readonly Regex HiddenRegex =
        new(
            "⌊⌊⌊⌊(\\d*)⌋⌋⌋⌋",
            RegexOptions.Compiled);

    /// <summary>
    /// Converts hyphens in page ranges in citation template fields to endashes
    /// To avoid false positives, not applied when field value contains hidden text (e.g. wiki comment) or wiki/external links
    /// </summary>
    /// <param name="templateCall">The template call</param>
    /// <param name="Params">Dictionary of template parameters in template call</param>
    /// <returns>The updated template call</returns>
    private static string FixPageRanges(string templateCall, Dictionary<string, string> Params)
    {
        foreach (KeyValuePair<string, string> kvp in Params.Where(x => PageFields.Contains(x.Key) && x.Value.Length > 0
            && !HiddenRegex.IsMatch(x.Value) && !x.Value.Contains("[")))
        {
            string res = FixPageRangesValue(kvp.Value);
            if (!res.Equals(kvp.Value))
                templateCall = Tools.UpdateTemplateParameterValue(templateCall, kvp.Key, res);
        }

        return templateCall;
    }

    /// <summary>
    /// Normalizes valid numeric page ranges within a citation page value.
    /// </summary>
    /// <param name="pageRange">
    /// Page value that may contain one or more numeric page ranges.
    /// </param>
    /// <returns>
    /// The normalized page value when all detected ranges are valid; otherwise,
    /// the original value.
    /// </returns>
    /// <remarks>
    /// Spaced ranges using an en dash or <c>&amp;ndash;</c> are normalized
    /// immediately by removing the surrounding spaces.
    ///
    /// For other numeric ranges, abbreviated ending pages are expanded for
    /// validation. For example, <c>350-2</c> is interpreted as
    /// <c>350-352</c>.
    ///
    /// All detected ranges must:
    /// <list type="bullet">
    /// <item>
    /// <description>Increase from the starting page to the ending page.</description>
    /// </item>
    /// <item>
    /// <description>Span fewer than 999 pages.</description>
    /// </item>
    /// <item>
    /// <description>Not overlap another range in the same value.</description>
    /// </item>
    /// </list>
    ///
    /// When every detected range is valid, hyphens and em dashes matched by
    /// <see cref="PageRange"/> are replaced with en dashes. The abbreviated
    /// ending page remains abbreviated in the returned text.
    /// </remarks>
    private static string FixPageRangesValue(string pageRange)
    {
        // TODO: Review whether this early return should remain. Once a spaced page
        // range is normalized, the method skips validation and normalization of any
        // additional page ranges contained in the same value.
        if (SpacedPageRange.IsMatch(pageRange))
            return SpacedPageRange.Replace(pageRange, "$1$2$3");

        if (!ShouldValidatePageRanges(pageRange))
            return pageRange;

        return ArePageRangesValid(pageRange)
            ? PageRange.Replace(pageRange, "$1–$2")
            : pageRange;
    }

    /// <summary>
    /// Determines whether a page value should be inspected for numeric ranges.
    /// </summary>
    private static bool ShouldValidatePageRanges(string pageRange)
    {
        return pageRange.Length > 2 &&
               !pageRange.Contains(" to ");
    }

    /// <summary>
    /// Determines whether every numeric page range in a page value is ascending,
    /// reasonably sized, and non-overlapping.
    /// </summary>
    private static bool ArePageRangesValid(string pageRange)
    {
        bool foundRange = false;
        Dictionary<int, int> pageRanges = new();

        foreach (Match pageRangeMatch in PageRange.Matches(pageRange))
        {
            foundRange = true;

            // TODO: Consider replacing the page-range dictionary with a collection of
            // range values. The starting page is used as a dictionary key even though
            // this method only needs to retain range boundaries for overlap checks.
            int firstPage =
                Convert.ToInt32(pageRangeMatch.Groups[1].Value);

            int lastPage =
                GetExpandedLastPage(
                    pageRangeMatch.Groups[1].Value,
                    pageRangeMatch.Groups[2].Value);

            if (!IsValidPageRange(firstPage, lastPage) ||
                OverlapsExistingPageRange(firstPage, lastPage, pageRanges))
            {
                return false;
            }

            pageRanges.Add(firstPage, lastPage);
        }

        return foundRange;
    }

    /// <summary>
    /// Expands an abbreviated ending page using the leading digits from the
    /// starting page.
    /// </summary>
    /// <example>
    /// <c>350</c> and <c>2</c> produce <c>352</c>.
    /// </example>
    private static int GetExpandedLastPage(
        string firstPage,
        string lastPage)
    {
        if (firstPage.Length > lastPage.Length)
        {
            lastPage =
                firstPage.Substring(
                    0,
                    firstPage.Length - lastPage.Length) +
                lastPage;
        }

        return Convert.ToInt32(lastPage);
    }

    /// <summary>
    /// Determines whether a numeric page range is ascending and spans fewer
    /// than 999 pages.
    /// </summary>
    private static bool IsValidPageRange(
        int firstPage,
        int lastPage)
    {
        return firstPage < lastPage &&
               lastPage - firstPage < 999;
    }

    // TODO: Review page-range overlap detection. The current check detects when
    // either endpoint falls inside an earlier range but does not detect a later
    // range that completely contains an earlier one.
    /// <summary>
    /// Determines whether either endpoint of a page range lies within a previously
    /// accepted range.
    /// </summary>
    private static bool OverlapsExistingPageRange(
        int firstPage,
        int lastPage,
        Dictionary<int, int> pageRanges)
    {
        return pageRanges.Any(
            existingRange =>
                firstPage >= existingRange.Key &&
                firstPage <= existingRange.Value ||
                lastPage >= existingRange.Key &&
                lastPage <= existingRange.Value);
    }
    #endregion

    #region CitationPublisherToWork

    // TODO: Review CiteWebOrNews template aliases against currently supported
    // citation-template redirects and aliases.
    /// <summary>
    /// Matches supported web and news citation templates, including nested
    /// template content.
    /// </summary>
    /// <remarks>
    /// Recognizes <c>cite web</c>, <c>citeweb</c>, <c>cite news</c>, and
    /// <c>citenews</c> template names.
    /// </remarks>
    private static readonly Regex CiteWebOrNews =
        Tools.NestedTemplateRegex(
            new[] { "cite web", "citeweb", "cite news", "citenews" });

    // TODO: Review whether PressPublishers should include additional wire
    // services or use a shared publisher classification mechanism.
    /// <summary>
    /// Matches recognized wire-service publisher names.
    /// </summary>
    /// <remarks>
    /// Recognizes <c>Associated Press</c> and
    /// <c>United Press International</c>. Matching is case-insensitive.
    /// </remarks>
    private static readonly Regex PressPublishers =
        new(
            @"(Associated Press|United Press International)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // TODO: Consider replacing WorkParameterAndAliases with a read-only
    // collection because the supported parameter names are constant.
    /// <summary>
    /// Identifies citation parameters that represent the publication or work
    /// containing the cited item.
    /// </summary>
    /// <remarks>
    /// Includes the primary <c>work</c> parameter and its supported aliases:
    /// <c>newspaper</c>, <c>journal</c>, <c>periodical</c>, and
    /// <c>magazine</c>.
    /// </remarks>
    private static readonly List<string> WorkParameterAndAliases =
        new(new[] { "work", "newspaper", "journal", "periodical", "magazine" });

    /// <summary>
    /// Where the publisher field is used incorrectly instead of the work field in a {{cite web}} or {{cite news}} citation
    /// convert the parameter to be 'work'
    /// Scenarios covered:
    /// * publisher == URL domain, no work= used
    /// </summary>
    /// <param name="citation">the citation</param>
    /// <returns>the updated citation</returns>
    public static string CitationPublisherToWork(string citation)
    {
        // only for {{cite web}} or {{cite news}}
        if (!CiteWebOrNews.IsMatch(citation))
            return citation;

        string publisher = Tools.GetTemplateParameterValue(citation, "publisher");

        // nothing to do if no publisher, or publisher is a press publisher
        if (publisher.Length == 0 | PressPublishers.IsMatch(publisher))
            return citation;

        List<string> workandaliases = Tools.GetTemplateParametersValues(citation, WorkParameterAndAliases);

        if (string.Join("", workandaliases.ToArray()).Length == 0)
        {
            citation = Tools.RenameTemplateParameter(citation, "publisher", "work");
            citation = WorkInItalics.Replace(citation, "$1$2");
        }

        return citation;
    }

    #endregion

    #region CiteTemplateDates

    /// <summary>
    /// Corrects common formatting errors in dates in external reference citation templates (doesn't link/delink dates)
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="noChange"></param>
    /// <returns>The modified article text.</returns>
    public string CiteTemplateDates(string articleText, out bool noChange)
    {
        string newText = CiteTemplateDates(articleText);

        noChange = newText.Equals(articleText);

        return newText;
    }

    // TODO: Consider replacing shared regex string fragments with reusable
    // RegexBuilder/helper methods during the planned regex reorganization if
    // doing so improves readability without changing behavior.
    /// <summary>
    /// Common regular-expression prefix used by citation parameter patterns.
    /// </summary>
    /// <remarks>
    /// Enables single-line and case-insensitive matching, then matches the
    /// beginning of a template parameter assignment, including the leading pipe
    /// and optional whitespace.
    /// </remarks>
    private const string SiCitStart =
        @"(?si)(\|\s*";

    /// <summary>
    /// Common regular-expression prefix for citation access-date parameters.
    /// </summary>
    /// <remarks>
    /// Matches the beginning of an <c>accessdate</c>, <c>access-date</c>,
    /// <c>archivedate</c>, or <c>archive-date</c> parameter assignment,
    /// immediately before the parameter value.
    /// </remarks>
    private const string CitAccessdate =
        SiCitStart + @"(?:access|archive)\-?date\s*=\s*";

    /// <summary>
    /// Common regular-expression prefix for citation date parameters.
    /// </summary>
    /// <remarks>
    /// Matches the beginning of <c>date</c>, <c>date2</c>,
    /// <c>airdate</c>, <c>airdate2</c>, <c>archivedate</c>, or
    /// <c>archivedate2</c> parameter assignments.
    /// </remarks>
    private const string CitDate =
        SiCitStart + @"(?:archive|air)?date2?\s*=\s*";

    /// <summary>
    /// Defines ordered replacements that normalize malformed or nonstandard
    /// citation access-date and archive-date values to ISO-style
    /// <c>YYYY-MM-DD</c> format.
    /// </summary>
    /// <remarks>
    /// The replacements recognize several historical date layouts, including:
    /// <list type="bullet">
    /// <item><description><c>MM-DD-YY</c> and <c>MM-DD-YYYY</c>.</description></item>
    /// <item><description><c>DD-MM-YY</c> and <c>DD-MM-YYYY</c>.</description></item>
    /// <item><description><c>YYYY-MM-DD</c> values with missing or inconsistent separators.</description></item>
    /// <item><description>Single-digit months or days that require leading zeroes.</description></item>
    /// <item><description>Ambiguous dates whose equal month and day values allow safe normalization.</description></item>
    /// </list>
    ///
    /// Rules are evaluated in declaration order. More specific or unambiguous
    /// patterns must therefore remain before broader patterns.
    ///
    /// The expressions use <see cref="CitAccessdate"/> so that the matched
    /// parameter prefix is retained in each replacement. Supported parameter
    /// names include access-date and archive-date variants.
    ///
    /// The patterns intentionally preserve the trailing template separator or
    /// closing braces where those characters are included in the match.
    /// </remarks>
    private static readonly RegexReplacement[] CiteTemplateIncorrectISOAccessdates =
    {
    new RegexReplacement(
        CitAccessdate + @")(1[0-2])[/_\-\.]?(1[3-9])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))",
        "${1}20$4-$2-$3"),

    new RegexReplacement(
        CitAccessdate + @")(1[0-2])[/_\-\.]?([23]\d)[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))",
        "${1}20$4-$2-$3"),

    new RegexReplacement(
        CitAccessdate + @")(1[0-2])[/_\-\.]?\2[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))",
        "${1}20$3-$2-$2"), // nn-nn-2004 and nn-nn-04 to ISO format (both nn the same)

    new RegexReplacement(
        CitAccessdate + @")(1[3-9])[/_\-\.]?(1[0-2])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))",
        "${1}20$4-$3-$2"),

    new RegexReplacement(
        CitAccessdate + @")(1[3-9])[/_\-\.]?0?([1-9])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))",
        "${1}20$4-0$3-$2"),

    new RegexReplacement(
        CitAccessdate + @")(20[012]\d)0?([01]\d)[/_\-\.]([0-3]\d\s*(?:\||}}))",
        "$1$2-$3-$4"),

    new RegexReplacement(
        CitAccessdate + @")(20[012]\d)[/_\-\.]([01]\d)0?([0-3]\d\s*(?:\||}}))",
        "$1$2-$3-$4"),

    new RegexReplacement(
        CitAccessdate + @")(20[012]\d)[/_\-\.]?([01]\d)[/_\-\.]?([1-9]\s*(?:\||}}))",
        "$1$2-$3-0$4"),

    new RegexReplacement(
        CitAccessdate + @")(20[012]\d)[/_\-\.]?([1-9])[/_\-\.]?([0-3]\d\s*(?:\||}}))",
        "$1$2-0$3-$4"),

    new RegexReplacement(
        CitAccessdate + @")(20[012]\d)[/_\-\.]?([1-9])[/_\-\.]0?([1-9]\s*(?:\||}}))",
        "$1$2-0$3-0$4"),

    new RegexReplacement(
        CitAccessdate + @")(20[012]\d)[/_\-\.]0?([1-9])[/_\-\.]([1-9]\s*(?:\||}}))",
        "$1$2-0$3-0$4"),

    new RegexReplacement(
        CitAccessdate + @")(20[012]\d)[/_\.]?([01]\d)[/_\.]?([0-3]\d\s*(?:\||}}))",
        "$1$2-$3-$4"),

    new RegexReplacement(
        CitAccessdate + @")([23]\d)[/_\-\.](1[0-2])[/_\-\.]?(?:20)?([012]\d)(?=\s*(?:\||}}))",
        "${1}20$4-$3-$2"),

    new RegexReplacement(
        CitAccessdate + @")([23]\d)[/_\-\.]0?([1-9])[/_\-\.](?:20)?([012]\d)(?=\s*(?:\||}}))",
        "${1}20$4-0$3-$2"),

    new RegexReplacement(
        CitAccessdate + @")0?([1-9])[/_\-\.]?(1[3-9]|[23]\d)[/_\-\.]?(?:20)?([012]\d)(?=\s*(?:\||}}))",
        "${1}20$4-0$2-$3"),

    new RegexReplacement(
        CitAccessdate + @")0?([1-9])[/_\-\.]?0?\2[/_\-\.]?(?:20)?([012]\d)(?=\s*(?:\||}}))",
        "${1}20$3-0$2-0$2") // n-n-2004 and n-n-04 to ISO format (both n the same)
};

    /// <summary>
    /// Defines ordered replacements that normalize malformed or nonstandard
    /// citation date values to ISO-style <c>YYYY-MM-DD</c> format.
    /// </summary>
    /// <remarks>
    /// The collection applies to general citation date parameters matched by
    /// <see cref="CitDate"/>, including supported date, airdate, and archivedate
    /// parameter variants.
    ///
    /// The replacements recognize multiple historical and malformed layouts,
    /// including:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Year-first dates that use missing, incorrect, or inconsistent separators.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Month-day-year and day-month-year values where the component ranges make
    /// the intended order unambiguous.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Two-digit years that are normalized to years in the 2000s.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Four-digit years from the late twentieth century and later.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Single-digit month or day components that require leading zeroes.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Dates enclosed in optional wiki-link brackets.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Ambiguous dates whose identical month and day values allow safe normalization.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Compact year-month-day values where one or more separators are missing.
    /// </description>
    /// </item>
    /// </list>
    ///
    /// Rules are evaluated in declaration order. This ordering is significant:
    /// specific and unambiguous layouts must remain before broader expressions
    /// that could match the same input.
    ///
    /// Each replacement preserves the citation parameter prefix captured by
    /// <see cref="CitDate"/> and, where included in the match, preserves optional
    /// wiki-link brackets and the following template separator or closing braces.
    ///
    /// These expressions normalize recognized formatting errors; they are not a
    /// complete calendar-date validation system.
    /// </remarks>
    private static readonly RegexReplacement[] CiteTemplateIncorrectISODates =
    {
        new RegexReplacement(CitDate + @"\[?\[?)(20\d\d|19[7-9]\d)[/_]?([01]\d)[/_]?([0-3]\d\s*(?:\||}}))",
            "$1$2-$3-$4"),
        new RegexReplacement(CitDate + @"\[?\[?)(1[0-2])[/_\-\.]?([23]\d)[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$4-$2-$3"),
        new RegexReplacement(CitDate + @"\[?\[?)0?([1-9])[/_\-\.]?([23]\d)[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$4-0$2-$3"),
        new RegexReplacement(CitDate + @"\[?\[?)([23]\d)[/_\-\.]?0?([1-9])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$4-0$3-$2"),
        new RegexReplacement(CitDate + @"\[?\[?)([23]\d)[/_\-\.]?(1[0-2])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$4-$3-$2"),
        new RegexReplacement(CitDate + @"\[?\[?)(1[0-2])[/_\-\.]([23]\d)[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))",
            "${1}20$4-$2-$3"),
        new RegexReplacement(CitDate + @"\[?\[?)0?([1-9])[/_\-\.]([23]\d)[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))",
            "${1}20$4-0$2-$3"),
        new RegexReplacement(CitDate + @"\[?\[?)([23]\d)[/_\-\.]0?([1-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))",
            "${1}20$4-0$3-$2"),
        new RegexReplacement(CitDate + @"\[?\[?)([23]\d)[/_\-\.](1[0-2])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))",
            "${1}20$4-$3-$2"),
        new RegexReplacement(CitDate + @"\[?\[?)(1[0-2])[/_\-\.]?(1[3-9])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$4-$2-$3"),
        new RegexReplacement(
            CitDate + @"\[?\[?)0?([1-9])[/_\-\.](1[3-9])[/_\-\.](19[7-9]\d|20\d\d)(?=\s*(?:\||}}))", "$1$4-0$2-$3"),
        new RegexReplacement(CitDate + @"\[?\[?)(1[3-9])[/_\-\.]?0?([1-9])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$4-0$3-$2"),
        new RegexReplacement(CitDate + @"\[?\[?)(1[3-9])[/_\-\.]?(1[0-2])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$4-$3-$2"),
        new RegexReplacement(CitDate + @"\[?\[?)(1[0-2])[/_\-\.](1[3-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))",
            "${1}20$4-$2-$3"),
        new RegexReplacement(CitDate + @"\[?\[?)([1-9])[/_\-\.](1[3-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))",
            "${1}20$4-0$2-$3"),
        new RegexReplacement(CitDate + @"\[?\[?)(1[3-9])[/_\-\.]([1-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))",
            "${1}20$4-0$3-$2"),
        new RegexReplacement(CitDate + @"\[?\[?)(1[3-9])[/_\-\.](1[0-2])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))",
            "${1}20$4-$3-$2"),
        new RegexReplacement(CitDate + @")0?([1-9])[/_\-\.]0?\2[/_\-\.](20\d\d|19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$3-0$2-0$2"), // n-n-2004 and n-n-1980 to ISO format (both n the same)
        new RegexReplacement(CitDate + @")0?([1-9])[/_\-\.]0?\2[/_\-\.]([01]\d)(?=\s*(?:\||}}))", "${1}20$3-0$2-0$2"),
        // n-n-04 to ISO format (both n the same)
        new RegexReplacement(CitDate + @")(1[0-2])[/_\-\.]\2[/_\-\.]?(20\d\d|19[7-9]\d)(?=\s*(?:\||}}))",
            "$1$3-$2-$2"), // nn-nn-2004 and nn-nn-1980 to ISO format (both nn the same)
        new RegexReplacement(CitDate + @")(1[0-2])[/_\-\.]\2[/_\-\.]([01]\d)(?=\s*(?:\||}}))", "${1}20$3-$2-$2"),
        // nn-nn-04 to ISO format (both nn the same)
        new RegexReplacement(
            CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.]([1-9])[/_\-\.]0?([1-9](?:\]\])?\s*(?:\||}}))",
            "$1$2-0$3-0$4"),
        new RegexReplacement(
            CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.]0?([1-9])[/_\-\.]([1-9](?:\]\])?\s*(?:\||}}))",
            "$1$2-0$3-0$4"),
        new RegexReplacement(
            CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.]?([01]\d)[/_\-\.]?([1-9](?:\]\])?\s*(?:\||}}))",
            "$1$2-$3-0$4"),
        new RegexReplacement(
            CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.]?([1-9])[/_\-\.]?([0-3]\d(?:\]\])?\s*(?:\||}}))",
            "$1$2-0$3-$4"),
        new RegexReplacement(
            CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})([01]\d)[/_\-\.]([0-3]\d(?:\]\])?\s*(?:\||}}))", "$1$2-$3-$4"),
        new RegexReplacement(
            CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.](0[1-9]|1[0-2])0?([0-3]\d(?:\]\])?\s*(?:\||}}))",
            "$1$2-$3-$4")
    };

    // TODO: Review the supported year ranges in citation date and time patterns.
    // Some expressions are restricted to 1970–2099 or years beginning with 20.
    //
    // TODO: Review the redundant case-insensitive options in
    // CiteTemplateAbbreviatedMonthISO. The pattern contains inline (?si) options
    // while RegexOptions.IgnoreCase is also supplied externally.
    /// <summary>
    /// Matches citation date parameters containing a year, abbreviated month name,
    /// and day in a partially ISO-style order.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the parameter name and assignment prefix. Group 2 captures
    /// the date value in <c>year-month-day</c>, <c>year/month/day</c>, or
    /// whitespace-separated form. Group 3 captures the following template
    /// separator or closing braces.
    /// </remarks>
    private static readonly Regex CiteTemplateAbbreviatedMonthISO =
        new(
            @"(?si)(\|\s*(?:archive|air|access)?date2?\s*=\s*)(\d{4}[-/\s][A-Z][a-z]+\.?[-/\s][0-3]?\d)(\s*(?:\||}}))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // TODO: Verify whether CiteTemplateDateYYYYDDMMFormat intentionally accepts
    // day values from 32 through 39 as malformed-date detection rather than
    // valid calendar dates.
    /// <summary>
    /// Matches citation date parameters that appear to use a
    /// <c>YYYY-DD-MM</c> date order.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the parameter assignment through the four-digit year.
    /// Group 2 captures a day from 13 through 39, and group 3 captures a month
    /// from 01 through 12. Optional wiki-link brackets surrounding the date are
    /// also recognized.
    /// </remarks>
    private static readonly Regex CiteTemplateDateYYYYDDMMFormat =
        new(
            SiCitStart +
            @"(?:archive|air|access)?date2?\s*=\s*(?:\[\[)?20\d\d)-([23]\d|1[3-9])-(0[1-9]|1[0-2])(\]\])?",
            RegexOptions.Compiled);

    // TODO: Add focused tests for CiteTemplateTimeInDateParameter before
    // simplifying or decomposing the expression. It currently combines date,
    // time, wiki-link, namespace, and trailing-content handling.
    /// <summary>
    /// Matches a time appended to a citation date parameter.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the date parameter and date value. Group 2 captures the
    /// appended time and any associated trailing text. The expression supports
    /// numeric and month-name date formats and protects certain linked content
    /// from being treated as part of the time.
    /// </remarks>
    private static readonly Regex CiteTemplateTimeInDateParameter =
        new(
            @"(\|\s*(?:archive|air|access)?date2?\s*=\s*(?:(?:20\d\d|19[7-9]\d)-[01]?\d-[0-3]?\d|[0-3]?\d[a-z]{0,2}\s*\w+,?\s*(?:20\d\d|19[7-9]\d)|\w+\s*[0-3]?\d[a-z]{0,2},?\s*(?:20\d\d|19[7-9]\d)))(\s*[,-:]?\s+[0-2]?\d[:\.]?[0-5]\d(?:\:?[0-5]\d)?\s*(?:[^\|\}]*\[\[[^[\]\n]+(?<!\[\[[A-Z]?[a-z-]{2,}:[^[\]\n]+)\]\][^\|\}]*|[^\|\}]*)?)(?<!.*(?:20|1[7-9])\d+\s*)",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase |
            RegexOptions.Singleline);

    /// <summary>
    /// Matches one or more whitespace characters at the end of a string.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the trailing whitespace.
    /// </remarks>
    private static readonly Regex WhitespaceEnd =
        new(
            @"(\s+)$",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches a <c>cite podcast</c> template, including nested template content.
    /// </summary>
    private static readonly Regex CitePodcast =
        Tools.NestedTemplateRegex("cite podcast");

    /// <summary>
    /// Corrects common formatting errors in dates in external reference citation templates (doesn't link/delink dates)
    /// note some incorrect date formats such as 3-2-2009 are ambiguous as could be 3-FEB-2009 or MAR-2-2009, these fixes don't address such errors
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns>The modified article text.</returns>
    public static string CiteTemplateDates(string articleText)
    {
        // cite podcast is non-compliant to citation core standards
        if (!Variables.IsWikipediaEN || TemplateExists(GetAllTemplates(articleText), CitePodcast))
            return articleText;

        string originalArticleText = articleText;

        // loop in case a single citation has multiple dates to be fixed
        foreach (string s in GetAllTemplateDetail(articleText))
        {
            string res = s, original = "";
            while (!res.Equals(original))
            {
                original = res;
                res = WikiRegexes.CiteTemplate.Replace(res, CiteTemplateME);
            }

            if (!res.Equals(s))
                articleText = articleText.Replace(s, res);
        }

        // don't apply fixes when ambiguous dates present, for performance only appply this check if changes made
        if (!originalArticleText.Equals(articleText) && AmbiguousCiteTemplateDates(originalArticleText))
            return originalArticleText;

        return articleText;
    }

    /// <summary>
    /// convert invalid date formats like DD-MM-YYYY, MM-DD-YYYY, YYYY-D-M, YYYY-DD-MM, YYYY_MM_DD etc. to iso format of YYYY-MM-DD
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private static string CiteTemplateME(Match m)
    {
        string newValue = m.Value;

        Dictionary<string, string> paramsFound = Tools.GetTemplateParameterValues(newValue);

        string accessdate, date, date2, archivedate, airdate, journal;
        if (!paramsFound.TryGetValue("accessdate", out accessdate) &&
            !paramsFound.TryGetValue("access-date", out accessdate))
            accessdate = "";
        if (!paramsFound.TryGetValue("date", out date))
            date = "";
        if (!paramsFound.TryGetValue("date2", out date2))
            date2 = "";
        if (!paramsFound.TryGetValue("archivedate", out archivedate))
            archivedate = "";
        if (!paramsFound.TryGetValue("airdate", out airdate))
            airdate = "";
        if (!paramsFound.TryGetValue("journal", out journal))
            journal = "";

        List<string> dates = new List<string> { accessdate, archivedate, date, date2, airdate };

        if (CiteTemplateMEParameterToProcess(dates))
        {
            // accessdate=, archivedate=
            newValue = CiteTemplateIncorrectISOAccessdates.Aggregate(newValue,
                (current, rr) => rr.Regex.Replace(current, rr.Replacement));

            // date=, archivedate=, airdate=, date2=
            newValue = CiteTemplateIncorrectISODates.Aggregate(newValue,
                (current, rr) => rr.Regex.Replace(current, rr.Replacement));

            newValue = CiteTemplateDateYYYYDDMMFormat.Replace(newValue, "$1-$3-$2$4"); // YYYY-DD-MM to YYYY-MM-DD

            // date = YYYY-Month-DD fix, not for cite journal PubMed date format
            if (journal.Length == 0)
                newValue = CiteTemplateAbbreviatedMonthISO.Replace(newValue,
                    m2 =>
                        m2.Groups[1].Value + Tools.ConvertDate(m2.Groups[2].Value.Replace(".", ""), DateLocale.ISO) +
                        m2.Groups[3].Value);
        }
        // all citation dates: Remove time from date fields
        newValue = CiteTemplateTimeInDateParameter.Replace(newValue, m3 =>
        {
            // keep end whitespace outside comment
            string comm = m3.Groups[2].Value, whitespace = "";

            Match whm = WhitespaceEnd.Match(comm);

            if (whm.Success)
            {
                comm = comm.TrimEnd();
                whitespace = whm.Groups[1].Value;
            }

            return m3.Groups[1].Value + "<!--" + comm + @"-->" + whitespace;
        });

        return newValue;
    }

    /// <summary>
    /// Determines whether any supplied citation date parameter value requires
    /// additional processing.
    /// </summary>
    /// <param name="parameters">
    /// Citation parameter values to examine.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when at least one value is longer than four
    /// characters and is neither an ISO-formatted date nor a recognized
    /// month-name date; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Four-character values are ignored, which prevents year-only values from
    /// being treated as dates requiring normalization.
    ///
    /// A value is considered already acceptable when it either:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Matches <see cref="WikiRegexes.ISODates"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Begins with an optional one- or two-digit day followed by a recognized
    /// month name.
    /// </description>
    /// </item>
    /// </list>
    /// The method returns immediately after finding the first value that does not
    /// meet either condition.
    /// </remarks>
    private static bool CiteTemplateMEParameterToProcess(List<string> parameters)
    {
        foreach (string s in parameters)
        {
            if (s.Length > 4 &&
                !WikiRegexes.ISODates.IsMatch(s) &&
                !Regex.IsMatch(
                    s,
                    @"^(\d{1,2} *)?" + WikiRegexes.MonthsNoGroup))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Matches the numeric portion of a potentially ambiguous citation date.
    /// </summary>
    /// <remarks>
    /// Recognizes supported citation date parameters whose first two components
    /// are both valid month numbers, making the intended month/day order
    /// uncertain.
    ///
    /// Group 1 captures the first numeric component, group 2 captures the second
    /// numeric component, and group 3 captures either a four-digit year from
    /// 1970 onward or a two-digit year.
    ///
    /// The positive lookbehind requires the date to immediately follow a
    /// supported citation parameter assignment, but excludes that assignment
    /// prefix from the match.
    /// </remarks>
    private static readonly Regex PossibleAmbiguousCiteDate =
        new(
            @"(?<=\|\s*(?:access|archive|air)?\-?date2?\s*=\s*)(0?[1-9]|1[0-2])[/_\-\.](0?[1-9]|1[0-2])[/_\-\.](20\d\d|19[7-9]\d|[01]\d)\b",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches a supported citation parameter containing a potentially ambiguous
    /// numeric date.
    /// </summary>
    /// <remarks>
    /// This is the prefix-capturing counterpart to
    /// <see cref="PossibleAmbiguousCiteDate"/>.
    ///
    /// Group 1 captures the citation parameter assignment prefix, group 2
    /// captures the first numeric component, group 3 captures the second numeric
    /// component, and group 4 captures either a four-digit year from 1970 onward
    /// or a two-digit year.
    ///
    /// Including the parameter prefix allows callers to locate or replace the
    /// complete parameter value without relying on lookbehind matching.
    /// </remarks>
    private static readonly Regex PossibleAmbiguousCiteDateQuick =
        new(
            @"(\|\s*(?:access|archive|air)?\-?date2?\s*=\s*)(0?[1-9]|1[0-2])[/_\-\.](0?[1-9]|1[0-2])[/_\-\.](20\d\d|19[7-9]\d|[01]\d)\b",
            RegexOptions.Compiled);

    /// <summary>
    /// Returns whether the input article text contains ambiguous cite template dates in XX-XX-YYYY or XX-XX-YY format
    /// </summary>
    /// <param name="articleText">the article text to search</param>
    /// <returns>If any matches were found</returns>
    public static bool AmbiguousCiteTemplateDates(string articleText)
    {
        return AmbigCiteTemplateDates(articleText).Any();
    }

    /// <summary>
    /// Returns whether the input article text contains ambiguous cite template dates in XX-XX-YYYY or XX-XX-YY format
    /// </summary>
    /// <param name="articleText">The article text to search</param>
    /// <returns>A dictionary of matches (index and length)</returns>
    public static Dictionary<int, int> AmbigCiteTemplateDates(string articleText)
    {
        Dictionary<int, int> ambigDates = new Dictionary<int, int>();

        // check for performance
        if (
            PossibleAmbiguousCiteDateQuick.IsMatch(string.Join("",
                GetAllTemplateDetail(articleText).Where(t => t.Contains("date")).ToArray())))
        {
            foreach (Match m in WikiRegexes.CiteTemplate.Matches(articleText))
            {
                foreach (Match m2 in PossibleAmbiguousCiteDate.Matches(m.Value))
                {
                    // for YYYY-AA-BB date, ambiguous if AA and BB not the same
                    if (!m2.Groups[1].Value.Equals(m2.Groups[2].Value))
                        ambigDates.Add(m.Index + m2.Index, m2.Length);
                }
            }
        }

        return ambigDates;
    }

    #endregion
    // TODO: Consider replacing the large citation-parameter regex alternations
    // with shared read-only parameter collections once citation validation logic
    // is reorganized.
    //
    // TODO: Review the case-sensitive matching behavior of citeWebParameters and
    // citeArXivParameters. Capitalization variants are currently listed
    // individually and should not be consolidated without compatibility tests.
    /// <summary>
    /// Matches supported arXiv citation templates, including nested template
    /// content.
    /// </summary>
    /// <remarks>
    /// Recognizes both <c>cite arxiv</c> and <c>cite arXiv</c>.
    /// </remarks>
    private static readonly Regex CiteArXiv =
        Tools.NestedTemplateRegex(
            new[] { "cite arxiv", "cite arXiv" });

    // TODO: Review CitationPopulatedParameter handling of nested templates,
    /// links, and values containing pipes. The current expression stops at the
    // first pipe or closing brace and may rely on earlier masking or preprocessing.
    /// <summary>
    /// Matches a populated template parameter and captures its name and value.
    /// </summary>
    /// <remarks>
    /// Group 1 captures the parameter name. Group 2 captures the non-empty
    /// parameter value up to the next pipe or closing brace.
    /// Parameter names may contain letters, digits, underscores, hyphens,
    /// spaces, and apostrophes.
    /// </remarks>
    private static readonly Regex CitationPopulatedParameter =
        new(
            @"\|\s*([\w_\d- ']+)\s*=\s*([^\|}]+)",
            RegexOptions.Compiled);

    // TODO: Review citeWebParameters against the currently supported citation
    // template parameters and aliases. The expression contains many legacy and
    // deprecated names that may eventually be moved to shared metadata.
    /// <summary>
    /// Matches parameter names recognized for web-style citation templates.
    /// </summary>
    /// <remarks>
    /// The expression contains current and legacy aliases, capitalization
    /// variants, deprecated spellings, and identifier-specific parameter names.
    /// The word boundary prevents matching a recognized name as the prefix of a
    /// longer parameter name.
    /// </remarks>
    private static readonly Regex citeWebParameters =
        new(
            @"^(access-?date|agency|archive-?date|archive\-format|archive-?url|arxiv|ARXIV|asin|ASIN|asin-tld|ASIN-TLD|at|[Aa]uthor\d*|author\d*-first|author-?format|author\d*-(last|given|surname)|author-?link\d*|author\d*-?link|authors|author-mask|author-name-separator|author-separator|bibcode|BIBCODE|citeseerx|collaboration|date|dead-?url|department|df|dictionary|display-?(authors|subjects)|display-?editors|doi|DOI|DoiBroken|doi-broken|doi-broken-date|doi_brokendate|doi-inactive-date|doi_inactivedate|edition|[Ee]ditor|editor\d*|editor\d*-first|editor-?format|EditorGiven\d*|editor\d*-given|editor\d*-last|editor\d*-?link|editor-?mask|editor-name-separator|EditorSurname\d*|editor\d*-surname|editor-first\d*|editor-given\d*|editor-last\d*|editor-surname\d*|editorlink\d*|v?editors|eissn|[Ee]mbargo|encyclopa?edia|first\d*|format|given\d*|hdl|host|id|ID|ignoreisbnerror|ignore-isbn-error|institution|interviewer(\-(given|surname))?|i?sbn|ISBN|isbn13|ISBN13|issn|ISSN|issue|jfm|JFM|journal|jstor|JSTOR|language|last\d*|lastauthoramp|last-author-amp|lay-?(summary|url)|lccn|LCCN|location|magazine|medium|minutes|mode|mr|MR|name\-list\-style|newspaper|no-?pp|number|oclc|OCLC|ol|OL|orig-?(year|date)|others|osti|pp?|pages?|people|periodical|place|pmc|PMC|pmid|PMID|postscript|publication-?(?:place|date)|publisher|quotation|quote(\-pages?)?|[Rr]ef|registration|rfc|RFC|script\-title|script\-(website|work|quote)|separator|series|series-?link|ssrn|SSRN|subject(\-mask)?|subscription|surname\d*|s2cid(\-access)?|time|title(\-link)?|trans\-(quote|website|work)|trans[_-]title|translator\-(last\d*|surname)|translator(\-link\d*|\d+)?|translator\-(first\d*|given)|type|url|URL|vauthors|version|via|volume|website|work|year|zbl|ZBL)\b",
            RegexOptions.Compiled);

    // TODO: Review citeArXivParameters against the current Cite arXiv template
    // documentation and determine which legacy aliases must remain supported.
    /// <summary>
    /// Matches parameter names recognized for arXiv citation templates.
    /// </summary>
    /// <remarks>
    /// The expression includes current parameters, legacy aliases,
    /// capitalization variants, and known historical misspellings such as
    /// <c>seperator</c>.
    /// </remarks>
    private static readonly Regex citeArXivParameters =
        new(
            @"\b(arxiv|asin|ASIN|author\d*|authorlink\d*|author\d*-link|bibcode|class|coauthors?|collaboration|date|day|display\-authors|doi|DOI|doi brokendate|doi inactivedate|eprint|first\d*|format|given\d*|id|in|isbn|ISBN|issn|ISSN|jfm|JFM|jstor|JSTOR|language|last\d*|laydate|laysource|laysummary|lccn|LCCN|mode|month|mr|MR|oclc|OCLC|ol|OL|osti|OSTI|page|pmc|PMC|pmid|PMID|postscript|publication-date|quote|ref|rfc|RFC|separator|seperator|ssrn|SSRN|surname\d*|title|vauthors|version|year|zbl)\b",
            RegexOptions.Compiled);

    // TODO: Verify whether NoEqualsTwoBars should match positional template
    // parameters intentionally or only malformed named parameters.
    /// <summary>
    /// Matches text between two template pipes when the text does not contain an
    /// equals sign.
    /// </summary>
    /// <remarks>
    /// Used to identify positional or malformed template content that appears
    /// between parameter separators.
    /// </remarks>
    private static readonly Regex NoEqualsTwoBars =
        new(
            @"\|[^=\|]+\|",
            RegexOptions.Compiled);

    /// <summary>
    /// Searches citation templates for unknown parameters and malformed values.
    /// </summary>
    /// <param name="articleText">Wiki text to search.</param>
    /// <returns>
    /// A dictionary whose keys are character indexes in <paramref name="articleText"/>
    /// and whose values are the lengths of the invalid parameter names or values.
    /// </returns>
    public static Dictionary<int, int> BadCiteParameters(string articleText)
    {
        Dictionary<int, int> found = new();

        FindInvalidArXivParameters(articleText, found);
        FindInvalidCitationParametersAndValues(articleText, found);

        return found;
    }

    /// <summary>
    /// Finds populated parameters that are not recognized by
    /// <c>cite arXiv</c>.
    /// </summary>
    /// <param name="articleText">Wiki text to search.</param>
    /// <param name="found">
    /// Collection that receives the source indexes and lengths of invalid
    /// parameter names.
    /// </param>
    private static void FindInvalidArXivParameters(
        string articleText,
        Dictionary<int, int> found)
    {
        // Avoid the more expensive template scan when cite arXiv is absent.
        if (!TemplateExists(GetAllTemplates(articleText), CiteArXiv))
            return;

        foreach (Match citationMatch in CiteArXiv.Matches(articleText))
        {
            FindUnknownCitationParameters(
                citationMatch,
                citeArXivParameters,
                found);
        }
    }

    /// <summary>
    /// Searches citation templates for unknown web-citation parameters,
    /// malformed pipe-separated content, and URL values containing spaces.
    /// </summary>
    /// <param name="articleText">Wiki text to search.</param>
    /// <param name="found">
    /// Collection that receives the source indexes and lengths of invalid
    /// parameters or values.
    /// </param>
    private static void FindInvalidCitationParametersAndValues(
        string articleText,
        Dictionary<int, int> found)
    {
        foreach (Match citationMatch in WikiRegexes.CiteTemplate.Matches(articleText))
        {
            if (citationMatch.Groups[2].Value.EndsWith("web"))
            {
                FindUnknownCitationParameters(
                    citationMatch,
                    citeWebParameters,
                    found);
            }

            FindPipeWithoutParameterAssignment(citationMatch, found);
            FindUrlContainingSpaces(citationMatch, found);
        }
    }

    /// <summary>
    /// Finds populated citation parameters that are not accepted by the supplied
    /// parameter-name expression.
    /// </summary>
    /// <param name="citationMatch">
    /// Match containing the complete citation template.
    /// </param>
    /// <param name="validParameterNames">
    /// Expression that recognizes parameter names supported by the citation
    /// template.
    /// </param>
    /// <param name="found">
    /// Collection that receives source indexes and lengths of unknown parameter
    /// names.
    /// </param>
    private static void FindUnknownCitationParameters(
        Match citationMatch,
        Regex validParameterNames,
        Dictionary<int, int> found)
    {
        string citation = BuildCitationWithoutNestedTemplateContent(
            citationMatch.Value);

        foreach (Match parameterMatch in CitationPopulatedParameter.Matches(citation))
        {
            string parameterName = parameterMatch.Groups[1].Value;

            if (!validParameterNames.IsMatch(parameterName) &&
                Tools.GetTemplateParameterValue(citation, parameterName).Length > 0)
            {
                found.Add(
                    citationMatch.Index + parameterMatch.Groups[1].Index,
                    parameterMatch.Groups[1].Length);
            }
        }
    }

    /// <summary>
    /// Replaces nested template content with spaces while preserving character
    /// positions relative to the original citation template.
    /// </summary>
    /// <param name="citation">Complete citation template text.</param>
    /// <returns>
    /// Citation text in which nested templates are masked with spaces.
    /// </returns>
    private static string BuildCitationWithoutNestedTemplateContent(
        string citation)
    {
        string citationBody = citation.Substring(2);

        return "{{" +
               Tools.ReplaceWithSpaces(
                   citationBody,
                   WikiRegexes.NestedTemplates.Matches(citationBody));
    }

    /// <summary>
    /// Finds pipe-separated citation content that does not contain a parameter
    /// assignment.
    /// </summary>
    /// <param name="citationMatch">
    /// Match containing the complete citation template.
    /// </param>
    /// <param name="found">
    /// Collection that receives the source index and length of the malformed
    /// content.
    /// </param>
    private static void FindPipeWithoutParameterAssignment(
        Match citationMatch,
        Dictionary<int, int> found)
    {
        string pipeCleanedCitation =
            Tools.PipeCleanedTemplate(citationMatch.Value, false);

        // Preserve the existing guard so the check is only performed on
        // templates containing at least one named parameter.
        if (!pipeCleanedCitation.Contains("="))
            return;

        Match malformedContent = NoEqualsTwoBars.Match(pipeCleanedCitation);

        if (malformedContent.Success)
        {
            found.Add(
                citationMatch.Index + malformedContent.Index,
                malformedContent.Length);
        }
    }

    /// <summary>
    /// Finds citation URL values containing unformatted spaces.
    /// </summary>
    /// <param name="citationMatch">
    /// Match containing the complete citation template.
    /// </param>
    /// <param name="found">
    /// Collection that receives the source index and length of the invalid URL
    /// value.
    /// </param>
    private static void FindUrlContainingSpaces(
        Match citationMatch,
        Dictionary<int, int> found)
    {
        int urlParameterPosition =
            citationMatch.Value.IndexOf("url", StringComparison.Ordinal);

        if (urlParameterPosition <= 0)
            return;

        string url =
            Tools.GetTemplateParameterValue(citationMatch.Value, "url");

        if (!ContainsUnformattedUrlSpace(url))
            return;

        // The URL value may occur in an earlier parameter as well, so search
        // from the detected URL parameter name to identify the correct instance.
        string citationFromUrlParameter =
            citationMatch.Value.Substring(urlParameterPosition);

        // TODO: Verify the located URL value before adding its source position.
        // IndexOf returns -1 if the extracted value cannot be found in the expected
        // portion of the citation template.
        int urlValuePosition =
            citationFromUrlParameter.IndexOf(
                url,
                StringComparison.Ordinal);

        found.Add(
            citationMatch.Index +
            urlParameterPosition +
            urlValuePosition,
            url.Length);
    }

    /// <summary>
    /// Determines whether a URL value contains spaces outside nested templates
    /// or other protected wiki text.
    /// </summary>
    /// <param name="url">Citation URL parameter value.</param>
    /// <returns>
    /// <see langword="true"/> when the value contains an unformatted space;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool ContainsUnformattedUrlSpace(string url)
    {
        if (!url.Contains(" "))
            return false;

        string unformattedUrl =
            WikiRegexes.UnformattedText.Replace(
                WikiRegexes.NestedTemplates.Replace(url, string.Empty),
                string.Empty);

        return unformattedUrl
            .Trim()
            .Contains(" ");
    }
}