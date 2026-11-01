using NUnit.Framework;
using NUnit.Framework.Legacy;
using Twain.Core;
using Twain.Core.Parse;
using Twain.Core.TalkPages;
using Twain.Tests.Articles;

namespace Twain.Tests;

[TestFixture]
public class TalkPageTests : RequiresInitialization
{
    private const string articleTextHeader = @"{{talkheader|noarchive=no}}
[[Category:Foo bar]]";

    [Test]
    public void RenameTalkHeader()
    {
        string talkheader = @"{{talkheader|noarchive=no}}", talkrest = @"==hello==
hello talk";
        string articleText = talkrest + "\r\n" + talkheader;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);

        Assert.That(articleText, Is.EqualTo(@"{{Talk header|noarchive=no}}" + "\r\n" + talkrest + "\r\n"), "renamed to upper case with space");
    }

    [Test]
    public void TalkHeaderDefaultsortChange()
    {
        string start = @"
==Foo==
bar", df = @"{{DEFAULTSORT:Bert}}";

        string articleText = start + "\r\n" + df;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.MoveToBottom);
        Assert.That(articleText, Is.EqualTo(start + "\r\n" + "\r\n" + df), "removes second newline");

        articleText = start + df;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.MoveToTop);
        Assert.That(articleText, Is.EqualTo(df + "\r\n" + start), "moves df after text");

        articleText = start + df;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(start + df), "no changes");

        string df2 = @"{{DEFAULTSORT:}}";

        articleText = start + df2;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.MoveToBottom);
        Assert.That(articleText, Is.EqualTo(start), "defaultsort with no key removed");

        articleText = start + df2;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.MoveToTop);
        Assert.That(articleText, Is.EqualTo(start), "defaultsort with no key removed");
    }

    [Test]
    public void SkipToTalk()
    {
        string articleText = @"{{Skiptotoc}}", stt = @"{{Skip to talk}}";

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(stt + "\r\n"));

        articleText = @"{{skiptotoctalk}}";

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(stt + "\r\n"));
    }

    [Test]
    public void AddMissingFirstCommentHeader()
    {
        const string comment = @"
Hello world comment.";
        string articleTextIn = articleTextHeader + comment;

        // plain comment
        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(articleTextHeader + "\r\n" + @"
==Untitled==
Hello world comment."));

        // idented comment2
        articleTextIn = articleTextHeader + @"
*Hello world comment2.";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(articleTextHeader + "\r\n" + @"
==Untitled==
*Hello world comment2."));

        // idented comment3
        articleTextIn = articleTextHeader + @"
:Hello world comment3.";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(articleTextHeader + "\r\n" + @"
==Untitled==
:Hello world comment3."));

        // quoted comment
        articleTextIn = articleTextHeader + @"
""Hello world comment4"".";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(articleTextHeader + "\r\n" + @"
==Untitled==
""Hello world comment4""."));

        // heading level 3 changed to level 2
        articleTextIn = articleTextHeader + "\r\n" + @"===Foo bar===
*Hello world comment2.";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        ClassicAssert.IsTrue(articleTextIn.Contains(@"==Foo bar=="));
        ClassicAssert.IsFalse(articleTextIn.Contains(@"==Untitled=="));

        // heading level 1 changed to level 2
        articleTextIn = articleTextHeader + "\r\n" + @"=Foo bar=
*Hello world comment2.";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        ClassicAssert.IsTrue(articleTextIn.Contains(@"==Foo bar=="));
        ClassicAssert.IsFalse(articleTextIn.Contains(@"==Untitled=="));

        articleTextIn = comment;

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"==Untitled==
Hello world comment."), "don't add blank line when header is on the top");

        articleTextIn = @"{{Football}}" + "\r\n" + comment;

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"{{Football}}

==Untitled==
Hello world comment."), "add header between template and text");
    }

    [Test]
    public void AddMissingFirstCommentHeaderLocalization()
    {
#if DEBUG
        Variables.SetProjectLangCode("zh");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=yes|blp=yes}}"), Is.EqualTo(@"{{WikiProject banner shell|blp=yes|blp=yes}}"));

        const string comment = @"
Hello world comment.";
        string articleTextIn = articleTextHeader + comment;

        // plain comment
        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(articleTextHeader + "\r\n" + @"
==無標題==
Hello world comment."));
        Variables.SetProjectLangCode("en");
#endif
    }

    [Test]
    public void AddMissingFirstCommentHeaderNoChanges()
    {
        // no change – header already
        string articleTextIn = articleTextHeader + @"
==Question==
:Hello world comment3.";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(articleTextHeader + @"
==Question==
:Hello world comment3."));

        // no change – already header at top
        articleTextIn = @"
{{Some template}}
==Question==
:Hello world comment3.";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"
{{Some template}}
==Question==
:Hello world comment3."));

        // no change – already header at top 2
        articleTextIn = @"
==Question==
{{Some template}}
:Hello world comment3.";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"
==Question==
{{Some template}}
:Hello world comment3."));

        // no change – no comments
        articleTextIn = @"
==Question==
{{Some template}}";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"
==Question==
{{Some template}}"));

        // no change – only text in template
        articleTextIn = @"
{{foo|
bar|
end}}";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"
{{foo|
bar|
end}}"));

        // no change – only comments
        articleTextIn = @"
<!--
foo
-->";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"
<!--
foo
-->"));

        // no change – only TOC
        articleTextIn = @"
__TOC__";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"
__TOC__"));

        // no change -- only in template
        const string allInTemplate = @"{{archive box|
*[[/Archive: GA review|Good Article review]]}}

== Explanation of Wright's work in ''Certaine Errors'' ==";

        articleTextIn = allInTemplate;

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(allInTemplate));

        // no change -- only after template on same line
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_15#Section_header_added_in_wrong_position
        const string allAfterTemplate = @"{{archive box|words}} extra foo

{{another one}}";

        articleTextIn = allAfterTemplate;

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(allAfterTemplate));

        // no change – only text in gallery tags
        articleTextIn = @"
<gallery>
File:Example.jpg|Caption1
File:Example.jpg|Caption2
</gallery>";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"
<gallery>
File:Example.jpg|Caption1
File:Example.jpg|Caption2
</gallery>"));

        // no change – don't pick up text within wiki table
        articleTextIn = @"
{|
|Foo
bar
|}";

        TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);

        Assert.That(articleTextIn, Is.EqualTo(@"
{|
|Foo
bar
|}"));
    }

    [Test]
    public void RemoveTemplateNamespace()
    {
        string T1 = @"{{Template:Foo}}", T2 = @"{{Template:Foo}}
==Section==
{{Template:Bar}}";

        ClassicAssert.IsFalse(TalkPageFixes.ProcessTalkPage(ref T1, DEFAULTSORT.NoChange));
        Assert.That(T1, Is.EqualTo("{{Foo}}"), "template namespace removed");

        ClassicAssert.IsFalse(TalkPageFixes.ProcessTalkPage(ref T2, DEFAULTSORT.NoChange));
        Assert.That(T2, Is.EqualTo(@"{{Foo}}
==Section==
{{Template:Bar}}"), "changes only made in zeroth section");
    }

}