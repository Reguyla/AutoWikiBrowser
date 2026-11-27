using NUnit.Framework;
using Twain.Core;
using Twain.Core.Disambiguation;
using static Twain.Core.Disambiguation.DisambiguationProcessor;

namespace Twain.Tests;

[TestFixture]
public class DisambiguationTests
{
    private static DisambiguationPreparation PrepareArticle(
        string articleText)
    {
        return DisambiguationProcessor.Prepare(
            articleText,
            "Matrix",
            new[] { "Matrix (mathematics)", "The Matrix" },
            100);
    }

    [TestCase(
        "[[Matrix (mathematics)|Matrix]]",
        DisambiguationCorrectionType.Resolved)]
    [TestCase(
        "Matrix",
        DisambiguationCorrectionType.Unlinked)]
    [TestCase(
        "[[Matrix]]{{Disambiguation needed}}",
        DisambiguationCorrectionType.Tagged)]
    [TestCase(
        "[[Matrix]]{{Disambiguation needed|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}",
        DisambiguationCorrectionType.Tagged)]
    [TestCase(
        "",
        DisambiguationCorrectionType.Removed)]
    [TestCase(
        "[[Matrix|Matrices]]",
        DisambiguationCorrectionType.Modified)]
    [TestCase(
        "Some replacement text",
        DisambiguationCorrectionType.Modified)]
    [TestCase(
        "[[Matrix (mathematics)]] and [[The Matrix]]",
        DisambiguationCorrectionType.Modified)]
    public void CollectCorrections_ClassifiesReplacement(
        string replacement,
        DisambiguationCorrectionType expectedType)
    {
        var preparation = PrepareArticle(
            "Example [[Matrix]] link.");

        var results = new[]
        {
            new DisambiguationResult(false, replacement)
        };

        var corrections = CollectCorrections(
            preparation,
            results);

        Assert.That(corrections, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(
                corrections[0].CorrectionType,
                Is.EqualTo(expectedType));

            Assert.That(
                corrections[0].OriginalTarget,
                Is.EqualTo("Matrix"));

            Assert.That(
                corrections[0].OriginalText,
                Is.EqualTo("[[Matrix]]"));

            Assert.That(
                corrections[0].ReplacementText,
                Is.EqualTo(replacement));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CollectCorrections_IgnoresUnchangedLink(
        bool noChange)
    {
        var preparation = PrepareArticle(
            "Example [[Matrix]] link.");

        var results = new[]
        {
            new DisambiguationResult(
                noChange,
                "[[Matrix]]")
        };

        var corrections = CollectCorrections(
            preparation,
            results);

        Assert.That(corrections, Is.Empty);
    }

    [Test]
    public void CollectCorrections_CapturesReplacementTarget()
    {
        var preparation = PrepareArticle(
            "Example [[Matrix]] link.");

        var results = new[]
        {
            new DisambiguationResult(
                false,
                "[[Matrix (mathematics)|Matrix]]")
        };

        var corrections = CollectCorrections(
            preparation,
            results);

        Assert.That(corrections, Has.Count.EqualTo(1));

        Assert.That(
            corrections[0].ReplacementTarget,
            Is.EqualTo("Matrix (mathematics)"));
    }

    [TestCase(
    "[[Matrix (mathematics)|Matrix]]",
    "Disambiguated: Matrix → Matrix (mathematics)")]
    [TestCase(
    "Matrix",
    "Unlinked ambiguous link: Matrix")]
    [TestCase(
    "[[Matrix]]{{Disambiguation needed}}",
    "Tagged ambiguous link: Matrix")]
    [TestCase(
    "",
    "Removed ambiguous link: Matrix")]
    [TestCase(
    "[[Matrix|Matrices]]",
    "Modified ambiguous link: Matrix")]
    public void CreateSummaryContributions_FormatsCorrection(
    string replacement,
    string expectedDescription)
    {
        var preparation = PrepareArticle(
            "Example [[Matrix]] link.");

        var results = new[]
        {
        new DisambiguationResult(false, replacement)
    };

        var corrections = CollectCorrections(
            preparation,
            results);

        var contributions = CreateSummaryContributions(
            corrections);

        Assert.That(contributions, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(
                contributions[0].Category,
                Is.EqualTo(EditSummaryCategory.Disambiguation));

            Assert.That(
                contributions[0].Description,
                Is.EqualTo(expectedDescription));

            Assert.That(
                contributions[0].ShortDescription,
                Is.EqualTo("Disambiguation: Matrix"));
        });
    }

    [Test]
    public void CreateSummaryContributions_GroupsIdenticalCorrections()
    {
        var preparation = PrepareArticle(
            "[[Matrix]], [[Matrix|matrix]], and [[Matrix|matrices]].");

        var results = new[]
        {
        new DisambiguationResult(
            false,
            "[[Matrix (mathematics)|Matrix]]"),

        new DisambiguationResult(
            false,
            "[[Matrix (mathematics)|matrix]]"),

        new DisambiguationResult(
            false,
            "[[Matrix (mathematics)|matrices]]")
    };

        var corrections = CollectCorrections(
            preparation,
            results);

        var contributions = CreateSummaryContributions(
            corrections);

        Assert.That(corrections, Has.Count.EqualTo(3));
        Assert.That(contributions, Has.Count.EqualTo(1));

        Assert.That(
            contributions[0].Description,
            Is.EqualTo(
                "Disambiguated: Matrix → Matrix (mathematics) (x3)"));
    }

    [Test]
    public void CreateSummaryContributions_SeparatesDifferentTargets()
    {
        var preparation = PrepareArticle(
            "[[Matrix]] and [[Matrix]].");

        var results = new[]
        {
        new DisambiguationResult(
            false,
            "[[Matrix (mathematics)|Matrix]]"),

        new DisambiguationResult(
            false,
            "[[The Matrix|Matrix]]")
    };

        var corrections = CollectCorrections(
            preparation,
            results);

        var contributions = CreateSummaryContributions(
            corrections);

        Assert.That(contributions, Has.Count.EqualTo(2));

        Assert.That(
            contributions.Select(c => c.Description),
            Is.EquivalentTo(new[]
            {
            "Disambiguated: Matrix → Matrix (mathematics)",
            "Disambiguated: Matrix → The Matrix"
            }));
    }

    [Test]
    public void CreateSummaryContributions_SeparatesCorrectionTypes()
    {
        var preparation = PrepareArticle(
            "[[Matrix]], [[Matrix]], [[Matrix]], and [[Matrix]].");

        var results = new[]
        {
        new DisambiguationResult(
            false,
            "[[Matrix (mathematics)|Matrix]]"),

        new DisambiguationResult(
            false,
            "Matrix"),

        new DisambiguationResult(
            false,
            "[[Matrix]]{{Disambiguation needed}}"),

        new DisambiguationResult(
            true,
            "[[Matrix]]")
    };

        var corrections = CollectCorrections(
            preparation,
            results);

        var contributions = CreateSummaryContributions(
            corrections);

        Assert.That(corrections, Has.Count.EqualTo(3));
        Assert.That(contributions, Has.Count.EqualTo(3));

        Assert.That(
            contributions.Select(c => c.Description),
            Is.EquivalentTo(new[]
            {
            "Disambiguated: Matrix → Matrix (mathematics)",
            "Unlinked ambiguous link: Matrix",
            "Tagged ambiguous link: Matrix"
            }));
    }

    [Test]
    public void CreateSummaryContributions_EmptyCorrections()
    {
        var contributions = CreateSummaryContributions(
            Array.Empty<DisambiguationCorrection>());

        Assert.That(contributions, Is.Empty);
    }

    [Test]
    public void CollectCorrections_RejectsMismatchedResultCount()
    {
        var preparation = PrepareArticle(
            "[[Matrix]] and [[Matrix]].");

        var results = new[]
        {
        new DisambiguationResult(false, "Matrix")
    };

        Assert.Throws<ArgumentException>(() =>
            CollectCorrections(preparation, results));
    }
}