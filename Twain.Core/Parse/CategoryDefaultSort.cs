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
    /// Adds the category to the article.
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="newCategory">The new category.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="noChange"></param>
    /// <returns>The article text.</returns>
    public string AddCategory(string newCategory, string articleText, string articleTitle, out bool noChange)
    {
        string newText = AddCategory(newCategory, articleText, articleTitle);

        noChange = newText.Equals(articleText);

        return newText;
    }

    // Covered by: RecategorizerTests.Addition()
    /// <summary>
    /// Adds the category to the article.
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="newCategory">The new category.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <returns>The article text.</returns>
    public string AddCategory(string newCategory, string articleText, string articleTitle)
    {
        string oldText = articleText;

        articleText = FixCategories(articleText);

        if (Regex.IsMatch(articleText, @"\[\["
                          + Variables.NamespacesCaseInsensitive[Namespace.Category]
                          + Regex.Escape(newCategory) + @"[\|\]]"))
        {
            return oldText;
        }

        string cat = Tools.Newline("[[" + Variables.Namespaces[Namespace.Category] + newCategory + "]]");
        cat = Tools.ApplyKeyWords(articleTitle, cat);

        if (Namespace.Determine(articleTitle) == Namespace.Template)
            articleText += "<noinclude>" + cat + Tools.Newline("</noinclude>");
        else
            articleText += cat;

        return SortMetaData(articleText, articleTitle, false); // Sort metadata ordering so general fixes do not need to be enabled
    }

    // Covered by: RecategorizerTests.Replacement()
    /// <summary>
    /// Re-categorizes the article.
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="oldCategory">The old category to replace.</param>
    /// <param name="newCategory">The new category.</param>
    /// <param name="noChange">Value that indicated whether no change was made.</param>
    /// <returns>The re-categorized article text.</returns>
    public static string ReCategoriser(string oldCategory, string newCategory, string articleText, out bool noChange)
    {
        return ReCategoriser(oldCategory, newCategory, articleText, out noChange, false);
    }

    // Covered by: RecategorizerTests.Replacement()

    /// <summary>
    /// Re-categorizes an article by replacing or removing the specified category.
    /// </summary>
    /// <param name="oldCategory">
    /// The category currently assigned to the article.
    /// </param>
    /// <param name="newCategory">
    /// The replacement category.
    /// </param>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="noChange">
    /// When this method returns, contains <see langword="true"/> if the article
    /// text was unchanged; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="removeSortKey">
    /// <see langword="true"/> to remove any existing category sort key when the
    /// category is replaced; otherwise, <see langword="false"/> to preserve it.
    /// </param>
    /// <returns>
    /// The article text after category replacement has been applied.
    /// </returns>
    public static string ReCategoriser(
        string oldCategory,
        string newCategory,
        string articleText,
        out bool noChange,
        bool removeSortKey)
    {
        string categoryNamespacePattern =
            Variables.NamespacesCaseInsensitive[Namespace.Category];

        string normalizedOldCategory =
            Regex.Replace(
                oldCategory,
                "^" + categoryNamespacePattern,
                "",
                RegexOptions.IgnoreCase);

        string normalizedNewCategory =
            Regex.Replace(
                newCategory,
                "^" + categoryNamespacePattern,
                "",
                RegexOptions.IgnoreCase);

        articleText =
            FixCategories(articleText);

        string originalText =
            articleText;

        string newCategoryPattern =
            @"\[\[" +
            categoryNamespacePattern +
            Tools.FirstLetterCaseInsensitive(
                Regex.Escape(normalizedNewCategory)) +
            @"\s*(\||\]\])";

        if (Regex.IsMatch(
                articleText,
                newCategoryPattern))
        {
            articleText =
                RemoveCategory(
                    normalizedOldCategory,
                    articleText,
                    out _);
        }
        else
        {
            string oldCategoryPattern =
                Variables.Namespaces[Namespace.Category] +
                Tools.FirstLetterCaseInsensitive(
                    Regex.Escape(normalizedOldCategory)) +
                @"\s*(\|[^\|\[\]]+\]\]|\]\])";

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Replacing_categoring_and_keeping_pipes
            string replacementCategory =
                removeSortKey
                    ? Variables.Namespaces[Namespace.Category] +
                      normalizedNewCategory +
                      @"]]"
                    : Variables.Namespaces[Namespace.Category] +
                      normalizedNewCategory +
                      "$1";

            articleText =
                Regex.Replace(
                    articleText,
                    oldCategoryPattern,
                    replacementCategory);
        }

        noChange =
            originalText.Equals(articleText);

        return articleText;
    }

    // Covered by: RecategorizerTests.Removal()
    /// <summary>
    /// Removes a category from an article.
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="strOldCat">The old category to remove.</param>
    /// <param name="noChange">Value that indicated whether no change was made.</param>
    /// <returns>The article text without the old category.</returns>
    public static string RemoveCategory(string strOldCat, string articleText, out bool noChange)
    {
        articleText = FixCategories(articleText);
        string testText = articleText;

        articleText = RemoveCategory(strOldCat, articleText);

        noChange = (testText.Equals(articleText));

        return articleText;
    }

    /// <summary>
    /// Removes a category from an article.
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="strOldCat">The old category to remove.</param>
    /// <returns>The article text without the old category.</returns>
    public static string RemoveCategory(string strOldCat, string articleText)
    {
        strOldCat = Tools.FirstLetterCaseInsensitive(Regex.Escape(strOldCat));

        if (!articleText.Contains("<includeonly>"))
            articleText = Regex.Replace(articleText, "\\[\\["
                                        + Variables.NamespacesCaseInsensitive[Namespace.Category] + " ?"
                                        + strOldCat + "( ?\\]\\]| ?\\|[^\\|]*?\\]\\])\r\n", "");

        articleText = Regex.Replace(articleText, "\\[\\["
                                    + Variables.NamespacesCaseInsensitive[Namespace.Category] + " ?"
                                    + strOldCat + "( ?\\]\\]| ?\\|[^\\|]*?\\]\\])", "");

        return articleText;
    }

    /// <summary>
    /// Returns whether the input string matches the name of a category in use in the input article text string, based on a case insensitive match
    /// </summary>
    /// <param name="articleText">the article text</param>
    /// <param name="categoryName">name of the category</param>
    /// <returns></returns>
    public static bool CategoryMatch(string articleText, string categoryName)
    {
        // for performance only search article from first category
        Match cq = WikiRegexes.CategoryQuick.Match(articleText);

        if (cq.Success)
        {
            Regex anyCategory = new Regex(@"\[\[\s*" + Variables.NamespacesCaseInsensitive[Namespace.Category] + @"\s*" + Regex.Escape(categoryName) + @"\s*(?:|\|([^\|\]]*))\s*\]\]", RegexOptions.IgnoreCase);

            return anyCategory.IsMatch(articleText.Substring(cq.Index));
        }

        return false;
    }

    /// <summary>
    /// Returns a concatenated string of all categories in the article
    /// </summary>
    /// <param name="articleText"></param>
    /// <returns></returns>
    private static string GetCats(string articleText)
    {
        return string.Join("", Tools.DeduplicateList(GetAllWikiLinks(articleText)).Where(l => l.Contains(":") && WikiRegexes.Category.IsMatch(l)).ToArray());
    }

    /// <summary>
    /// Returns whether the article is missing a defaultsort (i.e. criteria match so that defaultsort would be added)
    /// </summary>
    /// <param name="articletext"></param>
    /// <param name="articletitle"></param>
    /// <returns></returns>
    public static bool MissingDefaultSort(string articletext, string articletitle)
    {
        bool Skip, DSbefore = WikiRegexes.Defaultsort.IsMatch(articletext);
        if (!DSbefore)
        {
            articletext = ChangeToDefaultSort(articletext, articletitle, out Skip);
            return (!Skip && WikiRegexes.Defaultsort.IsMatch(articletext));
        }

        return false;
    }

    /// <summary>
    /// Changes an article to use defaultsort when all categories use the same sort field / cleans diacritics from defaultsort/categories
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="noChange">If there is no change (True if no Change)</param>
    /// <returns>The article text possibly using defaultsort.</returns>
    public static string ChangeToDefaultSort(string articleText, string articleTitle, out bool noChange)
    {
        return ChangeToDefaultSort(articleText, articleTitle, out noChange, false);
    }

    /// <summary>
    /// Returns the sortkey used by all categories, if
    /// * all categories use the same sortkey
    /// * no {{DEFAULTSORT}} in article
    /// Otherwise returns null
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns></returns>
    public static string GetCategorySort(string articleText)
    {
        if (WikiRegexes.Defaultsort.Matches(articleText).Count == 1)
            return string.Empty;

        int matches;
        const string dummy = @"@@@@";

        string sort = GetCategorySort(articleText, dummy, out matches);

        return sort == dummy ? "" : sort;
    }

    /// <summary>
    /// Returns the sortkey used by all categories, if all categories use the same sortkey
    /// Where no sortkey is used for all categories, returns the article title
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="matches">Number of categories with the same or no sortkey</param>
    /// <returns></returns>
    public static string GetCategorySort(string articleText, string articleTitle, out int matches)
    {
        string sort = string.Empty;
        bool allsame = true;
        matches = 0;

        articleText = articleText.Replace(@"{{PAGENAME}}", articleTitle);
        articleText = articleText.Replace(@"{{subst:PAGENAME}}", articleTitle);

        foreach (Match m in WikiRegexes.Category.Matches(articleText))
        {
            string explicitKey = m.Groups[2].Value;
            if (explicitKey.Length == 0)
                explicitKey = articleTitle;

            if (string.IsNullOrEmpty(sort))
                sort = explicitKey;

            if (sort != explicitKey && !String.IsNullOrEmpty(explicitKey))
            {
                allsame = false;
                break;
            }
            matches++;
        }
        if (allsame && matches > 0)
            return sort;
        return string.Empty;
    }

