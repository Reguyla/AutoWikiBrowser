using System;
using System.Linq;
using NUnit.Framework;
using Twain.Core.ArticleAnalysis;

namespace Twain.Tests;

[TestFixture]
public class ArticleStructureAnalyzerTests
{
    [Test]
    public void Analyze_ArticleWithoutHeadings_ReturnsLeadOnly()
    {
        const string text = "This article has no headings.";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.OriginalText, Is.EqualTo(text));
            Assert.That(result.Lead.Content, Is.EqualTo(text));
            Assert.That(result.Lead.StartIndex, Is.Zero);
            Assert.That(result.Lead.Length, Is.EqualTo(text.Length));
            Assert.That(result.Lead.IsLead, Is.True);
            Assert.That(result.HasSections, Is.False);
            Assert.That(result.SectionCount, Is.Zero);
        });
    }

    [Test]
    public void Analyze_ArticleWithMultipleSections_IdentifiesSections()
    {
        const string text =
            "Introduction.\r\n" +
            "== History ==\r\n" +
            "Historical content.\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Lead.Content, Is.EqualTo("Introduction.\r\n"));
            Assert.That(result.SectionCount, Is.EqualTo(2));

            Assert.That(result.Sections[0].Title, Is.EqualTo("History"));
            Assert.That(result.Sections[0].Level, Is.EqualTo(2));
            Assert.That(result.Sections[0].Content,
                Is.EqualTo("Historical content.\r\n"));

            Assert.That(result.Sections[1].Title, Is.EqualTo("References"));
            Assert.That(result.Sections[1].Level, Is.EqualTo(2));
            Assert.That(result.Sections[1].Content,
                Is.EqualTo("Reference content."));
        });
    }

    [Test]
    public void Analyze_NestedHeadings_PreservesHeadingLevels()
    {
        const string text =
            "Introduction.\r\n" +
            "== Career ==\r\n" +
            "Career content.\r\n" +
            "=== Early career ===\r\n" +
            "Early career content.\r\n" +
            "==== Later career ====\r\n" +
            "Later career content.";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.SectionCount, Is.EqualTo(3));

            Assert.That(result.Sections[0].Level, Is.EqualTo(2));
            Assert.That(result.Sections[1].Level, Is.EqualTo(3));
            Assert.That(result.Sections[2].Level, Is.EqualTo(4));

            Assert.That(result.Sections[0].Title, Is.EqualTo("Career"));
            Assert.That(result.Sections[1].Title, Is.EqualTo("Early career"));
            Assert.That(result.Sections[2].Title, Is.EqualTo("Later career"));
        });
    }

    [Test]
    public void Analyze_EmptySection_PreservesEmptyContent()
    {
        const string text =
            "== Empty ==\r\n" +
            "== Content ==\r\n" +
            "Some content.";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.SectionCount, Is.EqualTo(2));
            Assert.That(result.Sections[0].Title, Is.EqualTo("Empty"));
            Assert.That(result.Sections[0].Content, Is.Empty);
            Assert.That(result.Sections[1].Title, Is.EqualTo("Content"));
        });
    }

    [Test]
    public void Analyze_PreservesOriginalTextAndSectionPositions()
    {
        const string text =
            "Lead text.\r\n" +
            "== First ==\r\n" +
            "First section.\r\n" +
            "== Second ==\r\n" +
            "Second section.";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        string reconstructed = string.Concat(
            result.AllSections.Select(section => section.Text));

        Assert.Multiple(() =>
        {
            Assert.That(result.OriginalText, Is.EqualTo(text));
            Assert.That(reconstructed, Is.EqualTo(text));

            Assert.That(result.Sections[0].StartIndex,
                Is.EqualTo(text.IndexOf("== First ==", StringComparison.Ordinal)));

            Assert.That(result.Sections[1].StartIndex,
                Is.EqualTo(text.IndexOf("== Second ==", StringComparison.Ordinal)));

            Assert.That(result.Sections[0].EndIndex,
                Is.EqualTo(result.Sections[1].StartIndex));

            Assert.That(result.Sections[1].EndIndex,
                Is.EqualTo(text.Length));
        });
    }

    [Test]
    public void Analyze_TrailingCategory_RemainsInLastSectionForNow()
    {
        const string text =
            "Introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.SectionCount, Is.EqualTo(1));
            Assert.That(result.Sections[0].Title, Is.EqualTo("References"));

            Assert.That(result.Sections[0].Content,
                Does.Contain("[[Category:Military history]]"));
        });
    }

    [Test]
    public void Analyze_NullArticleText_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ArticleStructureAnalyzer.Analyze(null!),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Analyze_TrailingCategory_RecognizesMetadata()
    {
        const string text =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(1));

            Assert.That(
                result.TrailingMetadata[0].Text,
                Is.EqualTo("[[Category:Military history]]"));

            Assert.That(
                result.TrailingMetadata[0].StartIndex,
                Is.EqualTo(text.IndexOf("[[Category:", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void Analyze_MultipleTrailingCategories_RecognizesAll()
    {
        const string text =
            "Article content.\r\n" +
            "\r\n" +
            "[[Category:Military history]]\r\n" +
            "[[Category:Military personnel|Smith, John]]";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(2));

            Assert.That(
                result.TrailingMetadata[0].Text,
                Is.EqualTo("[[Category:Military history]]"));

            Assert.That(
                result.TrailingMetadata[1].Text,
                Is.EqualTo("[[Category:Military personnel|Smith, John]]"));
        });
    }

    [Test]
    public void Analyze_InlineCategoryLink_IsNotMetadata()
    {
        const string text =
            "See [[:Category:Military history]] for more information.";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.That(result.TrailingMetadata, Is.Empty);
    }

    [Test]
    public void Analyze_CategoryFollowedByContent_IsNotTrailingMetadata()
    {
        const string text =
            "[[Category:Military history]]\r\n" +
            "Additional article content.";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.That(result.TrailingMetadata, Is.Empty);
    }

    [Test]
    public void Analyze_TrailingCategory_ExcludedFromLogicalContent()
    {
        const string text =
            "Introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Sections[0].LogicalContent,
                Is.EqualTo("* Example reference."));

            Assert.That(
                result.Sections[0].Content,
                Does.Contain("[[Category:Military history]]"));

            Assert.That(
                result.TrailingMetadata,
                Has.Count.EqualTo(1));

            Assert.That(
                string.Concat(result.AllSections.Select(s => s.Text)),
                Is.EqualTo(text));
        });
    }

    [Test]
    public void Analyze_LeadOnlyArticle_ExcludesTrailingCategoryFromLogicalContent()
    {
        const string text =
            "Article introduction.\r\n" +
            "\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Lead.LogicalContent,
                Is.EqualTo("Article introduction."));

            Assert.That(
                result.Lead.Content,
                Does.Contain("[[Category:Military history]]"));

            Assert.That(
                result.TrailingMetadata,
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Analyze_TrailingCategory_IdentifiesMetadataType()
    {
        const string text =
            "Article content.\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.That(
            result.TrailingMetadata[0].Type,
            Is.EqualTo(ArticleMetadataType.Category));
    }

    [Test]
    public void Analyze_TrailingDefaultSort_IdentifiesMetadataType()
    {
        const string text =
            "Article content.\r\n" +
            "{{DEFAULTSORT:Smith, John}}";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.That(
            result.TrailingMetadata[0].Type,
            Is.EqualTo(ArticleMetadataType.DefaultSort));
    }

    [Test]
    public void Analyze_WithDefaultOptions_PreservesExistingBehavior()
    {
        const string text =
            "Article content.\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(1));

            Assert.That(
                result.TrailingMetadata[0].Type,
                Is.EqualTo(ArticleMetadataType.Category));
        });
    }

    [Test]
    public void Analyze_WithConfiguredTemplates_PreservesExistingRecognition()
    {
        const string text =
            "Article content.\r\n" +
            "{{DEFAULTSORT:Smith, John}}\r\n" +
            "[[Category:Military history]]";

        ArticleAnalysisOptions options = new();

        options.TrailingMetadataTemplates.Add("Wikipedia");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(2));

            Assert.That(
                result.TrailingMetadata[0].Type,
                Is.EqualTo(ArticleMetadataType.DefaultSort));

            Assert.That(
                result.TrailingMetadata[1].Type,
                Is.EqualTo(ArticleMetadataType.Category));
        });
    }

    [Test]
    public void Analyze_ConfiguredTrailingTemplate_RecognizesMetadata()
    {
        const string text =
            "Article content.\r\n" +
            "{{Wikipedia}}";

        ArticleAnalysisOptions options = new();

        options.TrailingMetadataTemplates.Add("Wikipedia");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(1));

            Assert.That(
                result.TrailingMetadata[0].Type,
                Is.EqualTo(ArticleMetadataType.Template));

            Assert.That(
                result.Lead.LogicalContent,
                Is.EqualTo("Article content."));
        });
    }

    [Test]
    public void Analyze_UnconfiguredTrailingTemplate_RemainsArticleContent()
    {
        const string text =
            "Article content.\r\n" +
            "{{Wikipedia}}";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Is.Empty);

            Assert.That(
                result.Lead.LogicalContent,
                Does.Contain("{{Wikipedia}}"));
        });
    }

    [Test]
    public void Analyze_ConfiguredTemplate_MatchesCaseInsensitively()
    {
        const string text =
            "Article content.\r\n" +
            "{{wIkIpEdIa}}";

        ArticleAnalysisOptions options = new();

        options.TrailingMetadataTemplates.Add("Wikipedia");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.That(result.TrailingMetadata, Has.Count.EqualTo(1));
    }

    [Test]
    public void Analyze_ConfiguredTemplateWithParameters_RecognizesMetadata()
    {
        const string text =
            "Article content.\r\n" +
            "{{Wikipedia|Military history}}";

        ArticleAnalysisOptions options = new();

        options.TrailingMetadataTemplates.Add("Wikipedia");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(1));

            Assert.That(
                result.TrailingMetadata[0].Text,
                Is.EqualTo("{{Wikipedia|Military history}}"));
        });
    }

    [Test]
    public void Analyze_NestedConfiguredTemplate_IsNotRecognized()
    {
        const string text =
            "Article content.\r\n" +
            "{{Wikipedia|source={{Example}}}}";

        ArticleAnalysisOptions options = new();

        options.TrailingMetadataTemplates.Add("Wikipedia");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.That(result.TrailingMetadata, Is.Empty);
    }

    [Test]
    public void Analyze_ConfiguredTemplateFollowedByContent_IsNotTrailingMetadata()
    {
        const string text =
            "== References ==\r\n" +
            "{{Wikipedia}}\r\n" +
            "Additional reference information.";

        ArticleAnalysisOptions options = new();

        options.TrailingMetadataTemplates.Add("Wikipedia");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.TrailingMetadata,
                Is.Empty);

            Assert.That(
                result.Sections[0].LogicalContent,
                Does.Contain("{{Wikipedia}}"));

            Assert.That(
                result.Sections[0].LogicalContent,
                Does.Contain("Additional reference information."));
        });
    }

    [Test]
    public void Analyze_ConfiguredInterwikiLink_RecognizesMetadata()
    {
        const string text =
            "Article content.\r\n" +
            "[[fr:Example article]]";

        ArticleAnalysisOptions options = new();

        options.TrailingInterwikiPrefixes.Add("fr");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(1));

            Assert.That(
                result.TrailingMetadata[0].Type,
                Is.EqualTo(ArticleMetadataType.InterwikiLink));

            Assert.That(
                result.Lead.LogicalContent,
                Is.EqualTo("Article content."));
        });
    }

    [Test]
    public void Analyze_UnconfiguredInterwikiLink_RemainsArticleContent()
    {
        const string text =
            "Article content.\r\n" +
            "[[fr:Example article]]";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Is.Empty);

            Assert.That(
                result.Lead.LogicalContent,
                Does.Contain("[[fr:Example article]]"));
        });
    }

    [Test]
    public void Analyze_VisibleInterwikiLink_IsNotMetadata()
    {
        const string text =
            "Article content.\r\n" +
            "[[:fr:Example article]]";

        ArticleAnalysisOptions options = new();

        options.TrailingInterwikiPrefixes.Add("fr");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.That(result.TrailingMetadata, Is.Empty);
    }

    [Test]
    public void Analyze_MultipleInterwikiLinks_RecognizesAll()
    {
        const string text =
            "Article content.\r\n" +
            "[[fr:Example article]]\r\n" +
            "[[de:Beispielartikel]]";

        ArticleAnalysisOptions options = new();

        options.TrailingInterwikiPrefixes.Add("fr");
        options.TrailingInterwikiPrefixes.Add("de");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.That(result.TrailingMetadata, Has.Count.EqualTo(2));

        Assert.That(
            result.TrailingMetadata.Select(m => m.Type),
            Is.All.EqualTo(ArticleMetadataType.InterwikiLink));
    }

    [Test]
    public void Analyze_InterwikiLinkFollowedByContent_IsNotTrailingMetadata()
    {
        const string text =
            "== References ==\r\n" +
            "[[fr:Example article]]\r\n" +
            "Additional reference information.";

        ArticleAnalysisOptions options = new();

        options.TrailingInterwikiPrefixes.Add("fr");

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Is.Empty);

            Assert.That(
                result.Sections[0].LogicalContent,
                Does.Contain("[[fr:Example article]]"));
        });
    }

    [Test]
    public void Analyze_LogicalContent_PreservesTrailingSpaces()
    {
        const string text =
            "== References ==\r\n" +
            "* Example reference.   \r\n" +
            "[[Category:Military history]]";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.That(
            result.Sections[0].LogicalContent,
            Is.EqualTo("* Example reference.   "));
    }

    [Test]
    public void Analyze_LogicalContent_PreservesTrailingTabs()
    {
        const string text =
            "== References ==\r\n" +
            "* Example reference.\t\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.That(
            result.Sections[0].LogicalContent,
            Is.EqualTo("* Example reference.\t"));
    }

    [Test]
    public void Analyze_LogicalContent_DoesNotModifyOriginalText()
    {
        const string text =
            "Introduction.\r\n" +
            "== References ==\r\n" +
            "* Example reference.   \r\n" +
            "\r\n" +
            "[[Category:Military history]]\r\n";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.OriginalText, Is.EqualTo(text));

            Assert.That(
                string.Concat(result.AllSections.Select(s => s.Text)),
                Is.EqualTo(text));

            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Analyze_TrailingMetadata_ExposesCorrectBoundary()
    {
        const string text =
            "== References ==\r\n" +
            "* Example reference.\r\n" +
            "\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.TrailingMetadataStartIndex,
                Is.EqualTo(text.IndexOf(
                    "[[Category:",
                    StringComparison.Ordinal)));

            Assert.That(
                result.TextBeforeTrailingMetadata,
                Is.EqualTo(
                    "== References ==\r\n" +
                    "* Example reference.\r\n" +
                    "\r\n"));
        });
    }

    [Test]
    public void Analyze_NoTrailingMetadata_BoundaryIsArticleEnd()
    {
        const string text =
            "== References ==\r\n" +
            "* Example reference.";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.TrailingMetadataStartIndex,
                Is.EqualTo(text.Length));

            Assert.That(
                result.TextBeforeTrailingMetadata,
                Is.EqualTo(text));
        });
    }

    [Test]
    public void Analyze_MultipleMetadataDeclarations_PreservesPositions()
    {
        const string text =
            "Article content.\r\n" +
            "\r\n" +
            "{{DEFAULTSORT:Smith, John}}\r\n" +
            "[[Category:Military history]]";

        ArticleStructure result =
            ArticleStructureAnalyzer.Analyze(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrailingMetadata, Has.Count.EqualTo(2));

            Assert.That(
                result.TrailingMetadata[0].StartIndex,
                Is.EqualTo(text.IndexOf(
                    "{{DEFAULTSORT:",
                    StringComparison.Ordinal)));

            Assert.That(
                result.TrailingMetadata[1].StartIndex,
                Is.EqualTo(text.IndexOf(
                    "[[Category:",
                    StringComparison.Ordinal)));

            Assert.That(
                result.TrailingMetadataStartIndex,
                Is.EqualTo(result.TrailingMetadata[0].StartIndex));
        });
    }

    [Test]
    public void Analyze_HeadingBeyondSixEquals_DoesNotThrow()
    {
        const string articleText =
            "== History ==\r\n" +
            "Original history.\r\n" +
            "\r\n" +
            "======= Invalid heading =======\r\n" +
            "Additional content.\r\n" +
            "\r\n" +
            "== References ==\r\n" +
            "Reference content.";

        ArticleStructure result = ArticleStructureAnalyzer.Analyze(articleText);

        Assert.Multiple(() =>
        {
            Assert.That(result.Sections, Has.Count.EqualTo(2));

            Assert.That(result.Sections[0].Title, Is.EqualTo("History"));
            Assert.That(result.Sections[1].Title, Is.EqualTo("References"));

            Assert.That(
                result.Sections[0].Content,
                Does.Contain("======= Invalid heading ======="));

            Assert.That(
                result.Sections[0].Content,
                Does.Contain("Additional content."));
        });
    }
}