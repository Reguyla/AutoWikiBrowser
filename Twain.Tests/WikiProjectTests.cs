using NUnit.Framework;
using Twain.Core;
using Twain.Core.TalkPages;

namespace Twain.Tests;

[TestFixture]
public class WikiProjectTests : RequiresInitialization
{
    private string ArticleText = string.Empty;

    private void TalkGenFixes()
    {
        TalkPageFixes.ProcessTalkPage(
            ref ArticleText,
            DEFAULTSORT.NoChange);
    }

    [Test]
    public void WikiProjectBannerShellRedirects()
    {
        string red1 = @"{{WPBS}}", red2 = @"{{Wikiprojectbannershell}}", red3 = @"{{wpb}}", red4 = @"{{WPB}}",

        WikiProjectBannerShell = @"{{WikiProject banner shell}}",
        WikiProjectBanners = @"{{WikiProjectBanners}}";

        Assert.That(TalkPageFixes.WikiProjectBannerShell(red1), Is.EqualTo(WikiProjectBannerShell));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(red2), Is.EqualTo(WikiProjectBannerShell));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(WikiProjectBannerShell), Is.EqualTo(WikiProjectBannerShell));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(red3), Is.EqualTo(WikiProjectBannerShell));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(red4), Is.EqualTo(WikiProjectBannerShell));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(WikiProjectBanners), Is.EqualTo(WikiProjectBannerShell));
    }

    [Test]
    public void WikiProjectBannerShellEnOnly()
    {
#if DEBUG
        Variables.SetProjectLangCode("fr");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=yes|blp=yes}}"), Is.EqualTo(@"{{WikiProject banner shell|blp=yes|blp=yes}}"));
        Variables.SetProjectLangCode("en");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=yes|blp=yes}}"), Is.EqualTo(@"{{WikiProject banner shell|blp=yes}}"));
#endif
    }

    [Test]
    public void WikiProjectBannerShellDupeParameters()
    {
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=yes|blp=yes}}"), Is.EqualTo(@"{{WikiProject banner shell|blp=yes}}"));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blpo=yes|blpo=yes|blp=yes|blp=yes}}"), Is.EqualTo(@"{{WikiProject banner shell|blpo=yes|blp=yes}}"));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|collapsed=yes|collapsed=yes|collapsed=yes}}"), Is.EqualTo(@"{{WikiProject banner shell|collapsed=yes}}"));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell||collapsed=yes}}"), Is.EqualTo(@"{{WikiProject banner shell|collapsed=yes}}"), "excess pipes cleaned");
    }

    [Test]
    public void WikiProjectBannerShellUnneededParams()
    {
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=no|collapsed=no|blpo=no}}"), Is.EqualTo(@"{{WikiProject banner shell|blp=no}}"), "Retain blp=no");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=no|collapsed=yes|blpo=no}}"), Is.EqualTo(@"{{WikiProject banner shell|blp=no|collapsed=yes}}"), "Retain blp=no");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBanners|collapsed=no|blpo=no}}"), Is.EqualTo(@"{{WikiProject banner shell}}"));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBanners|collapsed=yes|blpo=no}}"), Is.EqualTo(@"{{WikiProject banner shell|collapsed=yes}}"));
    }

    [Test]
    public void WikiProjectBannerShellWPBiography()
    {
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|1={{WPBiography|foo=bar}}}}"), Is.EqualTo(@"{{WikiProject banner shell|1={{WPBiography|foo=bar}}}}"));

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=|1={{WPBiography|foo=bar|living=yes}}}}"), Is.EqualTo(@"{{WikiProject banner shell|blp=yes|1={{WPBiography|foo=bar|living=yes}}}}"), "appends blp=yes to WPBS");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=|1={{WikiProject Biography|foo=bar|living=yes}}}}"), Is.EqualTo(@"{{WikiProject banner shell|blp=yes|1={{WikiProject Biography|foo=bar|living=yes}}}}"), "appends blp=yes to WPBS");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blpo=|1={{WPBiography|foo=bar|blpo=yes}}}}"), Is.EqualTo(@"{{WikiProject banner shell|blpo=yes|1={{WPBiography|foo=bar|blpo=yes}}}}"), "appends blpo=yes to WPBS");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blpo=|1={{WPBiography|foo=bar|blpo=no}}}}"), Is.EqualTo(@"{{WikiProject banner shell|blpo=|1={{WPBiography|foo=bar|blpo=no}}}}"));

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|blp=yes|1={{WPBiography|foo=bar|living=no}}}}"), Is.EqualTo(@"{{WikiProject banner shell|1={{WPBiography|foo=bar|living=no}}}}"));
    }

    [Test]
    public void WikiProjectBannerShellAddingWikiProjects()
    {
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|1={{WPBiography|foo=bar}}}}
{{WikiProject foo}}"), Is.EqualTo(@"{{WikiProject banner shell|1={{WPBiography|foo=bar}}
{{WikiProject foo}}}}
"), "WikiProjects pulled into WPBS");

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|1={{WPBiography|foo=bar}}}}
{{WikiProject foo}}
{{WikiProject bar}}"), Is.EqualTo(@"{{WikiProject banner shell|1={{WPBiography|foo=bar}}
{{WikiProject foo}}
{{WikiProject bar}}}}

