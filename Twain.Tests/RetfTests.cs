using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Twain.Core.Background;
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

    /// <summary>
    /// Simulates a typo-list provider that fails during rule retrieval.
    /// </summary>
    private sealed class FailingTypoList : ITyposProvider
    {
        public Dictionary<string, string> GetTypos()
        {
            throw new InvalidOperationException(
                "Simulated typo-list provider failure.");
        }
    }


    private static readonly TimeSpan BackgroundLoadTimeout =
        TimeSpan.FromSeconds(10);

    /// <summary>
    /// Captures background-loading completion notifications
    /// and allows tests to wait for worker termination.
    /// </summary>
    private sealed class BackgroundLoadObserver
    {
        private int _completionCount;

        private readonly TaskCompletionSource<BackgroundRequest>
            _completion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public int CompletionCount =>
            Volatile.Read(ref _completionCount);

        public void OnCompleted(BackgroundRequest request)
        {
            Interlocked.Increment(ref _completionCount);
            _completion.TrySetResult(request);
        }

        public void WaitForWorker()
        {
            Assert.That(
                _completion.Task.Wait(BackgroundLoadTimeout),
                Is.True,
                "Background loading did not report completion.");

            BackgroundRequest request = _completion.Task.Result;

            Assert.That(
                request,
                Is.Not.Null,
                "The completion callback did not provide a background request.");

            Assert.That(
                SpinWait.SpinUntil(
                    () => (request.ThreadStatus() & ThreadState.Stopped)
                        == ThreadState.Stopped,
                    BackgroundLoadTimeout),
                Is.True,
                "The background worker did not terminate.");

            Assert.That(
                request.ErrorException,
                Is.Null,
                "The background worker encountered an unhandled exception.");
        }
    }

    /// <summary>
    /// Simulates a delayed typo-list provider whose completion
    /// is controlled by the test.
    /// </summary>
    private sealed class DelayedTypoList : ITyposProvider
    {
        private readonly TaskCompletionSource<bool> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WaitUntilStarted()
        {
            return _started.Task.Wait(BackgroundLoadTimeout);
        }

        public void Release()
        {
            _release.TrySetResult(true);
        }

        public Dictionary<string, string> GetTypos()
        {
            _started.TrySetResult(true);

            if (!_release.Task.Wait(BackgroundLoadTimeout))
            {
                throw new TimeoutException(
                    "The delayed typo-list provider was not released.");
            }

            return new Dictionary<string, string>
            {
                ["teh"] = "the"
            };
        }
    }


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

    #region Rule Loading

    [Test]
    public void LoadSingleValidRule()
    {
        _typos["teh"] = "the";

        RegExTypoFix fixer = new(
            false,
            new TypoList(_typos));

        Assert.That(fixer.TyposLoaded, Is.True);
        Assert.That(fixer.TypoCount, Is.EqualTo(1));
    }

    [Test]
    public void LoadMultipleValidRules()
    {
        _typos["teh"] = "the";
        _typos["recieve"] = "receive";
        _typos["seperate"] = "separate";

        RegExTypoFix fixer = new(
            false,
            new TypoList(_typos));

        Assert.That(fixer.TyposLoaded, Is.True);
        Assert.That(fixer.TypoCount, Is.EqualTo(3));
    }

    [Test]
    public void LoadEmptyTypoList()
    {
        RegExTypoFix fixer = new(
            false,
            new TypoList(_typos));

        Assert.That(fixer.TyposLoaded, Is.False);
        Assert.That(fixer.TypoCount, Is.Zero);
    }

    [Test]
    public void LoadInvalidRegexRule()
    {
        // An unclosed character class is invalid regex syntax.
        _typos["["] = "replacement";

        RegExTypoFix fixer = new(
            false,
            new TypoList(_typos));

        Assert.That(fixer.TyposLoaded, Is.False);
        Assert.That(fixer.TypoCount, Is.Zero);
        Assert.That(fixer.LoadError, Is.Not.Null.And.Not.Empty);
    }


    [Test]
    public void LoadMixedValidAndInvalidRules()
    {
        _typos["teh"] = "the";
        _typos["recieve"] = "receive";
        _typos["["] = "replacement";

        RegExTypoFix fixer = new(
            false,
            new TypoList(_typos));

        Assert.That(fixer.TyposLoaded, Is.False);
        Assert.That(fixer.TypoCount, Is.Zero);
        Assert.That(fixer.GetTypos(), Is.Empty);

        Assert.That(
            fixer.LoadError,
            Does.Contain("Invalid regex typo rule"));
    }

    [Test]
    public void ProviderThrowsException()
    {
        RegExTypoFix fixer = new(
            false,
            new FailingTypoList());

        Assert.That(fixer.TyposLoaded, Is.False);
        Assert.That(fixer.TypoCount, Is.Zero);
        Assert.That(fixer.GetTypos(), Is.Empty);

        Assert.That(
            fixer.LoadError,
            Is.EqualTo("Simulated typo-list provider failure."));
    }

    [Test]
    public void FailedLoadPreventsCorrections()
    {
        _typos["teh"] = "the";
        _typos["["] = "replacement";

        RegExTypoFix fixer = new(
            false,
            new TypoList(_typos));

        const string articleText = "This is teh article.";

        string result = fixer.PerformTypoFixes(
            articleText,
            out bool noChange,
            out string summary,
            "Test");

        Assert.That(fixer.TyposLoaded, Is.False);
        Assert.That(result, Is.EqualTo(articleText));
        Assert.That(noChange, Is.True);
        Assert.That(summary, Is.Empty);

        Assert.That(
            fixer.DetectTypo(articleText, "Test"),
            Is.False);
    }

    [Test]
    public void CompletionCallbackInvokedOnce()
    {
        _typos["teh"] = "the";

        int completionCount = 0;

        RegExTypoFix fixer = new(
            false,
            new TypoList(_typos),
            _ => completionCount++);

        Assert.That(fixer.TyposLoaded, Is.True);
        Assert.That(fixer.LoadError, Is.Null);
        Assert.That(completionCount, Is.EqualTo(1));
    }

    [Test]
    public void CompletionCallbackInvokedOnFailure()
    {
        int completionCount = 0;

        RegExTypoFix fixer = new(
            false,
            new FailingTypoList(),
            _ => completionCount++);

        Assert.That(fixer.TyposLoaded, Is.False);
        Assert.That(fixer.LoadError, Is.Not.Null.And.Not.Empty);
        Assert.That(completionCount, Is.EqualTo(1));
    }


    #endregion


    #region Background Rule Loading

    [Test]
    public void ThreadedLoadCompletesSuccessfully()
    {
        _typos["teh"] = "the";
        _typos["recieve"] = "receive";

        BackgroundLoadObserver observer = new();

        RegExTypoFix fixer = new(
            true,
            new TypoList(_typos),
            observer.OnCompleted);

        observer.WaitForWorker();

        Assert.That(fixer.TyposLoaded, Is.True);
        Assert.That(fixer.TypoCount, Is.EqualTo(2));
        Assert.That(fixer.GetTypos(), Has.Count.EqualTo(2));
        Assert.That(fixer.LoadError, Is.Null);
    }

    [Test]
    public void ThreadedLoadInvokesCallbackOnce()
    {
        _typos["teh"] = "the";

        BackgroundLoadObserver observer = new();

        RegExTypoFix fixer = new(
            true,
            new TypoList(_typos),
            observer.OnCompleted);

        observer.WaitForWorker();

        Assert.That(fixer.TyposLoaded, Is.True);

        Assert.That(
            observer.CompletionCount,
            Is.EqualTo(1),
            "Background loading should report completion exactly once.");
    }

    [Test]
    public void ThreadedLoadReportsProviderFailure()
    {
        BackgroundLoadObserver observer = new();

        RegExTypoFix fixer = new(
            true,
            new FailingTypoList(),
            observer.OnCompleted);

        observer.WaitForWorker();

        Assert.That(fixer.TyposLoaded, Is.False);
        Assert.That(fixer.TypoCount, Is.Zero);
        Assert.That(fixer.GetTypos(), Is.Empty);

        Assert.That(
            fixer.LoadError,
            Is.EqualTo("Simulated typo-list provider failure."));

        Assert.That(
            observer.CompletionCount,
            Is.EqualTo(1));
    }

    [Test]
    public void ThreadedLoadWaitsForProvider()
    {
        DelayedTypoList provider = new();
        BackgroundLoadObserver observer = new();

        RegExTypoFix fixer = new(
            true,
            provider,
            observer.OnCompleted);

        try
        {
            Assert.That(
                provider.WaitUntilStarted(),
                Is.True,
                "The background provider did not start.");

            Assert.That(fixer.TyposLoaded, Is.False);
            Assert.That(fixer.TypoCount, Is.Zero);

            Assert.That(
                observer.CompletionCount,
                Is.Zero,
                "Completion was reported before the provider returned.");
        }
        finally
        {
            // Always release the provider, even if an assertion fails.
            provider.Release();
            observer.WaitForWorker();
        }

        Assert.That(fixer.TyposLoaded, Is.True);
        Assert.That(fixer.TypoCount, Is.EqualTo(1));
        Assert.That(fixer.LoadError, Is.Null);
        Assert.That(observer.CompletionCount, Is.EqualTo(1));
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


    #region Processing Results

    [Test]
    public void PerformTypoFixesReportsNoChange()
    {
        _typos["teh"] = "the";

        const string articleText = "This is the article.";

        string result = GetTypoFixer().PerformTypoFixes(
            articleText,
            out bool noChange,
            out _,
            "Test");

        Assert.That(result, Is.EqualTo(articleText));
        Assert.That(noChange, Is.True);
    }

    [Test]
    public void PerformTypoFixesReportsChange()
    {
        _typos["teh"] = "the";

        const string articleText = "This is teh article.";

        string result = GetTypoFixer().PerformTypoFixes(
            articleText,
            out bool noChange,
            out string summary,
            "Test");

        Assert.That(result, Is.EqualTo("This is the article."));
        Assert.That(noChange, Is.False);
        Assert.That(summary, Does.Contain("teh → the"));
    }

    [Test]
    public void PerformTypoFixesReportsMultipleCorrections()
    {
        _typos["teh"] = "the";

        const string articleText = "teh first and teh second.";

        string result = GetTypoFixer().PerformTypoFixes(
            articleText,
            out bool noChange,
            out string summary,
            "Test");

        Assert.That(result, Is.EqualTo("the first and the second."));
        Assert.That(noChange, Is.False);
        Assert.That(summary, Does.Contain("teh → the (2)"));
    }

    [Test]
    public void PerformTypoFixesReportsNoChangeForProtectedText()
    {
        _typos["teh"] = "the";

        const string articleText = "This is teh {{sic}} article.";

        string result = GetTypoFixer().PerformTypoFixes(
            articleText,
            out bool noChange,
            out string summary,
            "Test");

        Assert.That(result, Is.EqualTo(articleText));
        Assert.That(noChange, Is.True);
        Assert.That(summary, Is.Empty);
    }

    [Test]
    public void PerformTypoFixesReportsNoChangeForSelfMatches()
    {
        _typos["foo"] = "foo";

        const string articleText = "This is foo.";

        string result = GetTypoFixer().PerformTypoFixes(
            articleText,
            out bool noChange,
            out string summary,
            "Test");

        Assert.That(result, Is.EqualTo(articleText));
        Assert.That(noChange, Is.True);
        Assert.That(summary, Does.Not.Contain("foo → foo"));
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
