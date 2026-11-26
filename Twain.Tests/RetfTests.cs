
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Twain.Core.Parse;

namespace Twain.Tests.Parsing;

/// <summary>
/// Provides an in-memory collection of regex typo rules
/// without requiring an external wiki or network connection.
/// </summary>
public class TypoList : ITyposProvider
{
    private readonly Dictionary<string, string> _typos;

    public TypoList(Dictionary<string, string> typos)
    {
        _typos = typos;
    }

    public Dictionary<string, string> GetTypos()
    {
        return _typos;
    }
}

/// <summary>
/// Tests regex typo detection, correction, protected text,
/// regex behavior, and edit-summary generation.
/// </summary>
[TestFixture]
public class RegexTypoFixTests : RequiresInitialization
{
    #region Test Preparation

    private RegExTypoFix? _regexTypos;

    private readonly Dictionary<string, string> _typos = new();

    private string _summary = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _typos.Clear();
        _regexTypos = null;
        _summary = string.Empty;
    }

    /// <summary>
    /// Initializes the regex typo engine using the rules
    /// configured for the current test.
    /// </summary>
    private RegExTypoFix GetTypoFixer()
    {
        if (_regexTypos is not null)
        {
            return _regexTypos;
        }

        if (_typos.Count == 0)
        {
            throw new InvalidOperationException(
                "No regex typo rules have been configured for this test.");
        }

        _regexTypos = new RegExTypoFix(
            false,
            new TypoList(_typos));

        Assert.That(
            _regexTypos.TyposLoaded,
            Is.True,
            "The configured regex typo rules failed to load.");

        return _regexTypos;
    }

    /// <summary>
    /// Applies regex typo corrections and captures
    /// the generated edit summary.
    /// </summary>
    private string FixTypos(
        string articleText,
        string articleTitle = "Test")
    {
        return GetTypoFixer().PerformTypoFixes(
            articleText,
            out _,
            out _summary,
            articleTitle);
    }

    /// <summary>
    /// Determines whether the article contains
    /// a detectable regex typo.
    /// </summary>
    private bool DetectTypo(
        string articleText,
        string articleTitle = "Test")
    {
        return GetTypoFixer().DetectTypo(
            articleText,
            articleTitle);
    }

    /// <summary>
    /// Verifies that typo correction produces
    /// the expected article text.
    /// </summary>
    private void AssertFix(
        string expected,
        string articleText,
        string articleTitle = "Test")
    {
        Assert.That(
            FixTypos(articleText, articleTitle),
            Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that typo correction leaves
    /// the supplied article text unchanged.
    /// </summary>
    private void AssertNoFix(
        string articleText,
        string articleTitle = "Test")
    {
        Assert.That(
            FixTypos(articleText, articleTitle),
            Is.EqualTo(articleText));
    }

    #endregion

    #region Basic Typo Correction

    [Test]
    public void CorrectsSimpleTypos()
    {
        _typos["foo"] = "bar";

        AssertNoFix(string.Empty);
        AssertNoFix("test");

        AssertFix(
            "the bars are cute",
            "the foos are cute");

        AssertFix(
            "the bars are cute bars",
            "the foos are cute foos");
    }

    [Test]
    public void RespectsSpellFixNo()
    {
        _typos["foo"] = "bar";

        AssertNoFix("spellfixno foos are");
    }

    [Test]
    public void DetectsTyposAndRespectsSic()
    {
        _typos["foo"] = "bar";

        Assert.That(
            DetectTypo("The foo was"),
            Is.True);

        Assert.That(
            DetectTypo("The foo was "),
            Is.True);

        Assert.That(
            DetectTypo("The x was"),
            Is.False);

        Assert.That(
            DetectTypo("The foo {{sic}} was"),
            Is.False);
    }

    #endregion

    #region Regex Behavior

    [Test]
    public void RespectsRegexCaseSensitivity()
    {
        _typos["foo"] = "bar";
        _typos["[Bb]oz"] = "quux";
        _typos["(?i:fubar)"] = "fur";

        // Correct matching case.
        AssertFix("bars", "foos");

        // Preserve nonmatching case.
        AssertNoFix("Foobar");

        // Respect case-insensitive regex constructs.
        AssertFix("quux", "boz");
        AssertFix("quux", "Boz");
        AssertNoFix("BOZ");
        AssertFix("furbag", "FuBaRbag");
    }

    [Test]
    public void PreservesRegexBackreferences()
    {
        // Historical AWB regression:
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_6#Rules_in_Finnish_Regex_TypoFix_list_not_always_applied

        _typos["(taht)"] = "that";
        _typos[@"\b(a|the)\b\s+\1\b"] = "$1";

        // Each rule must function independently.
        AssertFix("that", "taht");
        AssertFix("the", "the the");
        AssertNoFix("a the");

        // Matches from other rules must not interfere
        // with regex backreferences.
        AssertFix("that the", "taht the the");
    }

    [Test]
    public void AppliesRulesAcrossRegexGroups()
    {
        _typos[@"\booze\b"] = "booze!";
        _typos[@"foo(\w*)\b"] = "bar$1";
        _typos[@"\b(a|the)\b\s+\1\b"] = "$1";

        AssertFix(
            "barwards viva the booze!",
            "foowards viva the the ooze");
    }

    #endregion

    #region Protected Text

    [Test]
    public void DoesNotChangeUrls()
    {
        _typos["foo"] = "bar";

        AssertNoFix(
            "http://foo.com/asdfasdf/asdf.htm");

        AssertNoFix(
            @"[http://foo.com/asdfasdf/asdf.htm Foo's best]");

        AssertNoFix(
            "http://foo.com/asdfasdf/foo.htm");
    }

    [Test]
    public void CorrectsWikilinkDisplayText()
    {
        _typos["foo"] = "bar";

        // Preserve the wikilink target.
        AssertNoFix("[[foo]]");

        // Correct the display text of a piped wikilink.
        AssertFix(
            "[[Fred|bar]]",
            "[[Fred|foo]]");
    }

    [Test]
    public void RespectsWikilinkProtection()
    {
        _typos["foo"] = "bar";

        // Preserve typos that match wikilink targets.
        AssertNoFix(
            "now [[foo]] was foo here");

        AssertNoFix(
            "now [[foo|bo]] was foo here");

        AssertNoFix(
            "now [[Mr foo oft]] was foo here");

        AssertNoFix(
            "now [[Mr foo oft]] was foo here [[Image:foo.png]] a");

        // Image names do not prevent corrections
        // elsewhere in the article.
        AssertFix(
            "now [[Image:foo.png]] was bar here",
            "now [[Image:foo.png]] was foo here");

        // Interwiki links do not prevent corrections
        // elsewhere in the article.
        AssertFix(
            "now [[sv:foo]] was bar here",
            "now [[sv:foo]] was foo here");

        // Category names do not prevent corrections
        // elsewhere in the article.
        AssertFix(
            "now [[Category:A foo]] was bar here",
            "now [[Category:A foo]] was foo here");

        // Preserve case-sensitive wikilink behavior.
        AssertFix(
            "now bar [[Foo]] was",
            "now foo [[Foo]] was");
    }

    [Test]
    public void PreservesTemplatesLinksAndUrls()
    {
        _typos["(T|t)eh"] = "the";

        AssertNoFix(
            @"{{teh}} http://tehcrappe.org/TehCrappe [[teh]] [http://tehcrappe.org/TehCrappe]");
    }

    [Test]
    public void PreservesArticleTitleText()
    {
        _typos["teh"] = "the";
        _typos["foo"] = "bar";

        AssertFix(
            "teh bar",
            "teh foo",
            "teh");

        AssertFix(
            "teh bar",
            "teh foo",
            "teh crap");

        AssertNoFix(
            "teh home",
            "teh");
    }

    #endregion

    #region Edit Summaries

    [Test]
    public void GeneratesAccurateTypoEditSummaries()
    {
        _typos["f[oO0]{2}"] = "foo";
        _typos[@"(?<=ten )bar"] = "BAR";

        AssertFix(
            "foo foo",
            "foo fO0");

        Assert.That(
            _summary,
            Does.Not.Contain("foo → foo"),
            "Unchanged text should not appear in the edit summary.");

        Assert.That(
            _summary,
            Does.Not.Contain("2"),
            "Unchanged matches should not contribute to the replacement count.");

        AssertFix(
            "ten BAR",
            "ten bar");

        Assert.That(
            _summary,
            Does.Contain("bar → BAR"),
            "Regex with lookbehind should generate a summary based on the match value.");

        Assert.That(
            _summary,
            Does.Not.Contain("ten"),
            "The summary should not include groups outside the match value.");
    }

    #endregion
}
