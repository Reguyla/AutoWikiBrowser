using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Text.RegularExpressions;
using Twain.Core;
using Twain.Core.Controls.Lists;
using Twain.Core.Parse;
using Twain.Core.Processing;
using Twain.Core.ReplaceSpecial;
using Twain.Core.TalkPages;
using Twain.Tests.Parsing;

namespace Twain.Tests;

[TestFixture]
public class ConcurrencyTests : RequiresInitialization
{
    [Test]
    public void Parser()
    {
        Parsers p1 = new Parsers();
        Parsers p2 = new Parsers();

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_10#NullReferenceException_in_HideText.AddBackMore
        string s1 = p1.HideText("<pre>foo bar</pre>");
        string s2 = p2.HideText("<source>quux</source>");
        Assert.That(p1.AddBackText(s1), Is.EqualTo("<pre>foo bar</pre>"));
        Assert.That(p2.AddBackText(s2), Is.EqualTo("<source>quux</source>"));

        // in the future, we may use parser objects for processing several wikis at once
        // Assert.AreNotEqual(p1.StubMaxWordCount, p2.StubMaxWordCount);
    }
}

[TestFixture]
public class TalkHeaderTests : RequiresInitialization
{


    [TestFixture]
    public class InTemplateRuleTests
    {
        [Test]
        public void TemplateUsedInText()
        {
            // casing
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"bert}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("bert", @"Bert}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("bert", @"bert}} was great"));

