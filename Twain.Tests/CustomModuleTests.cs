
using NUnit.Framework;
using Twain.Core;
using Twain.Core.Plugin;

namespace Twain.Tests;

[TestFixture]
public class CustomModuleTests : RequiresInitialization
{
    [Test]
    public void ModuleChangesText()
    {
        // Arrange
        Article article = new Article(
            "Test article",
            "Original text");

        IModule module = new TestModule(
            "Modified text",
            "Custom module: Test change");

        // Act
        article.SendPageToCustomModule(module);

        // Assert
        Assert.That(
            article.ArticleText,
            Is.EqualTo("Modified text"));
    }

    private sealed class TestModule : IModule
    {
        private readonly string _updatedText;
        private readonly string _editSummary;
        private readonly bool _skip;

        public TestModule(
            string updatedText,
            string editSummary,
            bool skip = false)
        {
            _updatedText = updatedText;
            _editSummary = editSummary;
            _skip = skip;
        }

        public string ProcessArticle(
            string articleText,
            string articleTitle,
            int @namespace,
            out string summary,
            out bool skip)
        {
            summary = _editSummary;
            skip = _skip;

            return _updatedText;
        }
    }

    [Test]
    public void ModuleSummaryIsPreserved()
    {
        // Arrange
        Article article = new Article(
            "Test article",
            "Original text");

        IModule module = new TestModule(
            "Modified text",
            "Custom module: Test change");

        // Act
        article.SendPageToCustomModule(module);

        // Assert
        Assert.That(
            article.EditSummary,
            Does.Contain("Custom module: Test change"));
    }

    [Test]
    public void ModuleWithoutSummaryUsesFallback()
    {
        // Arrange
        Article article = new Article(
            "Test article",
            "Original text");

        IModule module = new TestModule(
            "Modified text",
            string.Empty);

        // Act
        article.SendPageToCustomModule(module);

        // Assert
        Assert.That(
            article.EditSummary,
            Does.Contain("Custom module: Changes made"));
    }

    [Test]
    public void ModuleSummaryIsCollected()
    {
        // Arrange
        Article article = new Article(
            "Test article",
            "Original text");

        IModule module = new TestModule(
            "Modified text",
            "Custom module: Test change");

        // Act
        article.SendPageToCustomModule(module);

        // Assert
        Assert.That(
            article.EditSummaryCollector.Contributions,
            Has.Some.Matches<EditSummaryContribution>(
                contribution =>
                    contribution.Category ==
                        EditSummaryCategory.CustomModule &&
                    contribution.Description ==
                        "Custom module: Test change"));
    }

    [Test]
    public void ModuleWithoutChangesAddsNoContribution()
    {
        // Arrange
        Article article = new Article(
            "Test article",
            "Original text");

        IModule module = new TestModule(
            "Original text",
            string.Empty);

        // Act
        article.SendPageToCustomModule(module);

        // Assert
        Assert.That(
            article.EditSummaryCollector.Contributions,
            Has.None.Matches<EditSummaryContribution>(
                contribution =>
                    contribution.Category ==
                        EditSummaryCategory.CustomModule));
    }

    [Test]
    public void SkippedModuleAddsNoContribution()
    {
        // Arrange
        Article article = new Article(
            "Test article",
            "Original text");

        IModule module = new TestModule(
            "Modified text",
            "Custom module: Test change",
            skip: true);

        // Act
        article.SendPageToCustomModule(module);

        // Assert
        Assert.That(
            article.EditSummaryCollector.Contributions,
            Has.None.Matches<EditSummaryContribution>(
                contribution =>
                    contribution.Category ==
                        EditSummaryCategory.CustomModule));
    }

    [Test]
    public void ModuleRejectsInvalidSummary()
    {
        // Arrange
        string articleText = new string('A', 2000);

        Article article = new Article(
            "Test article",
            articleText);

        IModule module = new TestModule(
            articleText + " Modified",
            articleText);

        // Act & Assert
        Assert.That(
            () => article.SendPageToCustomModule(module),
            Throws.TypeOf<InvalidOperationException>());
    }

}
