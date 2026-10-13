using System;
using System.Linq;
using Twain.Core.AWBSettings;
using Twain.Core.ReplaceSpecial;

namespace Twain.UI.Options;

/// <summary>
/// Maps shared AWB preferences to and from the editing options view model.
/// </summary>
public static class OptionsPreferencesMapper
{
    public static void Load(UserPrefs preferences, OptionsViewModel options)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(options);

        // Automatic changes
        options.AutoTag = preferences.Editprefs.Tagger;
        options.ApplyGeneralFixes = preferences.Editprefs.GeneralFixes;
        options.UnicodifyWholePage = preferences.Editprefs.Unicodify;
        options.EnableTypoCorrection = preferences.Editprefs.RegexTypoFix;

        // Find and replace
        options.FindAndReplaceEnabled = preferences.FindAndReplace.Enabled;

        // Find and replace configuration
        options.FindReplace.IgnoreLinks =
            preferences.FindAndReplace.IgnoreSomeText;

        options.FindReplace.IgnoreMore =
            preferences.FindAndReplace.IgnoreMoreText;

        options.FindReplace.AppendToSummary =
            preferences.FindAndReplace.AppendSummary;

        // Restore the saved replacement rules.
        options.FindReplace.LoadReplacements(
            preferences.FindAndReplace.Replacements);

        // Disambiguation
        options.DisambiguationEnabled = preferences.Disambiguation.Enabled;
        options.DisambiguationLink = preferences.Disambiguation.Link;
        options.DisambiguationVariants =
            string.Join(
                Environment.NewLine,
                preferences.Disambiguation.Variants);

        options.DisambiguationContextCharacters =
            preferences.Disambiguation.ContextChars;

        // Skip options
        options.SkipNoChanges = preferences.SkipOptions.SkipWhenNoChanges;
        options.SkipWhitespaceChanges =
            preferences.SkipOptions.SkipWhenOnlyWhitespaceChanged;
        options.SkipCasingChanges =
            preferences.SkipOptions.SkipOnlyCasingChanged;
        options.SkipGeneralFixChanges =
            preferences.SkipOptions.SkipOnlyGeneralFixChanges;
        options.SkipMinorGeneralFixChanges =
            preferences.SkipOptions.SkipOnlyMinorGeneralFixChanges;
        options.SkipCosmeticChanges =
            preferences.SkipOptions.SkipOnlyCosmetic;
        options.SkipRedirects =
            preferences.SkipOptions.SkipIfRedirect;
        options.SkipIfNoAlerts =
            preferences.SkipOptions.SkipIfNoAlerts;
        options.SkipPagesWithNoLinks =
            preferences.SkipOptions.SkipNoLinksOnPage;

        // Find and replace skip conditions
        options.SkipIfNoReplacement =
            preferences.SkipOptions.SkipNoFindAndReplace;
        options.SkipIfOnlyMinorReplacement =
            preferences.SkipOptions.SkipMinorFindAndReplace;
        options.SkipIfNoRegexTypo =
            preferences.SkipOptions.SkipNoRegexTypoFix;
        options.SkipIfNoDisambiguation =
            preferences.SkipOptions.SkipNoDisambiguation;

        // options.Category operations
        options.CategoryOperation = preferences.Editprefs.Recategorisation;
        options.Category = preferences.Editprefs.NewCategory;
        options.CategoryReplacement = preferences.Editprefs.NewCategory2;
        options.SkipIfNoCategoryChange = preferences.Editprefs.SkipIfNoCatChange;
        options.RemoveCategorySortKey = preferences.Editprefs.RemoveSortKey;

        // File operations
        options.FileOperation = preferences.Editprefs.ReImage;
        options.FileReplace = preferences.Editprefs.ImageFind;
        options.FileWith = preferences.Editprefs.Replace;
        options.SkipIfNoFileChange = preferences.Editprefs.SkipIfNoImgChange;

        // Append / prepend
        options.AppendPrependEnabled = preferences.Editprefs.AppendText;
        options.AppendText = preferences.Editprefs.Append;
        options.AppendPrependText = preferences.Editprefs.Text;
        options.AppendNewlineCount = preferences.Editprefs.Newlines;
        options.SortMetadataAfterAppend = preferences.Editprefs.AppendTextMetaDataSort;

        // Page existence
        options.PageExistenceSkip =
            preferences.SkipOptions.SkipNonexistent ? 1 :
            preferences.SkipOptions.Skipexistent ? 0 : 2;

        options.SkipIfInUse =
            preferences.SkipOptions.SkipInuse;

        options.SkipIfSpamFilterBlocked =
            preferences.SkipOptions.SkipSpamFilterBlocked;

