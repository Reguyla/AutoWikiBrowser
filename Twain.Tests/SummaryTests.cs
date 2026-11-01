using NUnit.Framework;
using NUnit.Framework.Legacy;
using Twain.Core;
using Twain.Core.EditSummaries;

namespace Twain.Tests;

[TestFixture]
public class SummaryTests : RequiresInitialization
{
    [Test]
    public void IsCorrect()
    {
        // too long
        StringBuilder sb = new StringBuilder(300);
        for (int i = 0; i < 256; i++) sb.Append('x');
        ClassicAssert.IsFalse(Summary.IsCorrect(sb.ToString()));

        // no wikilinks
        ClassicAssert.IsTrue(Summary.IsCorrect(""));
        ClassicAssert.IsTrue(Summary.IsCorrect("test"));
        ClassicAssert.IsTrue(Summary.IsCorrect("["));
        ClassicAssert.IsTrue(Summary.IsCorrect("]"));
        ClassicAssert.IsTrue(Summary.IsCorrect("[test]"));
        ClassicAssert.IsTrue(Summary.IsCorrect("[test]]"));
        ClassicAssert.IsTrue(Summary.IsCorrect("[[]]"));

        // correctly (sort of..) terminated wikilinks
        ClassicAssert.IsTrue(Summary.IsCorrect("[[test]]"));
        ClassicAssert.IsTrue(Summary.IsCorrect("[[test]] [[foo]]"));

        // broken wikilinks, should be found to be invalid
        ClassicAssert.IsFalse(Summary.IsCorrect("[["));
        ClassicAssert.IsFalse(Summary.IsCorrect("[[["));
        ClassicAssert.IsFalse(Summary.IsCorrect("[[test]"));
        ClassicAssert.IsFalse(Summary.IsCorrect("[[test]] [["));
        ClassicAssert.IsFalse(Summary.IsCorrect("[[123456789 123456789 123456789 1[[WP:AWB]]"));
        ClassicAssert.IsFalse(Summary.IsCorrect("[[foo[[]]]"));
    }

    [Test]
    public void Trim()
    {
        Assert.That(Summary.Trim("test"), Is.EqualTo("test"));

        const string bug1 =
            @"replaced category 'Actual event ballads' → 'Songs based on actual events' per [[Wikipedia:Categories for discussion/Log/2009 November 6|CfD 2009 Nov 6]]";
        const string waffle = @"some waffle here to make the edit summary too long";

        Assert.That(Summary.Trim(bug1), Is.EqualTo(bug1));
        Assert.That(Summary.Trim(waffle + bug1), Is.EqualTo(waffle + bug1));
        Assert.That(
            Summary.Trim(waffle + waffle + bug1),
            Is.EqualTo(waffle + waffle + @"replaced category 'Actual event ballads' → 'Songs based on actual events' per..."));
    }

    [Test]
    public void ModifiedSection()
    {
        Assert.That(Summary.ModifiedSection("", ""), Is.Empty, "No change to text");
        Assert.That(Summary.ModifiedSection("foo", "foo"), Is.Empty, "No change to text 2");
        Assert.That(Summary.ModifiedSection("foo", "bar"), Is.EqualTo("top"), "Zeroth section change, no other sections");
        Assert.That(Summary.ModifiedSection("foo\r\n==sec==\r\na", "bar\r\n==sec==\r\na"), Is.EqualTo("top"), "Zeroth section change");

        Assert.That(Summary.ModifiedSection("==foo==\r\n123", "==foo==\r\ntest"), Is.EqualTo("foo"), "Section 1 change, no zeroth section");
        Assert.That(Summary.ModifiedSection("rawr\r\n==foo==\r\n123", "rawr\r\n==foo==\r\ntest"), Is.EqualTo("foo"));
        Assert.That(Summary.ModifiedSection("==foo==\r\n123", "== foo ==\r\ntest"), Is.EqualTo("foo"));
        Assert.That(Summary.ModifiedSection("==foo==\r\n123", "==bar==\r\ntest"), Is.EqualTo("bar"));

        // https://en.wikipedia.org/w/index.php?diff=prev&oldid=338360216
        Assert.That(Summary.ModifiedSection("==foo==\r\nbar", "test\r\n==foo==\r\nbar"), Is.Empty);
    }

