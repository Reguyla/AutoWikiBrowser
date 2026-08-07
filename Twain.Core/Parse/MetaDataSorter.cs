/*

Copyright (C) 2007 Martin Richards, Max Semenik et al.

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

using System.Globalization;
using Twain.Core.TalkPages;

namespace Twain.Core.Parse;

// TODO (Modernization):
// Consolidate metadata-section regular expressions into a dedicated helper
// or registry. Centralizing these patterns will improve maintainability,
// simplify testing, and make wiki-specific section handling configurable.
/// <summary>
/// Specifies the ordering method used for interwiki links.
/// </summary>
public enum InterWikiOrderEnum
{
    /// <summary>
    /// Sorts interwiki links alphabetically using the local language name.
    /// </summary>
    LocalLanguageAlpha,

    /// <summary>
    /// Sorts interwiki links alphabetically by the first word of the
    /// local language name.
    /// </summary>
    LocalLanguageFirstWord,

    /// <summary>
    /// Sorts interwiki links alphabetically by language code.
    /// </summary>
    Alphabetical,

    /// <summary>
    /// Places the English interwiki link first, followed by the remaining
    /// links sorted alphabetically by language code.
    /// </summary>
    AlphabeticalEnFirst
}

/// <summary>
/// Provides functionality for sorting and processing article metadata,
/// including categories and interwiki links.
/// </summary>
public class MetaDataSorter
{
    /// <summary>
    /// Contains the collection of recognized interwiki prefixes used during
    /// metadata processing.
    /// </summary>
    public List<string> PossibleInterwikis;

    /// <summary>
    /// Gets or sets a value indicating whether interwiki links should be sorted.
    /// </summary>
    public bool SortInterwikis
    { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether category sort keys should be added.
    /// </summary>
    public bool AddCatKey
    { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetaDataSorter"/> class
    /// and loads the interwiki ordering data.
    /// </summary>
    /// <exception cref="NullReferenceException">
    /// Thrown when the local-language interwiki ordering data could not be loaded.
    /// </exception>
    public MetaDataSorter()
    {
        SortInterwikis = true;

        if (!LoadInterWikiFromCache())
        {
            LoadInterWikiFromNetwork();
            SaveInterWikiToCache();
        }

        if (InterwikiLocalAlpha == null)
            throw new NullReferenceException("InterwikiLocalAlpha is null");

        // Create the comparer using the default interwiki ordering.
        InterWikiOrder = InterWikiOrderEnum.LocalLanguageAlpha;
    }

    // Generated dynamically using Variables.Stub.
    private readonly Regex InterLangRegex =
        new Regex(@"", RegexOptions.IgnoreCase);

    /// <summary>
    /// Matches category marker comments used during metadata processing.
    /// </summary>
    private readonly Regex CatCommentRegex =
        new Regex(
            "<!--+ ?cat(egories)? ?--+>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Contains interwiki entries sorted by local-language name.
    /// </summary>
    private List<string> InterwikiLocalAlpha;

    /// <summary>
    /// Contains interwiki entries sorted by the first word of the
    /// local-language name.
    /// </summary>
    private List<string> InterwikiLocalFirst;

    /// <summary>
    /// Contains interwiki entries sorted alphabetically by language code.
    /// </summary>
    private List<string> InterwikiAlpha;

    /// <summary>
    /// Contains interwiki entries with English first, followed by the
    /// remaining entries sorted alphabetically by language code.
    /// </summary>
    private List<string> InterwikiAlphaEnFirst;

    /// <summary>
    /// Comparer used to order interwiki links.
    /// </summary>
    private InterWikiComparer Comparer;

    /// <summary>
    /// Stores the currently selected interwiki ordering method.
    /// </summary>
    private InterWikiOrderEnum Order = InterWikiOrderEnum.LocalLanguageAlpha;

    // TODO (Modernization):
    // Review the InterWikiOrder setter for simplification and performance.
    // The current implementation rebuilds the interwiki language list and
    // creates a new InterWikiComparer each time the ordering changes. Determine
    // whether these objects can be cached or updated incrementally, and consider
    // replacing the switch statement with a more maintainable mapping once
    // regression tests are in place.
    /// <summary>
    /// Gets or sets the ordering method used when sorting interwiki links.
    /// </summary>
    /// <remarks>
    /// Setting this property updates the internal comparer used to order
    /// interwiki links for the current project.
    /// </remarks>
    public InterWikiOrderEnum InterWikiOrder
    {
        // Interwiki ordering definitions are based on:
        // https://meta.wikimedia.org/wiki/Interwiki_sorting_order
        set
        {
            Order = value;

            List<string> seq = GetInterWikiSequence(Order);

            PossibleInterwikis = SiteMatrix.GetProjectLanguages(Variables.Project);
            Comparer = CreateInterWikiComparer(seq);
        }

        get
        {
            return Order;
        }
    }

    /// <summary>
    /// Gets the interwiki ordering sequence associated with the specified
    /// ordering mode.
    /// </summary>
    /// <param name="order">
    /// The interwiki ordering mode.
    /// </param>
    /// <returns>
    /// The corresponding interwiki ordering sequence.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="order"/> does not represent a supported
    /// interwiki ordering mode.
    /// </exception>
    private List<string> GetInterWikiSequence(InterWikiOrderEnum order)
    {
        switch (order)
        {
            case InterWikiOrderEnum.Alphabetical:
                return InterwikiAlpha;

            case InterWikiOrderEnum.AlphabeticalEnFirst:
                return InterwikiAlphaEnFirst;

            case InterWikiOrderEnum.LocalLanguageAlpha:
                return InterwikiLocalAlpha;

            case InterWikiOrderEnum.LocalLanguageFirstWord:
                return InterwikiLocalFirst;

            default:
                throw new ArgumentOutOfRangeException(
                    "MetaDataSorter.InterWikiOrder",
                    (Exception)null);
        }
    }

    /// <summary>
    /// Creates the comparer used to sort interwiki links for the current project.
    /// </summary>
    /// <param name="sequence">
    /// The interwiki ordering sequence to use.
    /// </param>
    /// <returns>
    /// A comparer configured for the selected interwiki ordering and current
    /// project languages.
    /// </returns>
    private InterWikiComparer CreateInterWikiComparer(List<string> sequence)
    {
        return new InterWikiComparer(
            new List<string>(sequence),
            PossibleInterwikis);
    }

    /// <summary>
    /// Tracks whether all requested interwiki data was successfully loaded
    /// from the object cache.
    /// </summary>
    private bool Loaded = true;

    /// <summary>
    /// Loads a cached interwiki data collection.
    /// </summary>
    /// <param name="what">
    /// The cache entry name identifying the interwiki data to load.
    /// </param>
    /// <returns>
    /// The cached interwiki data, or an empty list if no cached value is found.
    /// </returns>
    private List<string> Load(string what)
    {
        var result = (List<string>)ObjectCache.Global.Get<List<string>>(Key(what));
        if (result == null)
        {
            Loaded = false;
            return new List<string>();
        }

        return result;
    }

    /// <summary>
    /// Saves the current interwiki ordering collections to the object cache.
    /// </summary>
    private void SaveInterWikiToCache()
    {
        ObjectCache.Global.Set(Key("InterwikiLocalAlpha"), InterwikiLocalAlpha);
        ObjectCache.Global.Set(Key("InterwikiLocalFirst"), InterwikiLocalFirst);
        ObjectCache.Global.Set(Key("InterwikiAlpha"), InterwikiAlpha);
        ObjectCache.Global.Set(Key("InterwikiAlphaEnFirst"), InterwikiAlphaEnFirst);
    }

    /// <summary>
    /// Builds the object-cache key used for <see cref="MetaDataSorter"/> data.
    /// </summary>
    /// <param name="what">
    /// The name of the metadata value being cached.
    /// </param>
    /// <returns>
    /// The namespaced cache key for the specified metadata value.
    /// </returns>
    private static string Key(string what)
    {
        return "MetaDataSorter::" + what;
    }

    /// <summary>
    /// Loads the interwiki ordering data from the local object cache when
    /// available.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if all required interwiki ordering data was loaded
    /// successfully, or if unit-test data was initialized; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// When unit-test mode is enabled, predefined interwiki ordering data is used
    /// instead of reading from the cache.
    /// </remarks>
    private bool LoadInterWikiFromCache()
    {
        if (Globals.UnitTestMode)
        {
            LoadUnitTestInterWikiData();
            return true;
        }

        InterwikiLocalAlpha = Load("InterwikiLocalAlpha");
        InterwikiLocalFirst = Load("InterwikiLocalFirst");
        InterwikiAlpha = Load("InterwikiAlpha");
        InterwikiAlphaEnFirst = Load("InterwikiAlphaEnFirst");

        return Loaded;
    }

    /// <summary>
    /// Initializes the predefined interwiki ordering data used during unit tests.
    /// </summary>
    private void LoadUnitTestInterWikiData()
    {
        List<string> one = new List<string> { "ar", "de", "en", "ru", "sq" };
        List<string> two = new List<string> { "en", "ar", "de", "ru", "sq" };

        InterwikiLocalAlpha = one;
        InterwikiLocalFirst = one;
        InterwikiAlpha = one;
        InterwikiAlphaEnFirst = two;
    }

    /// <summary>
    /// Provides the U.S. English culture used by metadata sorting operations.
    /// </summary>
    private static readonly CultureInfo EnUsCulture =
        new CultureInfo("en-US", true);

    /// <summary>
    /// Loads the interwiki ordering data from the configured source and
    /// initializes the supported ordering collections.
    /// </summary>
    /// <remarks>
    /// During normal operation, the ordering data is downloaded from the
    /// AutoWikiBrowser interwiki definition page. In unit-test mode, a
    /// predefined data set is used instead.
    /// </remarks>
    private void LoadInterWikiFromNetwork()
    {
        string text = GetInterWikiSourceText();

        InterwikiLocalAlpha = ParseInterWikiList(
            text,
            "<!--InterwikiLocalAlphaBegins-->",
            "<!--InterwikiLocalAlphaEnds-->");

        InterwikiLocalFirst = ParseInterWikiList(
            text,
            "<!--InterwikiLocalFirstBegins-->",
            "<!--InterwikiLocalFirstEnds-->");

        BuildAlphabeticalInterWikiLists();
    }

    /// <summary>
    /// Gets the raw interwiki ordering definition text.
    /// </summary>
    /// <returns>
    /// The interwiki ordering definition text used to initialize the ordering
    /// collections.
    /// </returns>
    private static string GetInterWikiSourceText()
    {
        return !Globals.UnitTestMode
            ? Tools.GetHTML(
                "https://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/IW&action=raw")
            : @"<!--InterwikiLocalAlphaBegins-->
ru, sq, en
<!--InterwikiLocalAlphaEnds-->
<!--InterwikiLocalFirstBegins-->
en, sq, ru
<!--InterwikiLocalFirstEnds-->";
    }

    /// <summary>
    /// Extracts and normalizes an interwiki ordering list from the supplied
    /// definition text.
    /// </summary>
    /// <param name="text">
    /// The raw interwiki definition text.
    /// </param>
    /// <param name="startMarker">
    /// The marker identifying the beginning of the ordering list.
    /// </param>
    /// <param name="endMarker">
    /// The marker identifying the end of the ordering list.
    /// </param>
    /// <returns>
    /// A normalized list of interwiki language codes.
    /// </returns>
    private static List<string> ParseInterWikiList(
        string text,
        string startMarker,
        string endMarker)
    {
        string raw =
            RemExtra(
                Tools.StringBetween(
                    text,
                    startMarker,
                    endMarker));

        List<string> result = new List<string>();

        foreach (string s in raw.Split(
            new[] { "," },
            StringSplitOptions.RemoveEmptyEntries))
        {
            result.Add(s.Trim().ToLower());
        }

        return result;
    }

    /// <summary>
    /// Builds the derived alphabetical interwiki ordering collections.
    /// </summary>
    private void BuildAlphabeticalInterWikiLists()
    {
        InterwikiAlpha = new List<string>(InterwikiLocalFirst);
        InterwikiAlpha.Sort(StringComparer.Create(EnUsCulture, true));

        InterwikiAlphaEnFirst = new List<string>(InterwikiAlpha);
        InterwikiAlphaEnFirst.Remove("en");
        InterwikiAlphaEnFirst.Insert(0, "en");
    }

    /// <summary>
    /// Removes extra formatting characters from extracted metadata text.
    /// </summary>
    /// <param name="input">
    /// The text to normalize.
    /// </param>
    /// <returns>
    /// The input text with carriage returns, line feeds, and closing angle
    /// brackets removed.
    /// </returns>
    private static string RemExtra(string input)
    {
        return input.Replace("\r\n", "").Replace(">", "").Replace("\n", "");
    }

    /// <summary>
    /// Matches English interwiki links that have been commented out in article
    /// text.
    /// </summary>
    private static readonly Regex CommentedOutEnInterwiki =
        new Regex("<!-- ?\\[\\[en:.*?\\]\\] ?-->");

    /// <summary>
    /// Sorts article metadata, including optional whitespace fixing
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <returns>The updated article text</returns>
    internal string Sort(string articleText, string articleTitle)
    {
        return Sort(articleText, articleTitle, true);
    }

    /// <summary>
    /// Contains the known disambiguation-link templates together with the
    /// <c>hatnote group</c> template.
    /// </summary>
    private static readonly List<string> DablinksPlusHatnoteGroupList =
        WikiRegexes.DablinksList
            .Union(new List<string>(new[] { "hatnote group" }))
            .ToList();

    /// <summary>
    /// Matches disambiguation-link templates and the
    /// <c>{{hatnote group}}</c> template, including nested template content.
    /// </summary>
    private static readonly Regex DablinksPlusHatnoteGroup =
        Tools.NestedTemplateRegex(DablinksPlusHatnoteGroupList);

    /// <summary>
    /// Matches the beginning of a wiki table at the start of a line.
    /// </summary>
    private static readonly Regex WikiTable =
        new Regex(@"^{\|", RegexOptions.Multiline);

    /// <summary>
    /// Sorts article meta data
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="fixOptionalWhitespace">Whether to request optional excess whitespace to be fixed</param>
    /// <returns>The updated article text</returns>
    internal string Sort(string articleText, string articleTitle, bool fixOptionalWhitespace)
    {
        if (Namespace.Determine(articleTitle) == Namespace.Template || Namespace.Determine(articleTitle) == Namespace.Module) // Don't sort on templates/modules
            return articleText;

        // trim stray tab whitespace
        articleText = Regex.Replace(articleText, "\t+\r\n", "\r\n");

        // Performance: get all the templates so "move template" functions below only called when template(s) present in article
        List<string> alltemplates = Parsers.GetAllTemplates(articleText);

        // sort zeroth section
        if (Namespace.IsMainSpace(articleTitle) && !Tools.IsRedirect(articleText))
        {
            if (TemplateExists(alltemplates, TemplatesToEndOfArticle))
                articleText = MoveTemplateToEndOfArticle(articleText);

            string zerothSection = Tools.GetZerothSection(articleText);
            string restOfArticle = articleText.Substring(zerothSection.Length);

            // cannot safely apply sorting if noinclude/{{{1}}}/{{:ifexist/wikitable in zeroth section
            if (!Parsers.NoIncludeIncludeOnlyProgrammingElement(zerothSection) && !WikiTable.IsMatch(zerothSection) && !WikiRegexes.MagicWordBehaviourSwitches.IsMatch(zerothSection))
                articleText = SortZerothSection(zerothSection) + restOfArticle;
        }

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Substituted_templates
        // if article contains some substituted template stuff, sorting the data may mess it up (further)
        if (Namespace.IsMainSpace(articleTitle) && (Parsers.NoIncludeIncludeOnlyProgrammingElement(articleText)))
            return articleText;

        // short pages monitor check for en-wiki: keep at very end of article if present
        // See [[Template:Long comment/doc]]
        // SPM regex quick check for performance on long pages
        string shortPagesMonitor = string.Empty;
        if (Variables.LangCode.Equals("en") && alltemplates.Contains("Short pages monitor"))
        {
            Match spm = WikiRegexes.ShortPagesMonitor.Match(articleText);

            if (spm.Success)
            {
                articleText = WikiRegexes.ShortPagesMonitor.Replace(articleText, "").TrimEnd();
                shortPagesMonitor = spm.Value.TrimEnd();
            }
        }

        articleText = CommentedOutEnInterwiki.Replace(articleText, "");

        string personData = string.Empty;
        if (TemplateExists(alltemplates, WikiRegexes.Persondata))
            personData = Tools.Newline(RemovePersonData(ref articleText));

        string disambig = string.Empty;
        if (TemplateExists(alltemplates, WikiRegexes.Disambigs))
            disambig = Tools.Newline(RemoveDisambig(ref articleText));

        string categories = Tools.Newline(RemoveCats(ref articleText, articleTitle));

        string interwikis = Tools.Newline(Interwikis(ref articleText, TemplateExists(alltemplates, WikiRegexes.LinkFGAs)));

        if (Namespace.IsMainSpace(articleTitle) && !Tools.IsRedirect(articleText))
        {
            if (Variables.LangCode.Equals("en"))
            {
                if (TemplateExists(alltemplates, WikiRegexes.PortalTemplate))
                    articleText = MovePortalTemplates(articleText);

                if (TemplateExists(alltemplates, WikiRegexes.SisterLinks))
                    articleText = MoveSisterlinks(articleText);

                if (alltemplates.Contains("Ibid"))
                    articleText = MoveTemplateToReferencesSection(articleText, WikiRegexes.Ibid);

                articleText = MoveExternalLinks(articleText);

                articleText = MoveSeeAlso(articleText);
            }
        }
        // two newlines here per https://en.wikipedia.org/w/index.php?title=Wikipedia_talk:AutoWikiBrowser&oldid=243224092#Blank_lines_before_stubs
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Two_empty_lines_before_stub-templates
        // ru, sl, ar, arz wikis use only one newline
        // T382578: WP:STUBSPACING is now one blank line only for en-wiki
        string strStub = string.Empty;

        // Category: can use {{Verylargestub}}/{{popstub}} which is not a stub template, don't do stub sorting
        if (!Namespace.Determine(articleTitle).Equals(Namespace.Category) && TemplateExists(alltemplates, new Regex(Variables.Stub)))
            strStub = Tools.Newline(RemoveStubs(ref articleText), (Variables.LangCode.Equals("ru") || Variables.LangCode.Equals("sl") || Variables.LangCode.Equals("ar") || Variables.LangCode.Equals("arz") || Variables.IsWikipediaEN) ? 1 : 2);

        // filter out excess white space and remove "----" from end of article
        if (Namespace.IsMainSpace(articleTitle))
            articleText = articleText.TrimEnd(); // better to trim here than process more slowly in RemoveWhiteSpace where <poem> checks etc. needed
        articleText = Parsers.RemoveWhiteSpace(articleText, fixOptionalWhitespace) + "\r\n";

        articleText += disambig;
        if (TemplateExists(alltemplates, WikiRegexes.MultipleIssues))
            articleText = WikiRegexes.MultipleIssues.Replace(articleText, m => Regex.Replace(m.Value, "(\r\n)+", "\r\n"));

        switch (Variables.LangCode)
        {
            case "de":
            case "sl":
                articleText += strStub + categories + personData;

                // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser#Removal_of_blank_lines
                // on de wiki a blank line is desired between persondata and interwikis
                if (Variables.LangCode.Equals("de") && personData.Length > 0 && interwikis.Length > 0)
                    articleText += "\r\n";
                break;

            case "ar":
            case "arz":
            case "cs":
            case "el":
            case "dk":
            case "pl":
            case "ru":
            case "uk":
                articleText += personData + strStub + categories;
                break;

            case "it":
                if (Variables.Project == ProjectEnum.wikiquote)
                    articleText += personData + strStub + categories;
                else
                    articleText += personData + categories + strStub;
                break;

            default:
                articleText += personData + categories + strStub;
                break;
        }
        articleText += interwikis;

        // Only trim start on Category namespace, restore any saved short page monitor text
        return (Namespace.Determine(articleTitle) == Namespace.Category ? articleText.Trim() : articleText.TrimEnd()) + shortPagesMonitor;
    }

    /// <summary>
    /// Matches templates that require special handling and should not be
    /// processed by the standard metadata sorting logic.
    /// </summary>
    /// <remarks>
    /// These templates define structural or layout regions whose contents may
    /// not be safely reordered by the metadata sorter.
    /// </remarks>
    private static readonly Regex TemplatesCannotHandle =
        Tools.NestedTemplateRegex(
            new[]
            {
            "stack begin",
            "stack end",
            "stack",
            "Collapsed infobox section begin",
            "Collapsed infobox section end"
            });

    /// <summary>
    /// Sorts article meta data - zeroth section per [[MOS:ORDER]]
    /// </summary>
    /// <param name="zerothSection">The wiki text of the zeroth section of the article.</param>
    internal string SortZerothSection(string zerothSection)
    {
        int moveDisplayLowerCaseItalicTitle = DisplayLowerCaseItalicTitleNeedsMoving(zerothSection);
        List<string> alltemplates = Parsers.GetAllTemplates(zerothSection);

        // do nothing if zeroth section contains these templates: cannot handle them
        if (TemplateExists(alltemplates, TemplatesCannotHandle))
            return zerothSection;

        int bl;
        if (Parsers.UnbalancedBrackets(zerothSection, out bl) > 0)
            return zerothSection;

        bool deletionProtectionTagsComments = WikiRegexes.DeletionProtectionTags.Matches(zerothSection).Cast<Match>().Any(m => WikiRegexes.Comments.IsMatch(m.Value));

        // (rest of section) {{DISPLAYTITLE}}, {{Lowercase title}}, {{Italic title}} kept not directly after an infobox
        if (moveDisplayLowerCaseItalicTitle == 4)
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.DisplayLowerCaseItalicTitle);

        // (L9) Language maintenance templates after infoboxes, per [[MOS:ORDER]]
        if (TemplateExists(alltemplates, WikiRegexes.LanguageMaintenanceTemplates))
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.LanguageMaintenanceTemplates);

        // (L8-after) {{DISPLAYTITLE}}, {{Lowercase title}}, {{Italic title}} kept directly after an infobox
        if (moveDisplayLowerCaseItalicTitle == 3)
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.DisplayLowerCaseItalicTitle);

        // L8 infoboxes after templates relating to English variety and date format, per [[MOS:ORDER]]
        if (TemplateExists(alltemplates, WikiRegexes.InfoBox))
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.InfoBox);

        // (L8-pre) {{DISPLAYTITLE}}, {{Lowercase title}}, {{Italic title}} kept directly before an infobox
        if (moveDisplayLowerCaseItalicTitle == 2)
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.DisplayLowerCaseItalicTitle);

        // L7 Templates relating to English variety and date format after maintenance templates, per [[MOS:ORDER]]
        if (TemplateExists(alltemplates, WikiRegexes.UseDatesEnglishTemplates))
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.UseDatesEnglishTemplates);

        // L6 maintenance templates (including {{in use}}, {{bots}}) above templates relating to English variety and date format etc., zeroth section only
        if (TemplateExists(alltemplates, WikiRegexes.MosLevel6MaintenanceCleanupDispute))
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.MosLevel6MaintenanceCleanupDispute);

        // L5 deletion/protection templates above maintenance tags, below dablinks per [[MOS:ORDER]]
        // special allowance of comments on line(s) after template is required, but only apply if comments there, otherwise can pick up unrelated comments later in section
        if (TemplateExists(alltemplates, WikiRegexes.DeletionProtectionTags))
        {
            if (deletionProtectionTagsComments)
                zerothSection = MoveTemplate(zerothSection, WikiRegexes.DeletionProtectionTags);
            else
                zerothSection = MoveTemplate(zerothSection, Tools.NestedTemplateRegex(WikiRegexes.DeletionProtectionTagsList));
        }

        // L4 featured article templates above deletion/protection templates [[MOS:ORDER]]
        if (TemplateExists(alltemplates, WikiRegexes.GoodFeaturedArticleTemplates))
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.GoodFeaturedArticleTemplates);

        // L3 Hatnotes/Dablinks above maintenance tags per [[MOS:ORDER]]
        // if have {{hatnote group}} then move that plus any standalone individual dablinks
        if (TemplateExists(alltemplates, DablinksPlusHatnoteGroup))
            zerothSection = MoveTemplate(zerothSection, DablinksPlusHatnoteGroup);

        // L2 {{DISPLAYTITLE}}, {{Lowercase title}}, {{Italic title}} above hatnotes per [[MOS:ORDER]] if not being kept directly above, or after an infobox
        if (moveDisplayLowerCaseItalicTitle == 1)
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.DisplayLowerCaseItalicTitle);

        // L1 {{short description}} above dablinks per [[MOS:ORDER]]
        if (TemplateExists(alltemplates, WikiRegexes.ShortDescriptionTemplate))
            zerothSection = MoveTemplate(zerothSection, WikiRegexes.ShortDescriptionTemplate);

        return zerothSection;
    }

    /// <summary>
    /// Finds current relative order of DISPLAYTITLE/italic title/lowercase title templates versus infobox and other templates, so MetaDataSorter knows where to sort them
    /// </summary>
    /// <param name="articleText">Article text.</param>
    private static int DisplayLowerCaseItalicTitleNeedsMoving(string articleText)
    {
        List<string> alltemplatesZ = WikiRegexes.NestedTemplates.Matches(Tools.GetZerothSection(articleText)).Cast<Match>().Select(m => m.Value).ToList();

        // determine relative position of DISPLAYTITLE/italic title/lowercase title template and any infobox
        int displaytitle = alltemplatesZ.FindIndex(t => WikiRegexes.DisplayLowerCaseItalicTitle.IsMatch(t));
        int infobox = alltemplatesZ.FindIndex(t => WikiRegexes.InfoBox.IsMatch(t));

        if (displaytitle > -1)
        {
            /* If no infobox then template should be sorted per MOS:ORDER in 2nd position (after short description)
             * If have infobox, but there are other templates inbetween, then again template should be sorted in 2nd position
             * If template just before infobox, or anywhere after infobox, don't sort it, as MOS:ORDER allows those positions too
             * */
            if (infobox == -1) // no infobox
                return 1;
            if (infobox - displaytitle > 1) // currently further before infobox
                return 1;
            if (infobox - displaytitle == 1) // currently just before infobox
                return 2;
            if (displaytitle - infobox == 1) // currently just after infobox
                return 3;
            if (displaytitle > infobox) // currently further after infobox
                return 4;
        }

        return 0;
    }
    /// <summary>
    /// Matches templates that are conventionally placed near the end of an
    /// article, such as coordinate and authority control templates.
    /// </summary>
    /// <remarks>
    /// The regular expression is generated using
    /// <see cref="Tools.NestedTemplateRegex(string[])"/> to correctly match
    /// nested template structures.
    /// </remarks>
    private static readonly Regex TemplatesToEndOfArticle =
        Tools.NestedTemplateRegex(
            new[]
            {
            "coord",
            "WikidataCoord",
            "Sky",
            "Authority control",
            "coord missing"
            });

    /// <summary>
    /// Moves templates to end of article text, from zeroth section only
    /// </summary>
    /// <param name="articleText"></param>
    /// <returns></returns>
    internal string MoveTemplateToEndOfArticle(string articleText)
    {
        string originalArticleText = articleText;
        string zerothSection = Tools.GetZerothSection(articleText);
        List<string> allTemplatesDetail = Parsers.GetAllTemplateDetail(zerothSection);

        allTemplatesDetail = allTemplatesDetail.Where(t => TemplatesToEndOfArticle.IsMatch(t)).ToList();

        // nothing to do if no templates found
        if (!allTemplatesDetail.Any())
            return articleText;

        // find last section of article
        MatchCollection hc = WikiRegexes.Headings.Matches(articleText);
        string lastSection = articleText, restOfArticleText = articleText;

        if (hc.Count > 0)
        {
            int h = hc[hc.Count - 1].Index;
            lastSection = lastSection.Substring(h);
            restOfArticleText = articleText.Substring(0, h);
        }
        else
            return articleText;

        // nothing to do if template is already in last section
        if (!TemplatesToEndOfArticle.IsMatch(restOfArticleText) || allTemplatesDetail.Any(t => lastSection.Contains(t)))
            return articleText;

        string allTemplatesFound = string.Empty;
        foreach (Match m in WikiRegexes.NestedTemplates.Matches(articleText))
        {
            if (!TemplatesToEndOfArticle.IsMatch(m.Value) || TemplatesToEndOfArticle.Match(m.Value).Index > 0)
                continue;

            // only pull templates from zeroth section
            if (m.Index > zerothSection.Length)
                continue;

            string templateFound = m.Value;
            if (Regex.IsMatch(articleText, @"^" + Regex.Escape(templateFound), RegexOptions.Multiline))
            {
                articleText = Regex.Replace(articleText, @"^" + Regex.Escape(templateFound) + @" *(?:\r\n)?", "", RegexOptions.Multiline);

                allTemplatesFound += "\r\n" + templateFound;
            }
        }

        articleText += allTemplatesFound;

        if (!Tools.UnformattedTextNotChanged(originalArticleText, articleText))
            return originalArticleText;

        return articleText;
    }

    /// <summary>
    /// Determines whether the specified template collection contains a template
    /// matching the supplied regular expression.
    /// </summary>
    /// <param name="templatesFound">
    /// The collection of template names found in the article.
    /// </param>
    /// <param name="r">
    /// The regular expression used to identify the template.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a matching template is found; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Template names are converted into simple wiki template markup before being
    /// evaluated against the supplied regular expression.
    /// </remarks>
    private static bool TemplateExists(List<string> templatesFound, Regex r)
    {
        return templatesFound.Any(s => r.IsMatch(@"{{" + s + "}}"));
    }

    /// <summary>
    /// Matches the <c>{{Lifetime}}</c> template.
    /// </summary>
    private static readonly Regex LifeTime =
        Tools.NestedTemplateRegex("Lifetime");

    /// <summary>
    /// Matches the <c>{{NF}}</c> template.
    /// </summary>
    private static readonly Regex NF =
        Tools.NestedTemplateRegex("NF");

    /// <summary>
    /// Matches deletion-related maintenance categories.
    /// </summary>
    private static readonly Regex CatsForDeletion =
        new Regex(
            @"\[\[Category:(Pages|Categories|Articles) for deletion\]\]");

    /// <summary>
    /// Matches templates requesting additional or improved article
    /// categorization.
    /// </summary>
    private static readonly Regex UncategorizedImproveCats =
        Tools.NestedTemplateRegex(
            new[]
            {
                "+cat", "Additional categories", "Categories improve", "Categories missing", "Categories needed",
                "Categories requested", "Categories-improve", "Categorise", "Categorize", "Categorízame",
                "Category improve", "Category needed", "Category requested", "Category-improve", "Categoryneeded",
                "Cat improve", "Cat needed", "Cat-improve", "CatNeeded", "Catimprove",
                "Catneeded", "CI", "Ci", "Cleanup cat", "Cleanup-cat",
                "Few categories", "Few cats", "Fewcategories", "Fewcats", "Improve categorization",
                "Improve cat", "Improve categories", "Improve cats", "Improve-categories", "Improve-cats",
                "Improvecategories", "Improvecats", "Missing categories", "More categories", "More category",
                "More cats", "Morecat", "Morecategories", "Morecats", "Ncat",
                "No categories", "No category", "No cats", "Noc", "Nocat",
                "Nocats", "Nocategory", "Needs cat", "Needs categories", "Needs cats",
                "Uncat", "Uncat stub", "Uncat-stub", "Uncategorized", "Uncategorized stub",
                "Uncategorizedstub", "Undercategorised", "Undercategorised stub", "Undercategorisedstub", "Undercategorized",
                "Undercat", "Uncategorised", "Uncategorizedstub", "Uncatstub"
            });

    /// <summary>
    /// Extracts DEFAULTSORT + categories from the article text; removes duplicate categories, cleans whitespace and underscores
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <returns>The cleaned page categories in a single string</returns>
    public string RemoveCats(ref string articleText, string articleTitle)
    {
        // don't pull category from redirects to a category e.g. page Hello is #REDIRECT[[Category:Hello]]
        string rt = Tools.RedirectTarget(articleText);
        if (rt.Length > 0 && WikiRegexes.Category.IsMatch(@"[[" + rt + @"]]"))
            return "";

        List<string> categoryList = new List<string>();
        string articleTextNoComments = Tools.ReplaceWithSpaces(articleText, WikiRegexes.Comments.Matches(articleText));

        // don't operate on pages with (incorrectly) multiple defaultsorts
        // ignore commented out DEFAULTSORT – https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_12#Moving_DEFAULTSORT_in_HTML_comments
        // but exact duplicate DEFAULTSORT can be cleaned
        MatchCollection mc = WikiRegexes.Defaultsort.Matches(articleTextNoComments);
        if (mc.Count > 1)
        {
            articleText = WikiRegexes.Defaultsort.Replace(articleText, d =>
            {
                if (d.Index > mc[0].Index && mc[0].Value == d.Value) // exact dupe
                    return "";
                return d.Value;
            });

            // check count after attempted deduplication
            articleTextNoComments = Tools.ReplaceWithSpaces(articleText, WikiRegexes.Comments.Matches(articleText));
            if (WikiRegexes.Defaultsort.Matches(articleTextNoComments).Count > 1)
            {
                Tools.WriteDebug("RemoveCats", "Page " + articleTitle + " has multiple DEFAULTSORTs");
                return "";
            }
        }

        string defaultSort = string.Empty;
        bool defaultSortRemoved = false;

        // allow comments between categories, and keep them in the same place, only grab any comment after the last category if on same line
        // whitespace: remove all whitespace after, but leave a blank newline before a heading (rare case where category not in last section)

        // performance: apply regex on portion of article containing category links rather than whole text
        Match cq = WikiRegexes.CategoryQuick.Match(articleTextNoComments);

        if (cq.Success)
        {
            // T387084 don't apply sort where magic word behavior switches present as these can be placed anywhere in article
            if (WikiRegexes.MagicWordBehaviourSwitches.IsMatch(articleText.Substring(cq.Index)))
                return "";

            List<string> allUnformatted = (from Match m in WikiRegexes.UnformattedText.Matches(articleText)
                                           select m.Value).ToList();

            int cutoff = Math.Max(0, cq.Index - 500);
            string cut = articleText.Substring(cutoff);

            // if unformatted text is matched by the cats regex then it's a commented out category or a category comment itself containing a category, which we can handle as normal
            List<string> catsList = WikiRegexes.RemoveCatsAllCats.Matches(cut).Cast<Match>().Select(m => m.Value).ToList();
            allUnformatted.RemoveAll(u => catsList.Any(c => c.Contains(u)));

            cut = WikiRegexes.RemoveCatsAllCats.Replace(cut, m =>
            {
                // don't pull cats from wiki comments/unformatted text regions
                if (allUnformatted.Any(u => u.Contains(m.Value.Trim()) && !u.Equals(m.Value.Trim()) && !categoryList.Contains(u)))
                    return m.Value;

                if (!CatsForDeletion.IsMatch(m.Value))
                    categoryList.Add(m.Value.Trim());

                // if category not at start of line, leave newline, otherwise text on next line moved up
                if (m.Index > 2 && !cut.Substring(m.Index - 2, 2).Trim().Equals(""))
                    return "\r\n";

                return "";
            });

            if (AddCatKey)
                categoryList = CatKeyer(categoryList, articleTitle);

            // now refresh defaultsort to pick up any comment on same line after it
            if (mc.Count > 0)
                mc = Regex.Matches(articleText, WikiRegexes.Defaultsort + @"(?: *<!--[^<>]*-->)?");

            // remove defaultsort now if we can, faster to remove from cut than whole articleText
            if (mc.Count > 0 && cut.Contains(mc[0].Value))
            {
                cut = cut.Replace(mc[0].Value, "");
                defaultSortRemoved = true;
            }

            articleText = articleText.Substring(0, cutoff) + cut;

            if (CatCommentRegex.IsMatch(cut))
                articleText = CatCommentRegex.Replace(articleText, m =>
                {
                    categoryList.Insert(0, m.Value);
                    return "";
                }, 1);
        }

        if (Variables.LangCode.Equals("sl") && LifeTime.IsMatch(articleText))
        {
            defaultSort = LifeTime.Match(articleText).Value;
        }
        if (Variables.LangCode.Equals("es") && NF.IsMatch(articleText))
        {
            defaultSort = NF.Match(articleText).Value;
        }
        else if (mc.Count > 0)
            defaultSort = mc[0].Value;

        if (!string.IsNullOrEmpty(defaultSort))
        {
            // if defaultsort wasn't in the cut area before the categories, remove now
            if (!defaultSortRemoved)
                articleText = articleText.Replace(defaultSort, "");

            if (defaultSort.ToUpper().Contains("DEFAULTSORT"))
                defaultSort = TalkPageFixes.FormatDefaultSort(defaultSort);
            defaultSort += "\r\n";
        }

        // Extract any {{uncategorized}}/{{improve categories}} template, only from last section
        // extract last section, if present, as text from final heading
        // if no headings then last section is whole article
        MatchCollection hc = WikiRegexes.Headings.Matches(articleText);
        string lastSection = articleText;
        string lastSectionNoComments = articleTextNoComments;
        string restOfArticleText = string.Empty;

        if (hc.Count > 0)
        {
            int h = hc[hc.Count - 1].Index;
            lastSection = lastSection.Substring(h);
            lastSectionNoComments = Tools.ReplaceWithSpaces(lastSection, WikiRegexes.Comments.Matches(lastSection));
            restOfArticleText = articleText.Substring(0, h);
        }

        string uncat = string.Empty;
        if (UncategorizedImproveCats.IsMatch(lastSectionNoComments) && !UncategorizedImproveCats.IsMatch(restOfArticleText))
        {
            articleText = UncategorizedImproveCats.Replace(articleText, uncatm =>
            {
                // remove exact duplicates
                if (!uncat.Contains(uncatm.Value))
                    uncat += uncatm.Value + "\r\n";

                return "";
            });
            // now process {{multiple issues}} in case {{improve categories}} was in it and MI needs cleanup
            Parsers p = new Parsers();
            articleText = p.MultipleIssues(articleText);
        }

        // per MOS:ORDER {{Improve categories}} or {{Uncategorized}} after cats if in last section
        if (Variables.IsWikipediaEN)
            return defaultSort + ListToString(categoryList) + uncat;

        return uncat + defaultSort + ListToString(categoryList);
    }

    /// <summary>
    /// Extracts the persondata template from the articleText, along with the persondata comment, if present on the line before
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns></returns>
    public static string RemovePersonData(ref string articleText)
    {
        string strPersonData = "", originalArticleText = articleText;

        articleText = WikiRegexes.Persondata.Replace(articleText, m =>
        {
            strPersonData += (strPersonData.Length == 0 ? m.Value : Tools.Newline(m.Value));
            return "";
        });

        if (!Tools.UnformattedTextNotChanged(originalArticleText, articleText))
        {
            articleText = originalArticleText;
            strPersonData = string.Empty;
        }

        return strPersonData;
    }

    /// <summary>
    /// Extracts stub templates from the article text
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns></returns>
    public static string RemoveStubs(ref string articleText)
    {
        // Per https://ru.wikipedia.org/wiki/Википедия:Опросы/Использование_служебных_разделов/Этап_2#.D0.A1.D0.BB.D1.83.D0.B6.D0.B5.D0.B1.D0.BD.D1.8B.D0.B5_.D1.88.D0.B0.D0.B1.D0.BB.D0.BE.D0.BD.D1.8B
        // Russian Wikipedia places stubs before navboxes
        if (Variables.LangCode.Equals("ru"))
            return "";

        List<string> stubList = new List<string>();
        string originalArticleText = articleText;

        articleText = WikiRegexes.PossiblyCommentedStub.Replace(articleText, m =>
        {
            if (!Regex.IsMatch(m.Value, Variables.SectStub))
            {
                stubList.Add(m.Value);
                return "";
            }

            return m.Value;
        });

        // Don't pull stubs out of comments
        if (!Tools.UnformattedTextNotChanged(originalArticleText, articleText + ListToString(stubList)))
        {
            articleText = originalArticleText;
            return "";
        }

        // en-wp only: remove {{stub}} if a more specific stub exists (not counting {{uncategorized stub}} template)
        if (Variables.IsWikipediaEN)
        {
            List<string> cp = new List<string>(stubList);
            cp.RemoveAll(s => Tools.GetTemplateName(s).ToLower().StartsWith("uncategori"));

            if (Parsers.GetAllTemplateDetail(ListToString(cp)).Count > 1)
                stubList.RemoveAll(s => Tools.GetTemplateName(s).TrimStart('-').ToLower().Equals("stub"));
        }

        return (stubList.Any()) ? ListToString(stubList) : "";
    }

    /// <summary>
    /// Removes any disambiguation templates from the article text, to be added at bottom later
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns>Article text stripped of disambiguation templates</returns>
    public static string RemoveDisambig(ref string articleText)
    {
        if (!Variables.LangCode.Equals("en"))
            return "";

        string strDisambig = string.Empty;

        // Extract up to one disambig (should not be multiple per page), don't pull out of comments
        if (WikiRegexes.Disambigs.IsMatch(WikiRegexes.Comments.Replace(articleText, "")))
        {
            articleText = WikiRegexes.Disambigs.Replace(articleText, m =>
            {
                strDisambig = m.Value;
                return "";
            }, 1);
        }

        return strDisambig;
    }

    /// <summary>
    /// Moves matching templates in the zeroth section to the top of the article (en only); ignoring section templates
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="templateRegex">Regex matching the templates to  be moved</param>
    /// <returns>Article text with disambiguation links at top</returns>
    public static string MoveTemplate(string articleText, Regex templateRegex)
    {
        string originalArticletext = articleText;

        // get the zeroth section (text upto first heading)
        string zerothSection = Tools.GetZerothSection(articleText);

        List<string> t1 = Parsers.GetAllTemplates(zerothSection).Where(t => templateRegex.IsMatch("{{" + t + "}}")).ToList();
        List<string> t2 = Parsers.GetAllTemplates(WikiRegexes.Comments.Replace(zerothSection, "")).Where(t => templateRegex.IsMatch("{{" + t + "}}")).ToList();

        // avoid moving commented out templates / part commented out
        if (!Variables.LangCode.Equals("en") || t1.Except(t2).Any())
            return articleText;

        // comment handling: a comment at start of line above the template belongs to the template, a comment on the same line as template belongs to the template
        templateRegex = new Regex(@"(?:^<!--\s*[^<>\r\n]+\s*-->\s*){0,5}" + templateRegex + @"(?: *<!--[^<>]+--> ?)*", RegexOptions.Multiline);

        // get the rest of the article including first heading (may be null if article has no headings)
        string restOfArticle = articleText.Substring(zerothSection.Length);

        string strTemplates = string.Empty;

        // extract templates, not section ones
        List<string> theTemplates = templateRegex.Matches(zerothSection).Cast<Match>().Where(m => Tools.GetTemplateArgument(m.Value, 1) != "section").Select(m => m.Value).ToList();

        // deduplicate tags
        List<string> theTemplatesDeduplicated = Parsers.DeduplicateMaintenanceTags(theTemplates);

        // determine whether multiple templates folded together e.g. as is often done with Infobox weather event template that has sub-templates
        bool folded = theTemplates.Count > 2 && zerothSection.Contains(string.Join("", theTemplates.ToArray()));

        // remove existing from article
        foreach (string t in theTemplates)
        {
            // remove any colon before template, whitespace after template
            zerothSection = Regex.Replace(zerothSection, ":?" + Regex.Escape(t) + @" *(?:\r\n)?", "");
        }

        // rebuild new
        foreach (string t in theTemplatesDeduplicated)
        {
            strTemplates += t + (folded ? "" : "\r\n");
        }

        if (folded)
            strTemplates += "\r\n";

        articleText = strTemplates + zerothSection + restOfArticle;

        // avoid moving commented out templates, round 2
        if (Tools.UnformattedTextNotChanged(originalArticletext, articleText))
            return articleText;

        return originalArticletext;
    }

    /// <summary>
    /// Matches the complete "External links" section when it is followed by
    /// another section heading.
    /// </summary>
    private static readonly Regex ExternalLinksSection =
        new Regex(
            @"(^== *[Ee]xternal +[Ll]inks? *==.*?)(?=^==+[^=][^\r\n]*?[^=]==+(\r\n?|\n)$)",
            RegexOptions.Multiline | RegexOptions.Singleline);

    /// <summary>
    /// Matches the "External links" section through the end of the article.
    /// </summary>
    private static readonly Regex ExternalLinksToEnd =
        new Regex(
            @"(==+) *[Ee]xternal +[Ll]inks? *\1.*",
            RegexOptions.Singleline);

    /// <summary>
    /// Moves sisterlinks such as {{wiktionary}} to the external links section
    /// </summary>
    /// <param name="articleText">The article text</param>
    /// <returns>The updated article text</returns>
    public static string MoveSisterlinks(string articleText)
    {
        string originalArticletext = articleText;
        foreach (Match m in WikiRegexes.SisterLinks.Matches(articleText))
        {
            string sisterlinkFound = m.Value;
            string ExternalLinksSectionString = ExternalLinksSection.Match(articleText).Value;

            // if ExteralLinksSection didn't match then 'external links' must be last section
            if (ExternalLinksSectionString.Length == 0)
                ExternalLinksSectionString = ExternalLinksToEnd.Match(articleText).Value;

            // need to have an 'external links' section to move the sisterlinks to
            // check sisterlink NOT currently in 'external links'
            if (ExternalLinksSectionString.Length > 0 && !ExternalLinksSectionString.Contains(sisterlinkFound.Trim()))
            {
                articleText = Regex.Replace(articleText, Regex.Escape(sisterlinkFound) + @"\s*(?:\r\n)?", "");
                articleText = WikiRegexes.ExternalLinksHeader.Replace(articleText, "$0" + "\r\n" + sisterlinkFound);
            }
        }

        if (Tools.UnformattedTextNotChanged(originalArticletext, articleText))
            return articleText;

        return originalArticletext;
    }

    /// <summary>
    /// Moves multiple issues template to the top of the article text.
    /// Does not move tags when only non-infobox templates are above the last tag
    /// For en-wiki apply this to zeroth section of article only
    /// </summary>
    /// <param name="articleText">the article text</param>
    /// <returns>the modified article text</returns>
    public static string MoveMultipleIssues(string articleText)
    {
        string originalArticleText = articleText;
        int multipleIssuesIndex = -1, infoboxIndex = -1;

        foreach (Match m in WikiRegexes.NestedTemplates.Matches(articleText))
        {
            if (Tools.GetTemplateName(m.Value).ToLower().Contains("infobox"))
                infoboxIndex = m.Index;
            else if (WikiRegexes.MultipleIssues.IsMatch(m.Value))
                multipleIssuesIndex = m.Index;
        }

        if (multipleIssuesIndex > infoboxIndex && infoboxIndex > -1)
        {
            string multipleIssues = WikiRegexes.MultipleIssues.Match(articleText).Value;

            articleText = multipleIssues + "\r\n" + articleText.Replace(multipleIssues, "");

            if (!Tools.UnformattedTextNotChanged(originalArticleText, articleText))
                return originalArticleText;
        }

        return articleText;
    }

    private static readonly Regex SeeAlsoSection = new Regex(@"(^== *[Ss]ee also *==.*?)(?=^==[^=][^\r\n]*?[^=]==(\r\n?|\n)$)", RegexOptions.Multiline | RegexOptions.Singleline);
    private static readonly Regex SeeAlsoToEnd = new Regex(@"(\s*(==+)\s*see\s+also\s*\2 *).*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// Moves template calls to the top of the "see also" section of the article
    /// </summary>
    /// <param name="articleText">The article text</param>
    /// <param name="TemplateToMove">The template calls to move</param>
    /// <returns>The updated article text</returns>
    public static string MoveTemplateToSeeAlsoSection(string articleText, Regex TemplateToMove)
    {
        MatchCollection mc = TemplateToMove.Matches(articleText);
        // need to have a 'see also' section to move the template to
        if (mc.Count < 1)
            return articleText;

        string originalArticletext = articleText;
        bool templateMoved = false;

        foreach (Match m in mc)
        {
            string TemplateFound = m.Value;
            Match sa = SeeAlsoSection.Match(articleText);
            string seeAlsoSectionString = sa.Value;
            int seeAlsoIndex = sa.Index;

            // if SeeAlsoSection didn't match then 'see also' must be last section
            if (seeAlsoSectionString.Length == 0)
            {
                Match sae = SeeAlsoToEnd.Match(articleText);
                seeAlsoSectionString = sae.Value;
                seeAlsoIndex = sae.Index;
            }

            // if still not found then no "see also" section to move templates to
            if (seeAlsoSectionString.Length == 0)
                break;

            // only move templates NOT currently in 'see also'
            if (m.Index < seeAlsoIndex || m.Index > (seeAlsoIndex + seeAlsoSectionString.Length))
            {
                // remove template, also remove newline after template if template on its own line
                articleText = Regex.Replace(articleText, @"^" + Regex.Escape(TemplateFound) + @" *(?:\r\n)?", "", RegexOptions.Multiline);

                articleText = articleText.Replace(TemplateFound, "");

                // place template at top of see also section
                articleText = WikiRegexes.SeeAlso.Replace(articleText, "$0" + Tools.Newline(TemplateFound));
                templateMoved = true;
            }
        }

        if (templateMoved && Tools.UnformattedTextNotChanged(originalArticletext, articleText))
            return articleText;

        return originalArticletext;
    }

    /// <summary>
    /// Moves any {{XX portal}} templates to the 'see also' section, if present (en only), per Template:Portal
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns>Article text with {{XX portal}} template correctly placed</returns>
    // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Placement_of_portal_template
    public static string MovePortalTemplates(string articleText)
    {
        return MoveTemplateToSeeAlsoSection(articleText, WikiRegexes.PortalTemplate);
    }

    private static readonly Regex ReferencesSectionRegex = new Regex(@"^== *[Rr]eferences *==\s*", RegexOptions.Multiline);
    private static readonly Regex NotesSectionRegex = new Regex(@"^== *[Nn]otes(?: and references)? *==\s*", RegexOptions.Multiline);
    private static readonly Regex FootnotesSectionRegex = new Regex(@"^== *(?:[Ff]ootnotes|Sources) *==\s*", RegexOptions.Multiline);

    /// <summary>
    /// Moves given template to the references section from the zeroth section, if present (en only)
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="TemplateRegex">A Regex to match the template to move</param>
    /// <param name="onlyfromzerothsection">Whether to check only the zeroth section of the article for the template</param>
    /// <returns>Article text with template correctly placed</returns>
    public static string MoveTemplateToReferencesSection(string articleText, Regex TemplateRegex, bool onlyfromzerothsection)
    {
        // no support for more than one of these templates in the article
        if (TemplateRegex.Matches(articleText).Count != 1 || (onlyfromzerothsection && TemplateRegex.Matches(WikiRegexes.ZerothSection.Match(articleText).Value).Count != 1))
            return articleText;

        // return if template is already in one the 'References', 'Notes' or 'Footnotes' sections
        string[] sec = Tools.SplitToSections(articleText);

        foreach (string s in sec)
        {
            if (TemplateRegex.IsMatch(s))
            {
                if (NotesSectionRegex.IsMatch(s) || ReferencesSectionRegex.IsMatch(s)
                   || FootnotesSectionRegex.IsMatch(s))
                    return articleText;
            }
        }

        // find the template position
        // the template must end up in one of the 'References', 'Notes' or 'Footnotes' section
        int templatePosition = TemplateRegex.Match(articleText).Index, notesSectionPosition = NotesSectionRegex.Match(articleText).Index;

        if (notesSectionPosition > 0 && templatePosition < notesSectionPosition)
            return MoveTemplateToSection(articleText, TemplateRegex, 2);

        int referencesSectionPosition = ReferencesSectionRegex.Match(articleText).Index;

        if (referencesSectionPosition > 0 && templatePosition < referencesSectionPosition)
            return MoveTemplateToSection(articleText, TemplateRegex, 1);

        int footnotesSectionPosition = FootnotesSectionRegex.Match(articleText).Index;

        if (footnotesSectionPosition > 0 && templatePosition < footnotesSectionPosition)
            return MoveTemplateToSection(articleText, TemplateRegex, 3);

        return articleText;
    }

    /// <summary>
    /// Moves the given template(s) from anywhere in the article to the references section.
    /// </summary>
    /// <returns>
    /// Updated article text
    /// </returns>
    /// <param name='articleText'>
    /// Article text.
    /// </param>
    /// <param name='templateRegex'>
    /// Regex to match the template(s) to be moved
    /// </param>
    public static string MoveTemplateToReferencesSection(string articleText, Regex templateRegex)
    {
        return MoveTemplateToReferencesSection(articleText, templateRegex, false);
    }

    /// <summary>
    /// Moves the given template(s) to the required section.
    /// </summary>
    /// <returns>
    /// Updated article text
    /// </returns>
    /// <param name='articleText'>
    /// Article text.
    /// </param>
    /// <param name='templateRegex'>
    /// Regex to match the template(s) to be moved
    /// </param>
    /// <param name='section'>
    /// Section (references/notes/footnotes)
    /// </param>
    private static string MoveTemplateToSection(string articleText, Regex templateRegex, int section)
    {
        string extractedTemplate = templateRegex.Match(articleText).Value;
        articleText = templateRegex.Replace(articleText, "");

        switch (section)
        {
            case 1:
                return ReferencesSectionRegex.Replace(articleText, "$0" + extractedTemplate + "\r\n", 1);
            case 2:
                return NotesSectionRegex.Replace(articleText, "$0" + extractedTemplate + "\r\n", 1);
            case 3:
                return FootnotesSectionRegex.Replace(articleText, "$0" + extractedTemplate + "\r\n", 1);
            default:
                return articleText;
        }
    }

    // TODO (Modernization):
    // Review references-section detection alongside WikiRegexes.ReferencesTemplates
    // and consolidate section-boundary handling into a dedicated, testable parser.
    /// <summary>
    /// Matches the complete "References" or "Notes" section when it is followed
    /// by another top-level section heading.
    /// </summary>
    private static readonly Regex ReferencesSection =
        new Regex(
            @"(^== *([Rr]eferences|Notes) *==.*?)(?=^==[^=][^\r\n]*?[^=]==(\r\n?|\n)$)",
            RegexOptions.Multiline | RegexOptions.Singleline);

    /// <summary>
    /// Matches a "References" or "Notes" section containing recognized reference
    /// templates immediately before default-sort or category metadata.
    /// </summary>
    private static readonly Regex ReferencesToEnd =
        new Regex(
            @"^== *([Rr]eferences|Notes) *==\s*" +
            WikiRegexes.ReferencesTemplates +
            @"\s*(?={{DEFAULTSORT\:|\[\[Category\:)",
            RegexOptions.Multiline);

    // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Place_.22External_links.22_section_after_.22References.22
    // TODO: only works when there is another section following the references section
    /// <summary>
    /// Ensures the external links section of an article is after the references section
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns>Article text with external links section below the references section</returns>
    public static string MoveExternalLinks(string articleText)
    {
        string articleTextAtStart = articleText;
        // is external links section above references?
        Match elm = ExternalLinksSection.Match(articleText);
        string externalLinks = elm.Groups[1].Value;

        // validate no <ref> in external links section
        if (!elm.Success || Regex.IsMatch(externalLinks, WikiRegexes.ReferenceEnd))
            return articleTextAtStart;

        string references = ReferencesSection.Match(articleText).Groups[1].Value;

        // references may be last section
        if (references.Length == 0)
            references = ReferencesToEnd.Match(articleText).Value;

        if (references.Length > 0 && elm.Index < articleText.IndexOf(references, StringComparison.Ordinal))
        {
            articleText = articleText.Replace(externalLinks, "");
            articleText = articleText.Replace(references, references + externalLinks);
        }

        return articleText;
    }

    /// <summary>
    /// Moves the 'see also' section to be above the 'references' section, subject to the limitation that the 'see also' section can't be the last level-2 section.
    /// Does not move section when two or more references sections in the same article
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns></returns>
    public static string MoveSeeAlso(string articleText)
    {
        // is 'see also' section below references?
        Match refSm = ReferencesSection.Match(articleText), seeAm = SeeAlsoSection.Match(articleText);
        string references = refSm.Groups[1].Value, seealso = seeAm.Groups[1].Value;

        if (seeAm.Success && seeAm.Index > refSm.Index && ReferencesSection.Matches(articleText).Count == 1)
        {
            articleText = articleText.Replace(seealso, "");
            articleText = articleText.Replace(references, seealso + "\r\n" + references);
        }
        // newlines are fixed by later logic
        return articleText;
    }

    /// <summary>
    /// Gets a list of Link FA/GA's from the article
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns>The List of {{Link [FG]A}}'s from the article</returns>
    private static List<string> RemoveLinkFGAs(ref string articleText)
    {
        MatchCollection matches =
            WikiRegexes.LinkFGAs.Matches(Tools.ReplaceWithSpaces(articleText, WikiRegexes.UnformattedText.Matches(articleText)));

        List<string> linkFGAList = (from Match m in matches select m.Value).ToList();
        articleText = Tools.RemoveMatches(articleText, matches);
        return linkFGAList;
    }

    /// <summary>
    /// Extracts all interwiki and featured-article interwiki links from the
    /// supplied article text.
    /// </summary>
    /// <param name="articleText">
    /// The article text. On return, this parameter contains the article text
    /// with the extracted interwiki and featured-article interwiki links
    /// removed. Interwiki links within comments and <c>&lt;nowiki&gt;</c>
    /// elements are ignored.
    /// </param>
    /// <returns>
    /// A string containing the extracted interwiki and featured-article
    /// interwiki links.
    /// </returns>
    public string Interwikis(ref string articleText)
    {
        return Interwikis(ref articleText, true);
    }

    /// <summary>
    /// Extracts all of the interwiki featured article and interwiki links from the article text
    /// Ignores interwikis in comments/nowiki tags
    /// </summary>
    /// <param name="articleText">Article text with interwiki and interwiki featured article links removed</param>
    /// <param name="linkFGAsInText"></param>
    /// <returns>string of interwiki featured article and interwiki links</returns>
    public string Interwikis(ref string articleText, bool linkFGAsInText)
    {
        string interWikiComment = string.Empty;
        if (articleText.Contains("<!--"))
            articleText = InterLangRegex.Replace(articleText, m =>
            {
                interWikiComment = m.Value;
                return "";
            }, 1);

        string interWikis = string.Empty;

        // Only search for linkFGAs if necessary
        if (linkFGAsInText)
            interWikis = ListToString(RemoveLinkFGAs(ref articleText));

        if (interWikiComment.Length > 0)
            interWikis += interWikiComment + "\r\n";

        interWikis += ListToString(RemoveInterWikis(ref articleText));

        return interWikis;
    }

    /// <summary>
    /// Extracts all of the interwiki links from the article text, handles comments beside interwiki links (not inline comments)
    /// </summary>
    /// <param name="articleText">Article text with interwikis removed</param>
    /// <returns>List of interwikis</returns>
    private List<string> RemoveInterWikis(ref string articleText)
    {
        List<string> interWikiList = new List<string>();

        // Performance: faster to get all wikilinks and filter on interwiki matches than simply run the regex on the whole article text
        var allInterwikisFound = (from Match m in WikiRegexes.WikiLink.Matches(articleText)
                                  where
            m.Value.Contains(":") && PossibleInterwikis.Contains(m.Groups[1].Value.Substring(0, m.Groups[1].Value.IndexOf(':')).Trim().ToLower())
                                  select m);

        if (!allInterwikisFound.Any())
            return interWikiList;

        // get all unformatted text in article to avoid taking interwikis from comments etc.
        StringBuilder ut = new StringBuilder();
        foreach (Match u in WikiRegexes.UnformattedText.Matches(articleText))
            ut.Append(u.Value);

        string unformattedText = ut.ToString();

        List<Match> goodMatches = new List<Match>();
        List<string> interWikiListLinksOnly = new List<string>();
        List<string> allTemplates = Parsers.GetAllTemplateDetail(articleText);

        foreach (Match m in WikiRegexes.PossibleInterwikis.Matches(articleText))
        {
            string site = m.Groups[1].Value.Trim().ToLower();

            // ignore interwikis in template calls
            if (!PossibleInterwikis.Contains(site) || allTemplates.Any((t => t.Contains((m.Value)))))
                continue;

            if (unformattedText.Contains(m.Value))
            {
                Tools.ReplaceOnce(ref unformattedText, m.Value, "");
                continue;
            }

            goodMatches.Add(m);

            // jbo is only Wikipedia article wiki that's first letter case sensitive
            string IWTarget = site.Equals("jbo") ? m.Groups[2].Value.Trim() : Tools.TurnFirstToUpper(m.Groups[2].Value.Trim());
            string IW = "[[" + site + ":" + IWTarget + "]]";

            // drop interwikis to own wiki, but not on commons where language = en and en interwikis go to Wikipedia
            if (!(m.Groups[1].Value.Equals(Variables.LangCode) && !Variables.IsWikimediaMonolingualProject) && !interWikiListLinksOnly.Contains(IW))
            {
                interWikiListLinksOnly.Add(IW);
                interWikiList.Add(IW + m.Groups[3].Value);
            }
        }

        articleText = Tools.RemoveMatches(articleText, goodMatches);

        if (SortInterwikis)
        {
            // sort twice to result in no reordering of two interwikis to same language project
            interWikiList.Sort(Comparer);
            interWikiList.Sort(Comparer);
        }

        return interWikiList;
    }

    /// <summary>
    /// Formats an interwiki-link regular expression match as normalized wiki
    /// link markup.
    /// </summary>
    /// <param name="match">
    /// The regular expression match containing the <c>site</c> and <c>text</c>
    /// capture groups.
    /// </param>
    /// <returns>
    /// The formatted interwiki link, with the site name converted to lowercase.
    /// </returns>
    public static string IWMatchEval(Match match)
    {
        string[] textArray =
        {
        "[[",
        match.Groups["site"].ToString().ToLower(),
        ":",
        match.Groups["text"].ToString(),
        "]]"
    };

        return string.Concat(textArray);
    }

    /// <summary>
    /// Remove duplicates, and return List as string, one item per line
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    private static string ListToString(ICollection<string> items)
    {
        if (!items.Any())
            return "";

        List<string> uniqueItems = new List<string>();

        // remove duplicates: duplicate if an existing list item starts the with string
        // also duplicate when one category is same as another with a sortkey
        // e.g. [[Category:One]] is duplicate of [[Category:One|A]]
        // Or sortkeys vary only by first letter case
        foreach (string s in items)
        {
            bool addme = true;

            string s2 = s;
            bool isACategory = WikiRegexes.Category.IsMatch(s2);
            // compare based on first letter upper sortkey for categories
            if (s2.Contains("|") && isACategory)
                s2 = Regex.Replace(s2, @"(\|\s*)(.+)(\s*\]\]$)", m => m.Groups[1].Value + Tools.TurnFirstToUpper(m.Groups[2].Value) + m.Groups[3].Value);

            foreach (string u in uniqueItems)
            {
                if (u.StartsWith(s2) || u.StartsWith(s2.TrimEnd(']') + @"|") || u.Equals(s) || u.Equals(s2))
                {
                    addme = false;
                    break;
                }
                if (s2.StartsWith(u)) // e.g. [[Category:A]] already added but [[Category:A]] <!-- comment--> next in list
                {
                    uniqueItems.Remove(u);
                    break;
                }
                // for Category: e.g. [[Category:A|Foo]] already added but [[Category:A|Foo bar]] next in list
                if (isACategory && u.Contains("|") && s2.TrimEnd(']').StartsWith(u.TrimEnd(']')))
                {
                    uniqueItems.Remove(u);
                    break;
                }
                if (isACategory && s2.Contains("|") && u.TrimEnd(']').StartsWith(s2.TrimEnd(']')))
                {
                    addme = false;
                    break;
                }
                // compare on first letter case insensitive for templates
                if (WikiRegexes.NestedTemplates.IsMatch(s2) && WikiRegexes.NestedTemplates.IsMatch(u))
                {
                    string s2upper = s2.Substring(1, 3).ToUpper() + s2.Substring(3);
                    string uupper = u.Substring(1, 3).ToUpper() + u.Substring(3);
                    if (s2upper.Equals(uupper))
                    {
                        addme = false;
                        break;
                    }
                }
            }

            if (addme)
                uniqueItems.Add(s);
        }

        StringBuilder list = new StringBuilder();
        foreach (string s in uniqueItems)
        {
            list.Append(s + "\r\n"); // Don't just use AppendLine as this may just give \n under Mono
        }

        return list.ToString();
    }

    /// <summary>
    /// Adds a category sort key to category links that do not already contain one.
    /// </summary>
    /// <param name="list">
    /// The category links to process.
    /// </param>
    /// <param name="name">
    /// The value used to generate the category sort key.
    /// </param>
    /// <returns>
    /// A new list containing the processed category links.
    /// </returns>
    private static List<string> CatKeyer(IEnumerable<string> list, string name)
    {
        name = Tools.MakeHumanCatKey(name, ""); // Generate the category sort key.

        // Add the generated key to categories that do not already contain one.
        List<string> newCats = new List<string>();
        foreach (string s in list)
        {
            string z = s;
            if (!z.Contains("|"))
                z = z.Replace("]]", "|" + name + "]]");

            newCats.Add(z);
        }

        return newCats;
    }
}