        // Skip if page contains text
        options.SkipIfContainsEnabled =
            preferences.SkipOptions.SkipDoes;

        options.SkipIfContainsText =
            preferences.SkipOptions.SkipDoesText;

        options.SkipIfContainsRegex =
            preferences.SkipOptions.SkipDoesRegex;

        options.SkipIfContainsCaseSensitive =
            preferences.SkipOptions.SkipDoesCaseSensitive;

        options.SkipIfContainsAfterProcessing =
            preferences.SkipOptions.SkipDoesAfterProcessing;

        // Skip if page does not contain text
        options.SkipIfNotContainsEnabled =
            preferences.SkipOptions.SkipDoesNot;

        options.SkipIfNotContainsText =
            preferences.SkipOptions.SkipDoesNotText;

        options.SkipIfNotContainsRegex =
            preferences.SkipOptions.SkipDoesNotRegex;

        options.SkipIfNotContainsCaseSensitive =
            preferences.SkipOptions.SkipDoesNotCaseSensitive;

        options.SkipIfNotContainsAfterProcessing =
            preferences.SkipOptions.SkipDoesNotAfterProcessing;

        // Individual automatic-change skip conditions
        options.SelectedAutoChangeSkipOptions =
            preferences.SkipOptions.GeneralSkipList.ToList();

        // General settings
        options.EditSummary = preferences.General.SelectedSummary;
        options.EditSummaryLocked = preferences.General.LockSummary;
        options.MinorEdit = preferences.General.Minor;

        // Editor search settings
        options.FindText = preferences.General.FindText;
        options.FindRegex = preferences.General.FindRegex;
        options.FindCaseSensitive = preferences.General.FindCaseSensitive;

        // Template substitution settings
        options.TemplateSubstitutionTemplates =
            preferences.FindAndReplace.SubstTemplates.ToArray();

        options.TemplateSubstitutionExpandRecursively =
            preferences.FindAndReplace.ExpandRecursively;

        options.TemplateSubstitutionIgnoreUnformatted =
            preferences.FindAndReplace.IgnoreUnformatted;

        options.TemplateSubstitutionIncludeComments =
            preferences.FindAndReplace.IncludeComments;

