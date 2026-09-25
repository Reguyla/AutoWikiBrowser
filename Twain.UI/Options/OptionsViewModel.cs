using CommunityToolkit.Mvvm.ComponentModel;
using Twain.UI.FindReplace;
using Twain.UI.Templates;

namespace Twain.UI.Options;

/// <summary>
/// Provides presentation state for the editing-options pane.
/// </summary>
/// <remarks>
/// This initial implementation establishes the options-pane presentation
/// contract. Persistent settings and article-processing behavior will later
/// be provided by services in Twain.Core.
/// </remarks>
public sealed partial class OptionsViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets whether automatic tagging is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _autoTag = true;

    /// <summary>
    /// Gets or sets whether general fixes are applied.
    /// </summary>
    [ObservableProperty]
    private bool _applyGeneralFixes = true;

    /// <summary>
    /// Gets or sets whether the entire page is normalized to Unicode.
    /// </summary>
    [ObservableProperty]
    private bool _unicodifyWholePage = true;

    /// <summary>
    /// Gets or sets whether typo correction is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _enableTypoCorrection;

    /// <summary>
    /// Gets or sets whether unchanged articles should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipUnchangedArticles = true;

    /// <summary>
    /// Gets or sets whether find and replace processing is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _findAndReplaceEnabled;

    /// <summary>
    /// Gets or sets whether an article should be skipped when no replacement is made.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNoReplacement;

    /// <summary>
    /// Gets or sets whether an article should be skipped when only minor replacements are made.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfOnlyMinorReplacement;

    /// <summary>
    /// Gets or sets whether articles should be skipped when regex typo fixing
    /// makes no changes.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNoRegexTypo;

    /// <summary>
    /// Gets or sets whether text should be appended or prepended to the article.
    /// </summary>
    [ObservableProperty]
    private bool _appendPrependEnabled;

    /// <summary>
    /// Gets or sets whether configured text should be appended to the article.
    /// </summary>
    [ObservableProperty]
    private bool _appendText = true;

    /// <summary>
    /// Gets or sets the text to append or prepend.
    /// </summary>
    [ObservableProperty]
    private string _appendPrependText = string.Empty;

    /// <summary>
    /// Gets or sets whether metadata should be sorted after appending or prepending.
    /// </summary>
    [ObservableProperty]
    private bool _sortMetadataAfterAppend;

    /// <summary>
    /// Gets or sets the selected file operation.
    /// </summary>
    [ObservableProperty]
    private int _fileOperation;

    /// <summary>
    /// Gets or sets the file targeted by the configured file operation.
    /// </summary>
    [ObservableProperty]
    private string _fileReplace = string.Empty;

    /// <summary>
    /// Gets or sets the replacement file.
    /// </summary>
    [ObservableProperty]
    private string _fileWith = string.Empty;

    /// <summary>
    /// Gets or sets whether the article should be skipped when no file is changed.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNoFileChange;

    /// <summary>
    /// Gets or sets the selected category operation.
    /// </summary>
    [ObservableProperty]
    private int _categoryOperation;

    /// <summary>
    /// Gets or sets the primary category used by the configured operation.
    /// </summary>
    [ObservableProperty]
    private string _category = string.Empty;

    /// <summary>
    /// Gets or sets the replacement or secondary category.
    /// </summary>
    [ObservableProperty]
    private string _categoryReplacement = string.Empty;

    /// <summary>
    /// Gets or sets whether the article should be skipped when no category is changed.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNoCategoryChange;

    /// <summary>
    /// Gets or sets whether the category sort key should be removed.
    /// </summary>
    [ObservableProperty]
    private bool _removeCategorySortKey;

    /// <summary>
    /// Gets or sets the number of newline characters used when appending
    /// or prepending text.
    /// </summary>
    [ObservableProperty]
    private int _appendNewlineCount = 2;

    /// <summary>
    /// Gets or sets whether disambiguation processing is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _disambiguationEnabled;

    /// <summary>
    /// Gets or sets the link to disambiguate.
    /// </summary>
    [ObservableProperty]
    private string _disambiguationLink = string.Empty;

    /// <summary>
    /// Gets or sets the newline-separated disambiguation variants.
    /// </summary>
    [ObservableProperty]
    private string _disambiguationVariants = string.Empty;

    /// <summary>
    /// Gets or sets the number of characters shown around a disambiguation
    /// match for context.
    /// </summary>
    [ObservableProperty]
    private int _disambiguationContextCharacters = 20;

    /// <summary>
    /// Gets or sets whether pages with no disambiguation changes should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNoDisambiguation;

    /// <summary>
    /// Gets or sets the page-existence condition used to skip articles.
    /// 0 = Exists, 1 = Doesn't exist, 2 = Don't care.
    /// </summary>
    [ObservableProperty]
    private int _pageExistenceSkip = 1;

    /// <summary>
    /// Gets or sets whether pages containing an in-use template should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfInUse;

    /// <summary>
    /// Gets or sets whether edits blocked by the spam filter should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfSpamFilterBlocked;

    /// <summary>
    /// Gets or sets whether pages with no automatic changes should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipNoChanges;

    /// <summary>
    /// Gets or sets whether pages with only whitespace changes should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipWhitespaceChanges;

    /// <summary>
    /// Gets or sets whether pages with only casing changes should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipCasingChanges;

    /// <summary>
    /// Gets or sets whether pages with only general-fix changes should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipGeneralFixChanges;

    /// <summary>
    /// Gets or sets whether pages with only minor general-fix changes should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipMinorGeneralFixChanges;

    /// <summary>
    /// Gets or sets whether redirect pages should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipRedirects;

    /// <summary>
    /// Gets or sets whether pages with no alerts should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNoAlerts;

    /// <summary>
    /// Gets or sets whether pages containing no links should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipPagesWithNoLinks;

    /// <summary>
    /// Gets or sets whether pages containing only cosmetic changes should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipCosmeticChanges;

    /// <summary>
    /// Gets or sets whether pages containing the specified text should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfContainsEnabled;

    /// <summary>
    /// Gets or sets the text used by the contains skip condition.
    /// </summary>
    [ObservableProperty]
    private string _skipIfContainsText = string.Empty;

    /// <summary>
    /// Gets or sets whether the contains skip condition is a regular expression.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfContainsRegex;

    /// <summary>
    /// Gets or sets whether the contains skip condition is case-sensitive.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfContainsCaseSensitive;

    /// <summary>
    /// Gets or sets whether pages not containing the specified text should be skipped.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNotContainsEnabled;

    /// <summary>
    /// Gets or sets the text used by the not-contains skip condition.
    /// </summary>
    [ObservableProperty]
    private string _skipIfNotContainsText = string.Empty;

    /// <summary>
    /// Gets or sets whether the not-contains skip condition is a regular expression.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNotContainsRegex;

    /// <summary>
    /// Gets or sets whether the not-contains skip condition is case-sensitive.
    /// </summary>
    [ObservableProperty]
    private bool _skipIfNotContainsCaseSensitive;

    /// <summary>
    /// Gets or sets the default edit summary.
    /// </summary>
    [ObservableProperty]
    private string _editSummary = "clean up";

    /// <summary>
    /// Gets or sets whether the default edit summary is locked.
    /// </summary>
    [ObservableProperty]
    private bool _editSummaryLocked;

    /// <summary>
    /// Gets or sets whether edits should be marked as minor.
    /// </summary>
    [ObservableProperty]
    private bool _minorEdit;

    /// <summary>
    /// Gets or sets the text to find in the current article.
    /// </summary>
    [ObservableProperty]
    private string _findText = string.Empty;

    /// <summary>
    /// Gets or sets whether the find text is interpreted as a regular expression.
    /// </summary>
    [ObservableProperty]
    private bool _findRegex;

    /// <summary>
    /// Gets or sets whether find operations are case-sensitive.
    /// </summary>
    [ObservableProperty]
    private bool _findCaseSensitive;

    /// <summary>
    /// Gets or sets the number of words in the current article.
    /// </summary>
    [ObservableProperty]
    private int _wordCount;

    /// <summary>
    /// Gets or sets the number of links in the current article.
    /// </summary>
    [ObservableProperty]
    private int _linkCount;

    /// <summary>
    /// Gets or sets the number of images in the current article.
    /// </summary>
    [ObservableProperty]
    private int _imageCount;

    /// <summary>
    /// Gets or sets the number of categories in the current article.
    /// </summary>
    [ObservableProperty]
    private int _categoryCount;

    /// <summary>
    /// Gets or sets the number of interwiki links in the current article.
    /// </summary>
    [ObservableProperty]
    private int _interwikiLinkCount;

    /// <summary>
    /// Gets or sets the ISO-format date count for the current article.
    /// </summary>
    [ObservableProperty]
    private int _isoDateCount;

    /// <summary>
    /// Gets or sets the international-format date count for the current article.
    /// </summary>
    [ObservableProperty]
    private int _internationalDateCount;

    /// <summary>
    /// Gets or sets the American-format date count for the current article.
    /// </summary>
    [ObservableProperty]
    private int _americanDateCount;

    /// <summary>
    /// Gets the alerts reported for the current article.
    /// </summary>
    public ObservableCollection<string> Alerts { get; } = [];

    /// <summary>
    /// Gets the editable find and replace configuration for the current workspace.
    /// </summary>
    public FindReplaceViewModel FindReplace { get; } = new();

    [ObservableProperty]
    private string[] _templateSubstitutionTemplates = [];

    [ObservableProperty]
    private bool _templateSubstitutionExpandRecursively = true;

    [ObservableProperty]
    private bool _templateSubstitutionIgnoreUnformatted;

    [ObservableProperty]
    private bool _templateSubstitutionIncludeComments;

    /// <summary>
    /// Gets whether the file name to replace can be edited for the
    /// selected file operation.
    /// </summary>
    public bool IsFileReplaceEnabled =>
        FileOperation is 1 or 2 or 3;

    /// <summary>
    /// Gets whether the replacement file or comment can be edited for the
    /// selected file operation.
    /// </summary>
    public bool IsFileWithEnabled =>
        FileOperation is 1 or 3;

    /// <summary>
    /// Gets whether the skip-if-no-file-change option is available for the
    /// selected file operation.
    /// </summary>
    public bool IsSkipIfNoFileChangeEnabled =>
        FileOperation is 1 or 2 or 3;

    partial void OnFileOperationChanged(int value)
    {
        OnPropertyChanged(nameof(IsFileReplaceEnabled));
        OnPropertyChanged(nameof(IsFileWithEnabled));
        OnPropertyChanged(nameof(IsSkipIfNoFileChangeEnabled));
        OnPropertyChanged(nameof(FileWithLabel));
    }

    /// <summary>
    /// Gets the label displayed for the secondary file-operation value.
    /// </summary>
    public string FileWithLabel =>
        FileOperation switch
        {
            1 => "With File:",
            3 => "Comment:",
            _ => string.Empty
        };

    /// <summary>
    /// Gets whether category controls are available for the selected operation.
    /// </summary>
    public bool IsCategoryEnabled =>
        CategoryOperation > 0;

    /// <summary>
    /// Gets whether replacement-specific category controls are available.
    /// </summary>
    public bool IsCategoryReplacementEnabled =>
        CategoryOperation == 1;

    /// <summary>
    /// Gets the label displayed for the replacement category.
    /// </summary>
    public string CategoryReplacementLabel =>
        CategoryOperation == 1
            ? "with Category:"
            : string.Empty;

    partial void OnCategoryOperationChanged(int value)
    {
        OnPropertyChanged(nameof(IsCategoryEnabled));
        OnPropertyChanged(nameof(IsCategoryReplacementEnabled));
        OnPropertyChanged(nameof(CategoryReplacementLabel));
    }

    /// <summary>
    /// Gets whether disambiguation links can be loaded from the current page name.
    /// </summary>
    public bool CanLoadDisambiguationLinks =>
        DisambiguationEnabled &&
        !string.IsNullOrWhiteSpace(DisambiguationLink);

    partial void OnDisambiguationEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(CanLoadDisambiguationLinks));
    }

    partial void OnDisambiguationLinkChanged(string value)
    {
        OnPropertyChanged(nameof(CanLoadDisambiguationLinks));
    }

    /// <summary>
    /// Gets or sets the source used to populate an empty disambiguation link.
    /// </summary>
    public Func<string>? DisambiguationSourceProvider { get; set; }

    /// <summary>
    /// Populates the disambiguation link from the current list source when
    /// the link has not already been entered.
    /// </summary>
    public void PopulateDisambiguationLinkFromSource()
    {
        if (DisambiguationLink.Length != 0)
        {
            return;
        }

        DisambiguationLink =
            DisambiguationSourceProvider?.Invoke() ??
            string.Empty;
    }

}