using NUnit.Framework;
using Twain.Core;
using Twain.UI.ViewModels.Lists;

namespace Twain.Tests;

[TestFixture]
public class PreparseTests
{
    private MakeListViewModel _makeList = null!;

    [SetUp]
    public void SetUp()
    {
        _makeList = new MakeListViewModel();
    }

    [Test]
    public void MarkPreparseRetained_MarksArticle()
    {
        Article article = new("Test Article");
        _makeList.Articles.Add(article);

        _makeList.MarkPreparseRetained(article);

        Assert.That(
            _makeList.IsPreparseRetained(article),
            Is.True);
    }

    [Test]
    public void UnprocessedArticle_IsNotHighlighted()
    {
        Article article = new("Test Article");
        _makeList.Articles.Add(article);

        Assert.That(
            _makeList.IsPreparseRetained(article),
            Is.False);
    }

    [Test]
    public void MarkPreparseRetained_DoesNotMarkOtherArticles()
    {
        Article retained = new("Retained Article");
        Article unprocessed = new("Unprocessed Article");

        _makeList.Articles.Add(retained);
        _makeList.Articles.Add(unprocessed);

        _makeList.MarkPreparseRetained(retained);

        Assert.Multiple(() =>
        {
            Assert.That(
                _makeList.IsPreparseRetained(retained),
                Is.True);

            Assert.That(
                _makeList.IsPreparseRetained(unprocessed),
                Is.False);
        });
    }

    [Test]
    public void MarkPreparseRetained_DoesNotDuplicateHighlight()
    {
        Article article = new("Test Article");
        _makeList.Articles.Add(article);

        _makeList.MarkPreparseRetained(article);

        int version = _makeList.PreparseHighlightVersion;

        _makeList.MarkPreparseRetained(article);

        Assert.That(
            _makeList.PreparseHighlightVersion,
            Is.EqualTo(version));
    }

    [Test]
    public void ClearPreparseHighlights_RemovesHighlights()
    {
        Article article = new("Test Article");
        _makeList.Articles.Add(article);

        _makeList.MarkPreparseRetained(article);
        _makeList.ClearPreparseHighlights();

        Assert.That(
            _makeList.IsPreparseRetained(article),
            Is.False);
    }

    [Test]
    public void RetainedArticle_RemainsHighlightedAfterReordering()
    {
        Article first = new("First Article");
        Article second = new("Second Article");

        _makeList.Articles.Add(first);
        _makeList.Articles.Add(second);

        _makeList.MarkPreparseRetained(second);

        _makeList.Articles.Move(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(
                _makeList.Articles.First(),
                Is.SameAs(second));

            Assert.That(
                _makeList.IsPreparseRetained(second),
                Is.True);
        });
    }

    [Test]
    public void RetainedHighlight_DoesNotTransferToNewArticle()
    {
        Article original = new("Test Article");
        _makeList.Articles.Add(original);

        _makeList.MarkPreparseRetained(original);

        Article replacement = new("Test Article");

        _makeList.ReplaceArticles([replacement]);

        Assert.That(
            _makeList.IsPreparseRetained(replacement),
            Is.False);
    }
}