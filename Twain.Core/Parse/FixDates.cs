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

namespace Twain.Core.Parse;

/// <summary>
/// Provides functions for editing wiki text, such as formatting and re-categorization.
/// </summary>
public partial class Parsers
{
    /// <summary>
    /// Matches a month followed by "of" and a four-digit year so the unnecessary
    /// "of" can be removed from date expressions.
    /// </summary>
    /// <remarks>
    /// Excludes constructions where changing the text could produce unnatural
    /// wording, such as "in the June of 2007".
    /// </remarks>
    private static readonly Regex OfBetweenMonthAndYear =
        new(@"\b" + WikiRegexes.Months + @" +of +(20\d\d|1[89]\d\d)\b(?<!\b[Tt]he {1,5}\w{3,15} {1,5}of {1,5}(20\d\d|1[89]\d\d))");

    /// <summary>
    /// Matches American-style dates containing ordinal day suffixes, including
    /// an optional second day in a date range.
    /// </summary>
    /// <remarks>
    /// Excludes date-like text in constructions where removing the ordinal suffix
    /// could alter the intended wording.
    /// </remarks>
    private static readonly Regex OrdinalsInDatesAm =
        new(@"(?<!\b[1-3]\d +)\b" + WikiRegexes.Months + @" +([0-3]?\d)(?:st|nd|rd|th)\b(?<!\b[Tt]he +\w{3,10} +(?:[0-3]?\d)(?:st|nd|rd|th)\b)(?:( *(?:to|and|.|&.dash;) *[0-3]?\d)(?:st|nd|rd|th)\b)?");

    /// <summary>
    /// Matches American-style dates containing ordinal day suffixes, including
    /// date ranges containing one or more additional ordinal days.
    /// </summary>
    /// <remarks>
    /// This expression is used when ordinal suffixes must be removed throughout
    /// an American-style date or date range.
    /// </remarks>
    private static readonly Regex OrdinalsInDatesAmRange =
        new(@"(?<!\b[1-3]\d +)\b" + WikiRegexes.Months + @" +([0-3]?\d)(?:st|nd|rd|th)\b(?<!\b[Tt]he +\w{3,10} +(?:[0-3]?\d)(?:st|nd|rd|th)\b)(?:( *(?:to|and|.|&.dash;) *[0-3]?\d)(?:st|nd|rd|th)\b)*");

    /// <summary>
    /// Matches international-style dates containing ordinal day suffixes,
    /// optionally including a preceding day range.
    /// </summary>
    /// <remarks>
    /// Excludes constructions where the ordinal appears to be part of descriptive
    /// text rather than a conventional date.
    /// </remarks>
    private static readonly Regex OrdinalsInDatesInt =
        new(@"(?:\b([0-3]?\d)(?:st|nd|rd|th)( *(?:to|and|.|&.dash;) *))?\b([0-3]?\d)(?:st|nd|rd|th) +" + WikiRegexes.Months + @"\b(?<!\b[Tt]he +(?:[0-3]?\d)(?:st|nd|rd|th) +\w{3,10})");

    /// <summary>
    /// Matches leading zeros on day numbers in American-style dates.
    /// </summary>
    private static readonly Regex DateLeadingZerosAm =
        new(@"(?<!\b[0-3]?\d *)\b" + WikiRegexes.Months + @" +0([1-9])" + @"\b");

    /// <summary>
    /// Matches leading zeros on day numbers in international-style dates.
    /// </summary>
    private static readonly Regex DateLeadingZerosInt =
        new(@"\b" + @"0([1-9]) +" + WikiRegexes.Months + @"\b");

    /// <summary>
    /// Matches a month name followed by up to 30 additional characters.
    /// </summary>
    /// <remarks>
    /// Used as a performance filter so date cleanup can operate on small portions
    /// of article text rather than repeatedly processing the entire article.
    /// Requires a word boundary immediately after the month name.
    /// </remarks>
    private static readonly Regex MonthsRegex =
        new(@"\b" + WikiRegexes.MonthsNoGroup + @"\b.{0,30}");

