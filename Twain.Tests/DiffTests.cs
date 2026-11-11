using NUnit.Framework;
using Twain.Core;

namespace Twain.Tests;

[TestFixture]
public class DiffTests
{
    private static void AssertWords(IList<Word> words, params string[] expected)
    {
        Assert.That(words.Count, Is.EqualTo(expected.Length));
        for (int i = 0; i < expected.Length; i++)
            Assert.That(words[i].ToString(), Is.EqualTo(expected[i]));
    }

    [Test]
    public void WordTests()
    {
        Word w1 = new Word("", "");
        Word w2 = new Word("", " ");

        Assert.That(w1.TheWord, Is.Empty);
        Assert.That(w1.Whitespace, Is.Empty);
        Assert.That(w1.ToString(), Is.Empty);
        Assert.That(w2.TheWord, Is.Empty);
        Assert.That(w2.Whitespace, Is.EqualTo(" "));
        Assert.That(w2.ToString(), Is.EqualTo(" "));
        Assert.That(w2, Is.EqualTo(w1));
        Assert.That(w2.GetHashCode(), Is.EqualTo(w1.GetHashCode()));

        w1 = new Word("foo", "  ");
        w2 = new Word("foo", "");
        Assert.That(w1.TheWord, Is.EqualTo("foo"));
        Assert.That(w1.ToString(), Is.EqualTo("foo  "));
        Assert.That(w2.ToString(), Is.EqualTo("foo"));
        Assert.That(w2, Is.EqualTo(w1));
        Assert.That(w2.GetHashCode(), Is.EqualTo(w1.GetHashCode()));

        w2 = new Word("Foo", "");
        Assert.That(w1, Is.Not.EqualTo(w2));
        Assert.That(w1.GetHashCode(), Is.Not.EqualTo(w2.GetHashCode()));
    }

    [Test]
    public void SplitString()
    {
        Assert.That(Word.SplitString(""), Is.Empty);
        AssertWords(Word.SplitString(" "), " ");
        AssertWords(Word.SplitString("   "), "   ");

        List<Word> lst = Word.SplitString("foo");
        Assert.That(lst.Count, Is.EqualTo(1));
        Assert.That(lst, Is.EqualTo(Word.SplitString("foo ")).AsCollection);

        lst = Word.SplitString("foo bar");
        AssertWords(lst, "foo ", "bar");
        Assert.That(lst, Is.EqualTo(Word.SplitString("foo    bar ")).AsCollection);
        Assert.That(lst, Is.Not.EqualTo(Word.SplitString(" foo bar")).AsCollection);

        lst = Word.SplitString("foo.bar");
        AssertWords(lst, "foo", ".", "bar");
        lst = Word.SplitString("foo..bar");
        AssertWords(lst, "foo", ".", ".", "bar");
        lst = Word.SplitString("{{foo}}");
        AssertWords(lst, "{", "{", "foo", "}", "}");
        lst = Word.SplitString("foo|bar");
        AssertWords(lst, "foo", "|", "bar");
        lst = Word.SplitString("foé bar");
        AssertWords(lst, "foé ", "bar");

        // Chinese and Thai characters are tokenized individually.
        AssertWords(Word.SplitString("汉语"), "汉", "语");
        AssertWords(Word.SplitString("ภา"), "ภ", "า");
    }

    [Test]
    public void UndoDeletion()
    {
        WikiDiff d = new WikiDiff();
        d.GetDiff("\nA1\nA2", "A1\nA2", 2);
        Assert.That(d.UndoDeletion(0, 0), Is.EqualTo("\nA1\nA2"),
            "Undo of delete first blank line");

        d = new WikiDiff();
        d.GetDiff("A1\nA2\n", "A1\nA2", 2);
        Assert.That(d.UndoDeletion(2, 2), Is.EqualTo("A1\nA2\n"),
            "Undo of delete last blank line");
    }

    private static string RenderDiff(string original, string modified)
    {
        return new WikiDiff().GetDiff(original, modified, 2);
    }

    private static void AssertModernCallbacks(string html)
    {
        Assert.That(html, Does.Not.Contain("window.external"),
            "Diff HTML must not use the legacy Internet Explorer bridge");
    }

    [Test]
    public void IdenticalTextProducesNoDiff()
    {
        Assert.That(RenderDiff("A1\nA2", "A1\nA2"), Is.Empty);
    }

    [Test]
    public void ChangedTextShowsOriginalAndReplacement()
    {
        string html = RenderDiff("A1\nA2", "A1X\nA2");

        Assert.That(html, Is.Not.Empty);
        Assert.That(html, Does.Contain("diff-deletedline"));
        Assert.That(html, Does.Contain("diff-addedline"));
        Assert.That(html, Does.Contain("A1"));
        Assert.That(html, Does.Contain("A1X"));
        Assert.That(html, Does.Contain("A2"), "Unchanged context should remain visible");
        AssertModernCallbacks(html);
    }

    [Test]
    public void MultipleChangesAppearInDiff()
    {
        string html = RenderDiff("A1\nA2", "A1X\nA2X");

        Assert.That(html, Does.Contain("A1X"));
        Assert.That(html, Does.Contain("A2X"));
        Assert.That(html, Does.Contain("diff-addedline"));
        Assert.That(html, Does.Contain("diff-deletedline"));
        AssertModernCallbacks(html);
    }

    [Test]
    public void DeletedLineIsMarkedAsDeleted()
    {
        string html = RenderDiff("A1\nA2", "A1");

        Assert.That(html, Does.Contain("diff-deletedline"));
        Assert.That(html, Does.Contain("A2"));
        AssertModernCallbacks(html);
    }

    [Test]
    public void AddedLineIsMarkedAsAdded()
    {
        string html = RenderDiff("A1", "A1\nA2");

        Assert.That(html, Does.Contain("diff-addedline"));
        Assert.That(html, Does.Contain("A2"));
        AssertModernCallbacks(html);
    }

    [Test]
    public void ChangedTextUsesCurrentNavigationCallbacks()
    {
        string html = RenderDiff("A1\nA2", "A1X\nA2");

        Assert.That(html, Does.Match(@"\bgoTo\(0\)"));
        Assert.That(html, Does.Match(@"\bundoChange\(0,\s*0\)"));
        AssertModernCallbacks(html);
    }

    [Test]
    public void HtmlSensitiveTextIsEncoded()
    {
        string html = RenderDiff("<tag> & old", "<tag> & new");

        Assert.That(html, Does.Not.Contain("<tag>"),
            "Article text must not be interpreted as an HTML element");
        Assert.That(html, Does.Contain("&lt;tag&gt;"));
        Assert.That(html, Does.Contain("&amp;"));
    }
}
