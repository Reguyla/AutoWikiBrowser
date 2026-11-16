
using System;
using System.Collections.Generic;
using System.Linq;

namespace Twain.Core.GeneralFixes;

/// <summary>
/// Categories used to organize general fixes in Preferences.
/// </summary>
public enum GeneralFixCategory
{
    DatesAndFormatting,
    LinksAndTemplates,
    ReferencesAndCitations,
    ArticleStructureAndMetadata
}

/// <summary>
/// Describes a general fix available to the user.
/// </summary>
public sealed record GeneralFixDefinition(
    GeneralFixId Id,
    string Name,
    GeneralFixCategory Category,
    string Description,
    bool EnabledByDefault = true);

/// <summary>
/// Provides the catalog of general fixes supported by Twain.
/// </summary>
public static class GeneralFixCatalog
{
    public static IReadOnlyList<GeneralFixDefinition> Fixes { get; } =
    [
        // Dates and formatting
        new(GeneralFixId.FixNonBreakingSpaces,
            "Fix non-breaking spaces",
            GeneralFixCategory.DatesAndFormatting,
            "Normalizes non-breaking spaces.",
            false),

        new(GeneralFixId.Mdashes,
            "Fix em dashes",
            GeneralFixCategory.DatesAndFormatting,
            "Applies dash formatting corrections."),

        new(GeneralFixId.FixDateOrdinalsAndOf,
            "Fix date ordinals and 'of'",
            GeneralFixCategory.DatesAndFormatting,
            "Corrects date ordinals and related formatting."),

        new(GeneralFixId.FixBrParagraphs,
            "Fix paragraph breaks",
            GeneralFixCategory.DatesAndFormatting,
            "Corrects paragraph breaks using HTML br tags."),

        new(GeneralFixId.FixDatesB,
            "Fix dates (pass 1)",
            GeneralFixCategory.DatesAndFormatting,
            "Applies contextual date formatting corrections."),

        new(GeneralFixId.FixDatesA,
            "Fix dates (pass 2)",
            GeneralFixCategory.DatesAndFormatting,
            "Applies additional date formatting corrections."),

        new(GeneralFixId.FixTemperatures,
            "Fix temperatures",
            GeneralFixCategory.DatesAndFormatting,
            "Corrects temperature formatting."),

        // Links and templates
        new(GeneralFixId.TemplateRedirects,
            "Template redirects",
            GeneralFixCategory.LinksAndTemplates,
            "Replaces configured template redirects."),

        new(GeneralFixId.MultipleIssues,
            "Fix multiple issues templates",
            GeneralFixCategory.LinksAndTemplates,
            "Normalizes multiple issues templates."),

        new(GeneralFixId.FixLinkWhitespace,
            "Fix link whitespace",
            GeneralFixCategory.LinksAndTemplates,
            "Corrects whitespace within wikilinks."),

        new(GeneralFixId.FixSyntax,
            "Fix wiki syntax",
            GeneralFixCategory.LinksAndTemplates,
            "Applies supported wiki syntax corrections."),

        new(GeneralFixId.RenameTemplateParameters,
            "Rename template parameters",
            GeneralFixCategory.LinksAndTemplates,
            "Updates configured template parameter names."),

        new(GeneralFixId.Conversions,
            "Conversions",
            GeneralFixCategory.LinksAndTemplates,
            "Applies supported text and markup conversions."),

        new(GeneralFixId.FixMainArticle,
            "Fix main article templates",
            GeneralFixCategory.LinksAndTemplates,
            "Corrects main article template formatting."),

        new(GeneralFixId.FixEmptyLinksAndTemplates,
            "Fix empty links and templates",
            GeneralFixCategory.LinksAndTemplates,
            "Cleans up empty links and templates."),

        new(GeneralFixId.FixLinks,
            "Fix links",
            GeneralFixCategory.LinksAndTemplates,
            "Applies automatic link corrections."),

        new(GeneralFixId.SimplifyLinks,
            "Simplify links",
            GeneralFixCategory.LinksAndTemplates,
            "Simplifies supported wikilink formatting."),

        // References and citations
        new(GeneralFixId.CiteTemplateDates,
            "Fix citation template dates",
            GeneralFixCategory.ReferencesAndCitations,
            "Normalizes dates in citation templates."),

        new(GeneralFixId.FixCitationTemplates,
            "Fix citation templates",
            GeneralFixCategory.ReferencesAndCitations,
            "Corrects supported citation template formatting."),

        new(GeneralFixId.FixReferenceListTags,
            "Fix reference list tags",
            GeneralFixCategory.ReferencesAndCitations,
            "Corrects reference list markup."),

        new(GeneralFixId.SimplifyReferenceTags,
            "Simplify reference tags",
            GeneralFixCategory.ReferencesAndCitations,
            "Simplifies reference tag formatting."),

        new(GeneralFixId.DuplicateUnnamedReferences,
            "Consolidate duplicate unnamed references",
            GeneralFixCategory.ReferencesAndCitations,
            "Processes repeated unnamed references."),

        new(GeneralFixId.DuplicateNamedReferences,
            "Consolidate duplicate named references",
            GeneralFixCategory.ReferencesAndCitations,
            "Processes repeated named references."),

        new(GeneralFixId.SameRefDifferentName,
            "Fix identical references with different names",
            GeneralFixCategory.ReferencesAndCitations,
            "Processes identical references using different names."),

        new(GeneralFixId.RefsAfterPunctuation,
            "References after punctuation",
            GeneralFixCategory.ReferencesAndCitations,
            "Corrects reference placement relative to punctuation."),

        new(GeneralFixId.ReorderReferences,
            "Reorder references",
            GeneralFixCategory.ReferencesAndCitations,
            "Applies supported reference ordering rules."),

        new(GeneralFixId.FixReferenceTags,
            "Fix reference tags",
            GeneralFixCategory.ReferencesAndCitations,
            "Corrects reference tag syntax."),

        new(GeneralFixId.AddMissingReflist,
            "Add missing reflist",
            GeneralFixCategory.ReferencesAndCitations,
            "Adds a reference list when required."),

        // Article structure and metadata
        new(GeneralFixId.EmboldenTitles,
            "Embolden article titles",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Applies supported title bolding rules."),

        new(GeneralFixId.FixLivingThingsRelatedDates,
            "Fix living things related dates",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Corrects date formatting related to living things."),

        new(GeneralFixId.FixHeadings,
            "Fix headings",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Corrects article heading formatting."),

        new(GeneralFixId.FixPeopleCategories,
            "Fix people categories",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Applies supported people category corrections."),

        new(GeneralFixId.SetDefaultSort,
            "Set DEFAULTSORT",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Applies supported DEFAULTSORT corrections."),

        new(GeneralFixId.FixCategories,
            "Fix categories",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Corrects category formatting."),

        new(GeneralFixId.FixImages,
            "Fix images",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Corrects supported image markup."),

        new(GeneralFixId.BulletExternalLinks,
            "Bullet external links",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Normalizes external link list formatting."),

        new(GeneralFixId.PersonData,
            "PersonData",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Processes legacy PersonData markup."),

        new(GeneralFixId.SortMetaData,
            "Sort metadata",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Organizes article metadata."),

        new(
            GeneralFixId.FixImageSelfLinks,
            "Fix image self-links",
            GeneralFixCategory.ArticleStructureAndMetadata,
            "Corrects image links that point to the image itself."),

        // Redirect-specific operations
        new(GeneralFixId.RedirectTagger,
            "Redirect tagger",
            GeneralFixCategory.LinksAndTemplates,
            "Applies supported redirect tagging corrections."),

        new(GeneralFixId.FixSyntaxRedirects,
            "Fix redirect syntax",
            GeneralFixCategory.LinksAndTemplates,
            "Corrects redirect syntax.")
    ];

    /// <summary>
    /// Returns all fixes belonging to a category.
    /// </summary>
    public static IEnumerable<GeneralFixDefinition> GetByCategory(
        GeneralFixCategory category)
    {
        return Fixes.Where(fix => fix.Category == category);
    }

    /// <summary>
    /// Returns the definition of a fix, if available.
    /// </summary>
    public static GeneralFixDefinition? GetDefinition(
        GeneralFixId id)
    {
        return Fixes.FirstOrDefault(fix => fix.Id == id);
    }

    /// <summary>
    /// Determines whether a fix is enabled by default.
    /// </summary>
    public static bool IsEnabledByDefault(GeneralFixId id)
    {
        return GetDefinition(id)?.EnabledByDefault ?? false;
    }
}