"), "WikiProjects pulled into WPBS");

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject Biography|living=yes|foo=bar}}{{WikiProject banner shell|1={{WikiProject foo}}
{{WikiProject bar}}}}"), Is.EqualTo(@"{{WikiProject banner shell|1={{WikiProject foo}}
{{WikiProject bar}}
{{WikiProject Biography|living=yes|foo=bar}} | blp=yes}}"), "WikiProjects pulled into WPBS, WPBIO contains living=yes");

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject bar}}

{{WikiProject banner shell|1={{WPBiography|foo=bar}}}}
{{WikiProject foo}}"), Is.EqualTo(@"{{WikiProject banner shell|1={{WPBiography|foo=bar}}
{{WikiProject bar}}
{{WikiProject foo}}}}
"), "WikiProjects pulled into WPBS, no excess whitespace left");

        const string VitalArticle = @"{{WikiProject banner shell|1={{WPBiography|foo=bar}}
{{WikiProject foo}}
{{Vital article|level=4|topic=Biology|class=FA}}}}
";

        Assert.That(TalkPageFixes.WikiProjectBannerShell(VitalArticle), Is.EqualTo(VitalArticle), "Support Vital article - already inside");

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{Vital article|level=4|topic=Biology|class=FA}}
{{WikiProject banner shell|1={{WPBiography|foo=bar}}
{{WikiProject foo}}}}
"), Is.EqualTo(VitalArticle), "Support Vital article - put inside");

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|{{WPBiography|foo=bar}}}}
{{WikiProject foo}}"), Is.EqualTo(@"{{WikiProject banner shell|{{WPBiography|foo=bar}}
{{WikiProject foo}}}}
"), "WikiProjects pulled into WPBS - new style WPBS without 1=");

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|class=A|{{WPBiography|foo=bar}}}}
{{WikiProject foo}}"), Is.EqualTo(@"{{WikiProject banner shell|class=A|{{WPBiography|foo=bar}}
{{WikiProject foo}}}}
"), "WikiProjects pulled into WPBS - new style WPBS with class");

        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|class=A}}
{{WikiProject foo}}
{{WikiProject bar}}"), Is.EqualTo(@"{{WikiProject banner shell|class=A|
{{WikiProject foo}}
{{WikiProject bar}}}}