// Covered by: UtilityFunctionTests.ChangeToDefaultSort()

/// <summary>
/// Normalizes, inserts, updates, or removes <c>{{DEFAULTSORT}}</c> markup based
/// on the article's category sort keys and project-specific rules.
/// </summary>
/// <param name="articleText">
/// The wiki text of the article.
/// </param>
/// <param name="articleTitle">
/// The title of the article.
/// </param>
/// <param name="noChange">
/// When this method returns, contains <see langword="true"/> if the article text
/// was unchanged; otherwise, <see langword="false"/>.
/// </param>
/// <param name="restrictDefaultsortChanges">
/// <see langword="true"/> to prevent insertion or modification of
/// <c>{{DEFAULTSORT}}</c> where AWB-generated values may be inappropriate,
/// particularly for articles about people; otherwise, <see langword="false"/>.
/// </param>
/// <returns>
/// The article text after DEFAULTSORT and category sort-key processing.
/// </returns>
/// <remarks>
/// This routine normalizes duplicate and existing DEFAULTSORT declarations,
/// cleans category formatting, derives DEFAULTSORT values from category sort
/// keys when appropriate, removes redundant explicit category sort keys, and
/// abandons changes on pages containing include/noinclude programming elements.
/// </remarks>
public static string ChangeToDefaultSort(
    string articleText,
    string articleTitle,
    out bool noChange,
    bool restrictDefaultsortChanges)
    {
        string originalArticleText =
            articleText;

        noChange = true;

        MatchCollection defaultSortMatches =
            WikiRegexes.Defaultsort.Matches(articleText);

        if (!TryNormalizeDuplicateDefaultSorts(
                ref articleText,
                ref defaultSortMatches))
        {
            return articleText;
        }

        NormalizeExistingDefaultSort(
            ref articleText,
            ref defaultSortMatches);

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_9#AWB_didn.27t_fix_special_characters_in_a_pipe
        articleText =
            FixCategories(articleText);

        if (!restrictDefaultsortChanges)
        {
            articleTitle =
                Tools.RemoveNamespaceString(articleTitle);

            if (defaultSortMatches.Count == 0)
            {
                articleText =
                    InsertDefaultSort(
                        articleText,
                        articleTitle);
            }
            else if (defaultSortMatches.Count == 1)
            {
                articleText =
                    UpdateExistingDefaultSort(
                        articleText,
                        articleTitle,
                        ref defaultSortMatches);
            }
        }

        noChange =
            originalArticleText.Equals(articleText);

        // Performance: run relatively slow
        // NoIncludeIncludeOnlyProgrammingElement check only if needed.
        if (!noChange &&
            NoIncludeIncludeOnlyProgrammingElement(
                originalArticleText))
        {
            noChange = true;
            return originalArticleText;
        }

        return articleText;
    }

    /// <summary>
    /// Normalizes duplicate DEFAULTSORT declarations when they all contain the
    /// same value.
    /// </summary>
    /// <param name="articleText">
    /// The article text being processed.
    /// </param>
    /// <param name="defaultSortMatches">
    /// The DEFAULTSORT matches found in <paramref name="articleText"/>. The
    /// collection may be refreshed when normalization changes the article text.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when conflicting DEFAULTSORT declarations are found.
    /// </returns>
    /// <remarks>
    /// Multiple matching DEFAULTSORT declarations are reduced to one. Conflicting
    /// declarations are left unchanged because this routine cannot safely determine
    /// which value should be retained.
    /// </remarks>
    private static bool TryNormalizeDuplicateDefaultSorts(
        ref string articleText,
        ref MatchCollection defaultSortMatches)
    {
        if (defaultSortMatches.Count <= 1 &&
            (defaultSortMatches.Count == 0 ||
             defaultSortMatches[0].Value
                 .ToUpper()
                 .Contains("DEFAULTSORT")))
        {
            return true;
        }

        bool allDefaultSortsMatch =
            false;

        string previousDefaultSort =
            string.Empty;

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Detect_multiple_DEFAULTSORT
        foreach (Match defaultSortMatch in defaultSortMatches)
        {
            if (previousDefaultSort.Length == 0)
            {
                previousDefaultSort =
                    defaultSortMatch.Value;

                allDefaultSortsMatch = true;
            }
            else
            {
                allDefaultSortsMatch =
                    defaultSortMatch.Value ==
                    previousDefaultSort;
            }
        }

        if (!allDefaultSortsMatch)
            return false;

        articleText =
            WikiRegexes.Defaultsort.Replace(
                articleText,
                "",
                defaultSortMatches.Count - 1);

        defaultSortMatches =
            WikiRegexes.Defaultsort.Matches(
                articleText);

        return true;
    }

    /// <summary>
    /// Normalizes an existing English-language DEFAULTSORT declaration when the
    /// normalized value differs from the current markup.
    /// </summary>
    /// <param name="articleText">
    /// The article text being processed.
    /// </param>
    /// <param name="defaultSortMatches">
    /// The DEFAULTSORT matches found in the current article text. The collection is
    /// refreshed when the text is modified.
    /// </param>
    private static void NormalizeExistingDefaultSort(
        ref string articleText,
        ref MatchCollection defaultSortMatches)
    {
        if (defaultSortMatches.Count == 0 ||
            !Variables.LangCode.Equals("en") ||
            DefaultsortME(defaultSortMatches[0])
                .Equals(defaultSortMatches[0].Value))
        {
            return;
        }

        articleText =
            WikiRegexes.Defaultsort.Replace(
                articleText,
                DefaultsortME);

        // Match again after normalization because the article text changed.
        defaultSortMatches =
            WikiRegexes.Defaultsort.Matches(
                articleText);
    }

    /// <summary>
    /// Attempts to create a DEFAULTSORT value for an article that does not already
    /// contain one.
    /// </summary>
    /// <param name="articleText">
    /// The article text being processed.
    /// </param>
    /// <param name="articleTitle">
    /// The article title with any namespace removed.
    /// </param>
    /// <returns>
    /// The article text after any appropriate DEFAULTSORT insertion or category
    /// sort-key cleanup.
    /// </returns>
    private static string InsertDefaultSort(
        string articleText,
        string articleTitle)
    {
        string categorySortKey =
            GetCategorySort(
                articleText,
                articleTitle,
                out int matches);

        // So that this does not get confused by sort keys of "*", " ", etc.
        //
        // MediaWiki bug: DEFAULTSORT does not treat leading spaces the same way as
        // categories do.
        //
        // If all existing categories use a suitable sort key, insert that rather
        // than generating a new one. GetCategorySort returns articleTitle when
        // categories do not have a sort key, so do not accept that result here.
        if (categorySortKey.Length > 4 &&
            matches > 1 &&
            !categorySortKey.StartsWith(" "))
        {
            articleText =
                WikiRegexes.Category.Replace(
                    articleText,
                    "[[" +
                    Variables.Namespaces[Namespace.Category] +
                    "$1]]");

            // Set DEFAULTSORT to the existing unique category sort value.
            // Do not add a DEFAULTSORT when the category sort is effectively the
            // same as the article title.
            if ((categorySortKey != articleTitle &&
                 Tools.FixupDefaultSort(categorySortKey)
                     .ToLower() !=
                 articleTitle.ToLower()) ||
                (!Variables.UnicodeCategoryCollation &&
                 Tools.RemoveDiacritics(categorySortKey) !=
                 categorySortKey &&
                 !IsArticleAboutAPerson(
                     articleText,
                     articleTitle,
                     false)))
            {
                articleText +=
                    Tools.Newline("{{DEFAULTSORT:") +
                    Tools.FixupDefaultSort(
                        categorySortKey) +
                    "}}";
            }
        }

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Add_defaultsort_to_pages_with_special_letters_and_no_defaultsort
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Human_DEFAULTSORT
        return DefaultsortTitlesWithDiacritics(
            articleText,
            articleTitle,
            matches,
            IsArticleAboutAPerson(
                articleText,
                articleTitle,
                true));
    }

    /// <summary>
    /// Normalizes an existing DEFAULTSORT declaration and removes redundant
    /// category sort keys.
    /// </summary>
    /// <param name="articleText">
    /// The article text being processed.
    /// </param>
    /// <param name="articleTitle">
    /// The article title with any namespace removed.
    /// </param>
    /// <param name="defaultSortMatches">
    /// The DEFAULTSORT matches found in the current article text. The collection is
    /// refreshed after any DEFAULTSORT replacement.
    /// </param>
    /// <returns>
    /// The article text after DEFAULTSORT normalization and redundant category
    /// sort-key removal.
    /// </returns>
    private static string UpdateExistingDefaultSort(
        string articleText,
        string articleTitle,
        ref MatchCollection defaultSortMatches)
    {
        string normalizedDefaultSort =
            Tools.FixupDefaultSort(
                    defaultSortMatches[0]
                        .Groups[1]
                        .Value
                        .TrimStart('|'),
                    HumanDefaultSortCleanupRequired(
                        defaultSortMatches[0]) &&
                    IsArticleAboutAPerson(
                        articleText,
                        articleTitle,
                        true))
                .Trim();

        // Do not change DEFAULTSORT solely because of casing.
        if (!normalizedDefaultSort
                .ToLower()
                .Equals(
                    defaultSortMatches[0]
                        .Groups[1]
                        .Value
                        .ToLower()) &&
            normalizedDefaultSort.Length > 0)
        {
            articleText =
                articleText.Replace(
                    defaultSortMatches[0].Value,
                    "{{DEFAULTSORT:" +
                    normalizedDefaultSort +
                    "}}");
        }

        // Get the key value again in case the replacement above changed it.
        defaultSortMatches =
            WikiRegexes.Defaultsort.Matches(
                articleText);

        string defaultSortKey =
            defaultSortMatches[0]
                .Groups["key"]
                .Value;

        // Remove explicit category sort keys that are case-insensitively equivalent
        // to DEFAULTSORT.
        return ExplicitCategorySortkeys(
            articleText,
            defaultSortKey);
    }


    /// <summary>
    /// Returns whether human name defaultsort cleanup required: contains apostrophe or unspaced comma
    /// </summary>
    /// <param name="ds"></param>
    /// <returns></returns>
    private static bool HumanDefaultSortCleanupRequired(Match ds)
    {
        return (ds.Groups[1].Value.Contains("'") || Regex.IsMatch(ds.Groups[1].Value, @"\w,\w"));
    }

    /// <summary>
    /// Removes any explicit keys that are case insensitively the same as the default sort OR entirely match the start of the defaultsort (To help tidy up on pages that already have defaultsort)
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="defaultsortKey"></param>
    /// <returns>The article text.</returns>
    private static string ExplicitCategorySortkeys(string articleText, string defaultsortKey)
    {
        foreach (Match m in WikiRegexes.Category.Matches(articleText))
        {
            string explicitKey = m.Groups[2].Value;
            if (explicitKey.Length == 0)
                continue;

            if (string.Compare(explicitKey, defaultsortKey, StringComparison.OrdinalIgnoreCase) == 0
                || defaultsortKey.StartsWith(explicitKey) || Tools.NestedTemplateRegex("PAGENAME").IsMatch(explicitKey))
            {
                articleText = articleText.Replace(m.Value,
                                                  "[[" + Variables.Namespaces[Namespace.Category] + m.Groups[1].Value + "]]");
            }
        }
        return (articleText);
    }

    /// <summary>
    /// If title has diacritics, no defaultsort added yet, adds a defaultsort with cleaned up title as sort key
    /// If article is about a person, generates human name sortkey
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="categories">Number of categories on page</param>
    /// <param name="articleAboutAPerson">Whether the article is about a person</param>
    /// <returns>The article text possibly using defaultsort.</returns>
    private static string DefaultsortTitlesWithDiacritics(string articleText, string articleTitle, int categories, bool articleAboutAPerson)
    {
        // need some categories and no defaultsort, and a sortkey not the same as the article title
        if (categories > 0 && !WikiRegexes.Defaultsort.IsMatch(articleText))
        {
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Human_DEFAULTSORT
            // if article is about a person, attempt to add a surname, forenames sort key rather than the tidied article title
            string sortkey = articleAboutAPerson ? Tools.MakeHumanCatKey(articleTitle, articleText) : Tools.FixupDefaultSort(articleTitle);

            // sortkeys now not case sensitive
            if (!sortkey.ToLower().Equals(articleTitle.ToLower()) || (!Variables.UnicodeCategoryCollation && Tools.RemoveDiacritics(articleTitle) != articleTitle))
            {
                articleText += Tools.Newline("{{DEFAULTSORT:") + sortkey + "}}";

                return (ExplicitCategorySortkeys(articleText, sortkey));
            }
        }

        return articleText;
    }

    /// <summary>
    /// Matches the birth year from the opening bolded biography line of an article
    /// about a living person.
    /// </summary>
    /// <remarks>
    /// This expression looks for a year appearing after a "Born" clause while
    /// excluding biographies that already contain a death indicator.
    /// </remarks>
    private static readonly Regex PersonYearOfBirth =
        new(
            @"(?<='''.{0,100}?)\( *[Bb]orn[^\)\.;]{1,150}?(?<!.*(?:[Dd]ied|&[nm]dash;|—).*)([12]?\d{3}(?: BC)?)\b[^\)]{0,200}");

    /// <summary>
    /// Matches the year of death from the opening bolded biography line of an
    /// article.
    /// </summary>
    /// <remarks>
    /// This expression identifies biographies that explicitly contain a "Died"
    /// clause and extracts the associated year.
    /// </remarks>
    private static readonly Regex PersonYearOfDeath =
        new(
            @"(?<='''.{0,100}?)\([^\(\)]*?[Dd]ied[^\)\.;]+?([12]?\d{3}(?: BC)?)\b");

    /// <summary>
    /// Matches both the birth and death years from the opening bolded biography
    /// line of an article.
    /// </summary>
    /// <remarks>
    /// Supports biography formats such as "1901–1980", "1901-1980", and MediaWiki
    /// HTML dash entities. The first capture group contains the birth year and the
    /// second capture group contains the death year.
    /// </remarks>
    private static readonly Regex PersonYearOfBirthAndDeath =
        new(
            @"^.{0,100}?'''\s*\([^\)\r\n]*?(?<![Dd]ied)\b([12]?\d{3})\b[^\)\r\n]*?(-|–|—|&[nm]dash;)[^\)\r\n]*?([12]?\d{3})\b[^\)]{0,200}",
            RegexOptions.Singleline);

    /// <summary>
    /// Adds [[Category:XXXX births]], [[Category:XXXX deaths]] to articles about people where available, for en-wiki only
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="noChange"></param>
    /// <returns></returns>
    [Obsolete]
    [CLSCompliant(false)]
    public string FixPeopleCategories(string articleText, string articleTitle, out bool noChange)
    {
        string newText = FixPeopleCategories(articleText, articleTitle);

        noChange = newText.Equals(articleText);

        return newText;
    }

    /// <summary>
    /// Adds [[Category:XXXX births]], [[Category:XXXX deaths]] to articles about people where available, for en-wiki only
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="parseTalkPage"></param>
    /// <param name="noChange"></param>
    /// <returns></returns>
    public string FixPeopleCategories(string articleText, string articleTitle, bool parseTalkPage, out bool noChange)
    {
        string newText = FixPeopleCategories(articleText, articleTitle, parseTalkPage);

        noChange = newText.Equals(articleText);

        return newText;
    }

    /// <summary>
    /// Matches wiki links whose target title is at least 11 characters long,
    /// optionally including a display-text pipe.
    /// </summary>
    private static readonly Regex LongWikilink =
        new(
            @"\[\[[^\[\]\|]{11,}(?:\|[^\[\]]+)?\]\]");

    /// <summary>
    /// Matches a three- or four-digit year, optionally followed by
    /// <c> BC</c>, while excluding values immediately followed by another digit
    /// or the letter <c>s</c>.
    /// </summary>
    private static readonly Regex YearPossiblyWithBC =
        new(
            @"\d{3,4}(?![\ds])(?: BC)?");

    /// <summary>
    /// Matches any three- or four-digit decimal number.
    /// </summary>
    private static readonly Regex ThreeOrFourDigitNumber =
        new(
            @"[0-9]{3,4}");

    /// <summary>
    /// Splits text around a death, baptism, transition, dash, or similar
    /// biographical separator.
    /// </summary>
    /// <remarks>
    /// The first capture group contains the text preceding the separator, and the
    /// second capture group contains the separator and the remaining text.
    /// </remarks>
    private static readonly Regex DiedOrBaptised =
        new(
            @"(^.*?)((?:&[nm]dash;|—|–|;|[Dd](?:ied|\.)|baptised|transitioned).*)");

    /// <summary>
    /// Matches a simple template while excluding <c>{{circa}}</c>,
    /// <c>{{fl}}</c>, and <c>{{fl.}}</c> templates.
    /// </summary>
    private static readonly Regex NotCircaTemplate =
        new(
            @"{{(?!(?:[Cc]irca|[Ff]l\.?))[^{]*?}}");

    /// <summary>
    /// Matches the phrase <c>as of</c> when it appears as a complete word
    /// sequence.
    /// </summary>
    private static readonly Regex AsOfText =
        new(
            @"\bas of\b");

    /// <summary>
    /// Matches nested floruit templates, including <c>{{fl}}</c>,
    /// <c>{{fl.}}</c>, and <c>{{floruit}}</c>.
    /// </summary>
    private static readonly Regex FloruitTemplate =
        Tools.NestedTemplateRegex(
            new[]
            {
            "fl",
            "fl.",
            "floruit"
            });

    /// <summary>
    /// Matches nested templates that derive a birth date from a person's age at
    /// death.
    /// </summary>
    private static readonly Regex BirthDateBasedOnAgeAtDeath =
        Tools.NestedTemplateRegex(
            new[]
            {
            "Birth date based on age at death",
            "Birth based on age at death"
            });

    /// <summary>
    /// Matches nested short-footnote and explanatory-footnote templates used in
    /// article text.
    /// </summary>
    private static readonly Regex FootnoteTemplates =
        Tools.NestedTemplateRegex(
            new[]
            {
            "Efn",
            "Efn-ua",
            "Efn-lr",
            "Sfn",
            "Shortened footnote",
            "Shortened footnote template",
            "Sfnb",
            "Sfnp",
            "Sfnm",
            "SfnRef"
            });

    /// <summary>
    /// Adds [[Category:XXXX births]], [[Category:XXXX deaths]] to articles about people where available, for en-wiki only
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <returns></returns>
    public static string FixPeopleCategories(string articleText, string articleTitle)
    {
        return FixPeopleCategories(articleText, articleTitle, false);
    }

    /// <summary>
    /// Adds [[Category:XXXX births]], [[Category:XXXX deaths]] to articles about people where available, for en-wiki only
    /// When page is not mainspace, adds [[:Category rather than [[Category
    /// Removes Date of birth missing/Date of birth missing (living people) category if full DOB in {{birth date and age}}
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <param name="articleTitle">Title of the article</param>
    /// <param name="parseTalkPage"></param>
    /// <returns></returns>
    public static string FixPeopleCategories(string articleText, string articleTitle, bool parseTalkPage)
    {
        if (!Variables.LangCode.Equals("en"))
            return articleText;

        // Performance: apply births/deaths category checks to the category text rather
        // than the complete article.
        string categories =
            GetCats(articleText);

        if (ShouldSkipPeopleCategoryProcessing(
                articleText,
                categories))
        {
            return YearOfBirthDeathMissingCategory(
                articleText,
                categories);
        }

        string articleTextBefore = articleText;
        int catCount = WikiRegexes.Category.Matches(articleText).Count;

        string zerothSection =
            PreparePeopleCategoryZerothSection(
                articleText);

        string categoryPrefix =
            GetPeopleCategoryPrefix(
                articleTitle);

        string yearstring;

        string sort =
            GetCategorySort(articleText);

        articleText =
            AddBirthCategory(
                articleText,
                articleTitle,
                zerothSection,
                categoryPrefix,
                sort);

        articleText =
            AddDeathCategory(
                articleText,
                articleTitle,
                zerothSection,
                categoryPrefix,
                sort);

        articleText =
            AddCombinedBirthDeathCategories(
                articleText,
                zerothSection,
                categoryPrefix,
                sort);

        // do this check last as IsArticleAboutAPerson can be relatively slow
        if (!articleText.Equals(articleTextBefore) && !IsArticleAboutAPerson(articleTextBefore, articleTitle, parseTalkPage))
            return YearOfBirthDeathMissingCategory(articleTextBefore, categories);

        articleText =
            UpdateUncategorizedTemplateIfNeeded(
                articleText,
                catCount);

        return YearOfBirthDeathMissingCategory(articleText, GetCats(articleText));
    }

    /// <summary>
    /// Determines whether birth- and death-category inference should be skipped for
    /// the supplied article.
    /// </summary>
    /// <param name="articleText">
    /// The complete wiki text of the article.
    /// </param>
    /// <param name="categories">
    /// The category text extracted from the article.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the article already has sufficient
    /// birth/death categorization or is unlikely to be suitable for automatic
    /// category inference; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool ShouldSkipPeopleCategoryProcessing(
        string articleText,
        string categories)
    {
        bool hasDeathOrLivingCategory =
            WikiRegexes.DeathsOrLivingCategory
                .IsMatch(categories);

        bool hasBirthCategory =
            WikiRegexes.BirthsCategory
                .IsMatch(categories);

        // No additional birth/death inference is needed when both categories are
        // already represented.
        if (hasDeathOrLivingCategory &&
            hasBirthCategory)
        {
            return true;
        }

        // Articles that are unusually long and have neither category, or that have
        // many references but no death/living category, are considered too
        // ambiguous for automatic inference.
        return
            (articleText.Length > 15000 &&
             !hasBirthCategory &&
             !hasDeathOrLivingCategory) ||
            (!hasDeathOrLivingCategory &&
             WikiRegexes.Refs.Matches(articleText).Count > 20);
    }

    /// <summary>
    /// Prepares the article's zeroth section for birth- and death-year detection by
    /// removing content likely to produce false-positive year matches.
    /// </summary>
    /// <param name="articleText">
    /// The complete wiki text of the article.
    /// </param>
    /// <returns>
    /// The cleaned zeroth section used for biographical date detection.
    /// </returns>
    /// <remarks>
    /// References, footnote templates, long wikilinks, and templates containing
    /// dated maintenance parameters are removed or replaced before year matching is
    /// performed.
    /// </remarks>
    private static string PreparePeopleCategoryZerothSection(
        string articleText)
    {
        string zerothSection =
            Tools.GetZerothSection(articleText);

        zerothSection =
            WikiRegexes.Refs.Replace(
                zerothSection,
                " ");

        zerothSection =
            FootnoteTemplates.Replace(
                zerothSection,
                " ");

        while (LongWikilink.IsMatch(zerothSection))
        {
            zerothSection =
                LongWikilink.Replace(
                    zerothSection,
                    " ");
        }

        zerothSection =
            WikiRegexes.NestedTemplates.Replace(
                zerothSection,
                match =>
                    ThreeOrFourDigitNumber.IsMatch(
                        Tools.GetTemplateParameterValue(
                            match.Value,
                            "date"))
                        ? ""
                        : match.Value);

        zerothSection =
            WikiRegexes.TemplateMultiline.Replace(
                zerothSection,
                match =>
                    ThreeOrFourDigitNumber.IsMatch(
                        Tools.GetTemplateParameterValue(
                            match.Value,
                            "date"))
                        ? ""
                        : match.Value);

        return zerothSection;
    }

    /// <summary>
    /// Builds the category-link prefix used when adding people-related categories
    /// to the current page.
    /// </summary>
    /// <param name="articleTitle">
    /// The title of the page being processed.
    /// </param>
    /// <returns>
    /// A newline followed by the appropriate category-link prefix. Mainspace pages
    /// use <c>[[Category:</c>, while non-mainspace pages use
    /// <c>[[:Category:</c> so the category is linked rather than applied.
    /// </returns>
    private static string GetPeopleCategoryPrefix(
        string articleTitle)
    {
        return Tools.Newline(
            "[[" +
            (Namespace.IsMainSpace(articleTitle)
                ? ""
                : ":") +
            "Category:");
    }

    /// <summary>
    /// Adds a birth-year category or uncertain-birth-year category when suitable
    /// birth information can be identified in the article lead or infobox.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="articleTitle">
    /// The title of the article.
    /// </param>
    /// <param name="zerothSection">
    /// The cleaned zeroth section used when identifying biographical dates.
    /// </param>
    /// <param name="categoryPrefix">
    /// The category-link prefix to use when adding the birth category.
    /// </param>
    /// <param name="sort">
    /// The category sort key to append to the new category.
    /// </param>
    /// <returns>
    /// The article text after any applicable birth category has been added.
    /// </returns>
    private static string AddBirthCategory(
        string articleText,
        string articleTitle,
        string zerothSection,
        string categoryPrefix,
        string sort)
    {
        string yearstring = string.Empty;
        string yearFromInfoBox = string.Empty;

        bool alreadyUncertain = false;

        // scrape any infobox for birth year, ignore
        // {{Birth date based on age at death}}
        string fromInfoBox =
            GetInfoBoxFieldValue(
                BirthDateBasedOnAgeAtDeath.Replace(
                    zerothSection,
                    ""),
                WikiRegexes.InfoBoxDOBFields);

        // ignore as of dates
        if (AsOfText.IsMatch(fromInfoBox))
        {
            fromInfoBox =
                fromInfoBox.Substring(
                    0,
                    AsOfText.Match(fromInfoBox).Index);
        }

        if (fromInfoBox.Length > 0 &&
            !UncertainWordings.IsMatch(fromInfoBox) &&
            !FloruitTemplate.IsMatch(fromInfoBox))
        {
            yearFromInfoBox =
                YearPossiblyWithBC.Match(fromInfoBox).Value;
        }

        // convert [[:Category to [[Category for non-mainspace Category checking
        string checkText =
            Namespace.IsMainSpace(articleTitle)
                ? articleText
                : articleText.Replace("[[:", "[[");

        // birth
        if (!WikiRegexes.BirthsCategory.IsMatch(checkText) &&
            (PersonYearOfBirth.Matches(zerothSection).Count == 1 ||
             WikiRegexes.DateBirthAndAge.IsMatch(zerothSection) ||
             WikiRegexes.DeathDateAndAge.IsMatch(zerothSection) ||
             ThreeOrFourDigitNumber.IsMatch(yearFromInfoBox)))
        {
            // look for '{{birth date...' template first
            yearstring =
                WikiRegexes.DateBirthAndAge
                    .Match(articleText)
                    .Groups[1]
                    .Value;

            // look for '{{death date and age' template second
            if (String.IsNullOrEmpty(yearstring))
            {
                yearstring =
                    WikiRegexes.DeathDateAndAge
                        .Match(articleText)
                        .Groups[2]
                        .Value;
            }

            // thirdly use yearFromInfoBox
            if (ThreeOrFourDigitNumber.IsMatch(yearFromInfoBox))
            {
                yearstring = yearFromInfoBox;
            }

            // look for '(born xxxx)'
            if (String.IsNullOrEmpty(yearstring))
            {
                Match m =
                    PersonYearOfBirth.Match(
                        zerothSection);

                // remove part beyond dash or died
                string birthpart =
                    DiedOrBaptised.Replace(
                        m.Value,
                        "$1");

                if (WikiRegexes.CircaTemplate.IsMatch(birthpart) ||
                    FloruitTemplate.IsMatch(birthpart))
                {
                    alreadyUncertain = true;
                }

                birthpart =
                    WikiRegexes.TemplateMultiline.Replace(
                        birthpart,
                        " ");

                // check born info before any untemplated died info
                if (!(m.Index >
                      PersonYearOfDeath.Match(zerothSection).Index) ||
                    !PersonYearOfDeath.IsMatch(zerothSection))
                {
                    // when there's only an approximate birth year, add the
                    // appropriate cat rather than the xxxx birth one
                    if (UncertainWordings.IsMatch(birthpart) ||
                        alreadyUncertain)
                    {
                        if (!CategoryMatch(
                                articleText,
                                YearOfBirthMissingLivingPeople) &&
                            !CategoryMatch(
                                articleText,
                                YearOfBirthUncertain))
                        {
                            articleText +=
                                categoryPrefix +
                                YearOfBirthUncertain +
                                CatEnd(sort);
                        }
                    }
                    else
                    {
                        // after removing dashes, birthpart must still contain year
                        // and not a year range
                        if (!birthpart.Contains(@"?") &&
                            Regex.IsMatch(
                                birthpart,
                                @"\d{3,4}") &&
                            !Regex.IsMatch(
                                m.Value,
                                @"[12]\d\d\d.[12]\d\d\d"))
                        {
                            yearstring =
                                m.Groups[1].Value;
                        }
                    }
                }
            }

            // per [[:Category:Living people]], don't apply birth category if born
            // > 121 years ago
            // validate a YYYY date is not in the future
            if (!string.IsNullOrEmpty(yearstring) &&
                yearstring.Length > 2 &&
                (!YearOnly.IsMatch(yearstring) ||
                 Convert.ToInt32(yearstring) <= DateTime.Now.Year) &&
                !(articleText.Contains(CategoryLivingPeople) &&
                  Convert.ToInt32(yearstring) <
                      (DateTime.Now.Year - 121)))
            {
                articleText +=
                    categoryPrefix +
                    yearstring +
                    " births" +
                    CatEnd(sort);
            }
        }

        return articleText;
    }

    /// <summary>
    /// Adds a death-year category when a suitable death year can be inferred from
    /// the article lead or infobox.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="articleTitle">
    /// The title of the article.
    /// </param>
    /// <param name="zerothSection">
    /// The cleaned zeroth section used for biographical date detection.
    /// </param>
    /// <param name="categoryPrefix">
    /// The category-link prefix to use when adding the death category.
    /// </param>
    /// <param name="sort">
    /// The category sort key to append to the new category.
    /// </param>
    /// <returns>
    /// The article text after any applicable death category has been added.
    /// </returns>
    private static string AddDeathCategory(
        string articleText,
        string articleTitle,
        string zerothSection,
        string categoryPrefix,
        string sort)
    {
        string yearstring;
        string yearFromInfoBox =
            string.Empty;

        // scrape any infobox
        string fromInfoBox =
            GetInfoBoxFieldValue(
                WikiRegexes.DateBirthAndAge.Replace(
                    zerothSection,
                    ""),
                WikiRegexes.InfoBoxDODFields);

        if (fromInfoBox.Length > 0 &&
            !UncertainWordings.IsMatch(fromInfoBox))
        {
            yearFromInfoBox =
                YearPossiblyWithBC
                    .Match(fromInfoBox)
                    .Value;
        }

        string checkText =
            Namespace.IsMainSpace(articleTitle)
                ? articleText
                : articleText.Replace(
                    "[[:",
                    "[[");

        if (!WikiRegexes.DeathsOrLivingCategory.IsMatch(
                RemoveCategory(
                    YearofDeathMissing,
                    checkText)) &&
            (PersonYearOfDeath.IsMatch(zerothSection) ||
             WikiRegexes.DeathDate.IsMatch(zerothSection) ||
             ThreeOrFourDigitNumber.IsMatch(yearFromInfoBox)))
        {
            // look for '{{death date...' template first
            yearstring =
                WikiRegexes.DeathDate
                    .Match(articleText)
                    .Groups[1]
                    .Value;

            // secondly use yearFromInfoBox
            if (ThreeOrFourDigitNumber.IsMatch(yearFromInfoBox))
            {
                yearstring =
                    yearFromInfoBox;
            }

            // look for '(died xxxx)'
            if (string.IsNullOrEmpty(yearstring))
            {
                Match m =
                    PersonYearOfDeath.Match(
                        zerothSection);

                // check died info after any untemplated born info
                if (m.Index >=
                        PersonYearOfBirth.Match(zerothSection).Index ||
                    !PersonYearOfBirth.IsMatch(zerothSection))
                {
                    if (!UncertainWordings.IsMatch(m.Value) &&
                        !m.Value.Contains(@"?"))
                    {
                        yearstring =
                            m.Groups[1].Value;
                    }
                }
            }

            // validate a YYYY date is not in the future
            if (!string.IsNullOrEmpty(yearstring) &&
                yearstring.Length > 2 &&
                (!YearOnly.IsMatch(yearstring) ||
                 Convert.ToInt32(yearstring) <= DateTime.Now.Year))
            {
                articleText +=
                    categoryPrefix +
                    yearstring +
                    " deaths" +
                    CatEnd(sort);
            }
        }

        return articleText;
    }

    /// <summary>
    /// Adds birth- and death-year categories when both years can be inferred from
    /// combined lifespan text in the article lead.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="zerothSection">
    /// The cleaned zeroth section used for biographical date detection.
    /// </param>
    /// <param name="categoryPrefix">
    /// The category-link prefix to use when adding birth or death categories.
    /// </param>
    /// <param name="sort">
    /// The category sort key to append to newly added categories.
    /// </param>
    /// <returns>
    /// The article text after any applicable birth or death categories have been
    /// added.
    /// </returns>
    /// <remarks>
    /// This helper handles combined lifespan formats such as
    /// <c>1901–1980</c>. It validates the inferred lifespan and preserves the
    /// existing uncertainty and biography safeguards before adding categories.
    /// </remarks>
    private static string AddCombinedBirthDeathCategories(
        string articleText,
        string zerothSection,
        string categoryPrefix,
        string sort)
    {
        zerothSection =
            NotCircaTemplate.Replace(
                zerothSection,
                " ");

        // Birth and death combined.
        // If not fully categorized, check it.
        if (PersonYearOfBirthAndDeath.IsMatch(zerothSection) &&
            (!WikiRegexes.BirthsCategory.IsMatch(articleText) ||
             !WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText)))
        {
            Match m =
                PersonYearOfBirthAndDeath.Match(
                    zerothSection);

            string birthyear =
                m.Groups[1].Value;

            int birthyearint =
                int.Parse(birthyear);

            string deathyear =
                m.Groups[3].Value;

            int deathyearint =
                int.Parse(deathyear);

            // Logical validation of dates.
            if (birthyearint <= (deathyearint - 2) &&
                (deathyearint - birthyearint) <= 125)
            {
                string birthpart =
                    zerothSection.Substring(
                        m.Index,
                        m.Groups[2].Index - m.Index);

                string deathpart =
                    zerothSection.Substring(
                        m.Groups[2].Index,
                        (m.Value.Length + m.Index) -
                        m.Groups[2].Index);

                if (!WikiRegexes.BirthsCategory.IsMatch(articleText))
                {
                    if (!UncertainWordings.IsMatch(birthpart) &&
                        !ReignedRuledUnsure.IsMatch(m.Value) &&
                        !Regex.IsMatch(
                            birthpart,
                            @"(?:[Dd](?:ied|\.)|baptised)") &&
                        !FloruitTemplate.IsMatch(birthpart))
                    {
                        articleText +=
                            categoryPrefix +
                            birthyear +
                            @" births" +
                            CatEnd(sort);
                    }
                    else if (UncertainWordings.IsMatch(birthpart) &&
                             !CategoryMatch(
                                 articleText,
                                 YearOfBirthMissingLivingPeople) &&
                             !CategoryMatch(
                                 articleText,
                                 YearOfBirthUncertain))
                    {
                        articleText +=
                            categoryPrefix +
                            YearOfBirthUncertain +
                            CatEnd(sort);
                    }
                }

                if (!UncertainWordings.IsMatch(deathpart) &&
                    !ReignedRuledUnsure.IsMatch(m.Value) &&
                    !Regex.IsMatch(
                        deathpart,
                        @"[Bb](?:orn|\.)") &&
                    !Regex.IsMatch(
                        birthpart,
                        @"[Dd](?:ied|\.)") &&
                    (!WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText) ||
                     CategoryMatch(
                         articleText,
                         YearofDeathMissing)))
                {
                    articleText +=
                        categoryPrefix +
                        deathyear +
                        @" deaths" +
                        CatEnd(sort);
                }
            }
        }

        return articleText;
    }

    /// <summary>
    /// Replaces an uncategorized maintenance template with
    /// <c>{{Improve categories}}</c> when new categories have been added.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="originalCategoryCount">
    /// The number of categories present before people-category processing began.
    /// </param>
    /// <returns>
    /// The article text after any applicable maintenance-template update.
    /// </returns>
    private static string UpdateUncategorizedTemplateIfNeeded(
        string articleText,
        int originalCategoryCount)
    {
        if (WikiRegexes.Category.Matches(articleText).Count >
                originalCategoryCount &&
            WikiRegexes.Uncategorized.IsMatch(articleText) &&
            !WikiRegexes.CatImprove.IsMatch(articleText))
        {
            articleText =
                Tools.RenameTemplate(
                    articleText,
                    Tools.GetTemplateName(
                        WikiRegexes.Uncategorized
                            .Match(articleText)
                            .Value),
                    "Improve categories");
        }

        return articleText;
    }

    private static string CatEnd(string sort)
    {
        return ((sort.Length > 3) ? "|" + sort : "") + "]]";
    }

    private const string YearOfBirthMissingLivingPeople = "Year of birth missing (living people)",
        YearOfBirthMissing = "Year of birth missing",
        YearOfBirthUncertain = "Year of birth uncertain",
        YearofDeathMissing = "Year of death missing";

    private static readonly Regex Cat4YearBirths = new Regex(@"\[\[Category *: *\d{1,4} (BC )?births\s*(?:\||\]\])");
    private static readonly Regex CatYearDeaths = new Regex(@"\[\[Category *: *[0-9]{1,4} (BC )?(deaths|suicides)\s*(?:\||\]\])");

    /// <summary>
    /// Removes year of birth/death missing categories when xxx births/deaths category also present
    /// Removes Date of birth missing/Date of birth missing (living people) category if full DOB in {{birth date and age}}
    /// </summary>
    /// <param name="articleText"></param>
    /// <param name="cats"></param>
    /// <returns>The updated article text</returns>
    private static string YearOfBirthDeathMissingCategory(string articleText, string cats)
    {
        // if there is a 'year of birth missing' and a year of birth, remove the 'missing' category
        if (Cat4YearBirths.IsMatch(cats))
        {
            if (CategoryMatch(cats, YearOfBirthMissingLivingPeople))
                articleText = RemoveCategory(YearOfBirthMissingLivingPeople, articleText);
            else if (CategoryMatch(cats, YearOfBirthMissing))
                articleText = RemoveCategory(YearOfBirthMissing, articleText);
        }

        // if there's a 'year of birth missing' and a 'year of birth uncertain', remove the former
        if (CategoryMatch(cats, YearOfBirthMissing) && CategoryMatch(cats, YearOfBirthUncertain))
            articleText = RemoveCategory(YearOfBirthMissing, articleText);

        // if there's a year of death and a 'year of death missing', remove the latter
        if (CatYearDeaths.IsMatch(cats) && CategoryMatch(cats, YearofDeathMissing))
            articleText = RemoveCategory(YearofDeathMissing, articleText);

        // if full DOB in {{birth date and age}} remove Date of birth missing/Date of birth missing (living people) category
        if (cats.IndexOf(@"Date of birth missing", StringComparison.OrdinalIgnoreCase) > 0 && Regex.IsMatch(WikiRegexes.DateBirthAndAge.Match(articleText).Value, @"(\|\s*[0-9]+\s*){3}"))
        {
            articleText = RemoveCategory(@"Date of birth missing", articleText);
            articleText = RemoveCategory(@"Date of birth missing (living people)", articleText);
        }

        return articleText;
    }

    // Covered by: LinkTests.TestFixCategories()
    /// <summary>
    /// Fix common spacing/capitalization errors in categories; remove diacritics and trailing whitespace from sortkeys (not leading whitespace)
    /// </summary>
    /// <param name="articleText">The wiki text of the article.</param>
    /// <returns>The modified article text.</returns>
    public static string FixCategories(string articleText)
    {
        CategoryStart = @"[[" + (Variables.Namespaces.ContainsKey(Namespace.Category) ? Variables.Namespaces[Namespace.Category] : "Category:");

        // Performance: only need to apply changes to portion of article containing categories
        Match cq = WikiRegexes.CategoryQuick.Match(articleText);

        if (cq.Success)
        {
            // Allow some characters before category start in case of excess opening braces
            int cutoff = Math.Max(0, cq.Index - 2);
            string cats = articleText.Substring(cutoff);
            string catsOriginal = cats;

            // fix extra brackets: three or more at end
            cats = Regex.Replace(cats, @"(" + Regex.Escape(CategoryStart) + @"[^\r\n\[\]{}<>]+\]\])\]+", "$1");
            // three or more at start
            cats = Regex.Replace(cats, @"\[+(?=" + Regex.Escape(CategoryStart) + @"[^\r\n\[\]{}<>]+\]\])", "");

            cats = WikiRegexes.LooseCategory.Replace(cats, LooseCategoryME);

            // Performance: return original text if no changes
            if (cats.Equals(catsOriginal))
                return articleText;

            articleText = articleText.Substring(0, cutoff) + cats;
        }

        return articleText;
    }

    private static string LooseCategoryME(Match m)
    {
        if (!Tools.IsValidTitle(m.Groups[1].Value))
            return m.Value;

        string sortkey = m.Groups[2].Value;

        if (!string.IsNullOrEmpty(sortkey))
        {
            // diacritic removal in sortkeys on en-wiki/simple-wiki only
            if (Variables.LangCode.Equals("en") || Variables.LangCode.Equals("simple"))
                sortkey = Tools.CleanSortKey(sortkey);

            sortkey = WordWhitespaceEndofline.Replace(sortkey, "$1");
        }

        return CategoryStart + Tools.TurnFirstToUpper(CanonicalizeTitleRaw(m.Groups[1].Value, false).Trim().TrimStart(':')) + sortkey + "]]";
    }
}