    /// <summary>
    /// Matches a month name followed by up to 30 additional characters without
    /// requiring a word boundary immediately after the month name.
    /// </summary>
    /// <remarks>
    /// Used as a broader performance filter when searching for text that may
    /// contain date-formatting issues.
    /// </remarks>
    private static readonly Regex MonthsRegexNoSecondBreak =
        new(@"\b" + WikiRegexes.MonthsNoGroup + @".{0,30}");

    /// <summary>
    /// Matches ordinal day-of-month expressions using the form
    /// "day of month", such as "6th of October".
    /// </summary>
    /// <remarks>
    /// Excludes expressions immediately preceded by "the", where rewriting the
    /// ordinal may produce unnatural prose.
    /// </remarks>
    private static readonly Regex DayOfMonth =
        new(@"(?<![Tt]he +)\b([1-9]|[12][0-9]|3[01])(?:st|nd|rd|th) +of +" + WikiRegexes.Months);

    /// <summary>
    /// Matches a numeric ordinal suffix such as "1st", "2nd", "3rd", or "4th".
    /// </summary>
    private static readonly Regex Ordinal =
        new(@"[0-9](?:st|nd|rd|th)");

    /// <summary>
    /// Matches English month names immediately followed by "Act".
    /// </summary>
    /// <remarks>
    /// Used to prevent date cleanup from altering names of legislation such as
    /// "June Act".
    /// </remarks>
    private static readonly Regex MonthsAct =
        new(@"\b(?:January|February|March|April|May|June|July|August|September|October|November|December) Act\b");

    /// <summary>
    /// Matches an ordinal suffix contained within HTML <c>&lt;sup&gt;</c> tags.
    /// </summary>
    /// <remarks>
    /// Used to normalize forms such as <c>1&lt;sup&gt;st&lt;/sup&gt;</c> before
    /// subsequent date-ordinal processing.
    /// </remarks>
    private static readonly Regex SupOrdinal =
        new(@"([0-9])<sup> ?(st|nd|rd|th) ?</sup>", RegexOptions.IgnoreCase);

    /// <summary>
    /// Quickly identifies text portions that may contain date ordinals, leading
    /// zeros, or removable uses of "of".
    /// </summary>
    /// <remarks>
    /// This is a preliminary performance filter. A match indicates that the text
    /// may require date cleanup; the more specific expressions perform the actual
    /// transformations.
    /// </remarks>
    private static readonly Regex FixDateOrdinalsAndOfQuick =
        new(@"[0-9](?:<sup> *)?(st|nd|rd|th)|\b0[1-9]\b| of +([0-9]|[A-Z])", RegexOptions.IgnoreCase);

    // Covered by TestFixDateOrdinalsAndOf
    /// <summary>
    /// Removes ordinals, leading zeros from dates and 'of' between a month and a year, per [[WP:MOSDATE]]; on en wiki only
    /// </summary>
    /// <param name="articleText">The wiki text of the article</param>
    /// <param name="articleTitle">The article's title</param>
    /// <returns>The modified article text.</returns>
    public string FixDateOrdinalsAndOf(string articleText, string articleTitle)
    {
        if (!Variables.LangCode.Equals("en"))
            return articleText;

        bool monthsInTitle = MonthsRegex.IsMatch(articleTitle);

        for (; ; )
        {
            bool reparse = false;
            // performance: better to loop through all instances of dates and apply regexes to those than
            // to apply regexes to whole article text
            // Secondly: filter down only to those portions that could be changed
            List<Match> monthsm = (from Match m in MonthsRegex.Matches(articleText) select m).Where(m =>
                FixDateOrdinalsAndOfQuick.IsMatch(articleText.Substring(m.Index - Math.Min(25, m.Index), Math.Min(25, m.Index) + m.Length))).ToList();

            foreach (Match m in monthsm)
            {
                // take up to 25 characters before match, unless match within first 25 characters of article
                string before = articleText.Substring(m.Index - Math.Min(25, m.Index), Math.Min(25, m.Index) + m.Length);

                if (MonthsAct.IsMatch(before))
                    continue;

                string after = FixDateOrdinalsAndOfLocal(before, monthsInTitle);

                // check substring as do not want to change start of string as we could have broken up a year etc. by taking exactly 25 characters
                if (!after.Equals(before) && after.Substring(0, 1).Equals(before.Substring(0, 1)))
                {
                    reparse = true;
                    articleText = articleText.Replace(before, after);

                    // catch after other fixes
                    articleText = IncorrectCommaAmericanDates.Replace(articleText, @"$1 $2, $3");
                    articleText = IncorrectCommaInternationalDates.Replace(articleText, @"$1 $2");

                    break;
                }
            }
            if (!reparse)
                break;
        }

        return articleText;
    }

