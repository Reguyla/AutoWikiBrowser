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
    /// Fixes and improves syntax (such as html markup)
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="noChange">Value that indicated whether no change was made.</param>
    /// <returns>The modified article text.</returns>
    public static string FixSyntax(string articleText, out bool noChange)
    {
        string newText = FixSyntax(articleText);

        noChange = newText.Equals(articleText);
        return newText;
    }

    /// <summary>
    /// Matches an external link that begins with an extra opening square bracket,
    /// while allowing balanced square brackets within the link.
    /// </summary>
    private static readonly Regex DoubleBracketAtStartOfExternalLink = new Regex(@"\[\[+(https?:/(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches an external link followed by an extra closing square bracket,
    /// while allowing balanced square brackets within the link.
    /// </summary>
    private static readonly Regex DoubleBracketAtEndOfExternalLink = new Regex(@"(\[ *https?:/(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\])\](?!\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches an external link followed by two extra closing square brackets,
    /// while allowing balanced square brackets within the link.
    /// </summary>
    private static readonly Regex TripleBracketAtEndOfExternalLink = new Regex(@"(\[ *https?:/(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\])\]\](?!\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches an external link with an extra closing square bracket when the
    /// link is followed by the closing brackets of an image or wiki-link context.
    /// </summary>
    private static readonly Regex DoubleBracketAtEndOfExternalLinkWithinImage = new Regex(@"(\[https?:/(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!)))\](?=\]{3})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches a list-item external link whose content ends with a closing
    /// parenthesis before the end of the line.
    /// </summary>
    /// <remarks>
    /// TODO: Verify whether the closing parenthesis is intentional. The current
    /// field name refers to a curly brace, while the expression matches <c>\)</c>.
    /// Rename the field in a later cleanup pass if the name is confirmed to be stale.
    /// </remarks>
    private static readonly Regex ListExternalLinkEndsCurlyBrace = new Regex(@"^(\* *\[https?://[^<>\[\]]+?)\)\s*$", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Matches a wiki link that appears to be missing one of its closing
    /// square brackets.
    /// </summary>
    private static readonly Regex SyntaxRegexWikilinkMissingClosingBracket = new Regex(@"\[\[([^][]*?)\|?\](?=[^\]]*?(?:$|\[|\n))", RegexOptions.Compiled);

    /// <summary>
    /// Matches a wiki link that appears to be missing one of its opening
    /// square brackets.
    /// </summary>
    private static readonly Regex SyntaxRegexWikilinkMissingOpeningBracket = new Regex(@"(?<=(?:^|\]|\n)[^\[]*?)\[([^][]*?)\]\](?!\])", RegexOptions.Compiled);

    /// <summary>
    /// Matches an external HTTP URL incorrectly associated with the localized
    /// File namespace inside wiki-link syntax.
    /// </summary>
    private static readonly Regex SyntaxRegexExternalLinkToImageURL = new Regex("\\[?\\[" + Variables.NamespacesCaseInsensitive[Namespace.File] + "(http:\\/\\/.*?)\\]\\]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches the beginning of a bracketed external link using one of the
    /// supported URI schemes.
    /// </summary>
    private static readonly Regex ExternalLinksStart = new Regex(@"^\[ *(?:https?|ftp|mailto|irc|gopher|telnet|nntp|worldwind|news|svn)://", RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches one or more trailing <c>&lt;br&gt;</c> tags at the end of a
    /// wiki list row.
    /// </summary>
    private static readonly Regex SyntaxRegexListRowBrTag = new Regex(@"((?:\r\n|^)[#\*:;]+.*?) *(?:<[/\\]?br ?[/\\]? ?>)+[ \t]*(?=\r\n|$)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches one or more <c>&lt;br&gt;</c> tags separating consecutive
    /// wiki list rows.
    /// </summary>
    private static readonly Regex SyntaxRegexListRowBrTagMiddle = new Regex(@"^([#\*:;]+.*?)\s*(?:<[/\\]?br ?[/\\]? ?>)+[ \t]*\r\n([#\*:;]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches a <c>&lt;br&gt;</c> tag immediately before a newline or the
    /// end of the input.
    /// </summary>
    private static readonly Regex SyntaxRegexBrNewline = new Regex(@"<[/\\]?[Bb][Rr] ?[/\\]? ?>[ \t]*(\r\n|$)");

    /// <summary>
    /// Matches a <c>&lt;br&gt;</c> tag immediately before the beginning of
    /// a wiki list row.
    /// </summary>
    private static readonly Regex SyntaxRegexListRowBrTagStart = new Regex(@"<[/\\]?br ?[/\\]? ?>[ \t]*(\r\n[#\*:;]+)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches simple HTML <c>&lt;i&gt;</c> or <c>&lt;b&gt;</c> elements
    /// containing text between matching opening and closing tags.
    /// </summary>
    private static readonly Regex SyntaxRegexItalicBoldEm = new Regex(@"< *(i|b) *>(.*?)< */ *\1 *>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches paragraph tags, including any following spaces, unless the
    /// current line begins with a wiki-table cell marker.
    /// </summary>
    private static readonly Regex SyntaxRemoveParagraphs = new Regex(@"(?<!^[!\|].*)</? ?[Pp]> *", RegexOptions.Multiline);

    /// <summary>
    /// Matches excess <c>&lt;br&gt;</c> tags unless the current line begins
    /// with a wiki-table cell marker.
    /// </summary>
    private static readonly Regex SyntaxRemoveBr = new Regex(@"(?:(?:<br[\s/]*> *){2,}|\r\n<br[\s/]*>\r\n<br[\s/]*>\r\n)(?<!^[!\|].*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    /// <summary>
    /// Matches a maintenance template followed by a <c>&lt;br&gt;</c> tag.
    /// </summary>
    /// <remarks>
    /// TODO: Consider renaming this field from <c>MaintanceTemplateWithBr</c>
    /// to <c>MaintenanceTemplateWithBr</c> during a later naming cleanup pass.
    /// </remarks>
    private static readonly Regex MaintanceTemplateWithBr = new Regex(@"({{" + WikiRegexes.MaintanceTemplatesString + @"\s*\|[^\}]*}}(\r\n)?)\<br[\s/]*\>", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    /// <summary>
    /// Provides a lightweight check for two adjacent <c>&lt;br&gt;</c> tags.
    /// </summary>
    /// <remarks>
    /// TODO: Confirm and document whether this expression is intentionally used
    /// as a quick pre-check before the more detailed line-break cleanup.
    /// </remarks>
    private static readonly Regex SyntaxRemoveBrQuick = new Regex(@"<br[\s/]*>\s*<br[\s/]*>", RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches duplicated or malformed HTTP or HTTPS protocol prefixes.
    /// </summary>
    private static readonly Regex MultipleHttpInLink = new Regex(@"(?<=[\s\[>=])(https?(?::?/+|:/*)) *(\1)+", RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches duplicated or malformed FTP protocol prefixes.
    /// </summary>
    private static readonly Regex MultipleFtpInLink = new Regex(@"(?<=[\s\[>=])(ftp(?::?/+|:/*))(\1)+", RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches external links that incorrectly use wiki-link pipe syntax.
    /// </summary>
    private static readonly Regex PipedExternalLink = new Regex(@"(\[\w+://[^\]\[<>\""\s]*?\s*)(?: +\||\|([ ']))(?=[^\[\]\|]*\])");

    /// <summary>
    /// Matches malformed text beginning with an HTTP-like protocol sequence.
    /// </summary>
    /// <remarks>
    /// TODO: Rename this field in a later cleanup pass so that its name describes
    /// the malformed HTTP syntax it recognizes rather than HTTP links generally.
    /// </remarks>
    private static readonly Regex HttpLinks = new Regex(@"http[htps:/ %]+");

    /// <summary>
    /// Matches malformed HTTP, HTTPS, or FTP links where the protocol separator
    /// contains a missing or incorrect colon or slash sequence.
    /// </summary>
    private static readonly Regex MissingColonInHttpLink = new Regex(@"(?<=[\s\[>=](?:ht|f))(tps?)(?://?:?|:(?::+//)?)(\w+)", RegexOptions.Compiled);

    /// <summary>
    /// Matches HTTP, HTTPS, or FTP links containing an incorrect number of
    /// slashes following the protocol.
    /// </summary>
    private static readonly Regex SingleTripleSlashInHttpLink = new Regex(@"(?<=[\s\[>=](?:ht|f))(tps?):(?:/|////?)(\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches common misspellings of the <c>cellpadding</c> attribute within
    /// a wikitable declaration.
    /// </summary>
    private static readonly Regex CellpaddingTypo = new Regex(@"({\s*\|\s*class\s*=\s*""wikitable[^}]*?)cel(?:lpa|pad?)ding\b", RegexOptions.IgnoreCase);

    /// <summary>
    /// Provides a lightweight check for common misspellings of
    /// <c>cellpadding</c>.
    /// </summary>
    /// <remarks>
    /// TODO: Confirm and document whether this expression is intentionally used
    /// as a quick pre-check before <see cref="CellpaddingTypo"/>.
    /// </remarks>
    private static readonly Regex CellpaddingTypoQuick = new Regex(@"\bcel(?:lpa|pad?)ding\b", RegexOptions.IgnoreCase);

    // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Remove_.3Cfont.3E_tags

    /// <summary>
    /// Matches <c>&lt;font&gt;</c> elements that contain no attributes and
    /// contain no nested HTML tags.
    /// </summary>
    private static readonly Regex RemoveNoPropertyFontTags = new Regex(@"<font>([^<>]+)</font>", RegexOptions.IgnoreCase);

    // Regexes for correcting malformed or unbalanced brackets and braces.

    /// <summary>
    /// Matches malformed closing braces or brackets on a citation-like template
    /// immediately inside a reference.
    /// </summary>
    private static readonly Regex RefTemplateIncorrectBracesAtEnd = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>/]+?\s*)?>\s*)({{\s*[Cc]it[ae][^{}<>]+?)(?:}\]?|\)\))?(?=\s*</ref>)", RegexOptions.Compiled);

    /// <summary>
    /// Matches an external link incorrectly enclosed in template braces inside
    /// a reference.
    /// </summary>
    private static readonly Regex RefExternalLinkUsingBraces = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>/]+?\s*)?>)\s*{{(\s*https?://[^{}\s\r\n]+)(\s+[^{}]+)?\s*}}\s*(</ref>)", RegexOptions.Compiled);

    /// <summary>
    /// Matches a <c>www.</c> URL inside a reference that is missing its
    /// protocol prefix.
    /// </summary>
    private static readonly Regex RefURLMissingHttp = new Regex(@"(<ref(?:\s*name\s*=[^{}<>]+?\s*)?>\[?)\s*www\.", RegexOptions.Compiled);

    /// <summary>
    /// Matches template-like content beginning with mismatched square and
    /// curly braces.
    /// </summary>
    private static readonly Regex TemplateIncorrectBracesAtStart = new Regex(@"(?:{\[|\[{)([^{}\[\]]+}})", RegexOptions.Compiled);

    /// <summary>
    /// Matches a citation-like template beginning with a single opening
    /// curly brace rather than a template's normal opening braces.
    /// </summary>
    private static readonly Regex CitationTemplateSingleBraceAtStart = new Regex(@"(?<=[^{])({\s*[Cc]it[ae])", RegexOptions.Compiled);

    /// <summary>
    /// Matches excess closing template braces immediately before the end of
    /// a reference.
    /// </summary>
    private static readonly Regex ReferenceTemplateQuadBracesAtEnd = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>/]+?\s*)?>\s*{{[^{}]+)}}(}}\s*</ref>)", RegexOptions.Compiled);

    /// <summary>
    /// Provides a lightweight check for excess closing template braces
    /// immediately before <c>&lt;/ref&gt;</c>.
    /// </summary>
    /// <remarks>
    /// TODO: Confirm and document whether this expression is intentionally used
    /// as a quick pre-check before <see cref="ReferenceTemplateQuadBracesAtEnd"/>.
    /// </remarks>
    private static readonly Regex ReferenceTemplateQuadBracesAtEndQuick = new Regex(@"}}}}\s*</ref>");

    /// <summary>
    /// Matches a citation-like template inside a reference that begins with
    /// mismatched curly and square braces.
    /// </summary>
    private static readonly Regex CitationTemplateIncorrectBraceAtStart = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>]+?\s*)?>){\[([Cc]it[ae])", RegexOptions.Compiled);

    /// <summary>
    /// Matches several malformed closing-brace combinations on a citation-like
    /// template immediately before the end of a reference.
    /// </summary>
    private static readonly Regex CitationTemplateIncorrectBracesAtEnd = new Regex(@"(<ref(?:\s*name\s*=[^{}<>]+?\s*)?>\s*{{[Cc]it[ae][^{}]+?)(?:}\]|\]}|{})(?=\s*</ref>)", RegexOptions.Compiled);

    /// <summary>
    /// Provides a lightweight check for malformed citation-template closing
    /// braces immediately before <c>&lt;/ref&gt;</c>.
    /// </summary>
    /// <remarks>
    /// TODO: Confirm and document whether this expression is intentionally used
    /// as a quick pre-check before <see cref="CitationTemplateIncorrectBracesAtEnd"/>.
    /// </remarks>
    private static readonly Regex CitationTemplateIncorrectBracesAtEndQuick = new Regex(@"(?:}\]|\]}|{})(?=\s*</ref>)");

    /// <summary>
    /// Matches an external link inside a reference that is missing its opening
    /// square bracket.
    /// </summary>
    private static readonly Regex RefExternalLinkMissingStartBracket = new Regex(@"(<ref(?:\s*name\s*=[^{}<>]+?\s*)?>[^{}\[\]<>]*?){?((?:ht|f)tps?://[^{}\[\]<>]+\][^{}\[\]<>]*</ref>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches an external link inside a reference that is missing its closing
    /// square bracket.
    /// </summary>
    private static readonly Regex RefExternalLinkMissingEndBracket = new Regex(@"(<ref(?:\s*name\s*=[^{}<>]+?\s*)?>[^{}\[\]<>]*?\[\s*(?:ht|f)tps?://[^{}\[\]<>]+)}?(</ref>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches a citation-like template inside a reference that has closing
    /// template braces but is missing its opening template braces.
    /// </summary>
    private static readonly Regex RefCitationMissingOpeningBraces = new Regex(@"(<\s*ref(?:\s+name\s*=[^<>]*?)?\s*>\s*)\(?\(?([Cc]it[ae][^{}]+}}\s*</ref>)", RegexOptions.Compiled);

    /// <summary>
    /// Matches a <c>{{dead link}}</c> template immediately following the
    /// reference with which it is associated.
    /// </summary>
    private static readonly Regex DeadlinkOutsideRef = new Regex(@"(</ref>) ?(\{\{[Dd]ead ?link\s*\|\s*date\s*=[^{}\|]+\}\})", RegexOptions.Compiled);

    // TODO: Consider extracting common <ref ...> regex fragments after the
    // FixSyntax behavior is sufficiently characterized by tests. Several of
    // the expressions above intentionally repeat similar reference syntax.

    /// <summary>
    /// Matches a reference containing descriptive wording followed by a
    /// bracketed bare external link so the wording can be associated with
    /// the link.
    /// </summary>
    private static readonly Regex WordingIntoBareExternalLinks = new Regex(@"(<ref(?:\s*name\s*=[^{}<>]+?\s*)?>\s*)([^<>{}\[\]\r\n]{3,70}?)[\.,::]?\s*\[\s*((?:[Hh]ttps?|[Ff]tp|[Mm]ailto)://[^\ \n\r<>]+)\s*\](?=\s*</ref>)", RegexOptions.Compiled);

    /// <summary>
    /// Matches an external link immediately preceded by a word character,
    /// indicating that whitespace may be required before the link.
    /// </summary>
    private static readonly Regex ExternalLinkWordSpacingBefore = new Regex(@"(?<=\w)(\[(?:https?|ftp|mailto|irc|gopher|telnet|nntp|worldwind|news|svn)://.*?\])", RegexOptions.Compiled);

    /// <summary>
    /// Matches an external link immediately followed by a word character,
    /// indicating that whitespace may be required after the link.
    /// </summary>
    private static readonly Regex ExternalLinkWordSpacingAfter = new Regex(@"(\[(?:https?|ftp|mailto|irc|gopher|telnet|nntp|worldwind|news|svn)://[^\]\[<>]*?\])(\w)", RegexOptions.Compiled);

    // TODO: Consider centralizing the repeated external-link protocol list
    // used by related expressions after behavior and compatibility have been
    // fully covered by tests.

    /// <summary>
    /// Matches a <c>&lt;br&gt;</c> tag immediately before the closing
    /// brackets of a wiki link at the end of the input.
    /// </summary>
    private static readonly Regex WikilinkEndsBr = new Regex(@"<br[\s/]*>\s*\]\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches HTTP external links that contain balanced square brackets
    /// within the link text.
    /// </summary>
    private static readonly Regex SquareBracketsInExternalLinks = new Regex(@"(\[https?://(?>[^\[\]<>]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\])", RegexOptions.Compiled);

    /// <summary>
    /// Matches malformed <c>&lt;br&gt;</c> syntax such as <c>&lt;\br&gt;</c>,
    /// <c>&lt;br\&gt;</c>, <c>&lt;br.&gt;</c>, and related variants.
    /// </summary>
    /// <remarks>
    /// CHECKWIKI error 2.
    /// </remarks>
    private static readonly Regex IncorrectBr = new Regex(@"<(\\ *br *| *br *\\ *| *br\. */?| *br */([a-z/0-9•\-]|br)| *br *\?|/ *br */?)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches <c>&lt;br&gt;</c> elements using the obsolete
    /// <c>clear</c> attribute.
    /// </summary>
    /// <remarks>
    /// CHECKWIKI error 2.
    /// See https://en.wikipedia.org/wiki/Wikipedia:HTML5#Other_obsolete_attributes.
    /// </remarks>
    private static readonly Regex IncorrectBr2 = new Regex(@"<br\s*clear\s*=\s*""?(both|all|left|right)""?\s*\/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches <c>&lt;br&gt;</c> elements expressing a clear operation through
    /// an inline style, optionally together with a <c>clear</c> attribute.
    /// </summary>
    private static readonly Regex IncorrectBr3 = new Regex(@"<br\s*style\s*=\s*""?clear\:\s?(all|both|left|right)\;?""?(\s*clear=\s*""?(all|left|right)""?)?\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches malformed opening or closing syntax for selected HTML tags
    /// where slash or backslash characters are incorrectly positioned.
    /// </summary>
    private static readonly Regex IncorrectClosingHtmlTags = new Regex(@"< */?(center|gallery|small|sub|sup|i) *[\\/] *>");

    /// <summary>
    /// Matches a horizontal rule represented by an HTML <c>&lt;hr&gt;</c>
    /// element or five or more hyphens at the beginning of a line.
    /// </summary>
    private static readonly Regex SyntaxRegexHorizontalRule = new Regex("^(<hr>|-{5,})", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Matches a wiki heading immediately followed by a horizontal rule.
    /// </summary>
    private static readonly Regex SyntaxRegexHeadingWithHorizontalRule = new Regex("(^==?[^=]*==?)\r\n(\r\n)?----+", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Matches an HTTP version prefix followed by a single version digit
    /// and period.
    /// </summary>
    private static readonly Regex SyntaxRegexHTTPNumber = new Regex(@"HTTP/\d\.", RegexOptions.Compiled);

    /// <summary>
    /// Matches malformed ISBN label syntax immediately before the first digit
    /// of an ISBN.
    /// </summary>
    private static readonly Regex SyntaxRegexISBN = new Regex(@"(?<![:/])(?:ISBN(?:[\-–]1[03])?:|\[\[ISBN\]\]|ISBN ?\t)\s*(\d)", RegexOptions.Compiled);

    /// <summary>
    /// Matches a hyphen or en dash immediately following <c>ISBN</c>, except
    /// when it forms the recognized <c>ISBN-10</c> or <c>ISBN-13</c> labels.
    /// </summary>
    private static readonly Regex SyntaxRegexISBN2 = new Regex(@"(?<![:/])ISBN[\-–](?!1[03]\b)", RegexOptions.Compiled);

    /// <summary>
    /// Matches <c>ISBN–10</c> or <c>ISBN–13</c> when the label uses an
    /// en dash.
    /// </summary>
    private static readonly Regex SyntaxRegexISBN2a = new Regex(@"ISBN–(1[03]\b)");

    /// <summary>
    /// Matches the legacy <c>[[ISBN]]</c> form followed by a
    /// <c>Special:BookSources</c> wiki link.
    /// </summary>
    private static readonly Regex SyntaxRegexISBN3 = new Regex(@"\[\[ISBN\]\]\s\[\[Special\:BookSources[^\|]*\|(?:<bdi>)?([^\]]*?)(?:</?bdi>)?\]\]", RegexOptions.Compiled);

    /// <summary>
    /// Matches the expanded <c>[[International Standard Book Number|ISBN]]</c>
    /// form followed by a <c>Special:BookSources</c> wiki link.
    /// </summary>
    private static readonly Regex SyntaxRegexISBN4 = new Regex(@"\[\[International Standard Book Number\|ISBN\]\]\:?\s\[\[Special\:BookSources[^\|]*\|(?:<bdi>)?([^\]]*?)(?:</?bdi>)?\]\]", RegexOptions.Compiled);

    /// <summary>
    /// Matches an ISBN whose numeric portion contains one or more en dashes.
    /// </summary>
    private static readonly Regex ISBNEndash = new Regex(@"ISBN ([0-9][0-9–]+[0-9X])\b");

    /// <summary>
    /// Matches a lowercase terminal <c>x</c> in an ISBN.
    /// </summary>
    private static readonly Regex ISBNx = new Regex(@"(ISBN [0-9\-]{9,14})x", RegexOptions.Compiled);

    // TODO: Replace the numbered SyntaxRegexISBN2/2a/3/4 names with descriptive
    // names during a later naming pass after their call sites and replacement
    // behavior have been reviewed.

    /// <summary>
    /// Matches a PMID label followed by optional spaces and the first digit
    /// of the identifier, excluding PMID text already inside a wiki link.
    /// </summary>
    private static readonly Regex SyntaxRegexPMID = new Regex(@"(?<!\[\[)(PMID): *(\d)", RegexOptions.Compiled);

    /// <summary>
    /// Matches an external HTTP link that occupies the entire input line.
    /// </summary>
    private static readonly Regex SyntaxRegexExternalLinkOnWholeLine = new Regex(@"^\[(\s*http.*?)\]$", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Matches an isolated closing square bracket that is not immediately
    /// adjacent to another closing square bracket.
    /// </summary>
    private static readonly Regex SyntaxRegexClosingBracket = new Regex(@"([^]])\]([^]]|$)", RegexOptions.Compiled);

    /// <summary>
    /// Matches an isolated opening square bracket that is not immediately
    /// adjacent to another opening square bracket.
    /// </summary>
    private static readonly Regex SyntaxRegexOpeningBracket = new Regex(@"([^[]|^)\[([^[])", RegexOptions.Compiled);

    /// <summary>
    /// Matches a wiki file link containing an HTTP URL.
    /// </summary>
    private static readonly Regex SyntaxRegexFileWithHTTP = new Regex("\\[\\[" + Variables.NamespacesCaseInsensitive[Namespace.File] + ":[^]]*http", RegexOptions.Compiled);

    /// <summary>
    /// Matches simple angle-bracketed tags whose contents do not contain
    /// quotes, hyphens, equals signs, or additional angle brackets.
    /// </summary>
    /// <remarks>
    /// TODO: Review the call site and document why these particular characters
    /// are excluded. The current field name does not explain the restrictions
    /// imposed by the expression.
    /// </remarks>
    private static readonly Regex SimpleTags = new Regex(@"<[^>""\-=]+>");

    /// <summary>
    /// Matches selected citation templates incorrectly enclosed in wiki-link
    /// square brackets inside a reference.
    /// </summary>
    private static readonly Regex CiteTemplateWithSquareBrackets = new Regex(@"(\<ref[^\[]*)\[\[(cite ?(journal|web|book|news)[^\]]*)\]\](\<\/ref\>)", RegexOptions.Compiled);

    /// <summary>
    /// Matches double piped links, for example <c>[[foo||bar]]</c>
    /// (CHECKWIKI error 32).
    /// </summary>
    private static readonly Regex DoublePipeInWikiLink = new Regex(@"(?<=\[\[[^\[\[\r\n\|{}]+)\|\|(?=[^\[\[\r\n\|{}]+\]\])", RegexOptions.Compiled);

    /// <summary>
    /// Matches empty gallery, center, blockquote, small, nowiki, noinclude,
    /// includeonly, sub, or sup elements containing zero or more whitespace
    /// characters and optionally containing attributes on the opening tag.
    /// </summary>
    private static readonly Regex EmptyTags = new Regex(@"<\s*(gallery|center|blockquote|small|noinclude|nowiki|includeonly|su[bp])\s*(\s+[^<>]*)?>\s*<\s*/\s*\1\s*>", RegexOptions.IgnoreCase);

    /// <summary>
    /// Provides British English culture information for syntax corrections
    /// that require culture-specific parsing or formatting.
    /// </summary>
    private static readonly System.Globalization.CultureInfo BritishEnglish = new System.Globalization.CultureInfo("en-GB");

    // Covered by: LinkTests.TestFixSyntax(), incomplete
    /// <summary>
    /// Fixes and improves syntax (such as html markup)
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns>The modified article text.</returns>
    public static string FixSyntax(string articleText)
    {
        List<string> alltemplates = GetAllTemplates(articleText);
        List<string> alltemplatesDetail = GetAllTemplateDetail(articleText);
        MatchCollection ssbMc = SingleSquareBrackets.Matches(articleText);
        string originalArticleText = articleText;

        if (Variables.LangCode.Equals("en"))
        {
            // DEFAULTSORT whitespace fix - CHECKWIKI error 88, 89
            articleText = FixSyntaxDefaultSort(articleText);

            // This category should not be directly added, remove if template present else replace with template
            if ((from Match m in ssbMc where m.Value.Equals(@"[[Category:Disambiguation pages]]") select m).Any())
                articleText = articleText.Replace(@"[[Category:Disambiguation pages]]", TemplateExists(alltemplates, Tools.NestedTemplateRegex("disambiguation")) ? "" : @"{{Disambiguation}}");

            // Remove br tags after maintenance templates
            articleText = MaintanceTemplateWithBr.Replace(articleText, "$1");
        }

        if (TemplateExists(alltemplates, WikiRegexes.MagicWordTemplates))
            articleText = Tools.TemplateToMagicWord(articleText);

        // get a list of all the simple html tags (not with properties) used in the article, so we can selectively apply HTML tag fixes below
        List<string> SimpleTagsList = Tools.DeduplicateList((from Match m in SimpleTags.Matches(articleText) select m.Value).ToList());
        SimpleTagsList = Tools.DeduplicateList(SimpleTagsList.Select(s => Regex.Replace(s, @"\s", "").ToLower()).ToList());

        // fix for <sup/>, <sub/>, <center/>, <small/>, <i/> etc.
        if (SimpleTagsList.Any(s => !s.Equals("<br/>") && (s.EndsWith("/>") || s.Contains(@"\"))))
            articleText = IncorrectClosingHtmlTags.Replace(articleText, "</$1>");

        // The <strike> tag is not supported in HTML5. - CHECKWIKI error 42
        if (SimpleTagsList.Any(s => s.Contains("strike")))
        {
            articleText = articleText.Replace(@"<strike>", @"<s>");
            articleText = articleText.Replace(@"</strike>", @"</s>");
        }

        // remove empty <gallery>, <center>, <blockquote>, <nowiki>, <sub> or <sup> tags, allow for nested tags
        while (EmptyTags.IsMatch(articleText))
            articleText = EmptyTags.Replace(articleText, string.Empty);

        // try to fix invalid opening <ref> tag
        if (UnclosedTags(articleText).Any())
        {
            articleText = articleText.Replace("<ref<", "<ref>").Replace("}}/ref>", "}}</ref>");

            if (Regex.IsMatch(articleText, @"[\.,] ?\/?ref"))
                articleText = Regex.Replace(articleText, @"([\.,]) ?ref(\s*name\s*=[^{}<>]+?\s*)?>", "$1<ref$2>");
        }

        // merge italic/bold html tags if there are one after the other
        //https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_21#Another_bug_on_italics
        if (SimpleTagsList.Any(s => s.StartsWith("<b") && !s.StartsWith("<br")))
            articleText = articleText.Replace("</b><b>", "");
        if (SimpleTagsList.Any(s => s.StartsWith("<i")))
            articleText = articleText.Replace("</i><i>", "");

        //replace html with wiki syntax - CHECKWIKI error 26 and 38
        if (SimpleTagsList.Any(s => Regex.IsMatch(s, @"<(i|b)\b")))
        {
            while (SyntaxRegexItalicBoldEm.IsMatch(articleText))
                articleText = SyntaxRegexItalicBoldEm.Replace(articleText, BoldItalicME);
        }

        if (SimpleTagsList.Any(s => s.StartsWith("<hr")) || articleText.Contains("-----"))
            articleText = SyntaxRegexHorizontalRule.Replace(articleText, "----");

        // remove appearance of double line break
        articleText = SyntaxRegexHeadingWithHorizontalRule.Replace(articleText, "$1");

        // remove unnecessary namespace
        if (alltemplatesDetail.Any(t => Regex.IsMatch(t, Variables.NamespacesCaseInsensitive[Namespace.Template])))
            articleText = RemoveTemplateNamespace(articleText);

        // removal of Unicode non-breaking space or newlines in template name
        List<string> templatesWithUnicodeNonBreakingSpaceOrNewline =
            alltemplatesDetail.Where(tc =>
            {
                if (tc.Contains("\u00a0") || tc.Contains("\u3000"))
                    return true;

                // check template call up to first bar for newline, but if have wiki comment will be hidden so ignore if have hidetext character
                if (tc.Contains("|"))
                {
                    string toFirstBar = tc.Substring(0, tc.IndexOf('|'));
                    return toFirstBar.Trim().Length > 2 && toFirstBar.Contains("\r\n") && !toFirstBar.Contains("⌊⌊⌊⌊");
                }

                return false;
            }).Select(tc => Tools.GetTemplateName(tc)).Where(t => t.Length > 0).ToList();

        foreach (var t in templatesWithUnicodeNonBreakingSpaceOrNewline)
            articleText = Tools.RenameTemplate(articleText, t, t, true);

        if (SyntaxRegexBrNewline.IsMatch(articleText))
        {
            // remove <br> from lists (end of list line) - CHECKWIKI error 54
            articleText = SyntaxRegexListRowBrTag.Replace(articleText, "$1");

            // remove <br> from the middle of lists
            articleText = SyntaxRegexListRowBrTagMiddle.Replace(articleText, "$1\r\n$2");
        }

        // CHECKWIKI error 93
        bool badHttpLinks = Tools.DeduplicateList((from Match m in HttpLinks.Matches(articleText.ToLower()) select m.Value).ToList()).Any(s => !Regex.IsMatch(s, @"^https?://[htps]*$"));

        if (badHttpLinks)
            articleText = MultipleHttpInLink.Replace(articleText, "$1");

        articleText = MultipleFtpInLink.Replace(articleText, "$1");

        if (badHttpLinks && TemplateExists(alltemplates, WikiRegexes.UrlTemplate))
            articleText = WikiRegexes.UrlTemplate.Replace(articleText, m => m.Value.Replace("http://http://", "http://"));

        if (badHttpLinks && !SyntaxRegexHTTPNumber.IsMatch(articleText))
        {
            articleText = MissingColonInHttpLink.Replace(articleText, "$1://$2");
            articleText = SingleTripleSlashInHttpLink.Replace(articleText, "$1://$2");
            articleText = articleText.Replace("https://http://", "https://");
            articleText = articleText.Replace("https:// www.", "https://www.");
            articleText = articleText.Replace("http:// www.", "http://www.");
            articleText = articleText.Replace("[http%3A//", "[http://");
            articleText = articleText.Replace("[https%3A//", "[https://");
        }

        if (CellpaddingTypoQuick.IsMatch(articleText))
            articleText = CellpaddingTypo.Replace(articleText, "$1cellpadding");

        if (SimpleTagsList.Any(s => s.Contains("font")))
            articleText = RemoveNoPropertyFontTags.Replace(articleText, "$1");

        //<ref>[[cite web|url=http://www.foo.com]]</ref>
        articleText = CiteTemplateWithSquareBrackets.Replace(articleText, "$1{{$2}}$4");

        if (SimpleTagsList.Any(s => s.Contains("reflist")))
            articleText = articleText.Replace("<<reflist>>", "{{reflist}}");

        // {{Category:foo]] or {{Category:foo}}
        articleText = CategoryCurlyBrackets.Replace(articleText, @"[[$1]]");

        // [[Category:foo}}
        articleText = CategoryCurlyBracketsEnd.Replace(articleText, @"[[$1]]");

        // fixes for missing/unbalanced brackets, for performance only run if article has unbalanced templates
        string withouttemplates = WikiRegexes.NestedTemplates.Replace(articleText, string.Empty);

        if (withouttemplates.IndexOf("{{", StringComparison.Ordinal) > -1 || withouttemplates.IndexOf("}}", StringComparison.Ordinal) > -1)
        {
            articleText = RefCitationMissingOpeningBraces.Replace(articleText, @"$1{{$2");
            articleText = RefTemplateIncorrectBracesAtEnd.Replace(articleText, @"$1}}");
            articleText = TemplateIncorrectBracesAtStart.Replace(articleText, @"{{$1");
            articleText = CitationTemplateSingleBraceAtStart.Replace(articleText, @"{$1");
            if (ReferenceTemplateQuadBracesAtEndQuick.IsMatch(articleText))
                articleText = ReferenceTemplateQuadBracesAtEnd.Replace(articleText, @"$1");
            articleText = CitationTemplateIncorrectBraceAtStart.Replace(articleText, @"{{$1");
            if (CitationTemplateIncorrectBracesAtEndQuick.IsMatch(articleText))
                articleText = CitationTemplateIncorrectBracesAtEnd.Replace(articleText, @"$1}}");
        }

        articleText = RefExternalLinkUsingBraces.Replace(articleText, @"[$1$2]$3");

        // refresh if necessary
        if (!originalArticleText.Equals(articleText))
            ssbMc = SingleSquareBrackets.Matches(articleText);

        originalArticleText = articleText;
        string nobrackets = Tools.ReplaceWithSpaces(articleText, ssbMc);
        bool orphanedSingleBrackets = (nobrackets.Contains("[") || nobrackets.Contains("]"));

        if (orphanedSingleBrackets)
        {
            articleText = RefExternalLinkMissingStartBracket.Replace(articleText, @"$1[$2");
            articleText = RefExternalLinkMissingEndBracket.Replace(articleText, @"$1]$2");

            // refresh
            ssbMc = SingleSquareBrackets.Matches(articleText);
        }

        // fixes for external links: internal square brackets, newlines or pipes - Partially CHECKWIKI error 80
        // Performance: filter down to matches with likely external link (contains //) and has pipe, newline or internal square brackets
        List<string> ssb = Tools.DeduplicateList((from Match m in ssbMc select m.Value).ToList());
        List<string> ssbExternalLink = ssb.FindAll(m => m.Contains("//") && (m.Contains("|") || m.Contains("\r\n") || m.Substring(3).Contains("[") || m.Trim(']').Contains("]")));

        foreach (string s in ssbExternalLink)
        {
            string newvalue = s;

            if (newvalue.Contains("\r\n") && !newvalue.Substring(1).Contains("[") && ExternalLinksStart.IsMatch(newvalue))
                newvalue = newvalue.Replace("\r\n", " ");

            newvalue = SquareBracketsInExternalLinks.Replace(newvalue, SquareBracketsInExternalLinksME);

            newvalue = PipedExternalLink.Replace(newvalue, "$1 $2");

            if (!s.Equals(newvalue))
                articleText = articleText.Replace(s, newvalue);
        }

        // needs to be applied after SquareBracketsInExternalLinks
        if (orphanedSingleBrackets && !SyntaxRegexFileWithHTTP.IsMatch(articleText))
        {
            articleText = SyntaxRegexWikilinkMissingClosingBracket.Replace(articleText, "[[$1]]");
            articleText = SyntaxRegexWikilinkMissingOpeningBracket.Replace(articleText, "[[$1]]");
        }

        // adds missing http:// to bare url references lacking it - CHECKWIKI error 62
        articleText = RefURLMissingHttp.Replace(articleText, @"$1http://www.");

        // repair bad Image/external links, ssb check for performance
        if (ssb.Any(m => m.Contains(":") && m.ToLower().Contains(":http")))
            articleText = SyntaxRegexExternalLinkToImageURL.Replace(articleText, "[$1]");

        // apply ISBN fixes
        articleText = FixSyntaxISBN(articleText, ssb.FindAll(s => s.Contains("ISBN]]")));

        // T198854 not for hu-wiki
        if (articleText.Contains("PMID:") && !Variables.LangCode.Equals("hu"))
            articleText = SyntaxRegexPMID.Replace(articleText, "$1 $2");

        // Remove sup tags from ordinals per [[WP:ORDINAL]].
        // CHECKWIKI error 101
        if (SimpleTagsList.Any(s => s.Contains("sup")))
            articleText = SupOrdinal.Replace(articleText, @"$1$2");

        // CHECKWIKI error 86
        bool doubleBracketHttp = articleText.ToLower().Contains("[[http");
        if (doubleBracketHttp)
            articleText = DoubleBracketAtStartOfExternalLink.Replace(articleText, "[$1");

        // if there are some unbalanced brackets, see whether we can fix them. Trim after to clean up after SplitToSections
        articleText = FixUnbalancedBrackets(articleText).Trim();

        // fix uneven bracketing on links
        if (doubleBracketHttp)
            articleText = DoubleBracketAtStartOfExternalLink.Replace(articleText, "[$1");

        // only refresh nobrackets if changes
        if (!originalArticleText.Equals(articleText))
            nobrackets = SingleSquareBrackets.Replace(articleText, string.Empty);

        if (nobrackets.Contains("[") || nobrackets.Contains("]"))
        {
            articleText = DoubleBracketAtEndOfExternalLink.Replace(articleText, m => m.Value.Contains("\r\n") ? m.Value : m.Groups[1].Value);
            articleText = DoubleBracketAtEndOfExternalLinkWithinImage.Replace(articleText, "$1");

            articleText = ListExternalLinkEndsCurlyBrace.Replace(articleText, "$1]");
        }

        // double piped links e.g. [[foo||bar]] - CHECKWIKI error 32
        if (ssb.Any(s => s.Contains("||")))
            articleText = DoublePipeInWikiLink.Replace(articleText, "|");

        // https://en.wikipedia.org/wiki/Wikipedia:WikiProject_Check_Wikipedia#Article_with_false_.3Cbr.2F.3E_.28AutoEd.29
        // fix incorrect <br> of <br.>, <\br> and <br\> - CHECKWIKI error 02
        if (SimpleTagsList.Any(s => (s.Contains("br") && !s.Equals("<br>") && !s.Equals("<br/>"))))
            articleText = IncorrectBr.Replace(articleText, "<br />");

        articleText = IncorrectBr2.Replace(articleText, m =>
        {
            if (m.Groups[1].Value == "left")
                return "{{clear|left}}";
            if (m.Groups[1].Value == "right")
                return "{{clear|right}}";

            return "{{clear}}";
        }
        );
        //<br style="clear:both;" clear="all" />
        //<br style="clear:both;" />
        articleText = IncorrectBr3.Replace(articleText, m =>
        {
            if (m.Groups[1].Value == "left")
                return "{{clear|left}}";
            if (m.Groups[1].Value == "right")
                return "{{clear|right}}";

            return "{{clear}}";
        }
        );

        // CHECKWIKI errors 55, 63, 66, 77
        if (SimpleTagsList.Any(s => s.Contains("small")))
            articleText = FixSmallTags(articleText);

        articleText = WordingIntoBareExternalLinks.Replace(articleText, @"$1[$3 $2]");

        if (TemplateExists(alltemplates, WikiRegexes.DeadLink))
            articleText = DeadlinkOutsideRef.Replace(articleText, @" $2$1");

        if (!Variables.LangCode.Equals("zh"))
        {
            articleText = ExternalLinkWordSpacingBefore.Replace(articleText, " $1");
            articleText = ExternalLinkWordSpacingAfter.Replace(articleText, "$1 $2");
        }

        // CHECKWIKI error 65: Image description ends with break – https://checkwiki.toolforge.org/cgi-bin/checkwiki.cgi?project=enwiki&view=only&id=65
        if (ssb.Any(s => s.Contains("<")))
            articleText = WikiRegexes.FileNamespaceLink.Replace(articleText, m => WikilinkEndsBr.Replace(m.Value, @"]]"));

        // workaround for https://phabricator.wikimedia.org/T4700 -- {{subst:}} doesn't work within ref tags
        articleText = FixSyntaxSubstRefTags(articleText);

        // ensure magic word behavior switches such as __TOC__ are in upper case
        if (nobrackets.IndexOf("__", StringComparison.Ordinal) > -1)
            articleText = WikiRegexes.MagicWordsBehaviourSwitches.Replace(articleText, m => @"__" + m.Groups[1].Value.ToUpper() + @"__");

        return articleText.Trim();
    }

    /// <summary>
    /// Matches an HTTP external link whose displayed text ends with an ISBN.
    /// </summary>
    /// <remarks>
    /// The expression captures the external-link portion separately from the
    /// trailing ISBN. An optional comma, semicolon, or colon may appear before
    /// the ISBN, and the ISBN may optionally end with <c>X</c> and a period.
    ///
    /// TODO: Review whether this expression should eventually support HTTPS
    /// explicitly rather than relying on the broader <c>http</c> prefix match,
    /// and verify whether ISBN formats containing characters other than digits,
    /// hyphens, and a terminal <c>X</c> need to be supported.
    /// </remarks>
    private static readonly Regex ExternalLinkEndsISBN = new Regex(@"(\[http[^[\]]+? +[^[\]]+?)[,;:]? +(ISBN +[0-9-]+X?\.?) ?\]");

    /// <summary>
    /// Normalizes common ISBN syntax errors and formatting issues in article text.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="ssbISBN">
    /// A collection of ISBN-related strings detected during preprocessing and used
    /// to determine whether specific ISBN cleanup rules should be applied.
    /// </param>
    /// <returns>
    /// The article text with supported ISBN syntax corrections applied.
    /// </returns>
    /// <remarks>
    /// This method handles CHECKWIKI error 69 and related ISBN cleanup, including
    /// malformed ISBN labels, legacy ISBN wiki-link forms, lowercase ISBN-10 check
    /// digits, en dashes within ISBN numbers, redundant ISBN prefixes in infobox
    /// parameters, and ISBNs incorrectly included at the end of external links.
    ///
    /// TODO: Consider extracting the individual ISBN cleanup operations into
    /// focused helpers during a later refactoring pass once their behavior and
    /// test coverage have been reviewed.
    /// </remarks>
    private static string FixSyntaxISBN(string articleText, List<string> ssbISBN)
    {
        // CHECKWIKI error 69.
        bool isbnDash =
            articleText.Contains("ISBN-") ||
            articleText.Contains("ISBN–");

        if (isbnDash ||
            articleText.Contains("ISBN:") ||
            articleText.Contains("ISBN\t") ||
            articleText.Contains("ISBN \t") ||
            ssbISBN.Contains("[[ISBN]]"))
        {
            articleText = SyntaxRegexISBN.Replace(articleText, "ISBN $1");
        }

        if (isbnDash)
        {
            articleText = SyntaxRegexISBN2.Replace(articleText, "ISBN ");
            articleText = SyntaxRegexISBN2a.Replace(articleText, "ISBN-$1");
        }

        if (ssbISBN.Contains("[[ISBN]]"))
            articleText = SyntaxRegexISBN3.Replace(articleText, "ISBN $1");

        if (ssbISBN.Contains("[[International Standard Book Number|ISBN]]"))
            articleText = SyntaxRegexISBN4.Replace(articleText, "ISBN $1");

        // Capitalize the ISBN-10 check digit.
        articleText = ISBNx.Replace(articleText, "$1X");

        // Replace en dashes with hyphens within ISBN numbers.
        articleText = ISBNEndash.Replace(
            articleText,
            m => "ISBN " + m.Groups[1].Value.Replace("–", "-"));

        // Remove a redundant ISBN prefix from isbn= parameters in infoboxes.
        if (TemplateExists(GetAllTemplates(articleText), WikiRegexes.InfoBox))
        {
            foreach (string infobox in GetAllTemplateDetail(articleText)
                         .Where(t => WikiRegexes.InfoBox.IsMatch(t)))
            {
                string isbn = Tools.GetTemplateParameterValue(infobox, "isbn");

                if (isbn.StartsWith("ISBN"))
                {
                    articleText = articleText.Replace(
                        infobox,
                        Tools.UpdateTemplateParameterValue(
                            infobox,
                            "isbn",
                            Regex.Replace(isbn, @"^ISBN\s*:?\s*", "")));
                }
            }
        }

        // Move an ISBN outside an external link when it appears at the end of
        // the link's display text.
        while (ExternalLinkEndsISBN.IsMatch(articleText))
            articleText = ExternalLinkEndsISBN.Replace(articleText, "$1] $2");

        return articleText;
    }

    /// <summary>
    /// Applies fixes to any DEFAULTSORT templates in the input text
    /// </summary>
    /// <returns>The updated article text</returns>
    /// <param name="articleText">Article text.</param>
    private static string FixSyntaxDefaultSort(string articleText)
    {
        // Performance: check DEFAULTSORT from cache, to avoid processing articleText if no changes to make
        List<string> alltemplates = GetAllTemplateDetail(articleText).FindAll(t => WikiRegexes.Defaultsort.IsMatch(t));

        // must apply DefaultsortME if no existing DEFAULTSORT as it may be a template with unclosed braces
        if (!alltemplates.Any() || alltemplates.Any(s => !s.Equals(WikiRegexes.Defaultsort.Replace(s, DefaultsortME))))
            articleText = WikiRegexes.Defaultsort.Replace(articleText, DefaultsortME);

        return articleText;
    }

    /// <summary>
    /// Trims whitespace around DEFAULTSORT value, ensures 'whitespace only' DEFAULTSORT left unchanged, removes trailing square brackets, adds missing space after comma
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private static string DefaultsortME(Match m)
    {
        string returned = @"{{DEFAULTSORT:", key = m.Groups["key"].Value;

        // avoid changing a defaultsort key value that is only whitespace: wrong before, would still be wrong after
        if (key.Trim().Length == 0)
            return m.Value;

        returned += (key.Trim().TrimEnd("[]".ToCharArray()).Trim() + @"}}");

        // handle case where defaultsort ended by newline, preserve newline at end of defaultsort returned
        string end = m.Groups["end"].Value;

        if (!end.TrimStart().Equals(@"}}"))
            returned += end;

        // space after comma, unless comma is at end of sort key
        returned = Regex.Replace(returned, @",(\S..)", ", $1");

        return returned;
    }

    /// <summary>
    /// Replaces with three apostrophes (''') if &lt;B> or &lt;b> tag, else just two ('')
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private static string BoldItalicME(Match m)
    {
        string ret = (m.Groups[1].Value.Equals("b", StringComparison.OrdinalIgnoreCase) ? "'''" : "''");
        return ret + m.Groups[2].Value + ret;
    }

    /// <summary>
    /// Fixes bracket problems within external links, converting internal [ or ] to &#91; or &#93; respectively
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private static string SquareBracketsInExternalLinksME(Match m)
    {
        // strip off leading [ and trailing ]
        string externalLink = SyntaxRegexExternalLinkOnWholeLine.Replace(m.Value, "$1");

        // if there are unmatched double brackets, we can't fix this
        if ((externalLink.Contains("[[") && !externalLink.Contains("]]")) ||
            (!externalLink.Contains("[[") && externalLink.Contains("]]")))
            return (@"[" + externalLink + @"]");

        // if there are some single brackets left then they need fixing; the mediawiki parser finishes the external link at the first ] found
        if (!WikiRegexes.Newline.IsMatch(externalLink) && (externalLink.Contains("]") || externalLink.Contains("[")))
        {
            // replace single ] with &#93; when used for brackets in the link description
            if (externalLink.Contains("]"))
                externalLink = SyntaxRegexClosingBracket.Replace(externalLink, @"$1&#93;$2");

            if (externalLink.Contains("["))
                externalLink = SyntaxRegexOpeningBracket.Replace(externalLink, @"$1&#91;$2");
        }
        return (@"[" + externalLink + @"]");
    }

    /// <summary>
    /// Matches the opening wiki-link brackets of a redirect target when they
    /// are immediately preceded by an equals sign or colon, with an optional
    /// intervening space.
    /// </summary>
    private static readonly Regex RedirectBracketsWithPrefix = new Regex(@"[=:] ?\[\[", RegexOptions.Compiled);

    /// <summary>
    /// Matches sequences of three or four consecutive opening square brackets.
    /// </summary>
    private static readonly Regex TooManyOpenSquareBrackets = new Regex(@"\[{3,4}", RegexOptions.Compiled);

    /// <summary>
    /// Matches sequences of three or four consecutive closing square brackets.
    /// </summary>
    private static readonly Regex TooManyCloseSquareBrackets = new Regex(@"\]{3,4}", RegexOptions.Compiled);

    /// <summary>
    /// Performs fixes to redirect pages:
    /// * removes newline between #REDIRECT and link (CHECKWIKI error 36)
    /// * removes equals sign and double dot between #REDIRECT and link (CHECKWIKI error 36)
    /// * Template to Magic word conversion; removes unnecessary brackets around redirect
    /// * Simple closing bracket fixing to {{R...}} templates
    /// </summary>
    /// <param name="articleText"></param>
    /// <returns></returns>
    public static string FixSyntaxRedirects(string articleText)
    {
        articleText = WikiRegexes.Redirect.Replace(articleText, m =>
            TooManyCloseSquareBrackets.Replace(
                TooManyOpenSquareBrackets.Replace(
                    RedirectBracketsWithPrefix.Replace(m.Value.Replace("\r\n", " "), " [["), "[["), "]]"));

        articleText = Tools.TemplateToMagicWord(articleText);

        // apply some simple bracket fixing to redirect templates
        if (articleText.Contains("{{") && !articleText.Contains("}}"))
        {
            // fix incorrect closing bracket
            articleText = Regex.Replace(articleText.TrimEnd(), @"(\]\]|\]?}|}\])$", "}}");

            // append missing closing }}
            if (!articleText.Contains("}}"))
                articleText += "}}";
        }

        return RemoveTemplateNamespace(articleText);
    }

    /// <summary>
    /// workaround for https://phabricator.wikimedia.org/T4700 -- {{subst:}} doesn't work within ref tags
    /// </summary>
    /// <param name="articleText"></param>
    /// <returns></returns>
    public static string FixSyntaxSubstRefTags(string articleText)
    {
        if (Variables.LangCode.Equals("en") && articleText.Contains(@"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}"))
        {
            articleText = WikiRegexes.Refs.Replace(articleText, FixSyntaxSubstRefTagsME);

            articleText = WikiRegexes.Images.Replace(articleText, FixSyntaxSubstRefTagsME);

            articleText = WikiRegexes.GalleryTag.Replace(articleText, FixSyntaxSubstRefTagsME);
        }

        return articleText;
    }

    // TODO: Consider supporting additional substituted date variables if
    // FixSyntax is expanded to normalize other MediaWiki substitution patterns.
    /// <summary>
    /// Replaces substituted CURRENTMONTHNAME and CURRENTYEAR variables inside a
    /// matched reference with the current UTC month and year using British
    /// English month names.
    /// </summary>
    /// <param name="m">
    /// The regex match containing the substituted variables.
    /// </param>
    /// <returns>
    /// The updated reference text.
    /// </returns>
    private static string FixSyntaxSubstRefTagsME(Match m)
    {
        return m.Value.Replace(
            @"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}",
            DateTime.UtcNow.ToString("MMMM yyyy", BritishEnglish));
    }

    // TODO: Consider replacing List<Regex> with IReadOnlyList<Regex> or an array
    // if the collection is immutable after initialization.
    /// <summary>
    /// Stores dynamically generated regular expressions used when processing
    /// HTML <c>&lt;small&gt;</c> elements.
    /// </summary>
    private static readonly List<Regex> SmallTagRegexes = new();

    /// <summary>
    /// Matches nested <c>{{legend}}</c> templates.
    ///</summary>
    private static readonly Regex LegendTemplate = Tools.NestedTemplateRegex("legend");

    /// <summary>
    /// remove &lt;small> in small, ref, sup, sub tags and images, but not within {{legend}} template
    /// CHECKWIKI errors 55, 63, 66, 77
    /// </summary>
    /// <param name="articleText">The article text</param>
    /// <returns>The updated article text</returns>
    private static string FixSmallTags(string articleText)
    {
        Match sm = WikiRegexes.Small.Match(articleText);

        // Performance: restrict changes to portion of article text containing small tags
        if (sm.Success)
        {
            int cutoff = Math.Max(0, sm.Index - 999); // if <ref><small> then must allow offset before <small> tag
            string beforesmall = articleText.Substring(0, cutoff);
            articleText = articleText.Substring(cutoff);

            // don't apply if there are unclosed tags
            if (!UnclosedTags(articleText).Any())
            {
                articleText = SmallTagRegexes.Aggregate(articleText, (current, rx) => rx.Replace(current, FixSmallTagsME));

                // fixes for small tags surrounding ref/sup/sub tags
                articleText = WikiRegexes.Small.Replace(articleText, FixSmallTagsME2);
            }

            return beforesmall + articleText;
        }

        return articleText;
    }

    /// <summary>
    /// Matches the closing markup of a wiki table at the beginning of a line.
    /// </summary>
    private static readonly Regex TableEnd = new Regex(@"^\|}", RegexOptions.Multiline);

    /// <summary>
    /// Removes unnecessary <c>&lt;small&gt;</c> tags from the matched text while
    /// preserving cases where the tags are required or expected.
    /// </summary>
    /// <param name="m">
    /// The matched text containing one or more <c>&lt;small&gt;</c> elements.
    /// </param>
    /// <returns>
    /// The modified text if removable <c>&lt;small&gt;</c> tags are found;
    /// otherwise, the original matched text.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <c>&lt;small&gt;</c> tags are intentionally preserved inside
    /// <c>{{legend}}</c> templates and within wiki tables, where they are commonly
    /// used for formatting.
    /// </para>
    /// <para>
    /// When nested <c>&lt;small&gt;</c> tags are encountered, only the inner
    /// removable tags are stripped while preserving the outer structure.
    /// </para>
    /// </remarks>
    private static string FixSmallTagsME(Match m)
    {
        // Don't remove <small> tags from within {{legend}} where their use is
        // intentional, or from wiki tables where they may be used for formatting.
        if (!LegendTemplate.IsMatch(m.Value) && !TableEnd.IsMatch(m.Value))
        {
            Match s = WikiRegexes.Small.Match(m.Value);
            if (s.Success)
            {
                if (s.Index > 0)
                    return WikiRegexes.Small.Replace(m.Value, "$1");

                // Nested <small> element: preserve the outer tag while removing
                // unnecessary inner tags.
                return m.Value.Substring(0, 7) + WikiRegexes.Small.Replace(m.Value.Substring(7), "$1");
            }
        }

        return m.Value;
    }

    /// <summary>
    /// Removes redundant outer <c>&lt;small&gt;</c> tags when they contain only a
    /// reference or superscript/subscript markup.
    /// </summary>
    /// <param name="m">
    /// The matched <c>&lt;small&gt;</c> element.
    /// </param>
    /// <returns>
    /// The inner content when the surrounding <c>&lt;small&gt;</c> element is
    /// unnecessary; otherwise, the original matched text.
    /// </returns>
    /// <remarks>
    /// References and superscript/subscript elements already render with reduced
    /// visual emphasis, so an additional surrounding <c>&lt;small&gt;</c> element
    /// is generally redundant.
    /// </remarks>
    private static string FixSmallTagsME2(Match m)
    {
        string smallContent = m.Groups[1].Value.Trim();

        if (!smallContent.Contains("<"))
            return m.Value;

        if (WikiRegexes.Refs.Match(smallContent).Value.Equals(smallContent) ||
            WikiRegexes.SupSub.Match(smallContent).Value.Equals(smallContent))
            return smallContent;

        return m.Value;
    }

    /// <summary>
    /// Removes link parameter within a [[File: wikilink if link target is same as the file i.e. self link with no other URL parameters
    /// </summary>
    /// <param name="articleText"></param>
    /// <returns>Updated article text</returns>
    public string FixImageSelfLinks(string articleText)
    {
        articleText = WikiRegexes.FileNamespaceLink.Replace(articleText, m =>
        {
            string res = m.Value;

            // is value of link parameter (cleaned of URL part and tidied up)
            string linkParam = Tools.GetTemplateParameterValue("{{" + m.Groups[1].Value + "}}", "link");

            if (!String.IsNullOrEmpty(linkParam))
            {
                linkParam = Controls.Lists.ListMaker.NormalizeTitleCore(linkParam);
                linkParam = Tools.RemoveSyntax(linkParam);

                // is the link target, tidied up
                string fileLinkTarget = WikiRegexes.WikiLink.Match(m.Value).Groups[1].Value;

                if (linkParam == Tools.RemoveSyntax(fileLinkTarget))
                {
                    // use of template function to adjust parameters
                    int len = res.Length;
                    res = Tools.RemoveTemplateParameter("{{" + res.Substring(2, len - 4) + "}}", "link");
                    len = res.Length;
                    res = "[[" + res.Substring(2, len - 4) + "]]";
                }
            }
            return res;
        });

        return articleText;
    }

    /// <summary>
    /// Matches the MediaWiki <c>__INDEX__</c> and <c>__NOINDEX__</c> magic words
    /// at the beginning of a line.
    /// </summary>
    /// <remarks>
    /// These magic words are not used in article (mainspace) pages and are removed
    /// during syntax cleanup.
    /// </remarks>
    private static readonly Regex IndexNoIndexMagicWord =
        new(@"^__(NO)?INDEX__(\s+|$)", RegexOptions.Multiline);

    /// <summary>
    /// Removes unsupported indexing magic words from mainspace articles.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="articleTitle">
    /// The title of the article.
    /// </param>
    /// <returns>
    /// The updated article text. If the page is not in the main namespace, the
    /// original text is returned unchanged.
    /// </returns>
    /// <remarks>
    /// This cleanup removes the MediaWiki <c>__INDEX__</c> and
    /// <c>__NOINDEX__</c> magic words only from articles in the main namespace.
    /// Other namespaces are left unchanged because these magic words may be
    /// intentionally used there.
    /// </remarks>
    public static string FixSyntaxMainspace(string articleText, string articleTitle)
    {
        if (!Namespace.IsMainSpace(articleTitle))
            return articleText;

        return IndexNoIndexMagicWord.Replace(articleText, string.Empty);
    }
}