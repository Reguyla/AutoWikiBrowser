using NUnit.Framework;
using Twain.Core.ArticleAnalysis;

namespace Twain.Tests;

[TestFixture]
public class ArticleSectionComparerTests
{
    [Test]
    public void GetModifiedSections_CategoryOnlyChange_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military personnel]]";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_SectionContentChange_ReturnsSection()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "== References ==\r\n" +
            "* Updated reference.\r\n" +
            "[[Category:Military history]]";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.EqualTo(new[] { "References" }));
    }

    [Test]
    public void GetModifiedSections_CategoryAndContentChange_ReturnsSection()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "== References ==\r\n" +
            "* Updated reference.\r\n" +
            "[[Category:Military personnel]]";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.EqualTo(new[] { "References" }));
    }

    [Test]
    public void GetModifiedSections_UnchangedArticle_ReturnsEmpty()
    {
        const string article =
            "== History ==\r\n" +
            "Original history.";

        var result = ArticleSectionComparer.GetModifiedSections(
            article,
            article);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_SectionCountChanged_ReturnsEmpty()
    {
        const string original =
            "== History ==\r\n" +
            "Original history.";

        const string modified =
            "== History ==\r\n" +
            "Original history.\r\n" +
            "== References ==\r\n" +
            "New references.";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_AddFirstCategory_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_RemoveLastCategory_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_ReorderedSections_ReturnsEmpty()
    {
        const string original =
            "== History ==\r\n" +
            "History content.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        const string modified =
            "== References ==\r\n" +
            "Reference content.\r\n" +
            "== History ==\r\n" +
            "History content.";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_RenamedHeading_ReturnsEmpty()
    {
        const string original =
            "== History ==\r\n" +
            "Original content.";

        const string modified =
            "== Background ==\r\n" +
            "Original content.";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_DuplicateHeadings_ReturnsEmpty()
    {
        const string original =
            "== History ==\r\n" +
            "First history section.\r\n" +
            "== History ==\r\n" +
            "Second history section.";

        const string modified =
            "== History ==\r\n" +
            "Updated first section.\r\n" +
            "== History ==\r\n" +
            "Second history section.";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSectionByPosition_CategoryOnlyChange_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military history]]";

        const string modified =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "[[Category:Military personnel]]";

        string result =
            ArticleSectionComparer.GetModifiedSectionByPosition(
                original,
                modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSectionByPosition_SectionChange_ReturnsHeading()
    {
        const string original =
            "== References ==\r\n" +
            "* Example reference.";

        const string modified =
            "== References ==\r\n" +
            "* Updated reference.";

        string result =
            ArticleSectionComparer.GetModifiedSectionByPosition(
                original,
                modified);

        Assert.That(result, Is.EqualTo("References"));
    }

    [Test]
    public void GetModifiedSectionByPosition_RenamedHeading_ReturnsNewHeading()
    {
        const string original =
            "== History ==\r\n" +
            "Original content.";

        const string modified =
            "== Background ==\r\n" +
            "Original content.";

        string result =
            ArticleSectionComparer.GetModifiedSectionByPosition(
                original,
                modified);

        Assert.That(result, Is.EqualTo("Background"));
    }

    [Test]
    public void GetModifiedSectionByPosition_MultipleChanges_ReturnsEmpty()
    {
        const string original =
            "== History ==\r\n" +
            "Original history.\r\n" +
            "== References ==\r\n" +
            "Original references.";

        const string modified =
            "== History ==\r\n" +
            "Updated history.\r\n" +
            "== References ==\r\n" +
            "Updated references.";

        string result =
            ArticleSectionComparer.GetModifiedSectionByPosition(
                original,
                modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSectionByPosition_LeadChange_ReturnsTop()
    {
        const string original =
            "Original introduction.\r\n" +
            "== History ==\r\n" +
            "History content.";

        const string modified =
            "Updated introduction.\r\n" +
            "== History ==\r\n" +
            "History content.";

        string result =
            ArticleSectionComparer.GetModifiedSectionByPosition(
                original,
                modified);

        Assert.That(result, Is.EqualTo("top"));
    }

    [Test]
    public void GetModifiedSectionByPosition_AddedLeadSection_ReturnsEmpty()
    {
        const string original =
            "==foo==\r\nbar";

        const string modified =
            "test\r\n==foo==\r\nbar";

        string result =
            ArticleSectionComparer.GetModifiedSectionByPosition(
                original,
                modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_ReorderedSectionsWithLeadingWhitespace_ReturnsEmpty()
    {
        const string original =
            "== History ==\r\n" +
            "History content.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        const string modified =
            "\r\n" +
            "== References ==\r\n" +
            "Reference content.\r\n" +
            "== History ==\r\n" +
            "History content.";

        var result = ArticleSectionComparer.GetModifiedSections(
            original,
            modified);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetModifiedSections_BlankLineAddedWithinSection_ReturnsSection()
    {
        const string original =
            "== History ==\r\n" +
            "First paragraph.\r\n" +
            "Second paragraph.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        const string modified =
            "== History ==\r\n" +
            "First paragraph.\r\n" +
            "\r\n" +
            "Second paragraph.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        Assert.That(
            ArticleSectionComparer.GetModifiedSections(original, modified),
            Is.EqualTo(new[] { "History" }));
    }

    [Test]
    public void GetModifiedSections_BlankLineRemovedWithinSection_ReturnsSection()
    {
        const string original =
            "== History ==\r\n" +
            "First paragraph.\r\n" +
            "\r\n" +
            "Second paragraph.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        const string modified =
            "== History ==\r\n" +
            "First paragraph.\r\n" +
            "Second paragraph.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        Assert.That(
            ArticleSectionComparer.GetModifiedSections(original, modified),
            Is.EqualTo(new[] { "History" }));
    }

    [Test]
    public void GetModifiedSections_TrailingSpaceChanged_ReturnsSection()
    {
        const string original =
            "== History ==\r\n" +
            "History content.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        const string modified =
            "== History ==\r\n" +
            "History content. \r\n" +
            "== References ==\r\n" +
            "Reference content.";

        Assert.That(
            ArticleSectionComparer.GetModifiedSections(original, modified),
            Is.EqualTo(new[] { "History" }));
    }

    [Test]
    public void GetModifiedSections_ReorderedSectionsWithDifferentLineEndings_ReturnsEmpty()
    {
        const string original =
            "== History ==\r\n" +
            "History content.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        const string modified =
            "== References ==\r\n" +
            "Reference content.\r\n" +
            "== History ==\r\n" +
            "History content.";

        Assert.That(
            ArticleSectionComparer.GetModifiedSections(original, modified),
            Is.Empty);
    }

    [Test]
    public void GetModifiedSections_CategoryChangeAfterDefaultSort_DoesNotModifyReferences()
    {
        const string original =
            "Article introduction.\r\n" +
            "\r\n" +
            "== References ==\r\n" +
            "{{Refbegin}}\r\n" +
            "* Example reference.\r\n" +
            "{{Refend}}\r\n" +
            "\r\n" +
            "{{Wikipedia|John Wilson Danenhower}}\r\n" +
            "\r\n" +
            "{{DEFAULTSORT:Danenhower, John Wilson}}\r\n" +
            "[[Category:American polar explorers]]\r\n" +
            "[[Category:United States Navy officers]]\r\n" +
            "[[Category:American memoirists]]\r\n" +
            "[[Category:Graduates of the United States Naval Academy Class of 1870]]\r\n" +
            "[[Category:People from Annapolis, Maryland]]\r\n";

        const string modified =
            "Article introduction.\r\n" +
            "\r\n" +
            "== References ==\r\n" +
            "{{Refbegin}}\r\n" +
            "* Example reference.\r\n" +
            "{{Refend}}\r\n" +
            "\r\n" +
            "{{Wikipedia|John Wilson Danenhower}}\r\n" +
            "\r\n" +
            "{{DEFAULTSORT:Danenhower, John Wilson}}\r\n" +
            "[[Category:American polar explorers]]\r\n" +
            "[[Category:United States Navy officers]]\r\n" +
            "[[Category:American memoirists]]\r\n" +
            "[[Category:Graduates of the United States Naval Academy Class of 1870]]\r\n" +
            "[[Category:People from Annapolis, Maryland|Danenhower]]\r\n";

        IReadOnlyList<string> result =
            ArticleSectionComparer.GetModifiedSections(
                original,
                modified);

        Assert.That(
            result,
            Is.Empty,
            "Category-only changes must not be attributed to References.");
    }

    [Test]
    public void GetModifiedSections_MalformedCategoryFixed_DoesNotModifyReferences()
    {
        const string original =
            "== References ==\r\n" +
            "{{Refbegin}}\r\n" +
            "* Example reference.\r\n" +
            "{{Refend}}\r\n" +
            "\r\n" +
            "{{Wikipedia|John Wilson Danenhower}}\r\n" +
            "\r\n" +
            "{{DEFAULTSORT:Danenhower, John Wilson}}\r\n" +
            "[[Category:American polar explorers]]\r\n" +
            "[[Category:Graduates of the United States Naval Academy Class of 1870]\r\n" +
            "[[Category:People from Annapolis, Maryland]]";

        const string modified =
            "== References ==\r\n" +
            "{{Refbegin}}\r\n" +
            "* Example reference.\r\n" +
            "{{Refend}}\r\n" +
            "\r\n" +
            "{{Wikipedia|John Wilson Danenhower}}\r\n" +
            "\r\n" +
            "{{DEFAULTSORT:Danenhower, John Wilson}}\r\n" +
            "[[Category:American polar explorers]]\r\n" +
            "[[Category:Graduates of the United States Naval Academy Class of 1870]]\r\n" +
            "[[Category:People from Annapolis, Maryland]]";

        IReadOnlyList<string> result =
            ArticleSectionComparer.GetModifiedSections(
                original,
                modified);

        Assert.That(
            result,
            Is.Empty,
            "Correcting malformed category markup must not modify References.");
    }

    [Test]
    public void GetModifiedSectionByPosition_MalformedCategoryFixed_ReturnsEmpty()
    {
        const string original =
            "== References ==\r\n" +
            "{{Refbegin}}\r\n" +
            "* Example reference.\r\n" +
            "{{Refend}}\r\n" +
            "\r\n" +
            "{{Wikipedia|John Wilson Danenhower}}\r\n" +
            "\r\n" +
            "{{DEFAULTSORT:Danenhower, John Wilson}}\r\n" +
            "[[Category:American polar explorers]]\r\n" +
            "[[Category:Graduates of the United States Naval Academy Class of 1870]\r\n" +
            "[[Category:People from Annapolis, Maryland]]";

        const string modified =
            "== References ==\r\n" +
            "{{Refbegin}}\r\n" +
            "* Example reference.\r\n" +
            "{{Refend}}\r\n" +
            "\r\n" +
            "{{Wikipedia|John Wilson Danenhower}}\r\n" +
            "\r\n" +
            "{{DEFAULTSORT:Danenhower, John Wilson}}\r\n" +
            "[[Category:American polar explorers]]\r\n" +
            "[[Category:Graduates of the United States Naval Academy Class of 1870]]\r\n" +
            "[[Category:People from Annapolis, Maryland]]";

        string result =
            ArticleSectionComparer.GetModifiedSectionByPosition(
                original,
                modified);

        Assert.That(
            result,
            Is.Empty,
            "Correcting malformed category markup must not be reported " +
            "as a modification to the References section.");
    }
}