"), "WikiProjects pulled into WPBS - new style WPBS with class, no existing WPB");
    }

    [Test]
    public void WikiProjectBannerShellUnnamedParam()
    {
        Assert.That(TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject banner shell|
{{WPBiography|foo=bar}}}}"), Is.EqualTo(@"{{WikiProject banner shell|
{{WPBiography|foo=bar}}}}"));

        const string otherUnnamed = @"{{WikiProject banner shell|random}}";
        Assert.That(TalkPageFixes.WikiProjectBannerShell(otherUnnamed), Is.EqualTo(otherUnnamed), "other unknown parameter not named 1=");
    }

    [Test]
    public void WikiProjectBannerShellBLP()
    {
        const string a = @"{{WikiProject banner shell|blp=yes|1={{WPBiography|foo=bar|living=yes}}}}";

        Assert.That(TalkPageFixes.WikiProjectBannerShell(a + "{{Blp}}"), Is.EqualTo(a), "removes redundant banner");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a.Replace("blp=yes", "blp=") + "{{Blp}}"), Is.EqualTo(a), "empty parameter in WPBS");
        Assert.That(TalkPageFixes.WikiProjectBannerShell("{{Blp}}"), Is.EqualTo("{{Blp}}"));
    }

    [Test]
    public void WikiProjectBannerShellBLPO()
    {
        const string a = @"{{WikiProject banner shell|blpo=yes|1={{WPBiography|foo=bar}}}}";

        Assert.That(TalkPageFixes.WikiProjectBannerShell(a + "{{Blpo}}"), Is.EqualTo(a), "removes redundant banner");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a + "{{BLPO}}"), Is.EqualTo(a), "removes redundant banner");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a + "{{BLP others}}"), Is.EqualTo(a), "removes redundant banner");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a.Replace("blpo=yes", "blpo=") + "{{Blpo}}"), Is.EqualTo(a), "empty parameter in WPBS");
        Assert.That(TalkPageFixes.WikiProjectBannerShell("{{Blpo}}"), Is.EqualTo("{{Blpo}}"));
    }

    [Test]
    public void WikiProjectBannerShellMisc()
    {
        const string a = @"{{wpbs|banner collapsed=no|
{{WPBiography|living=yes|class=Start|priority=|listas=Hill, A}}
{{WikiProject Gender Studies}}
{{WikiProject Oklahoma}}
}}", b = @"{{WikiProject banner shell|banner collapsed=no|
{{WPBiography|living=yes|class=Start|priority=|listas=Hill, A}}
{{WikiProject Gender Studies}}
{{WikiProject Oklahoma}}
| blp=yes
}}";
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a), Is.EqualTo(b));
    }

    [Test]
    public void AddWikiProjectBannerShell()
    {
        const string a = @"{{WikiProject a|text}}", b = @"{{WikiProject b|text}}", c = @"{{WikiProject c|text}}", d = @"{{WikiProject d|text}}";
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a), Is.EqualTo(a));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a + b), Is.EqualTo(a + b));
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a + b + c), Is.EqualTo(@"{{WikiProject banner shell|1=" + "\r\n" + a + "\r\n" + b + "\r\n" + c + "\r\n" + @"}}"), "banner shell added for 3 or more wikiproject links");
        Assert.That(TalkPageFixes.WikiProjectBannerShell(a + b + c + d), Is.EqualTo(@"{{WikiProject banner shell|1=" + "\r\n" + a + "\r\n" + b + "\r\n" + c + "\r\n" + d + "\r\n" + @"}}"));

        const string e = @"{{talk header}}
{{WikiProject Biography|listas=Bar, F|living=yes|class=Start|priority=mid|sports-work-group=yes}}
{{WikiProject F|class=Start|importance=mid|italy=y}}
{{WikiProject X|class=Start|importance=Mid}}
{{WikiProject D|class=Start|importance=Low}}", f = @"{{WikiProject banner shell|1=
{{WikiProject Biography|listas=Bar, F|living=yes|class=Start|priority=mid|sports-work-group=yes}}
{{WikiProject F|class=Start|importance=mid|italy=y}}
{{WikiProject X|class=Start|importance=Mid}}
{{WikiProject D|class=Start|importance=Low}}
| blp=yes
}}{{talk header}}

