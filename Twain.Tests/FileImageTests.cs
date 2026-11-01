using NUnit.Framework;
using NUnit.Framework.Legacy;
using Twain.Core.Parse;

namespace Twain.Tests;

[TestFixture]
public class FileImageTests : RequiresInitialization
{
    [Test, Category("Incomplete")]
    public void BasicImprovements()
    {
        Assert.That(Parsers.FixImages("[[ file : foo.jpg|thumb|200px|Bar]]"),
                        Is.EqualTo("[[File:foo.jpg|thumb|200px|Bar]]"));

        Assert.That(Parsers.FixImages("[[ image : foo.jpg|thumb|200px|Bar]]"),
                        Is.EqualTo("[[Image:foo.jpg|thumb|200px|Bar]]"));

        // apostrophe handling
        Assert.That(Parsers.FixImages(@"[[Image:foo%27s.jpg|thumb|200px|Bar]]"), Is.EqualTo(@"[[Image:foo's.jpg|thumb|200px|Bar]]"));

        const string doubleApos = @"[[Image:foo%27%27s.jpg|thumb|200px|Bar]]";
        Assert.That(Parsers.FixImages(doubleApos), Is.EqualTo(doubleApos));

        // TODO: decide if such improvements really belong here
        // Assert.AreEqual("[[Media:foo]]",
        // Parsers.FixImages("[[ media : foo]]"));

        // Assert.AreEqual("[[:Media:foo]]",
        // Parsers.FixImages("[[ : media : foo]]"));

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_6#URL_underscore_regression
        Assert.That(Parsers.FixImages("[[File:foo|thumb]] # [http://a_b c] [[link]]"),
                        Is.EqualTo("[[File:foo|thumb]] # [http://a_b c] [[link]]"));

        Assert.That(Parsers.FixImages("[[Image:foo|thumb]] # [http://a_b c] [[link]]"),
                        Is.EqualTo("[[Image:foo|thumb]] # [http://a_b c] [[link]]"));

        // no changes should be made to this one
        const string Diamminesilver = @"[[Image:Diamminesilver(I)-3D-balls.png|thumb|right|200px|Ball-and-stick model of the diamminesilver(I) cation, [Ag(NH<sub>3</sub>)<sub>2</sub>]<sup>+</sup>]]";
        Assert.That(Parsers.FixImages(Diamminesilver), Is.EqualTo(Diamminesilver));

        const string a = @"[[Image:End CEST Transparent.png|thumb|left|120px|
alt=Diagram of a clock showing a transition from 3:00 to 2:00.|
When DST ends in central Europe, clocks retreat from 03:00 CEST to 02:00 CET. Other regions switch at different times.]]";

        Assert.That(Parsers.FixSyntax(a), Is.EqualTo(a));
    }

    [Test]
    public void Removal()
    {
        bool noChange;

        Assert.That(Parsers.RemoveImage("Foo.jpg", "[[Image:Foo.jpg]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("Foo.jpg", "[[:Image:Foo.jpg]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("foo.jpg", "[[Image: foo.jpg]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("Foo, bar", "[[File:foo%2C_bar|quux]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("Foo%2C_bar", "[[File:foo, bar|quux]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("foo.jpg", "[[Media:foo.jpg]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("foo.jpg", "[[:media : foo.jpg]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("foo", "{{infobox|image=foo}}", false, "", out noChange),
                        Is.EqualTo("{{infobox|image=}}"));
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("foo", "{{infobox|image=foo|other=bar}}", false, "", out noChange),
                        Is.EqualTo("{{infobox|image=|other=bar}}"));
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("Foo, bar", "[[File:foo%2C_bar|quux [[here]] there]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage(@"Image:Bar.jpg", @"<gallery>
Image:foo.jpg
Image:Bar.jpg</gallery>", false, "", out noChange), Is.EqualTo(@"<gallery>
Image:foo.jpg
</gallery>"));

        Assert.That(Parsers.RemoveImage(@"Image:Bar.jpg", @"<gallery>
Image:foo.jpg
Image:Bar.jpg|Some text description
</gallery>", false, "", out noChange), Is.EqualTo(@"<gallery>
Image:foo.jpg

</gallery>"));

        Assert.That(Parsers.RemoveImage(@"Image:Bar.jpg", @"<gallery>
Image:foo.jpg
Image:Bar.jpg|Some text description
</gallery>", true, "x", out noChange), Is.EqualTo(@"<gallery>
Image:foo.jpg
<!-- x Image:Bar.jpg|Some text description -->
</gallery>"));

        Assert.That(Parsers.RemoveImage("FOO.jpg", "[[Media:foo.jpg]]", false, "", out noChange), Is.EqualTo("[[Media:foo.jpg]]"), "image name is case sensitive");
        ClassicAssert.IsTrue(noChange);

        Assert.That(Parsers.RemoveImage("", "[[Media:foo.jpg]]", false, "", out noChange), Is.EqualTo("[[Media:foo.jpg]]"), "no change when blank image name input");
        ClassicAssert.IsTrue(noChange);
    }

    [Test]
    public void RemovalMultipleLines()
    {
        bool noChange;

        Assert.That(Parsers.RemoveImage("Foo, bar", @"[[File:foo%2C_bar|quux [[here]]
there]]", false, "", out noChange), Is.Empty);
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.RemoveImage("Foo, bar", @"[[File:foo%2C_bar|quux [[here]] there]] [[now]]", false, "", out noChange), Is.EqualTo(" [[now]]"));
        ClassicAssert.IsFalse(noChange);
        Assert.That(Parsers.RemoveImage("Foo, bar", @"[[File:foo%2C_bar|quux there]] [[now]]", false, "", out noChange), Is.EqualTo(" [[now]]"));
        ClassicAssert.IsFalse(noChange);
    }

    [Test]
    public void Replacement()
    {
        bool noChange;

        // just in case...
        Assert.That(Parsers.ReplaceImage("", "", "", out noChange), Is.Empty);
        ClassicAssert.IsTrue(noChange);

        Assert.That(Parsers.ReplaceImage("foo", "bar", "[[File:Foo]]", out noChange), Is.EqualTo("[[File:bar]]"));
        ClassicAssert.IsFalse(noChange);

        // preserve namespace
        Assert.That(Parsers.ReplaceImage("foo", "bar", "[[image:Foo]]", out noChange), Is.EqualTo("[[Image:bar]]"));
        ClassicAssert.IsFalse(noChange);

        // pipes, non-canonical NS casing
        Assert.That(Parsers.ReplaceImage("Foo%2C_bar", "bar", "[[FIle:foo, bar|boz!|666px]]", out noChange),
                        Is.EqualTo("[[File:bar|boz!|666px]]"));
        ClassicAssert.IsFalse(noChange);

        Assert.That(Parsers.ReplaceImage("foo", "bar", "[[Media:foo]]", out noChange), Is.EqualTo("[[Media:bar]]"));
        ClassicAssert.IsFalse(noChange);

        // normalising Media: is not yet supported, see TODO in BasicImprovements()
        Assert.That(Parsers.ReplaceImage("foo", "bar", "[[:media : foo]]", out noChange), Is.EqualTo("[[:media:bar]]"));
        ClassicAssert.IsFalse(noChange);
    }
}