            // spacing
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert }} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @" Bert}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert|fo=yes}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert
|fo=yes}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert |fo=yes}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @" Bert|fo=yes}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"
    Bert|fo=yes}} was great"));

            // comments
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert|fo=yes}} was <!--great-->"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"<!--thing--> Bert }} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert<!--thing-->}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert<!--thing-->|foo=bar}} was great"));

            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("", @"Bert}} was great"));

            // underscores
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert Li", @"Bert Li}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert Li", @"Bert   Li}} was great"));
            ClassicAssert.IsTrue(InTemplateRule.TemplateUsedInText("Bert Li", @"Bert_Li}} was great"));
        }

        [Test]
        public void TemplateUsedInTextNoMatches()
        {
            ClassicAssert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"{{Bert}} was great"));
            ClassicAssert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"Bert was great"));
            ClassicAssert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"[[Bert]] was great"));
            ClassicAssert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"Tim}} was great"));
            ClassicAssert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"BERT}} was great"));
            ClassicAssert.IsFalse(InTemplateRule.TemplateUsedInText("Bert Li", @"BertLi}} was great"));

            ClassicAssert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"<!--Bert}}--> was great"));
        }
    }

    [TestFixture]
    public class ListComparerTests : RequiresInitialization
    {
        [Test]
        public void ListComparerSimple()
        {
            ListMaker LMaker = new();
            LMaker.Add(new Article("A"));
            LMaker.Add(new Article("B"));
            LMaker.Add(new Article("C"));
            LMaker.Add(new Article("C")); // duplicate, removed during compare
            System.Windows.Forms.ListBox lb1 = new System.Windows.Forms.ListBox();
            System.Windows.Forms.ListBox lb2 = new System.Windows.Forms.ListBox();
            System.Windows.Forms.ListBox lb3 = new System.Windows.Forms.ListBox();

            List<Article> articles = new();
            articles.Add(new Article("A"));
            articles.Add(new Article("D"));
            articles.Add(new Article("E"));

            ListComparer.CompareLists(LMaker, articles, lb1, lb2, lb3);

            // unique in 1
            ClassicAssert.IsTrue(lb1.Items.Contains("B"));
            ClassicAssert.IsTrue(lb1.Items.Contains("C"));
            ClassicAssert.IsFalse(lb1.Items.Contains("A"));
            Assert.That(lb1.Items.Count, Is.EqualTo(2));

            // unique in 2
            ClassicAssert.IsFalse(lb2.Items.Contains("A"));
            ClassicAssert.IsTrue(lb2.Items.Contains("D"));
            ClassicAssert.IsTrue(lb2.Items.Contains("E"));
            Assert.That(lb2.Items.Count, Is.EqualTo(2));

            // common to both
            ClassicAssert.IsTrue(lb3.Items.Contains("A"));
            Assert.That(lb3.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void ListComparer10K()
        {
            const int big = 10000;
            ListMaker LMakerC10K = new();

            for (int i = 0; i < big; i++)
                LMakerC10K.Add(new Article(i.ToString()));

            LMakerC10K.Add(new Article("A"));
            LMakerC10K.Add(new Article("B"));

            System.Windows.Forms.ListBox lb1 = new System.Windows.Forms.ListBox();
            System.Windows.Forms.ListBox lb2 = new System.Windows.Forms.ListBox();
            System.Windows.Forms.ListBox lb3 = new System.Windows.Forms.ListBox();

            List<Article> articlesC = new();
            for (int i = 0; i < big; i++)
                articlesC.Add(new Article(i.ToString()));

            articlesC.Add(new Article("C"));
            articlesC.Add(new Article("D"));

            ListComparer.CompareLists(LMakerC10K, articlesC, lb1, lb2, lb3);

            // unique in 1
            ClassicAssert.IsTrue(lb1.Items.Contains("A"));
            ClassicAssert.IsTrue(lb1.Items.Contains("B"));

            // unique in 2
            ClassicAssert.IsTrue(lb2.Items.Contains("C"));
            ClassicAssert.IsTrue(lb2.Items.Contains("D"));

            // common to both
            ClassicAssert.IsTrue(lb3.Items.Contains("1"));
            Assert.That(lb3.Items.Count, Is.EqualTo(big));
        }
    }

    [TestFixture]
    public class ListBoxArticleTests : RequiresInitialization
    {
        [Test]
        public void RemoveSelected()
        {
            ListBoxArticle lbArticles = new();

            const int big = 70000, sel = 5000;

            // sequential block deletion performance
            for (int i = 0; i < big; i++)
                lbArticles.Items.Add(new Article(i.ToString()));

            for (int j = sel; j > 0; j--)
                lbArticles.SetSelected(big - j, true);

            lbArticles.RemoveSelected(true);

            if (Globals.UsingMono) // Mono implementation of SetSelected does not seem to honour second input to multi-select
                Assert.That(lbArticles.Items.Count, Is.EqualTo(big - 1));
            else
                Assert.That(lbArticles.Items.Count, Is.EqualTo(big - sel));

            // single selected item deletion performance
            lbArticles.Items.Clear();
            for (int i = 0; i < big * 3; i++)
                lbArticles.Items.Add(new Article(i.ToString()));
            lbArticles.SetSelected((int)big / 2, true);
            lbArticles.RemoveSelected(true);
            Assert.That(lbArticles.Items.Count, Is.EqualTo(big * 3 - 1));

            // non-sequential deletion
            lbArticles.Items.Clear();
            for (int i = 0; i < 10; i++)
                lbArticles.Items.Add(new Article(i.ToString()));
            lbArticles.SetSelected(1, true);
            lbArticles.SetSelected(3, true);
            lbArticles.RemoveSelected(true);
            if (Globals.UsingMono)
            {
                lbArticles.SetSelected(1, true);
                lbArticles.RemoveSelected(true);
            }
            ClassicAssert.IsFalse(lbArticles.Items.Contains(new Article("1")));
            ClassicAssert.IsFalse(lbArticles.Items.Contains(new Article("3")));
            ClassicAssert.IsTrue(lbArticles.Items.Contains(new Article("2")));

            lbArticles.Items.Clear();
            for (int i = 0; i < 10; i++)
                lbArticles.Items.Add(new Article(i.ToString()));
            lbArticles.SetSelected(1, true);
            lbArticles.SetSelected(5, true);
            lbArticles.RemoveSelected(true);
            if (Globals.UsingMono)
            {
                lbArticles.SetSelected(1, true);
                lbArticles.RemoveSelected(true);
            }

            ClassicAssert.IsFalse(lbArticles.Items.Contains(new Article("1")));
            ClassicAssert.IsFalse(lbArticles.Items.Contains(new Article("5")));
            ClassicAssert.IsTrue(lbArticles.Items.Contains(new Article("2")));

            lbArticles.Items.Clear();
            lbArticles.Items.Add(new Article("A"));
            lbArticles.Items.Add(new Article("A"));
            lbArticles.Items.Add(new Article("B"));
            lbArticles.SetSelected(2, true);
            lbArticles.RemoveSelected(false);
            Assert.That(lbArticles.Items.Count, Is.EqualTo(2), "Duplicates not removed if not in duplicate mode");

            lbArticles.Items.Clear();
            lbArticles.Items.Add(new Article("A"));
            lbArticles.Items.Add(new Article("B"));
            lbArticles.Items.Add(new Article("C"));
            lbArticles.SetSelected(0, true);
            lbArticles.SetSelected(1, true);
            lbArticles.SetSelected(2, true);
            lbArticles.RemoveSelected(false);

            // under mono list count is still 2 now, appears it implements RemoveSelected as for one item only not all selected
            // so set and remove to get down to count of zero under Mono too
            if (Globals.UsingMono)
            {
                lbArticles.SetSelected(0, true);
                lbArticles.RemoveSelected(false);
                lbArticles.SetSelected(0, true);
                lbArticles.RemoveSelected(false);
            }
            Assert.That(lbArticles.Items.Count, Is.EqualTo(0), "List cleared if all items selected and removed");
        }

        [Test]
        public void FindAndReplace()
        {
            Replacement r =
                new(
                    "foo",
                    "bar",
                    true,
                    true,
                    true,
                    true,
                    RegexOptions.None,
                    "");

            FindReplace.ProcessReplacementResult result =
                FindReplace.ProcessReplacement(
                    r,
                    "the was",
                    "Test",
                    null,
                    null,
                    Variables.LangCode,
                    Variables.RTL);

            Assert.That(
                result.Text,
                Is.EqualTo("the was"));

            ClassicAssert.IsFalse(
                result.ChangeMade);

            Assert.That(
                result.ReplacedSummary,
                Is.Null,
                "No match: no edit summary");

            result =
                FindReplace.ProcessReplacement(
                    r,
                    "the foo was",
                    "Test",
                    result.ReplacedSummary,
                    result.RemovedSummary,
                    Variables.LangCode,
                    Variables.RTL);

            Assert.That(
                result.Text,
                Is.EqualTo("the bar was"));

            ClassicAssert.IsTrue(
                result.ChangeMade);

            Assert.That(
                result.ReplacedSummary,
                Is.EqualTo("foo → bar"),
                "One match: a to b");

            result =
                FindReplace.ProcessReplacement(
                    r,
                    "the foo was or foo was",
                    "Test",
                    null,
                    null,
                    Variables.LangCode,
                    Variables.RTL);

            Assert.That(
                result.Text,
                Is.EqualTo("the bar was or bar was"));

            Assert.That(
                result.ReplacedSummary,
                Is.EqualTo("foo → bar (2)"),
                "Match count shown");

            result =
                FindReplace.ProcessReplacement(
                    r,
                    "the foo was or foo was foo",
                    "Test",
                    null,
                    null,
                    Variables.LangCode,
                    Variables.RTL);

            Assert.That(
                result.Text,
                Is.EqualTo("the bar was or bar was bar"));

            Assert.That(
                result.ReplacedSummary,
                Is.EqualTo("foo → bar (3)"),
                "Match count shown, 3");

            r =
                new Replacement(
                    "foot?",
                    "bar",
                    true,
                    true,
                    true,
                    true,
                    RegexOptions.None,
                    "");

            result =
                FindReplace.ProcessReplacement(
                    r,
                    "the foot was or foo was",
                    "Test",
                    null,
                    null,
                    Variables.LangCode,
                    Variables.RTL);

            Assert.That(
                result.Text,
                Is.EqualTo("the bar was or bar was"));

            ClassicAssert.IsTrue(
                result.ChangeMade);

            Assert.That(
                result.ReplacedSummary,
                Is.EqualTo("foot → bar (2)"),
                "Different matches, match text of first used");

            r =
                new Replacement(
                    "fooo?",
                    "foo",
                    true,
                    true,
                    true,
                    true,
                    RegexOptions.None,
                    "");

            result =
                FindReplace.ProcessReplacement(
                    r,
                    "the foo was a fooo it",
                    "Test",
                    null,
                    null,
                    Variables.LangCode,
                    Variables.RTL);

            Assert.That(
                result.Text,
                Is.EqualTo("the foo was a foo it"));

            ClassicAssert.IsTrue(
                result.ChangeMade);

            Assert.That(
                result.ReplacedSummary,
                Is.EqualTo("fooo → foo"),
                "No-change match ignored");

            result =
                FindReplace.ProcessReplacement(
                    r,
                    "the foo was",
                    "Test",
                    null,
                    null,
                    Variables.LangCode,
                    Variables.RTL);

            Assert.That(
                result.Text,
                Is.EqualTo("the foo was"));

            ClassicAssert.IsFalse(
                result.ChangeMade,
                "Only match is no-change on replace, so no change made");

            Assert.That(
                result.ReplacedSummary,
                Is.Null,
                "Only match is no-change match, no edit summary");
        }
    }

    [TestFixture]
    public class SubstTemplates
    {
        [Test]
        public void SubstTemplatesNoExpand()
        {
            Twain.Core.SubstTemplates st = new Twain.Core.SubstTemplates();
            st.ExpandRecursively = false;

            Assert.That(st.SubstituteTemplates("Now {{foo}}", "test"), Is.EqualTo("Now {{foo}}"), "no change when no templates in list");

            st.TemplateList = new[] { "foo", "bar" };

            Assert.That(st.NoOfRegexes, Is.EqualTo(2));

            Assert.That(st.SubstituteTemplates("Now {{foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "simple case");
            Assert.That(st.SubstituteTemplates("Now {{Foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "First letter casing handling");
            Assert.That(st.SubstituteTemplates("Now {{ foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "whitespace before");
            Assert.That(st.SubstituteTemplates("Now {{ foo }}", "test"), Is.EqualTo("Now {{subst:foo}}"), "whitespace after");
            Assert.That(st.SubstituteTemplates("Now {{foo|first=y}}", "test"), Is.EqualTo("Now {{subst:foo|first=y}}"), "template with parameters");
            Assert.That(st.SubstituteTemplates("Now {{foo|first}}", "test"), Is.EqualTo("Now {{subst:foo|first}}"), "template with arguments");
            Assert.That(st.SubstituteTemplates("Now {{foo}} {{bar}}", "test"), Is.EqualTo("Now {{subst:foo}} {{subst:bar}}"), "Multiple");

            Assert.That(st.SubstituteTemplates("Now {{Template:foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "Template prefix");
            Assert.That(st.SubstituteTemplates("Now {{template:foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "template prefix");
            Assert.That(st.SubstituteTemplates("Now {{template :foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "template spacing after");
            Assert.That(st.SubstituteTemplates("Now {{ template :foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "template spacing before");
            Assert.That(st.SubstituteTemplates("Now {{ template : foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "all whitespace");
            Assert.That(st.SubstituteTemplates("Now {{Msg:foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "Msg prefix");
            Assert.That(st.SubstituteTemplates("Now {{msg :foo}}", "test"), Is.EqualTo("Now {{subst:foo}}"), "Msg prefix spacing");

            st.TemplateList = new[] { "foo (bar)" };
            Assert.That(st.NoOfRegexes, Is.EqualTo(1));

            Assert.That(st.SubstituteTemplates("Now {{foo (bar)}}", "test"), Is.EqualTo("Now {{subst:foo (bar)}}"), "template name is escaped");
            Assert.That(st.SubstituteTemplates("Now {{Foo (bar)}}", "test"), Is.EqualTo("Now {{subst:foo (bar)}}"), "template name is escaped, casing handling");
        }
    }
}