    /// <summary>
    /// Applies ordinal, leading-zero, and month/year wording corrections to a
    /// localized portion of article text.
    /// </summary>
    /// <param name="textPortion">
    /// The portion of wiki text to inspect and modify.
    /// </param>
    /// <param name="monthsInTitle">
    /// Indicates whether the article title contains a month name. When
    /// <see langword="true"/>, ordinal-date cleanup is skipped to avoid changing
    /// text that may be part of a proper name or title.
    /// </param>
    /// <returns>
    /// The text portion with supported date-formatting corrections applied.
    /// </returns>
    private string FixDateOrdinalsAndOfLocal(string textPortion, bool monthsInTitle)
    {
        textPortion = OfBetweenMonthAndYear.Replace(textPortion, "$1 $2");

        // Skip ordinal cleanup when the article title contains a month name,
        // since date-like wording may be part of a proper name
        // (for example, [[6th of October City]]).
        //
        // Check for a possible ordinal first to avoid running the more specific
        // ordinal expressions when no relevant text is present.
        if (!monthsInTitle &&
            Regex.IsMatch(
                textPortion,
                @"[0-9](?:<sup> *)?(st|nd|rd|th)",
                RegexOptions.IgnoreCase))
        {
            // Remove <sup> formatting from ordinal suffixes before applying
            // the date-specific ordinal corrections.
            // CHECKWIKI error 101; see [[WP:ORDINAL]].
            textPortion = SupOrdinal.Replace(textPortion, @"$1$2");

            textPortion = OrdinalsInDatesAmRange.Replace(
                textPortion,
                m => Regex.Replace(
                    m.Value,
                    @"\b([1-3]?[0-9])(?:st|nd|rd|th)\b",
                    "$1"));

            textPortion = OrdinalsInDatesInt.Replace(
                textPortion,
                "$1$2$3 $4");

            textPortion = DayOfMonth.Replace(
                textPortion,
                "$1 $2");
        }

        textPortion = DateLeadingZerosAm.Replace(textPortion, "$1 $2");

        return DateLeadingZerosInt.Replace(textPortion, "$1 $2");
    }

    /// <summary>
    /// Matches American-style dates containing incorrect comma or spacing
    /// between the month, day, and year.
    /// </summary>
    /// <remarks>
    /// Supports single days and same-month day ranges.
    /// </remarks>
    private static readonly Regex IncorrectCommaAmericanDates =
        new(WikiRegexes.Months + @"[ ,]*([1-3]?\d(?:–[1-3]?\d)?)[ ,]+([12]\d{3})\b");

    /// <summary>
    /// Matches international-style dates containing an incorrect comma between
    /// the month and year.
    /// </summary>
    private static readonly Regex IncorrectCommaInternationalDates =
        new(@"\b((?:[1-3]?\d) +" + WikiRegexes.MonthsNoGroup + @") *, *(1\d{3}|20\d{2})\b", RegexOptions.Compiled);

    /// <summary>
    /// Matches same-month international-style date ranges that use a hyphen
    /// instead of an en dash.
    /// </summary>
    /// <remarks>
    /// Date ranges are normalized to use an en dash in accordance with
    /// Wikipedia date-formatting guidance.
    /// </remarks>
    private static readonly Regex SameMonthInternationalDateRange =
        new(@"( [1-3]?\d) *- *([1-3]?\d +" + WikiRegexes.MonthsNoGroup + @")\b", RegexOptions.Compiled);

