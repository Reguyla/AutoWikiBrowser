using NUnit.Framework;
using Twain.Core.Processing;

namespace Twain.Tests;

[TestFixture]
public class MainProcessTests
{
    [TestCase(true, false, true, false,
        ArticleProcessingOutcome.ChangesMade)]
    [TestCase(true, false, false, false,
        ArticleProcessingOutcome.NoChanges)]
    [TestCase(false, true, false, false,
        ArticleProcessingOutcome.Skipped)]
    [TestCase(false, false, false, false,
        ArticleProcessingOutcome.Failed)]
    [TestCase(false, false, false, true,
        ArticleProcessingOutcome.Failed)]
    [TestCase(true, true, true, false,
        ArticleProcessingOutcome.Skipped)]
    [TestCase(true, true, true, true,
        ArticleProcessingOutcome.Failed)]
    public void DetermineOutcome_ReturnsExpectedResult(
        bool completed,
        bool wasSkipped,
        bool hasTextChanges,
        bool failed,
        ArticleProcessingOutcome expected)
    {
        ArticleProcessingOutcome actual =
            MainProcess.DetermineOutcome(
                completed,
                wasSkipped,
                hasTextChanges,
                failed);

        Assert.That(actual, Is.EqualTo(expected));
    }
}