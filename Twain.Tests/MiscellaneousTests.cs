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
public class HideTextTests : RequiresInitialization
{
    #region Helpers
    const string Hidden = @"⌊⌊⌊⌊M?\d+⌋⌋⌋⌋",
    AllHidden = @"^(⌊⌊⌊⌊M?\d+⌋⌋⌋⌋)*$";
    HideText Hider;

    private string HideMore(string text, bool hideExternalLinks, bool leaveMetaHeadings, bool hideImages)
    {
        Hider = new HideText(hideExternalLinks, leaveMetaHeadings, hideImages);
        string s = Hider.HideMore(text);
        Assert.That(Hider.AddBackMore(s), Is.EqualTo(text));
        return s;
    }

    private string HideMore(string text)
    {
        return HideMore(text, false);
    }

    private string HideMore(string text, bool hideOnlyTargetOfWikilink)
    {
        Hider = new HideText();
        string s = Hider.HideMore(text, hideOnlyTargetOfWikilink);
        Assert.That(Hider.AddBackMore(s), Is.EqualTo(text));
        return s;
    }

    private void AssertHiddenMore(string text)
    {
        RegexAssert.IsMatch(Hidden, HideMore(text));
    }

    private void AssertAllHiddenMore(string text)
    {
        RegexAssert.IsMatch(AllHidden, HideMore(text));
    }

    private void AssertAllHiddenMore(string text, bool hideExternalLinks)
    {
        RegexAssert.IsMatch(AllHidden, HideMore(text, hideExternalLinks, false, true));
    }

    private void AssertAllHiddenMore(string text, bool hideExternalLinks, string message)
    {
        RegexAssert.IsMatch(AllHidden, HideMore(text, hideExternalLinks, false, true), message);
    }

    private string Hide(string text)
    {
        return Hide(text, true, false, true);
    }

    private string Hide(string text, bool hideExternalLinks, bool leaveMetaHeadings, bool hideImages)
    {
        Hider = new HideText(hideExternalLinks, leaveMetaHeadings, hideImages);
        string s = Hider.Hide(text);
        Assert.That(Hider.AddBack(s), Is.EqualTo(text));
        return s;
    }

    private void AssertHidden(string text, bool hideExternalLinks, bool leaveMetaHeadings, bool hideImages)
    {
        RegexAssert.IsMatch(Hidden, Hide(text, hideExternalLinks, leaveMetaHeadings, hideImages));
    }

    private void AssertHidden(string text)
    {
        AssertHidden(text, true, false, true);
    }

    private void AssertAllHidden(string text)
    {
        RegexAssert.IsMatch(AllHidden, Hide(text));
    }

    private void AssertBothHidden(string text)
    {
        AssertBothHidden(text, true, false, true);
    }

    private void AssertBothHidden(string text, bool hideExternalLinks, bool leaveMetaHeadings, bool hideImages)
    {
        Hider = new HideText(hideExternalLinks, leaveMetaHeadings, hideImages);
        AssertAllHidden(text);
        AssertAllHiddenMore(text);
    }

    #endregion

    [Test]
    public void HideMore()
    {
        Hider = new HideText(true, false, true);

        string text = Hider.HideMore("[[foo]]", false, true);
        RegexAssert.IsMatch(AllHidden, text);
        text = Hider.AddBackMore(text);
        Assert.That(text, Is.EqualTo("[[foo]]"));
    }

    [Test]
    public void AcceptEmptyStrings()
    {
        Assert.That(Hide(""), Is.Empty);
        Assert.That(HideMore(""), Is.Empty);
    }

    [Test]
    public void HideLinks()
    {
        AssertHiddenMore("[[foo]]");
        AssertHiddenMore("[[foo|bar]]");
    }

    [Test]
    public void LeaveLinkFace()
    {
        Assert.That(HideMore("[[foo|bar]]", true), Does.Contain("bar"));
    }

    [Test]
    public void HideItalics()
    {
        Assert.That(HideMore("Now ''text'' was"), Does.Not.Contain("text"));
    }

    [Test]
    public void HideTemplates()
    {
        AssertAllHiddenMore("{{foo}}");
        AssertAllHiddenMore("{{{foo}}}");
        AssertAllHiddenMore("{{foo|}}");
        AssertAllHiddenMore("{{foo|bar}}");
        RegexAssert.IsMatch("123" + Hidden + "123", HideMore("123{{foo}}123"));
        AssertAllHiddenMore("{{foo|{{bar}}}}");
        AssertAllHiddenMore(@"{{foo|
abc={{bar}}
|def=hello}}");
        AssertAllHiddenMore("{{foo|{{bar|{{{1|}}}}}}}");
        AssertAllHiddenMore("{{foo|\r\nbar= {blah} blah}}");
        AssertAllHiddenMore("{{foo|\r\nbar= {blah} {{{1|{{blah}}}}}}}");