";

        Assert.That(TalkPageFixes.WikiProjectBannerShell(e), Is.EqualTo(f), "adds WPBS, ignores non-wikiproject templates");
    }

    [Test]
    public void WPSongs()
    {
        string a = @"{{WPSongs}}";

        Assert.That(TalkPageFixes.WPSongs(a + @"{{sir}}"), Is.EqualTo(a.Replace(@"}}", " | needs-infobox=yes}}")), "Add sir to WPSongs");
        Assert.That(TalkPageFixes.WPSongs(a.Replace(@"}}", " | needs-infobox=}}" + @"{{sir}}")), Is.EqualTo(a.Replace(@"}}", " | needs-infobox=yes}}")), "Add sir to WPSongs with empty parameter");
        Assert.That(TalkPageFixes.WPSongs(a.Replace(@"}}", " | needs-infobox=}}" + @"{{Single infobox request}}")), Is.EqualTo(a.Replace(@"}}", " | needs-infobox=yes}}")), "Add Single infobox request to WPSongs with empty parameter");
        Assert.That(TalkPageFixes.WPSongs(a.Replace(@"}}", " | needs-infobox=yes}}") + @"{{sir}}"), Is.EqualTo(a.Replace(@"}}", " | needs-infobox=yes}}")), "Remove sir when WPSongs with needs-infobox=yes exists");
        Assert.That(TalkPageFixes.WPSongs(a), Is.EqualTo(a), "Do nothing");
        Assert.That(TalkPageFixes.WPSongs(a.Replace(@"}}", "|importance=yes}}")), Is.EqualTo(a), "Remove importance");
        Assert.That(TalkPageFixes.WPSongs(a.Replace(@"}}", "|needs-infobox=no}}")), Is.EqualTo(a), "Remove needs-infobox=no");
        Assert.That(TalkPageFixes.WPSongs(a.Replace(@"}}", "|importance=yes|needs-infobox=no}}")), Is.EqualTo(a), "Remove importance and needs-infobox=no");

        string b = @"{{WikiProject Songs}}";

        Assert.That(TalkPageFixes.WPSongs(b + @"{{sir}}"), Is.EqualTo(b.Replace(@"}}", " | needs-infobox=yes}}")), "Add sir to WPSongs");
        Assert.That(TalkPageFixes.WPSongs(b.Replace(@"}}", " | needs-infobox=}}" + @"{{sir}}")), Is.EqualTo(b.Replace(@"}}", " | needs-infobox=yes}}")), "Add sir to WPSongs with empty parameter");
        Assert.That(TalkPageFixes.WPSongs(b.Replace(@"}}", " | needs-infobox=}}" + @"{{Single infobox request}}")), Is.EqualTo(b.Replace(@"}}", " | needs-infobox=yes}}")), "Add Single infobox request to WPSongs with empty parameter");
        Assert.That(TalkPageFixes.WPSongs(b.Replace(@"}}", " | needs-infobox=yes}}") + @"{{sir}}"), Is.EqualTo(b.Replace(@"}}", " | needs-infobox=yes}}")), "Remove sir when WPSongs with needs-infobox=yes exists");
        Assert.That(TalkPageFixes.WPSongs(b), Is.EqualTo(b), "Do nothing");
    }

    [Test]
    public void WPJazz()
    {
        string a = @"{{WPJazz}}";

        Assert.That(TalkPageFixes.WPJazz(a + @"{{WPSongs}}"), Is.EqualTo(a.Replace(@"}}", " | song=yes}}{{WPSongs}}")), "Add song to WPJazz");
        Assert.That(TalkPageFixes.WPJazz(a + @"{{WPAlbums}}"), Is.EqualTo(a.Replace(@"}}", " | album=yes}}{{WPAlbums}}")), "Add album to WPJazz");
        Assert.That(TalkPageFixes.WPJazz(a.Replace(@"}}", " | song=}}" + @"{{WikiProject Songs}}")), Is.EqualTo(a.Replace(@"}}", " | song=yes}}{{WikiProject Songs}}")), "Add song to WPJazz with empty parameter");
        Assert.That(TalkPageFixes.WPJazz(a.Replace(@"}}", " | album=}}" + @"{{WikiProject Albums}}")), Is.EqualTo(a.Replace(@"}}", " | album=yes}}{{WikiProject Albums}}")), "Add album to WPJazz with empty parameter");
        Assert.That(TalkPageFixes.WPJazz(a), Is.EqualTo(a), "Do nothing");
        Assert.That(TalkPageFixes.WPJazz(a.Replace(@"}}", "|needs-infobox=no}}")), Is.EqualTo(a), "Remove needs-infobox=no");

        string b = @"{{WikiProject Jazz}}";

        Assert.That(TalkPageFixes.WPJazz(b + @"{{WPSongs}}"), Is.EqualTo(b.Replace(@"}}", " | song=yes}}{{WPSongs}}")), "Add song to WikiProject Jazz");
        Assert.That(TalkPageFixes.WPJazz(b + @"{{WPAlbums}}"), Is.EqualTo(b.Replace(@"}}", " | album=yes}}{{WPAlbums}}")), "Add album to WikiProject Jazz");
        Assert.That(TalkPageFixes.WPJazz(b.Replace(@"}}", " | song=}}" + @"{{WikiProject Songs}}")), Is.EqualTo(b.Replace(@"}}", " | song=yes}}{{WikiProject Songs}}")), "Add song to WPJAzz with empty parameter");
        Assert.That(TalkPageFixes.WPJazz(b), Is.EqualTo(b), "Do nothing");
        Assert.That(TalkPageFixes.WPJazz(b.Replace(@"}}", "|needs-infobox=no}}")), Is.EqualTo(b), "Remove needs-infobox=no");
    }

    [Test]
    public void WPBiographyTopBilling()
    {
        string a = @"{{WPBiography|foo=yes|living=yes}}
{{WikiProject London}}
", b = @"{{WikiProject banner shell|banner collapsed=no|1=
{{WPBiography|living=yes|class=Start|priority=|listas=Hill, A}}
{{WikiProject Gender Studies}}
{{WikiProject Oklahoma}}
| blp=yes
}}", c = @"{{WPBiography|foo=yes|living=no}}
{{WikiProject London}}";
        Assert.That(TalkPageFixes.WPBiography(@"{{WikiProject London}}
{{WPBiography|foo=yes|living=yes}}"), Is.EqualTo(a), "WPBiography moved above WikiProjects");
        Assert.That(TalkPageFixes.WPBiography(a), Is.EqualTo(a), "no change when WPBiography ahead of WikiProjects");
        Assert.That(TalkPageFixes.WPBiography(b), Is.EqualTo(b), "no change when WPBS present");
        Assert.That(TalkPageFixes.WPBiography(c), Is.EqualTo(c), "no change when not living");
        Assert.That(TalkPageFixes.WPBiography(c + @"{{blp}}"), Is.EqualTo(c + @"{{blp}}"), "no change when not living");

        Assert.That(TalkPageFixes.WPBiography(a + @"{{blp}}"), Is.EqualTo(a), "blp template removed when living=y");
    }

    [Test]
    public void MoveBannersAndSpacing()
    {
        string articleText = @"==Untitled==" + "\r\n" + @"some text";
        string articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if nothing is detected; starts with header");

        articleText = @"{{afd-merged-from|George Piggins Medal|George Piggins Medal|10 June 2012}}

{{User:WildBot/m04|sect={{User:WildBot/m03|1|History of rugby league#Professional rugby begins in Australia|schism}}|m04}}
{{ArticleHistory
|action1=FAC

|currentstatus=GA
|topic=Everydaylife}}
{{WikiProject banner shell|1=
{{WikiProject Rugby league|class=GA|importance=High}}
}}

== older entries ==
The";

        articleText2 = @"{{ArticleHistory
|action1=FAC

|currentstatus=GA
|topic=Everydaylife}}
{{afd-merged-from|George Piggins Medal|George Piggins Medal|10 June 2012}}
{{WikiProject banner shell|1=
{{WikiProject Rugby league|class=GA|importance=High}}
}}

{{User:WildBot/m04|sect={{User:WildBot/m03|1|History of rugby league#Professional rugby begins in Australia|schism}}|m04}}

== older entries ==
The";
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "unknown templates get below known ones");

        articleText = @"{{Talk header}}
{{WikiProject banner shell|1=
{{WikiProject Architecture|class=Start |importance=Low }}
{{WikiProject Virginia|class=Start |importance=Low }}
{{WikiProject National Register of Historic Places|class=start|importance=low}}
{{Image requested|in=Virginia}}
}}";
        articleText2 = @"{{Talk header}}
{{WikiProject banner shell|1=
{{WikiProject Architecture|class=Start |importance=Low }}
{{WikiProject Virginia|class=Start |importance=Low }}
{{WikiProject National Register of Historic Places|class=start|importance=low}}
}}
{{Image requested|in=Virginia}}
";
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "image requested is moved away from WPBS; at some point we could fix the whitespace inside WPBS");

        articleText = @"{{Talk header|search=yes}}

==Random header==
Some text";
        articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if nothing is detected; starts with talk header");

        articleText = @"{{Not a forum}}

==Random header==
Some text";
        articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if nothing is detected; starts with not a forum");

        articleText = @"{{Random template}}

==Random header==
Some text";
        articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if nothing is detected; starts with a random template");

        articleText = @"{{Random template}}
{{Yet another random template}}

==Random header==
Some text";
        articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if nothing is detected; starts with two random templates");

        articleText = @"{{Skip to talk}}
{{Talk header}}
{{Random template}}

==Random header==
Some text";
        articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if nothing is detected; starts with two skt, th and a random template");

        articleText = @"{{Skip to talk}}
{{Not a forum}}
{{Random template}}

==Random header==
Some text";
        articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if nothing is detected; starts with skt, naf and a random template");

        articleText = @"{{Not a forum}}
{{Skip to talk}}
{{Random template}}

==Random header==
Some text";
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "moves skip to talk on the top 1");

        articleText = @"{{Talk header}}
{{Skip to talk}}
{{Random template}}

==Random header==
Some text";
        articleText2 = @"{{Skip to talk}}
{{Talk header}}
{{Random template}}

==Random header==
Some text";
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "moves skip to talk on the top 2");

        articleText = @"{{Talk header}}
{{WikiProject banner shell|1=
{{WikiProject Iraq|class=C|importance=mid}}
{{WikiProject Crime|class=C|importance=low}}
{{WikiProject Terrorism|class=C|importance=low}}
}}
{{image requested}}

== Reliable sources ==";
        articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "No change, heading whitespace not affected");
    }

    [Test]
    public void MoveTalkHeader()
    {
        string talkheader = @"{{talk header|noarchive=no|search=no|arpol=no|wp=no|disclaimer=no|shortcut1|shortcut2}}", talkrest = @"==hello==
hello talk";
        string articleText = talkrest + "\r\n" + talkheader;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);

        Assert.That(articleText, Is.EqualTo(talkheader.Replace("{{talk", @"{{Talk") + "\r\n" + talkrest + "\r\n"), "move talk header");

        // handles {{talk header}} on same line as other template
        string WPBS = @"{{WikiProject banner shell|blp=yes|1=
{{OH-Project|class=B|importance=Low|nested=yes}}
{{WPBiography|living=yes|class=B|priority=Low|filmbio-work-group=yes|nested=yes|listas=Parker, Sarah Jessica}}
{{WikiProject Cincinnati|class=B|importance=mid|nested=yes}}
}}", rest = "\r\n\r\n" + @"==Song Jessie by Joshua Kadison==
In the article it says that above mentioned";
        articleText = WPBS + @"{{Talkheader}}" + rest;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);

        Assert.That(articleText, Is.EqualTo(@"{{Talk header}}" + "\r\n" + WPBS + rest), "talk header on same line as other template");

        // no change if already at top
        articleText = talkheader + "\r\n" + talkrest;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(talkheader + "\r\n" + talkrest), "talk header already on top");

        // no change if no talk header
        articleText = talkrest;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(talkrest), "no talk header");

        talkheader = @"{{Talk header}}";
        talkrest = @"==hello==
