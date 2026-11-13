using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.Alerts;
using Twain.Core.API;
using Twain.Core.AWBSettings;
using Twain.Core.CustomModules;
using Twain.Core.Editing;
using Twain.Core.EditSummaries;
using Twain.Core.Parse;
using Twain.Core.Plugin;
using Twain.Core.Processing;
using Twain.Core.Workspaces;
using Twain.Core.Workspaces.Layouts;
using Twain.Core.Workspaces.Panes;
using Twain.UI.BotSettings;
using Twain.UI.Controls;
using Twain.UI.Diff;
using Twain.UI.Editor;
using Twain.UI.Options;
using Twain.UI.ViewModels.Lists;

namespace Twain.UI.ViewModels.Workspaces;

/// <summary>
/// Provides presentation state and commands for the active workspace.
/// </summary>
public partial class WorkspaceViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the title of the article currently loaded in the workspace.
    /// </summary>
    [ObservableProperty]
    private string _currentArticleName = string.Empty;

    /// <summary>
    /// Whether navigation is using its compact layout.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNavigationExpanded))]
    [NotifyPropertyChangedFor(nameof(ShowNavigationIcons))]
    [NotifyPropertyChangedFor(nameof(ShowNavigationText))]
    private bool _isNavigationCollapsed;

    private string? _lastNavigatedAlert;

    private int _lastAlertLocationIndex = -1;

    private string? _alertEvaluatedText;

    /// <summary>
    /// Identifies how an article completed processing.
    /// </summary>
    private enum ArticleCompletionKind
    {
        Saved,
        Skipped,
        PreparseRetained,
        PreparseRemoved,
        PreparseError
    }

    /// <summary>
    /// Resets alert navigation when the article or its
    /// detected issues change.
    /// </summary>
    private void ResetAlertNavigation()
    {
        _lastNavigatedAlert = null;
        _lastAlertLocationIndex = -1;
    }


    /// <summary>
    /// Whether navigation is currently expanded.
    /// </summary>
    public bool IsNavigationExpanded => !IsNavigationCollapsed;

    /// <summary>
    /// Controls how navigation items display icons and text.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNavigationIcons))]
    [NotifyPropertyChangedFor(nameof(ShowNavigationText))]
    private DisplayMode _navigationDisplayMode =
        DisplayMode.IconsAndText;

    /// <summary>
    /// Whether navigation icons should be visible.
    /// Collapsed navigation always displays icons.
    /// </summary>
    public bool ShowNavigationIcons =>
        IsNavigationCollapsed ||
        NavigationDisplayMode != DisplayMode.TextOnly;

    /// <summary>
    /// Whether navigation labels should be visible.
    /// Collapsed navigation always hides labels.
    /// </summary>
    public bool ShowNavigationText =>
        !IsNavigationCollapsed &&
        NavigationDisplayMode != DisplayMode.IconsOnly;

    [RelayCommand]
    private void ToggleNavigation()
    {
        IsNavigationCollapsed = !IsNavigationCollapsed;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingVisible))]
    [NotifyPropertyChangedFor(nameof(IsEditingSelected))]
    private bool _isCustomModulesVisible;

    /// <summary>
    /// Gets or sets the edit summary used when saving the current article.
    /// </summary>
    [ObservableProperty]
    private string _editSummary = string.Empty;

    /// <summary>
    /// Gets the most recent article alert evaluation,
    /// including detected issue locations.
    /// </summary>
    public ArticleAlertResult? CurrentAlertResult { get; private set; }

    /// <summary>
    /// Gets the number of successfully saved edits in this session.
    /// </summary>
    public int TotalEdits => _sessionCounters.NumberOfEdits;

    /// <summary>
    /// Gets or sets whether the workspace is displaying its options.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingVisible))]
    [NotifyPropertyChangedFor(nameof(IsEditingSelected))]
    private bool _isOptionsVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingVisible))]
    [NotifyPropertyChangedFor(nameof(IsEditingSelected))]
    private bool _isBotSettingsVisible;

    /// <summary>
    /// Gets or sets whether the workspace is displaying profiles.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingVisible))]
    [NotifyPropertyChangedFor(nameof(IsEditingSelected))]
    private bool _isProfilesVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingVisible))]
    [NotifyPropertyChangedFor(nameof(IsEditingSelected))]
    private bool _isPreferencesVisible;

    /// <summary>
    /// Gets whether the primary editing workspace is visible.
    /// </summary>
    public bool IsEditingVisible =>
        !IsProfilesVisible &&
        !IsPreferencesVisible &&
        !IsBotSettingsVisible &&
        !IsCustomModulesVisible;

    /// <summary>Whether Editing should be highlighted in navigation.</summary>
    public bool IsEditingSelected => IsEditingVisible && !IsOptionsVisible;

    [RelayCommand]
    private void ShowBotSettings()
    {
        IsOptionsVisible = false;
        IsProfilesVisible = false;
        IsPreferencesVisible = false;
        IsBotSettingsVisible = true;
        IsCustomModulesVisible = false;
    }

    /// <summary>
    /// Displays the primary article editing workspace.
    /// </summary>
    [RelayCommand]
    private void ShowEditing()
    {
        IsOptionsVisible = false;
        IsProfilesVisible = false;
        IsPreferencesVisible = false;
        IsBotSettingsVisible = false;
        IsCustomModulesVisible = false;
    }

    /// <summary>
    /// Gets the application preferences shared with the shell.
    /// </summary>
    public UserPrefs Preferences { get; }

    /// <summary>
    /// Displays the article processing options.
    /// </summary>
    [RelayCommand]
    private void ShowOptions()
    {
        IsProfilesVisible = false;
        IsPreferencesVisible = false;
        IsOptionsVisible = true;
        IsBotSettingsVisible = false;
        IsCustomModulesVisible = false;
    }

    /// <summary>
    /// Displays the profiles workspace.
    /// </summary>
    [RelayCommand]
    private void ShowProfiles()
    {
        IsOptionsVisible = false;
        IsPreferencesVisible = false;
        IsProfilesVisible = true;
        IsBotSettingsVisible = false;
        IsCustomModulesVisible = false;
    }

    [RelayCommand]
    private void ShowPreferences()
    {
        IsOptionsVisible = false;
        IsProfilesVisible = false;
        IsPreferencesVisible = true;
        IsBotSettingsVisible = false;
        IsCustomModulesVisible = false;
    }

    /// <summary>
    /// Displays the embedded Custom Modules editor.
    /// </summary>
    [RelayCommand]
    private void ShowCustomModules()
    {
        IsOptionsVisible = false;
        IsProfilesVisible = false;
        IsPreferencesVisible = false;
        IsBotSettingsVisible = false;

        IsCustomModulesVisible = true;
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
    /// Holds the active custom module and its editor state
    /// for the lifetime of the workspace.
    /// </summary>
    public CustomModuleState CustomModuleState { get; } = new();

    /// <summary>
    /// Gets the application host exposed to custom modules.
    /// </summary>
    public ITwainHost TwainHost { get; }

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
    /// Tracks activity completed during the current processing session.
    /// </summary>
    private readonly SessionCounters _sessionCounters = new();

    /// <summary>
    /// The original Make List entry currently being processed.
    /// This may differ from CurrentArticle when a redirect is resolved.
    /// </summary>
    private Article? _activeQueueArticle;

    /// <summary>
    /// Snapshot of the article-list entries scheduled for the
    /// current Preparse run.
    /// </summary>
    private List<Article>? _preparseQueue;

    /// <summary>
    /// Position of the next article in the Preparse snapshot.
    /// </summary>
    private int _preparseQueueIndex;

    /// <summary>
    /// Indicates whether an unattended Preparse run is active.
    /// </summary>
    private bool IsPreparseRunning => _preparseQueue is not null;

    private Article? _pendingSaveArticle;

    /// <summary>
    /// Identifies the Save operation associated with the pending article.
    /// </summary>
    private long _pendingSaveOperationId;

    /// <summary>
    /// Cancels the pending nudge timer for the current article.
    /// </summary>
    private CancellationTokenSource? _nudgeCancellation;

    /// <summary>
    /// Requests confirmation before performing the configured
    /// after-processing action.
    /// </summary>
    public Func<BotShutdownAction, Task<bool>>?
        ConfirmShutdownRequested
    { get; set; }

    /// <summary>
    /// Gets or sets whether the upper document region
    /// is displaying the rendered article preview.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDiffVisible))]
    private bool _isPreviewVisible;

    /// <summary>
    /// Gets whether the upper document region
    /// should display the diff viewer.
    /// </summary>
    public bool IsDiffVisible => !IsPreviewVisible;

    /// <summary>
    /// Switches the upper document region to the diff viewer.
    /// </summary>
    [RelayCommand]
    private void ShowDiff()
    {
        IsPreviewVisible = false;
    }

    /// <summary>
    /// Gets or sets the rendered HTML returned by the
    /// MediaWiki preview API.
    /// </summary>
    [ObservableProperty]
    private string _previewHtml = string.Empty;

    /// <summary>
    /// Gets or sets whether a preview request is running.
    /// </summary>
    [ObservableProperty]
    private bool _isPreviewLoading;

    /// <summary>
    /// Gets or sets a message describing a preview failure.
    /// </summary>
    [ObservableProperty]
    private string _previewError = string.Empty;

    /// <summary>
    /// Gets the number of articles skipped during this session.
    /// </summary>
    public int SkippedArticles => _sessionCounters.NumberOfIgnoredEdits;

    /// <summary>
    /// Gets the number of pages processed during the last minute.
    /// </summary>
    public int PagesPerMinute =>
        _sessionCounters.NumberOfPagesPerMinute;

    /// <summary>
    /// Gets the number of edits completed during the last minute.
    /// </summary>
    public int EditsPerMinute =>
        _sessionCounters.NumberOfEditsPerMinute;

    /// <summary>
    /// Updates the displayed session rates once per minute.
    /// </summary>
    private readonly DispatcherTimer _statisticsTimer = new();

    /// <summary>
    /// Requests a rendered preview of the current editor contents.
    /// </summary>
    [RelayCommand]
    private void ShowPreview()
    {
        if (CurrentArticle is null ||
            Session.Editor.IsActive ||
            IsPreviewLoading)
        {
            return;
        }

        PreviewError = string.Empty;
        PreviewHtml = string.Empty;
        IsPreviewLoading = true;

        Session.Editor.Preview(
            CurrentArticle.Name,
            Editor.Document.CurrentText);
    }

    /// <summary>
    /// Reprocesses the current editor contents using the active
    /// article-processing options without advancing the article list.
    /// </summary>
    [RelayCommand]
    private void ReparseProcessing()
    {
        if (CurrentArticle is null ||
            !Options.IsProcessing ||
            Options.IsProcessingActive ||
            Session.IsBusy ||
            Session.Editor.IsActive ||
            _pendingSaveArticle is not null)
        {
            return;
        }

        Article article = CurrentArticle;

        string editorText = Editor.Document.CurrentText;

        // Preserve manual edits before running the processing pipeline.
        article.AWBChangeArticleText(
            "Reparse",
            editorText,
            false);

        MainProcessOptions options =
            Options.CreateMainProcessOptions(Preferences);

        MainProcessDependencies dependencies =
            CreateMainProcessDependencies();

        MainProcessCallbacks callbacks =
            CreateMainProcessCallbacks();

        // Reparse uses the shared processing pipeline but does not
        // advance the article list or save the article.
        ArticleProcessingResult result =
            _mainProcess.ProcessPageWithResult(
                article,
                false,
                options,
                Session,
                dependencies,
                callbacks);

        // Processing may have been aborted by a callback.
        if (!Options.IsProcessing ||
            !ReferenceEquals(CurrentArticle, article))
        {
            return;
        }

        // Do not update the editor with an incomplete or failed result.
        if (!result.Completed ||
            result.Outcome == ArticleProcessingOutcome.Failed)
        {
            return;
        }

        // Update the editor without resetting its original-text baseline.
        Editor.Document.CurrentText = article.ArticleText;

        UpdateArticleAlerts();

        // Refresh the calculated edit summary after reprocessing.
        UpdateCalculatedEditSummary();
    }

    /// <summary>
    /// Initializes the standard Twain editing workspace.
    /// </summary>
    public WorkspaceViewModel(
        Session session,
        UserPrefs preferences)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(preferences);

        Session = session;
        Preferences = preferences;

        _mainProcess = new MainProcess(new Parsers());

        ArticleEditingSession editingSession = new(
            """
    This is the original article text.

    Edit this text to produce an updated version.
    """);

        ArticleDocumentViewModel document = new(editingSession);

        Editor = new ArticleEditorViewModel(
            document,
            Preferences);

        Diff = new DiffViewModel(document);

        MakeList = new MakeListViewModel(Session);
        Options = new OptionsViewModel();

        TwainHost = new TwainHost(
            Session,
            () => Options.IsProcessing,
            () => StopProcessing(),
            message => System.Diagnostics.Debug.WriteLine(message));

        WorkspaceLayout layout =
            BuiltInWorkspaceLayouts.CreateDefaultEditing();

        Panes = CreateWorkspacePanes(layout);

        InitializeProcessing();
        InitializeEditor(document);
        InitializeWorkspaceOptions();
        InitializeStatisticsTimer();
        ConfigureWorkspacePanes();
    }

    private void InitializeProcessing()
    {

        BotSettings.CanUseBotMode =
            Session.IsBot || Session.IsSysop;

        Session.OpenComplete += Editor_OpenComplete;

        Session.Editor.SaveCompletedWithId +=
            Editor_SaveCompletedWithId;

        Session.PreviewComplete +=
            Editor_PreviewComplete;

        Session.ExceptionCaught +=
            Editor_ExceptionCaught;

        Session.Editor.SaveFailedWithId +=
            Editor_SaveFailedWithId;
    }

    private void InitializeWorkspaceOptions()
    {
        MakeList.Articles.CollectionChanged +=
            MakeListArticles_CollectionChanged;

        Options.CanStartProcessing =
            MakeList.ArticleCount > 0;

        Options.DisambiguationSourceProvider =
            () => MakeList.SourceText;

        Options.FindNextRequested =
            FindNext;

        Options.ToggleArticleWatchRequested =
            ToggleArticleWatchAsync;

        Options.StartProcessingRequested =
            StartProcessing;

        Options.ReparseProcessingRequested =
            ReparseProcessing;

        Options.SaveProcessingRequested =
            () => _ = SaveProcessing();

        Options.StopProcessingRequested =
            StopProcessing;

        Options.SkipProcessingRequested =
            SkipProcessing;

        Options.ShowPreviewRequested =
            ShowPreview;

        Options.ShowDiffRequested =
            ShowDiff;
    }

    private void InitializeStatisticsTimer()
    {
        _statisticsTimer.Interval =
            TimeSpan.FromMinutes(1);

        _statisticsTimer.Tick += (_, _) =>
        {
            _sessionCounters.GenerateEditStatistics();

            OnPropertyChanged(nameof(PagesPerMinute));
            OnPropertyChanged(nameof(EditsPerMinute));
        };

        _statisticsTimer.Start();
    }

    private ObservableCollection<PaneViewModel> CreateWorkspacePanes(
        WorkspaceLayout layout)
    {
        return new ObservableCollection<PaneViewModel>
    {
        CreatePane(
            BuiltInPaneDefinitions.ArticleEditor,
            FindState(layout, BuiltInPaneIds.ArticleEditor),
            Editor),

        CreatePane(
            BuiltInPaneDefinitions.ArticleList,
            FindState(layout, BuiltInPaneIds.ArticleList),
            MakeList),

        CreatePane(
            BuiltInPaneDefinitions.Options,
            FindState(layout, BuiltInPaneIds.Options),
            Options),

        CreatePane(
            BuiltInPaneDefinitions.Diff,
            FindState(layout, BuiltInPaneIds.Diff),
            Diff)
      };
    }

    private void ConfigureWorkspacePanes()
    {
        PaneViewModel articleEditorPane = Panes.Single(
            pane => pane.Id == BuiltInPaneIds.ArticleEditor);

        articleEditorPane.HeaderAction = ShowOptions;
        articleEditorPane.HistoryAction = OpenArticleHistory;
        articleEditorPane.EditOnWikiAction = OpenArticleOnWiki;

        articleEditorPane.Title =
            string.IsNullOrWhiteSpace(CurrentArticleName)
                ? "Page Editor"
                : CurrentArticleName;

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CurrentArticleName))
            {
                articleEditorPane.Title =
                    string.IsNullOrWhiteSpace(CurrentArticleName)
                        ? "Page Editor"
                        : CurrentArticleName;
            }
        };

        PaneViewModel makeListPane = Panes.Single(
            pane => pane.Id == BuiltInPaneIds.ArticleList);

        makeListPane.HeaderAction = MakeList.RequestShowFilter;
        makeListPane.HeaderActionToolTip = "Filter article list";
        makeListPane.HeaderActionIcon = PaneHeaderActionIcon.Filter;

        makeListPane.HeaderStatusText = MakeList.ArticleCountText;

        MakeList.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName ==
                nameof(MakeListViewModel.ArticleCountText))
            {
                makeListPane.HeaderStatusText =
                    MakeList.ArticleCountText;
            }
        };
    }

    /// <summary>
    /// Opens the current article in the connected
    /// MediaWiki site's editing interface.
    /// </summary>
    private void OpenArticleOnWiki()
    {
        if (string.IsNullOrWhiteSpace(CurrentArticleName))
            return;

        string wikiUrl = Session.Editor.URL;

        string editUrl =
            wikiUrl +
            "index.php?title=" +
            Uri.EscapeDataString(CurrentArticleName) +
            "&action=edit";

        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = editUrl,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to open article on wiki: {ex}");
        }
    }

    /// <summary>
    /// Opens the current article's revision history
    /// on the connected MediaWiki site.
    /// </summary>
    private void OpenArticleHistory()
    {
        if (string.IsNullOrWhiteSpace(CurrentArticleName))
            return;

        string wikiUrl = Session.Editor.URL;

        string historyUrl =
            wikiUrl +
            "index.php?title=" +
            Uri.EscapeDataString(CurrentArticleName) +
            "&action=history";

        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = historyUrl,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to open article history: {ex}");
        }
    }

    private void InitializeEditor(
        ArticleDocumentViewModel document)
    {
        document.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName !=
                nameof(ArticleDocumentViewModel.CurrentText))
            {
                return;
            }

            Options.UpdateArticleStatistics(
                ArticleStatisticsCalculator.Calculate(
                    document.CurrentText));
        };

        Diff.GoToLineRequested = GoToDiffLineAsync;
    }

    /// <summary>
    /// Begins processing the selected article or the next Preparse article.
    /// </summary>
    private void StartProcessing()
    {
        if (!CanBeginProcessing())
        {
            return;
        }

        Article? article = GetNextProcessingArticle();

        if (article is null)
        {
            return;
        }

        BeginArticleProcessing(article);
    }

    /// <summary>
    /// Verifies that processing can begin and that the session
    /// has the necessary permissions.
    /// </summary>
    private bool CanBeginProcessing()
    {
        if (Options.IsProcessingActive)
        {
            return false;
        }

        if (!Session.User.IsLoggedIn)
        {
            Options.IsWikiAuthenticated = false;

            if (Options.IsProcessing)
            {
                StopProcessing();
            }

            return false;
        }

        if (Session.IsBusy ||
            (MakeList.SelectedArticle is null &&
             MakeList.Articles.Count == 0))
        {
            return false;
        }

        // Automatic-save permissions apply only to normal editing.
        if (!Options.PreParseMode &&
            !IsPreparseRunning &&
            BotSettings.AutoSaveEnabled &&
            (!Session.User.IsLoggedIn ||
             (!Session.IsBot && !Session.IsSysop)))
        {
            BotSettings.AutoSaveEnabled = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the next article to process, using a stable queue
    /// snapshot when Preparse is active.
    /// </summary>
    private Article? GetNextProcessingArticle()
    {
        Article? article =
            MakeList.SelectedArticle ??
            MakeList.Articles.FirstOrDefault();

        if (!Options.PreParseMode &&
            !IsPreparseRunning)
        {
            return article;
        }

        if (_preparseQueue is null)
        {
            _preparseQueue = MakeList.Articles.ToList();
            _preparseQueueIndex = 0;
        }

        if (_preparseQueueIndex >= _preparseQueue.Count)
        {
            _preparseQueue = null;
            _preparseQueueIndex = 0;

            CompleteProcessing(
                allowAutomaticShutdown: false);

            return null;
        }

        return _preparseQueue[_preparseQueueIndex];
    }

    /// <summary>
    /// Initializes processing state and opens the selected article.
    /// </summary>
    private void BeginArticleProcessing(Article article)
    {
        PaneViewModel articleEditorPane = Panes.Single(
            pane => pane.Id == BuiltInPaneIds.ArticleEditor);

        articleEditorPane.HasWatchlistStatus = false;
        articleEditorPane.IsArticleWatched = false;
        Options.IsCurrentArticleWatched = false;

        articleEditorPane.LastEditTimestamp = string.Empty;

        _activeQueueArticle = article;

        CurrentArticleName = article.Name;

        Options.IsProcessing = true;
        Options.IsProcessingActive = true;

        // Preparse does not use the automatic-save nudge workflow.
        if (!Options.PreParseMode &&
            !IsPreparseRunning &&
            BotSettings.AutoSaveEnabled &&
            BotSettings.NudgeEnabled)
        {
            _ = ScheduleNudgeAsync();
        }

        bool followRedirects =
            Preferences.General.BypassRedirect &&
            !Options.SkipRedirects;

        Session.Editor.Open(
            article.Name,
            followRedirects);
    }

    private bool _isWatchOperationRunning;

    [RelayCommand]
    private async Task ToggleArticleWatchAsync()
    {
        if (_isWatchOperationRunning ||
            string.IsNullOrWhiteSpace(CurrentArticleName) ||
            Session.Editor.IsActive)
        {
            return;
        }

        var articleEditorPane = Panes.Single(
            pane => pane.Id == BuiltInPaneIds.ArticleEditor);

        if (!articleEditorPane.HasWatchlistStatus)
            return;

        string articleTitle = CurrentArticleName;
        bool wasWatched = articleEditorPane.IsArticleWatched;

        _isWatchOperationRunning = true;
        Options.IsWatchOperationRunning = true;

        try
        {
            if (wasWatched)
                await Task.Run(() => Session.Editor.Unwatch(articleTitle));
            else
                await Task.Run(() => Session.Editor.Watch(articleTitle));

            if (CurrentArticleName == articleTitle)
            {
                articleEditorPane.IsArticleWatched = !wasWatched;
                Options.IsCurrentArticleWatched = !wasWatched;

            }
            MakeList.UpdateArticleWatchStatus(
                articleTitle,
                !wasWatched);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
        finally
        {
            _isWatchOperationRunning = false;
            Options.IsWatchOperationRunning = false;
        }
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
    /// Runs the enabled custom module for the supplied article.
    /// </summary>
    /// <returns>
    /// True when article processing should continue; otherwise false.
    /// </returns>
    private bool RunExtensionProcessing(Article article)
    {
        if (!CustomModuleState.ModuleUsable)
            return true;

        IModule? module = CustomModuleState.Module;

        if (module is null)
            return true;

        article.SendPageToCustomModule(module);

        return !article.SkipArticle;
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
    /// Handles successful Save completion with its operation identifier.
    /// </summary>
    private void Editor_SaveCompletedWithId(
        AsyncApiEdit editor,
        SaveInfo saveInfo,
        long saveOperationId)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_pendingSaveArticle is null ||
                _pendingSaveOperationId != saveOperationId)
            {
                return;
            }

            if (!Options.IsProcessing)
            {
                _pendingSaveArticle = null;
                _pendingSaveOperationId = 0;
                return;
            }

            if (!ReferenceEquals(CurrentArticle, _pendingSaveArticle))
            {
                return;
            }

            Editor_SaveComplete(editor, saveInfo);
        });
    }

    /// <summary>
    /// Handles completion of a successful article save.
    /// Records confirmed saves even when processing has been stopped,
    /// but only advances the article queue while processing is active.
    /// </summary>
    private void Editor_SaveComplete(
        AsyncApiEdit editor,
        SaveInfo saveInfo)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (CurrentArticle is null)
            {
                return;
            }

            if (_pendingSaveArticle is null ||
                !ReferenceEquals(CurrentArticle, _pendingSaveArticle))
            {
                return;
            }

            bool continueProcessing = Options.IsProcessing;

            // Preserve the original Make List entry, which may differ
            // from CurrentArticle when MediaWiki resolves a redirect.
            Article? queueArticle = _activeQueueArticle;

            int currentIndex = queueArticle is null
                ? -1
                : MakeList.Articles.IndexOf(queueArticle);

            if (currentIndex < 0 ||
                queueArticle is null ||
                !MakeList.RemoveArticle(queueArticle.Name))
            {
                _pendingSaveArticle = null;
                _pendingSaveOperationId = 0;

                if (continueProcessing)
                {
                    StopProcessing();
                }

                return;
            }

            // The wiki confirmed the Save.
            _pendingSaveArticle = null;
            _pendingSaveOperationId = 0;

            // The original queue entry has been removed.
            _activeQueueArticle = null;

            _sessionCounters.NumberOfEdits++;
            OnPropertyChanged(nameof(TotalEdits));

            BotSettings.CurrentArticleNudgeCount = 0;

            // Preserve maximum-edits enforcement.
            if (continueProcessing &&
                BotSettings.AutoSaveEnabled &&
                BotSettings.MaximumEdits > 0 &&
                _sessionCounters.NumberOfEdits >= BotSettings.MaximumEdits)
            {
                StopProcessing();
                return;
            }

            // A successful Save after Stop must be recorded,
            // but processing must not restart automatically.
            if (!continueProcessing)
            {
                return;
            }

            AdvanceToNextArticle(currentIndex);
        });
    }

    /// <summary>
    /// Handles completion of a MediaWiki preview request.
    /// </summary>
    private void Editor_PreviewComplete(
        AsyncApiEdit editor,
        string html)
    {
        Dispatcher.UIThread.Post(() =>
        {
            PreviewHtml = html;
            PreviewError = string.Empty;
            IsPreviewLoading = false;
            IsPreviewVisible = true;
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

        _nudgeCancellation?.Cancel();
        _nudgeCancellation?.Dispose();
        _nudgeCancellation = null;

        Dispatcher.UIThread.Post(() =>
        {
            // Stopping processing intentionally cancels any active
            // HTTP request. This is expected behavior, not an error.
            if (exception is OperationCanceledException &&
                !Options.IsProcessing &&
                !Options.IsProcessingActive)
            {
                return;
            }

            if (IsPreviewLoading)
            {
                IsPreviewLoading = false;
                PreviewError = exception.Message;
                IsPreviewVisible = false;
            }
            else if (Options.IsProcessingActive)
            {
                // The article operation failed.
                // Restore Start without automatically retrying.
                StopProcessing();
            }

            ErrorHandler.HandleException(exception);
        });
    }

    /// <summary>
    /// Handles a failed Save operation using its operation identifier.
    /// </summary>
    private void Editor_SaveFailedWithId(
        AsyncApiEdit editor,
        Exception exception,
        long saveOperationId)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_pendingSaveArticle is null ||
                _pendingSaveOperationId != saveOperationId)
            {
                return;
            }

            _pendingSaveArticle = null;
            _pendingSaveOperationId = 0;
        });
    }

    /// <summary>
    /// Saves the current article to the active wiki.
    /// A confirmed Save is recorded even if processing was stopped
    /// while the request was running.
    /// </summary>
    [RelayCommand]
    private async Task SaveProcessing()
    {
        // Preparse is a read-only processing workflow.
        // Never submit edits while Preparse is active.
        if (Options.PreParseMode ||
            IsPreparseRunning ||
            !Options.IsProcessing ||
            CurrentArticle is null ||
            Session.Editor.IsActive ||
            _pendingSaveArticle is not null)
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

        string articleText = Editor.Document.CurrentText;

        if (string.IsNullOrEmpty(articleText))
        {
            return;
        }

        Article savingArticle = CurrentArticle;

        _pendingSaveArticle = savingArticle;

        bool saveCompleted = false;

        try
        {
            // Preserve the summary displayed in the article editor,
            // including any manual changes made by the user.
            string finalSummary = Editor.CalculatedEditSummary;

            // Verify that any existing section prefix still matches
            // the actual changes in the article before saving.
            finalSummary = Summary.CorrectSectionEditSummary(
                finalSummary,
                savingArticle.OriginalArticleText,
                articleText);

            SaveInfo saveInfo = await Session.Editor.SaveArticleAsync(
                articleText,
                finalSummary,
                Options.MinorEdit,
                WatchOptions.NoChange);

            // A successful API response confirms that the wiki
            // accepted the edit, even if Stop was pressed.
            //
            // Editor_SaveComplete records the edit and removes
            // the article from Make List. It only advances when
            // Options.IsProcessing is still true.
            if (!ReferenceEquals(CurrentArticle, savingArticle))
            {
                return;
            }

            saveCompleted = true;

            Editor_SaveComplete(Session.Editor, saveInfo);
        }
        catch (OperationCanceledException)
        {
            // Cancellation was requested and the API operation
            // did not return a confirmed successful result.
            //
            // The article remains in Make List.
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
        finally
        {
            // Successful saves are finalized by Editor_SaveComplete
            // after its UI-dispatched callback executes.
            //
            // Failed or cancelled operations release the pending
            // article immediately.
            if (!saveCompleted &&
                ReferenceEquals(_pendingSaveArticle, savingArticle))
            {
                _pendingSaveArticle = null;
                _pendingSaveOperationId = 0;
            }
        }
    }

    /// <summary>
    /// Stops the active article-processing workflow.
    /// </summary>
    [RelayCommand]
    private void StopProcessing()
    {
        if (!Options.IsProcessing &&
            !Options.IsProcessingActive)
        {
            return;
        }

        _autoSaveCancellation?.Cancel();
        _autoSaveCancellation?.Dispose();
        _autoSaveCancellation = null;

        _nudgeCancellation?.Cancel();
        _nudgeCancellation?.Dispose();
        _nudgeCancellation = null;

        if (Session.Editor.IsActive)
        {
            Session.Editor.Abort();
        }

        Options.IsProcessingActive = false;
        Options.IsProcessing = false;

        // Discard the temporary Preparse snapshot.
        // Articles retained in Make List are not affected.
        _preparseQueue = null;
        _preparseQueueIndex = 0;
    }

    /// <summary>
    /// Completes article processing after the article queue has been exhausted.
    /// </summary>
    private async void CompleteProcessing(bool allowAutomaticShutdown = true)
    {
        StopProcessing();

        // Preparse must not trigger an automatic system shutdown.
        if (!allowAutomaticShutdown)
        {
            return;
        }

        if (!BotSettings.AutomaticShutdownEnabled ||
            ConfirmShutdownRequested is null)
        {
            return;
        }

        bool confirmed =
            await ConfirmShutdownRequested(
                BotSettings.ShutdownAction);

        if (!confirmed)
        {
            return;
        }

        App.SystemPowerAction?.Invoke(
            BotSettings.ShutdownAction);
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

            await SaveProcessing();
        }
        catch (OperationCanceledException)
        {
            // A pending automatic save was intentionally cancelled.
        }
    }

    /// <summary>
    /// Waits for the current article operation to time out and records a nudge.
    /// </summary>
    private async Task ScheduleNudgeAsync()
    {
        _nudgeCancellation?.Cancel();
        _nudgeCancellation?.Dispose();

        _nudgeCancellation =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            _nudgeCancellation.Token;

        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(30),
                cancellationToken);

            if (cancellationToken.IsCancellationRequested ||
                !BotSettings.AutoSaveEnabled ||
                !BotSettings.NudgeEnabled)
            {
                return;
            }

            ProcessNudge();
        }
        catch (OperationCanceledException)
        {
            // The pending nudge was intentionally cancelled.
        }
    }

    /// <summary>
    /// Handles expiration of the nudge timer for the current article.
    /// </summary>
    private async void ProcessNudge()
    {
        if (!Options.IsProcessing ||
            !BotSettings.AutoSaveEnabled ||
            !BotSettings.NudgeEnabled)
        {
            return;
        }

        BotSettings.NudgeCount++;

        if (BotSettings.SkipAfterRepeatedNudge &&
            BotSettings.CurrentArticleNudgeCount > 0)
        {
            BotSettings.CurrentArticleNudgeCount = 0;
            SkipProcessing();
            return;
        }

        BotSettings.CurrentArticleNudgeCount++;

        Session.Editor.Abort();

        while (Session.IsBusy)
        {
            await Task.Delay(50);
        }

        // Stop may have been pressed while waiting.
        if (!Options.IsProcessing)
        {
            return;
        }

        // The previous operation has been aborted.
        // Allow StartProcessing to retry the article.
        Options.IsProcessingActive = false;

        StartProcessing();
    }

    /// <summary>
    /// Completes a skipped article, removes its original queue
    /// entry, and advances to the next article.
    /// </summary>
    private void CompleteCurrentArticle(bool manualSkip = true)
    {
        if (!Options.IsProcessing ||
            CurrentArticle is null ||
            _pendingSaveArticle is not null ||
            Session.Editor.IsActive)
        {
            return;
        }

        _autoSaveCancellation?.Cancel();
        _autoSaveCancellation?.Dispose();
        _autoSaveCancellation = null;

        _nudgeCancellation?.Cancel();
        _nudgeCancellation?.Dispose();
        _nudgeCancellation = null;

        // Manual skips are recorded here.
        // Automatic skip reasons are recorded by the processing logic.
        if (manualSkip)
        {
            CurrentArticle.Trace.UserSkipped();
        }

        Session.Editor.Reset();

        // Use the original queue entry, which may differ from
        // CurrentArticle when a redirect was followed.
        Article? queueArticle = _activeQueueArticle;

        int currentIndex = queueArticle is null
            ? -1
            : MakeList.Articles.IndexOf(queueArticle);

        if (currentIndex < 0 ||
            queueArticle is null ||
            !MakeList.RemoveArticle(queueArticle.Name))
        {
            StopProcessing();
            return;
        }

        _activeQueueArticle = null;

        _sessionCounters.NumberOfIgnoredEdits++;
        OnPropertyChanged(nameof(SkippedArticles));

        AdvanceToNextArticle(currentIndex);
    }

    /// <summary>
    /// Clears the completed article and advances to the next
    /// available entry in Make List.
    /// </summary>
    /// <param name="completedIndex">
    /// The original position of the completed queue entry,
    /// captured before that entry was removed.
    /// </param>
    private void AdvanceToNextArticle(int completedIndex)
    {
        if (!Options.IsProcessing)
        {
            return;
        }

        Article? nextArticle =
            completedIndex >= 0 &&
            completedIndex < MakeList.Articles.Count
                ? MakeList.Articles[completedIndex]
                : MakeList.Articles.FirstOrDefault();

        CurrentArticle = null;
        CurrentArticleName = string.Empty;

        Editor.Document.CurrentText = string.Empty;

        Options.ResetArticleStatistics();

        CurrentAlertResult = null;
        Options.ResetAlerts();

        BotSettings.CurrentArticleNudgeCount = 0;

        if (nextArticle is null)
        {
            CompleteProcessing();
            return;
        }

        MakeList.SelectedArticle = nextArticle;

        Options.IsProcessingActive = false;

        StartProcessing();
    }

    /// <summary>
    /// Skips the current article and advances to the next.
    /// </summary>
    [RelayCommand]
    private void SkipProcessing()
    {
        CompleteCurrentArticle();
    }

    /// <summary>
    /// Advances to the next article in the Preparse snapshot.
    /// Retained articles remain in Make List but are not
    /// processed again during the same run.
    /// </summary>
    private void AdvancePreparseQueue()
    {
        if (!IsPreparseRunning ||
            !Options.IsProcessing)
        {
            return;
        }

        // The user disabled Preparse while the current article
        // was being processed. Finish this article, then stop.
        if (!Options.PreParseMode)
        {
            StopProcessing();
            return;
        }

        _preparseQueueIndex++;

        if (_preparseQueueIndex >= _preparseQueue!.Count)
        {
            // The Preparse snapshot has been exhausted.
            _preparseQueue = null;
            _preparseQueueIndex = 0;

            CompleteProcessing(allowAutomaticShutdown: false);
            return;
        }

        Article nextArticle =
            _preparseQueue[_preparseQueueIndex];

        // Clear the previous article's workspace state.
        CurrentArticle = null;
        CurrentArticleName = string.Empty;
        _activeQueueArticle = null;

        Editor.Document.CurrentText = string.Empty;
        Editor.CalculatedEditSummary = string.Empty;

        Options.ResetArticleStatistics();

        CurrentAlertResult = null;
        Options.ResetAlerts();

        // Select the next snapshot entry, even when previously
        // processed articles were retained in Make List.
        MakeList.SelectedArticle = nextArticle;

        Options.IsProcessingActive = false;

        StartProcessing();
    }

    /// <summary>
    /// Completes Preparse processing for an article, retaining
    /// articles with changes or errors and removing articles
    /// that were unchanged or skipped.
    /// </summary>
    private void CompletePreparseArticle(
        ArticleProcessingResult result)
    {
        if (!IsPreparseRunning ||
            !Options.IsProcessing ||
            _activeQueueArticle is null)
        {
            return;
        }

        Article queueArticle = _activeQueueArticle;

        switch (result.Outcome)
        {
            case ArticleProcessingOutcome.ChangesMade:
                MakeList.MarkPreparseRetained(queueArticle);
                break;

            case ArticleProcessingOutcome.NoChanges:
            case ArticleProcessingOutcome.Skipped:
                // Remove articles that do not require editing.
                if (!MakeList.RemoveArticle(queueArticle.Name))
                {
                    StopProcessing();
                    return;
                }
                break;

            case ArticleProcessingOutcome.Failed:
                // Preserve the failed article in Make List.
                // Stop Preparse and require an explicit restart.
                StopProcessing();
                return;

            default:
                StopProcessing();
                return;
        }

        AdvancePreparseQueue();
    }

    /// <summary>
    /// Updates the calculated edit summary displayed above the article editor.
    /// </summary>
    private void UpdateCalculatedEditSummary()
    {
        if (CurrentArticle is null)
        {
            Editor.CalculatedEditSummary = string.Empty;
            return;
        }

        string originalText = CurrentArticle.OriginalArticleText;
        string currentText = Editor.Document.CurrentText;

        IReadOnlyList<string> modifiedSections =
            Summary.GetModifiedSections(originalText, currentText);

        string calculatedSummary = EditSummaryBuilder.Build(
            Options.EditSummary,
            CurrentArticle.EditSummary,
            originalText,
            currentText,
            !Preferences.General.noSectionEditSummary);

        System.Diagnostics.Debug.WriteLine(
            $"[EditSummary] Modified sections: " +
            $"{string.Join(", ", modifiedSections)}");

        System.Diagnostics.Debug.WriteLine(
            $"[EditSummary] Configured summary: {Options.EditSummary}");

        System.Diagnostics.Debug.WriteLine(
            $"[EditSummary] Article summary: {CurrentArticle.EditSummary}");

        System.Diagnostics.Debug.WriteLine(
            $"[EditSummary] Calculated summary: {calculatedSummary}");

        System.Diagnostics.Debug.WriteLine(
            $"[EditSummary] Original length: {originalText.Length}; " +
            $"Current length: {currentText.Length}");

        Editor.CalculatedEditSummary = calculatedSummary;
    }

    private void Editor_OpenComplete(
        AsyncApiEdit editor,
        PageInfo page)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Ignore callbacks after processing has stopped.
            if (!Options.IsProcessing ||
                !Options.IsProcessingActive)
            {
                return;
            }

            InitializeOpenedArticle(page);

            // Before-processing skip: redirect page.
            if (!IsPreparseRunning &&
                Options.SkipRedirects &&
                Tools.IsRedirect(page.Text))
            {
                CurrentArticle!.AWBSkip("Page is a redirect");

                CompleteCurrentArticle(manualSkip: false);
                return;
            }

            // Before-processing skip: page is in use.
            // Keep Preparse on its existing path for now.
            if (!IsPreparseRunning &&
                Options.SkipIfInUse &&
                CurrentArticle!.IsInUse)
            {
                CurrentArticle.AWBSkip("Page contains {{inuse}}");

                CompleteCurrentArticle(manualSkip: false);
                return;
            }

            ArticleProcessingResult result =
                ProcessOpenedArticle();

            // Core may have stopped processing.
            if (!Options.IsProcessing)
            {
                return;
            }

            if (IsPreparseRunning)
            {
                HandlePreparseProcessingResult(result);
                return;
            }

            if (result.Outcome == ArticleProcessingOutcome.Skipped)
            {
                CompleteCurrentArticle(manualSkip: false);
                return;
            }

            CompleteNormalArticleProcessing();
        });
    }

    /// <summary>
    /// Initializes the newly opened article and resets
    /// state associated with the previous article.
    /// </summary>
    private void InitializeOpenedArticle(PageInfo page)
    {
        _nudgeCancellation?.Cancel();
        _nudgeCancellation?.Dispose();
        _nudgeCancellation = null;

        CurrentArticle = new Article(page);

        PaneViewModel articleEditorPane = Panes.Single(
            pane => pane.Id == BuiltInPaneIds.ArticleEditor);

        articleEditorPane.IsArticleWatched = page.IsWatched;
        Options.IsCurrentArticleWatched = page.IsWatched;
        articleEditorPane.HasWatchlistStatus = true;

        if (DateTimeOffset.TryParse(
                page.Timestamp,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out DateTimeOffset lastEdit))
        {
            articleEditorPane.LastEditTimestamp =
                $"Last edit: {lastEdit.UtcDateTime:yyyy-MM-dd HH:mm} UTC";
        }
        else
        {
            articleEditorPane.LastEditTimestamp = string.Empty;
        }

        Editor.CalculatedEditSummary = string.Empty;

        Options.CanSaveCurrentArticle = true;

        // Reset the previous article's preview.
        IsPreviewVisible = false;
        PreviewHtml = string.Empty;
        PreviewError = string.Empty;
        IsPreviewLoading = false;

        Article? queuedArticle =
            MakeList.Articles.FirstOrDefault(
                article =>
                    string.Equals(
                        article.Name,
                        CurrentArticle.Name,
                        StringComparison.Ordinal));

        if (queuedArticle is not null)
        {
            queuedArticle.DisplayTitle =
                CurrentArticle.DisplayTitle;
        }

        CurrentArticleName = CurrentArticle.Name;

        Editor.Document.LoadArticle(
            CurrentArticle.ArticleText);
    }

    /// <summary>
    /// Runs the configured article-processing pipeline.
    /// </summary>
    private ArticleProcessingResult ProcessOpenedArticle()
    {
        MainProcessOptions options =
            Options.CreateMainProcessOptions(Preferences);

        MainProcessDependencies dependencies =
            CreateMainProcessDependencies();

        MainProcessCallbacks callbacks =
            CreateMainProcessCallbacks();

        Article article = CurrentArticle!;

        return _mainProcess.ProcessPageWithResult(
            article,
            true,
            options,
            Session,
            dependencies,
            callbacks);
    }

    /// <summary>
    /// Handles the result of unattended Preparse processing.
    /// </summary>
    private void HandlePreparseProcessingResult(
        ArticleProcessingResult result)
    {
        if (!result.Completed &&
            result.Outcome != ArticleProcessingOutcome.Failed)
        {
            StopProcessing();
            return;
        }

        CompletePreparseArticle(result);
    }

    /// <summary>
    /// Updates the editor after normal article processing
    /// and schedules automatic saving when enabled.
    /// </summary>
    private void CompleteNormalArticleProcessing()
    {
        Editor.Document.CurrentText =
            CurrentArticle!.ArticleText;

        // Evaluate alerts for the processed article.
        UpdateArticleAlerts();

        // Display the calculated edit summary.
        UpdateCalculatedEditSummary();

        // Keep the article available for manual review.
        if (!BotSettings.AutoSaveEnabled &&
            !Options.PreParseMode)
        {
            Options.IsProcessingActive = false;
        }

        // Preparse must never save articles.
        if (!Options.PreParseMode &&
            !IsPreparseRunning &&
            BotSettings.AutoSaveEnabled)
        {
            if (BotSettings.AutoSaveDelay == 0)
            {
                _ = SaveProcessing();
            }
            else
            {
                _ = ScheduleAutoSaveAsync();
            }
        }
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
    /// Requests that the article-list filtering dialog be opened.
    /// </summary>
    public Action? ShowFilterRequested { get; set; }

    /// <summary>
    /// Opens the article-list filtering dialog when a handler is available.
    /// </summary>
    public void RequestShowFilter()
    {
        ShowFilterRequested?.Invoke();
    }

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

    /// <summary>
    /// Refreshes workspace controls after preferences are loaded.
    /// </summary>
    public void ApplyLoadedSettings()
    {
        Options.LoadFromPreferences(Preferences);
        MakeList.LoadFromPreferences(Preferences);
        CustomModuleState.LoadFromPreferences(Preferences);
    }

    /// <summary>
    /// Copies the current workspace options into the shared AWB preferences.
    /// </summary>
    public void UpdatePreferencesForSave()
    {
        Options.SaveToPreferences(Preferences);
        MakeList.SaveToPreferences(Preferences);
        CustomModuleState.LoadFromPreferences(Preferences);
    }

    /// <summary>
    /// Evaluates alerts for the currently processed article,
    /// respecting the user's alert preferences.
    /// </summary>
    private void UpdateArticleAlerts()
    {
        ResetAlertNavigation();

        if (CurrentArticle is null)
        {
            CurrentAlertResult = null;
            _alertEvaluatedText = null;
            Options.ResetAlerts();
            return;
        }

        string articleText = Editor.Document.CurrentText;

        List<int> enabledAlertIds =
            Preferences.General.AlertPreferences;

        // Legacy settings use an empty list to mean all alerts enabled.
        bool allAlertsEnabled = enabledAlertIds.Count == 0;

        // The evaluator currently checks template patterns against article markup.
        // Keep the full text until a dedicated extraction method is verified.
        string templateSource = articleText;

        ArticleAlertResult result = ArticleAlertEvaluator.Evaluate(
            CurrentArticle,
            articleText,
            templateSource,
            Options.WordCount,
            Options.CategoryCount,
            allAlertsEnabled,
            enabledAlertIds,
            Options.EnableTypoCorrection);

        // Preserve the evaluation and the exact text used.
        CurrentAlertResult = result;
        _alertEvaluatedText = articleText;

        // Continue updating the existing Alerts panel.
        Options.UpdateAlerts(result.Alerts);
    }

    /// <summary>
    /// Gets the location of the first unbalanced bracket
    /// in the current article.
    /// </summary>
    /// <returns>
    /// The character offset and selection length,
    /// or null if no unbalanced bracket was detected.
    /// </returns>
    public (int Offset, int Length)? GetFirstUnbalancedBracket()
    {
        if (CurrentArticle is null ||
            CurrentAlertResult is null ||
            CurrentAlertResult.UnbalancedBrackets.Count == 0)
        {
            return null;
        }

        KeyValuePair<int, int> first =
            CurrentAlertResult.UnbalancedBrackets
                .OrderBy(entry => entry.Key)
                .First();

        return (first.Key, first.Value);
    }

    /// <summary>
    /// Gets the location of the first targetless link
    /// in the current article.
    /// </summary>
    public (int Offset, int Length)? GetFirstTargetlessLink()
    {
        if (CurrentArticle is null ||
            CurrentAlertResult is null ||
            CurrentAlertResult.TargetlessLinks.Count == 0)
        {
            return null;
        }

        KeyValuePair<int, int> first =
            CurrentAlertResult.TargetlessLinks
                .OrderBy(entry => entry.Key)
                .First();

        return (first.Key, first.Value);
    }

    /// <summary>
    /// Gets the first detected location associated with an article alert.
    /// </summary>
    public (int Offset, int Length)? GetAlertLocation(
        string alertText)
    {
        Dictionary<int, int>? locations =
            ResolveAlertLocations(alertText);

        if (locations is null || locations.Count == 0)
        {
            return null;
        }

        KeyValuePair<int, int> first =
            locations.OrderBy(entry => entry.Key).First();

        return (first.Key, first.Value);
    }

    /// <summary>
    /// Returns the next location for an alert, cycling through
    /// detected occurrences in document order.
    /// </summary>
    public (int Offset, int Length)? GetNextAlertLocation(
        string alertText)
    {
        Dictionary<int, int>? locations =
            ResolveAlertLocations(alertText);

        if (locations is null || locations.Count == 0)
        {
            return null;
        }

        KeyValuePair<int, int>[] orderedLocations =
            locations.OrderBy(entry => entry.Key).ToArray();

        if (!string.Equals(
            _lastNavigatedAlert,
            alertText,
            StringComparison.Ordinal))
        {
            _lastNavigatedAlert = alertText;
            _lastAlertLocationIndex = -1;
        }

        _lastAlertLocationIndex =
            (_lastAlertLocationIndex + 1) % orderedLocations.Length;

        KeyValuePair<int, int> location =
            orderedLocations[_lastAlertLocationIndex];

        return (location.Key, location.Value);
    }

    /// <summary>
    /// Gets the detected locations associated with an article alert.
    /// </summary>
    /// <param name="alertText">
    /// The alert description displayed in the Alerts panel.
    /// </param>
    /// <returns>
    /// The matching location dictionary, or null if the alert
    /// has no supported navigation locations.
    /// </returns>
    private Dictionary<int, int>? ResolveAlertLocations(
        string alertText)
    {
        if (CurrentArticle is null ||
            CurrentAlertResult is null ||
            string.IsNullOrWhiteSpace(alertText))
        {
            return null;
        }

        // Prevent navigation using outdated character offsets.
        if (!string.Equals(
            _alertEvaluatedText,
            Editor.Document.CurrentText,
            StringComparison.Ordinal))
        {
            return null;
        }

        ArticleAlertResult result = CurrentAlertResult;

        if (alertText.StartsWith(
            "Unbalanced brackets",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.UnbalancedBrackets;
        }

        if (alertText.StartsWith(
            "Links with no target",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.TargetlessLinks;
        }

        if (alertText.StartsWith(
            "Links with double pipes",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.DoublePipeLinks;
        }

        if (alertText.StartsWith(
            "Dead links",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.DeadLinks;
        }

        if (alertText.StartsWith(
            "Ambiguous citation dates",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.AmbiguousCiteDates;
        }

        if (alertText.StartsWith(
            "Invalid citation parameter(s)",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.BadCiteParameters;
        }
        if (alertText.StartsWith(
            "Unclosed tag(s)",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.UnclosedTags;
        }

        if (alertText.StartsWith(
            "Header(s) with wikilinks",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.WikilinkedHeaders;
        }

        if (alertText.StartsWith(
            "Duplicate parameter(s) in WPBannerShell",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.DuplicateBannerShellParameters;
        }

        if (alertText.StartsWith(
            "Unknown parameters in Multiple issues",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.UnknownMultipleIssuesParameterLocations;
        }

        if (alertText.StartsWith(
            "Unknown parameters in WikiProject banner shell",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.UnknownWikiProjectBannerShellParameterLocations;
        }

        if (alertText.StartsWith(
            "See also section out of place",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.OtherErrors;
        }

        if (alertText.StartsWith(
            "Editor's signature or link to user space",
            StringComparison.OrdinalIgnoreCase))
        {
            return result.UserSignatures;
        }

        if (alertText.StartsWith(
            "Contains 'sic' tag",
            StringComparison.OrdinalIgnoreCase))
        {
            return CurrentAlertResult.SicTagLocations;
        }

        if (alertText.StartsWith(
            "DAB page with <ref>s",
            StringComparison.OrdinalIgnoreCase))
        {
            return CurrentAlertResult.DisambiguationReferenceLocations;
        }

        if (alertText.StartsWith(
            "Unformatted references",
            StringComparison.OrdinalIgnoreCase))
        {
            return CurrentAlertResult.BareReferenceLocations;
        }

        if (alertText.StartsWith(
            "Has a <ref> after <references/>",
            StringComparison.OrdinalIgnoreCase))
        {
            return CurrentAlertResult.ReferencesAfterReflistLocations;
        }

        if (alertText.StartsWith(
            "Has 'No/More footnotes' template yet many references",
            StringComparison.OrdinalIgnoreCase))
        {
            return CurrentAlertResult.MoreFootnotesTemplateLocations;
        }

        return null;
    }


}