        RegexAssert.IsMatch("123{{" + Hidden + @"\|foo}}123", HideMore("123{{short description|foo}}123"));
    }

    [Test]
    public void HidePstyles()
    {
        AssertAllHiddenMore(@"<p style=""margin:0px;font-size:100%""><span style=""color:#00ff00"">▪</span> <small>Francophone minorities</small></p>");
        AssertAllHiddenMore(@"<p style=""font-family:monospace; line-height:130%"">hello</p>");
        AssertAllHiddenMore(@"<p style=""font-family:monospace; line-height:130%"">hello</P>");
        AssertAllHiddenMore(@"<p style=""font-family:monospace; line-height:130%"">hello
</P>");
    }

    const string Caption1 = @"|image_caption=London is a European Parliament constituency. It has water. |",
    Caption2 = @"|image_caption= some load of text here. Some more there.}}",
    Caption3 = @"|image_caption=some load of text here. Some more there.   }}",
    Caption4 = @"|image_caption= some load of text here. Some more there.|",
    Caption5 = @"|image_caption
            = some load of text here. Some more there.
            |",
    Field1 = @"field = value |";

    [Test]
    public void HideImages()
    {
        ClassicAssert.IsFalse(Hide(@"[[File:foo.jpg]]").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[File:foo with space and 0004.jpg]]").Contains("foo"), "with space");
        ClassicAssert.IsFalse(Hide(@"[[File:foo.jpeg]]").Contains("foo"), "jpeg");
        ClassicAssert.IsFalse(Hide(@"[[File:foo.JPEG]]").Contains("foo"), "JPEG");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo with space and 0004.jpeg]]").Contains("foo"), "space and jpeg");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo.jpeg]]").Contains("foo"), "Image jpeg");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo with space and 0004.jpg]]").Contains("foo"), "image jpeg space");
        ClassicAssert.IsFalse(Hide(@"[[File:foo.jpg|").Contains("foo"), "To pipe");
        ClassicAssert.IsFalse(Hide(@"[[File:foo with space and 0004.jpg|").Contains("foo"), "Space to pipe");
        ClassicAssert.IsFalse(Hide(@"[[File:foo.jpeg|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo with space and 0004.jpeg|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo.jpeg|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo with SPACE() and 0004.jpg|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[File:foo.gif|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo with space and 0004.gif|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo.gif|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo with SPACE() and 0004.gif|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[File:foo.png|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo with space and 0004.png|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo_here.png|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:foo with SPACE() and 0004.png|").Contains("foo"), "Standard case");
        ClassicAssert.IsFalse(Hide(@"[[Image:westminster.tube.station.jubilee.arp.jpg|").Contains("westminster.tube.station.jubilee.arp"), "Dot name");

        ClassicAssert.IsTrue(Hide(@"[[File:foo.jpg|thumb|140px|[[Jo]] Assistant [[Ge]]]]").StartsWith("[["), "Retain starting brackets");
        ClassicAssert.IsTrue(Hide(@"[[File:foo.jpg|thumb|140px|[[Jo]] Assistant [[Ge]]]]").Contains(@"thumb|140px|[[Jo]] Assistant [[Ge]]]]"), "Retain ending brackets");

        AssertAllHidden(@"<imagemap>
File:Blogs001.jpeg|Description
File:Blogs002.jpeg|Description
</imagemap>");

        ClassicAssert.IsFalse(HideMore(@"[[Category:Foo|abc]]", false).Contains("abc"), "Category sort key always hidden if hiding wikilinks and not leaving target");
        ClassicAssert.IsFalse(HideMore(@"[[Category:Foo|abc]]", true).Contains("abc"), "Category sort key hidden even if keeping targets");

        HideText h = new HideText(true, false, false);
        ClassicAssert.IsTrue(h.HideMore(@"[[Category:Foo|abc]]", false, false).Contains("abc"), "Category sort key kept if keeping wikilinks");
    }

    [Test]
    public void HideImagesLimits()
    {
        // in these ones all but the last | is hidden
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|image_skyline=442px_-_London_Lead_Image.jpg|"), Hidden + @"\|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|image_skyline=442px_-_London_Lead_Image.jpg <!--comm-->|"), Hidden + " <!--" + Hidden + @"-->\|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|image_map=London (European Parliament constituency).svg   |"), Hidden + @"   \|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|image_map=London (European Parliament constituency).webm   |"), Hidden + @"   \|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|image_map=westminster.tube.station.jubilee.arp.jpg|"), Hidden + @"\|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|Cover  = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|image = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|
image = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|Img = BBC_logo1.jpg
|"), Hidden + @"
\|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"| image name = Fred Astaire.jpg |"), Hidden + @" \|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|image2 = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|map = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"|AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
    }

    [Test]
    public void HideCiteTitles()
    {
        ClassicAssert.IsTrue(Hide(@"{{cite web| title = foo | date = May 2011").Contains(@"foo"), "Title not hidden if no year");
        ClassicAssert.IsFalse(Hide(@"{{cite web| title = Now September 20 - September 26, 2010 was | date = May 2011").Contains(@"September"), "title hidden when contains year");
        ClassicAssert.IsTrue(Tools.GetTemplateParameterValue(Hide(@"{{cite web| title = September 20 - September 26, 2010 | date = May 2011"), "title").Length > 0, "title parameter has length if hidden");

        ClassicAssert.IsTrue(Hide(@"{{cite web| title = [Now September 20 - September 26, 2010] was | date = May 2011").Contains(@"]"), "title hiding, bracket handling");

        ClassicAssert.IsTrue(Hide(@"{{cite web| trans_title = foo | date = May 2011").Contains(@"foo"), "trans_title not hidden if no year");
        ClassicAssert.IsFalse(Hide(@"{{cite web| trans_title = Now September 20 - September 26, 2010 was | date = May 2011").Contains(@"September"), "trans_title hidden when contains year");
        ClassicAssert.IsTrue(Tools.GetTemplateParameterValue(Hide(@"{{cite web| trans_title = September 20 - September 26, 2010 | date = May 2011"), "trans_title").Length > 0, "trans_title parameter has length if hidden");

        Assert.That(Hide(@"{{cite web | url = www.site.com/a.pdf }}"), Is.EqualTo(@"{{cite web | url = www.site.com/a.pdf }}"), "PDF www URL not hidden");

        ClassicAssert.IsFalse(Hide(@"{{cite web| trans_title = Now September 2009 - September 26, 2010 was | date = May 2011").Contains(@"2010"), "2 years");
        ClassicAssert.IsTrue(Hide(@"{{cite web| trans_title = September 26, 2010 was (in three volumes} | date = May 2011").Contains(@"(in three volumes}"), "Don't hide beyond year");
    }

    [Test]
    public void HideNotATypo()
    {
        AssertAllHidden(@"{{not a typo|foo}}");
        AssertAllHidden(@"{{Not a typo|The pEneLOpe[s]}}");
        AssertAllHidden(@"{{typo|foo}}");
        AssertAllHidden(@"{{proper name|foo}}");
        AssertAllHidden(@"{{as written|foo}}");
    }

    [Test]
    public void HideImagesPartial()
    {
        // in tests below no text is hidden
        Assert.That(Hide(Caption1), Is.EqualTo(Caption1));
        Assert.That(Hide(Caption2), Is.EqualTo(Caption2));
        Assert.That(Hide(Caption3), Is.EqualTo(Caption3));
        Assert.That(Hide(Caption4), Is.EqualTo(Caption4));
        Assert.That(Hide(Caption5), Is.EqualTo(Caption5));

        // in tests below part of string is hidden
        ClassicAssert.IsTrue(Hide(@"[[Image:some image name.JPG|thumb|words with typos]]").EndsWith(@"thumb|words with typos]]"));
        ClassicAssert.IsTrue(Hide(@"[[Image:some image name.JPEG|words with typos]]").EndsWith(@"words with typos]]"));
        ClassicAssert.IsTrue(Hide(@"[[Image:some image name.jpg|thumb|words with typos ]]").EndsWith(@"thumb|words with typos ]]"));
        ClassicAssert.IsTrue(Hide(@"[[Image:some image name.png|20px|words with typos and [[links]] here]]").EndsWith(@"20px|words with typos and [[links]] here]]"));
        ClassicAssert.IsTrue(Hide(@"[[Image:some image name.svg|thumb|words with typos ]]").EndsWith(@"thumb|words with typos ]]"));
        ClassicAssert.IsTrue(Hide(@"[[Image:some_image_name.further words.tiff|thumb|words with typos<ref name=a/>]]").EndsWith(@"thumb|words with typos<ref name=a/>]]"));
        ClassicAssert.IsTrue(Hide(@"[[Image:some_image_name.for a word.png|thumb|words with typo]]").EndsWith(@"thumb|words with typo]]"));
        ClassicAssert.IsTrue(Hide(@"[[Image:some_image_name.png|thumb|words with typo]] Normal words in text").EndsWith(@"thumb|words with typo]] Normal words in text"));
        ClassicAssert.IsTrue(Hide(@"[[Image:some_image_name.png]] Normal words in text").EndsWith(@" Normal words in text"));
        ClassicAssert.IsTrue(Hide(Caption4 + Field1).EndsWith(Field1));
    }

    [Test]
    public void HideImagesMisc()
    {
        ClassicAssert.IsFalse(HideMore(@"{{Drugbox|
   |IUPAC_name = 6-chloro-1,1-dioxo-2''H''-1,2,4-benzothiadiazine-7-sulfonamide
   | image=Chlorothiazide.svg
   | image2=Chlorothiazide-from-xtal-3D-balls.png
   | CAS_number=58-94-6}}").Contains("=Chlorothiazide"));

        AssertAllHidden(@"{{ gallery
|title=Lamu images
|width=150
|lines=2
|Image:LamuFort.jpg|Lamu Fort
|Image:LAMU Riyadha Mosque.jpg|Riyadha Mosque
|Image:04 Donkey Hospital (June 30 2001).jpg|Donkey Sanctuary
}}");
    }

    [Test]
    public void HideImagesMore()
    {
        AssertAllHiddenMore(@"[[File:foo.jpg]]");
        AssertAllHiddenMore(@"[[File:foo with space and 0004.jpg]]");
        AssertAllHiddenMore(@"[[File:foo.jpeg]]");
        AssertAllHiddenMore(@"[[File:foo.JPEG]]");
        AssertAllHiddenMore(@"[[Image:foo with space and 0004.jpeg]]");
        AssertAllHiddenMore(@"[[Image:foo.jpeg]]");
        AssertAllHiddenMore(@"[[Image:foo with space and 0004.jpg]]");
        AssertAllHiddenMore(@"[[File:foo.jpg|");
        AssertAllHiddenMore(@"[[File:foo with space and 0004.jpg|");
        AssertAllHiddenMore(@"[[File:foo.jpeg|");
        AssertAllHiddenMore(@"[[Image:foo with space and 0004.jpeg|");
        AssertAllHiddenMore(@"[[Image:foo.jpeg|");
        AssertAllHiddenMore(@"[[Image:foo with SPACE() and 0004.jpg|");
        AssertAllHiddenMore(@"[[File:foo.gif|");
        AssertAllHiddenMore(@"[[Image:foo with space and 0004.gif|");
        AssertAllHiddenMore(@"[[Image:foo.gif|");
        AssertAllHiddenMore(@"[[Image:foo with SPACE() and 0004.gif|");
        AssertAllHiddenMore(@"[[File:foo.png|");
        AssertAllHiddenMore(@"[[Image:foo with space and 0004.png|");
        AssertAllHiddenMore(@"[[Image:foo_here.png|");
        AssertAllHiddenMore(@"[[Image:foo with SPACE() and 0004.png|");
        AssertAllHiddenMore(@"[[Image:westminster.tube.station.jubilee.arp.jpg|");

        // in tests below no text is hidden

        Assert.That(HideMore(Caption1), Is.EqualTo(Caption1));
        Assert.That(HideMore(Caption2), Is.EqualTo(Caption2));
        Assert.That(HideMore(Caption3), Is.EqualTo(Caption3));
        Assert.That(HideMore(Caption4), Is.EqualTo(Caption4));
        Assert.That(HideMore(Caption5), Is.EqualTo(Caption5));

        // in tests below part of string is hidden
        ClassicAssert.IsTrue(HideMore(@"[[Image:some_image_name.png]] Normal words in text").EndsWith(@" Normal words in text"));
        ClassicAssert.IsTrue(HideMore(Caption4 + Field1).EndsWith(Field1));

        ClassicAssert.IsFalse(HideMore(@"{{Drugbox|
   |IUPAC_name = 6-chloro-1,1-dioxo-2''H''-1,2,4-benzothiadiazine-7-sulfonamide
   | image=Chlorothiazide.svg
   | image2=Chlorothiazide-from-xtal-3D-balls.png
   | CAS_number=58-94-6}}").Contains("=Chlorothiazide"));

        AssertAllHiddenMore(@"<imagemap>
File:Blogs001.jpeg|Description
File:Blogs002.jpeg|Description
</imagemap>");
    }

    [Test]
    public void HideGalleries()
    {
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
Image:quux.JPEG|text
</gallery>"), @"<gallery>
" + Hidden + @"|text
</gallery>"), "Simple case: one image link with namespace hidden, description retained");
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
quux.JPEG|text
</gallery>"), @"<gallery>
" + Hidden + @"|text
</gallery>"), "One image link without namespace hidden, description retained");
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
quux.JPEG|
</gallery>"), @"<gallery>
" + Hidden + @"|
</gallery>"), "One image link without namespace hidden, blank description");
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
quux.JPEG
</gallery>"), @"<gallery>
" + Hidden + @"
</gallery>"), "One image link without namespace hidden, no description ");
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
Image:foo.png|a bar
Image:quux.JPEG|text
</gallery>"), @"<gallery>
" + Hidden + @"|a bar
" + Hidden + @"|text
</gallery>"), "Multiple image links hidden");

        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery param=""a"">
Image:quux.svg|text
</gallery>"), @"<gallery param=""a"">
" + Hidden + @"|text
</gallery>"), "Images hidden within gallery tags with parameters");
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<Gallery>
Image:foo.png|a bar
Image:quux.JPEG|text
</gallery>"), @"<Gallery>
" + Hidden + @"|a bar
" + Hidden + @"|text
</gallery>"), "gallery tag casing");

        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
Image:foo
Image:quux.JPEG|text
</gallery>"), @"<gallery>
Image:foo
" + Hidden + @"|text
</gallery>"), "Image link without file extension not hidden");
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
File:foo.png
File:9th of quux.JPEG|text
</gallery>"), @"<gallery>
" + Hidden + @"
" + Hidden + @"|text
</gallery>"), "image without description hidden OK");
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
File:bar ă foo.jpg|text1
File:9th of May, ățuux.JPEG|text2
</gallery>"), @"<gallery>
" + Hidden + @"|text1
" + Hidden + @"|text2
</gallery>"), "special characters in image name handled OK");
        ClassicAssert.IsTrue(Regex.IsMatch(HideMore(@"<gallery>
File:foo1.jpg|[[foobar]] barbar
File:foo2.jpg|Winter Festival
Image:Foo3.jpg|Bar, detail
File:9th of June street , Bacău.JPG|[[Romanian War of Independence#Overview|9th of May]] Street
</gallery>"), @"<gallery>
" + Hidden + "|" + Hidden + @" barbar
" + Hidden + @"|Winter Festival
" + Hidden + @"|Bar, detail
" + Hidden + "|" + Hidden + @" Street
</gallery>"), "Wiki formatting within image description handled OK");

        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"<gallery>
quux.JPEG|text
</gallery>"), @"<gallery>
" + Hidden + @"|text
</gallery>"), "One image link without namespace hidden, description retained");
    }

    [Test]
    public void HideExternalLinks()
    {
        AssertAllHiddenMore("[http://foo]", true, "External link no text");
        AssertAllHiddenMore("[http://foo bar]", true, "External link with text");
        AssertAllHiddenMore("[//foo.com bar]", true, "External link Protocol-relative URL");
        AssertAllHiddenMore("[http://foo [bar]", true, "External link unbalanced bracket");
        AssertAllHiddenMore("[[ru:Link]]", true, "Possible interwiki, hidemore");
        ClassicAssert.IsFalse(Hide(@"[[ru:Link]]", true, true, true).Contains("Link"), "Possible interwiki, hide");

        ClassicAssert.IsTrue(Hide(@"date=April 2010|url=http://w/010111a.html}}", true, true, true).Contains(@"date=April 2010|url="), "cite template URL");
        ClassicAssert.IsTrue(Hide(@"date=April 2010|url=//w/010111a.html}}", true, true, true).Contains(@"date=April 2010|url="), "Protocol-relative URL");
        ClassicAssert.IsTrue(Hide(@"date=April 2010|url=http://w/010111a.html}}", true, true, true).Contains(@"}}"), "Cite template URL, }} retention");
    }

    [Test]
    public void HidePlainExternalLinks()
    {
        AssertAllHiddenMore("http://foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("https://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("//www.foo.com/asdfasdf/asdf.htm", true); // Protocol-relative URL
        AssertAllHiddenMore("ftp://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("mailto://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("irc://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("gopher://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("telnet://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("nntp://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("worldwind://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("news://www.foo.com/asdfasdf/asdf.htm", true);
        AssertAllHiddenMore("svn://www.foo.com/asdfasdf/asdf.htm", true);

        ClassicAssert.IsFalse(HideMore(@"http://foo.com/asdfasdf/asdf.htm", true, false, false).Contains("asdf"));
        ClassicAssert.IsTrue(HideMore(@"http://foo.com/asdfasdf/asdf.htm", false, false, false).Contains("asdf"));
    }

    [Test]
    public void HideWikiLinksOnlyPlusWord()
    {
        AssertAllHiddenMore("[[Foo]]bar");
        AssertAllHiddenMore("[[Foo|test]]bar");
    }

    [Test]
    public void SimpleHide()
    {
        AssertHidden("<source>foo</source>");
        AssertHidden(@"<source  lang=""foo_bar"">\nfoo\n</source>");
        AssertAllHidden("<source>\r\nfoo\r\n</source><source foo>bar</source>");
        AssertHidden(@"<syntaxhighlight  lang=""foo_bar"">\nfoo\n</syntaxhighlight>");
        AssertHidden(@"<syntaxhighlight  lang=""javascript"">\nfoo\n</syntaxhighlight>");
        AssertAllHidden("<syntaxhighlight>\r\nfoo\r\n</source><source foo>bar</syntaxhighlight>");
        AssertHidden("<tt>foo</tt>");

        AssertHidden("<pre>foo\r\nbar</pre>");
        AssertHidden("<pre style=quux>foo\r\nbar</pre>");
        AssertHidden("<math>foo\r\nbar</math>");
        AssertHidden("<chem>foo\r\nbar</chem>");
        AssertHidden("<math chem>foo\r\nbar</math>");
        AssertHidden("{{math|foo\r\nbar}}");
        AssertHidden("{{code|foo\r\nbar}}");
        AssertHidden("<timeline>foo\r\nbar</timeline>");
        AssertHidden("<timeline foo=bar>foo\r\nbar</timeline>");
        AssertHidden("<nowiki>foo\r\nbar</nowiki>");

        AssertHidden("<!--foo-->");
        AssertHidden("<!--foo\r\nbar-->");
        ClassicAssert.IsTrue(Regex.IsMatch(Hide(@"<!--foo-->"), "<!--" + Hidden + @"-->"));

        // hideExternalLinks
        AssertHidden("[http://foo]");
        AssertHidden("[http://foo bar]");
        AssertHidden("https://bar");
        Assert.That(Hide("http://foo", false, false, true), Is.EqualTo("http://foo"));

        // leaveMetaHeadings
        AssertHidden("<!--Categories -->");
        AssertHidden("<!--foo-->", true, true, true);
        Assert.That(Hide("<!-- categories-->", true, true, true), Is.EqualTo("<!-- categories-->"));

        // hideImages
        AssertHidden("[[Image:foo.JPG]]");
        Assert.That(Hide("[[Image:foo.jpg]]", true, false, false), Is.EqualTo("[[Image:foo.jpg]]"));
        AssertHidden("[[File:foo.JPG]]");
        Assert.That(Hide("[[File:foo.jpg]]", true, false, false), Is.EqualTo("[[File:foo.jpg]]"));

        // <hiero> tags
        AssertAllHiddenMore(@"<hiero>foo</hiero>");
    }
}

[TestFixture]
public class ArticleTests : RequiresInitialization
{

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

[TestFixture]
public class NamespaceTests : RequiresInitialization
{
    [Test]
    public void Determine()
    {
        Assert.That(Namespace.Determine("test"), Is.EqualTo(0));
        Assert.That(Namespace.Determine(" test "), Is.EqualTo(0));
        Assert.That(Namespace.Determine(":test"), Is.EqualTo(0));
        Assert.That(Namespace.Determine("test:test"), Is.EqualTo(0));
        Assert.That(Namespace.Determine("My Project:Foo"), Is.EqualTo(0));
        Assert.That(Namespace.Determine("Magic: The Gathering"), Is.EqualTo(0));

        Assert.That(Namespace.Determine("User:"), Is.EqualTo(Namespace.User));

        Assert.That(Namespace.Determine("Talk:foo"), Is.EqualTo(Namespace.Talk));
        Assert.That(Namespace.Determine("Talk%3Afoo"), Is.EqualTo(Namespace.Talk), "Handles URL encoded colon");
        Assert.That(Namespace.Determine("User talk:bar"), Is.EqualTo(Namespace.UserTalk));

        Assert.That(Namespace.Determine("File:foo"), Is.EqualTo(Namespace.File));
        Assert.That(Namespace.Determine("Image:foo"), Is.EqualTo(Namespace.File));

        Assert.That(Namespace.Determine("Wikipedia:Foo"), Is.EqualTo(Namespace.Project));
        Assert.That(Namespace.Determine("Project:Foo"), Is.EqualTo(Namespace.Project));

        Assert.That(Namespace.Determine(@"Talk:ʿAyn"), Is.EqualTo(Namespace.Talk), "handles pages with spacing modifier Unicode characters at start of name");
        Assert.That(Namespace.Determine("Module:foo"), Is.EqualTo(Namespace.Module));
        Assert.That(Namespace.Determine("Module talk:foo"), Is.EqualTo(Namespace.ModuleTalk));

        Assert.That(Namespace.Determine("MOS:foo"), Is.EqualTo(Namespace.MOS));
        Assert.That(Namespace.Determine("MOS talk:foo"), Is.EqualTo(Namespace.MOSTalk));
        Assert.That(Namespace.Determine("TimedText:foo"), Is.EqualTo(Namespace.TimedText));
        Assert.That(Namespace.Determine("TimedText talk:foo"), Is.EqualTo(Namespace.TimedTextTalk));
        Assert.That(Namespace.Determine("Draft:foo"), Is.EqualTo(Namespace.Draft));
        Assert.That(Namespace.Determine("Draft talk:foo"), Is.EqualTo(Namespace.DraftTalk));
    }

    [Test]
    public void DetermineDeviations()
    {
        Assert.That(Namespace.Determine("File : foo"), Is.EqualTo(Namespace.File));
        Assert.That(Namespace.Determine("File :foo"), Is.EqualTo(Namespace.File));
        Assert.That(Namespace.Determine("File: foo"), Is.EqualTo(Namespace.File));
        Assert.That(Namespace.Determine("File :  foo"), Is.EqualTo(Namespace.File));
        Assert.That(Namespace.Determine("user:foo"), Is.EqualTo(Namespace.User));
        Assert.That(Namespace.Determine("user_talk:foo"), Is.EqualTo(Namespace.UserTalk));
        Assert.That(Namespace.Determine("user%20talk:foo"), Is.EqualTo(Namespace.UserTalk));
    }

    [Test]
    public void OtherLanguages()
    {
#if DEBUG
        Variables.SetProjectLangCode("fr");
        Variables.Namespaces[1] = "Discussion:";
        Assert.That(Namespace.Determine(@"Discussion:foo"), Is.EqualTo(Namespace.Talk));
        Variables.SetProjectLangCode("en");
        Variables.Namespaces[1] = "Talk:";
#endif
    }

    [Test]
    public void IsTalk()
    {
        ClassicAssert.IsTrue(Namespace.IsTalk(Namespace.Talk));
        ClassicAssert.IsTrue(Namespace.IsTalk(Namespace.UserTalk));

        ClassicAssert.IsFalse(Namespace.IsTalk(Namespace.User));
        ClassicAssert.IsFalse(Namespace.IsTalk(Namespace.Project));

        ClassicAssert.IsFalse(Namespace.IsTalk(Namespace.Special));
        ClassicAssert.IsFalse(Namespace.IsTalk(Namespace.Media));

        ClassicAssert.IsTrue(Namespace.IsTalk("Talk:Test"));
        ClassicAssert.IsTrue(Namespace.IsTalk("User talk:Test"));

        ClassicAssert.IsFalse(Namespace.IsTalk("Test"));
        ClassicAssert.IsFalse(Namespace.IsTalk("User:Test"));
    }

    [Test]
    public void IsMainSpace()
    {
        ClassicAssert.IsTrue(Namespace.IsMainSpace(Namespace.Article));
        ClassicAssert.IsFalse(Namespace.IsMainSpace(Namespace.Talk));

        ClassicAssert.IsTrue(Namespace.IsMainSpace("Test"));

        ClassicAssert.IsFalse(Namespace.IsMainSpace("User:"));
        ClassicAssert.IsFalse(Namespace.IsMainSpace("Talk:Test"));
        ClassicAssert.IsFalse(Namespace.IsMainSpace("User:Test"));
        ClassicAssert.IsFalse(Namespace.IsMainSpace("User talk:Test"));

        ClassicAssert.IsFalse(Namespace.IsMainSpace("File:Test"));
        ClassicAssert.IsFalse(Namespace.IsMainSpace("Image:Test"));
    }

    [Test]
    public void IsImportant()
    {
        ClassicAssert.IsTrue(Namespace.IsImportant("Test"));
        ClassicAssert.IsTrue(Namespace.IsImportant("File:Test.jpg"));
        ClassicAssert.IsTrue(Namespace.IsImportant("Template:Test"));
        ClassicAssert.IsTrue(Namespace.IsImportant("Category:Test"));

        ClassicAssert.IsFalse(Namespace.IsImportant("Talk:Test"));
        ClassicAssert.IsFalse(Namespace.IsImportant("File talk:Test"));
        ClassicAssert.IsFalse(Namespace.IsImportant("Template talk:Test"));
        ClassicAssert.IsFalse(Namespace.IsImportant("Category talk:Test"));
        ClassicAssert.IsFalse(Namespace.IsImportant("User:Test"));
    }

    [Test]
    public void IsUserSpace()
    {
        ClassicAssert.IsTrue(Namespace.IsUserSpace("User:Test"));
        ClassicAssert.IsTrue(Namespace.IsUserSpace("User talk:Test"));

        ClassicAssert.IsFalse(Namespace.IsUserSpace("User"));
        ClassicAssert.IsFalse(Namespace.IsUserSpace("Test"));
        ClassicAssert.IsFalse(Namespace.IsUserSpace("Project:User"));
    }

    [Test]
    public void IsUserTalk()
    {
        ClassicAssert.IsTrue(Namespace.IsUserTalk("User talk:Test"));

        ClassicAssert.IsFalse(Namespace.IsUserTalk("User:Test"));
        ClassicAssert.IsFalse(Namespace.IsUserTalk("Test"));
        ClassicAssert.IsFalse(Namespace.IsUserTalk("Project:User"));
    }

    [Test]
    public void IsUserPage()
    {
        ClassicAssert.IsTrue(Namespace.IsUserPage("User:Test"));

        ClassicAssert.IsFalse(Namespace.IsUserPage("User talk:Test"));
        ClassicAssert.IsFalse(Namespace.IsUserPage("Test"));
        ClassicAssert.IsFalse(Namespace.IsUserPage("Project:User"));
    }

    [Test]
    public void IsSpecial()
    {
        ClassicAssert.IsTrue(Namespace.IsSpecial(Namespace.Media));
        ClassicAssert.IsTrue(Namespace.IsSpecial(Namespace.Special));

        ClassicAssert.IsFalse(Namespace.IsSpecial(Namespace.Article));
        ClassicAssert.IsFalse(Namespace.IsSpecial(Namespace.TemplateTalk));
    }

    [Test]
    public void NormalizeNamespace()
    {
        Assert.That(Namespace.Normalize("User:", 2), Is.EqualTo("User:"));
        Assert.That(Namespace.Normalize("user :", 2), Is.EqualTo("User:"));
        Assert.That(Namespace.Normalize("User_talk:", 3), Is.EqualTo("User talk:"));

        Assert.That(Namespace.Normalize("image:", 6), Is.EqualTo("Image:"));
        Assert.That(Namespace.Normalize("file:", 6), Is.EqualTo("File:"));
        Assert.That(Namespace.Normalize("image talk:", 7), Is.EqualTo("Image talk:"));

        Assert.That(Namespace.Normalize("user :", 7), Is.EqualTo("user:"), "only changes colon for incorrect namespace number");
        Assert.That(Namespace.Normalize("user:", 7), Is.EqualTo("user:"), "only changes colon for incorrect namespace number");
    }

    [Test]
    public void Verify()
    {
        ClassicAssert.IsTrue(Namespace.VerifyNamespaces(Variables.CanonicalNamespaces));

        var ns = new Dictionary<int, string>(Variables.CanonicalNamespaces);

        ns[0] = string.Empty;
        ClassicAssert.IsFalse(Namespace.VerifyNamespaces(ns));

        ns.Remove(0);
        ns[1] = "Talk";
        ClassicAssert.IsFalse(Namespace.VerifyNamespaces(ns));

        ns.Remove(1);
        ClassicAssert.IsFalse(Namespace.VerifyNamespaces(ns));
    }
}

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