hello talk";
        articleText = talkrest + "\r\n" + talkheader + "\r\n{{GA nominee}}";

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);

        Assert.That(articleText, Is.EqualTo("{{GA nominee}}\r\n" + talkheader + "\r\n" + talkrest + "\r\n"), "GA nominee involved");
    }

    [Test]
    public void AddWikiProjectBannerShellThroughTalkGenFixes()
    {
        const string AllCommented = @"<!-- {{WikiProject a|text}}
{{WikiProject b|text}}
{{WikiProject c|text}
{{WikiProject d|text}} -->";

        ArticleText = AllCommented;
        TalkGenFixes();

        TestContext.Out.WriteLine("Expected:");
        TestContext.Out.WriteLine(AllCommented);

        TestContext.Out.WriteLine("Actual:");
        TestContext.Out.WriteLine(ArticleText);

        Assert.That(ArticleText, Is.EqualTo(AllCommented),
            "no WikiProject banner shell addition when templates all commented out");

        string a = @"{{Talk header}}
{{WikiProject banner shell|1=
{{WikiProject a |text}}
{{WikiProject b|text}}
{{WikiProject Biography|living=yes}}
{{WikiProject c|text}}
| blp=yes
}}
";

        ArticleText = @"{{Talk header}}
{{WikiProject a |text}}
{{WikiProject b|text}}
{{WikiProject Biography|living=yes}}
{{WikiProject c|text}}";

        TalkGenFixes();
        Assert.That(ArticleText, Is.EqualTo(a), "Adds WikiProject banner shell below talk header");

        a = @"{{Talk header}}
{{WikiProject banner shell|1=
{{WikiProject a |text}}
{{WikiProject d|text}}
{{WikiProject c|text}}
}}
";

        ArticleText = @"{{Talk header}}
{{WikiProject a |text}}
{{WikiProject d|text}}
{{WikiProject c|text}}";

        TalkGenFixes();
        Assert.That(ArticleText, Is.EqualTo(a), "Adds WikiProject banner shell when 3 wikiproject links");

        ArticleText = @"{{Talk header}}
{{wikiProject a |text}}
{{wikiProject d|text}}
{{wiki project c|text}}";

        TalkGenFixes();
        Assert.That(ArticleText, Is.EqualTo(a.Replace("WikiProject c", "wiki project c").Replace("WikiProject a", "wikiProject a").Replace("WikiProject d", "wikiProject d")), "Adds WikiProject banner shell when 3 wikiProject links, wikiproject name variations");
    }

    [Test]
    public void AddWikiProjectBannerShellWhitespace()
    {
        ArticleText = @"{{WikiProject Biography|living=yes}}
{{WikiProject a |text}}
{{WikiProject b|text}}
{{WikiProject c|text}}";

        string a = @"{{WikiProject banner shell|1=
{{WikiProject Biography|living=yes}}
{{WikiProject a |text}}
{{WikiProject b|text}}
{{WikiProject c|text}}
| blp=yes
}}
";

        TalkGenFixes();
        Assert.That(ArticleText, Is.EqualTo(a), "Adds WikiProject banner shell when 3 wikiproject links, cleans whitespace");

        ArticleText = @"{{WikiProject Biography|living=yes}}
{{WikiProject a |text}}
{{WikiProject b|text}}
{{WikiProject c|text}}

