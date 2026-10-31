using System;
using System.Collections.Generic;

namespace Twain.Core.ArticleAnalysis;

/// <summary>
/// Compares the logical content of article sections while excluding
/// recognized trailing article metadata.
/// </summary>
public static class ArticleSectionComparer
{
    /// <summary>
    /// Returns the names of sections whose logical content differs
    /// between two versions of an article.
    /// </summary>
    /// <remarks>
    /// Sections are matched by heading. If the section structure is
    /// ambiguous, an empty collection is returned.
    /// </remarks>
    public static IReadOnlyList<string> GetModifiedSections(
        string originalText,
        string modifiedText,
        ArticleAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(modifiedText);

        ArticleStructure before =
            ArticleStructureAnalyzer.Analyze(originalText, options);

        ArticleStructure after =
            ArticleStructureAnalyzer.Analyze(modifiedText, options);

        if (before.AllSections.Count() != after.AllSections.Count())
        {
            return Array.Empty<string>();
        }

        var beforeByHeading = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (ArticleSection section in before.AllSections)
        {
            string heading = section.IsLead
                ? "top"
                : section.Title;

            if (!beforeByHeading.TryAdd(
                    heading,
                    GetComparableContent(section)))
            {
                return Array.Empty<string>();
            }
        }

        var modifiedSections = new List<string>();

        var matchedHeadings = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (ArticleSection section in after.AllSections)
        {
            string heading = section.IsLead
                ? "top"
                : section.Title;

            if (!beforeByHeading.TryGetValue(
                    heading,
                    out string? originalContent) ||
                !matchedHeadings.Add(heading))
            {
                return Array.Empty<string>();
            }

            if (!string.Equals(
                    originalContent,
                    GetComparableContent(section),
                    StringComparison.Ordinal))
            {
                modifiedSections.Add(heading);
            }
        }

        return modifiedSections;
    }

    /// <summary>
    /// Returns the name of the single modified section by comparing
    /// sections in their original positions.
    /// </summary>
    /// <remarks>
    /// Preserves legacy positional matching behavior, including reporting
    /// the new heading when a section is renamed.
    ///
    /// Returns an empty string when the section counts differ, multiple
    /// sections change, or no section content changes.
    ///
    /// Recognized trailing metadata is excluded from comparisons.
    /// </remarks>
    public static string GetModifiedSectionByPosition(
        string originalText,
        string modifiedText,
        ArticleAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(modifiedText);

        ArticleStructure before =
            ArticleStructureAnalyzer.Analyze(originalText, options);

        ArticleStructure after =
            ArticleStructureAnalyzer.Analyze(modifiedText, options);

        ArticleSection[] sectionsBefore =
            before.AllSections.ToArray();

        ArticleSection[] sectionsAfter =
            after.AllSections.ToArray();

        if (sectionsBefore.Length != sectionsAfter.Length)
        {
            return string.Empty;
        }

        // Preserve legacy behavior when a lead section is added or removed.
        // The analyzer always creates a lead section, even when it is empty.
        bool hadLeadContent = !string.IsNullOrEmpty(
            before.Lead.Content);

        bool hasLeadContent = !string.IsNullOrEmpty(
            after.Lead.Content);

        if (hadLeadContent != hasLeadContent)
        {
            return string.Empty;
        }

        int changedSectionIndex = -1;

        for (int i = 0; i < sectionsBefore.Length; i++)
        {
            ArticleSection originalSection = sectionsBefore[i];
            ArticleSection modifiedSection = sectionsAfter[i];

            bool headingChanged = !string.Equals(
                originalSection.Heading,
                modifiedSection.Heading,
                StringComparison.Ordinal);

            bool contentChanged = !string.Equals(
                originalSection.LogicalContent,
                modifiedSection.LogicalContent,
                StringComparison.Ordinal);

            if (!headingChanged && !contentChanged)
            {
                continue;
            }

            // More than one section changed.
            if (changedSectionIndex >= 0)
            {
                return string.Empty;
            }

            changedSectionIndex = i;
        }

        if (changedSectionIndex < 0)
        {
            return string.Empty;
        }

        ArticleSection changedSection =
            sectionsAfter[changedSectionIndex];

        return changedSection.IsLead
            ? "top"
            : changedSection.Title;
    }

    /// <summary>
    /// Normalizes structural line breaks at section boundaries
    /// without removing trailing spaces or tabs.
    /// </summary>
    private static string GetComparableContent(
        ArticleSection section)
    {
        string content = section.LogicalContent;

        // The lead may contain only whitespace preceding the first heading.
        // This is structural separation rather than meaningful lead content.
        if (section.IsLead && string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // Normalize line endings at section boundaries.
        return content.TrimEnd('\r', '\n');
    }
}