using Avalonia.Controls;
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
using Twain.Core.AWBSettings;
using Twain.Core.CustomModules;
using Twain.Core.Editing;
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

    private Article? _pendingSaveArticle;

    /// <summary>
    /// Identifies the Save operation associated with the pending article.
    /// </summary>
    private long _pendingSaveOperationId;

    /// <summary>
    /// Indicates that a Save request is being started and its
    /// operation identifier has not yet been recorded.
    /// </summary>
    private bool _pendingSaveInitialization;

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

        // Reparse is not normal article-list processing.
        _mainProcess.ProcessPageCore(
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

        // Update the editor without resetting its original-text baseline.
        Editor.Document.CurrentText = article.ArticleText;
    }

    /// <summary>
    /// Initializes the standard Twain editing workspace.
    /// </summary>
    public WorkspaceViewModel(
        Session session,
        UserPrefs preferences)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
        Preferences = preferences;

        TwainHost = new TwainHost(
            Session,
            () => Options.IsProcessing,
            () => StopProcessing(),
            message => System.Diagnostics.Debug.WriteLine(message));

        BotSettings.CanUseBotMode =
            Session.IsBot || Session.IsSysop;

        _mainProcess =
            new MainProcess(
                new Parsers());

        Session.OpenComplete +=
            Editor_OpenComplete;

        Session.Editor.SaveCompletedWithId +=
            Editor_SaveCompletedWithId;

        Session.PreviewComplete +=
           Editor_PreviewComplete;

        Session.ExceptionCaught +=
            Editor_ExceptionCaught;

        Session.Editor.SaveFailedWithId +=
            Editor_SaveFailedWithId;

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
            document,
            Preferences);

        Diff = new DiffViewModel(
           document);

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

        Diff.GoToLineRequested =
           GoToDiffLineAsync;

        MakeList = new MakeListViewModel(
            Session);

        Options = new OptionsViewModel();

        _statisticsTimer.Interval = TimeSpan.FromMinutes(1);

        _statisticsTimer.Tick += (_, _) =>
        {
            _sessionCounters.GenerateEditStatistics();

            OnPropertyChanged(nameof(PagesPerMinute));
            OnPropertyChanged(nameof(EditsPerMinute));
        };

        _statisticsTimer.Start();

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

        // Open editing options from the Page Editor header.
        PaneViewModel articleEditorPane = Panes.Single(
            pane => pane.Id == BuiltInPaneIds.ArticleEditor);

        articleEditorPane.HeaderAction = ShowOptions;

        // Open article-list filtering from the Make List header.
        PaneViewModel makeListPane = Panes.Single(
            pane => pane.Id == BuiltInPaneIds.ArticleList);

        makeListPane.HeaderAction = MakeList.RequestShowFilter;
        makeListPane.HeaderActionToolTip = "Filter article list";
        makeListPane.HeaderActionIcon = PaneHeaderActionIcon.Filter;
    }

    /// <summary>
    /// Begins processing the first article in the current article list.
    /// </summary>
    private void StartProcessing()
    {
        if (Options.IsProcessingActive)
        {
            return;
        }
        // Verify the actual session before starting an API operation.
        if (!Session.User.IsLoggedIn)
        {
            Options.IsWikiAuthenticated = false;

            if (Options.IsProcessing)
            {
                StopProcessing();
            }

            return;
        }

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
        Options.IsProcessingActive = true;

        if (BotSettings.AutoSaveEnabled &&
            BotSettings.NudgeEnabled)
        {
            _ = ScheduleNudgeAsync();
        }

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

            Article? nextArticle =
                currentIndex < MakeList.Articles.Count
                    ? MakeList.Articles[currentIndex]
                    : MakeList.Articles.FirstOrDefault();

            CurrentArticle = null;
            CurrentArticleName = string.Empty;

            Editor.Document.CurrentText = string.Empty;

            Options.ResetArticleStatistics();

            if (nextArticle is null)
            {
                CompleteProcessing();
                return;
            }

            MakeList.SelectedArticle = nextArticle;

            Options.IsProcessingActive = false;

            StartProcessing();
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
                Options.IsProcessingActive = false;
                Options.IsProcessing = false;
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
        if (!Options.IsProcessing ||
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
            SaveInfo saveInfo = await Session.Editor.SaveArticleAsync(
                articleText,
                Options.EditSummary,
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
        }

    /// <summary>
    /// Completes article processing after the article queue has been exhausted.
    /// </summary>
    private async void CompleteProcessing()
    {
        StopProcessing();

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

            SaveProcessing();
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
    /// Removes the current article from the processing queue,
    /// updates the appropriate session counter, and advances
    /// to the next article.
    /// </summary>
    /// <param name="wasSaved">
    /// True when the article was successfully saved;
    /// false when the article was skipped.
    /// </param>
    private void CompleteCurrentArticle(bool wasSaved)
    {
        if (!Options.IsProcessing || CurrentArticle is null)
        {
            return;
        }

        // Remove the completed article from Make List.
        if (!MakeList.RemoveArticle(CurrentArticle.Name))
        {
            StopProcessing();
            return;
        }

        // Update the appropriate session counter.
        if (wasSaved)
        {
            _sessionCounters.NumberOfEdits++;
            OnPropertyChanged(nameof(TotalEdits));
        }
        else
        {
            _sessionCounters.NumberOfIgnoredEdits++;
            OnPropertyChanged(nameof(SkippedArticles));
        }

        BotSettings.CurrentArticleNudgeCount = 0;

        // Clear the completed article.
        CurrentArticle = null;
        CurrentArticleName = string.Empty;

        Editor.Document.CurrentText = string.Empty;

        Options.ResetArticleStatistics();

        // Allow processing to begin for the next article.
        Options.IsProcessingActive = false;

        // Check whether the queue is exhausted.
        if (MakeList.Articles.Count == 0)
        {
            CompleteProcessing();
            return;
        }

        // MakeList.RemoveArticle() maintains the selection.
        StartProcessing();
    }

    /// <summary>
    /// Skips the current article and continues processing
    /// the next article.
    /// </summary>
    [RelayCommand]
    private void SkipProcessing()
    {
        if (!Options.IsProcessing || CurrentArticle is null)
        {
            return;
        }

        // Cancel any pending automatic save.
        _autoSaveCancellation?.Cancel();
        _autoSaveCancellation?.Dispose();
        _autoSaveCancellation = null;

        // Cancel the current article's nudge timer.
        _nudgeCancellation?.Cancel();
        _nudgeCancellation?.Dispose();
        _nudgeCancellation = null;

        // Record that the user skipped this article.
        CurrentArticle.Trace.UserSkipped();

        // Reset the API editor before advancing.
        Session.Editor.Reset();

        // Complete the article without counting an edit.
        CompleteCurrentArticle(wasSaved: false);
    }

    private void Editor_OpenComplete(
        AsyncApiEdit editor,
        PageInfo page)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Ignore completion callbacks after processing has stopped.
            if (!Options.IsProcessing ||
                !Options.IsProcessingActive)
            {
                return;
            }

            _nudgeCancellation?.Cancel();
            _nudgeCancellation?.Dispose();
            _nudgeCancellation = null;

            CurrentArticle =
                new Article(page);

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

            CurrentArticleName =
                CurrentArticle.Name;

            Editor.Document.LoadArticle(
                CurrentArticle.ArticleText);

            MainProcessOptions options =
                Options.CreateMainProcessOptions(Preferences);

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

            // Core may have requested that processing stop.
            // Do not continue updating the article or schedule
            // an automatic save after the workflow was aborted.
            if (!Options.IsProcessing)
            {
                return;
            }

            Editor.Document.CurrentText =
                CurrentArticle.ArticleText;

            // Core processing has completed.
            // Keep the article available for review and saving.
            if (!BotSettings.AutoSaveEnabled &&
                !Options.PreParseMode)
            {
                Options.IsProcessingActive = false;
            }

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
}
