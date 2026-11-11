using NUnit.Framework;
using NUnit.Framework.Legacy;
using Twain.Core;
using Twain.Core.Parse;

namespace Twain.Tests.Articles;

[TestFixture]
public class ArticleTests : RequiresInitialization
{

    [Test]
    public void UnbalancedBrackets()
    {
        Dictionary<int, int> UnB = new();

        Article a = new Article("TestArticle", "This is the text (here.");
        UnB = a.UnbalancedBrackets();
        Assert.That(UnB.Count, Is.EqualTo(1), "One unbalanced bracket in mainspace article");

        // UnB.Clear();
        a = new Article("TestArticle", @"This is the text here.
    == Section ==
    There [was.");
        UnB = a.UnbalancedBrackets();
        Assert.That(UnB.Count, Is.EqualTo(1), "Unbalanced bracket in mainspace article later sections found");

        // UnB.Clear();
        a = new Article("Talk:TestArticle", @"This is the text (here.
    == Section ==
    There [was.");
        UnB = a.UnbalancedBrackets();
        Assert.That(UnB.Count, Is.EqualTo(1), "One unbalanced bracket in zeroth section of talk article");

        // UnB.Clear();
        a = new Article("Talk:TestArticle", @"This is the text here.
    == Section ==
    There [was.");
        UnB = a.UnbalancedBrackets();
        Assert.That(UnB.Count, Is.EqualTo(0), "No unbalanced bracket in zeroth section of talk article");
    }

    [Test]
    public void Unicodify()
    {
        Parsers Parser = new Parsers();
        HideText RemoveText = new HideText(false, true, false);
        Article a = new Article("a", @"'''test'''. z &amp; a&Dagger; &dagger;.

{{DEFAULTSORT:Hello test}}
[[Category:Test pages]]
");

        a.Unicodify(true, Parser, RemoveText);

        Assert.That(a.ArticleText, Is.EqualTo(@"'''test'''. z & a‡ †.

{{DEFAULTSORT:Hello test}}
[[Category:Test pages]]
"), "Text unicodified");

        a = new Article("a", @"'''test'''. z &amp; {{t|a&Dagger; &dagger;}}.

{{DEFAULTSORT:Hello test}}
[[Category:Test pages]]
");

        a.Unicodify(true, Parser, RemoveText);

        Assert.That(a.ArticleText, Is.EqualTo(@"'''test'''. z & {{t|a&Dagger; &dagger;}}.

{{DEFAULTSORT:Hello test}}
[[Category:Test pages]]
"), "Text unicodified, hidemore used");

        a = new Article("a", @"ABC");
        a.Unicodify(true, Parser, RemoveText);
        Assert.That(a.ArticleText, Is.EqualTo(@"ABC"), "No change");
    }

    [Test]
    public void BulletExternalLinks()
    {
        Article a = new Article("a", @"A
==External links==
http://www.site.com

[[Category:A]]");
        a.BulletExternalLinks(false);
        ClassicAssert.IsTrue(a.ArticleText.Contains("* http://www.site.com"));

        a = new Article("a", @"A
==External links==
http://www.site.com

[[Category:A]]");
        a.BulletExternalLinks(true);
        ClassicAssert.IsTrue(a.ArticleText.Contains("* http://www.site.com"));

        a = new Article("a", @"A
==External links==
* http://www.site.com

[[Category:A]]");
        a.BulletExternalLinks(true);
        ClassicAssert.IsTrue(a.ArticleText.Contains("* http://www.site.com"));
    }

    [Test]
    public void Changes()
    {
        Article a = new Article("A", "ABC");
        a.AWBChangeArticleText("test", "ABC D", false);
        ClassicAssert.IsFalse(a.OnlyWhiteSpaceChanged);
        ClassicAssert.IsFalse(a.OnlyCasingChanged);

        a = new Article("A", "ABC");
        a.AWBChangeArticleText("test", "AB C", false);
        ClassicAssert.IsTrue(a.OnlyWhiteSpaceChanged);
        ClassicAssert.IsTrue(a.OnlyWhiteSpaceAndCasingChanged);
        ClassicAssert.IsFalse(a.OnlyCasingChanged);
        ClassicAssert.IsFalse(a.NoArticleTextChanged);

        a = new Article("A", "ABC");
        a.AWBChangeArticleText("test", "ABc", false);
        ClassicAssert.IsTrue(a.OnlyCasingChanged);

        a = new Article("A", "ABC");
        a.AWBChangeArticleText("test", "ABC", false);
        ClassicAssert.IsTrue(a.NoArticleTextChanged);
        ClassicAssert.IsFalse(a.OnlyMinorGeneralFixesChanged);
    }

    [Test]
    public void Properties()
    {
        Article a = new Article("A", "ABC");
        ClassicAssert.IsFalse(a.ArticleIsAboutAPerson);

        a = new Article("A");
        ClassicAssert.IsFalse(a.HasDiacriticsInTitle);

        a = new Article("Aé");
        ClassicAssert.IsTrue(a.HasDiacriticsInTitle);

        ClassicAssert.IsTrue(a.IsStub);

        a = new Article("A", "ABC");
        ClassicAssert.IsFalse(a.HasStubTemplate);
        ClassicAssert.IsFalse(a.HasInfoBox);
        ClassicAssert.IsFalse(a.HasSicTag);
        ClassicAssert.IsFalse(a.IsInUse);
        ClassicAssert.IsFalse(a.HasTargetLessLinks);
        ClassicAssert.IsFalse(a.HasDoublePipeLinks);
        ClassicAssert.IsFalse(a.HasMorefootnotesAndManyReferences);
        ClassicAssert.IsFalse(a.IsDisambiguationPage);
        ClassicAssert.IsFalse(a.IsDisambiguationPageWithRefs);
        ClassicAssert.IsFalse(a.IsSIAPage);
        ClassicAssert.IsFalse(a.HasRefAfterReflist);
        ClassicAssert.IsFalse(a.HasNamedReferences);
        ClassicAssert.IsFalse(a.HasBareReferences);
        ClassicAssert.IsFalse(a.HasAmbiguousCiteTemplateDates);
        ClassicAssert.IsFalse(a.HasSeeAlsoAfterNotesReferencesOrExternalLinks);
        ClassicAssert.IsFalse(a.IsMissingReferencesDisplay);
        ClassicAssert.IsFalse(a.IsRedirect);
    }

    [Test]
    public void EmboldenTitles()
    {
        Parsers Parser = new Parsers();
        Article a = new Article("a", @"A [[B]]");
        a.EmboldenTitles(Parser, true);
        ClassicAssert.IsTrue(a.ArticleText.Contains("[[B]]"));
    }

    [Test]
    public void General()
    {
        Article a = new Article("A");
        Article b = new Article("A");
        b = null;
        ClassicAssert.IsFalse(a.Equals(null));
        b = new Article("A");
        Assert.That(a.ToString(), Is.EqualTo("A"));
        Assert.That(a.Name, Is.EqualTo("A"));
        ClassicAssert.IsFalse(a != b);

        Assert.That(a.URLEncodedName, Is.EqualTo("A"));
        Assert.That(a.DisplayTitle, Is.EqualTo(null));
    }

    [Test]
    public void NamespacelessName()
    {
        Assert.That(new Article("Foo").NamespacelessName, Is.EqualTo("Foo"));
        Assert.That(new Article("Category:Foo").NamespacelessName, Is.EqualTo("Foo"));
        Assert.That(new Article("Category:Category:Foo").NamespacelessName, Is.EqualTo("Category:Foo"));

        // TODO: uncomment when Namespace.Determine() will support non-normalised names
        // Assert.AreEqual("Foo", new Article("Category : Foo").NamespacelessName);

        // this behaviour has changed in recent MW versions
        // Assert.AreEqual("", new Article("Category:").NamespacelessName);
        // Assert.AreEqual("", new Article("Category: ").NamespacelessName);
    }

    [Test]
    public void ContainsComparers()
    {
        IArticleComparer containsComparer = ArticleComparerFactory.Create("foo\nbar",
                                                                          false,
                                                                          false,
                                                                          false, // singleline
                                                                          false); // multiline

        IArticleComparer notContainsComparer = ArticleComparerFactory.Create("foo\nbar",
                                                                             false,
                                                                             false,
                                                                             false, // singleline
                                                                             false); // multiline

        Article a = new Article("A", "Now foo\nbar");
        ClassicAssert.IsTrue(notContainsComparer.Matches(a), "does not contain comparer");
        ClassicAssert.IsTrue(containsComparer.Matches(a), "it contains comparer");

        a = new Article("A", @"Now foo-bar");
        ClassicAssert.IsFalse(notContainsComparer.Matches(a), "not contains is false");
        ClassicAssert.IsFalse(containsComparer.Matches(a), "contains is false");
    }

    [Test]
    public void Comparers()
    {
        Article a = new Article("A"), b = new Article("B"), z = new Article("Z"), one = new Article("1"), diacritic = new Article("Ș"),
            dollar = new Article("$");

        Assert.That(a.CompareTo(b), Is.EqualTo(-1), "A just before B");
        Assert.That(a.CompareTo(a), Is.EqualTo(0), "equal");
        Assert.That(b.CompareTo(a), Is.EqualTo(1), "B just after A");
        Assert.That(one.CompareTo(a), Is.EqualTo(-16), "1 before A");
        Assert.That(z.CompareTo(diacritic), Is.EqualTo(-446), "Diacritics later than Latin");
        Assert.That(one.CompareTo(diacritic), Is.EqualTo(-487), "Diacritics later than numbers");
        Assert.That(a.CompareTo(dollar), Is.EqualTo(29), "Keyboard characters before Latin");
    }


    [Test]
    public void FixLinks()
    {
        Article a = new Article("a", @"A [[_B ]]");
        a.FixLinks(false);
        ClassicAssert.IsTrue(a.ArticleText.Contains("[[B]]"));

        a = new Article("a", @"A [[ B_]]");
        a.FixLinks(true);
        ClassicAssert.IsTrue(a.ArticleText.Contains("[[B]]"));

        a = new Article("a", @"A [[B]]");
        a.FixLinks(true);
        ClassicAssert.IsTrue(a.ArticleText.Contains("[[B]]"));

        a = new Article("a", @"A [[B]]");
        a.FixLinks(false);
        ClassicAssert.IsTrue(a.ArticleText.Contains("[[B]]"));
    }

    [Test]
    public void CiteTemplateDates()
    {
        Parsers Parser = new Parsers();
        Article a = new Article("a", @"A [[B]]");
        a.CiteTemplateDates(Parser, true);
        ClassicAssert.IsTrue(a.ArticleText.Contains("[[B]]"));
    }
}
