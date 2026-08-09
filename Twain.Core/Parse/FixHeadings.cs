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

// TODO: Review heading regex anchoring and call sites during a future
// regex-consolidation pass. Some expressions are line-anchored while
// equivalent heading corrections are not.
//
// TODO: Standardize case-insensitive heading matching after adding focused
// regression tests for the accepted heading variants.

/// <summary>
/// Matches common variants of the "See also" heading that should be normalized.
/// </summary>
private static readonly Regex RegexHeadingsSeeAlso =
    new(
        "^(== *)(?:see also|related topics|related articles|internal links|also see):?( *==)",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches common variants of the "External links" heading that should be
    /// normalized.
    /// </summary>
    private static readonly Regex RegexHeadingsExternalLink =
        new(
            "(== *)(external links?|external sites?|outside links?|web ?links?|exterior links?):?( *==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches misspelled or otherwise nonstandard variants of the "References"
    /// heading that should be normalized.
    /// </summary>
    // TODO: Consider renaming RegexHeadingsReferencess to RegexHeadingsReferences
    // during the dedicated member-renaming pass.
    private static readonly Regex RegexHeadingsReferencess =
        new(
            "(== *)(?:reff?e?rr?en[sc]es?:?)( *==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches singular or plural variants of the "Sources" heading that should
    /// be normalized.
    /// </summary>
    private static readonly Regex RegexHeadingsSources =
        new(
            "(== *)(?:sources?:?)( *==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches singular or plural variants of the "Further reading" heading that
    /// should be normalized.
    /// </summary>
    private static readonly Regex RegexHeadingsFurtherReading =
        new(
            "(== *)(further readings?:?)( *==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches life-section headings such as "Early Life", "Personal Life",
    /// "Adult Life", and "Later Life".
    /// </summary>
    private static readonly Regex RegexHeadingsLife =
        new(
            "(== *)(Early|Personal|Adult|Later) Life( *==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches membership-section headings such as "Current Members", "Past
    /// Members", and "Prior Members".
    /// </summary>
    private static readonly Regex RegexHeadingsMembers =
        new(
            "(== *)(Current|Past|Prior) Members( *==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches a complete heading whose text is wrapped in wiki or HTML bold
    /// markup.
    /// </summary>
    private static readonly Regex RegexHeadingsBold =
        new(@"^(=+\s*)(?:'''|<[Bb]>)(.*?)(?:'''|</[Bb]>)(\s*=+\s*)$");

    /// <summary>
    /// Matches the "Track listing" heading.
    /// </summary>
    private static readonly Regex RegexHeadingsTrackListing =
        new(
            "(== *)track listing( *==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches the "Life and Career" heading.
    /// </summary>
    private static readonly Regex RegexHeadingsLifeCareer =
        new(
            "(== *)Life and Career( *==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches career headings consisting of a single alphabetic qualifier
    /// followed by "Career".
    /// </summary>
    // TODO: Verify whether RegexHeadingsCareer should intentionally match only
    // single-word career qualifiers before broadening its accepted heading text.
    private static readonly Regex RegexHeadingsCareer =
        new(
            "(== ?)([a-zA-Z]+) Career( ?==)",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches introductory headings at the start of an article that are treated
    /// as unnecessary or nonstandard top-level introductory sections.
    /// </summary>
    private static readonly Regex RegexBadHeaderStartOfArticle =
        new(
            "^={1,4} ?'*(about|description|overview|definition|profile|(?:general )?information|background|intro(?:duction)?|summary|bio(?:graphy)?)'* ?={1,4}",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches a complete heading containing one excess equals sign on each side
    /// so that it can be promoted by one heading level.
    /// </summary>
    private static readonly Regex RegexHeadingUpOneLevel =
        new(
            @"^=(==+[^=].*?[^=]==+)=(\r\n?|\n)$",
            RegexOptions.Multiline);

    // Standard end-of-article headings handled specially when validating or
    // repairing References, External links, and See also sections.

    /// <summary>
    /// Matches supported References, External links, or See also headings.
    /// </summary>
    private static readonly Regex ReferencesExternalLinksSeeAlso =
        new(@"== *([Rr]eferences|[Ee]xternal +[Ll]inks|[Ss]ee +[Aa]lso) *==\s");

    /// <summary>
    /// Matches a correctly formatted References, External links, or See also
    /// heading at the start of the input.
    /// </summary>
    private static readonly Regex ReferencesExternalLinksSeeAlsoValid =
        new(@"^== *(References|External links|See also) *==\s");

    /// <summary>
    /// Matches a supported References, External links, or See also heading whose
    /// closing heading delimiter is missing one equals sign.
    /// </summary>
    private static readonly Regex ReferencesExternalLinksSeeAlsoUnbalancedRight =
        new(@"(== *(?:[Rr]eferences|[Ee]xternal +[Ll]inks?|[Ss]ee +[Aa]lso) *=) *\r\n");

    /// <summary>
    /// Matches headings whose text ends with a colon immediately before the
    /// closing heading delimiter.
    /// </summary>
    private static readonly Regex RegexHeadingColonAtEnd =
        new(@"^(=+)(\s*[^=\s].*?)\:(\s*\1\s*)$");

    /// <summary>
    /// Matches wiki or HTML bold markup occurring within heading delimiters.
    /// </summary>
    // TODO: Review whether heading-format cleanup can eventually be expressed
    // using parsed heading boundaries rather than variable-length regex
    // lookarounds.
    private static readonly Regex RegexHeadingWithBold =
        new(@"(?<====+.*?)(?:'''|<[Bb]>)(.*?)(?:'''|</[Bb]>)(?=.*?===+)");

    /// <summary>
    /// Contains text fragments used to identify headings that may require
    /// additional normalization or validation.
    /// </summary>
    /// <remarks>
    /// Entries are matching indicators rather than a definitive list of invalid
    /// heading names.
    /// </remarks>
    // TODO: Rename BadHeadings during the member-renaming pass to better reflect
    // that it contains trigger fragments rather than complete invalid headings.
    //
    // TODO: Review whether this collection should remain a mutable List<string>
    // after its call sites have been examined.
    private static readonly List<string> BadHeadings =
        new(
            new[]
            {
            "career",
            "track listing",
            " members",
            "further reading",
            "related ",
            " life",
            "source",
            " links",
            "weblink",
            "external",
            "also",
            "reff",
            "refer",
            "refr",
            "<",
            "\t",
            "'''",
            ":"
            });

    /// <summary>
    /// Fix ==See also== and similar section common errors.
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">the title of the article</param>
    /// <param name="noChange">Value that indicates whether no change was made.</param>
    /// <returns>The modified article text.</returns>
    public static string FixHeadings(string articleText, string articleTitle, out bool noChange)
    {
        string newText = FixHeadings(articleText, articleTitle);

        noChange = newText.Equals(articleText);

        return newText.Trim();
    }

    /// <summary>
    /// Matches text beginning with "List of" or "Lists of".
    /// </summary>
    // TODO: Review the call sites during the member-renaming pass to determine
    // whether ListOf should be given a more descriptive name.
    private static readonly Regex ListOf =
        new(
            @"^Lists? of",
            RegexOptions.Compiled);

    /// <summary>
    /// Matches an anchor-related template followed by one or more blank lines
    /// immediately before a level-two heading.
    /// </summary>
    /// <remarks>
    /// Supports the Anchor, Anchors, Anchor for redirect, ANCHOR, and Anc
    /// template names through <see cref="Tools.NestedTemplateRegex(string[])"/>.
    /// </remarks>
    // TODO: Review whether these anchor-template aliases should eventually be
    // centralized if the same aliases are maintained elsewhere.
    private static readonly Regex Anchor2NewlineHeader =
        new(
            Tools.NestedTemplateRegex(
                new[]
                {
                "Anchor",
                "Anchors",
                "Anchor for redirect",
                "ANCHOR",
                "Anc"
                }) +
            "\r\n(\r\n)+==",
            RegexOptions.Multiline);

    /// <summary>
    /// Matches a heading delimiter preceded by whitespace, excessive blank lines,
    /// or an HTML line-break element that requires normalization.
    /// </summary>
    // TODO: Review this expression together with its replacement logic before
    // simplifying the whitespace or HTML line-break handling.
    //
    // TODO: Review the CRLF-specific handling during a future behavior-focused
    // pass before adding support for other line-ending styles.
    private static readonly Regex HeadingsIncorrectWhitespaceBefore =
        new(@"(?<=\S *(?:(\r\n){3,}|\r\n|\s*< *[Bb][Rr] *\/? *>\s*) *)=");

    /// <summary>
    /// Matches a heading followed by one or more headings that use an additional
    /// heading level.
    /// </summary>
    // TODO: Add focused regression tests for nested heading sequences before
    // modifying or simplifying this expression.
    //
    // TODO: Review this regex together with its replacement logic to document
    // precisely how the matched heading levels are adjusted.
    private static readonly Regex HeadingSubHeading =
        new(
            @"^(==+)(.*)\1((?:\r\n\1=+.*\1=+\s*)+)\r\n",
            RegexOptions.Multiline);

    /// <summary>
    /// Matches a heading that immediately follows the closing delimiter of an
    /// HTML comment.
    /// </summary>
    // TODO: Review whether the CRLF-specific line-ending requirement is
    // intentional before broadening this expression.
    private static readonly Regex CommentThenHeading =
        new(@"-->\r\n={1,6}(.*?)={1,6}");

    /// <summary>
    /// Matches a level-one MediaWiki heading consisting of a single equals sign
    /// on each side of the heading text.
    /// </summary>
    /// <remarks>
    /// The heading may be followed by the existing numeric marker sequence or an
    /// HTML comment before the end of the line.
    /// </remarks>
    // TODO: Document the purpose of the "⌊⌊⌊⌊...⌋⌋⌋⌋" marker sequence after
    // reviewing the code that creates and consumes it.
    //
    // TODO: Add focused regression tests before changing the lookahead or the
    // handling of trailing comments and marker text.
    private static readonly Regex HeadingLevelOne =
        new(
            @"^=([^=](?:.*?[^=])?)=(?=(?: *⌊⌊⌊⌊\d{1,4}⌋⌋⌋⌋| *<!--.*?-->)?\s*$)",
            RegexOptions.Multiline);

    /// <summary>
    /// Matches adjacent duplicate headings that use the same heading level and
    /// identical heading text.
    /// </summary>
    /// <remarks>
    /// Supports MediaWiki heading levels represented by two through six equals
    /// signs.
    /// </remarks>
    // TODO: Review duplicate-heading detection and repair together before
    // simplifying this expression or extracting the behavior into another helper.
    //
    // TODO: Add regression tests covering duplicate headings at each supported
    // heading level before changing the capture-group structure.
    private static readonly Regex DuplicatedSameLevelHeadings =
        new(
            @"^((={2,6}) *([^=\r\n<>]+) *\2)\s+\2 *\3 *\2",
            RegexOptions.Multiline);

    // Covered by: FormattingTests.TestFixHeadings(), incomplete
    /// <summary>
    /// Fix ==See also== and similar section common errors. Removes unnecessary introductory headings and cleans excess whitespace (but not the optional single space at the start & end of headings).
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">the title of the article</param>
    /// <returns>The modified article text.</returns>
    public static string FixHeadings(string articleText, string articleTitle)
    {
        // remove unnecessary general header from start of article
        articleText = RegexBadHeaderStartOfArticle.Replace(articleText, string.Empty);

        // remove identical duplicated headings with only whitespace in between
        articleText = DuplicatedSameLevelHeadings.Replace(articleText, "$1");

        // one blank line before each heading per MOS:HEAD, but not between headings
        // avoid special case of indented text that may be code with lots of == that matches a heading
        if (Variables.IsWikipediaEN)
        {
            // Check for performance
            if (HeadingsIncorrectWhitespaceBefore.IsMatch(articleText))
            {
                // list of headings that have a comment on the line before: these are correct as is
                List<string> commentBeforeHeadings = CommentThenHeading.Matches(articleText).Cast<Match>().Select(match => match.Groups[1].Value).ToList();

                articleText = WikiRegexes.HeadingsWhitespaceBefore.Replace(articleText, match =>
                    {
                        // avoid special case of indented text that may be code with lots of == that matches a heading
                        if (match.Groups[2].Value.Contains("=="))
                            return match.Value;

                        // if a sub-heading directly after a heading don't add blank line
                        foreach (Match subHeadingMatch in HeadingSubHeading.Matches(articleText))
                        {
                            if (subHeadingMatch.Groups[3].Value.Contains(match.Groups[1].Value))
                                return match.Value;
                        }

                        // if comment on the line before heading then it's correct as is
                        if (commentBeforeHeadings.Any(heading => heading.Equals(match.Groups[2].Value)))
                            return match.Value;

                        return "\r\n\r\n" + match.Groups[1].Value;
                    });

                articleText = Anchor2NewlineHeader.Replace(articleText, match => match.Value.Replace("\r\n\r\n==", "\r\n=="));
            }
        }

        // Get all the custom headings, ignoring normal References, External links, See also sections with correct capitalization
        List<string> customHeadings =
            (from Match headingMatch in WikiRegexes.Headings.Matches(articleText)
             where !ReferencesExternalLinksSeeAlsoValid.IsMatch(headingMatch.Value)
             select headingMatch.Value.ToLower())
            .ToList();

        // Removes level 2 heading (at start of article only) if it matches pagetitle
        if (customHeadings.Any(heading => heading.Contains(articleTitle.ToLower())))
        {
            articleText = Regex.Replace(
                articleText,
                @"^\s*(==) *" + Regex.Escape(articleTitle) + @" *\1\r\n",
                "");
        }

        // Performance: apply fixes to all headings only if a custom heading matches
        // one of the known normalization trigger words.
        if (customHeadings.Any(heading => BadHeadings.Any(heading.Contains)))
        {
            articleText = WikiRegexes.Headings.Replace(articleText, FixHeadingsME);
        }

        // CHECKWIKI error 8. Add missing = in some headers.
        if (customHeadings.Any(heading => Regex.Matches(heading, "=").Count == 3))
        {
            articleText = ReferencesExternalLinksSeeAlsoUnbalancedRight.Replace(
                articleText,
                "$1=\r\n");
        }

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Section_header_level_.28WikiProject_Check_Wikipedia_.237.29
        // CHECKWIKI error 7
        // if no level 2 heading in article, remove a level from all headings (i.e. '===blah===' to '==blah==' etc.)
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Standard_level_2_headers
        // don't consider the "references", "see also", or "external links" level 2 headings when counting level two headings
        // only apply if all level 3 headings and lower are before the first of references/external links/see also
        if (Namespace.IsMainSpace(articleTitle))
        {
            if (!customHeadings.Any(heading => WikiRegexes.HeadingLevelTwo.IsMatch(heading)))
            {
                string articleTextWithoutStandardHeadings = articleText;
                articleTextWithoutStandardHeadings = ReferencesExternalLinksSeeAlso.Replace(articleTextWithoutStandardHeadings, string.Empty);

                string previousArticleText = string.Empty;
                while (!previousArticleText.Equals(articleText))
                {
                    previousArticleText = articleText;
                    if (!WikiRegexes.HeadingLevelTwo.IsMatch(articleTextWithoutStandardHeadings))
                    {
                        // get index of last level 3+ heading
                        int lastHeadingToPromoteIndex = 0;
                        foreach (Match match in RegexHeadingUpOneLevel.Matches(articleText))
                        {
                            if (match.Index > lastHeadingToPromoteIndex)
                                lastHeadingToPromoteIndex = match.Index;
                        }

                        if (!ReferencesExternalLinksSeeAlso.IsMatch(articleText) || (lastHeadingToPromoteIndex < ReferencesExternalLinksSeeAlso.Match(articleText).Index))
                            articleText = RegexHeadingUpOneLevel.Replace(articleText, "$1$2");
                    }

                    articleTextWithoutStandardHeadings = ReferencesExternalLinksSeeAlso.Replace(articleText, string.Empty);
                }
            }

            // level 1 headings to level 2 on mainspace
            if (Namespace.IsMainSpace(articleTitle) && customHeadings.Any(heading => HeadingLevelOne.IsMatch(heading)))
                articleText = HeadingLevelOne.Replace(articleText, "==$1==");
        }

        return articleText;
    }

    /// <summary>
    /// Matches one or more spaces immediately before trailing whitespace at the
    /// end of the input.
    /// </summary>
    // TODO: Review the call site during the member-renaming pass to determine
    // whether SpaceNewLineEnd should be given a more descriptive name.
    private static readonly Regex SpaceNewLineEnd =
        new(@" +(\s+)$");

    /// <summary>
    /// Normalizes formatting and common naming issues within a matched heading.
    /// </summary>
    /// <param name="headingMatch">
    /// The heading match to normalize.
    /// </param>
    /// <returns>
    /// The normalized heading text.
    /// </returns>
    /// <remarks>
    /// The order of the individual cleanup operations is significant and should
    /// be preserved unless behavior is covered by focused regression tests.
    /// </remarks>
    // TODO: Add focused regression tests that verify replacement ordering before
    // consolidating or reordering any of the normalization operations.
    private static string FixHeadingsME(Match headingMatch)
    {
        string hAfter = NormalizeHeadingWhitespaceAndMarkup(headingMatch.Value);
        hAfter = RemoveInvalidHeadingFormatting(hAfter);
        hAfter = NormalizeHeadingNames(hAfter);
        hAfter = RemoveNestedHeadingBold(hAfter);

        return WikiRegexes.EmptyBold.Replace(hAfter, string.Empty);
    }

    /// <summary>
    /// Removes preliminary markup and normalizes whitespace before other heading
    /// corrections are applied.
    /// </summary>
    /// <param name="heading">
    /// The heading text to normalize.
    /// </param>
    /// <returns>
    /// The heading text with preliminary markup and whitespace normalized.
    /// </returns>
    private static string NormalizeHeadingWhitespaceAndMarkup(string heading)
    {
        string normalizedHeading = WikiRegexes.Br.Replace(heading, string.Empty);
        normalizedHeading = WikiRegexes.Big.Replace(normalizedHeading, "$1").TrimStart(' ');

        normalizedHeading = normalizedHeading.Replace("\t", " ");

        while (SpaceNewLineEnd.IsMatch(normalizedHeading))
            normalizedHeading = SpaceNewLineEnd.Replace(normalizedHeading, "$1");

        return normalizedHeading;
    }

    /// <summary>
    /// Removes heading formatting that is not valid or necessary.
    /// </summary>
    /// <param name="heading">
    /// The heading text to normalize.
    /// </param>
    /// <returns>
    /// The heading text with invalid formatting removed.
    /// </returns>
    private static string RemoveInvalidHeadingFormatting(string heading)
    {
        // Removes bold from heading - CHECKWIKI error 44.
        heading = RegexHeadingsBold.Replace(heading, "$1$2$3");

        // Removes colon at end of heading - CHECKWIKI error 57.
        heading = RegexHeadingColonAtEnd.Replace(heading, "$1$2$3");

        return heading;
    }

    /// <summary>
    /// Normalizes known heading names and capitalization variants.
    /// </summary>
    /// <param name="heading">
    /// The heading text to normalize.
    /// </param>
    /// <returns>
    /// The heading text with recognized heading names normalized.
    /// </returns>
    private static string NormalizeHeadingNames(string heading)
    {
        heading = RegexHeadingsExternalLink.Replace(heading, "$1External links$3");

        heading = RegexHeadingsFurtherReading.Replace(heading, "$1Further reading$3");
        heading = RegexHeadingsLife.Replace(heading, "$1$2 life$3");
        heading = RegexHeadingsMembers.Replace(heading, "$1$2 members$3");
        heading = RegexHeadingsTrackListing.Replace(heading, "$1Track listing$2");
        heading = RegexHeadingsLifeCareer.Replace(heading, "$1Life and career$2");
        heading = RegexHeadingsCareer.Replace(heading, "$1$2 career$3");
        heading = RegexHeadingsSeeAlso.Replace(heading, "$1See also$2");

        // Plural per [[WP:FNNR]].
        heading = RegexHeadingsReferencess.Replace(
            heading,
            headingNameMatch =>
                headingNameMatch.Groups[1].Value +
                "References" +
                headingNameMatch.Groups[2].Value);

        heading = RegexHeadingsSources.Replace(
            heading,
            headingNameMatch =>
                headingNameMatch.Groups[1].Value +
                "Sources" +
                headingNameMatch.Groups[2].Value);

        return heading;
    }

    /// <summary>
    /// Removes bold markup occurring within lower-level headings where it has no
    /// visible effect.
    /// </summary>
    /// <param name="heading">
    /// The heading text to normalize.
    /// </param>
    /// <returns>
    /// The heading text with unnecessary nested bold markup removed.
    /// </returns>
    private static string RemoveNestedHeadingBold(string heading)
    {
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Bold_text_in_headers
        // Removes bold from level 3 headers and below, as it makes no visible difference.
        return RegexHeadingWithBold.Replace(heading, "$1");
    }
}