using Twain.Core.Parse;
using Twain.Core.Plugin;

namespace Twain.Core.Processing;

/// <summary>
/// Coordinates article-processing operations that are independent of the
/// application user interface.
/// </summary>
public sealed class MainProcess
{
    private readonly Parsers _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainProcess"/> class.
    /// </summary>
    /// <param name="parser">
    /// The parser used by article-processing operations.
    /// </param>
    public MainProcess(Parsers parser)
    {
        _parser = parser;
    }

    /// <summary>
    /// Applies the configured image or file replacement operation to the supplied
    /// article.
    /// </summary>
    /// <param name="article">
    /// The article to update.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when the operation skips the article.
    /// </returns>
    public static bool ApplyImageChanges(
        Article article,
        MainProcessOptions options)
    {
        if (options.ImageOperation == 0)
        {
            return true;
        }

        article.UpdateImages(
            (Twain.Core.Options.ImageReplaceOptions)options.ImageOperation,
            options.ImageReplace,
            options.ImageWith,
            options.SkipIfNoImageChange);

        return !article.SkipArticle;
    }

    /// <summary>
    /// Applies the configured categorization operation to the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to update.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when categorization skips the article.
    /// </returns>
    public bool ApplyCategorisationChanges(
        Article article,
        MainProcessOptions options)
    {
        return article.ApplyCategorisationChanges(
            (Twain.Core.Options.CategorisationOptions)
                options.CategorisationOperation,
            _parser,
            options.SkipIfNoCategoryChange,
            options.NewCategory,
            options.NewCategory2,
            options.RemoveCategorySortKey,
            options.GeneralFixesEnabled);
    }

    /// <summary>
    /// Applies the configured append or prepend operation to the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to update.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    public void ApplyAppendOrPrependText(
        Article article,
        MainProcessOptions options)
    {
        if (!options.AppendEnabled)
        {
            return;
        }

        article.ApplyAppendOrPrependText(
            options.AppendText,
            options.AppendNewLineCount,
            options.AppendInsteadOfPrepend,
            options.SortMetadataAfterAppend,
            _parser);
    }

    /// <summary>
    /// Applies whole-article Unicode conversion when standard processing and the
    /// corresponding processing option are enabled.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="applyStandardProcessing">
    /// <see langword="true"/> when the article is eligible for standard parsing
    /// operations; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="skip">
    /// The skip options used by article processing.
    /// </param>
    /// <param name="removeText">
    /// The text-hiding helper used while Unicode conversion is performed.
    /// </param>
    public void ApplyWholeArticleUnicodify(
        Article article,
        bool applyStandardProcessing,
        MainProcessOptions options,
        ISkipOptions skip,
        HideText removeText)
    {
        if (!applyStandardProcessing ||
            !options.UnicodifyWholeArticle)
        {
            return;
        }

        article.Unicodify(
            skip.SkipNoUnicode,
            _parser,
            removeText);

        Variables.Profiler.Profile("Unicodify");
    }
}