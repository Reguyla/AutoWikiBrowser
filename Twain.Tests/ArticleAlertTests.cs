using NUnit.Framework;
using Twain.Core;
using Twain.Core.Parse;

namespace Twain.Tests.Alerts;

[TestFixture]
public class ArticleAlertTests : RequiresInitialization
{
    [Test]
    public void DeadLinks()
    {
        Article article = new("A", "{{dead link}}");

        Assert.That(article.DeadLinks().Count, Is.EqualTo(1));
        Assert.That(article.HasDeadLinks, Is.True);
    }

    [Test]
    public void UnclosedTags()
    {
        Article article = new("A", "<small>");

        Assert.That(article.UnclosedTags().Count, Is.EqualTo(1));
    }

    [Test]
    public void WikiLinkedHeaders()
    {
        Article article = new("A", "==[[A]]==");

        Assert.That(article.WikiLinkedHeaders().Count, Is.EqualTo(1));
    }

    [Test]
    public void UnknownMultipleIssuesParameters()
    {
        Article article = new("A", "{{multiple issues|foo=bar}}");

        Assert.That(
            article.UnknownMultipleIssuesParameters().Count,
            Is.EqualTo(1));
    }

    [Test]
    public void ValidMultipleIssuesParameters()
    {
        Article article = new(
            "A",
            "{{multiple issues|section=bar|collapsed=yes}}");

        Assert.That(
            article.UnknownMultipleIssuesParameters().Count,
            Is.Zero);
    }

    [Test]
    public void UnknownWikiProjectBannerShellParameters()
    {
        Article article = new(
            "Talk:A",
            "{{WikiProject banner shell|foo=bar}}");

        Assert.That(
            article.UnknownWikiProjectBannerShellParameters().Count,
            Is.EqualTo(1));
    }

    [Test]
    public void ValidWikiProjectBannerShellParameters()
    {
        Article article = new(
            "Talk:A",
            "{{WikiProject banner shell|collapsed=true|living=a|class=a|listas=a|demo_page=a|vital=a}}");

        Assert.That(
            article.UnknownWikiProjectBannerShellParameters().Count,
            Is.Zero);
    }

    [Test]
    public void DuplicateWikiProjectBannerShellParameters()
    {
        Article article = new(
            "Talk:A",
            "{{WikiProject banner shell|foo=bar|foo=bar}}");

        Assert.That(
            article.DuplicateWikiProjectBannerShellParameters().Count,
            Is.EqualTo(1));
    }

    [TestCase("[[A|B|C]]")]
    [TestCase("[[A||BC]]")]
    public void DoublePipeLinks(string text)
    {
        Article article = new("A", text);

        Assert.That(article.DoublepipeLinks().Count, Is.EqualTo(1));
    }

    [Test]
    public void TargetlessLinks()
    {
        Article article = new("A", "[[|A]]");

        Assert.That(article.TargetlessLinks().Count, Is.EqualTo(1));
    }

    [Test]
    public void BadCitationParameters()
    {
        Article article = new("A", "{{cite web|sajksdfa=a}}");

        Assert.That(article.BadCiteParameters().Count, Is.EqualTo(1));
    }

    [Test]
    public void AmbiguousCitationDates()
    {
        Article article = new("A", "{{cite web|date=5-4-10}}");

        Assert.That(
            article.AmbiguousCiteTemplateDates().Count,
            Is.EqualTo(1));
    }

    [Test]
    public void UserSignatures()
    {
        Article article = new("A", "[[User talk:Noobie]]");

        Assert.That(article.UserSignature().Count, Is.EqualTo(1));
    }

    [Test]
    public void ReferenceAfterReflist()
    {
        const string text = """
        Article text.

        == References ==
        {{reflist}}

        <ref>Reference appearing after reflist.</ref>
        """;

        Article article = new("A", text);

        Assert.That(article.HasRefAfterReflist, Is.True);
    }

    [Test]
    public void ReferenceBeforeReflist()
    {
        const string text = """
        Article text.

        <ref>Reference appearing before reflist.</ref>

        == References ==
        {{reflist}}
        """;

        Article article = new("A", text);

        Assert.That(article.HasRefAfterReflist, Is.False);
    }

