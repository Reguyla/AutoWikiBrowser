using Twain.Core.Disambiguation;
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

    private bool _templateRedirectsLoaded;

    private bool _datedTemplatesLoaded;

    private bool _renamedTemplateParametersLoaded;

    private bool _userTalkWarningsLoaded;
    private Regex? _userTalkTemplatesRegex;

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
        if (options.ImageOperation == ImageReplaceOptions.NoAction)
        {
            return true;
        }

        article.UpdateImages(
            options.ImageOperation,
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

    /// <summary>
    /// Applies automatic article tagging when enabled.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="mainProcess">
    /// <see langword="true"/> when processing is part of the normal save workflow;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="skip">
    /// The skip options used by article processing.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when the article should be skipped.
    /// </returns>
    public bool ApplyAutoTagging(
        Article article,
        bool mainProcess,
        MainProcessOptions options,
        ISkipOptions skip)
    {
        if (!options.AutoTaggerEnabled)
        {
            return true;
        }

        article.AutoTag(
            _parser,
            skip.SkipNoTag,
            options.RestrictOrphanTagging);

        return !(mainProcess && article.SkipArticle);
    }

    /// <summary>
    /// Runs the configured find-and-replace processing for the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="mainProcess">
    /// <see langword="true"/> when processing is part of the normal save workflow;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="findAndReplace">
    /// The configured find-and-replace processor.
    /// </param>
    /// <param name="substTemplates">
    /// The configured template-substitution processor.
    /// </param>
    /// <param name="replaceSpecial">
    /// The configured advanced replacement processor.
    /// </param>
    /// <param name="onlyApplyAfter">
    /// <see langword="false"/> to run the before-general-fixes pass;
    /// <see langword="true"/> to run the after-general-fixes pass.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when the article has been skipped.
    /// </returns>
    public static bool ApplyFindAndReplace(
        Article article,
        bool mainProcess,
        MainProcessOptions options,
        FindandReplace findAndReplace,
        SubstTemplates substTemplates,
        ReplaceSpecial.ReplaceSpecial replaceSpecial,
        bool onlyApplyAfter)
    {
        if (!options.FindAndReplaceEnabled)
        {
            return true;
        }

        article.PerformFindAndReplace(
            findAndReplace,
            substTemplates,
            replaceSpecial,
            mainProcess && options.SkipWhenNoFindAndReplace,
            mainProcess && options.SkipOnlyMinorFindAndReplace,
            onlyApplyAfter);

        article.DoFaRSkips(findAndReplace);

        Variables.Profiler.Profile(
            onlyApplyAfter
                ? "F&R (2nd)"
                : "F&R");

        return !article.SkipArticle;
    }

    /// <summary>
    /// Applies regular-expression typo fixes to the supplied article when enabled
    /// and returns the resulting typo statistics.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="regexTypos">
    /// The configured regular-expression typo processor.
    /// </param>
    /// <param name="noRetf">
    /// The collection of article titles excluded from regular-expression typo fixing.
    /// </param>
    /// <returns>
    /// The typo statistics produced by the processing pass, or
    /// <see langword="null"/> when typo processing did not run or produced no
    /// statistics.
    /// </returns>
    public static List<TypoStat> ApplyRegexTypoFixes(
        Article article,
        MainProcessOptions options,
        RegExTypoFix regexTypos,
        IReadOnlyCollection<string> noRetf)
    {
        if (!options.RegexTypoFixEnabled ||
            regexTypos == null ||
            options.BotMode ||
            Namespace.IsTalk(article.NameSpaceKey))
        {
            return null;
        }

        if (!noRetf.Contains(article.Name))
        {
            article.PerformTypoFixes(
                regexTypos,
                options.SkipIfNoRegexTypo);

            Variables.Profiler.Profile("Typos");

            return regexTypos.GetStatistics();
        }

        if (options.SkipIfNoRegexTypo)
        {
            article.Trace.AWBSkipped(
                "No typo fixes (Title blacklisted from RegExTypoFix Typo Fixing)");
        }

        return null;
    }

    /// <summary>
    /// Applies the configured general fixes to the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="skip">
    /// The skip options used by general-fix processing.
    /// </param>
    /// <param name="removeText">
    /// The text-hiding helper used while general fixes are performed.
    /// </param>
    public void ApplyGeneralFixes(
        Article article,
        MainProcessOptions options,
        ISkipOptions skip,
        HideText removeText)
    {
        article.PerformGeneralFixes(
            _parser,
            removeText,
            skip,
            options.ReplaceReferenceTags,
            options.RestrictDefaultSortChanges,
            options.NoMosComplianceFixes);
    }

    /// <summary>
    /// Applies the supported talk-page general fixes to the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="removeText">
    /// The text-hiding helper used while talk-page fixes are performed.
    /// </param>
    /// <param name="userTalkTemplatesRegex">
    /// The configured user-talk template expression, when available.
    /// </param>
    /// <param name="skipNoUserTalkTemplatesSubstd">
    /// <see langword="true"/> when the article should be skipped when no configured
    /// user-talk template substitutions are made.
    /// </param>
    public static void ApplyTalkGeneralFixes(
        Article article,
        HideText removeText,
        Regex userTalkTemplatesRegex,
        bool skipNoUserTalkTemplatesSubstd)
    {
        if (article.NameSpaceKey == Namespace.UserTalk)
        {
            article.PerformUserTalkGeneralFixes(
                removeText,
                userTalkTemplatesRegex,
                skipNoUserTalkTemplatesSubstd);

            return;
        }

        if (article.CanDoTalkGeneralFixes)
        {
            article.PerformTalkGeneralFixes(removeText);
        }
    }

    /// <summary>
    /// Applies the initial article-processing checks and determines whether standard
    /// processing may be performed.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="session">
    /// The active wiki session.
    /// </param>
    /// <param name="noParse">
    /// The collection of article titles excluded from standard processing.
    /// </param>
    /// <param name="applyStandardProcessing">
    /// When this method returns, contains <see langword="true"/> when standard
    /// processing may continue for the article; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the processing pipeline may continue; otherwise,
    /// <see langword="false"/> when the article has been skipped.
    /// </returns>
    public bool ApplyInitialProcessing(
        Article article,
        MainProcessOptions options,
        Session session,
        ICollection<string> noParse,
        out bool applyStandardProcessing)
    {
        article.AWBChangeArticleText(
            "Fixes for Unicode compatibility",
            _parser.FixUnicode(article.ArticleText),
            true);

        applyStandardProcessing =
            !noParse.Contains(article.Name);

        if (!options.IgnoreNoBots &&
            !Parsers.CheckNoBots(
                article.ArticleText,
                session.User.Name))
        {
            article.AWBSkip(
                "Restricted by {{bots}}/{{nobots}}");

            return false;
        }

        Variables.Profiler.Profile(
            "Initial skip checks");

        return true;
    }

    /// <summary>
    /// Applies universal general fixes to the supplied article when enabled.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    public static void ApplyUniversalGeneralFixes(
        Article article,
        MainProcessOptions options)
    {
        if (!options.GeneralFixesEnabled)
        {
            return;
        }

        article.PerformUniversalGeneralFixes();

        Variables.Profiler.Profile(
            "Universal Genfixes");
    }

    /// <summary>
    /// Applies the general-fix processing path appropriate for the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="mainProcess">
    /// <see langword="true"/> when processing is part of the normal save workflow;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="skip">
    /// The skip options used by article processing.
    /// </param>
    /// <param name="removeText">
    /// The text-hiding helper used during processing.
    /// </param>
    /// <param name="userTalkTemplatesRegex">
    /// The configured user-talk template expression, when available.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when automatic tagging skips the article.
    /// </returns>
    public bool ApplyGeneralFixProcessing(
        Article article,
        bool mainProcess,
        MainProcessOptions options,
        ISkipOptions skip,
        HideText removeText,
        Regex userTalkTemplatesRegex)
    {
        ApplyUniversalGeneralFixes(
            article,
            options);

        if (article.CanDoGeneralFixes)
        {
            if (options.GeneralFixesEnabled)
            {
                ApplyGeneralFixes(
                    article,
                    options,
                    skip,
                    removeText);
            }

            Variables.Profiler.Profile("Mainspace Genfixes");

            if (!ApplyAutoTagging(
                    article,
                    mainProcess,
                    options,
                    skip))
            {
                return false;
            }

            Variables.Profiler.Profile("Auto-tagger");

            return true;
        }

        if (options.GeneralFixesEnabled)
        {
            ApplyTalkGeneralFixes(
                article,
                removeText,
                userTalkTemplatesRegex,
                skip.SkipNoUserTalkTemplatesSubstd);

            Variables.Profiler.Profile("Talk Genfixes");
        }

        return true;
    }

    /// <summary>
    /// Fully processes a page using the supplied processing configuration and
    /// processing dependencies.
    /// </summary>
    /// <param name="article">
    /// The page to process.
    /// </param>
    /// <param name="mainProcess">
    /// <see langword="true"/> when the page is being processed for the normal save
    /// workflow; otherwise, <see langword="false"/> for reparsing, prefetching, and
    /// similar operations.
    /// </param>
    /// <param name="options">
    /// The processing options captured when processing began.
    /// </param>
    /// <param name="session">
    /// The active wiki session used during article processing.
    /// </param>
    /// <param name="dependencies">
    /// The configured processing dependencies used throughout the article-processing
    /// pipeline.
    /// </param>
    /// <param name="callbacks">
    /// The application-owned callbacks used by the article-processing pipeline.
    /// </param>
    public void ProcessPageCore(
        Article article,
        bool mainProcess,
        MainProcessOptions options,
        Session session,
        MainProcessDependencies dependencies,
        MainProcessCallbacks callbacks)
    {
        Variables.Profiler.Start(
            "ProcessPage(\"" + article.Name + "\")");

        try
        {
            if (!ApplyInitialProcessing(
                    article,
                    options,
                    session,
                    dependencies.NoParse,
                    out bool process))
            {
                return;
            }

            if (!callbacks.RunExtensionProcessing(article))
            {
                return;
            }

            ApplyWholeArticleUnicodify(
                article,
                process,
                options,
                dependencies.Skip,
                dependencies.RemoveText);

            if (!ApplyFindAndReplace(
                    article,
                    mainProcess,
                    options,
                    dependencies.FindAndReplace,
                    dependencies.SubstTemplates,
                    dependencies.ReplaceSpecial,
                    false))
            {
                return;
            }

            if (!ApplyCategorisationChanges(
                    article,
                    options))
            {
                return;
            }

            Variables.Profiler.Profile("Categories");

            if (process)
            {
                if (article.CanDoGeneralFixes &&
                    options.GeneralFixesEnabled &&
                    !_templateRedirectsLoaded)
                {
                    LoadTemplateRedirects(session);

                    Variables.Profiler.Profile(
                        "LoadTemplateRedirects");
                }

                if (article.CanDoGeneralFixes &&
                    options.GeneralFixesEnabled &&
                    !_datedTemplatesLoaded)
                {
                    LoadDatedTemplates(session);

                    Variables.Profiler.Profile(
                        "LoadDatedTemplates");
                }

                if (article.CanDoGeneralFixes &&
                    options.GeneralFixesEnabled &&
                    !_renamedTemplateParametersLoaded)
                {
                    LoadRenameTemplateParameters(session);

                    Variables.Profiler.Profile(
                        "LoadRenameTemplateParameters");
                }

                if (options.GeneralFixesEnabled &&
                    article.NameSpaceKey == Namespace.UserTalk &&
                    !_userTalkWarningsLoaded)
                {
                    LoadUserTalkWarnings(session);

                    Variables.Profiler.Profile(
                        "loadUserTalkWarnings");
                }

                if (!ApplyGeneralFixProcessing(
                        article,
                        mainProcess,
                        options,
                        dependencies.Skip,
                        dependencies.RemoveText,
                        _userTalkTemplatesRegex))
                {
                    return;
                }
            }

            callbacks.ApplyRegexTypoProcessing(
                article,
                mainProcess,
                options);

            // Find and replace after general fixes.
            // Do not apply skip checks when reparsing.
            if (!ApplyFindAndReplace(
                    article,
                    mainProcess,
                    options,
                    dependencies.FindAndReplace,
                    dependencies.SubstTemplates,
                    dependencies.ReplaceSpecial,
                    true))
            {
                return;
            }

            ApplyAppendOrPrependText(
                article,
                options);

            Variables.Profiler.Profile("Append Text");

            if (!ApplyImageChanges(
                    article,
                    options))
            {
                return;
            }

            Variables.Profiler.Profile("Files");

            if (!DisambiguationProcessor.ApplyDisambiguation(
                    article,
                    options,
                    session))
            {
                callbacks.AbortProcessing();
                return;
            }

            Variables.Profiler.Profile("Disambiguate");
        }
        catch (Exception ex)
        {
            callbacks.HandleProcessingException(
                article,
                ex);
        }
        finally
        {
            Variables.Profiler.Flush();
        }
    }

    /// <summary>
    /// Invalidates wiki-specific processing resources so they will be
    /// reloaded for the active project when next required.
    /// </summary>
    public void ResetProjectResources()
    {
        _userTalkWarningsLoaded = false;
        _userTalkTemplatesRegex = null;

        _templateRedirectsLoaded = false;
        _datedTemplatesLoaded = false;
        _renamedTemplateParametersLoaded = false;
    }

    /// <summary>
    /// Loads the configured user-talk templates and builds the regular
    /// expression used by general-fix processing.
    /// </summary>
    /// <param name="session">
    /// The active wiki session.
    /// </param>
    private void LoadUserTalkWarnings(
        Session session)
    {
        Regex userTalkTemplate = new(
            @"# ?\[\[" +
            Variables.NamespacesCaseInsensitive[Namespace.Template] +
            @"(.*?)\]\]");

        _userTalkTemplatesRegex = null;
        _userTalkWarningsLoaded = true;

        try
        {
            string text =
                LoadWikiConfigurationText(
                    session,
                    "Project:AutoWikiBrowser/User talk templates",
                    "LoadUserTalkWarnings",
                    "Unable to load user talk templates: ");

            if (text.Length == 0)
            {
                return;
            }

            List<string> userTalkTemplates =
                Parsers.ParseUserTalkTemplates(
                    text,
                    userTalkTemplate);

            if (userTalkTemplates.Any())
            {
                _userTalkTemplatesRegex =
                    Tools.NestedTemplateRegex(
                        userTalkTemplates);
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
            _userTalkWarningsLoaded = false;
        }
    }

    /// <summary>
    /// Determines whether the processed article should be skipped based on the
    /// kinds of changes made during processing.
    /// </summary>
    /// <param name="article">
    /// The processed article.
    /// </param>
    /// <param name="options">
    /// The processing options captured from the current application state.
    /// </param>
    /// <returns>
    /// The reason the article should be skipped, or <see langword="null"/> when
    /// processing should continue.
    /// </returns>
    public static string? GetArticleChangeSkipReason(
        Article article,
        MainProcessOptions options)
    {
        if ((options.SkipNoChanges || options.BotMode) &&
            article.NoArticleTextChanged)
        {
            return "No change";
        }

        if (options.SkipWhitespaceChanges &&
            options.SkipCasingChanges &&
            article.OnlyWhiteSpaceAndCasingChanged)
        {
            return "Only whitespace/casing changed";
        }

        if (options.SkipWhitespaceChanges &&
            article.OnlyWhiteSpaceChanged)
        {
            return "Only whitespace changed";
        }

        if (options.SkipCasingChanges &&
            article.OnlyCasingChanged)
        {
            return "Only casing changed";
        }

        if (options.SkipMinorGeneralFixChanges &&
            options.GeneralFixesEnabled &&
            article.OnlyMinorGeneralFixesChanged)
        {
            return "Only minor general fix changes";
        }

        if (options.SkipGeneralFixChanges &&
            options.GeneralFixesEnabled &&
            article.OnlyGeneralFixesChanged)
        {
            return "Only general fix changes";
        }

        if (options.SkipPagesWithNoLinks &&
            !WikiRegexes.WikiLinksOnly.IsMatch(
                article.ArticleText))
        {
            return "Page contains no links";
        }

        if (options.SkipCosmeticChanges &&
            (article.NoArticleTextChanged ||
             article.OnlyCosmeticChanged))
        {
            return "Only cosmetic changes made";
        }

        return null;
    }

    /// <summary>
    /// Loads the configured template redirects used by the general-fix
    /// processing pipeline.
    /// </summary>
    /// <param name="session">
    /// The active wiki session.
    /// </param>
    private void LoadTemplateRedirects(
        Session session)
    {
        _templateRedirectsLoaded = true;

        string text =
            LoadWikiConfigurationText(
                session,
                "Project:AutoWikiBrowser/Template redirects",
                "LoadTemplateRedirects",
                "Unable to load template redirects: ");

        // Always update the parser state, even when no text was loaded.
        // This clears redirects left over from a previously selected project.
        WikiRegexes.TemplateRedirects =
            Parsers.LoadTemplateRedirects(text);
    }

    /// <summary>
    /// Loads the configured dated-template definitions used by the
    /// general-fix processing pipeline.
    /// </summary>
    /// <param name="session">
    /// The active wiki session.
    /// </param>
    private void LoadDatedTemplates(
        Session session)
    {
        _datedTemplatesLoaded = true;

        string text =
            LoadWikiConfigurationText(
                session,
                "Project:AutoWikiBrowser/Dated templates",
                "LoadDatedTemplates",
                "Unable to load dated templates: ");

        if (text.Length > 0)
        {
            WikiRegexes.DatedTemplates =
                Parsers.LoadDatedTemplates(text);
        }
    }

    /// <summary>
    /// Loads renamed template-parameter definitions used by the
    /// general-fix processing pipeline.
    /// </summary>
    /// <param name="session">
    /// The active wiki session.
    /// </param>
    private void LoadRenameTemplateParameters(
        Session session)
    {
        _renamedTemplateParametersLoaded = true;

        string text =
            LoadWikiConfigurationText(
                session,
                "Project:AutoWikiBrowser/Rename template parameters",
                "LoadRenameTemplateParameters",
                "Unable to load renamed template parameters: ");

        if (text.Length > 0)
        {
            WikiRegexes.RenamedTemplateParameters =
                Parsers.LoadRenamedTemplateParameters(text);
        }
    }

    /// <summary>
    /// Loads configuration text from the specified wiki page.
    /// </summary>
    /// <param name="session">
    /// The active wiki session.
    /// </param>
    /// <param name="pageTitle">
    /// The wiki page containing the configuration text.
    /// </param>
    /// <param name="logSource">
    /// The source name used for debug logging.
    /// </param>
    /// <param name="errorMessage">
    /// The message prefix used when loading fails.
    /// </param>
    /// <returns>
    /// The loaded configuration text, or an empty string when the page
    /// cannot be loaded.
    /// </returns>
    private static string LoadWikiConfigurationText(
        Session session,
        string pageTitle,
        string logSource,
        string errorMessage)
    {
        try
        {
            return session.Editor
                .SynchronousEditor
                .Clone()
                .Open(
                    pageTitle,
                    true);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                logSource,
                errorMessage + ex.Message);

            return string.Empty;
        }
    }
}