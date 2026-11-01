using NUnit.Framework;
using Twain.Core;

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
}