    [Test]
    public void ReferenceAfterReflistLocations()
    {
        const string text = """
        Article text.

        == References ==
        {{reflist}}

        <ref>First reference.</ref>
        <ref>Second reference.</ref>
        """;

        Dictionary<int, int> locations =
            Parsers.GetReferencesAfterReflistLocations(text);

        int firstIndex = text.IndexOf(
            "<ref>First reference.</ref>",
            StringComparison.Ordinal);

        int secondIndex = text.IndexOf(
            "<ref>Second reference.</ref>",
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(locations.Count, Is.EqualTo(2));
            Assert.That(
                locations.ContainsKey(firstIndex),
                Is.True);
            Assert.That(
                locations.ContainsKey(secondIndex),
                Is.True);
        });
    }

    [Test]
    public void ReferenceAfterReflistLocationsIgnoreComments()
    {
        const string text = """
        Article text.

        == References ==
        {{reflist}}

        <!-- <ref>Commented reference.</ref> -->

        <ref>Actual reference.</ref>
        """;

        Dictionary<int, int> locations =
            Parsers.GetReferencesAfterReflistLocations(text);

        int actualIndex = text.IndexOf(
            "<ref>Actual reference.</ref>",
            StringComparison.Ordinal);

        int commentedIndex = text.IndexOf(
            "<ref>Commented reference.</ref>",
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(locations.Count, Is.EqualTo(1));

            Assert.That(
                locations.ContainsKey(actualIndex),
                Is.True);

            Assert.That(
                locations.ContainsKey(commentedIndex),
                Is.False);
        });
    }

    [Test]
    public void MoreFootnotesWithManyReferences()
    {
        const string text = """
        {{More footnotes needed}}

        Article text.

        <ref>Reference 1</ref>
        <ref>Reference 2</ref>
        <ref>Reference 3</ref>
        <ref>Reference 4</ref>
        <ref>Reference 5</ref>

        == References ==
        {{reflist}}
        """;

        Article article = new("A", text);

        Assert.That(
            article.HasMorefootnotesAndManyReferences,
            Is.True);
    }

    [Test]
    public void MoreFootnotesWithFourReferences()
    {
        const string text = """
        {{More footnotes needed}}

        Article text.

        <ref>Reference 1</ref>
        <ref>Reference 2</ref>
        <ref>Reference 3</ref>
        <ref>Reference 4</ref>

        == References ==
        {{reflist}}
        """;

        Article article = new("A", text);

        Assert.That(
            article.HasMorefootnotesAndManyReferences,
            Is.False);
    }

    [Test]
    public void MoreFootnotesTemplateLocations()
    {
        const string text = """
        {{More footnotes needed}}

        Article text.

        <ref>Reference 1</ref>
        <ref>Reference 2</ref>
        <ref>Reference 3</ref>
        <ref>Reference 4</ref>
        <ref>Reference 5</ref>

        == References ==
        {{reflist}}
        """;

        Dictionary<int, int> locations =
            Parsers.GetMoreFootnotesTemplateLocations(text);

        const string template = "{{More footnotes needed}}";

        int expectedIndex = text.IndexOf(
            template,
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(locations.Count, Is.EqualTo(1));

            Assert.That(
                locations.ContainsKey(expectedIndex),
                Is.True);

            Assert.That(
                locations.GetValueOrDefault(expectedIndex),
                Is.EqualTo(template.Length));
        });
    }

    [Test]
    public void MoreFootnotesTemplateLocationsIgnoreComments()
    {
        const string text = """
        <!-- {{More footnotes needed}} -->

        {{More footnotes needed}}

        Article text.

        <ref>Reference 1</ref>
        <ref>Reference 2</ref>
        <ref>Reference 3</ref>
        <ref>Reference 4</ref>
        <ref>Reference 5</ref>

        == References ==
        {{reflist}}
        """;

        Dictionary<int, int> locations =
            Parsers.GetMoreFootnotesTemplateLocations(text);

        const string template = "{{More footnotes needed}}";

        int commentedIndex = text.IndexOf(
            template,
            StringComparison.Ordinal);

        int actualIndex = text.LastIndexOf(
            template,
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(locations.Count, Is.EqualTo(1));

            Assert.That(
                locations.ContainsKey(commentedIndex),
                Is.False);

            Assert.That(
                locations.ContainsKey(actualIndex),
                Is.True);

            Assert.That(
                locations.GetValueOrDefault(actualIndex),
                Is.EqualTo(template.Length));
        });
    }

}