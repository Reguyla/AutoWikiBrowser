using NUnit.Framework;
using Twain.Core.EditSummaries;

namespace Twain.Tests;

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
}