    /// <summary>
    /// Matches same-month American-style date ranges that use a hyphen instead
    /// of an en dash.
    /// </summary>
    private static readonly Regex SameMonthAmericanDateRange =
        new(@"(" + WikiRegexes.MonthsNoGroup + @" *)([0-3]?\d) *- *([0-3]?\d)\b(?!\-)", RegexOptions.Compiled);

    /// <summary>
    /// Matches long-form international date ranges that repeat the month name,
    /// such as "13 July - 28 July 2009".
    /// </summary>
    /// <remarks>
    /// Used to normalize the range to the compact form
    /// "13–28 July 2009".
    /// </remarks>
    private static readonly Regex LongFormatInternationalDateRange =
        new(@"\b([1-3]?\d) +" + WikiRegexes.Months + @" *(?:-|–|&nbsp;) *([1-3]?\d) +\2,? *([12]\d{3})\b", RegexOptions.Compiled);

    /// <summary>
    /// Matches long-form American date ranges that repeat the month name,
    /// such as "July 13 - July 28 2009".
    /// </summary>
    /// <remarks>
    /// Used to normalize the range to the compact form
    /// "July 13–28, 2009".
    /// </remarks>
    private static readonly Regex LongFormatAmericanDateRange =
        new(WikiRegexes.Months + @" +([1-3]?\d) +" + @" *(?:-|–|&nbsp;) *\1 +([1-3]?\d) *,? *([12]\d{3})\b", RegexOptions.Compiled);

    /// <summary>
    /// Matches ranges between two month names that use a hyphen instead of an
    /// en dash.
    /// </summary>
    private static readonly Regex EnMonthRange =
        new(@"\b" + WikiRegexes.Months + @"-" + WikiRegexes.Months + @"\b", RegexOptions.Compiled);

