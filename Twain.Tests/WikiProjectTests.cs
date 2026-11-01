using NUnit.Framework;
using Twain.Core;
using Twain.Core.Parse;
using Twain.Core.TalkPages;

namespace Twain.Tests;

[TestFixture]
public class WikiProjectTests : RequiresInitialization
{
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
}
