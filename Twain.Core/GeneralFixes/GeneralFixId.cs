
namespace Twain.Core.GeneralFixes;

/// <summary>
/// Identifies individual general fixes that can be
/// enabled or disabled during article processing.
/// </summary>
public enum GeneralFixId
{
    // Dates and formatting
    FixNonBreakingSpaces = 1,
    Mdashes = 2,
    FixDateOrdinalsAndOf = 3,
    FixBrParagraphs = 4,
    FixDatesB = 5,
    FixDatesA = 6,
    FixTemperatures = 7,

    // Links and templates
    TemplateRedirects = 20,
    MultipleIssues = 21,
    FixLinkWhitespace = 22,
    FixSyntax = 23,
    RenameTemplateParameters = 24,
    Conversions = 25,
    FixMainArticle = 26,
    FixEmptyLinksAndTemplates = 27,
    FixLinks = 28,
    SimplifyLinks = 29,

    // References and citations
    CiteTemplateDates = 40,
    FixCitationTemplates = 41,
    FixReferenceListTags = 42,
    SimplifyReferenceTags = 43,
    DuplicateUnnamedReferences = 44,
    DuplicateNamedReferences = 45,
    SameRefDifferentName = 46,
    RefsAfterPunctuation = 47,
    ReorderReferences = 48,
    FixReferenceTags = 49,
    AddMissingReflist = 50,

    // Article structure and metadata
    EmboldenTitles = 60,
    FixLivingThingsRelatedDates = 61,
    FixHeadings = 62,
    FixPeopleCategories = 63,
    SetDefaultSort = 64,
    FixCategories = 65,
    FixImages = 66,
    BulletExternalLinks = 67,
    PersonData = 68,
    SortMetaData = 69,
    FixImageSelfLinks = 70,

    // Redirect-specific fixes
    RedirectTagger = 80,
    FixSyntaxRedirects = 81
}