    [Test]
    public void GetModifiedSections_SingleSection()
    {
        const string original =
            "== History ==\r\nOriginal text\r\n== References ==\r\nOriginal references";

        const string modified =
            "== History ==\r\nUpdated text\r\n== References ==\r\nOriginal references";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.EqualTo(new[] { "History" }));
    }

    [Test]
    public void GetModifiedSections_MultipleSections()
    {
        const string original =
            "== History ==\r\nOriginal text\r\n== References ==\r\nOriginal references";

        const string modified =
            "== History ==\r\nUpdated text\r\n== References ==\r\nUpdated references";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.EqualTo(new[] { "History", "References" }));
    }

    [Test]
    public void GetModifiedSections_UnchangedArticle()
    {
        const string article =
            "== History ==\r\nOriginal text";

        Assert.That(
            Summary.GetModifiedSections(article, article),
            Is.Empty);
    }

    [Test]
    public void GetModifiedSections_SectionCountChanged()
    {
        const string original =
            "== History ==\r\nOriginal text";

        const string modified =
            "== History ==\r\nOriginal text\r\n== References ==\r\nNew references";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void GetModifiedSections_LeadSectionChanged()
    {
        const string original =
            "Original introduction\r\n== History ==\r\nOriginal history";

        const string modified =
            "Updated introduction\r\n== History ==\r\nOriginal history";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.EqualTo(new[] { "top" }));
    }

    [Test]
    public void GetModifiedSections_ReorderedSections_DoesNotReportContentChanges()
    {
        const string original =
            "== History ==\r\nHistory content\r\n" +
            "== References ==\r\nReferences content";

        const string modified =
            "== References ==\r\nReferences content\r\n" +
            "== History ==\r\nHistory content";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void EditSummaryBuilder_SingleSection_UsesMediaWikiPrefix()
    {
        const string original =
            "== History ==\r\nOriginal text";

        const string modified =
            "== History ==\r\nUpdated text";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed references",
            original,
            modified,
            true);

        Assert.That(
            result,
            Is.EqualTo("/* History */ General cleanup, Fixed references"));
    }

    [Test]
    public void EditSummaryBuilder_TwoSections_ListsBothSections()
    {
        const string original =
            "== History ==\r\nOriginal text\r\n" +
            "== References ==\r\nOriginal references";

        const string modified =
            "== History ==\r\nUpdated text\r\n" +
            "== References ==\r\nUpdated references";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed references",
            original,
            modified,
            true);

        Assert.That(
            result,
            Is.EqualTo(
                "Updated History and References sections; General cleanup, Fixed references"));
    }

    [Test]
    public void EditSummaryBuilder_ThreeSections_UsesGenericDescription()
    {
        const string original =
            "== History ==\r\nOriginal text\r\n" +
            "== References ==\r\nOriginal references\r\n" +
            "== See also ==\r\nOriginal links";

        const string modified =
            "== History ==\r\nUpdated text\r\n" +
            "== References ==\r\nUpdated references\r\n" +
            "== See also ==\r\nUpdated links";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed references",
            original,
            modified,
            true);

        Assert.That(
            result,
            Is.EqualTo(
                "Updated multiple sections; General cleanup, Fixed references"));
    }

    [Test]
    public void EditSummaryBuilder_SectionSummariesDisabled_OmitsSections()
    {
        const string original =
            "== History ==\r\nOriginal text\r\n" +
            "== References ==\r\nOriginal references";

        const string modified =
            "== History ==\r\nUpdated text\r\n" +
            "== References ==\r\nUpdated references";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed references",
            original,
            modified,
            false);

        Assert.That(
            result,
            Is.EqualTo("General cleanup, Fixed references"));
    }

    [Test]
    public void EditSummaryBuilder_NoChanges_OmitsSectionDescription()
    {
        const string article =
            "== History ==\r\nOriginal text";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            "Fixed references",
            article,
            article,
            true);

        Assert.That(
            result,
            Is.EqualTo("General cleanup, Fixed references"));
    }

    [Test]
    public void EditSummaryBuilder_Collector_OrdersContributionsByCategory()
    {
        var collector = new EditSummaryCollector();

        collector.Add(new EditSummaryContribution
        {
            Category = EditSummaryCategory.TypoCorrections,
            Description = "Corrected typos"
        });

        collector.Add(new EditSummaryContribution
        {
            Category = EditSummaryCategory.CategoryChanges,
            Description = "Updated categories"
        });

        collector.Add(new EditSummaryContribution
        {
            Category = EditSummaryCategory.GeneralFixes,
            Description = "General cleanup"
        });

        EditSummaryResult result = EditSummaryBuilder.Build(
            string.Empty,
            collector,
            new EditSummaryPolicy(),
            "Original text",
            "Updated text",
            false);

        Assert.That(
            result.Text,
            Is.EqualTo(
                "General cleanup; Updated categories; Corrected typos"));
    }

    [Test]
    public void EditSummaryBuilder_Collector_RemovesDuplicateDescriptions()
    {
        var collector = new EditSummaryCollector();

        collector.Add(new EditSummaryContribution
        {
            Category = EditSummaryCategory.GeneralFixes,
            Description = "General cleanup"
        });

        collector.Add(new EditSummaryContribution
        {
            Category = EditSummaryCategory.MetadataCleanup,
            Description = "General cleanup"
        });

        EditSummaryResult result = EditSummaryBuilder.Build(
            string.Empty,
            collector,
            new EditSummaryPolicy(),
            "Original text",
            "Updated text",
            false);

        Assert.That(
            result.Text,
            Is.EqualTo("General cleanup"));
    }

    [Test]
    public void EditSummaryBuilder_Collector_FormatsMultipleSections()
    {
        const string original =
            "== History ==\r\nOriginal text\r\n" +
            "== References ==\r\nOriginal references";

        const string modified =
            "== History ==\r\nUpdated text\r\n" +
            "== References ==\r\nUpdated references";

        var collector = new EditSummaryCollector();

        collector.Add(new EditSummaryContribution
        {
            Category = EditSummaryCategory.GeneralFixes,
            Description = "General cleanup"
        });

        EditSummaryResult result = EditSummaryBuilder.Build(
            string.Empty,
            collector,
            new EditSummaryPolicy(),
            original,
            modified,
            true);

        Assert.That(
            result.Text,
            Is.EqualTo(
                "Updated History and References sections; General cleanup"));
    }

    [Test]
    public void EditSummaryBuilder_Collector_RespectsSectionPreference()
    {
        const string original =
            "== History ==\r\nOriginal text";

        const string modified =
            "== History ==\r\nUpdated text";

        var collector = new EditSummaryCollector();

        collector.Add(new EditSummaryContribution
        {
            Category = EditSummaryCategory.GeneralFixes,
            Description = "General cleanup"
        });

        EditSummaryResult result = EditSummaryBuilder.Build(
            string.Empty,
            collector,
            new EditSummaryPolicy(),
            original,
            modified,
            false);

        Assert.That(
            result.Text,
            Is.EqualTo("General cleanup"));
    }

    [Test]
    public void EditSummaryBuilder_LeadAndNamedSection_UsesSectionDescription()
    {
        const string original =
            "Original introduction\r\n" +
            "== Engagement ==\r\nOriginal engagement text\r\n" +
            "== References ==\r\nOriginal references";

        const string modified =
            "Updated introduction\r\n" +
            "== Engagement ==\r\nUpdated engagement text\r\n" +
            "== References ==\r\nOriginal references";

        string result = EditSummaryBuilder.Build(
            "General cleanup",
            string.Empty,
            original,
            modified,
            true);

        Assert.That(
            result,
            Is.EqualTo(
                "Updated lead and Engagement sections; General cleanup"));
    }

    [Test]
    public void GetModifiedSections_CategoryOnlyChanges_DoesNotModifyReferences()
    {
        const string original =
            "== References ==\r\n" +
            "{{reflist}}\r\n" +
            "[[Category:American explorers]]";

        const string modified =
            "== References ==\r\n" +
            "{{reflist}}\r\n" +
            "[[Category:United States Navy officers]]";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void GetModifiedSections_SectionAndCategoryChanges_ReportsOnlySection()
    {
        const string original =
            "== Biography ==\r\n" +
            "Original biography\r\n" +
            "== References ==\r\n" +
            "{{reflist}}\r\n" +
            "[[Category:American explorers]]";

        const string modified =
            "== Biography ==\r\n" +
            "Updated biography\r\n" +
            "== References ==\r\n" +
            "{{reflist}}\r\n" +
            "[[Category:United States Navy officers]]";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.EqualTo(new[] { "Biography" }));
    }

    [Test]
    public void GetModifiedSections_AddedCategory_DoesNotModifyReferences()
    {
        const string original =
            "== References ==\r\n" +
            "{{reflist}}\r\n" +
            "[[Category:American explorers]]";

        const string modified =
            "== References ==\r\n" +
            "{{reflist}}\r\n" +
            "[[Category:American explorers]]\r\n" +
            "[[Category:United States Navy officers]]";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void GetModifiedSections_CategoryOnlyChange_ReturnsNoSections()
    {
        const string original =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "\r\n" +
            "[[Category:Military personnel]]";

        var result = Summary.GetModifiedSections(original, modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ModifiedSection_CategoryOnlyChange_ReturnsEmpty()
    {
        const string original =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "\r\n" +
            "[[Category:Military personnel]]";

        string result = Summary.ModifiedSection(original, modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_ReferencesChange_ReturnsReferences()
    {
        const string original =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Updated reference.\r\n" +
            "[[Category:Military history]]";

        var result = Summary.GetModifiedSections(original, modified);

        Assert.That(result, Is.EquivalentTo(new[] { "References" }));
    }

    [Test]
    public void GetModifiedSections_CategoryAndReferencesChange_ReturnsReferences()
    {
        const string original =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Updated reference.\r\n" +
            "[[Category:Military personnel]]";

        var result = Summary.GetModifiedSections(original, modified);

        Assert.That(result, Is.EquivalentTo(new[] { "References" }));
    }

    [Test]
    public void AddSectionEditSummary_CategoryOnlyChange_DoesNotAddPrefix()
    {
        const string original =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military personnel]]";

        string result = Summary.AddSectionEditSummary(
            "Updated categories",
            original,
            modified);

        Assert.That(result, Is.EqualTo("Updated categories"));
    }

    [Test]
    public void AddSectionEditSummary_ReferencesChange_AddsPrefix()
    {
        const string original =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.";

        const string modified =
            "Article introduction.\r\n" +
            "== References ==\r\n" +
            "* Updated reference.";

        string result = Summary.AddSectionEditSummary(
            "Updated references",
            original,
            modified);

        Assert.That(
            result,
            Is.EqualTo("/* References */ Updated references"));
    }

    [Test]
    public void GetModifiedSections_DefaultSortOnlyChange_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Smith, John}}";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Jones, John}}";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void GetModifiedSections_DefaultSortAndContentChange_ReturnsSection()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Smith, John}}";

        const string modified =
            "== References ==\r\n" +
            "* Updated reference.\r\n" +
            "{{DEFAULTSORT:Jones, John}}";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.EqualTo(new[] { "References" }));
    }

    [Test]
    public void AddSectionEditSummary_DefaultSortOnlyChange_DoesNotAddPrefix()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Smith, John}}";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Jones, John}}";

        string result = Summary.AddSectionEditSummary(
            "Updated default sort",
            original,
            modified);

        Assert.That(
            result,
            Is.EqualTo("Updated default sort"));
    }

    [Test]
    public void GetModifiedSections_AddDefaultSort_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Smith, John}}";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void GetModifiedSections_RemoveDefaultSort_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Smith, John}}";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void GetModifiedSections_CategoryAndDefaultSortChange_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Smith, John}}\r\n" +
            "[[Category:Writers]]";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Jones, John}}\r\n" +
            "[[Category:Authors]]";

        Assert.That(
            Summary.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void ModifiedSection_AddDefaultSort_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "{{DEFAULTSORT:Smith, John}}";

        Assert.That(
            Summary.ModifiedSection(original, modified),
            Is.Empty);
    }
}