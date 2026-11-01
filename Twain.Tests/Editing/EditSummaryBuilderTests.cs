using NUnit.Framework;
using Twain.Core.EditSummaries;

namespace Twain.Tests.Editing;

[TestFixture]
public class EditSummaryBuilderTests
{
    [Test]
    public void Build_SingleModifiedSection_AddsSectionPrefix()
    {
        const string originalText =
            "Introduction\n\n" +
            "== History ==\n" +
            "Original history.\n\n" +
            "== References ==\n" +
            "Original references.\n";

        const string modifiedText =
            "Introduction\n\n" +
            "== History ==\n" +
            "Updated history.\n\n" +
            "== References ==\n" +
            "Original references.\n";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed formatting",
            originalText,
            modifiedText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo(
                "/* History */ General cleanup, Fixed formatting"));
    }

    [Test]
    public void Build_TwoModifiedSections_IncludesBothSectionNames()
    {
        const string originalText =
            "Introduction\n\n" +
            "== History ==\n" +
            "Original history.\n\n" +
            "== References ==\n" +
            "Original references.\n";

        const string modifiedText =
            "Introduction\n\n" +
            "== History ==\n" +
            "Updated history.\n\n" +
            "== References ==\n" +
            "Updated references.\n";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed formatting",
            originalText,
            modifiedText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo(
                "Updated History and References sections; General cleanup, Fixed formatting"));
    }

    [Test]
    public void Build_ThreeModifiedSections_UsesMultipleSectionsDescription()
    {
        const string originalText =
            "Introduction\n\n" +
            "== History ==\n" +
            "Original history.\n\n" +
            "== Geography ==\n" +
            "Original geography.\n\n" +
            "== References ==\n" +
            "Original references.\n";

        const string modifiedText =
            "Introduction\n\n" +
            "== History ==\n" +
            "Updated history.\n\n" +
            "== Geography ==\n" +
            "Updated geography.\n\n" +
            "== References ==\n" +
            "Updated references.\n";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed formatting",
            originalText,
            modifiedText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo(
                "Updated multiple sections; General cleanup, Fixed formatting"));
    }

    [Test]
    public void Build_SectionSummariesDisabled_DoesNotAddSectionInformation()
    {
        const string originalText =
            "== History ==\n" +
            "Original history.\n";

        const string modifiedText =
            "== History ==\n" +
            "Updated history.\n";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed formatting",
            originalText,
            modifiedText,
            includeSectionSummary: false);

        Assert.That(
            result,
            Is.EqualTo("General cleanup, Fixed formatting"));
    }

    [Test]
    public void Build_UnchangedArticle_DoesNotAddSectionInformation()
    {
        const string articleText =
            "== History ==\n" +
            "Original history.\n";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed formatting",
            articleText,
            articleText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo("General cleanup, Fixed formatting"));
    }

    [Test]
    public void Build_ReorderedSections_DoesNotAddSectionInformation()
    {
        const string originalText =
            "== History ==\n" +
            "History content.\n" +
            "== References ==\n" +
            "Reference content.";

        const string modifiedText =
            "== References ==\n" +
            "Reference content.\n" +
            "== History ==\n" +
            "History content.";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed formatting",
            originalText,
            modifiedText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo("General cleanup, Fixed formatting"));
    }

    [Test]
    public void Build_ExistingSectionPrefix_DoesNotDuplicatePrefix()
    {
        const string originalText =
            "== History ==\n" +
            "Original history.\n";

        const string modifiedText =
            "== History ==\n" +
            "Updated history.\n";

        string result = EditSummaryBuilder.Build(
            "/* History */ General cleanup",
            "Fixed formatting",
            originalText,
            modifiedText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo(
                "/* History */ General cleanup, Fixed formatting"));
    }

    [Test]
    public void CorrectSectionEditSummary_InvalidPrefix_RemovesPrefix()
    {
        const string originalText =
            "== History ==\n" +
            "Original history.\n" +
            "== References ==\n" +
            "Original references.";

        const string modifiedText =
            "== History ==\n" +
            "Original history.\n" +
            "== References ==\n" +
            "Updated references.";

        string result = Summary.CorrectSectionEditSummary(
            "/* History */ General cleanup",
            originalText,
            modifiedText);

        Assert.That(
            result.TrimStart(),
            Is.EqualTo("General cleanup"));
    }

    [Test]
    public void Build_CategoryOnlyChange_AddsCategoryDescription()
    {
        const string originalText =
            "== References ==\n" +
            "Reference content.\n" +
            "[[Category:Military history]]";

        const string modifiedText =
            "== References ==\n" +
            "Reference content.\n" +
            "[[Category:Military personnel]]";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            string.Empty,
            originalText,
            modifiedText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo("Updated categories; General cleanup"));
    }

    [Test]
    public void Build_SectionAndCategoryChange_ReportsBoth()
    {
        const string originalText =
            "== History ==\n" +
            "Original history.\n" +
            "[[Category:Military history]]";

        const string modifiedText =
            "== History ==\n" +
            "Updated history.\n" +
            "[[Category:Military personnel]]";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            string.Empty,
            originalText,
            modifiedText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo(
                "Updated History section and categories; General cleanup"));
    }

    [Test]
    public void Build_CategoryReordering_DoesNotAddCategoryDescription()
    {
        const string originalText =
            "Article content.\n" +
            "[[Category:Military history]]\n" +
            "[[Category:Military personnel]]";

        const string modifiedText =
            "Article content.\n" +
            "[[Category:Military personnel]]\n" +
            "[[Category:Military history]]";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            string.Empty,
            originalText,
            modifiedText,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo("General cleanup"));
    }

    [Test]
    public void Build_ExistingSectionPrefixAndCategoryChange_PreservesPrefix()
    {
        const string original =
            "== History ==\r\n" +
            "Original history.\r\n" +
            "\r\n" +
            "[[Category:Old category]]";

        const string modified =
            "== History ==\r\n" +
            "Updated history.\r\n" +
            "\r\n" +
            "[[Category:New category]]";

        string result = EditSummaryBuilder.Build(
            "/* History */ Corrected dates",
            string.Empty,
            original,
            modified,
            includeSectionSummary: true);

        Assert.That(
            result,
            Is.EqualTo("/* History */ Corrected dates; Updated categories"),
            "The existing section prefix must remain at the beginning " +
            "while category changes are reported.");

        Assert.That(
            Summary.IsCorrect(result),
            Is.True,
            "The combined edit summary must pass existing validation.");
    }

    [TestFixture]
    public class LeadSectionEditSummaryTests
    {
        private const string GeneralSummary = "General summary";

        [Test]
        public void Build_LeadChanged_ReportsLeadSection()
        {
            const string original =
                "Original introduction.\n\n" +
                "== History ==\n" +
                "History text.";

            const string modified =
                "Updated introduction.\n\n" +
                "== History ==\n" +
                "History text.";

            string result = BuildSummary(original, modified);

            Assert.That(
                result,
                Is.EqualTo("Updated lead section; General summary"));
        }

        [Test]
        public void Build_LeadAndCategoriesChanged_ReportsBothSeparately()
        {
            const string original =
                "Original introduction.\n\n" +
                "== History ==\n" +
                "History text.\n\n" +
                "[[Category:Old category]]";

            const string modified =
                "Updated introduction.\n\n" +
                "== History ==\n" +
                "History text.\n\n" +
                "[[Category:New category]]";

            string result = BuildSummary(original, modified);

            Assert.That(
                result,
                Is.EqualTo(
                    "Updated lead section; Updated categories; General summary"));
        }

        [Test]
        public void Build_CategoriesOnly_ReportsCategories()
        {
            const string original =
                "Original introduction.\n\n" +
                "== History ==\n" +
                "History text.\n\n" +
                "[[Category:Old category]]";

            const string modified =
                "Original introduction.\n\n" +
                "== History ==\n" +
                "History text.\n\n" +
                "[[Category:New category]]";

            string result = BuildSummary(original, modified);

            Assert.That(
                result,
                Is.EqualTo("Updated categories; General summary"));
        }
    }

        [Test]
        public void Build_NamedSectionChanged_PreservesSectionPrefix()
        {
            const string original =
                "Introduction.\n\n" +
                "== History ==\n" +
                "Original history.";

            const string modified =
                "Introduction.\n\n" +
                "== History ==\n" +
                "Updated history.";

            string result = BuildSummary(original, modified);

            Assert.That(
                result,
                Is.EqualTo("/* History */ General summary"));
        }

        [Test]
        public void Build_LeadAndNamedSectionChanged_ReportsBoth()
        {
            const string original =
                "Original introduction.\n\n" +
                "== History ==\n" +
                "Original history.";

            const string modified =
                "Updated introduction.\n\n" +
                "== History ==\n" +
                "Updated history.";

            string result = BuildSummary(original, modified);

            Assert.That(
                result,
                Is.EqualTo(
                    "Updated lead and History sections; General summary"));
        }

    private static string BuildSummary(
        string original,
        string modified)
    {
        return EditSummaryBuilder.Build(
            "General summary",
            string.Empty,
            original,
            modified,
            true);
    }
}