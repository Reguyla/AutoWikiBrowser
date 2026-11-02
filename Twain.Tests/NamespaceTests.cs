using NUnit.Framework;
using NUnit.Framework.Legacy;
using Twain.Core;

namespace Twain.Tests;

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