    /// <summary>
    /// Matches full four-digit year ranges written with a hyphen in supported
    /// textual contexts.
    /// </summary>
    /// <remarks>
    /// The surrounding context is captured so the replacement logic can preserve
    /// introductory punctuation or words such as "from", "between", "reigned",
    /// "for", "ca.", or "circa".
    /// </remarks>
    private static readonly Regex FullYearRange =
        new(
            @"((?:[\(,=;\|]|\b(?:from|between|and|reigned|f?or|ca?\.?\]*|circa)) *)([12]\d{3}) *- *([12]\d{3})(?= *(?:\)|[,;\|]|and\b|\s*$))",
            RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Matches full four-digit year ranges that already use an en dash but
    /// contain unwanted spacing around it.
    /// </summary>
    /// <remarks>
    /// Excludes ranges immediately preceded by supported circa expressions.
    /// </remarks>
    private static readonly Regex SpacedFullYearRange =
        new(
            @"(?<!\b(?:ca?\.?\]*|circa) *)([12]\d{3})(?: +– *| *– +)([12]\d{3})",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches year ranges where the ending year is abbreviated to two digits,
    /// such as "1965-68".
    /// </summary>
    /// <remarks>
    /// The replacement logic validates that the abbreviated ending year follows
    /// the starting year before normalizing the range.
    /// </remarks>
    private static readonly Regex YearRangeShortenedCentury =
        new(
            @"((?:[\(,=;]|\b(?:from|between|and|reigned|the)) *)([12]\d{3}) *- *(\d{2})(?= *(?:\)|[,;]|and\b|\s*$))",
            RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Matches date-and-year ranges ending in "present", where the range is
    /// written with a hyphen.
    /// </summary>
    private static readonly Regex DateRangeToPresent =
        new(
            @"\b(" + WikiRegexes.MonthsNoGroup + @"| [0-3]?\d,?) +" +
            @"([12]\d{3}) *- *([Pp]resent\b)",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches a full date followed immediately by a year range without the
    /// required spacing around the dash.
    /// </summary>
    /// <remarks>
    /// Handles forms such as "11 May 2010–2012", which should use spacing
    /// between the complete date and the ending year.
    /// </remarks>
    private static readonly Regex DateRangeToYear =
        new(
            @"\b(" + WikiRegexes.MonthsNoGroup + @"|\b" +
            WikiRegexes.MonthsNoGroup +
            @"(?:&nbsp;|\s+)[0-3]?\d,?) +" +
            @"([12]\d{3})[-–]([12]\d{3})\b",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches a four-digit year followed by up to 25 additional characters.
    /// </summary>
    /// <remarks>
    /// Used as a performance filter so year-range cleanup can inspect localized
    /// portions of article text rather than repeatedly processing the entire
    /// article.
    /// </remarks>
    private static readonly Regex YearRange =
        new(@"\b[12][0-9]{3}.{0,25}");

    /// <summary>
    /// Matches four-digit year ranges ending in "present" that use a hyphen.
    /// </summary>
    private static readonly Regex YearRangeToPresent =
        new(@"\b([12]\d{3}) *- *([Pp]resent\b)", RegexOptions.Compiled);

    /// <summary>
    /// Quickly detects a four-digit year immediately followed by either a
    /// hyphen or an en dash.
    /// </summary>
    /// <remarks>
    /// Used as a preliminary check before applying the more specific full-date
    /// range expressions.
    /// </remarks>
    private static readonly Regex YearDash =
        new(@"[12]\d{3}[–-]");

    /// <summary>
    /// Matches two complete international-style dates separated by an unspaced
    /// hyphen or en dash.
    /// </summary>
    private static readonly Regex InternationalDateFullUnspacedRange =
        new(WikiRegexes.InternationalDates + @"[–-]" + WikiRegexes.InternationalDates);

    /// <summary>
    /// Matches two complete American-style dates separated by an unspaced
    /// hyphen or en dash.
    /// </summary>
    private static readonly Regex AmericanDateFullUnspacedRange =
        new(WikiRegexes.AmericanDates + @"[–-]" + WikiRegexes.AmericanDates);

    /// <summary>
    /// Fix date and decade formatting errors: commas in American/international dates, full date ranges, month ranges
    /// </summary>
    /// <param name="articleText"></param>
    /// <returns></returns>
    public string FixDatesA(string articleText)
    {
        if (!Variables.LangCode.Equals("en"))
            return articleText;

        /* performance check: on most articles no date changes, on long articles HideMore is slow, so if no changes to raw text
         * don't need to perform actual check on HideMore text, and this is faster overall
         * Secondly: faster to apply regexes to each date found than to apply regexes to whole article text
         */
        bool changes = false;
        foreach (Match m in MonthsRegexNoSecondBreak.Matches(articleText))
        {
            // take up to 25 characters before match, unless match within first 25 characters of article
            string before = articleText.Substring(m.Index - Math.Min(25, m.Index), Math.Min(25, m.Index) + m.Length);

            string after = FixDatesAInternal(before);

            if (!after.Equals(before))
            {
                changes = true;
                break;
            }
        }

        if (!changes)
            return articleText;

        articleText = HideTextImages(articleText);

        articleText = FixDatesAInternal(articleText);

        return AddBackTextImages(articleText);
    }

    /// <summary>
    /// Applies comma, date-range, and month-range formatting corrections to a
    /// portion of wiki text.
    /// </summary>
    /// <param name="textPortion">
    /// The portion of wiki text to inspect and modify.
    /// </param>
    /// <returns>
    /// The corrected text portion, or the original text when a proposed change
    /// would modify a wiki-link target.
    /// </returns>
    /// <remarks>
    /// Date-range corrections are applied before American-style comma correction
    /// because the range transformations may affect the surrounding punctuation.
    /// Wiki-link targets are compared before and after processing to prevent
    /// formatting changes from altering link destinations.
    /// </remarks>
    private string FixDatesAInternal(string textPortion)
    {
        string originaltextPortion = textPortion;

        bool hasDash =
            textPortion.Contains("-") ||
            textPortion.Contains("–");

        bool hasComma = textPortion.Contains(",");

        if (hasComma)
        {
            textPortion = IncorrectCommaInternationalDates.Replace(
                textPortion,
                @"$1 $2");
        }

        if (hasDash)
        {
            textPortion = SameMonthInternationalDateRange.Replace(
                textPortion,
                @"$1–$2");

            textPortion = SameMonthAmericanDateRange.Replace(
                textPortion,
                SameMonthAmericanDateRangeME);

            textPortion = LongFormatInternationalDateRange.Replace(
                textPortion,
                @"$1–$3 $2 $4");

            textPortion = LongFormatAmericanDateRange.Replace(
                textPortion,
                @"$1 $2–$3, $4");
        }

        // Apply this after the date-range corrections because those transformations
        // may affect the comma placement in American-style dates.
        textPortion = IncorrectCommaAmericanDates.Replace(
            textPortion,
            @"$1 $2, $3");

        if (hasDash)
        {
            textPortion = EnMonthRange.Replace(
                textPortion,
                @"$1–$2");
        }

        // Do not accept corrections that change wiki-link targets.
        // Example:
        // [[July 29 1966, P.N.E. Garden Aud., Vancouver Canada]]
        if (!originaltextPortion.Equals(textPortion) &&
            originaltextPortion.Contains("[["))
        {
            List<string> wikiLinkTargetsBefore =
                new(
                    from Match m in WikiRegexes.WikiLinksOnlyPossiblePipe.Matches(textPortion)
                    select m.Groups[1].Value);

            List<string> wikiLinkTargetsAfter =
                new(
                    from Match m in WikiRegexes.WikiLinksOnlyPossiblePipe.Matches(originaltextPortion)
                    select m.Groups[1].Value);

            if (!wikiLinkTargetsBefore.SequenceEqual(wikiLinkTargetsAfter))
            {
                return originaltextPortion;
            }
        }

        return textPortion;
    }

    /// <summary>
    /// Fix date and decade formatting errors: date/year ranges to present, full year ranges, performs floruit term wikilinking
    /// </summary>
    /// <param name="articleText"></param>
    /// <param name="CircaLink"></param>
    /// <param name="Floruit"></param>
    /// <returns></returns>
    public string FixDatesB(string articleText, bool CircaLink, bool Floruit)
    {
        if (!Variables.LangCode.Equals("en"))
            return articleText;

        for (; ; )
        {
            /* performance check: faster to apply regexes to each year/date found
             * than to apply regexes to whole article text
             */
            bool reparse = false;
            foreach (Match m in YearRange.Matches(articleText))
            {
                // take up to 25 characters before match, unless match within first 25 characters of article
                string before = articleText.Substring(m.Index - Math.Min(25, m.Index), Math.Min(25, m.Index) + m.Length);

                string after = FixDatesBInternal(before, CircaLink);

                if (!after.Equals(before))
                {
                    reparse = true;
                    articleText = articleText.Replace(before, after);
                    break;
                }
            }

            if (!reparse)
                break;
        }

        // replace first occurrence of unlinked floruit with linked version, zeroth section only
        if (Floruit)
            articleText = WikiRegexes.UnlinkedFloruit.Replace(articleText, @"([[floruit|fl.]] $1", 1);

        return articleText;
    }

    /// <summary>
    /// Applies year-range and date-range formatting corrections to a portion of
    /// wiki text.
    /// </summary>
    /// <param name="textPortion">
    /// The portion of wiki text to inspect and modify.
    /// </param>
    /// <param name="CircaLink">
    /// Indicates whether circa-link processing is enabled. When enabled, full
    /// four-digit year-range normalization is skipped.
    /// </param>
    /// <returns>
    /// The text portion with supported date and year-range corrections applied.
    /// </returns>
    private string FixDatesBInternal(string textPortion, bool CircaLink)
    {
        textPortion = DateRangeToPresent.Replace(textPortion, @"$1 $2 – $3");
        textPortion = YearRangeToPresent.Replace(textPortion, @"$1–$2");

        // Normalize full year ranges only when circa-link processing is disabled.
        // The match evaluators verify that the ending year follows the starting year.
        if (!CircaLink)
        {
            textPortion = FullYearRange.Replace(textPortion, FullYearRangeME);
            textPortion = SpacedFullYearRange.Replace(textPortion, SpacedFullYearRangeME);
        }

        // Normalize shortened year ranges such as 1965-68.
        textPortion = YearRangeShortenedCentury.Replace(
            textPortion,
            YearRangeShortenedCenturyME);

        // Add spacing when a complete date is followed by a year range.
        // Example: 11 May 2010–2012 -> 11 May 2010 – 2012.
        textPortion = DateRangeToYear.Replace(textPortion, @"$1 $2 – $3");

        // Add spacing around the separator between two complete dates.
        if (YearDash.IsMatch(textPortion))
        {
            textPortion = InternationalDateFullUnspacedRange.Replace(
                textPortion,
                m => m.Value.Replace("-", "–").Replace("–", " – "));

            textPortion = AmericanDateFullUnspacedRange.Replace(
                textPortion,
                m => m.Value.Replace("-", "–").Replace("–", " – "));
        }

        return textPortion;
    }

    /// <summary>
    /// Normalizes a full four-digit year range when the years form a plausible
    /// ascending range.
    /// </summary>
    /// <param name="m">
    /// The regex match containing the surrounding context, starting year, and
    /// ending year.
    /// </param>
    /// <returns>
    /// The normalized year range, or the original match when the range fails
    /// validation.
    /// </returns>
    /// <remarks>
    /// Ranges are changed only when the ending year is later than the starting
    /// year and no more than 300 years later. Ranges associated with circa-style
    /// context use spaces around the en dash.
    /// </remarks>
    private static string FullYearRangeME(Match m)
    {
        int year1 = Convert.ToInt32(m.Groups[2].Value);
        int year2 = Convert.ToInt32(m.Groups[3].Value);

        if (year2 > year1 && year2 - year1 <= 300)
        {
            return m.Groups[1].Value +
                   m.Groups[2].Value +
                   (m.Groups[1].Value.ToLower().Contains("c") ? @" – " : @"–") +
                   m.Groups[3].Value;
        }

        return m.Value;
    }

    /// <summary>
    /// Removes inappropriate spacing around the en dash in a valid full
    /// four-digit year range.
    /// </summary>
    /// <param name="m">
    /// The regex match containing the starting and ending years.
    /// </param>
    /// <returns>
    /// The normalized year range, or the original match when the range fails
    /// validation.
    /// </returns>
    /// <remarks>
    /// Ranges are changed only when the ending year is later than the starting
    /// year and no more than 300 years later.
    /// </remarks>
    private static string SpacedFullYearRangeME(Match m)
    {
        int year1 = Convert.ToInt32(m.Groups[1].Value);
        int year2 = Convert.ToInt32(m.Groups[2].Value);

        if (year2 > year1 && year2 - year1 <= 300)
        {
            return m.Groups[1].Value + @"–" + m.Groups[2].Value;
        }

        return m.Value;
    }

    /// <summary>
    /// Normalizes an abbreviated year range such as <c>1965-68</c> when the
    /// abbreviated ending year forms a valid ascending range.
    /// </summary>
    /// <param name="m">
    /// The regex match containing the surrounding context, full starting year,
    /// and abbreviated ending year.
    /// </param>
    /// <returns>
    /// The normalized year range, or the original match when the range fails
    /// validation.
    /// </returns>
    private static string YearRangeShortenedCenturyME(Match m)
    {
        int year1 = Convert.ToInt32(m.Groups[2].Value);

        // Combine the century from the starting year with the abbreviated
        // ending year: 1965 and 68 -> 1968.
        int year2 = Convert.ToInt32(
            m.Groups[2].Value.Substring(0, 2) + m.Groups[3].Value);

        if (year2 > year1 && year2 - year1 <= 99)
        {
            return m.Groups[1].Value +
                   m.Groups[2].Value +
                   @"–" +
                   m.Groups[3].Value;
        }

        return m.Value;
    }

    /// <summary>
    /// Normalizes the separator in an American-style same-month date range when
    /// the ending day follows the starting day.
    /// </summary>
    /// <param name="m">
    /// The regex match containing the month and starting and ending day numbers.
    /// </param>
    /// <returns>
    /// The date range using an en dash, or the original match when the ending day
    /// does not follow the starting day.
    /// </returns>
    private static string SameMonthAmericanDateRangeME(Match m)
    {
        int day1 = Convert.ToInt32(m.Groups[2].Value);
        int day2 = Convert.ToInt32(m.Groups[3].Value);

        if (day2 > day1)
        {
            return Regex.Replace(m.Value, @" *- *", @"–");
        }

        return m.Value;
    }
}