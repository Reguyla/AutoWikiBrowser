using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Text.RegularExpressions;
using Twain.Core;
using Twain.Core.Parse;
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