==First==
Word.

Second.";

        a = @"{{WikiProject banner shell|1=
{{WikiProject Biography|living=yes}}
{{WikiProject a |text}}
{{WikiProject b|text}}
{{WikiProject c|text}}
| blp=yes
}}

==First==
Word.

Second.";

        TalkGenFixes();
        Assert.That(ArticleText, Is.EqualTo(a), "Adds WikiProject banner shell when 3 wikiproject links, cleans whitespace");
    }

    [Test]
    public void MoveBanners()
    {
        string a = @"{{Skip to talk}}", b = @"{{Talk header}}", c = @"{{GA nominee}}", d = @"{{Controversial}}", e = @"{{Not a forum}}", f = @"{{British English}}", g = @"{{FailedGA}}";
        string correct = c + "\r\n" + a + "\r\n" + b + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + g + "\r\n";
        string articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefg with newlines");

        articleText = a + b + c + d + e + f + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefg");

        articleText = b + a + c + d + e + f + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefg without newlines");

        articleText = a + c + b + d + e + f + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "acbdefg");

        articleText = a + b + c + e + d + f + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcedfg");

        articleText = a + e + c + b + d + f + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "aecbdfg");

        articleText = a + e + c + b + d + g + f;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "aecbdgf");

        articleText = a + c + d + e + f + b + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "acdefbg");

        articleText = f + a + c + b + d + e + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "facbdeg");

        articleText = f + e + d + c + b + a + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "fedcbag");

        string h = @"{{WikiProject banner shell|1={{WikiProject Greece|class=}}}}", i = @"{{Image requested}}", j = @"{{Connected contributor|John Doe}}";

        correct = correct + h + "\r\n";
        articleText = correct;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefgh");

        correct = correct + i + "\r\n";

        articleText = correct;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefghi");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + h + "\r\n" + g + "\r\n" + i;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefhgi");

        correct = correct + j + "\r\n";

        articleText = correct;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefghij");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + h + "\r\n" + g + "\r\n" + j + "\r\n" + i;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefhgji");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + h + "\r\n" + g + "\r\n" + j + "\r\n" + i + "\r\n" + "\r\n" + "\r\n";
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefhgji with newlines at the end");

        articleText = b + "\r\n" + "\r\n" + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + h + "\r\n" + g + "\r\n" + j + "\r\n" + "\r\n" + "\r\n" + i + "\r\n" + "\r\n" + "\r\n";

        string k = @"{{To do}}", m = @"{{Find sources notice}}", n = @"{{Split from|foo}}";

        correct = correct + k + "\r\n";
        articleText = correct;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefghijk");

        string cop = correct + @"{{Copied}}" + "\r\n";
        articleText = cop;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(cop), "Templates including {{Copied}}");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + k + "\r\n" + g + "\r\n" + j + "\r\n" + i + "\r\n" + h;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefkgjih");

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefhgji with newlines at the end and in the middle");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + k + "\r\n" + g + "\r\n" + j + "\r\n" + i + "\r\n" + h;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefkgjlih");

        correct = correct + m + "\r\n" + n + "\r\n";
        articleText = correct;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefghijklmn");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + k + "\r\n" + n + "\r\n" + g + "\r\n" + j + "\r\n" + m + "\r\n" + i + "\r\n" + h;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefkngjmlih");

        articleText = @"{{some other template}}" + "\r\n" + @"==Untitled==" + "\r\n" + @"some text";
        string articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if nothing is detected; starts with template");

        articleText = @"{{GA|21:12, 11 May 2013 (UTC)|topic=Geography|page=1|oldid=554646767}}
{{WikiProject banner shell|1=
{{WikiProject Canada|geography=yes|class=Start|importance=Mid|bc=yes}}
{{WikiProject Protected areas|class=Start|importance=Mid}}
{{WikiProject Greece|class=Start}}
}}
{{DYK talk|17 April|2013|entry=... that Canada}}
{{some random template}}";
        articleText2 = @"{{GA|21:12, 11 May 2013 (UTC)|topic=Geography|page=1|oldid=554646767}}
{{DYK talk|17 April|2013|entry=... that Canada}}
{{WikiProject banner shell|1=
{{WikiProject Canada|geography=yes|class=Start|importance=Mid|bc=yes}}
{{WikiProject Protected areas|class=Start|importance=Mid}}
{{WikiProject Greece|class=Start}}
}}

