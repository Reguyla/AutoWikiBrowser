using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.API;
using Twain.Core.Editing;
using Twain.Core.Parse;
using Twain.Core.Processing;
using Twain.Core.Workspaces;
using Twain.Core.Workspaces.Layouts;
using Twain.Core.Workspaces.Panes;
using Twain.UI.BotSettings;
using Twain.UI.Diff;
using Twain.UI.Editor;
using Twain.UI.Options;
using Twain.UI.ViewModels.Lists;

namespace Twain.UI.ViewModels.Workspaces;

/// <summary>
/// Provides presentation state and commands for the active workspace.
/// </summary>
public sealed partial class WorkspaceViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the title of the article currently loaded in the workspace.
    /// </summary>
    [ObservableProperty]
    private string _currentArticleName = string.Empty;

    /// <summary>
    /// Gets or sets the edit summary used when saving the current article.
    /// </summary>
    [ObservableProperty]
    private string _editSummary = string.Empty;

    /// <summary>
    /// Gets or sets whether the workspace is displaying its options.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingVisible))]
    private bool _isOptionsVisible;

    /// <summary>
    /// Gets whether the primary editing workspace is visible.
    /// </summary>
    public bool IsEditingVisible =>
        !IsOptionsVisible;

    /// <summary>
    /// Displays the primary article editing workspace.
    /// </summary>
    [RelayCommand]
    private void ShowEditing()
    {
        IsOptionsVisible = false;
    }

    /// <summary>
    /// Displays the article processing options.
    /// </summary>
    [RelayCommand]
    private void ShowOptions()
    {
        IsOptionsVisible = true;
    }

    /// <summary>
    /// Gets the bot processing settings for the current workspace.
    /// </summary>
    public BotSettingsViewModel BotSettings { get; } = new();

    /// <summary>
    /// Gets the active wiki session used by the workspace.
    /// </summary>
    public Session Session { get; }

    /// <summary>
    /// Gets the view model for the diff pane.
    /// </summary>
    public DiffViewModel Diff { get; }

    /// <summary>
    /// Gets the article currently loaded for processing.
    /// </summary>
    [ObservableProperty]
    private Article? _currentArticle;

    /// <summary>
    /// Cancels a pending delayed automatic save.
    /// </summary>
    private CancellationTokenSource? _autoSaveCancellation;

    private readonly MainProcess _mainProcess;

    private readonly HideText _removeText =
    new(false, true, true);

    private readonly List<string> _noParse = new();

    private readonly List<string> _noRetf = new();

    private readonly FindandReplace _findAndReplace =
        new();

    private readonly SubstTemplates _substTemplates =
        new();

    private readonly Twain.Core.ReplaceSpecial.ReplaceSpecial
        _replaceSpecial = new();

    /// <summary>
    /// Initializes the standard Twain editing workspace.
    /// </summary>
    public WorkspaceViewModel(
        Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;

        BotSettings.CanUseBotMode =
            Session.IsBot || Session.IsSysop;

        _mainProcess =
            new MainProcess(
                new Parsers());

        Session.OpenComplete +=
            Editor_OpenComplete;

        Session.SaveComplete +=
            Editor_SaveComplete;

        Session.ExceptionCaught +=
            Editor_ExceptionCaught;

        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        ArticleEditingSession editingSession = new(
            """
        This is the original article text.

        Edit this text to produce an updated version.
        """);

        ArticleDocumentViewModel document = new(
            editingSession);

        Editor = new ArticleEditorViewModel(
           document);

        Diff = new DiffViewModel(
           document);

        Diff.GoToLineRequested =
           GoToDiffLineAsync;

        MakeList = new MakeListViewModel(
            Session);

        Options = new OptionsViewModel();

        MakeList.Articles.CollectionChanged +=
            MakeListArticles_CollectionChanged;

        Options.CanStartProcessing =
            MakeList.ArticleCount > 0;

        Options.DisambiguationSourceProvider =
            () => MakeList.SourceText;

        Options.FindNextRequested =
            FindNext;

        Options.StartProcessingRequested =
           StartProcessing;

        Panes =
        [
            CreatePane(
                BuiltInPaneDefinitions.ArticleEditor,
                FindState(
                    layout,
                    BuiltInPaneIds.ArticleEditor),
                        Editor),

            CreatePane(
                BuiltInPaneDefinitions.ArticleList,
                FindState(
                    layout,
                    BuiltInPaneIds.ArticleList),
                MakeList),

            CreatePane(
                BuiltInPaneDefinitions.Options,
                FindState(
                    layout,
                    BuiltInPaneIds.Options),
                Options),

            CreatePane(
                BuiltInPaneDefinitions.Diff,
                FindState(
                    layout,
                    BuiltInPaneIds.Diff),
                Diff)
        ];
    }

    /// <summary>
    /// Begins processing the first article in the current article list.
    /// </summary>
    private void StartProcessing()
    {
        Article? article =
            MakeList.SelectedArticle ??
            MakeList.Articles.FirstOrDefault();

        if (article is null ||
            Session.IsBusy)
        {
            return;
        }

        if (BotSettings.AutoSaveEnabled &&
            (!Session.User.IsLoggedIn ||
             (!Session.IsBot && !Session.IsSysop)))
        {
            BotSettings.AutoSaveEnabled = false;
            return;
        }

        CurrentArticleName =
            article.Name;

        Options.IsProcessing = true;

        Session.Editor.Open(
            article.Name,
            true);
    }

    /// <summary>
    /// Captures the configured dependencies used by the Core article-processing
    /// pipeline.
    /// </summary>
    /// <returns>
    /// The dependencies associated with the current workspace processing state.
    /// </returns>
    private MainProcessDependencies CreateMainProcessDependencies()
    {
        return new MainProcessDependencies
        {
            Skip = Options,
            RemoveText = _removeText,
            NoParse = _noParse,
            FindAndReplace = _findAndReplace,
            SubstTemplates = _substTemplates,
            ReplaceSpecial = _replaceSpecial,
            NoRetf = _noRetf
        };
    }

    /// <summary>
    /// Captures the callbacks used by the Core article-processing pipeline.
    /// </summary>
    /// <returns>
    /// The callbacks associated with the current workspace processing lifecycle.
    /// </returns>
    private MainProcessCallbacks CreateMainProcessCallbacks()
    {
        return new MainProcessCallbacks
        {
            RunExtensionProcessing =
                RunExtensionProcessing,

            ApplyRegexTypoProcessing =
                ApplyRegexTypoProcessing,

            AbortProcessing =
                AbortProcessing,

            HandleProcessingException =
                HandleProcessingException
        };
    }

    /// <summary>
    /// Runs extension processing for the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article being processed.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing should continue.
    /// </returns>
    private bool RunExtensionProcessing(
        Article article)
    {
        // Custom modules, external programs, and plugins are not yet connected
        // to the Twain workspace processing lifecycle.
        return true;
    }

    /// <summary>
    /// Handles the results produced by regular-expression typo processing.
    /// </summary>
    /// <param name="article">
    /// The article that was processed.
    /// </param>
    /// <param name="mainProcess">
    /// Whether processing is part of the normal article workflow.
    /// </param>
    /// <param name="typoStats">
    /// The typo statistics produced by Core processing, when available.
    /// </param>
    private void ApplyRegexTypoProcessing(
        Article article,
        bool mainProcess,
        List<TypoStat>? typoStats)
    {
        // Typo statistics are not yet displayed by the Twain workspace.
        // Core processing has already been applied before this callback runs.
    }

    /// <summary>
    /// Handles completion of a successful article save.
    /// </summary>
    private void Editor_SaveComplete(
        AsyncApiEdit editor,
        SaveInfo saveInfo)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (CurrentArticle is null)
            {
                StopProcessing();
                return;
            }

            int currentIndex =
                MakeList.Articles
                    .Select(
                        (article, index) =>
                            new
                            {
                                article,
                                index
                            })
                    .Where(
                        item =>
                            string.Equals(
                                item.article.Name,
                                CurrentArticle.Name,
                                StringComparison.Ordinal))
                    .Select(item => item.index)
                    .DefaultIfEmpty(-1)
                    .First();

            if (currentIndex < 0 ||
                !MakeList.RemoveArticle(CurrentArticle.Name))
            {
                StopProcessing();
                return;
            }

            Article? nextArticle =
                currentIndex < MakeList.Articles.Count
                    ? MakeList.Articles[currentIndex]
                    : null;

            CurrentArticle = null;
            CurrentArticleName = string.Empty;

            Editor.Document.CurrentText =
                string.Empty;

            if (nextArticle is null)
            {
                StopProcessing();
                return;
            }

            MakeList.SelectedArticle =
                nextArticle;

            StartProcessing();
        });
    }

    /// <summary>
    /// Handles an exception reported by the active API editor.
    /// </summary>
    private void Editor_ExceptionCaught(
        AsyncApiEdit editor,
        Exception exception)
    {
        _autoSaveCancellation?.Cancel();
        _autoSaveCancellation?.Dispose();
        _autoSaveCancellation = null;

        Dispatcher.UIThread.Post(() =>
        {
            ErrorHandler.HandleException(exception);
        });
    }

    /// <summary>
    /// Saves the current article text to the active wiki.
    /// </summary>
    [RelayCommand]
    private void SaveProcessing()
    {
        if (!Options.IsProcessing ||
            CurrentArticle is null ||
            Session.Editor.IsActive)
        {
            return;
        }

        if (!string.Equals(
                CurrentArticle.Name,
                Session.Page.Title,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Attempted to save a page that does not match the active session page.");
        }

        string articleText =
            Editor.Document.CurrentText;

        if (string.IsNullOrEmpty(articleText))
        {
            return;
        }

        Session.Editor.Save(
            articleText,
            Editor.EditSummary,
            Editor.IsMinorEdit,
            WatchOptions.NoChange);
    }

    /// <summary>
    /// Stops the active article-processing workflow.
    /// </summary>
    [RelayCommand]
    private void StopProcessing()
    {
        if (!Options.IsProcessing)
        {
            return;
        }

        _autoSaveCancellation?.Cancel();
        _autoSaveCancellation?.Dispose();
        _autoSaveCancellation = null;

        Session.Editor.Abort();

        Options.IsProcessing = false;
    }

    /// <summary>
    /// Handles a request from the Core processing pipeline to abort the
    /// current article-processing workflow.
    /// </summary>
    private void AbortProcessing()
    {
        StopProcessing();
    }

    /// <summary>
    /// Handles an exception raised by the Core article-processing pipeline.
    /// </summary>
    /// <param name="article">
    /// The article being processed when the exception occurred.
    /// </param>
    /// <param name="exception">
    /// The exception raised by the processing pipeline.
    /// </param>
    private void HandleProcessingException(
        Article article,
        Exception exception)
    {
        ErrorHandler.HandleException(exception);

        string stackTrace =
            exception.StackTrace ?? string.Empty;

        // A regular-expression failure is a processing/configuration problem,
        // rather than a problem with the article itself.
        if (!stackTrace.Contains(
                "System.Text.RegularExpressions",
                StringComparison.Ordinal))
        {
            article.Trace.AWBSkipped(
                "Exception: " + exception.Message);
        }

        AbortProcessing();
    }

    /// <summary>
    /// Waits for the configured bot delay and then saves the current article.
    /// </summary>
    private async Task ScheduleAutoSaveAsync()
    {
        _autoSaveCancellation?.Cancel();
        _autoSaveCancellation?.Dispose();

        _autoSaveCancellation =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            _autoSaveCancellation.Token;

        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(
                    BotSettings.AutoSaveDelay),
                cancellationToken);

            if (cancellationToken.IsCancellationRequested ||
                !BotSettings.AutoSaveEnabled)
            {
                return;
            }

            SaveProcessing();
        }
        catch (OperationCanceledException)
        {
            // A pending automatic save was intentionally cancelled.
        }
    }

    /// <summary>
    /// Skips the current article and continues processing the next article.
    /// </summary>
    [RelayCommand]
    private void SkipProcessing()
    {
        if (!Options.IsProcessing || CurrentArticle is null)
        {
            return;
        }

        _autoSaveCancellation?.Cancel();
        _autoSaveCancellation?.Dispose();
        _autoSaveCancellation = null;

        CurrentArticle.Trace.UserSkipped();

        Session.Editor.Reset();

        if (!MakeList.RemoveArticle(CurrentArticle.Name))
        {
            StopProcessing();
            return;
        }

        CurrentArticle = null;
        CurrentArticleName = string.Empty;

        Editor.Document.CurrentText = string.Empty;

        StartProcessing();
    }

    private void Editor_OpenComplete(
        AsyncApiEdit editor,
        PageInfo page)
    {
        Dispatcher.UIThread.Post(() =>
        {
            CurrentArticle =
                new Article(page);

            CurrentArticleName =
                CurrentArticle.Name;

            Editor.Document.LoadArticle(
                CurrentArticle.ArticleText);

            MainProcessOptions options =
                Options.CreateMainProcessOptions();

            MainProcessDependencies dependencies =
                CreateMainProcessDependencies();

            MainProcessCallbacks callbacks =
                CreateMainProcessCallbacks();

            _mainProcess.ProcessPageCore(
                CurrentArticle,
                true,
                options,
                Session,
                dependencies,
                callbacks);

            Editor.Document.CurrentText =
                CurrentArticle.ArticleText;

            if (BotSettings.AutoSaveEnabled)
            {
                if (BotSettings.AutoSaveDelay == 0)
                {
                    SaveProcessing();
                }
                else
                {
                    _ = ScheduleAutoSaveAsync();
                }
            }
        });
    }

    /// <summary>
    /// Updates processing availability when the article list changes.
    /// </summary>
    private void MakeListArticles_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        Options.CanStartProcessing =
            MakeList.ArticleCount > 0;
    }

    /// <summary>
    /// Requests navigation to the next matching search result in the
    /// active article editor.
    /// </summary>
    private async void FindNext()
    {
        if (Editor.FindNextRequested is null)
        {
            return;
        }

        await Editor.FindNextRequested(
            Options.FindText,
            Options.FindRegex,
            Options.FindCaseSensitive,
            CurrentArticleName);
    }

    /// <summary>
    /// Gets the panes available in the current workspace.
    /// </summary>
    public ObservableCollection<PaneViewModel> Panes { get; }

    /// <summary>
    /// Gets the panes assigned to the left workspace region.
    /// </summary>
    public IEnumerable<PaneViewModel> LeftPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Left);

    /// <summary>
    /// Gets the panes assigned to the center workspace region.
    /// </summary>
    /// <remarks>
    /// The center region is intended for secondary tools such as job options,
    /// configuration panels, and other supporting functionality that complements
    /// the primary editing experience.
    /// </remarks>
    public IEnumerable<PaneViewModel> CenterPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Center);

    /// <summary>
    /// Gets the panes assigned to the primary document region.
    /// </summary>
    public IEnumerable<PaneViewModel> DocumentPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Document);

    /// <summary>
    /// Gets the panes assigned to the lower results region.
    /// </summary>
    /// <remarks>
    /// The bottom region is intended for supporting output such as diffs,
    /// previews, validation results, and other information related to the
    /// active document.
    /// </remarks>
    public IEnumerable<PaneViewModel> BottomPanes =>
        Panes.Where(
            pane =>
                pane.Definition.PreferredRegion ==
                WorkspaceRegion.Bottom);

    /// <summary>
    /// Shows all panes in the current workspace.
    /// </summary>
    [RelayCommand]
    private void ShowAllPanes()
    {
        foreach (PaneViewModel pane in Panes)
        {
            pane.IsVisible = true;
        }
    }

    /// <summary>
    /// Hides the specified pane.
    /// </summary>
    /// <param name="pane">The pane to hide.</param>
    [RelayCommand]
    private static void HidePane(PaneViewModel? pane)
    {
        if (pane is not null)
        {
            pane.IsVisible = false;
        }
    }

    /// <summary>
    /// Finds the initial state associated with a pane definition.
    /// </summary>
    /// <param name="layout">
    /// The workspace layout containing the pane state.
    /// </param>
    /// <param name="definitionId">
    /// The stable identifier of the pane definition to locate.
    /// </param>
    /// <returns>
    /// The pane state associated with the specified definition.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the layout does not contain exactly one matching pane state.
    /// </exception>
    private static PaneState FindState(
        WorkspaceLayout layout,
        PaneId definitionId)
    {
        return layout.Panes.Single(
            pane => pane.DefinitionId == definitionId);
    }

    /// <summary>
    /// Creates presentation state for a defined workspace pane.
    /// </summary>
    /// <param name="definition">
    /// The pane definition.
    /// </param>
    /// <param name="state">
    /// The initial workspace state of the pane.
    /// </param>
    /// <param name="content">
    /// The view model or temporary content displayed within the pane.
    /// </param>
    /// <returns>
    /// The corresponding pane view model.
    /// </returns>
    private static PaneViewModel CreatePane(
        PaneDefinition definition,
        PaneState state,
        object content)
    {
        return new PaneViewModel(
            definition,
            state,
            content);
    }

    /// <summary>
    /// Gets the view model for the article-list pane.
    /// </summary>
    public MakeListViewModel MakeList { get; }

    /// <summary>
    /// Gets the view model for the options pane.
    /// </summary>
    public OptionsViewModel Options { get; }

    /// <summary>
    /// Gets the view model for the article editor pane.
    /// </summary>
    public ArticleEditorViewModel Editor { get; }

    /// <summary>
    /// Navigates the article editor to a line selected in the diff.
    /// </summary>
    private Task GoToDiffLineAsync(
        int line)
    {
        if (Editor.GoToLineRequested is null)
        {
            return Task.CompletedTask;
        }

        return Editor.GoToLineRequested(line);
    }
}