        options.AdvancedFindReplaceRules =
            preferences.FindAndReplace.AdvancedReps
                .Select(CloneAdvancedRule)
                .ToList();
    }


    public static void Save(OptionsViewModel options, UserPrefs preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(options);

        // Automatic changes
        preferences.Editprefs.Tagger = options.AutoTag;
        preferences.Editprefs.GeneralFixes = options.ApplyGeneralFixes;
        preferences.Editprefs.Unicodify = options.UnicodifyWholePage;
        preferences.Editprefs.RegexTypoFix = options.EnableTypoCorrection;

        // Disambiguation
        preferences.Disambiguation.Enabled = options.DisambiguationEnabled;
        preferences.Disambiguation.Link = options.DisambiguationLink;
        preferences.Disambiguation.ContextChars =
            options.DisambiguationContextCharacters;

        preferences.Disambiguation.Variants =
            options.DisambiguationVariants.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries);

        // Skip options
        preferences.SkipOptions.SkipWhenNoChanges = options.SkipNoChanges;
        preferences.SkipOptions.SkipWhenOnlyWhitespaceChanged =
            options.SkipWhitespaceChanges;
        preferences.SkipOptions.SkipOnlyCasingChanged = options.SkipCasingChanges;
        preferences.SkipOptions.SkipOnlyGeneralFixChanges =
            options.SkipGeneralFixChanges;
        preferences.SkipOptions.SkipOnlyMinorGeneralFixChanges =
            options.SkipMinorGeneralFixChanges;
        preferences.SkipOptions.SkipOnlyCosmetic = options.SkipCosmeticChanges;
        preferences.SkipOptions.SkipIfRedirect = options.SkipRedirects;
        preferences.SkipOptions.SkipIfNoAlerts = options.SkipIfNoAlerts;
        preferences.SkipOptions.SkipNoLinksOnPage = options.SkipPagesWithNoLinks;

        // Find and replace skip conditions
        preferences.SkipOptions.SkipNoFindAndReplace =
            options.SkipIfNoReplacement;
        preferences.SkipOptions.SkipMinorFindAndReplace =
            options.SkipIfOnlyMinorReplacement;
        preferences.SkipOptions.SkipNoRegexTypoFix = options.SkipIfNoRegexTypo;
        preferences.SkipOptions.SkipNoDisambiguation =
            options.SkipIfNoDisambiguation;

        // Page existence
        preferences.SkipOptions.SkipNonexistent =
            options.PageExistenceSkip == 1;
        preferences.SkipOptions.Skipexistent =
            options.PageExistenceSkip == 0;
        preferences.SkipOptions.SkipDontCare =
            options.PageExistenceSkip == 2;

        // Additional skip conditions
        preferences.SkipOptions.SkipInuse = options.SkipIfInUse;
        preferences.SkipOptions.SkipSpamFilterBlocked =
            options.SkipIfSpamFilterBlocked;

        // Skip if page contains text
        preferences.SkipOptions.SkipDoes = options.SkipIfContainsEnabled;
        preferences.SkipOptions.SkipDoesText = options.SkipIfContainsText;
        preferences.SkipOptions.SkipDoesRegex = options.SkipIfContainsRegex;
        preferences.SkipOptions.SkipDoesCaseSensitive =
            options.SkipIfContainsCaseSensitive;
        preferences.SkipOptions.SkipDoesAfterProcessing =
            options.SkipIfContainsAfterProcessing;

        // Skip if page does not contain text
        preferences.SkipOptions.SkipDoesNot = options.SkipIfNotContainsEnabled;
        preferences.SkipOptions.SkipDoesNotText = options.SkipIfNotContainsText;
        preferences.SkipOptions.SkipDoesNotRegex = options.SkipIfNotContainsRegex;
        preferences.SkipOptions.SkipDoesNotCaseSensitive =
            options.SkipIfNotContainsCaseSensitive;
        preferences.SkipOptions.SkipDoesNotAfterProcessing =
            options.SkipIfNotContainsAfterProcessing;

        // Individual automatic-change skip conditions
        preferences.SkipOptions.GeneralSkipList =
            options.SelectedAutoChangeSkipOptions;

        // options.Category operations
        preferences.Editprefs.Recategorisation = options.CategoryOperation;
        preferences.Editprefs.NewCategory = options.Category;
        preferences.Editprefs.NewCategory2 = options.CategoryReplacement;
        preferences.Editprefs.SkipIfNoCatChange = options.SkipIfNoCategoryChange;
        preferences.Editprefs.RemoveSortKey = options.RemoveCategorySortKey;

        // File operations
        preferences.Editprefs.ReImage = options.FileOperation;
        preferences.Editprefs.ImageFind = options.FileReplace;
        preferences.Editprefs.Replace = options.FileWith;
        preferences.Editprefs.SkipIfNoImgChange = options.SkipIfNoFileChange;

        // Append / prepend
        preferences.Editprefs.AppendText = options.AppendPrependEnabled;
        preferences.Editprefs.Append = options.AppendText;
        preferences.Editprefs.Text = options.AppendPrependText;
        preferences.Editprefs.Newlines = options.AppendNewlineCount;
        preferences.Editprefs.AppendTextMetaDataSort =
            options.SortMetadataAfterAppend;

        // General settings
        preferences.General.SelectedSummary = options.EditSummary;
        preferences.General.LockSummary = options.EditSummaryLocked;
        preferences.General.Minor = options.MinorEdit;

        // Editor search settings
        preferences.General.FindText = options.FindText;
        preferences.General.FindRegex = options.FindRegex;
        preferences.General.FindCaseSensitive = options.FindCaseSensitive;

        // Standard Find and Replace
        preferences.FindAndReplace.Enabled = options.FindAndReplaceEnabled;
        preferences.FindAndReplace.IgnoreSomeText = options.FindReplace.IgnoreLinks;
        preferences.FindAndReplace.IgnoreMoreText = options.FindReplace.IgnoreMore;
        preferences.FindAndReplace.AppendSummary = options.FindReplace.AppendToSummary;

        preferences.FindAndReplace.Replacements =
            options.FindReplace.GetReplacements();

        // Template substitution
        preferences.FindAndReplace.SubstTemplates =
            options.TemplateSubstitutionTemplates.ToArray();

        preferences.FindAndReplace.ExpandRecursively =
            options.TemplateSubstitutionExpandRecursively;

        preferences.FindAndReplace.IgnoreUnformatted =
            options.TemplateSubstitutionIgnoreUnformatted;

        preferences.FindAndReplace.IncludeComments =
            options.TemplateSubstitutionIncludeComments;

        // Advanced Find and Replace
        preferences.FindAndReplace.AdvancedReps =
            options.AdvancedFindReplaceRules
                .Select(CloneAdvancedRule)
                .ToList();
    }

    /// <summary>
    /// Creates an independent copy of an Advanced Find and Replace
    /// rule, including its nested child rules.
    /// </summary>
    private static IRule CloneAdvancedRule(IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        IRule clone = (IRule)rule.Clone();

        clone.Children = rule.Children is null
            ? null
            : rule.Children
                .Select(CloneAdvancedRule)
                .ToList();

        return clone;
    }
}