{{some random template}}";
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "articleilestonene templates above wikiproject banners; contains WPBS");

        articleText = @"{{GA|21:12, 11 May 2013 (UTC)|topic=Geography|page=1|oldid=554646767}}
{{WikiProject Canada|geography=yes|class=Start|importance=Mid|bc=yes}}
{{WikiProject Protected areas|class=Start|importance=Mid}}
{{some random template}}";
        articleText2 = articleText;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(articleText2), "no changes if everything is in place; contains 2 Wikiprojects");
    }

    [Test]
    public void MoveBannersAndWikiProjects()
    {
        string a = @"{{Skip to talk}}", b = @"{{Talk header}}", c = @"{{GA nominee}}", d = @"{{Controversial}}", e = @"{{Not a forum}}", f = @"{{British English}}", g = @"{{FailedGA}}", h = @"{{WikiProject Greece|class=}}", i = @"{{Image requested}}", j = @"{{Connected contributor|John Doe}}";
        string correct = c + "\r\n" + a + "\r\n" + b + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + g + "\r\n" + h + "\r\n" + i + "\r\n";
        string articleText = correct;

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefghi");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + h + "\r\n" + g + "\r\n" + i;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefhgi");

        articleText = h + "\r\n" + c + "\r\n" + a + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + b + "\r\n" + g + "\r\n" + i;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "hcadefbgi");

        articleText = d + "\r\n" + c + "\r\n" + a + "\r\n" + h + "\r\n" + f + "\r\n" + e + "\r\n" + b + "\r\n" + i + "\r\n" + g;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "dcahfebig");

        correct = correct + j + "\r\n";

        articleText = correct;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefghij");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + h + "\r\n" + g + "\r\n" + j + "\r\n" + i;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefhgji");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + h + "\r\n" + g + "\r\n" + j + "\r\n" + i + "\r\n" + "\r\n" + "\r\n";
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefhgji with newlines at the end");

        articleText = b + "\r\n" + "\r\n" + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + h + "\r\n" + g + "\r\n" + j + "\r\n" + "\r\n" + "\r\n" + i + "\r\n" + "\r\n" + "\r\n";

        string k = @"{{To do}}";

        correct = correct + k + "\r\n";
        articleText = correct;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "abcdefghijk");

        articleText = b + "\r\n" + a + "\r\n" + c + "\r\n" + d + "\r\n" + e + "\r\n" + f + "\r\n" + k + "\r\n" + g + "\r\n" + j + "\r\n" + i + "\r\n" + h;
        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefkgjih");

        TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
        Assert.That(articleText, Is.EqualTo(correct), "bacdefhgji with newlines at the end and in the middle");
    }
}
