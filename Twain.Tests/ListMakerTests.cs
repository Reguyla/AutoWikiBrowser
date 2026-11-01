using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Twain.Core;
using Twain.Core.Controls.Lists;

namespace Twain.Tests;

[TestFixture]
public class ListMakerTests : RequiresInitialization
{
    [Test]
    public void NormalizeTitle()
    {
        ListMaker LMaker = new();

        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&diff=3&oldid=4"), Is.EqualTo("Foo"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&diff=3&curid=4"), Is.EqualTo("Foo"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Health_effects_of_chocolate&diff=4018&oldid=40182"), Is.EqualTo("Health effects of chocolate"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&action=history"), Is.EqualTo("Foo"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&action=edit"), Is.EqualTo("Foo"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&oldid=5"), Is.EqualTo("Foo"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Science%20%28journal%29&action=history"), Is.EqualTo("Science (journal)"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/Sandbox&action=edit"), Is.EqualTo(@"Wikipedia:AutoWikiBrowser/Sandbox"));
        Assert.That(LMaker.NormalizeTitle(@"en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/Sandbox&action=edit"), Is.EqualTo(@"Wikipedia:AutoWikiBrowser/Sandbox"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo#References"), Is.EqualTo("Foo"));

        Assert.That(LMaker.NormalizeTitle(@"   Foo"), Is.EqualTo("Foo"), "cleans spacing from Firefox Category paste");

        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&pe=1&#Date_on"), Is.EqualTo(@"Foo"));
        Assert.That(LMaker.NormalizeTitle(@"Foo‎"), Is.EqualTo(@"Foo"), "title has left-to-right mark at the end");

        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&offset=20130214190000&action=history"), Is.EqualTo("Foo"), "cleans spacing from Firefox Category paste");
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/?title=Foo"), Is.EqualTo("Foo"));
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/wiki/Foo"), Is.EqualTo("Foo"));
        Assert.That(LMaker.NormalizeTitle(@"http://en.wikipedia.org/wiki/Foo"), Is.EqualTo("Foo"), "HTTP not HTTPS support");
        Assert.That(LMaker.NormalizeTitle(@"//en.wikipedia.org/w/index.php?title=Foo&action=history"), Is.EqualTo("Foo"), "Protocol-relative support");
        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&redirect=no"), Is.EqualTo("Foo"));

        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&type=revision&diff=75558108&oldid=65316243"), Is.EqualTo("Foo"));

        Assert.That(LMaker.NormalizeTitle(@"#[[Foo]]"), Is.EqualTo("#[[Foo]]"), "#wikilinked");
        Assert.That(LMaker.NormalizeTitle(@"Foo#bar"), Is.EqualTo("Foo"), "#wikilinked");

        Assert.That(LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Sandeep+Nayak&diff=prev&oldid=1320026009"), Is.EqualTo("Sandeep Nayak"));
    }

    [Test]
    public void NormalizeTitleSecure()
    {
        ListMaker LMaker = new();

        Assert.That(
            LMaker.NormalizeTitle(@"https://en.wikipedia.org/w/index.php?title=Foo&diff=3&oldid=4"),
            Is.EqualTo("Foo"));
    }

    [Test]
    public void RemoveListDuplicatesSimple()
    {
        ListMaker LMakerRLD = new();

        LMakerRLD.Add(new Article("A"));
        LMakerRLD.Add(new Article("B"));
        LMakerRLD.Add(new Article("C"));
        LMakerRLD.Add(new Article("A"));

        LMakerRLD.RemoveListDuplicates();

        Assert.That(LMakerRLD.Count, Is.EqualTo(3), "Duplicate removed");

        foreach (Article a in LMakerRLD)
        {
            Assert.That(a.ToString(), Is.EqualTo("A"), "Duplicates removed from end of list");
            break;
        }
    }

    [Test]
    public void RemoveListDuplicates10K()
    {
        const int big = 10000;

        ListMaker LMakerLarge = new();
        LMakerLarge.Clear();

        for (int i = 1; i < big; i++)
            LMakerLarge.Add(new Article(i.ToString()));

        LMakerLarge.Add(new Article("1"));

        Assert.That(LMakerLarge.Count, Is.EqualTo(big));

        LMakerLarge.RemoveListDuplicates();

        Assert.That(LMakerLarge.Count, Is.EqualTo(big - 1), "Duplicate removed");

        ClassicAssert.IsTrue(
            LMakerLarge.Contains(new Article("1")),
            "First instance of article retained");
    }

    [Test]
    public void FilterNonMainArticlesVolume()
    {
        const int big = 500;

        ListMaker LMakerLarge = new();
        LMakerLarge.Clear();

        for (int i = 1; i < big; i++)
            LMakerLarge.Add(i.ToString());

        LMakerLarge.Add("Talk:Me");

        LMakerLarge.FilterNonMainArticles();

        Assert.That(
            LMakerLarge.Count,
            Is.EqualTo(big - 1),
            "Non-mainspace article removed");
    }

    [Test]
    public void FilterNonMainArticles()
    {
        ListMaker LMaker = new();

        LMaker.Add("One");
        LMaker.Add("Two");
        LMaker.Add("Talk:Three");
        LMaker.Add("Four");
        LMaker.Add("Talk:Five");
        LMaker.Add("Talk:Five2");
        LMaker.Add("Six");

        LMaker.FilterNonMainArticles();

        Assert.That(LMaker.NumberOfArticles, Is.EqualTo(4));
    }

    [Test]
    public void AddList()
    {
        ListMaker LMaker = new();
        List<Article> l = new();

        l.Add(new Article("A"));
        l.Add(new Article("B"));

        LMaker.Add("A");
        LMaker.Add("B");

        LMaker.FilterDuplicates = true;

        LMaker.Add(l);

        Assert.That(LMaker.NumberOfArticles, Is.EqualTo(2));

        l.Add(new Article("C"));
        LMaker.Add(l);

        Assert.That(LMaker.NumberOfArticles, Is.EqualTo(3));

        l.Add(new Article("C"));
        l.Add(new Article("C"));
        l.Add(new Article("C"));
        l.Add(new Article("C"));

        LMaker.Add("D");
        LMaker.Add(l);

        Assert.That(LMaker.NumberOfArticles, Is.EqualTo(4));
    }

    [Test]
    public void GetArticleList()
    {
        ListMaker listMaker = new();

        listMaker.Add("A");
        listMaker.Add("B");

        Assert.That(listMaker.GetArticleList().Count, Is.EqualTo(2));
    }
}