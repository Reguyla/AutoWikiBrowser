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

}
