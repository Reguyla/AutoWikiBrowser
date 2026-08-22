/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

 */

using AutoWikiBrowser.Services.Diff;
using AutoWikiBrowser.Services.Settings;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Twain.Core;
using Twain.Core.API;
using Twain.Core.Background;
using Twain.Core.Controls;
using Twain.Core.Controls.Lists;
using Twain.Core.Editing;
using Twain.Core.Lists.Providers;
using Twain.Core.Parse;
using Twain.Core.Plugin;
using Twain.Core.Processing;
using ThreadState = System.Threading.ThreadState;

namespace AutoWikiBrowser;

// TODO(Twain): Continue decomposing MainForm by moving non-UI workflow,
// processing, navigation, configuration, and authorization logic into
// testable Twain.Core services.
//
// TODO(Twain): Consolidate reusable regular expressions into WikiRegexes
// when they are not dependent on dynamic article or project state.

public sealed partial class MainForm : Form, IAutoWikiBrowser
{ // this class needs to be public, otherwise we get an exception which recommends setting ComVisibleAttribute to true (which we've already done)
    #region Fields

    // --------------------------------------------------------------------
    // UI
    // --------------------------------------------------------------------

    private readonly Splash _splashScreen = new();
    private readonly Twain.Core.Profiles.AWBProfilesForm _profiles;

    private FormWindowState _lastState = FormWindowState.Normal;

    // doesn't look like we can use RestoreBounds for this - any other built in way?
    private readonly ToolStripMenuItem[] _pasteMoreItems;

    private readonly SessionCounters _sessionCounters = new();

    private readonly string[] _pasteMoreItemsPrefixes =
    {
    "&1. ", "&2. ", "&3. ", "&4. ", "&5. ",
    "&6. ", "&7. ", "&8. ", "&9. ", "1&0. "
};

    // --------------------------------------------------------------------
    // Workflow State
    // --------------------------------------------------------------------

    private bool _abort;
    private bool _ignoreNoBots;
    private bool _clearPageListOnProjectChange;
    private bool _pageReload;
    private bool _doDiffInBotMode;
    private bool _loggingEnabled;

    private bool _skippable = true;
    private bool _shuttingDown;

    private string _lastArticle = string.Empty;
    private string _settingsFile = string.Empty;
    private string _settingsFileDisplay = string.Empty;

    private const int MaxRetries = 10;

    private int _oldSelection;
    private int _retries;
    private int _sameArticleNudges;
    private int _actionOnLoad;

    private ArticleRedirected _articleWasRedirected;

    // --------------------------------------------------------------------
    // Text Processing
    // --------------------------------------------------------------------

    private readonly HideText _removeText = new(false, true, true);

    private readonly List<string> _noParse = new();
    private readonly List<string> _noRetf = new();

    private readonly FindandReplace _findAndReplace = new();
    private readonly SubstTemplates _substTemplates = new();

    private RegExTypoFix _regexTypos;

    private readonly SkipOptions _skip = new();

    private readonly Twain.Core.ReplaceSpecial.ReplaceSpecial _replaceSpecial =
        new();

    private readonly Parsers _parser;

    // --------------------------------------------------------------------
    // Feature State
    // --------------------------------------------------------------------

    private bool _userTalkWarningsLoaded;
    private bool _templateRedirectsLoaded;
    private bool _datedTemplatesLoaded;
    private bool _renamedTemplateParametersLoaded;

    private Regex _userTalkTemplatesRegex;

    // --------------------------------------------------------------------
    // External Components
    // --------------------------------------------------------------------

    private readonly CustomModule _customModule = new();
    private readonly ExternalProgram _externalProgram = new();

    private RegexTester _regexTester;

    // --------------------------------------------------------------------
    // List Processing
    // --------------------------------------------------------------------

    // TODO(Twain): Move auxiliary tool lifetime and creation management out of
    // MainForm once List Comparer, List Splitter, and database search launch
    // workflows are consolidated.
    private ListComparer _comparer;
    private ListSplitter _splitter;
    private Twain.Core.DBScanner.DatabaseScanner _dataBaseScanner;

    // --------------------------------------------------------------------
    // Statistics
    // --------------------------------------------------------------------

    private readonly Stopwatch _sessionTimer = Stopwatch.StartNew();

    private readonly List<string> _recentList = new();

    private List<TypoStat> _typoStats;

    // --------------------------------------------------------------------
    // Diff
    // --------------------------------------------------------------------

    private readonly WikiDiff _diff = new();

    private readonly JsAdapter _diffScriptingAdapter;

    private readonly DiffGenerationService _diffGenerationService = new();

    private Task? _diffWebViewInitializationTask;

    private readonly SettingsPersistenceService _settingsPersistenceService = new();

    #endregion

    /// <summary>
    /// Gets the active wiki session for the main application window.
    /// </summary>
    /// <remarks>
    /// The session manages authentication, API communication, site information,
    /// and other shared state used throughout the application.
    /// </remarks>
    public Session TheSession
    { get; private set; }

    #region Constructor and MainForm load/resize
    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    /// <remarks>
    /// Performs application startup initialization, including loading application
    /// settings, creating the user interface, initializing the editing session,
    /// configuring application services, and preparing the main window for use.
    /// Long-running startup tasks continue during <see cref="MainForm_Load"/> after
    /// the form has been displayed.
    /// </remarks>
    public MainForm()
    {
        CheckSettings();

        _diffScriptingAdapter = new JsAdapter(this);

        _splashScreen.Show(this);
        RightToLeft = System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft
            ? RightToLeft.Yes : RightToLeft.No;

        _splashScreen.SetProgress(1);

        InitializeComponent();

        CreateWebView2DiffBrowser();

        _splashScreen.SetProgress(5);
        try
        {
            InitializeToolbarImages();

            _splashScreen.SetProgress(10);
            try
            {
                _parser = new Parsers(500, false);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex);
            }

            InitializeControls();

            InitializeSession();

            _profiles = InitializeProfiles();

            _splashScreen.SetProgress(15);

            _pasteMoreItems = InitializePasteMoreItems();
            InitializeFileDialogs();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Initializes the toolbar button images from the application's embedded resources.
    /// </summary>
    /// <remarks>
    /// Called during application startup after the form controls have been created.
    /// </remarks>
    private void InitializeToolbarImages()
    {
        btntsShowHide.Image = Resources.Showhide;
        btntsShowHideParameters.Image = Resources.Showhideparameters;
        btntsSave.Image = Resources.Save;

        btntsIgnore.Image = Resources.RightArrow;
        btntsStop.Image = Resources.Stop;
        btntsPreview.Image = Resources.preview;
        btntsChanges.Image = Resources.changes;
        btntsFalsePositive.Image = Resources.RollBack;
        btntsStart.Image = Resources.Run;
        btntsDelete.Image = Resources.Vista_trashcan_empty;
    }

    /// <summary>
    /// Initializes control default values and registers control event handlers.
    /// </summary>
    /// <remarks>
    /// Configures the initial state of user interface controls and wires the
    /// ListMaker events used to update the main window during list generation.
    /// </remarks>
    private void InitializeControls()
    {
        addToWatchList.SelectedIndex = 3;
        cmboCategorise.SelectedIndex = 0;
        cmboImages.SelectedIndex = 0;

        // TODO(Twain): Move ListMaker state observation behind a view-model or
        // coordinator so MainForm does not directly subscribe to processing events.
        listMaker.UserInputTextBox.ContextMenuStrip = mnuMakeFromTextBox;
        listMaker.BusyStateChanged += SetProgressBar;
        listMaker.NoOfArticlesChanged += UpdateButtons;
        listMaker.StatusTextChanged += UpdateListStatus;
        listMaker.cmboSourceSelect.SelectedIndexChanged +=
            ListMakerSourceSelectHandler;
    }

    /// <summary>
    /// Creates the editing session and initializes the editor.
    /// </summary>
    /// <remarks>
    /// The session must be created before any components that depend on it,
    /// including the profile manager and editor-related services.
    /// </remarks>
    private void InitializeSession()
    {
        TheSession = new Session(this);
        CreateEditor();
    }

    /// <summary>
    /// Creates and configures the profile manager.
    /// </summary>
    /// <returns>
    /// A fully initialized <see cref="Twain.Core.Profiles.AWBProfilesForm"/>
    /// instance with its event handlers registered.
    /// </returns>
    /// <remarks>
    /// Returning the configured instance allows the constructor to assign the
    /// readonly <c>Profiles</c> field while keeping the initialization logic
    /// encapsulated in this helper method.
    /// </remarks>
    private Twain.Core.Profiles.AWBProfilesForm InitializeProfiles()
    {
        Twain.Core.Profiles.AWBProfilesForm profiles =
            new Twain.Core.Profiles.AWBProfilesForm(TheSession);

        profiles.LoggedIn += ProfileLoggedIn;
        profiles.UserDefaultSettingsLoadRequired +=
            UserDefaultSettingsLoadRequired;

        return profiles;
    }

    /// <summary>
    /// Creates the ordered collection of additional paste menu items.
    /// </summary>
    /// <returns>
    /// The ordered collection of additional paste menu items.
    /// </returns>
    private ToolStripMenuItem[] InitializePasteMoreItems()
    {
        return new[]
        {
        PasteMore1,
        PasteMore2,
        PasteMore3,
        PasteMore4,
        PasteMore5,
        PasteMore6,
        PasteMore7,
        PasteMore8,
        PasteMore9,
        PasteMore10
    };
    }

    /// <summary>
    /// Initializes the default locations used by the settings file dialogs.
    /// </summary>
    /// <remarks>
    /// File dialogs default to the user's Documents folder to avoid saving
    /// settings files under the application data directory.
    /// </remarks>
    private void InitializeFileDialogs()
    {
        string documentsFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        saveXML.InitialDirectory = documentsFolder;
        openXML.InitialDirectory = documentsFolder;
    }

    // TODO(Twain): Move corrupt user-settings detection and recovery into
    // startup/settings infrastructure so MainForm is not responsible for
    // application configuration repair.
    /// <summary>
    /// Checks whether the current per-user application configuration can be opened
    /// and deletes the configuration file when it is found to be corrupt.
    /// </summary>
    /// <remarks>
    /// A corrupt user configuration file can prevent Twain from starting. When a
    /// <see cref="ConfigurationErrorsException"/> identifies an existing settings
    /// file, the file is deleted so that .NET can recreate it with default values.
    /// </remarks>
    public static void CheckSettings()
    {
        try
        {
            ConfigurationManager.OpenExeConfiguration(
                ConfigurationUserLevel.PerUserRoamingAndLocal);
        }
        catch (ConfigurationErrorsException ex)
        {
            string settingsFilePath = ex.Filename;

            if (string.IsNullOrEmpty(settingsFilePath) &&
                ex.InnerException is ConfigurationErrorsException innerException)
            {
                settingsFilePath = innerException.Filename;
            }

            if (string.IsNullOrEmpty(settingsFilePath) ||
                !File.Exists(settingsFilePath))
            {
                return;
            }

            FileInfo settingsFile = new FileInfo(settingsFilePath);

            if (settingsFile.Directory == null)
            {
                return;
            }

            using FileSystemWatcher watcher = new(
                settingsFile.Directory.FullName,
                settingsFile.Name);

            Tools.WriteDebug(
                $"Deleting corrupt settings file {settingsFilePath}",
                ex.Message);

            File.Delete(settingsFilePath);

            if (File.Exists(settingsFilePath))
            {
                watcher.WaitForChanged(WatcherChangeTypes.Deleted);
            }
        }
    }

    // TODO(Twain): Move command-line parsing into the application host and
    // return a strongly typed startup-options model instead of mutating MainForm.
    /// <summary>
    /// Parses supported command-line arguments and applies the requested startup options.
    /// </summary>
    /// <param name="args">
    /// The command-line arguments supplied to the application.
    /// </param>
    /// <remarks>
    /// Supported options are:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>/s &lt;file&gt;</c> loads the specified settings file. An <c>.xml</c>
    /// extension is added when no extension is supplied and the original path
    /// does not exist.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>/u &lt;profile&gt;</c> selects the profile to load after startup.
    /// </description>
    /// </item>
    /// </list>
    /// Unsupported arguments, missing values, and settings files that cannot be
    /// found are ignored.
    /// </remarks>
    public void ParseCommandLine(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];

            if (argument is not ("/s" or "/u") || i + 1 >= args.Length)
            {
                continue;
            }

            string value = args[++i];

            switch (argument)
            {
                case "/s":
                    string settingsFile = value;

                    if (string.IsNullOrEmpty(Path.GetExtension(settingsFile)) &&
                        !File.Exists(settingsFile))
                    {
                        settingsFile += ".xml";
                    }

                    if (File.Exists(settingsFile))
                    {
                        SettingsFile = settingsFile;
                    }

                    break;

                case "/u":
                    _profileToLoad = value;
                    break;
            }
        }
    }

    /// <summary>
    /// Gets or sets the path of the settings file currently associated with the application.
    /// </summary>
    /// <remarks>
    /// Updating the value also refreshes the main window title and notification-area
    /// tooltip to show the application name, revision number in debug builds, and
    /// the selected settings file name.
    /// </remarks>
    private string SettingsFile
    {
        get => _settingsFile;

        set
        {
            _settingsFile = value;

            string displayText = BuildSettingsFileDisplayText(value);

            Text = displayText;
            ntfyTray.Text = GetTrayTooltipText(displayText);
        }
    }

    // TODO(Twain): Separate settings-file state from window/tray presentation
    // so display text can be bound or applied by the UI layer independently.
    /// <summary>
    /// Builds the display text used for the main window title and notification-area tooltip.
    /// </summary>
    /// <param name="settingsFile">
    /// The path of the current settings file, or an empty value when no settings file is loaded.
    /// </param>
    /// <returns>
    /// The application display text, including the revision number and settings
    /// file name when available.
    /// </returns>
    private static string BuildSettingsFileDisplayText(string settingsFile)
    {
        string displayText = Program.Name;

        if (Variables.RevisionNumber > 0)
        {
            displayText += $" rev {Variables.RevisionNumber}";
        }

        if (!string.IsNullOrEmpty(settingsFile))
        {
            displayText += $" – {Path.GetFileName(settingsFile)}";
        }

        return displayText;
    }

    /// <summary>
    /// Limits display text to the maximum length supported by the notification-area tooltip.
    /// </summary>
    /// <param name="displayText">The text to display.</param>
    /// <returns>
    /// The original text when it fits, otherwise a truncated version.
    /// </returns>
    private static string GetTrayTooltipText(string displayText)
    {
        const int MaximumTrayTextLength = 63;
        const int TruncatedTrayTextLength = 62;

        return displayText.Length > MaximumTrayTextLength
            ? displayText[..TruncatedTrayTextLength]
            : displayText;
    }

    /// <summary>
    /// Stores the profile name requested from the command line so it can be
    /// logged in after startup initialization has completed.
    /// </summary>
    private string _profileToLoad = string.Empty;

    /// <summary>
    /// Completes application startup after the main form has loaded, including
    /// browser initialization, logging, settings restoration, plugin loading,
    /// client-version validation, and profile login.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private async void MainForm_Load(object sender, EventArgs e)
    {
        PrepareStartupUi();

        try
        {
            try
            {
                await InitializeWebView2DiffBrowserAsync();
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(
                    nameof(InitializeWebView2DiffBrowserAsync),
                    ex.ToString());

                ErrorHandler.HandleException(ex);
            }

            _splashScreen.SetProgress(25);

            InitializeLogging();

            RestoreWindowState();

            _splashScreen.SetProgress(25);

            Twain.Core.Plugin.PluginManager.LoadPluginsStartup(this);

            _splashScreen.SetProgress(50);

            LoadPrefs();

            InitializeBuildConfiguration();

            _splashScreen.SetProgress(60);
            UpdateButtons(null, null);

            _splashScreen.SetProgress(62);
            LoadRecentSettingsList();

            Updater.WaitForCompletion();

            ntfyTray.Visible = true;

            _splashScreen.SetProgress(80);

            _splashScreen.SetProgress(90);

            _profiles.Login(_profileToLoad);

            _splashScreen.SetProgress(95);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }

        CompleteStartup();
    }

    /// <summary>
    /// Handles changes to the main window's size and state.
    /// </summary>
    /// <remarks>
    /// Hides the main window when it is minimized to the notification area,
    /// if that behavior is enabled. Otherwise, records the last non-minimized
    /// window state so it can be restored when the user reopens the window
    /// from the notification area.
    /// </remarks>
    private void MainForm_Resize(object sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            if (_minimize) Visible = false;
        }
        else
            _lastState = WindowState; // remember if maximized or normal so can restore same when dbl click tray icon
    }

    /// <summary>
    /// Restores the main window's saved location, size, and state.
    /// </summary>
    /// <remarks>
    /// A minimized state is restored as normal because restoring Twain minimized
    /// may cause the diff display to lose its vertical scroll bar.
    /// </remarks>
    private void RestoreWindowState()
    {
        Location = Properties.Settings.Default.WindowLocation;
        Size = Properties.Settings.Default.WindowSize;

        FormWindowState savedWindowState =
            Properties.Settings.Default.WindowState;

        WindowState = savedWindowState == FormWindowState.Minimized
            ? FormWindowState.Normal
            : savedWindowState;
    }

    /// <summary>
    /// Initializes the application logging and article action log controls.
    /// </summary>
    private void InitializeLogging()
    {
        logControl.Initialise(listMaker);
        articleActionLogControl1.Initialise(listMaker);
    }

    /// <summary>
    /// Applies build configuration initialization that is common to both Debug
    /// and Release builds.
    /// </summary>
    /// <remarks>
    /// Calls the Debug and Release initialization routines. Each routine is
    /// conditionally compiled and performs work only when its corresponding
    /// build configuration is active.
    /// </remarks>
    private void InitializeBuildConfiguration()
    {
        EnableDebugMode();
        Release();
    }

    /// <summary>
    /// Prepares the main form and splash screen for startup initialization.
    /// </summary>
    private void PrepareStartupUi()
    {
        EditBoxTab.TabPages.Remove(tpTypos);

        StatusLabelText = "Initialising...";
        _splashScreen.SetProgress(20);

        Variables.MainForm = this;
        lblOnlyBots.BringToFront();

        _splashScreen.SetProgress(22);
    }

    // TODO (.NET10 Modernization):
    // Review startup completion responsibilities after the startup workflow has
    // been moved out of MainForm. Splash screen management and status updates may
    // belong in a dedicated startup coordinator or UI service.
    /// <summary>
    /// Finalizes application startup by clearing the status message, completing
    /// the splash screen progress indicator, and closing the splash screen.
    /// </summary>
    private void CompleteStartup()
    {
        StatusLabelText = string.Empty;
        _splashScreen.SetProgress(100);
        _splashScreen.Close();
    }
    #endregion

    #region Properties

    /// <summary>
    /// Gets the article currently being processed or displayed.
    /// </summary>
    internal Article TheArticle { get; private set; }

    // TODO(Twain): Decouple bot-mode workflow state from chkAutoMode.
    // BotMode is referenced throughout processing, typo handling, diff behavior,
    // and completion logic, so the checkbox should not remain the application
    // source of truth. Introduce explicit processing state and bind the UI to it.
    /// <summary>
    /// Gets or sets whether AWB is running in bot mode.
    /// </summary>
    private bool BotMode
    {
        get => chkAutoMode.Checked;
        set => chkAutoMode.Checked = value;
    }

    private bool _lowThreadPriority;
    /// <summary>
    /// Gets or sets whether the current thread uses the lowest priority.
    /// </summary>
    /// <remarks>
    /// Changing this value immediately updates the priority of the current thread.
    /// </remarks>
    private bool LowThreadPriority
    {
        get => _lowThreadPriority;

        set
        {
            _lowThreadPriority = value;

            Thread.CurrentThread.Priority = value
                ? ThreadPriority.Lowest
                : ThreadPriority.Normal;
        }
    }

    /// <summary>
    /// Tracks whether the list comparer should use the current article list.
    /// </summary>
    private int _listComparerUseCurrentArticleList;

    /// <summary>
    /// Tracks whether the list splitter should use the current article list.
    /// </summary>
    private int _listSplitterUseCurrentArticleList;

    /// <summary>
    /// Tracks whether the database scanner should use the current article list.
    /// </summary>
    private int _dbScannerUseCurrentArticleList;

    /// <summary>
    /// Indicates whether the user should be alerted by flashing the window.
    /// </summary>
    private bool _flash;

    /// <summary>
    /// Indicates whether the user should be alerted with an audible beep.
    /// </summary>
    private bool _beep;

    /// <summary>
    /// True if user has been warned in AWB session that articles with characters in Unicode private use area can't be saved
    /// </summary>
    private bool _userWarnedAboutUnicodePUA;

    /// <summary>
    /// True if AWB should be minimized to the system tray; False if it should minimize to the taskbar
    /// </summary>
    private bool _minimize;

    /// <summary>
    /// Indicates whether the current article list should be saved when AWB exits.
    /// </summary>
    private bool _saveArticleList = true;

    /// <summary>
    /// Indicates whether automatic saving of the edit box contents is enabled.
    /// </summary>
    private bool _autoSaveEditBoxEnabled;

    /// <summary>
    /// The file used to store automatic backups of the edit box contents.
    /// </summary>
    private string _autoSaveEditBoxFile =
        Path.Combine(Application.StartupPath, "Edit Box.txt");

    /// <summary>
    /// Indicates whether the "using AWB" edit summary tag should be suppressed.
    /// </summary>
    private bool _suppressUsingAWB;

    private decimal _autoSaveEditBoxPeriod = 60;
    /// <summary>
    /// Gets or sets the interval, in seconds, between automatic saves of the edit box.
    /// </summary>
    private decimal AutoSaveEditBoxPeriod
    {
        get => _autoSaveEditBoxPeriod;

        set
        {
            _autoSaveEditBoxPeriod = value;
            EditBoxSaveTimer.Interval = (int)(value * 1000m);
        }
    }

    /// <summary>
    /// Gets or sets the text displayed in the main status label.
    /// </summary>
    /// <remarks>
    /// Updates are marshaled to the UI thread when necessary. Assigning
    /// <see langword="null"/> or an empty string restores the default
    /// application name and version text.
    /// </remarks>
    private string StatusLabelText
    {
        get => lblStatusText.Text;
        set
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => StatusLabelText = value));
                return;
            }

            lblStatusText.Text = string.IsNullOrEmpty(value)
                ? $"{Program.Name} {Program.VersionString}"
                : value;
        }
    }

    /// <summary>
    /// Gets or sets whether ignored articles are added to the log file.
    /// </summary>
    /// <remarks>
    /// Changing this value updates the stop-button layout and controls the
    /// visibility of the false-positive actions.
    /// </remarks>
    // TODO (.NET10 Modernization):
    // Replace this write-only property with a clearly named method such as
    // UpdateIgnoredArticleControls(bool). The current implementation performs
    // UI layout changes rather than representing state, so a method would
    // better communicate its behavior. This requires updating all callers and
    // should be done as a dedicated refactoring to avoid changing behavior.
    private bool AddIgnoredToLogFile
    {
        set
        {
            btnStop.Location = value
                ? new Point(220, 62)
                : new Point(156, 62);

            btnStop.Size = value
                ? new Size(51, 23)
                : new Size(117, 23);

            btnFalsePositive.Visible = value;
            btntsFalsePositive.Visible = value;
        }
    }

    /// <summary>
    /// Indicates whether the moving-average timer is currently shown.
    /// </summary>
    private bool _timerShown = true;

    /// <summary>
    /// Gets or sets whether the moving-average timer is displayed.
    /// </summary>
    /// <remarks>
    /// Changing this value immediately refreshes the timer display.
    /// </remarks>
    private bool ShowMovingAverageTimer
    {
        get => _timerShown;

        set
        {
            _timerShown = value;
            ShowTimer();
        }
    }

    #endregion

    #region MainProcess

    /// <summary>
    /// Verifies that the current session is still logged in.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the user remains logged in; otherwise,
    /// <see langword="false"/> after the logged-off state has been handled.
    /// </returns>
    private bool CheckLoginStatus()
    {
        if (TheSession.User.IsLoggedIn)
            return true;

        HandleLogoff();
        return false;
    }

    /// <summary>
    /// Stops processing, informs the user that the session has been lost,
    /// and opens the profiles dialog when it is not already visible.
    /// </summary>
    private void HandleLogoff()
    {
        MessageBox.Show(
            "You've been logged off, probably due to loss of session data.\r\n" +
            "Please re-login.",
            "Logged off",
            MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation);

        Stop();

        if (!_profiles.Visible)
            _profiles.ShowDialog(this);
    }

    /// <summary>
    /// Connects the session editor events to their corresponding
    /// main-form event handlers.
    /// </summary>
    // TODO (.NET10 Modernization):
    // Rename CreateEditor() to SubscribeToSessionEvents() because the method
    // does not create an editor; it attaches MainForm handlers to session events.
    // Before renaming, verify that the method is called only once per session.
    // Repeated calls could register duplicate handlers and cause events to be
    // processed more than once.
    private void CreateEditor()
    {
        TheSession.PreviewComplete += PreviewComplete;
        TheSession.ExceptionCaught += ApiEditExceptionCaught;
        TheSession.SaveComplete += PageSaved;
        TheSession.MaxlagExceeded += MaxlagExceeded;
        TheSession.OpenComplete += OpenComplete;
        TheSession.LoggedOff += LoggedOff;
    }

    /// <summary>
    /// Handles notification that the active editing session has been logged off.
    /// </summary>
    /// <param name="sender">The editor that raised the event.</param>
    private void LoggedOff(AsyncApiEdit sender)
    {
        DisableButtons();
    }

    /// <summary>
    /// Handles a server maxlag response by incrementing the consecutive retry
    /// count and scheduling another attempt until the retry limit is reached.
    /// </summary>
    /// <param name="sender">
    /// The editor that reported the maxlag condition.
    /// </param>
    /// <param name="maxlag">
    /// The maxlag value reported by the server.
    /// </param>
    /// <param name="retryAfter">
    /// The number of seconds to wait before attempting to restart processing.
    /// </param>
    private void MaxlagExceeded(
        AsyncApiEdit sender,
        double _maxlag,
        int retryAfter)
    {
        _retries++;

        if (_retries < MaxRetries)
        {
            StartDelayedRestartTimer(retryAfter);
            return;
        }

        Stop();

        MessageBox.Show(
            this,
            $"Maxlag was exceeded {MaxRetries} times in a row. " +
            "Processing has stopped. Please try again later when the server is under less load.",
            "Stopped",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    /// <summary>
    /// Handles exceptions raised during asynchronous API editing operations.
    /// </summary>
    /// <param name="sender">
    /// The editor that raised the exception. This parameter is required by the
    /// event signature but is not currently used.
    /// </param>
    /// <param name="ex">
    /// The exception raised by the editing operation.
    /// </param>
    /// <remarks>
    /// Routes known exception types to their specialized handlers and performs
    /// the appropriate recovery action, such as retrying, skipping, stopping,
    /// or reporting unexpected errors.
    /// </remarks>
    private void ApiEditExceptionCaught(
        AsyncApiEdit sender,
        Exception ex)
    {
        if (ex is InterwikiException)
        {
            SkipPage(ex.Message);
        }
        else if (ex is SpamlistException spamlistException)
        {
            HandleSpamlistException(spamlistException);
        }
        else if (ex is ApiErrorException apiError)
        {
            HandleApiError(apiError);
        }
        else if (ex is ApiBlankException)
        {
            Tools.WriteDebug("ApiBlankException", ex.Message);
            StartDelayedRestartTimer();
        }
        else if (ex is NewMessagesException)
        {
            WeHaveNewMessages();
        }
        else if (ex is LoggedOffException)
        {
            HandleLogoff();
        }
        else if (ex is CaptchaException)
        {
            MessageBox.Show(
                "Captcha required, is the user account auto-confirmed etc?",
                "Captcha Required");

            Stop();
        }
        else if (ex is InvalidTitleException)
        {
            SkipPage("Invalid title");
        }
        else if (ex is RedirectToSpecialPageException)
        {
            SkipPage("Page is a redirect to a special page");
        }
        else if (IsRetryableNetworkException(ex))
        {
            HandleNetworkException(ex);
        }
        else if (ex is SharedRepoException)
        {
            MessageBox.Show(
                "Cannot move this file to the specified target, as it exists in a shared repo (such as commons).",
                "Target file exists in shared repo");
        }
        else if (ex is MediaWikiSaysNoException)
        {
            MessageBox.Show(
                "MediaWiki prevented you from making that edit. Chances are it's spam or abuse filter related",
                "MediaWiki says no");

            SkipPage("Edit blocked by spam/abuse filter");
        }
        else
        {
            Stop();
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Handles an error returned by the MediaWiki API.
    /// </summary>
    /// <param name="exception">
    /// The API exception containing the error code and associated message.
    /// </param>
    // TODO (.NET10 Modernization):
    // Inventory exception types, MediaWiki API error-code literals, and repeated
    // recovery behavior throughout the solution. Centralize stable MediaWiki
    // protocol values in a focused MediaWikiApiErrorCodes class, organize custom
    // exceptions by subsystem, and extract repeated retry, skip, stop, logging,
    // and user-notification behavior into reusable handlers where appropriate.
    // Avoid creating a single global error manager coupled to MainForm or other
    // UI components.
    private void HandleApiError(ApiErrorException exception)
    {
        switch (exception.ErrorCode)
        {
            case "editconflict":
                HandleEditConflict();
                break;

            case "writeapidenied":
                NoWriteApiRight();
                break;

            case "customcssprotected":
                SkipPage("You're not allowed to edit custom CSS pages");
                break;

            case "customjsprotected":
                SkipPage("You're not allowed to edit custom JavaScript pages");
                break;

            case "badmd5":
                SkipPage(
                    "API MD5 hash error: The page you are editing may contain " +
                    "an unsupported or invalid Unicode character");
                break;

            case "assertuserfailed":
                HandleLogoff();
                break;

            case "badtoken":
                // Likely a session timeout forced by MediaWiki, so reprocess the page.
                Tools.WriteDebug(
                    "ApiExceptionCaught/badtoken",
                    exception.Message);

                StartDelayedRestartTimer();
                break;

            case "blocked":
                MessageBox.Show(
                    "API reports this user is blocked from editing.",
                    "User blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);

                Stop();
                break;

            case "readonly":
                MessageBox.Show(
                    exception.ApiErrorMessage,
                    "Wiki read-only",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);

                StartDelayedRestartTimer();
                break;

            case "tpt-target-page":
                SkipPage("Translation pages cannot currently be edited");
                break;

            case "titleblacklist-forbidden-edit":
                SkipPage(
                    "TitleBlacklist prevents this title from being created");
                break;

            default:
                Tools.WriteDebug(
                    "ApiExceptionCaught",
                    exception.Message);

                StartDelayedRestartTimer();
                break;
        }
    }

    /// <summary>
    /// Handles an edit conflict by reloading the current article and reapplying
    /// AWB's automated changes.
    /// </summary>
    /// <remarks>
    /// Manual edits made in the editor cannot currently be merged with the latest
    /// revision and will be lost when processing restarts.
    /// </remarks>
    // TODO (.NET10 Modernization):
    // Preserve manual editor changes during edit-conflict recovery by comparing
    // the original article text, the current editor text, and the latest server
    // revision, then presenting or applying a three-way merge before retrying.
    private void HandleEditConflict()
    {
        MessageBox.Show(
            this,
            "There has been an edit conflict. Twain will now re-apply its " +
            "changes on the updated page.\r\nPlease re-review the changes " +
            "before saving. Any custom edits will be lost and must be " +
            "re-added manually.",
            "Edit Conflict",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

        NudgeTimer.Stop();
        Start();
    }

    /// <summary>
    /// Determines whether an exception represents a retryable network failure.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>
    /// <see langword="true"/> when processing should be retried; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool IsRetryableNetworkException(
        Exception exception)
    {
        for (Exception? current = exception;
             current != null;
             current = current.InnerException)
        {
            if (current is WebException or HttpRequestException)
            {
                return true;
            }

            if (current is IOException
                && current.Message.Contains(
                    "0x2746",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Handles an edit rejected by the spam blacklist.
    /// </summary>
    /// <param name="exception">
    /// The exception containing the URL rejected by the spam blacklist.
    /// </param>
    private void HandleSpamlistException(
        SpamlistException exception)
    {
        string message = exception.URL;

        if (!BotMode
            && !chkSkipSpamFilter.Checked
            && MessageBox.Show(
                $"{message}.\r\nTry and edit again?",
                "Spam Blacklist",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            Start();
            return;
        }

        SkipPage(message);
    }

    /// <summary>
    /// Handles a retryable network exception and schedules processing to restart.
    /// </summary>
    /// <param name="exception">
    /// The network exception that interrupted processing.
    /// </param>
    private void HandleNetworkException(
        Exception exception)
    {
        StatusLabelText = exception.Message;

        if (Tools.WriteDebugEnabled)
        {
            Tools.WriteTextFile(
                exception.Message,
                "Log.txt",
                true);
        }

        // TODO (.NET10 Modernization):
        // Support retry delays from HttpResponseMessage when the remaining
        // HttpWebRequest-based request paths are migrated to HttpClient.
        // Some HTTP responses specify a delay before another request should be
        // attempted. MediaWiki currently uses 429 and 503 responses with a delay
        // expressed in seconds.
        WebException? webException =
            FindException<WebException>(exception);

        if (webException?.Response is HttpWebResponse response)
        {
            int restartDelay = Tools.ParseRetry(response);

            if (restartDelay >= 0)
            {
                StartDelayedRestartTimer(restartDelay);
                return;
            }
        }

        StartDelayedRestartTimer();
    }

    /// <summary>
    /// Searches an exception and its inner-exception chain for the first exception
    /// of the specified type.
    /// </summary>
    /// <typeparam name="TException">
    /// The exception type to locate.
    /// </typeparam>
    /// <param name="exception">
    /// The exception at the beginning of the chain to inspect.
    /// </param>
    /// <returns>
    /// The first matching exception in the chain, or <see langword="null"/> when
    /// no matching exception is found.
    /// </returns>
    private static TException? FindException<TException>(
        Exception exception)
        where TException : Exception
    {
        for (Exception? current = exception;
             current != null;
             current = current.InnerException)
        {
            if (current is TException matchingException)
            {
                return matchingException;
            }
        }

        return null;
    }

    /// <summary>
    /// Opens the specified article in the editor.
    /// </summary>
    /// <param name="title">The title of the article to open.</param>
    private void OpenPage(string title)
    {
        StatusLabelText = "Loading...";

        bool followRedirects =
            followRedirectsToolStripMenuItem.Checked
            && !chkSkipIfRedirect.Checked;

        TheSession.Editor.Open(title, followRedirects);
    }

    private bool _stopProcessing;
    private bool _inStart;
    private bool _startAgain;

    /// <summary>
    /// Starts article processing when the session is registered and idle.
    /// </summary>
    /// <remarks>
    /// Prevents recursive entry into the start sequence. If another start request
    /// occurs while processing is being started, one additional pass is requested
    /// and performed after the current pass completes.
    /// </remarks>
    private void Start()
    {
        if (TheSession.Status != WikiStatusResult.Registered
            || TheSession.IsBusy)
        {
            return;
        }

        if (_inStart)
        {
            _startAgain = true;
            return;
        }

        _inStart = true;

        try
        {
            do
            {
                _startAgain = false;
                StartArticleProcessing();
            }
            while (_startAgain);
        }
        finally
        {
            _inStart = false;
        }
    }

    /// <summary>
    /// Prepares the next article in the list and begins loading it for processing.
    /// </summary>
    /// <remarks>
    /// Stops immediately when processing has been cancelled, the edit summary is
    /// invalid, the article list is empty, or the selected title is invalid.
    /// Unexpected failures schedule processing to restart.
    /// </remarks>
    // TODO (.NET10 Modernization):
    // Classify failures from article startup so transient network/session errors
    // can be retried while unexpected programming or UI errors are reported and
    // processing is stopped instead of entering a restart loop.
    private void StartArticleProcessing()
    {
        if (_stopProcessing)
        {
            return;
        }

        try
        {
            Tools.WriteDebug(Name, "Starting");

            Shutdown();
            PrepareArticleProcessingUi();

            if (!ValidateEditSummary())
            {
                return;
            }

            PrepareArticleProcessingState();

            if (!TryGetSelectedArticleTitle(out string title))
            {
                return;
            }

            title = NormalizeSelectedArticleTitle(title);

            PrepareCurrentArticle(title);
            BeginArticleLoading(title);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                Name,
                $"StartArticleProcessing() error: {ex.Message}");

            StartDelayedRestartTimer();
        }
    }

    /// <summary>
    /// Prepares the editing controls for a new article-processing cycle.
    /// </summary>
    private void PrepareArticleProcessingUi()
    {
        txtEdit.Enabled = true;
        txtReviewEditSummary.Enabled = true;
        SetEditToolBarEnabled(true);

        txtReviewEditSummary.Clear();

        DisableButtons();

        _skippable = true;
        txtEdit.Clear();

        ArticleInfo(true);
    }

    /// <summary>
    /// Validates the current edit summary and adds it to the history when needed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool ValidateEditSummary()
    {
        bool editSummaryRequired =
            Variables.Project != ProjectEnum.custom
            && !Twain.Core.Plugin.PluginManager.AWBPlugins.Any();

        if (editSummaryRequired
            && string.IsNullOrEmpty(cmboEditSummary.Text))
        {
            MessageBox.Show(
                "Please enter an edit summary.",
                "Edit Summary",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);

            Stop();
            return false;
        }

        if (!string.IsNullOrEmpty(cmboEditSummary.Text)
            && !cmboEditSummary.Items.Contains(cmboEditSummary.Text))
        {
            cmboEditSummary.Items.Add(cmboEditSummary.Text);
        }

        return true;
    }

    /// <summary>
    /// Resets timers and state used by the previous processing cycle.
    /// </summary>
    private void PrepareArticleProcessingState()
    {
        StopDelayedRestartTimer();
    }

    /// <summary>
    /// Retrieves the title of the currently selected article.
    /// </summary>
    /// <param name="title">
    /// When successful, receives the selected article title.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when an article is available; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool TryGetSelectedArticleTitle(out string title)
    {
        if (listMaker.NumberOfArticles < 1)
        {
            StopSaveInterval();
            lblTimer.Text = string.Empty;
            StopProgressBar();

            StatusLabelText =
                "No articles in list; use Make list to add articles.";

            Text = _settingsFileDisplay;
            listMaker.MakeListEnabled = true;

            title = string.Empty;
            return false;
        }

        title = listMaker.SelectedArticle().Name;

        if (Tools.IsValidTitle(title))
        {
            return true;
        }

        // TheArticle must exist before SkipPage() can process the invalid entry.
        TheArticle = new Article(title, string.Empty);
        SkipPage("Invalid page title");

        title = string.Empty;
        return false;
    }

    /// <summary>
    /// Canonicalizes the selected title and updates the article list when it
    /// changes.
    /// </summary>
    /// <param name="title">The selected article title.</param>
    /// <returns>The canonicalized article title.</returns>
    private string NormalizeSelectedArticleTitle(string title)
    {
        string canonicalTitle =
            Parsers.CanonicalizeTitleAggressively(title);

        if (string.Equals(
            canonicalTitle,
            title,
            StringComparison.Ordinal))
        {
            return title;
        }

        listMaker.ReplaceArticle(
            listMaker.SelectedArticle(),
            new Article(canonicalTitle));

        return canonicalTitle;
    }

    /// <summary>
    /// Creates and initializes the article state for the selected title.
    /// </summary>
    /// <param name="title">The title being processed.</param>
    private void PrepareCurrentArticle(string title)
    {
        if (BotMode)
        {
            NudgeTimer.StartMe();
        }

        if (TheArticle != null
            && !string.Equals(
                TheArticle.Name,
                title,
                StringComparison.Ordinal))
        {
            _lastArticle = string.Empty;
        }

        TheArticle = new Article(title, string.Empty);

        // Ensure the editor's Find operation uses the current search text.
        txtEdit.ResetFind();

        NewHistory(title);
        NewWhatLinksHere(title);

        EditBoxSaveTimer.Enabled = _autoSaveEditBoxEnabled;
    }

    /// <summary>
    /// Starts progress reporting and opens the selected article.
    /// </summary>
    /// <param name="title">The title to load.</param>
    private void BeginArticleLoading(string title)
    {
        StartProgressBar();
        OpenPage(title);
    }

    // TODO (.NET10 Modernization):
    // Review these error-tracking collections after the MainForm cleanup is
    // complete. Determine whether they should remain cached fields, become
    // method-local variables, or be replaced with a single strongly typed error
    // reporting model. Also evaluate whether these dictionaries duplicate state
    // already owned by Article and can be eliminated entirely.
    private Dictionary<int, int> _unbalancedBrackets = new();
    private Dictionary<int, int> _badCiteParameters = new();
    private Dictionary<int, int> _duplicateBannerShellParameters = new();
    private Dictionary<int, int> _unclosedTags = new();
    private Dictionary<int, int> _wikilinkedHeaders = new();
    private Dictionary<int, int> _deadLinks = new();
    private Dictionary<int, int> _ambiguousCiteDates = new();
    private Dictionary<int, int> _targetlessLinks = new();
    private Dictionary<int, int> _doublePipeLinks = new();
    private Dictionary<int, int> _otherErrors = new();
    private Dictionary<int, int> _userSignatures = new();

    private List<string> _unknownWikiProjectBannerShellParameters = new();
    private List<string> _unknownMultipleIssuesParameters = new();

    private readonly SortedDictionary<int, int> _errors = new();

    /// <summary>
    /// Skips the current redirect while preserving the redirect-specific logging
    /// context.
    /// </summary>
    /// <param name="reason">
    /// The reason the redirect is being skipped.
    /// </param>
    /// <remarks>
    /// Redirect processing uses a different trace listener than the primary
    /// article. This wrapper ensures the skip is recorded against the redirect
    /// before control returns to the main article processing workflow.
    /// </remarks>
    private void SkipRedirect(string reason)
    {
        SkipPage(reason);
    }

    /// <summary>
    /// Matches Unicode characters in the Private Use Area (PUA).
    /// </summary>
    private static readonly Regex _unicodePrivateUseRegex =
        new(@"\p{IsPrivateUse}", RegexOptions.Compiled);

    // TODO (.NET10 Modernization):
    // Replace the remaining BackgroundRequest-based processing with the modern
    // task-based infrastructure used elsewhere in the application.
    private BackgroundRequest _runProcessPageBackground;

    /// <summary>
    /// Handles a successfully loaded page, performs applicable skip checks, and
    /// begins the main page-processing workflow.
    /// </summary>
    /// <param name="page">Information and content for the loaded page.</param>
    private void PageLoaded(PageInfo page)
    {
        if (!LoadSuccessApi())
        {
            return;
        }

        _retries = 0;

        if (_stopProcessing)
        {
            return;
        }

        if (!InitializeLoadedArticle(page))
        {
            return;
        }

        InitializeArticleTrace(page.Title);

        if (HandleRedirect(page))
        {
            return;
        }

        HandleNormalizedTitle(page);

        ErrorHandler.CurrentRevision = page.RevisionID;

        if (HandlePageReload())
        {
            return;
        }

        if (SkipChecks(
            !skipIfContains.After,
            !skipIfNotContains.After))
        {
            return;
        }

        if (HandlePageInUse())
        {
            return;
        }

        if (HandleUnicodePrivateUseCharacter(page.Text))
        {
            return;
        }

        BeginPageProcessing();
    }

    /// <summary>
    /// Creates the current article and verifies that processing may continue.
    /// </summary>
    /// <param name="page">The loaded page.</param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool InitializeLoadedArticle(PageInfo page)
    {
        if (Namespace.IsSpecial(Namespace.Determine(page.Title)))
        {
            SkipPage("Page is a special page");

            // Preserve or remove this return based on the intended legacy behavior.
            return false;
        }

        TheArticle = new Article(page);

        return preParseModeToolStripMenuItem.Checked
            || CheckLoginStatus();
    }

    /// <summary>
    /// Initializes tracing and updates the window title for the loaded article.
    /// </summary>
    /// <param name="title">The loaded article title.</param>
    private void InitializeArticleTrace(string title)
    {
        if (Program.MyTrace.HaveOpenFile)
        {
            Program.MyTrace.WriteBulletedLine(
                "The application has begun processing",
                true,
                true,
                true);
        }
        else
        {
            Program.MyTrace.Initialise();
        }

        Text = $"{_settingsFileDisplay} – {title}";
    }

    // TODO (.NET10 Modernization):
    // Move redirect decisions, skip-rule evaluation, article validation, and
    // Unicode content checks out of MainForm into testable page-processing
    // components. Keep only workflow coordination and direct UI updates in the
    // form.

    /// <summary>
    /// Handles redirect skipping, redirect loops, namespace filtering, and list
    /// replacement for redirected pages.
    /// </summary>
    /// <param name="page">The loaded page.</param>
    /// <returns>
    /// <see langword="true"/> when page processing has been stopped; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool HandleRedirect(PageInfo page)
    {
        if (chkSkipIfRedirect.Checked
            && Tools.IsRedirect(page.Text))
        {
            SkipPage("Page is a redirect");
            return true;
        }

        bool wasRedirected = PageInfo.WasRedirected(page);

        if (!followRedirectsToolStripMenuItem.Checked
            || !wasRedirected
            || _pageReload)
        {
            return false;
        }

        if (page.TitleChangedStatus.HasFlag(
            PageTitleStatus.RedirectLoop))
        {
            SkipRedirect("Recursive redirect");
            return true;
        }

        if (filterOutNonMainSpaceToolStripMenuItem.Checked
            && Namespace.Determine(page.Title) != Namespace.Article)
        {
            SkipRedirect("Page redirects to non-mainspace");
            return true;
        }

        _articleWasRedirected?.Invoke(
            page.OriginalTitle,
            page.Title);

        listMaker.ReplaceArticle(
            new Article(page.OriginalTitle),
            TheArticle);

        return false;
    }

    /// <summary>
    /// Replaces the original list entry when the API normalized the requested
    /// page title.
    /// </summary>
    /// <param name="page">The loaded page.</param>
    private void HandleNormalizedTitle(PageInfo page)
    {
        if (page.TitleChangedStatus != PageTitleStatus.Normalised)
        {
            return;
        }

        listMaker.ReplaceArticle(
            new Article(page.OriginalTitle),
            TheArticle);
    }

    /// <summary>
    /// Completes processing for a page that was reloaded for diff generation.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the reload was handled; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool HandlePageReload()
    {
        if (!_pageReload)
        {
            return false;
        }

        _pageReload = false;
        GetDiff();

        return true;
    }

    /// <summary>
    /// Handles pages containing an in-use template.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the page was skipped; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool HandlePageInUse()
    {
        if (!TheArticle.IsInUse)
        {
            return false;
        }

        if (chkSkipIfInuse.Checked)
        {
            SkipPage("Page contains {{inuse}}");
            return true;
        }

        if (!BotMode
            && !preParseModeToolStripMenuItem.Checked)
        {
            MessageBox.Show(
                "This page has the \"Inuse\" tag; consider skipping it.",
                "Page in use",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        return false;
    }

    // TODO (.NET10 Modernization):
    // Re-evaluate this check after replacing the legacy editor. This skip exists
    // because the current RichTextBox-based editor cannot reliably preserve
    // Unicode Private Use Area (PUA) characters. If the new editor fully supports
    // these characters, remove or relax this restriction and update the user
    // warning accordingly.
    /// <summary>
    /// Detects Unicode Private Use Area characters that cannot safely be edited
    /// in the current editor.
    /// </summary>
    /// <param name="articleText">The loaded article text.</param>
    /// <returns>
    /// <see langword="true"/> when the page was skipped; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool HandleUnicodePrivateUseCharacter(
        string articleText)
    {
        Match privateUseMatch =
            _unicodePrivateUseRegex.Match(articleText);

        if (!privateUseMatch.Success)
        {
            return false;
        }

        if (!_userWarnedAboutUnicodePUA
            && !preParseModeToolStripMenuItem.Checked
            && !BotMode)
        {
            string surroundingText =
                GetSurroundingText(
                    articleText,
                    privateUseMatch.Index,
                    25);

            MessageBox.Show(
                "This page contains character(s) in the Unicode Private Use Area "
                + "and cannot safely be edited with this application. The page will now be "
                + "skipped. Surrounding text of the first character is: "
                + surroundingText);

            _userWarnedAboutUnicodePUA = true;
        }

        SkipPage(
            "Page has character in Unicode Private Use Area");

        return true;
    }

    /// <summary>
    /// Returns text surrounding a character position.
    /// </summary>
    private static string GetSurroundingText(
        string text,
        int index,
        int radius)
    {
        int start = Math.Max(0, index - radius);
        int end = Math.Min(text.Length, index + radius);
        int length = end - start;

        return text.Substring(start, length);
    }

    /// <summary>
    /// Starts automatic processing in the background or completes processing
    /// synchronously when automatic actions are disabled.
    /// </summary>
    private void BeginPageProcessing()
    {
        if (!automaticallyDoAnythingToolStripMenuItem.Checked)
        {
            CompleteProcessPage();
            return;
        }

        _runProcessPageBackground = new BackgroundRequest();
        _runProcessPageBackground.Complete +=
            AutomaticallyDoAnythingComplete;

        _runProcessPageBackground.Execute(
            ProcessPageBackground);
    }

    /// <summary>
    /// Updates the processing status, establishes the current page context, and
    /// runs automatic page processing on the background worker.
    /// </summary>
    private void ProcessPageBackground()
    {
        StatusLabelText = preParseModeToolStripMenuItem.Checked
            ? "Processing page (pre-parse mode)"
            : "Processing page";

        // TODO (.NET10 Modernization):
        // Replace the global CurrentPage assignment with a scoped, thread-safe page
        // context that is established immediately around page processing and restored
        // afterward. Consider AsyncLocal or explicit context passing so concurrent or
        // unrelated failures are not attributed to the wrong article.
        ErrorHandler.CurrentPage = TheArticle.Name;

        ProcessPage(TheArticle, true);
    }

    /// <summary>
    /// Performs skip checks after page processing and completes processing when
    /// the article is not skipped or aborted.
    /// </summary>
    private void RunSkipChecks()
    {
        // TODO (.NET10 Modernization):
        // Replace the global CurrentPage value with scoped page-processing context.
        // The context is currently cleared before typo statistics and post-processing,
        // so failures in those operations may lack useful page information.
        ErrorHandler.CurrentPage = string.Empty;

        UpdateCurrentTypoStats();

        if (_abort)
        {
            return;
        }

        if (TheArticle.SkipArticle)
        {
            // ProcessPage() should already have logged the skip reason.
            SkipPageReasonAlreadyProvided();
            return;
        }

        if (_skippable && TrySkipBasedOnArticleChanges())
        {
            return;
        }

        Variables.Profiler.Profile("Skip checks");

        if (SkipChecks(
            skipIfContains.After,
            skipIfNotContains.After))
        {
            return;
        }

        CompleteProcessPage();
    }

    /// <summary>
    /// Applies skip rules based on the kinds of changes made to the current
    /// article.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the article was skipped; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool TrySkipBasedOnArticleChanges()
    {
        if ((chkSkipNoChanges.Checked || BotMode)
            && TheArticle.NoArticleTextChanged)
        {
            SkipPage("No change");
            return true;
        }

        if (chkSkipWhitespace.Checked
            && chkSkipCasing.Checked
            && TheArticle.OnlyWhiteSpaceAndCasingChanged)
        {
            SkipPage("Only whitespace/casing changed");
            return true;
        }

        if (chkSkipWhitespace.Checked
            && TheArticle.OnlyWhiteSpaceChanged)
        {
            SkipPage("Only whitespace changed");
            return true;
        }

        if (chkSkipCasing.Checked
            && TheArticle.OnlyCasingChanged)
        {
            SkipPage("Only casing changed");
            return true;
        }

        if (chkSkipMinorGeneralFixes.Checked
            && chkGeneralFixes.Checked
            && TheArticle.OnlyMinorGeneralFixesChanged)
        {
            SkipPage("Only minor general fix changes");
            return true;
        }

        if (chkSkipGeneralFixes.Checked
            && chkGeneralFixes.Checked
            && TheArticle.OnlyGeneralFixesChanged)
        {
            SkipPage("Only general fix changes");
            return true;
        }

        if (chkSkipNoPageLinks.Checked
            && !WikiRegexes.WikiLinksOnly.IsMatch(
                TheArticle.ArticleText))
        {
            SkipPage("Page contains no links");
            return true;
        }

        if (chkSkipCosmetic.Checked
            && (TheArticle.NoArticleTextChanged
                || TheArticle.OnlyCosmeticChanged))
        {
            SkipPage("Only cosmetic changes made");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles completion of the background page-processing operation and runs
    /// the remaining skip checks on the UI thread.
    /// </summary>
    /// <param name="req">
    /// The completed background request.
    /// </param>
    private void AutomaticallyDoAnythingComplete(
        BackgroundRequest req)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(
                new System.Windows.Forms.MethodInvoker(
                    RunSkipChecks));

            return;
        }

        RunSkipChecks();
    }

    /// <summary>
    /// Completes processing of the current article by updating the editor,
    /// calculating alerts, displaying the configured result, applying
    /// highlighting, and restoring the user interface.
    /// </summary>
    private void CompleteProcessPage()
    {
        WriteProcessedArticleToEditor();

        if (TrySkipArticleWithoutAlerts())
        {
            return;
        }

        if (HandlePreParseCompletion())
        {
            return;
        }

        PrepareEditorForHighlighting();

        if (_abort)
        {
            RestoreInterfaceAfterAbort();
            return;
        }

        CompleteInteractiveProcessing();
    }

    /// <summary>
    /// Copies the processed article text into the editor and updates article
    /// statistics and alerts.
    /// </summary>
    private void WriteProcessedArticleToEditor()
    {
        Tools.WriteDebug(
            "WriteProcessedArticleToEditor",
            $"Starting. Article text length: {TheArticle.ArticleText.Length}; " +
            $"InvokeRequired: {InvokeRequired}; " +
            $"Editor disposed: {txtEdit.IsDisposed}");

        Tools.WriteDebug(
            "WriteProcessedArticleToEditor",
            "Toggling WordWrap off/on.");

        txtEdit.WordWrap = !txtEdit.WordWrap;
        txtEdit.WordWrap = !txtEdit.WordWrap;

        Tools.WriteDebug(
            "WriteProcessedArticleToEditor",
            "WordWrap toggled successfully. Assigning editor text.");

        txtEdit.Text = TheArticle.ArticleText;

        Tools.WriteDebug(
            "WriteProcessedArticleToEditor",
            $"Editor text assigned successfully. Editor length: {txtEdit.TextLength}");

        Variables.Profiler.Profile("Set edit box text");

        Tools.WriteDebug(
            "WriteProcessedArticleToEditor",
            $"Profiler updated. BotMode: {BotMode}");

        if (!BotMode)
        {
            Tools.WriteDebug(
                "WriteProcessedArticleToEditor",
                "Calling ArticleInfo(false).");

            ArticleInfo(false);

            Tools.WriteDebug(
                "WriteProcessedArticleToEditor",
                "ArticleInfo(false) completed.");
        }

        Tools.WriteDebug(
            "WriteProcessedArticleToEditor",
            "Completed.");
    }

    /// <summary>
    /// Skips the current page when alert-based skipping is enabled and no alerts
    /// were produced.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the page was skipped; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool TrySkipArticleWithoutAlerts()
    {
        if (chkSkipIfNoAlerts.Checked
            && lbAlerts.Items.Count == 0)
        {
            SkipPage("Page has no alerts");
            return true;
        }

        Variables.Profiler.Profile("Alerts");

        return false;
    }

    /// <summary>
    /// Completes processing for pre-parse mode and advances to the next article.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when pre-parse processing was handled; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool HandlePreParseCompletion()
    {
        if (!preParseModeToolStripMenuItem.Checked)
        {
            return false;
        }

        SavePreParseSettingsIfDue();

        if (listMaker.NextArticle())
        {
            Start();
        }
        else
        {
            Stop();
            SaveFinalPreParseSettings();
        }

        _sessionCounters.NumberOfPagesParsed++;

        return true;
    }

    // TODO (.NET10 Modernization):
    // Review whether the NumberOfIgnoredEdits > 5 condition is still necessary.
    // Combined with NumberOfIgnoredEdits % 10 == 0, the first possible save occurs
    // at 10 ignored edits, so the greater-than-five check currently appears
    // redundant. Verify the original intent and persisted settings behavior before
    // removing it.
    /// <summary>
    /// Saves the current settings after each group of ten ignored edits when
    /// automatic settings saving is enabled.
    /// </summary>
    private void SavePreParseSettingsIfDue()
    {
        if (!autoSaveSettingsToolStripMenuItem.Checked
            || string.IsNullOrEmpty(SettingsFile)
            || _sessionCounters.NumberOfIgnoredEdits <= 5
            || _sessionCounters.NumberOfIgnoredEdits % 10 != 0)
        {
            return;
        }

        SavePrefs(SettingsFile);
    }

    /// <summary>
    /// Saves settings when pre-parsing finishes unless they were saved during the
    /// current ten-edit interval.
    /// </summary>
    private void SaveFinalPreParseSettings()
    {
        if (!autoSaveSettingsToolStripMenuItem.Checked
            || string.IsNullOrEmpty(SettingsFile)
            || _sessionCounters.NumberOfIgnoredEdits % 10 == 0)
        {
            return;
        }

        SavePrefs(SettingsFile);
    }

    /// <summary>
    /// Hides the editor while syntax highlighting is prepared, preventing
    /// intermediate rendering from being displayed.
    /// </summary>
    private void PrepareEditorForHighlighting()
    {
        if (syntaxHighlightEditBoxToolStripMenuItem.Checked)
        {
            txtEdit.Visible = false;
        }
    }

    // TODO (.NET10 Modernization):
    // Verify editor visibility when processing is aborted after the edit control
    // has been hidden for highlighting. The abort path currently enables buttons
    // but does not explicitly restore txtEdit.Visible.
    /// <summary>
    /// Restores the interface after page processing was aborted.
    /// </summary>
    private void RestoreInterfaceAfterAbort()
    {
        EnableButtons();
        _abort = false;
    }

    /// <summary>
    /// Completes normal interactive or bot-mode processing for the current page.
    /// </summary>
    private void CompleteInteractiveProcessing()
    {
        UpdateUserNotifications();

        bool showDiffInBotMode =
            BotMode && _doDiffInBotMode;

        if (HandleBotModeCompletion(showDiffInBotMode))
        {
            return;
        }

        DisplayConfiguredPageResult();

        PageWatched = TheSession.Page.IsWatched;

        Variables.Profiler.Profile("ActionOnLoad");

        UpdateDefaultEditSummary();
        ApplyEditorHighlighting();
        FinalizeProcessedPageInterface();
    }

    /// <summary>
    /// Handles bot-mode completion before the normal interactive display and
    /// highlighting workflow.
    /// </summary>
    /// <param name="showDiffInBotMode">
    /// Whether a diff should still be generated in bot mode.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when no further interactive processing is required;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool HandleBotModeCompletion(
        bool showDiffInBotMode)
    {
        if (!BotMode)
        {
            // Clear a timer label left behind after bot mode is disabled.
            lblBotTimer.Text = string.Empty;
            return false;
        }

        // Do not overwrite the stopped status or restart the bot loop when the
        // user pressed Stop while background processing was running.
        if (StatusLabelText != "Stopped")
        {
            StatusLabelText = "Ready to save";
            StartDelayedAutoSaveTimer();
        }

        if (showDiffInBotMode)
        {
            return false;
        }

        txtReviewEditSummary.Text =
            MakeDefaultEditSummary();

        return true;
    }

    // TODO (.NET10 Modernization):
    // Replace the numeric actionOnLoad values with a named enum after verifying
    // compatibility with persisted settings.
    /// <summary>
    /// Displays the configured result after page processing.
    /// </summary>
    private void DisplayConfiguredPageResult()
    {
        switch (_actionOnLoad)
        {
            case 0:
                GetDiff();
                break;

            case 1:
                GetPreview();
                break;

            case 2:
                GuiUpdateAfterProcessing();

                txtEdit.Focus();
                txtEdit.SelectionLength = 0;
                break;
        }
    }

    /// <summary>
    /// Generates and displays the default edit summary.
    /// </summary>
    private void UpdateDefaultEditSummary()
    {
        txtReviewEditSummary.Text =
            MakeDefaultEditSummary();

        Variables.Profiler.Profile(
            "Make Edit summary");
    }

    /// <summary>
    /// Applies syntax, find-result, and alert highlighting to the processed
    /// article.
    /// </summary>
    private void ApplyEditorHighlighting()
    {
        // Keep the editor hidden until all highlighting is complete.
        txtEdit.Visible = false;

        ApplySyntaxHighlighting();
        ApplyFindHighlighting();
        ApplyAlertHighlighting();

        txtEdit.Visible = true;

        Variables.Profiler.Profile(
            "Find/alert highlighting");
    }

    /// <summary>
    /// Applies syntax highlighting and restores the initial editor position when
    /// focus-at-end is disabled.
    /// </summary>
    private void ApplySyntaxHighlighting()
    {
        if (!syntaxHighlightEditBoxToolStripMenuItem.Checked)
        {
            return;
        }

        HighlightSyntax();

        Variables.Profiler.Profile(
            "Syntax highlighting");

        if (focusAtEndOfEditTextBoxToolStripMenuItem.Checked)
        {
            return;
        }

        txtEdit.SetEditBoxSelection(0, 0);
        txtEdit.Select(0, 0);
        txtEdit.ScrollToCaret();
    }

    /// <summary>
    /// Highlights all current find matches when enabled.
    /// </summary>
    private void ApplyFindHighlighting()
    {
        if (highlightAllFindToolStripMenuItem.Checked)
        {
            HighlightAllFind();
        }
    }

    /// <summary>
    /// Clears previous error information and highlights current alerts when
    /// configured.
    /// </summary>
    private void ApplyAlertHighlighting()
    {
        // Always clear stale errors when alert highlighting has been disabled.
        _errors.Clear();

        if (!scrollToAlertsToolStripMenuItem.Checked)
        {
            return;
        }

        EditBoxTab.SelectedTab = tpEdit;
        HighlightErrors();
    }

    // TODO (.NET10 Modernization):
    // Restore editor visibility with try/finally so highlighting failures cannot
    // leave the edit control hidden.
    //
    // TODO (.NET10 Modernization):
    // Review the completion workflow for robustness and exception safety.
    // Specifically verify that:
    // - the editor is always made visible again if processing is aborted or an
    //   exception occurs after it has been hidden for highlighting;
    // - the progress bar and status indicators are restored correctly on every
    //   exit path, including bot-mode early returns;
    // - UI cleanup is consolidated so partially completed processing cannot leave
    //   the interface in an inconsistent state.
    /// <summary>
    /// Restores the final editor position, selects the Save button, updates the
    /// status, and stops the progress indicator.
    /// </summary>
    private void FinalizeProcessedPageInterface()
    {
        if (focusAtEndOfEditTextBoxToolStripMenuItem.Checked)
        {
            txtEdit.Select(
                txtEdit.Text.Length,
                0);

            txtEdit.ScrollToCaret();
        }

        btnSave.Select();

        // Do not overwrite the stopped status when Stop was pressed while the
        // background worker was still running.
        if (StatusLabelText != "Stopped")
        {
            StatusLabelText = "Ready to save";
        }

        StopProgressBar();
    }

    /// <summary>
    /// Adds error locations from the specified collection to the master error
    /// list without replacing previously recorded locations.
    /// </summary>
    /// <param name="source">
    /// Error positions keyed by character offset, with the associated highlight
    /// length.
    /// </param>
    private void AddErrors(
        IEnumerable<KeyValuePair<int, int>> source)
    {
        foreach (KeyValuePair<int, int> error in source)
        {
            _errors.TryAdd(error.Key, error.Value);
        }
    }

    /// <summary>
    /// Highlights up to the configured maximum number of collected editor errors,
    /// then clears the active selection.
    /// </summary>
    /// <param name="errors">
    /// Error positions keyed by character offset, with the associated highlight
    /// length.
    /// </param>
    private void HighlightEditorErrors(
        IEnumerable<KeyValuePair<int, int>> errors)
    {
        const int maximumHighlightedErrors = 100;

        int highlightedCount = 0;

        foreach (KeyValuePair<int, int> error in errors)
        {
            if (highlightedCount >= maximumHighlightedErrors)
            {
                break;
            }

            // TODO (.NET10 Modernization):
            // Verify that error highlight offsets and lengths are validated before they
            // are applied. Detection results may become stale if the editor content
            // changes after analysis, potentially producing an invalid selection range.
            RedSelection(error.Key, error.Value);
            highlightedCount++;
        }

        if (highlightedCount > 0)
        {
            txtEdit.Select(0, 0);
        }
    }

    /// <summary>
    /// Merges detected editor errors in category-priority order and highlights
    /// the resulting locations.
    /// </summary>
    private void HighlightErrors()
    {
        // Categories are added in priority order. The first error recorded at a
        // given character position determines the highlight length.
        AddErrors(_unbalancedBrackets);
        AddErrors(_badCiteParameters);
        AddErrors(_duplicateBannerShellParameters);
        AddErrors(_deadLinks);
        AddErrors(_ambiguousCiteDates);
        AddErrors(_unclosedTags);
        AddErrors(_wikilinkedHeaders);
        AddErrors(_targetlessLinks);
        AddErrors(_doublePipeLinks);
        AddErrors(_otherErrors);
        AddErrors(_userSignatures);

        HighlightEditorErrors(_errors);
    }

    /// <summary>
    /// Applies syntax highlighting to the edit box while suppressing text-change
    /// handling caused by the highlighting operation.
    /// </summary>
    private void HighlightSyntax()
    {
        txtEdit.TextChanged -= txtEdit_TextChanged;

        try
        {
            txtEdit.HighlightSyntax();
        }
        finally
        {
            txtEdit.TextChanged += txtEdit_TextChanged;
        }
    }

    // TODO (.NET10 Modernization):
    // Error categories are added in priority order. When multiple categories
    // identify the same character position, the first category is retained.
    /// <summary>
    /// Highlights the collected editor error locations.
    ///
    /// Applies highlighting for up to the first 100 error positions to
    /// maintain editor responsiveness, then clears the active selection
    /// so the final highlighted range is not left selected.
    /// </summary>
    /// <param name="errors">
    /// Collection of editor error positions keyed by character offset,
    /// with the associated highlight length.
    /// </param>
    private void HighlightEditorErrors(SortedDictionary<int, int> errors)
    {
        // performance: only highlight first 100 errors
        int done = 0;
        foreach (KeyValuePair<int, int> a in errors)
        {
            RedSelection(a.Key, a.Value);
            done++;

            if (done > 100)
                break;
        }

        // If any text highlighted, don't leave last text selected
        if (done > 0)
            txtEdit.Select(0, 0);
    }

    private WebView2 _diffWebView;

    /// <summary>
    /// Creates the WebView2 diff control in the existing diff-browser container.
    /// </summary>
    private void CreateWebView2DiffBrowser()
    {
        if (_diffWebView != null)
        {
            return;
        }

        Control parent = webBrowser.Parent
            ?? throw new InvalidOperationException(
                "The existing diff browser does not have a parent container.");

        _diffWebView = new WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        parent.Controls.Add(_diffWebView);
    }

    /// <summary>
    /// Initializes the WebView2 diff renderer, ensuring that only one
    /// initialization attempt runs at a time.
    /// </summary>
    private async Task InitializeWebView2DiffBrowserAsync()
    {
        Task initializationTask =
            _diffWebViewInitializationTask ??=
                InitializeWebView2DiffBrowserCoreAsync();

        try
        {
            await initializationTask;
        }
        catch
        {
            if (ReferenceEquals(
                    _diffWebViewInitializationTask,
                    initializationTask))
            {
                _diffWebViewInitializationTask = null;
            }

            throw;
        }
    }

    /// <summary>
    /// Performs the WebView2 diff renderer initialization.
    /// </summary>
    private async Task InitializeWebView2DiffBrowserCoreAsync()
    {
        WebView2 diffWebView = _diffWebView
            ?? throw new InvalidOperationException(
                "The WebView2 diff control has not been created.");

        if (diffWebView.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(_diffWebView));
        }

        await diffWebView.EnsureCoreWebView2Async();

        if (diffWebView.IsDisposed)
            return;

        CoreWebView2 core = diffWebView.CoreWebView2
            ?? throw new InvalidOperationException(
                "WebView2 initialization completed without creating CoreWebView2.");

        ConfigureWebView2DiffBrowser(core);

        core.WebMessageReceived -= DiffWebView_WebMessageReceived;
        core.WebMessageReceived += DiffWebView_WebMessageReceived;

        Tools.WriteDebug(
            nameof(InitializeWebView2DiffBrowserAsync),
            "WebView2 initialized.");
    }

    /// <summary>
    /// Configures the WebView2 instance used to display generated article diffs.
    /// </summary>
    /// <param name="core">
    /// The initialized WebView2 core whose browser settings will be configured.
    /// </param>
    /// <remarks>
    /// The diff viewer is restricted to the features required for rendering and
    /// interacting with locally generated diff content. Web messaging and script
    /// execution remain enabled so the document can send commands to the host and
    /// support diff navigation behavior.
    /// </remarks>
    private static void ConfigureWebView2DiffBrowser(
        CoreWebView2 core)
    {
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;

        core.Settings.IsWebMessageEnabled = true;
        core.Settings.IsScriptEnabled = true;
    }

    /// <summary>
    /// Handles commands sent from the generated WebView2 diff document.
    /// </summary>
    /// <param name="sender">
    /// The WebView2 core that received the message.
    /// </param>
    /// <param name="e">
    /// Information about the message received from the diff document.
    /// </param>
    private async void DiffWebView_WebMessageReceived(
        object sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            DiffWebMessage message =
                JsonSerializer.Deserialize<DiffWebMessage>(
                    e.WebMessageAsJson);

            if (message == null ||
                string.IsNullOrWhiteSpace(message.Action))
            {
                Tools.WriteDebug(
                    nameof(DiffWebView_WebMessageReceived),
                    "An empty or invalid diff message was received.");

                return;
            }

            switch (message.Action)
            {
                case "UndoChange":
                    if (message.LeftLine.HasValue &&
                        message.RightLine.HasValue)
                    {
                        await UndoChangeGenericAsync(
                            DiffChangeMode.Change,
                            message.LeftLine.Value,
                            message.RightLine.Value);
                    }

                    break;

                case "UndoDeletion":
                    if (message.LeftLine.HasValue &&
                        message.RightLine.HasValue)
                    {
                        await UndoChangeGenericAsync(
                            DiffChangeMode.Deletion,
                            message.LeftLine.Value,
                            message.RightLine.Value);
                    }

                    break;

                case "UndoAddition":
                    if (message.RightLine.HasValue)
                    {
                        await UndoChangeGenericAsync(
                            DiffChangeMode.Addition,
                            0,
                            message.RightLine.Value);
                    }

                    break;

                case "GoTo":
                    if (message.RightLine.HasValue)
                    {
                        GoTo(message.RightLine.Value);
                    }

                    break;

                default:
                    Tools.WriteDebug(
                        nameof(DiffWebView_WebMessageReceived),
                        $"Unknown diff action: {message.Action}");

                    break;
            }
        }
        catch (JsonException ex)
        {
            Tools.WriteDebug(
                nameof(DiffWebView_WebMessageReceived),
                $"Invalid diff message: {ex.Message}");
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    // TODO (.NET10 Modernization):
    // Harden the WebView2 diff renderer as a non-browsing display surface.
    // Disable unused script, host-object, and web-message capabilities, and
    // explicitly handle link navigation and new-window requests so generated
    // diff content cannot navigate the embedded control unexpectedly.
    //
    // TODO (.NET10 Modernization):
    // Replace NavigateToString() or provide a fallback for generated diff HTML
    // approaching WebView2's 2 MB input limit. Large articles may generate diff
    // documents that exceed the supported size.
    /// <summary>
    /// Attempts to render generated diff HTML in the WebView2 diff control.
    /// </summary>
    /// <param name="html">The complete diff HTML document.</param>

    private void RenderWebView2Diff(string html)
    {
        if (_diffWebView == null ||
            _diffWebView.IsDisposed ||
            _diffWebView.CoreWebView2 == null)
        {
            Tools.WriteDebug(
                nameof(RenderWebView2Diff),
                "WebView2 was unavailable when the diff was rendered.");

            return;
        }

        ShowDiffBrowser();

        _diffWebView.NavigateToString(html);
    }

    /// <summary>
    /// Displays the legacy browser used for article previews.
    /// </summary>
    private void ShowPreviewBrowser()
    {
        if (_diffWebView != null)
        {
            _diffWebView.Visible = false;
        }

        webBrowser.Visible = true;
    }

    /// <summary>
    /// Displays the WebView2 control used for article diffs.
    /// </summary>
    private void ShowDiffBrowser()
    {
        webBrowser.Visible = false;

        if (_diffWebView != null)
        {
            _diffWebView.Visible = true;
        }
    }

    /// <summary>
    /// Renders generated diff HTML and waits for the WebView2 document to finish
    /// loading.
    /// </summary>
    /// <param name="html">
    /// The complete diff HTML document.
    /// </param>
    /// <returns>
    /// A task that completes when the diff navigation has finished.
    /// </returns>
    private async Task RenderWebView2DiffAsync(string html)
    {
        if (_diffWebView == null ||
            _diffWebView.IsDisposed ||
            _diffWebView.CoreWebView2 == null)
        {
            Tools.WriteDebug(
                nameof(RenderWebView2DiffAsync),
                "WebView2 was unavailable when the diff was rendered.");

            return;
        }

        ShowDiffBrowser();

        var navigationCompletion =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        void NavigationCompleted(
            object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                navigationCompletion.TrySetResult(true);
            }
            else
            {
                navigationCompletion.TrySetException(
                    new InvalidOperationException(
                        $"WebView2 diff navigation failed: {e.WebErrorStatus}."));
            }
        }

        _diffWebView.NavigationCompleted += NavigationCompleted;

        try
        {
            _diffWebView.NavigateToString(html);

            await navigationCompletion.Task;
        }
        finally
        {
            _diffWebView.NavigationCompleted -= NavigationCompleted;
        }
    }

    /// <summary>
    /// Skips the article based on protection level and contains/not contains logic
    /// </summary>
    /// <param name="checkContains">Whether to test contains logic</param>
    /// <param name="checkNotContains">Whether to test not contains logic</param>
    /// <returns>Whether the page has been skipped</returns>
    private bool SkipChecks(bool checkContains, bool checkNotContains)
    {
        if (!TheSession.User.CanEditPage(TheSession.Page))
        {
            SkipPage("Page is protected");
            return true;
        }

        if (!TheSession.User.CanCreatePage(TheSession.Page))
        {
            SkipPage("Page is protected from creation");
            return true;
        }

        if (checkContains && skipIfContains.CheckEnabled && skipIfContains.Matches(TheArticle))
        {
            SkipPage(skipIfContains.SkipReason);
            return true;
        }

        if (checkNotContains && skipIfNotContains.CheckEnabled && skipIfNotContains.Matches(TheArticle))
        {
            SkipPage(skipIfNotContains.SkipReason);
            return true;
        }

        return false;
    }

    // TODO (.NET10 Modernization):
    // Remove this legacy WebBrowser-specific document reset when the remaining
    // browser functionality is migrated to WebView2.
    /// <summary>
    /// Clears the contents of the embedded browser document.
    /// </summary>
    private void ClearBrowser()
    {
        webBrowser.Document?.OpenNew(true);
    }

    /// <summary>
    /// Alerts the user by flashing the application window and optionally playing
    /// a notification sound when the application does not currently have focus.
    /// </summary>
    private void BleepFlash()
    {
        if (ContainsFocus)
        {
            return;
        }

#if !MONO
        if (_flash)
        {
            Tools.FlashWindow(this);
        }
#endif

        if (_beep)
        {
            Tools.Beep();
        }
    }

    // TODO (.NET10 Modernization):
    // Confirm that the reusable talk-message dialog is disposed with MainForm.
    private readonly TalkMessage _talkMessageDialog = new TalkMessage();

    /// <summary>
    /// Stops processing and prompts the user when new talk-page messages are
    /// detected.
    /// </summary>
    private void WeHaveNewMessages()
    {
        Stop();
        Focus();
        TheSession.RequireUpdate();

        // Do not display a second instance while the reusable dialog is visible.
        if (_talkMessageDialog.Visible)
        {
            return;
        }

        if (_talkMessageDialog.ShowDialog(this) != DialogResult.Yes)
        {
            return;
        }

        Tools.OpenUserTalkInBrowser(TheSession.User.Name);

        // The external browser may be logged in as a different user, so clear the
        // notification explicitly rather than relying on the talk page visit.
        TheSession.Editor.SynchronousEditor.ClearNewMessages();
    }

    /// <summary>
    /// Stops processing and informs the user that the current account does not
    /// have permission to perform automatic edits on the active wiki.
    /// </summary>
    private void NoWriteApiRight()
    {
        Stop();

        MessageBox.Show(
            this,
            "This user doesn't have enough privileges to make automatic edits on this wiki.",
            "Permission error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    // TODO (.NET10 Modernization):
    // Narrow this exception handling once the expected API and session failure
    // types are known. Catching all exceptions can hide programming errors and
    // makes load failures difficult to classify.
    /// <summary>
    /// Validates the loaded API page state and determines whether article
    /// processing may continue.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when the page was skipped, new messages were
    /// handled, or validation failed.
    /// </returns>
    private bool LoadSuccessApi()
    {
        try
        {
            bool pageExists = TheSession.Editor.Page.Exists;

            if (!pageExists && radSkipNonExistent.Checked)
            {
                SkipPage("Non-existent page");
                return false;
            }

            if (pageExists && radSkipExistent.Checked)
            {
                SkipPage("Existing page");
                return false;
            }

            if (!preParseModeToolStripMenuItem.Checked &&
                TheSession.User.HasMessages)
            {
                WeHaveNewMessages();
                return false;
            }

            NudgeTimer.Reset();
            return true;
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
            return false;
        }
    }

    // TODO (.NET10 Modernization):
    // Confirm whether the NumberOfEdits > 5 check is still required. Combined
    // with NumberOfEdits % 10 == 0, the first automatic save already occurs at
    // ten successful edits.
    /// <summary>
    /// Completes post-save processing after an article has been saved successfully.
    /// </summary>
    /// <param name="sender">
    /// The API editor that completed the save operation.
    /// </param>
    /// <param name="saveInfo">
    /// Information returned by the successful save operation.
    /// </param>
    private void PageSaved(
        AsyncApiEdit sender,
        SaveInfo saveInfo)
    {
        ClearBrowser();
        txtEdit.Clear();

        // Gradually reduce the retry delay after successful saves.
        if (_restartDelay > 5)
        {
            _restartDelay--;
        }

        _sessionCounters.NumberOfEdits++;
        _lastArticle = string.Empty;

        listMaker.Remove(TheArticle);

        NudgeTimer.Stop();
        _sameArticleNudges = 0;

        if (EditBoxTab.SelectedTab == tpHistory)
        {
            EditBoxTab.SelectedTab = tpEdit;
        }

        if (_loggingEnabled)
        {
            TheArticle.LogListener.NewId = saveInfo.NewId;
            TheArticle.LogListener.URLLong = Variables.URLLong;

            logControl.AddLog(
                false,
                TheArticle.LogListener);
        }

        UpdateOverallTypoStats();

        if (!listMaker.Any() &&
            _autoSaveEditBoxEnabled)
        {
            EditBoxSaveTimer.Enabled = false;
        }

        _retries = 0;

        // Persist the active settings file after every ten successful edits when
        // automatic settings saving is enabled.
        if (ShouldAutoSaveSettings())
        {
            SavePrefs(SettingsFile);
        }

        Start();
    }

    /// <summary>
    /// Determines whether the current settings file should be automatically saved.
    /// </summary>
    private bool ShouldAutoSaveSettings()
    {
        return autoSaveSettingsToolStripMenuItem.Checked
            && _sessionCounters.NumberOfEdits % 10 == 0
            && !string.IsNullOrEmpty(SettingsFile);
    }

    /// <summary>
    /// Completes a page skip when the skip reason has already been recorded.
    /// </summary>
    /// <remarks>
    /// Resets the editor and timers, removes the current article from the list,
    /// records the skip when logging is enabled, and continues processing the next
    /// article. Processing is stopped when the article cannot be removed safely.
    /// </remarks>
    private void SkipPageReasonAlreadyProvided()
    {
        try
        {
            ResetSkippedPageState();

            bool articleRemoved = listMaker.Remove(TheArticle);

            _sameArticleNudges = 0;

            if (!articleRemoved)
            {
                HandleSkippedPageRemovalFailure();
                return;
            }

            CompleteSuccessfulPageSkip();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    // TODO (.NET10 Modernization):
    // Define consistent recovery behavior for failures during page-skip cleanup.
    // An exception after partially resetting editor, timer, or article state may
    // leave the workflow unable to continue safely.
    /// <summary>
    /// Resets editor, timer, and counter state before removing a skipped article.
    /// </summary>
    private void ResetSkippedPageState()
    {
        TheSession.Editor.Reset();

        _sessionCounters.NumberOfIgnoredEdits++;
        StopDelayedAutoSaveTimer();
        NudgeTimer.Stop();
        txtEdit.Clear();
    }

    /// <summary>
    /// Handles failure to remove a skipped article from the active list.
    /// </summary>
    private void HandleSkippedPageRemovalFailure()
    {
        // Historical safeguard against repeatedly loading and skipping the same
        // article when automatic list removal fails.
        TheArticle = null;

        MessageBox.Show(
            this,
            "Application failed to automatically remove the page from the list while "
            + "skipping the page. Please remove it manually.",
            "Page removal from list failed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation);

        Stop();
    }

    /// <summary>
    /// Records a successful skip, clears the current article, and continues
    /// processing.
    /// </summary>
    private void CompleteSuccessfulPageSkip()
    {
        if (_loggingEnabled)
        {
            logControl.AddLog(
                true,
                TheArticle.LogListener);
        }

        TheArticle = null;
        _retries = 0;

        Start();
    }

    // TODO (.NET10 Modernization):
    // Replace the string-based skip source with a strongly typed enum or separate
    // methods so user, plugin, and AWB skips cannot be misclassified by a typo.
    /// <summary>
    /// Records the supplied skip reason for the current article and completes the
    /// page-skip workflow.
    /// </summary>
    /// <param name="reason">
    /// The skip source or reason. The values <c>user</c> and <c>plugin</c> are
    /// recorded through their dedicated trace methods; all other non-empty values
    /// are recorded as AWB skip reasons.
    /// </param>
    private void SkipPage(string reason)
    {
        if (TheArticle == null)
        {
            DisableButtons();
            return;
        }

        switch (reason)
        {
            case "user":
                TheArticle.Trace.UserSkipped();
                break;

            case "plugin":
                TheArticle.Trace.PluginSkipped();
                break;

            default:
                if (!string.IsNullOrEmpty(reason))
                {
                    TheArticle.Trace.AWBSkipped(reason);
                }

                break;
        }

        SkipPageReasonAlreadyProvided();
    }

    /// <summary>
    /// Fully processes a page, applying all needed changes
    /// </summary>
    /// <param name="theArticle">Page to process</param>
    /// <param name="mainProcess">True if the page is being processed for save as usual,
    /// otherwise (Re-parse in context menu, prefetch, etc) false</param>
    private void ProcessPage(Article theArticle, bool mainProcess)
    {
        bool process = true;
        _typoStats = null;

        Variables.Profiler.Start("ProcessPage(\"" + theArticle.Name + "\")");

        try
        {
            // Must be performed regardless of general fixes, otherwise there may be breakage
            theArticle.AWBChangeArticleText("Fixes for Unicode compatibility",
                                            _parser.FixUnicode(theArticle.ArticleText),
                                            true);

            if (_noParse.Contains(theArticle.Name))
                process = false;

            if (!_ignoreNoBots &&
                !Parsers.CheckNoBots(theArticle.ArticleText, TheSession.User.Name))
            {
                theArticle.AWBSkip("Restricted by {{bots}}/{{nobots}}");
                return;
            }

            Variables.Profiler.Profile("Initial skip checks");

            if (!RunExtensionProcessing(theArticle))
            {
                return;
            }

            ApplyWholeArticleUnicodify(theArticle, process);

            // find and replace before general fixes
            // Do not apply skip checks when reparsing
            if (chkFindandReplace.Checked)
            {
                theArticle.PerformFindAndReplace(_findAndReplace, _substTemplates, _replaceSpecial,
                                                 (mainProcess && chkSkipWhenNoFAR.Checked), (mainProcess && chkSkipOnlyMinorFaR.Checked), false);

                Variables.Profiler.Profile("F&R");

                theArticle.DoFaRSkips(_findAndReplace);
                if (theArticle.SkipArticle)
                    return;
            }

            if (!ApplyCategorisationChanges(theArticle))
            {
                return;
            }

            Variables.Profiler.Profile("Categories");

            if (process)
            {
                if (chkGeneralFixes.Checked)
                {
                    theArticle.PerformUniversalGeneralFixes();
                    Variables.Profiler.Profile("Universal Genfixes");
                }

                if (theArticle.CanDoGeneralFixes)
                {
                    if (chkGeneralFixes.Checked)
                    {
                        EnsureGeneralFixResourcesLoaded();

                        theArticle.PerformGeneralFixes(_parser, _removeText, _skip,
                                                       replaceReferenceTagsToolStripMenuItem.Checked,
                                                       restrictDefaultsortChangesToolStripMenuItem.Checked,
                                                       noMOSComplianceFixesToolStripMenuItem.Checked);
                    }

                    Variables.Profiler.Profile("Mainspace Genfixes");

                    if (!ApplyAutoTagging(
                        theArticle,
                        mainProcess))
                    {
                        return;
                    }

                    Variables.Profiler.Profile("Auto-tagger");
                }
                else if (chkGeneralFixes.Checked)
                {
                    if (theArticle.NameSpaceKey == Namespace.UserTalk)
                    {
                        if (!_userTalkWarningsLoaded)
                        {
                            LoadUserTalkWarnings();
                            Variables.Profiler.Profile("loadUserTalkWarnings");
                        }

                        theArticle.PerformUserTalkGeneralFixes(_removeText, _userTalkTemplatesRegex,
                                                               _skip.SkipNoUserTalkTemplatesSubstd);
                    }
                    else if (theArticle.CanDoTalkGeneralFixes)
                    {
                        theArticle.PerformTalkGeneralFixes(_removeText);
                    }
                    Variables.Profiler.Profile("Talk Genfixes");
                }
            }

            // RegexTypoFix
            if (chkRegExTypo.Checked && _regexTypos != null && !BotMode && !Namespace.IsTalk(theArticle.NameSpaceKey))
            {
                if (!_noRetf.Contains(theArticle.Name))
                {
                    theArticle.PerformTypoFixes(_regexTypos, chkSkipIfNoRegexTypo.Checked);
                    Variables.Profiler.Profile("Typos");
                    _typoStats = _regexTypos.GetStatistics();
                }
                else if (chkSkipIfNoRegexTypo.Checked)
                    TheArticle.Trace.AWBSkipped("No typo fixes (Title blacklisted from RegExTypoFix Typo Fixing)");

                if (theArticle.SkipArticle)
                {
                    if (mainProcess)
                    {
                        // update stats only if not called from e.g. 'Re-parse' than could be clicked repeatedly
                        OverallTypoStats.UpdateStats(_typoStats, true);
                        UpdateTypoCount();
                    }
                }
            }

            // find and replace after general fixes
            // Do not apply skip checks when reparsing
            if (chkFindandReplace.Checked)
            {
                theArticle.PerformFindAndReplace(_findAndReplace, _substTemplates, _replaceSpecial,
                                                 (mainProcess && chkSkipWhenNoFAR.Checked), (mainProcess && chkSkipOnlyMinorFaR.Checked), true);

                theArticle.DoFaRSkips(_findAndReplace);

                Variables.Profiler.Profile("F&R (2nd)");

                if (theArticle.SkipArticle) return;
            }

            ApplyAppendOrPrependText(theArticle);
            Variables.Profiler.Profile("Append Text");

            if (!ApplyImageChanges(theArticle))
            {
                return;
            }

            Variables.Profiler.Profile("Files");

            if (!ApplyDisambiguation(theArticle))
            {
                return;
            }

            Variables.Profiler.Profile("Disambiguate");
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);

            string stackTrace = ex.StackTrace ?? string.Empty;

            // Don't remove the page after a regular-expression error;
            // the page itself is not responsible for the failure.
            if (!stackTrace.Contains("System.Text.RegularExpressions"))
            {
                theArticle.Trace.AWBSkipped("Exception: " + ex.Message);
            }
            else
            {
                _skippable = false;
            }

            Stop();
            StopDelayedAutoSaveTimer();
        }
        finally
        {
            Variables.Profiler.Flush();
        }
    }

    /// <summary>
    /// Captures the current user-interface processing settings for use by the
    /// article-processing pipeline.
    /// </summary>
    /// <returns>
    /// A snapshot containing the processing options currently selected in the
    /// user interface.
    /// </returns>
    /// <remarks>
    /// This method forms the boundary between MainForm controls and the processing
    /// configuration consumed by the article-processing pipeline. Processing code
    /// should use the returned option values rather than reading user-interface
    /// controls directly.
    /// </remarks>
    private MainProcessOptions CreateMainProcessOptions()
    {
        return new MainProcessOptions
        {
            IgnoreNoBots = _ignoreNoBots,

            FindAndReplaceEnabled = chkFindandReplace.Checked,
            SkipWhenNoFindAndReplace = chkSkipWhenNoFAR.Checked,
            SkipOnlyMinorFindAndReplace = chkSkipOnlyMinorFaR.Checked,

            GeneralFixesEnabled = chkGeneralFixes.Checked,
            ReplaceReferenceTags = replaceReferenceTagsToolStripMenuItem.Checked,
            RestrictDefaultSortChanges =
                restrictDefaultsortChangesToolStripMenuItem.Checked,
            NoMosComplianceFixes =
                noMOSComplianceFixesToolStripMenuItem.Checked,

            RegexTypoFixEnabled = chkRegExTypo.Checked,
            SkipIfNoRegexTypo = chkSkipIfNoRegexTypo.Checked,

            BotMode = BotMode,

            UnicodifyWholeArticle = chkUnicodifyWhole.Checked,

            AutoTaggerEnabled = chkAutoTagger.Checked,
            RestrictOrphanTagging =
                restrictOrphanTaggingToolStripMenuItem.Checked,

            PreParseMode = preParseModeToolStripMenuItem.Checked,

            DisambiguationEnabled = chkEnableDab.Checked,
            DisambiguationLink = txtDabLink.Text.Trim(),
            DisambiguationVariants = txtDabVariants.Lines,
            DisambiguationContextCharacters =
                (int)udContextChars.Value,
            SkipIfNoDisambiguation = chkSkipNoDab.Checked
        };
    }

    /// <summary>
    /// Runs the configured custom module, external program, and plugins for the
    /// supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to pass through the configured extension pipeline.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when extension processing completes without
    /// skipping the article; otherwise, <see langword="false"/>.
    /// </returns>
    private bool RunExtensionProcessing(Article article)
    {
        if (_customModule.ModuleUsable)
        {
            article.SendPageToCustomModule(_customModule.Module);

            if (article.SkipArticle)
            {
                return false;
            }
        }

        Variables.Profiler.Profile("Custom module");

        if (_externalProgram.ModuleEnabled)
        {
            article.SendPageToCustomModule(_externalProgram);

            if (article.SkipArticle)
            {
                return false;
            }
        }

        Variables.Profiler.Profile("External Program");

        if (Twain.Core.Plugin.PluginManager.AWBPlugins.Any())
        {
            foreach (KeyValuePair<string, IAWBPlugin> plugin in Twain.Core.Plugin.PluginManager.AWBPlugins)
            {
                article.SendPageToPlugin(plugin.Value, this);

                if (article.SkipArticle)
                {
                    return false;
                }
            }
        }

        Variables.Profiler.Profile("Plugins");

        return true;
    }

    /// <summary>
    /// Loads the supporting data required by the general-fixes pipeline.
    /// </summary>
    private void EnsureGeneralFixResourcesLoaded()
    {
        if (!_templateRedirectsLoaded)
        {
            LoadTemplateRedirects();
            Variables.Profiler.Profile("LoadTemplateRedirects");
        }

        if (!_datedTemplatesLoaded)
        {
            LoadDatedTemplates();
            Variables.Profiler.Profile("LoadDatedTemplates");
        }

        if (!_renamedTemplateParametersLoaded)
        {
            LoadRenameTemplateParameters();
            Variables.Profiler.Profile("LoadRenameTemplateParameters");
        }
    }

    /// <summary>
    /// Applies the append/prepend settings from the current interface to the
    /// supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to update.
    /// </param>
    private void ApplyAppendOrPrependText(Article article)
    {
        if (!chkAppend.Checked)
        {
            return;
        }

        article.ApplyAppendOrPrependText(
            txtAppendMessage.Text,
            (int)udNewlineChars.Value,
            rdoAppend.Checked,
            chkAppendMetaDataSort.Checked,
            _parser);
    }

    /// <summary>
    /// Applies the configured image or file replacement operation to the supplied
    /// article.
    /// </summary>
    /// <param name="article">
    /// The article to update.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when the operation skips the article.
    /// </returns>
    private bool ApplyImageChanges(Article article)
    {
        if (cmboImages.SelectedIndex == 0)
        {
            return true;
        }

        article.UpdateImages(
            (Twain.Core.Options.ImageReplaceOptions)cmboImages.SelectedIndex,
            txtImageReplace.Text,
            txtImageWith.Text,
            chkSkipNoImgChange.Checked);

        return !article.SkipArticle;
    }

    /// <summary>
    /// Applies the current categorization settings to the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to update.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when categorization skips the article.
    /// </returns>
    private bool ApplyCategorisationChanges(Article article)
    {
        return article.ApplyCategorisationChanges(
            (Twain.Core.Options.CategorisationOptions)
                cmboCategorise.SelectedIndex,
            _parser,
            chkSkipNoCatChange.Checked,
            txtNewCategory.Text.Trim(),
            txtNewCategory2.Text.Trim(),
            chkRemoveSortKey.Checked,
            chkGeneralFixes.Checked);
    }

    /// <summary>
    /// Applies whole-article Unicode conversion when standard processing and the
    /// corresponding user option are enabled.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="applyStandardProcessing">
    /// <see langword="true"/> when the article is eligible for standard parsing
    /// operations; otherwise, <see langword="false"/>.
    /// </param>
    private void ApplyWholeArticleUnicodify(
        Article article,
        bool applyStandardProcessing)
    {
        if (!applyStandardProcessing ||
            !chkUnicodifyWhole.Checked)
        {
            return;
        }

        article.Unicodify(
            _skip.SkipNoUnicode,
            _parser,
            _removeText);

        Variables.Profiler.Profile("Unicodify");
    }

    /// <summary>
    /// Applies automatic maintenance tagging to the supplied article when the
    /// feature is enabled.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <param name="mainProcess">
    /// <see langword="true"/> when processing as part of the normal save workflow;
    /// otherwise, <see langword="false"/> for operations such as reparsing, where
    /// automatic tagging should not terminate processing.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when automatic tagging skips the article during the
    /// main processing workflow.
    /// </returns>
    private bool ApplyAutoTagging(
    Article article,
    bool mainProcess)
    {
        if (!chkAutoTagger.Checked)
        {
            return true;
        }

        article.AutoTag(
            _parser,
            _skip.SkipNoTag,
            restrictOrphanTaggingToolStripMenuItem.Checked);

        return !(mainProcess && article.SkipArticle);
    }

    /// <summary>
    /// Applies the configured disambiguation processing to the supplied article.
    /// </summary>
    /// <param name="article">
    /// The article to process.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when processing may continue; otherwise,
    /// <see langword="false"/> when processing has been stopped because the
    /// article was skipped or disambiguation failed.
    /// </returns>
    private bool ApplyDisambiguation(Article article)
    {
        if (preParseModeToolStripMenuItem.Checked ||
            !chkEnableDab.Checked ||
            txtDabLink.Text.Trim().Length == 0 ||
            txtDabVariants.Text.Trim().Length == 0)
        {
            return true;
        }

        if (article.Disambiguate(
                TheSession,
                txtDabLink.Text.Trim(),
                txtDabVariants.Lines,
                BotMode,
                (int)udContextChars.Value,
                chkSkipNoDab.Checked))
        {
            return !article.SkipArticle;
        }

        _abort = true;
        Stop();

        return false;
    }

    /// <summary>
    /// Starts generation and display of the current article diff.
    /// </summary>
    private void GetDiff()
    {
        _ = GetDiffAsync();
    }

    /// <summary>
    /// Generates and displays the diff between the article's original text and
    /// the current editor contents.
    /// </summary>
    /// <remarks>
    /// When no changes are present, a message is displayed instead of a diff.
    /// After rendering, the editor and surrounding UI are restored to their
    /// normal post-processing state.
    /// </remarks>
    private async Task GetDiffAsync()
    {
        if (TheArticle == null)
        {
            DisableButtons();
            return;
        }

        try
        {
            string diffHtml =
                BuildDiffHtml(TheArticle);

            await DisplayDiffHtmlAsync(diffHtml);
            CompleteDiffDisplay();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Builds the HTML document used to display the article diff.
    /// </summary>
    /// <param name="article">
    /// The article whose original text is compared with the current editor text.
    /// </param>
    /// <returns>
    /// A complete HTML document containing either the generated diff or a
    /// no-changes message.
    /// </returns>
    private string BuildDiffHtml(Article article)
    {
        if (string.Equals(article.OriginalArticleText, txtEdit.Text, StringComparison.Ordinal))
        {
            return BuildNoChangesDiffHtml();
        }

        return BuildArticleDiffHtml(article);
    }

    /// <summary>
    /// Builds the HTML displayed when the article text has not changed.
    /// </summary>
    private static string BuildNoChangesDiffHtml()
    {
        return
            @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<title>AWB Diff</title>
</head>
<body>
<h2 style='padding-top: .5em;
padding-bottom: .17em;
border-bottom: 1px solid #aaa;
font-size: 150%;'>No changes</h2>
<p>Press the ""Skip"" button below to skip to the next page.</p>
</body>
</html>";
    }

    /// <summary>
    /// Builds the HTML document containing the generated article diff.
    /// </summary>
    /// <param name="article">
    /// The article whose original text is compared with the editor text.
    /// </param>
    private string BuildArticleDiffHtml(Article article)
    {
        string tableHeader =
            _sessionCounters.NumberOfEdits < 10
                ? WikiDiff.TableHeader
                : WikiDiff.TableHeaderNoMessages;

        return
            "<!DOCTYPE html>" +
            "<html>" +
            "<head>" +
            "<meta charset=\"utf-8\">" +
            WikiDiff.DiffHead() +
            "</head>" +
            "<body>" +
            tableHeader +
            _diff.GetDiff(
                article.OriginalArticleText,
                txtEdit.Text,
                2) +
            "</table>" +
            "</body>" +
            "</html>";
    }

    /// <summary>
    /// Displays the generated diff using the available platform-specific viewer.
    /// </summary>
    /// <param name="diffHtml">
    /// The complete HTML diff document to display.
    /// </param>
    private void DisplayDiffHtml(string diffHtml)
    {
        // WebView2 is unavailable under Mono, so write the diff to a file.
        // TODO (Platform Modernization):
        // Re-evaluate the Mono diff fallback. Determine whether writing the diff to a
        // temporary HTML file is still required, or whether Mono support should be
        // retired in favor of a supported cross-platform rendering solution.
        if (Globals.UsingMono)
        {
            Tools.WriteTextFile(
                diffHtml,
                "Diff.html",
                false);

            return;
        }

        RenderWebView2Diff(diffHtml);
    }

    /// <summary>
    /// Displays the generated diff using the available platform-specific viewer
    /// and waits for WebView2 navigation to complete when applicable.
    /// </summary>
    /// <param name="diffHtml">
    /// The complete HTML diff document to display.
    /// </param>
    private async Task DisplayDiffHtmlAsync(string diffHtml)
    {
        // WebView2 is unavailable under Mono, so write the diff to a file.
        if (Globals.UsingMono)
        {
            Tools.WriteTextFile(
                diffHtml,
                "Diff.html",
                false);

            return;
        }

        await RenderWebView2DiffAsync(diffHtml);
    }

    /// <summary>
    /// Restores the editor and surrounding UI after the diff has been displayed.
    /// </summary>
    private void CompleteDiffDisplay()
    {
        txtEdit.Focus();
        txtEdit.SelectionLength = 0;

        GuiUpdateAfterProcessing();
    }

    private string _webBrowserMouseOverUrl = string.Empty;
    /// <summary>
    /// WebBrowser Document mouse move event: if mouse is over a link, store the URL
    /// Enables use of system browser for right-click Open in New Window option
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">E.</param>
    private void Document_MouseMove(
        object sender,
        HtmlElementEventArgs e)
    {
        _webBrowserMouseOverUrl = string.Empty;

        if (!(sender is HtmlDocument document))
        {
            return;
        }

        if (document.GetElementFromPoint(e.ClientMousePosition)
                is HtmlElement currentElement &&
            string.Equals(
                currentElement.TagName,
                "A",
                StringComparison.OrdinalIgnoreCase))
        {
            _webBrowserMouseOverUrl =
                currentElement.GetAttribute("href");
        }
    }

    /// <summary>
    /// Validates a URL before it is opened outside the embedded browser.
    /// </summary>
    /// <param name="url">The candidate URL.</param>
    /// <param name="allowedUrl">
    /// Contains the normalized URL when validation succeeds.
    /// </param>
    /// <returns>
    /// <c>true</c> when the URL is an absolute HTTP or HTTPS address;
    /// otherwise <c>false</c>.
    /// </returns>
    private static bool TryGetAllowedExternalUrl(
        string url,
        out string allowedUrl)
    {
        allowedUrl = null;

        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out Uri uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        allowedUrl = uri.AbsoluteUri;
        return true;
    }

    /// <summary>
    /// Cancels popup navigation in the embedded preview browser and opens
    /// permitted web links in the user's default browser.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments.</param>
    private void webBrowser_NewWindow(
        object sender,
        CancelEventArgs e)
    {
        e.Cancel = true;

        if (!TryGetAllowedExternalUrl(
                _webBrowserMouseOverUrl,
                out string externalUrl))
        {
            Tools.WriteDebug(
                nameof(webBrowser_NewWindow),
                "The preview attempted to open an invalid or unsupported URL.");

            return;
        }

        Tools.WriteDebug(
            nameof(webBrowser_NewWindow),
            externalUrl);

        Tools.OpenURLInBrowser(externalUrl);
    }

    /// <summary>
    /// Handles completion of an asynchronous preview request and displays the
    /// returned HTML in the preview browser.
    /// </summary>
    /// <param name="sender">
    /// The editor that completed the preview request.
    /// </param>
    /// <param name="result">
    /// The rendered preview HTML returned by the wiki API.
    /// </param>
    /// <remarks>
    /// The method records the current editor text, marks the page as no longer
    /// skippable, refreshes the browser document when available, restores the
    /// browser mouse-move handler, clears the status message, and updates the
    /// surrounding user interface.
    /// </remarks>
    private void PreviewComplete(
        AsyncApiEdit sender,
        string previewHtml)
    {
        _lastArticle = txtEdit.Text;
        _skippable = false;

        ShowPreviewBrowser();

        HtmlDocument document = webBrowser.Document;

        if (document == null)
        {
            webBrowser.DocumentText = BuildPreviewHtml(sender, previewHtml);
        }
        else
        {
            document.OpenNew(false);
            document.Write(BuildPreviewHtml(sender, previewHtml));

            document.MouseMove -= Document_MouseMove;
            document.MouseMove += Document_MouseMove;
        }

        StatusLabelText = string.Empty;
        GuiUpdateAfterProcessing();
    }

    /// <summary>
    /// Handles completion of an asynchronous page-open request and begins
    /// processing the loaded page.
    /// </summary>
    /// <param name="editor">
    /// The editor that completed the page-open request.
    /// </param>
    /// <param name="page">
    /// Information and content for the page returned by the open operation.
    /// </param>
    /// <remarks>
    /// Diagnostic messages are written before and after page processing so the
    /// editor's active state can be observed during the completion workflow.
    /// </remarks>
    private void OpenComplete(
        AsyncApiEdit editor,
        PageInfo page)
    {
        Tools.WriteDebug(
            nameof(OpenComplete),
            $"Before PageLoaded: IsActive={editor.IsActive}");

        PageLoaded(page);

        Tools.WriteDebug(
            nameof(OpenComplete),
            $"After PageLoaded: IsActive={editor.IsActive}");
    }

    /// <summary>
    /// Starts generation of a preview for the current article using the current
    /// editor contents.
    /// </summary>
    /// <remarks>
    /// Preview generation is not started when no article is loaded or when the
    /// session editor is already processing another request. Diagnostic messages
    /// record the preview state and reason when the operation cannot be started.
    /// </remarks>
    private void GetPreview()
    {
        Article article = TheArticle;

        Tools.WriteDebug(
            nameof(GetPreview),
            $"Entered. ArticleNull={article == null}, " +
            $"EditorActive={TheSession.Editor.IsActive}");

        if (article == null)
        {
            DisableButtons();
            return;
        }

        if (TheSession.Editor.IsActive)
        {
            StatusLabelText = "Editor busy";

            Tools.WriteDebug(
                nameof(GetPreview),
                "Preview was not started because the editor was active.");

            return;
        }

        StatusLabelText = "Previewing...";

        Tools.WriteDebug(
            nameof(GetPreview),
            $"Starting preview for '{article.Name}'.");

        TheSession.Editor.Preview(
            article.Name,
            txtEdit.Text);
    }

    /// <summary>
    /// Builds the HTML document displayed by the preview browser.
    /// </summary>
    /// <param name="sender">
    /// The editor providing the HTML header content required by the preview.
    /// </param>
    /// <param name="result">
    /// The rendered article HTML returned by the wiki API.
    /// </param>
    /// <returns>
    /// A complete HTML document containing the rendered preview.
    /// </returns>
    private static string BuildPreviewHtml(
        AsyncApiEdit sender,
        string result)
    {
        return
            "<html><head>" +
            sender.HtmlHeaders +
            "</head><body style=\"background:white; margin:10px; text-align:left;\">" +
            result +
            "</body></html>";
    }

    /// <summary>
    /// Restores the main UI after page processing completes.
    /// </summary>
    private void GuiUpdateAfterProcessing()
    {
        if (_stopProcessing)
            Stop();
        else
        {
            BleepFlash();
            Focus();
            EnableButtons();
            btnSave.Select();
        }
    }

    // TODO (.NET10 Modernization):
    // Verify that SkipPage() always restores buttons, progress indicators, and
    // status text when the user declines to save a blank page. The save workflow
    // enters its busy UI state before displaying the confirmation dialog.
    /// <summary>
    /// Validates the current page and begins the save operation.
    /// </summary>
    private void Save()
    {
        ValidateArticleForSave();

        if (!PrepareEditorForSave())
        {
            return;
        }

        ValidateSaveContentForDebugBuild();

        DisableButtons();
        StartProgressBar();

        if (CanSaveCurrentText())
        {
            SaveArticle();
            return;
        }

        SkipPage("Nothing to save - blank page");
    }

    /// <summary>
    /// Verifies that the loaded article matches the page held by the current
    /// session.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no article is loaded or the article does not match the current
    /// session page.
    /// </exception>
    private void ValidateArticleForSave()
    {
        if (TheArticle != null &&
            TheArticle.Name == TheSession.Page.Title)
        {
            return;
        }

        DisableButtons();

        string details = TheArticle == null
            ? "the article was null"
            : $"Article name: '{TheArticle.Name}', " +
              $"session page title: '{TheSession.Page.Title}'";

        throw new InvalidOperationException(
            $"Attempted to save a wrong page ({details})");
    }

    /// <summary>
    /// Verifies that the editor is available and updates the save status.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when saving may continue; otherwise,
    /// <see langword="false"/> when the editor is busy.
    /// </returns>
    private bool PrepareEditorForSave()
    {
        if (TheSession.Editor.IsActive)
        {
            StatusLabelText = "Editor busy";
            return false;
        }

        StatusLabelText = "Saving...";
        return true;
    }

    // TODO (.NET10 Modernization):
    // Define and enforce the nullability contract for TheSession.Page. Save
    // validation currently assumes that a session page always exists and may
    // otherwise fail before producing the intended diagnostic information.
    /// <summary>
    /// Determines whether the current editor contents may be saved.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the editor contains text or the user confirms
    /// saving a blank existing page; otherwise, <see langword="false"/>.
    /// </returns>
    private bool CanSaveCurrentText()
    {
        if (!string.IsNullOrEmpty(txtEdit.Text))
        {
            return true;
        }

        if (TheArticle.Exists != Exists.Yes)
        {
            return false;
        }

        return MessageBox.Show(
            this,
            "Do you really want to save a blank page?",
            "Save?",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }

    /// <summary>
    /// Performs additional save validation in debug builds.
    /// </summary>
    [Conditional("DEBUG")]
    private void ValidateSaveContentForDebugBuild()
    {
        const string messagePrefix =
            "Extra validation for debug builds " +
            "(don't use a debug build if you want to save blank pages): ";

        if (string.IsNullOrEmpty(TheArticle.ArticleText))
        {
            throw new InvalidOperationException(
                messagePrefix +
                "Attempted to save page with zero length ArticleText");
        }

        if (string.IsNullOrEmpty(txtEdit.Text))
        {
            throw new InvalidOperationException(
                messagePrefix +
                "Attempted to save page with zero length txtEditText");
        }
    }

    /// <summary>
    /// Saves the current article text using the selected edit and watch-list
    /// options.
    /// </summary>
    /// <remarks>
    /// The method preserves the current editor text, updates save timing,
    /// corrects section edit summaries when necessary, tracks newly created
    /// pages, and starts the asynchronous save operation. If the editor is
    /// already busy, the save is not started and the surrounding controls are
    /// re-enabled.
    /// </remarks>
    private void SaveArticle()
    {
        _lastArticle = txtEdit.Text;

        UpdateSaveIntervalTracking();

        if (TheSession.Editor.IsActive)
        {
            StatusLabelText = "Editor busy";
            EnableButtons();
            return;
        }

        if (!TheSession.Page.Exists)
        {
            _sessionCounters.NumberOfNewPages++;
        }

        CorrectSectionEditSummary();

        WatchOptions watchOption =
            GetSelectedWatchOption();

        TheSession.Editor.Save(
            txtEdit.Text,
            AppendUsingAWBSummary(txtReviewEditSummary.Text),
            markAllAsMinorToolStripMenuItem.Checked,
            watchOption);
    }

    /// <summary>
    /// Updates the moving-average save interval when save timing is enabled.
    /// </summary>
    private void UpdateSaveIntervalTracking()
    {
        if (!ShowMovingAverageTimer)
        {
            return;
        }

        StopSaveInterval();
        Ticker += SaveInterval;
    }

    /// <summary>
    /// Gets the watch-list option selected in the save controls.
    /// </summary>
    /// <returns>
    /// The corresponding <see cref="WatchOptions"/> value.
    /// </returns>
    private WatchOptions GetSelectedWatchOption()
    {
        return addToWatchList.SelectedIndex switch
        {
            0 => WatchOptions.Watch,
            1 => WatchOptions.Unwatch,
            3 => WatchOptions.UsePreferences,
            _ => WatchOptions.NoChange
        };
    }

    // TODO (Defensive Validation):
    // Handle malformed section edit summaries that begin with "/*" but do not
    // contain a closing "*/" marker before attempting to remove the prefix.
    /// <summary>
    /// Removes an invalid section prefix from the edit summary when the edited
    /// section no longer matches the section named in the summary.
    /// </summary>
    private void CorrectSectionEditSummary()
    {
        if (!txtReviewEditSummary.Text.StartsWith(
                "/*",
                StringComparison.Ordinal))
        {
            return;
        }

        string sectionEditText =
            Summary.ModifiedSection(
                TheArticle.OriginalArticleText,
                txtEdit.Text);

        string expectedSectionSummary =
            "/* " + sectionEditText + " */";

        if (sectionEditText.Length > 0 &&
            txtReviewEditSummary.Text.Contains(
                expectedSectionSummary,
                StringComparison.Ordinal))
        {
            return;
        }

        int sectionMarkerEnd =
            txtReviewEditSummary.Text.IndexOf(
                "*/",
                StringComparison.Ordinal);

        txtReviewEditSummary.Text =
            txtReviewEditSummary.Text.Substring(
                sectionMarkerEnd + 2);
    }

    #endregion

    #region extra stuff

    #region Diff

    private enum DiffChangeMode { Deletion, Change, Addition }

    /// <summary>
    /// This class serves as a proxy between the main window and WebBrowser, isolating the former
    /// from malicious site JS calls of window.external.
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class JsAdapter
    {
        private readonly MainForm _owner;

        internal JsAdapter(MainForm owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Reverses the changes to a line of text in the page
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void UndoChange(int left, int right)
        {
            _owner.UndoChangeGeneric(DiffChangeMode.Change, left, right);
        }

        /// <summary>
        /// Reverses the deletion of a line of text from the page
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void UndoDeletion(int left, int right)
        {
            _owner.UndoChangeGeneric(DiffChangeMode.Deletion, left, right);
        }

        /// <summary>
        /// Reverses an added line in the displayed diff.
        /// </summary>
        /// <param name="right">
        /// The right-side diff line index to restore.
        /// </param>
        public void UndoAddition(int right)
        {
            _owner.UndoChangeGeneric(DiffChangeMode.Addition, 0, right);
        }

        /// <summary>
        /// Moves the caret to the input line within the article text box
        /// </summary>
        /// <param name="destLine">the line number the caret should be moved to</param>
        public void GoTo(int destLine)
        {
            _owner.GoTo(destLine);
        }
    }

    /// <summary>
    /// Represents a command sent from the generated diff document to the host
    /// application through WebView2 messaging.
    /// </summary>
    private sealed class DiffWebMessage
    {
        /// <summary>
        /// Gets or sets the requested diff action.
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the corresponding line in the original text, when used.
        /// </summary>
        [JsonPropertyName("leftLine")]
        public int? LeftLine { get; set; }

        /// <summary>
        /// Gets or sets the corresponding line in the modified text, when used.
        /// </summary>
        [JsonPropertyName("rightLine")]
        public int? RightLine { get; set; }
    }

    /// <summary>
    /// Reverses a selected change, addition, or deletion in the current article
    /// text and refreshes the displayed diff.
    /// </summary>
    /// <param name="changeType">
    /// The type of diff operation to reverse.
    /// </param>
    /// <param name="left">
    /// The position of the affected content in the original text.
    /// </param>
    /// <param name="right">
    /// The position of the affected content in the modified text.
    /// </param>
    /// <remarks>
    /// The method preserves the current WebView2 diff scroll position and editor
    /// caret position while rebuilding the diff. Any exception raised while
    /// processing the undo operation is passed to the application's central error
    /// handler.
    /// </remarks>
    private async Task UndoChangeGenericAsync(
        DiffChangeMode changeType,
        int left,
        int right)
    {
        if (!txtEdit.Enabled)
        {
            return;
        }

        try
        {
            int browserScrollPosition =
                await GetDiffScrollPositionAsync();

            int caretPosition = txtEdit.SelectionStart;

            // Rebuild the internal diff state from the current editor contents.
            _ = BuildDiffHtml(TheArticle);

            ApplyDiffUndo(
                changeType,
                left,
                right);

            // Generate and display the updated diff after modifying the editor contents.
            await GetDiffAsync();

            if (syntaxHighlightEditBoxToolStripMenuItem.Checked)
            {
                HighlightSyntax();
            }

            await RestoreDiffScrollPositionAsync(
                browserScrollPosition);

            RestoreEditorCaretPosition(
                caretPosition);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Starts the asynchronous diff undo workflow for legacy synchronous callers.
    /// </summary>
    private void UndoChangeGeneric(
        DiffChangeMode changeType,
        int left,
        int right)
    {
        _ = UndoChangeGenericAsync(
            changeType,
            left,
            right);
    }

    /// <summary>
    /// Gets the current vertical scroll position of the WebView2 diff document.
    /// </summary>
    /// <returns>
    /// The vertical scroll position in pixels, or zero when the diff viewer is
    /// unavailable or the script result cannot be parsed.
    /// </returns>
    private async Task<int> GetDiffScrollPositionAsync()
    {
        if (_diffWebView == null ||
            _diffWebView.IsDisposed ||
            _diffWebView.CoreWebView2 == null)
        {
            return 0;
        }

        string scriptResult =
            await _diffWebView.CoreWebView2.ExecuteScriptAsync(
                "Math.round(window.scrollY)");

        return int.TryParse(
            scriptResult,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int scrollPosition)
            ? scrollPosition
            : 0;
    }

    /// <summary>
    /// Applies the requested undo operation to the current editor contents.
    /// </summary>
    /// <param name="changeType">
    /// The type of diff operation to reverse.
    /// </param>
    /// <param name="left">
    /// The position of the affected content in the original text.
    /// </param>
    /// <param name="right">
    /// The position of the affected content in the modified text.
    /// </param>
    /// <remarks>
    /// The undo methods operate against the state produced by the most recently
    /// generated diff, so callers must rebuild the diff before invoking this
    /// method.
    /// </remarks>
    private void ApplyDiffUndo(
        DiffChangeMode changeType,
        int left,
        int right)
    {
        switch (changeType)
        {
            case DiffChangeMode.Change:
                txtEdit.Text =
                    _diff.UndoChange(
                        left,
                        right);

                break;

            case DiffChangeMode.Deletion:
                txtEdit.Text =
                    _diff.UndoDeletion(
                        left,
                        right);

                break;

            case DiffChangeMode.Addition:
                txtEdit.Text =
                    _diff.UndoAddition(right);

                break;
        }
    }

    /// <summary>
    /// Restores the vertical scroll position of the WebView2 diff document.
    /// </summary>
    /// <param name="scrollPosition">
    /// The vertical scroll position, in pixels, to restore.
    /// </param>
    private async Task RestoreDiffScrollPositionAsync(
        int scrollPosition)
    {
        if (_diffWebView == null ||
            _diffWebView.IsDisposed ||
            _diffWebView.CoreWebView2 == null)
        {
            return;
        }

        await _diffWebView.CoreWebView2.ExecuteScriptAsync(
            FormattableString.Invariant(
                $"window.scrollTo(0, {scrollPosition});"));
    }

    /// <summary>
    /// Restores the editor caret to its previous position, constrained to the
    /// current text length, and scrolls the editor to make it visible.
    /// </summary>
    /// <param name="caretPosition">
    /// The requested caret position.
    /// </param>
    private void RestoreEditorCaretPosition(
        int caretPosition)
    {
        int restoredPosition =
            Math.Min(
                caretPosition,
                txtEdit.Text.Length);

        txtEdit.Select(
            restoredPosition,
            0);

        txtEdit.ScrollToCaret();
    }

    // TODO (Editor Modernization):
    // Verify why the target line number is subtracted from the CRLF match
    // position. Preserve the calculation until line-navigation behavior has
    // been tested with empty lines, the final line, and multi-line content.
    /// <summary>
    /// Moves the caret to the specified line in the article editor.
    /// </summary>
    /// <param name="destLine">
    /// The zero-based line number to which the caret should be moved.
    /// </param>
    /// <remarks>
    /// The editor is not focused when text is selected in the diff or preview
    /// browser, allowing the selected browser text to remain available for
    /// keyboard copy operations.
    /// </remarks>
    private void GoTo(int destLine)
    {
        // Preserve browser selection so it can be copied with keyboard shortcuts.
        if (webBrowser.TextSelected())
        {
            return;
        }

        try
        {
            EditBoxTab.SelectedTab = tpEdit;
            txtEdit.Select();

            if (destLine < 0)
            {
                return;
            }

            MatchCollection lineBreaks =
                Regex.Matches(
                    txtEdit.Text,
                    "\r\n");

            int targetLine =
                Math.Min(
                    destLine,
                    lineBreaks.Count);

            int caretPosition =
                GetCaretPositionForLine(
                    targetLine,
                    lineBreaks);

            txtEdit.Select(
                caretPosition,
                0);

            txtEdit.ScrollToCaret();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Calculates the editor character position corresponding to a line number.
    /// </summary>
    /// <param name="targetLine">
    /// The zero-based line number, constrained to the available line count.
    /// </param>
    /// <param name="lineBreaks">
    /// The CRLF matches found in the editor text.
    /// </param>
    /// <returns>
    /// The character position at which the caret should be placed.
    /// </returns>
    private static int GetCaretPositionForLine(
        int targetLine,
        MatchCollection lineBreaks)
    {
        const int CarriageReturnLineFeedLength = 2;

        if (targetLine == 0)
        {
            return 0;
        }

        return lineBreaks[targetLine - 1].Index +
               CarriageReturnLineFeedLength -
               targetLine;
    }

    #endregion

    // TODO (UI Modernization):
    // Rename legacy designer controls such as panel1 and label8 to names that
    // describe their purpose after the current migration work is complete.
    /// <summary>
    /// Toggles the visibility of the auxiliary panel and updates the corresponding
    /// menu-item state.
    /// </summary>
    /// <remarks>
    /// The browser layout is recalculated after the panel visibility changes.
    /// </remarks>
    private void PanelShowHide()
    {
        panel1.Visible = !panel1.Visible;
        showHidePanelToolStripMenuItem.Checked = panel1.Visible;

        SetBrowserSize();
    }

    /// <summary>
    /// Stores the article editor's location before the parameter panels are
    /// hidden and the editor is enlarged.
    /// </summary>
    private Point _oldPosition;

    /// <summary>
    /// Stores the article editor's size before the parameter panels are hidden
    /// and the editor is enlarged.
    /// </summary>
    private Size _oldSize;

    /// <summary>
    /// Toggles between the normal parameter layout and an enlarged article-editing
    /// area.
    /// </summary>
    /// <remarks>
    /// When the parameter controls are visible, their space is reassigned to the
    /// article editor and the editor's original bounds are saved. When the
    /// controls are restored, the saved editor bounds are reapplied.
    /// </remarks>
    private void ParametersShowHide()
    {
        bool parametersVisible = listMaker.Visible;

        enlargeEditAreaToolStripMenuItem.Checked =
            !enlargeEditAreaToolStripMenuItem.Checked;

        if (parametersVisible)
        {
            EnlargeEditorArea();
        }
        else
        {
            RestoreEditorArea();
        }

        bool showParameters = !parametersVisible;

        listMaker.Visible = showParameters;
        MainTab.Visible = showParameters;
        label8.Visible = showParameters;
    }

    /// <summary>
    /// Saves the current editor bounds and enlarges the editor into the space
    /// occupied by the parameter controls.
    /// </summary>
    private void EnlargeEditorArea()
    {
        btntsShowHideParameters.Image =
            Resources.Showhideparameters2;

        _oldPosition = EditBoxTab.Location;
        _oldSize = EditBoxTab.Size;

        EditBoxTab.Location = new Point(
            listMaker.Location.X,
            listMaker.Location.Y - 17);

        EditBoxTab.Size = new Size(
            EditBoxTab.Width +
            MainTab.Width +
            listMaker.Width +
            8,
            EditBoxTab.Height);
    }

    /// <summary>
    /// Restores the editor bounds that were saved before the parameter controls
    /// were hidden.
    /// </summary>
    private void RestoreEditorArea()
    {
        btntsShowHideParameters.Image =
            Resources.Showhideparameters;

        EditBoxTab.Location = _oldPosition;
        EditBoxTab.Size = _oldSize;
    }

    /// <summary>
    /// Refreshes the user identity and permission indicators displayed in the
    /// status area.
    /// </summary>
    private void UpdateStatusUI()
    {
        UpdateUserName();
        UpdateBotStatus();
        UpdateAdminStatus();
    }

    // TODO (UI Modernization):
    // Cache the bold and regular notification fonts instead of creating new
    // Font instances each time the notification count is updated. This will
    // reduce GDI object allocations and centralize font lifetime management.
    // TODO (Globalization):
    // Review whether the notification count should be formatted using the
    // current UI culture or InvariantCulture. The current implementation uses
    // the default integer formatting, which is appropriate for most UI scenarios.
    /// <summary>
    /// Updates the user notification indicator shown in the status area.
    /// </summary>
    /// <remarks>
    /// The notification indicator is hidden when notifications are disabled.
    /// Otherwise, the current notification count is displayed and the visual
    /// appearance is updated to indicate whether any unread notifications exist.
    /// </remarks>
    private void UpdateUserNotifications()
    {
        lblUserNotifications.Visible = Variables.NotificationsEnabled;

        if (!Variables.NotificationsEnabled)
        {
            return;
        }

        int notificationCount = TheSession.User.Notifications;

        lblUserNotifications.Text = notificationCount.ToString();

        UpdateNotificationAppearance(notificationCount);
    }

    /// <summary>
    /// Updates the visual appearance of the notification indicator.
    /// </summary>
    /// <param name="notificationCount">
    /// The current number of unread user notifications.
    /// </param>
    private void UpdateNotificationAppearance(int notificationCount)
    {
        bool hasNotifications = notificationCount > 0;

        lblUserNotifications.BackColor =
            hasNotifications
                ? Color.Tomato
                : Color.Gray;

        lblUserNotifications.Font = new Font(
            lblUserNotifications.Font,
            hasNotifications
                ? FontStyle.Bold
                : FontStyle.Regular);
    }

    // TODO (UI Consistency):
    // Verify whether the user-name foreground color should be explicitly reset
    // when the session is no longer registered. The current behavior preserves
    // the previously assigned foreground color.
    /// <summary>
    /// Updates the displayed user name and the controls that reflect the current
    /// wiki registration status.
    /// </summary>
    /// <remarks>
    /// When no user name is available, the localized user namespace name is
    /// displayed when possible. Registered users are shown with the registered
    /// status styling and may start processing; all other statuses disable the
    /// Start button.
    /// </remarks>
    private void UpdateUserName()
    {
        string userName = TheSession.User.Name;

        if (string.IsNullOrEmpty(userName))
        {
            lblUserName.Text =
                Variables.Namespaces.TryGetValue(
                    Namespace.User,
                    out string userNamespace)
                    ? userNamespace
                    : "User:";
        }
        else
        {
            lblUserName.Text = userName;
        }

        bool isRegistered =
            TheSession.Status == WikiStatusResult.Registered;

        lblUserName.BackColor =
            isRegistered
                ? Color.Green
                : Color.Red;

        if (isRegistered)
        {
            lblUserName.ForeColor = Color.White;
        }

        btnStart.Enabled = isRegistered;
    }

    /// <summary>
    /// Updates the main status display with the current list-maker status.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the status update.
    /// </param>
    private void UpdateListStatus(
        object sender,
        EventArgs e)
    {
        StatusLabelText = listMaker.Status;
    }

    /// <summary>
    /// Refreshes the typo statistics for the current article.
    /// </summary>
    private void UpdateCurrentTypoStats()
    {
        CurrentTypoStats.UpdateStats(
            _typoStats,
            false);
    }

    /// <summary>
    /// Refreshes the accumulated typo statistics and updates the displayed typo
    /// count.
    /// </summary>
    /// <remarks>
    /// Overall regular-expression typo statistics are updated only when the
    /// regular-expression typo option is enabled. The displayed typo count is
    /// refreshed regardless of that option.
    /// </remarks>
    private void UpdateOverallTypoStats()
    {
        if (chkRegExTypo.Checked)
        {
            OverallTypoStats.UpdateStats(
                _typoStats,
                false);
        }

        UpdateTypoCount();
    }

    /// <summary>
    /// Updates the displayed overall typo statistics.
    /// </summary>
    /// <remarks>
    /// When at least one article has been saved, the method displays the total
    /// typo count, self-match count, and average typos per save. Otherwise, the
    /// displayed typo ratio is reset to zero.
    /// </remarks>
    private void UpdateTypoCount()
    {
        if (OverallTypoStats.Saves <= 0)
        {
            lblTypoRatio.Text = "0";
            return;
        }

        // TODO (UI Consistency):
        // Verify whether lblOverallTypos and lblNoChange should also be reset when
        // no saves have been recorded. The current behavior only resets the typo
        // ratio and may leave previous totals visible.
        // Copy the values to locals to avoid CS1690 when accessing members of
        // the statistics value through its containing field.
        int totalTypos = OverallTypoStats.TotalTypos;
        int selfMatches = OverallTypoStats.SelfMatches;

        lblOverallTypos.Text = totalTypos.ToString();
        lblNoChange.Text = selfMatches.ToString();
        lblTypoRatio.Text = OverallTypoStats.TyposPerSave;
    }

    /// <summary>
    /// Clears the current and overall typo statistics and refreshes the displayed
    /// totals.
    /// </summary>
    private void ResetTypoStats()
    {
        CurrentTypoStats.ClearStats();
        OverallTypoStats.ClearStats();

        UpdateTypoCount();
    }

    // TODO (Shutdown Modernization):
    // Review whether persistent settings should be saved when the user cancels
    // the closing request, and whether CloseDownAWB() should run for shutdown
    // reasons that cannot be cancelled, such as Windows session termination.
    /// <summary>
    /// Handles the form-closing request, saves persistent settings, and optionally
    /// asks the user to confirm that AWB should terminate.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The form-closing event data, which may be updated to cancel closing.
    /// </param>
    /// <remarks>
    /// Window settings are saved even when the user cancels the closing request,
    /// preserving the existing behavior.
    /// </remarks>
    private void MainForm_FormClosing(
        object sender,
        FormClosingEventArgs e)
    {
        SaveWindowSettings();

        if (!Properties.Settings.Default.AskForTerminate)
        {
            Properties.Settings.Default.Save();
            CloseDownAWB();
            return;
        }

        TimeSpan elapsedTime = _sessionTimer.Elapsed;

        using ExitQuestion dialog = new ExitQuestion(
            elapsedTime,
            _sessionCounters.NumberOfEdits,
            string.Empty);

        DialogResult result = dialog.ShowDialog(this);

        Properties.Settings.Default.AskForTerminate =
            !dialog.CheckBoxDontAskAgain;

        // Preserve user settings even when closing is cancelled.
        Properties.Settings.Default.Save();

        switch (result)
        {
            case DialogResult.OK:
                CloseDownAWB();
                break;

            case DialogResult.Cancel:
                e.Cancel = true;
                break;
        }
    }

    /// <summary>
    /// Stores the current form state, size, and location in the user settings.
    /// </summary>
    /// <remarks>
    /// When the form is minimized or maximized, its restored bounds are stored so
    /// that the next session opens using the normal window size and position.
    /// </remarks>
    private void SaveWindowSettings()
    {
        Properties.Settings.Default.WindowState =
            WindowState;

        if (WindowState == FormWindowState.Normal)
        {
            Properties.Settings.Default.WindowSize =
                Size;

            Properties.Settings.Default.WindowLocation =
                Location;

            return;
        }

        Properties.Settings.Default.WindowSize =
            RestoreBounds.Size;

        Properties.Settings.Default.WindowLocation =
            RestoreBounds.Location;
    }

    // TODO (Shutdown Reliability):
    // Review whether shutdown operations should be isolated so that a failure in
    // editor cancellation or settings persistence does not prevent the remaining
    // cleanup steps from completing.
    /// <summary>
    /// Performs the one-time application shutdown cleanup for AWB.
    /// </summary>
    /// <remarks>
    /// The method prevents duplicate shutdown processing, aborts any active editor
    /// operation when a session is available, saves recent settings, and disposes
    /// the notification-area icon.
    /// </remarks>
    private void CloseDownAWB()
    {
        if (_shuttingDown)
        {
            return;
        }

        _shuttingDown = true;

        AbortActiveEditorOperation();
        SaveRecentSettingsList();
        DisposeTrayIcon();
    }

    // TODO (AsyncApiEdit Modernization):
    // Replace the legacy editor Abort() call with cooperative cancellation when
    // the MainForm shutdown workflow is migrated to AsyncApiEditModern.
    /// <summary>
    /// Aborts the active editor operation when a session was created successfully.
    /// </summary>
    /// <remarks>
    /// The session may be unavailable when startup fails before initialization is
    /// complete, such as after an early network error.
    /// </remarks>
    private void AbortActiveEditorOperation()
    {
        TheSession?.Editor.Abort();
    }

    /// <summary>
    /// Hides and disposes the notification-area icon during application shutdown.
    /// </summary>
    private void DisposeTrayIcon()
    {
        ntfyTray.Visible = false;
        ntfyTray.Dispose();
    }

    /// <summary>
    /// Builds the default edit summary for the current article.
    /// </summary>
    /// <returns>
    /// The completed edit summary, including the configured default summary,
    /// article-specific summary, and optional section prefix.
    /// </returns>
    /// <remarks>
    /// When section edit summaries are enabled and the edit modifies a single
    /// level-2 section, the returned summary is prefixed with the corresponding
    /// section marker.
    /// </remarks>
    private string MakeDefaultEditSummary()
    {
        Article article = TheArticle;

        if (article == null)
        {
            return string.Empty;
        }

        string summary = GetConfiguredEditSummary();

        Tools.WriteDebug(
            nameof(MakeDefaultEditSummary),
            $"Configured summary length: {summary.Length}; " +
            $"value: '{summary}'");

        Tools.WriteDebug(
            nameof(MakeDefaultEditSummary),
            $"Article.EditSummary length: {article.EditSummary?.Length ?? 0}; " +
            $"starts with: '{GetDiagnosticPrefix(article.EditSummary, 200)}'");

        summary = AppendArticleEditSummary(
            summary,
            article.EditSummary);

        Tools.WriteDebug(
            nameof(MakeDefaultEditSummary),
            $"Combined summary length: {summary.Length}; " +
            $"starts with: '{GetDiagnosticPrefix(summary, 300)}'");

        if (!noSectionEditSummaryToolStripMenuItem.Checked)
        {
            summary = AddSectionEditSummary(
                summary,
                article);
        }

        ValidateEditSummary(summary);

        return summary;
    }

    /// <summary>
    /// TEMP helper file
    /// </summary>
    private static string GetDiagnosticPrefix(
    string value,
    int maximumLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maximumLength
            ? value
            : value.Substring(0, maximumLength);
    }

    /// <summary>
    /// Gets the trimmed edit summary entered by the user.
    /// </summary>
    private string GetConfiguredEditSummary()
    {
        return string.IsNullOrWhiteSpace(cmboEditSummary.Text)
            ? string.Empty
            : cmboEditSummary.Text.Trim();
    }

    // TODO (Internationalization):
    // Review whether edit-summary separators should be determined from
    // localization data instead of maintaining a hard-coded language list.
    /// <summary>
    /// Appends the article-specific edit summary using the punctuation
    /// appropriate for the current wiki language.
    /// </summary>
    private static string AppendArticleEditSummary(
        string summary,
        string articleSummary)
    {
        if (string.IsNullOrEmpty(articleSummary))
        {
            return summary;
        }

        string separator =
            Variables.LangCode switch
            {
                "ar" or "arz" or "fa" => "، ",
                _ => ", "
            };

        return summary +
               (string.IsNullOrEmpty(summary)
                   ? string.Empty
                   : separator) +
               articleSummary;
    }

    /// <summary>
    /// Adds a section-edit prefix when the edit modifies a single level-2 section.
    /// </summary>
    private string AddSectionEditSummary(
        string summary,
        Article article)
    {
        string sectionEditText =
            Summary.ModifiedSection(
                article.OriginalArticleText,
                txtEdit.Text);

        if (string.IsNullOrEmpty(sectionEditText))
        {
            return summary;
        }

        return $"/* {sectionEditText} */ {summary.TrimStart()}";
    }

    /// <summary>
    /// Writes a diagnostic message when the generated edit summary is invalid.
    /// </summary>
    private static void ValidateEditSummary(string summary)
    {
        if (!Summary.IsCorrect(summary))
        {
            Tools.WriteDebug(
                "edit summary not correct",
                summary);
        }
    }

    // TODO (Architecture):
    // Move the AWB summary-tag eligibility rules out of MainForm and into a
    // dedicated summary or session policy component when UI logic is separated
    // from edit-summary generation.
    /// <summary>
    /// Appends the localized AWB edit-summary tag when tagging is enabled for the
    /// current user, wiki, and session.
    /// </summary>
    /// <param name="summary">
    /// The edit summary to which the AWB tag may be appended.
    /// </param>
    /// <returns>
    /// The trimmed edit summary with the AWB tag appended when required;
    /// otherwise, the original summary.
    /// </returns>
    /// <remarks>
    /// Bot users may suppress the tag through the corresponding option. The tag
    /// is appended only on supported Wikimedia projects and when session-level
    /// suppression has not been enabled.
    /// </remarks>
    private string AppendUsingAWBSummary(string summary)
    {
        bool suppressForBot =
            TheSession.User.IsBot &&
            chkSuppressTag.Checked;

        bool shouldAppendTag =
            !suppressForBot &&
            Variables.IsWikimediaProject &&
            !_suppressUsingAWB;

        if (!shouldAppendTag)
        {
            return summary;
        }

        return Summary.Trim(summary) +
               Variables.SummaryTag;
    }

    /// <summary>
    /// Updates the availability of find-and-replace options when the main
    /// find-and-replace option changes.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the checked-state change.
    /// </param>
    private void chkFindandReplace_CheckedChanged(
        object sender,
        EventArgs e)
    {
        bool findAndReplaceEnabled =
            chkFindandReplace.Checked;

        btnMoreFindAndReplce.Enabled =
            findAndReplaceEnabled;

        btnFindAndReplaceAdvanced.Enabled =
            findAndReplaceEnabled;

        chkSkipWhenNoFAR.Enabled =
            findAndReplaceEnabled;

        chkSkipOnlyMinorFaR.Enabled =
            findAndReplaceEnabled;

        btnSubst.Enabled =
            findAndReplaceEnabled;
    }

    /// <summary>
    /// Updates the minor-general-fixes option when skipping all general fixes is
    /// enabled or disabled.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the checked-state change.
    /// </param>
    /// <remarks>
    /// Selecting the option to skip all general fixes clears and disables the
    /// option that skips only minor general fixes.
    /// </remarks>
    private void chkSkipGeneralFixes_CheckedChanged(
        object sender,
        EventArgs e)
    {
        bool skipGeneralFixes =
            chkSkipGeneralFixes.Checked;

        chkSkipMinorGeneralFixes.Enabled =
            !skipGeneralFixes;

        if (skipGeneralFixes)
        {
            chkSkipMinorGeneralFixes.Checked = false;
        }
    }

    // TODO (UI Maintainability):
    // Replace categorization SelectedIndex checks with a named enum or typed
    // selection model so behavior does not depend on the order of combo-box items.
    //
    // TODO (UI Modernization):
    // Rename legacy designer controls such as label1 and
    // btnMoreFindAndReplce to descriptive, consistently spelled names after the
    // current migration work is complete.
    //
    // TODO (Localization):
    // Move category-related UI text into application resources instead of
    // assigning English text directly in the selection-change handler.
    /// <summary>
    /// Updates category-related controls when the selected categorization action
    /// changes.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the selection change.
    /// </param>
    /// <remarks>
    /// Category input and skip controls are enabled for any categorization action
    /// other than the default selection. Secondary category and sort-key options
    /// are available only for the category-replacement action at index 1.
    /// </remarks>
    private void cmboCategorise_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        int selectedIndex =
            cmboCategorise.SelectedIndex;

        bool categorisationEnabled =
            selectedIndex > 0;

        bool replacingCategory =
            selectedIndex == 1;

        txtNewCategory.Enabled =
            categorisationEnabled;

        chkSkipNoCatChange.Enabled =
            categorisationEnabled;

        label1.Text =
            replacingCategory
                ? "with Category:"
                : string.Empty;

        txtNewCategory2.Enabled =
            replacingCategory;

        chkRemoveSortKey.Enabled =
            replacingCategory;
    }

    // TODO (Platform Modernization):
    // Re-test bot-tab updates on the currently supported runtimes and remove the
    // Mono-specific branch if Mono support is no longer required.
    /// <summary>
    /// Updates the controls and tab availability that depend on the current
    /// session's bot status.
    /// </summary>
    /// <remarks>
    /// Bot-only controls are enabled only for bot accounts. On supported
    /// platforms, the Bots tab is added or removed to match the current status.
    /// The bot timer is refreshed after the UI state changes.
    /// </remarks>
    private void UpdateBotStatus()
    {
        bool isBot = TheSession.IsBot;

        chkAutoMode.Enabled = isBot;
        chkSuppressTag.Enabled = isBot;
        lblOnlyBots.Visible = !isBot;

        if (!Globals.UsingMono)
        {
            UpdateBotTabVisibility(isBot);
        }

        UpdateBotTimer();
    }

    /// <summary>
    /// Adds or removes the Bots tab to match the current bot status.
    /// </summary>
    /// <param name="isBot">
    /// <see langword="true"/> when the current session is using a bot account;
    /// otherwise, <see langword="false"/>.
    /// </param>
    private void UpdateBotTabVisibility(bool isBot)
    {
        bool botTabVisible =
            MainTab.TabPages.Contains(tpBots);

        if (isBot)
        {
            if (!botTabVisible)
            {
                int startTabIndex =
                    MainTab.TabPages.IndexOf(tpStart);

                MainTab.TabPages.Insert(
                    startTabIndex,
                    tpBots);
            }

            return;
        }

        BotMode = false;

        if (botTabVisible)
        {
            MainTab.TabPages.Remove(tpBots);
        }
    }

    /// <summary>
    /// Updates page-management controls according to the current user's
    /// permissions and the state of the active page.
    /// </summary>
    /// <remarks>
    /// Page protection may be available for a page that does not yet exist,
    /// allowing administrators to create a protection entry for a future page.
    /// Move and delete operations additionally require the page to exist.
    /// </remarks>
    private void UpdateAdminStatus()
    {
        bool articleAvailable =
            TheArticle != null;

        bool saveAvailable =
            btnSave.Enabled;

        bool pageExists =
            TheSession.Page.Exists;

        bool canProtect =
            articleAvailable &&
            saveAvailable &&
            TheSession.User.CanProtectPage(
                TheSession.Page);

        bool canDelete =
            articleAvailable &&
            saveAvailable &&
            pageExists &&
            TheSession.User.CanDeletePage(
                TheSession.Page);

        btnProtect.Enabled = canProtect;
        btnMove.Enabled = canProtect && pageExists;

        btnDelete.Enabled = canDelete;
        btntsDelete.Enabled = canDelete;

        bypassAllRedirectsToolStripMenuItem.Enabled =
            TheSession.User.IsSysop;
    }

    // TODO (Architecture):
    // Move bot-mode eligibility rules and page-action permission calculations out
    // of MainForm and into dedicated policy helpers when UI state is separated
    // from session and authorization logic.
    //
    // TODO (UI Maintainability):
    // Verify whether BotMode should always be cleared when the bot tab is removed,
    // or whether account-status changes should be handled through a single
    // centralized bot-mode state transition.
    /// <summary>
    /// Applies the UI and processing changes required when automatic bot mode is
    /// enabled or disabled.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the checked-state change.
    /// </param>
    /// <remarks>
    /// Enabling bot mode also enables nudging and disables RegExTypoFix on
    /// Wikimedia projects, where automated typo fixing is not permitted by the
    /// current workflow. Disabling bot mode stops any pending delayed auto-save.
    /// </remarks>
    private void chkAutoMode_CheckedChanged(
        object sender,
        EventArgs e)
    {
        if (!BotMode)
        {
            SetBotModeEnabled(false);
            StopDelayedAutoSaveTimer();
            return;
        }

        SetBotModeEnabled(true);

        chkNudge.Checked = true;
        chkNudgeSkip.Checked = false;

        DisableRegexTypoFixForBotMode();
    }

    // TODO (Localization):
    // Move the bot-mode RegExTypoFix warning text and caption into application
    // resources instead of embedding English strings in the event handler.
    /// <summary>
    /// Disables RegExTypoFix when bot mode is used on a non-custom project.
    /// </summary>
    private void DisableRegexTypoFixForBotMode()
    {
        if (!chkRegExTypo.Checked ||
            Variables.IsCustomProject)
        {
            return;
        }

        MessageBox.Show(
            "Sorry, bot mode cannot be used with RegExTypoFix.\r\n" +
            "RegExTypoFix will now be turned off.",
            "Warning",
            MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation);

        chkRegExTypo.Checked = false;
    }

    // TODO (UI State Management):
    // Review whether SetBotModeEnabled() should also modify chkNudge.Checked.
    // Consider separating control availability from default bot-mode option
    // selection so the method name accurately reflects its behavior.
    /// <summary>
    /// Enables or disables the controls used to configure and monitor bot mode.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable bot-mode controls; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// The nudge option is also checked or cleared to match the enabled state,
    /// preserving the existing behavior.
    /// </remarks>
    private void SetBotModeEnabled(bool enabled)
    {
        label2.Enabled = enabled;
        nudBotSpeed.Enabled = enabled;
        botEditsStop.Enabled = enabled;
        lblAutoDelay.Enabled = enabled;
        lblbotEditsStop.Enabled = enabled;
        btnResetNudges.Enabled = enabled;
        lblNudges.Enabled = enabled;
        chkNudge.Enabled = enabled;
        chkNudgeSkip.Enabled = enabled;
        chkShutdown.Enabled = enabled;

        chkNudge.Checked = enabled;
    }

    // TODO (UI Modernization):
    // Review whether the About window should be modal or limited to a single
    // modeless instance, and ensure repeated menu selections cannot create
    // unnecessary duplicate windows.
    /// <summary>
    /// Opens the application About dialog.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu selection.
    /// </param>
    private void aboutToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        AboutBox aboutBox =
            new AboutBox(
                webBrowserHistory.Version.ToString());

        aboutBox.Show(this);
    }

    // TODO (UI Architecture):
    // Separate wiki-status evaluation from message boxes, dialog display, browser
    // navigation, and control updates so status checks can be tested without UI
    // side effects.
    //
    // TODO (Authentication Architecture):
    // Consolidate client compatibility, authentication, account eligibility,
    // AWB approval, block status, and MediaWiki capability discovery into a
    // single structured session-validation workflow. Introduce explicit disabled,
    // read-only, review-only, and full-editing modes, and enforce write
    // authorization below the UI layer.
    //
    // TODO (Event Handler Modernization):
    // Replace UpdateButtons(null, null) with a parameterless UI refresh helper and
    // keep the event handler as a thin adapter.
    //
    // TODO (Localization):
    // Move status messages, captions, and the registered-user status text into
    // application resources.
    //
    // TODO (Navigation Modernization):
    // Replace the hard-coded AWB check-page title with a centralized URI or
    // project-page constant.
    /// <summary>
    /// Checks the current wiki, software, and user status and updates the
    /// surrounding interface.
    /// </summary>
    /// <param name="login">
    /// <see langword="true"/> when the status check is being performed as part of
    /// an active login workflow; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the current user and software are registered
    /// and enabled; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The method may display status-specific messages, open the profile dialog,
    /// or open the AWB check page in the user's browser. It also refreshes
    /// right-to-left layout state and the surrounding status controls.
    /// </remarks>
    public bool CheckStatus(bool login)
    {
        StatusLabelText =
            "Loading page to check if we are logged in.";

        WikiStatusResult result =
            TheSession.Update();

        bool isRegistered = false;
        string statusText = "Software disabled";

        switch (result)
        {
            case WikiStatusResult.Error:
                ShowStatusCheckError();
                break;

            case WikiStatusResult.NotLoggedIn:
                HandleNotLoggedIn(login);
                break;

            case WikiStatusResult.NotRegistered:
                HandleNotRegistered();
                break;

            case WikiStatusResult.OldVersion:
                OldVersion();
                break;

            case WikiStatusResult.NoRights:
                NoWriteApiRight();
                break;

            case WikiStatusResult.Registered:
                PrepareRegisteredSession();

                isRegistered = true;
                statusText = BuildRegisteredStatusText();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported wiki status result: {result}.");
        }

        UpdateRightToLeftState();

        StatusLabelText = statusText;

        UpdateStatusUI();
        UpdateButtons(null, null);

        return isRegistered;
    }

    /// <summary>
    /// Displays an error when the wiki status page cannot be loaded.
    /// </summary>
    private static void ShowStatusCheckError()
    {
        MessageBox.Show(
            "Check page failed to load.\r\n\r\n" +
            "Check your Internet is working and that the Wiki is online.",
            "User check problem",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    /// <summary>
    /// Handles a status result indicating that the user is not logged in.
    /// </summary>
    /// <param name="login">
    /// <see langword="true"/> when an active login workflow is already underway;
    /// otherwise, <see langword="false"/>.
    /// </param>
    private void HandleNotLoggedIn(bool login)
    {
        if (login)
        {
            return;
        }

        MessageBox.Show(
            "You are not logged in. The profile screen will now load, " +
            "enter your name and password, click \"Log in\", wait for it " +
            "to complete, then start the process again.",
            "Not logged in",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        _profiles.ShowDialog();
    }

    /// <summary>
    /// Handles a status result indicating that the current user is not enabled
    /// to use AWB.
    /// </summary>
    private void HandleNotRegistered()
    {
        MessageBox.Show(
            $"{TheSession.User.Name} is not enabled to use this.",
            "Not enabled",
            MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation);

        Tools.OpenURLInBrowser(
            Variables.URLIndex +
            "?title=Project:AutoWikiBrowser/CheckPageJSON");
    }

    /// <summary>
    /// Refreshes the exclusion lists used by general fixes and RegExTypoFix for a
    /// registered session.
    /// </summary>
    private void PrepareRegisteredSession()
    {
        _noParse.Clear();
        _noRetf.Clear();

        _noParse.AddRangeIfNotNull(
            TheSession.NoGenfixes);

        _noRetf.AddRangeIfNotNull(
            TheSession.NoRETF);
    }

    /// <summary>
    /// Builds the status message displayed for a registered user.
    /// </summary>
    private string BuildRegisteredStatusText()
    {
        return string.Format(
            "Logged in, user and software enabled. Bot = {0}, Admin = {1}",
            TheSession.User.IsBot,
            TheSession.User.IsSysop);
    }

    /// <summary>
    /// Updates the form's writing direction to match the current wiki.
    /// </summary>
    private void UpdateRightToLeftState()
    {
        RightToLeft =
            Variables.RTL
                ? RightToLeft.Yes
                : RightToLeft.No;
    }

    // TODO (UI Consistency):
    // Verify that lblUserName.BackColor is restored after a later successful
    // status check so the old-version error state does not persist.
    //
    // TODO (Localization):
    // Move the old-version message and caption into application resources.
    //
    // TODO (Client Validation Architecture):
    // Return a structured client-version validation result instead of handling
    // update prompts and UI disabling directly inside MainForm.
    //
    // TODO (Twain Policy):
    // Replace this legacy global AWB version-enablement check with the per-wiki
    // Twain compatibility and policy service.
#pragma warning disable CA1303 // Literal messages are retained until localization work is completed.
    /// <summary>
    /// Handles an unsupported or disabled AWB version by disabling editing and
    /// offering the user the option to open the manual download page.
    /// </summary>
    /// <remarks>
    /// The legacy automatic updater has been removed. The user may open the manual
    /// download page or leave the application open with editing disabled.
    /// </remarks>
    private void OldVersion()
    {
        lblUserName.BackColor = Color.Red;
        DisableButtons();

        DialogResult result = MessageBox.Show(
            "This version of AWB is not enabled. Please download the newest " +
            "version. If you already have the newest version, check that Wikipedia " +
            "is online.\r\n\r\n" +
            "Would you like to open the download page?",
            "Problem",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (result == DialogResult.Yes)
        {
            OpenManualUpdatePage();
        }
    }
#pragma warning restore CA1303

    // TODO(Twain): Remove this legacy AWB manual-update fallback when the
    // AutoWikiBrowser application is retired or its remaining version-check
    // workflow is replaced by the Twain update and policy services.
    /// <summary>
    /// Opens the GitHub releases page for a manual application update.
    /// </summary>
    private static void OpenManualUpdatePage()
    {
        Tools.OpenURLInBrowser(
            "https://github.com/Reguyla/AutoWikiBrowser/releases");
    }

    private void CategoryLeave(object sender, EventArgs e)
    {
        TextBox cat = sender as TextBox;

        if (cat != null)
        {
            string text = cat.Text.Trim('[', ']');

            text = Regex.Replace(text, "^" + Variables.NamespacesCaseInsensitive[Namespace.Category], "");
            cat.Text = Tools.TurnFirstToUpper(text);
        }
    }

    // TODO (Shutdown Architecture):
    // Consolidate all application-close entry points through a single named
    // shutdown command if additional pre-close behavior is introduced.
    /// <summary>
    /// Closes the application when the Exit menu item is selected.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu selection.
    /// </param>
    private void exitToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        CloseAWB();
    }

    /// <summary>
    /// Initiates the normal application close workflow.
    /// </summary>
    private void CloseAWB()
    {
        Close();
    }

    // TODO (UI Maintainability):
    // Group append-related controls in a dedicated container or helper so their
    // enabled state can be updated as a single logical unit.
    /// <summary>
    /// Updates append and prepend controls when article text appending is enabled
    /// or disabled.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the checked-state change.
    /// </param>
    private void chkAppend_CheckedChanged(
        object sender,
        EventArgs e)
    {
        bool appendEnabled = chkAppend.Checked;

        txtAppendMessage.Enabled = appendEnabled;
        rdoAppend.Enabled = appendEnabled;
        rdoPrepend.Enabled = appendEnabled;
        udNewlineChars.Enabled = appendEnabled;
        lblUse.Enabled = appendEnabled;
        lblNewlineCharacters.Enabled = appendEnabled;
        chkAppendMetaDataSort.Enabled = appendEnabled;
    }

    // TODO (UI Modernization):
    // Rename wordWrapToolStripMenuItem1 to a descriptive name and verify whether
    // duplicate word-wrap controls should share a single command or checked state.
    /// <summary>
    /// Applies the selected word-wrap setting to the article editor.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the menu selection.
    /// </param>
    private void wordWrapToolStripMenuItem1_Click(
        object sender,
        EventArgs e)
    {
        txtEdit.WordWrap =
            wordWrapToolStripMenuItem1.Checked;
    }

    // TODO (Localization):
    // Move article statistics labels into application resources so they can be
    // localized independently of the application code.
    //
    // TODO (Documentation):
    // Document the meaning of "Dates O/I/A" or replace it with clearer
    // terminology if the UI is modernized.
    /// <summary>
    /// Label prefixes used when displaying article analysis statistics.
    /// </summary>
    private const string Words = "Words: ",
    Cats = "Categories: ",
    Imgs = "Images: ",
    Links = "Links: ",
    IWLinks = "Interwiki links: ",
    Dates = "Dates O/I/A: ";

    /// <summary>
    /// Clears any previous article analysis results and either resets the displayed
    /// article information or evaluates the current editor contents for alerts,
    /// statistics, date formats, and duplicate wikilinks.
    /// </summary>
    /// <param name="reset">
    /// <see langword="true"/> to restore the default article information labels
    /// without analyzing the current article; otherwise, <see langword="false"/>
    /// to analyze the current editor contents and update the related controls.
    /// </param>
    private void ArticleInfo(bool reset)
    {
        ClearArticleInfoResults();

        if (reset)
        {
            ResetArticleInfoLabels();
        }
        else
        {
            string articleText = txtEdit.Text;
            string templates =
                string.Join(
                    "",
                    Parsers.GetAllTemplateDetail(articleText).ToArray());

            int wordCount = Tools.WordCount(articleText);
            int catCount = WikiRegexes.Category.Matches(articleText).Count;

            bool hasAlertsOn = !alertPreferences.Any();

            EvaluateBasicArticleAlerts(
                templates,
                wordCount,
                catCount,
                hasAlertsOn);

            EvaluateArticleStructureAlerts(
                articleText,
                templates,
                hasAlertsOn);

            EvaluateCitationAndUrlAlerts(hasAlertsOn);
            EvaluateSicTagAlert(hasAlertsOn);
            EvaluateTalkAndUserNamespaceAlerts(hasAlertsOn);

            MatchCollection imagesMC =
                WikiRegexes.ImagesCountOnly.Matches(articleText);

            lblWords.Text = Words + wordCount;
            lblCats.Text = Cats + catCount;
            lblImages.Text = Imgs + imagesMC.Count;
            lblLinks.Text = Links + Tools.LinkCount(articleText);
            lblInterLinks.Text =
                IWLinks + Tools.InterwikiCount(articleText);

            UpdateDateStatistics(articleText, imagesMC);
            UpdateDuplicateWikilinks(articleText);
        }

        UpdateDuplicateWikilinkVisibility();
    }

    /// <summary>
    /// Clears article-analysis results left by the previous article or editor
    /// contents.
    /// </summary>
    private void ClearArticleInfoResults()
    {
        lbDuplicateWikilinks.Items.Clear();
        lbAlerts.Items.Clear();

        _ambiguousCiteDates.Clear();
        _badCiteParameters.Clear();
        _deadLinks.Clear();
        _doublePipeLinks.Clear();
        _duplicateBannerShellParameters.Clear();
        _targetlessLinks.Clear();
        _unclosedTags.Clear();
        _wikilinkedHeaders.Clear();
        _unbalancedBrackets.Clear();
        _otherErrors.Clear();
        _userSignatures.Clear();
    }

    /// <summary>
    /// Restores the default article-information label text.
    /// </summary>
    private void ResetArticleInfoLabels()
    {
        lblWords.Text = Words;
        lblCats.Text = Cats;
        lblImages.Text = Imgs;
        lblLinks.Text = Links;
        lblInterLinks.Text = IWLinks;
        lblDates.Text = Dates;
    }

    /// <summary>
    /// Shows duplicate-wikilink controls when duplicate links were found.
    /// </summary>
    private void UpdateDuplicateWikilinkVisibility()
    {
        bool hasDuplicateWikilinks =
            lbDuplicateWikilinks.Items.Count > 0;

        lblDuplicateWikilinks.Visible =
            hasDuplicateWikilinks;

        lbDuplicateWikilinks.Visible =
            hasDuplicateWikilinks;

        btnRemove.Visible =
            hasDuplicateWikilinks;
    }

    /// <summary>
    /// Finds duplicate wikilinks in the current article and displays them.
    /// </summary>
    /// <param name="articleText">
    /// The article text to analyze.
    /// </param>
    private void UpdateDuplicateWikilinks(string articleText)
    {
        // Find multiple wikilinks.
        // Get all the links, ignoring commented-out text and similar markup.
        lbDuplicateWikilinks.Items.AddRange(
            Tools.DuplicateWikiLinks(articleText).ToArray());
    }

    /// <summary>
    /// Adds an alert when the current article contains a <c>{{sic}}</c> tag or
    /// similar markup and the corresponding alert should be evaluated.
    /// </summary>
    /// <param name="hasAlertsOn">
    /// <see langword="true"/> when all article alerts are enabled because no
    /// individual alert preferences are selected.
    /// </param>
    /// <remarks>
    /// The alert is also evaluated whenever RegExTypoFix is enabled, even when
    /// the individual <c>sic</c> alert preference is disabled.
    /// </remarks>
    private void EvaluateSicTagAlert(bool hasAlertsOn)
    {
        bool shouldEvaluate =
            hasAlertsOn ||
            alertPreferences.Contains(2) ||
            chkRegExTypo.Checked;

        if (shouldEvaluate && TheArticle.HasSicTag)
        {
            lbAlerts.Items.Add("Contains 'sic' tag");
        }
    }

    /// <summary>
    /// Calculates and displays the article's date format statistics.
    /// </summary>
    /// <param name="articleText">
    /// The article text to analyze.
    /// </param>
    /// <param name="images">
    /// The image matches that should be ignored when counting date formats.
    /// </param>
    private void UpdateDateStatistics(
        string articleText,
        MatchCollection images)
    {
        // For date type counts, ignore images and external URLs.
        string articleTextNoImagesUrls =
            WikiRegexes.ExternalLinksHTTPOnlyQuick.Replace(
                Tools.ReplaceWithSpaces(articleText, images),
                "");

        Dictionary<Parsers.DateLocale, int> results =
            Tools.DatesCount(articleTextNoImagesUrls);

        lblDates.Text =
            Dates +
            results[Parsers.DateLocale.ISO] +
            "/" +
            results[Parsers.DateLocale.International] +
            "/" +
            results[Parsers.DateLocale.American];
    }

    /// <summary>
    /// Evaluates high-level article condition alerts, such as stub status,
    /// missing categories, and reference template usage.
    /// </summary>
    /// <param name="templates">
    /// The template markup extracted from the current article.
    /// </param>
    /// <param name="wordCount">
    /// The number of words in the current article.
    /// </param>
    /// <param name="catCount">
    /// The number of categories found in the current article.
    /// </param>
    /// <param name="hasAlertsOn">
    /// <see langword="true"/> when all alerts are enabled because no
    /// individual alert preferences are selected.
    /// </param>
    private void EvaluateBasicArticleAlerts(
        string templates,
        int wordCount,
        int catCount,
        bool hasAlertsOn)
    {
        if ((hasAlertsOn || alertPreferences.Contains(12))
            && TheArticle.NameSpaceKey == Namespace.Article
            && wordCount > Parsers.StubMaxWordCount
            && WikiRegexes.Stub.IsMatch(templates))
        {
            lbAlerts.Items.Add("Long article with a stub tag.");
        }

        if ((hasAlertsOn || alertPreferences.Contains(14))
            && catCount == 0
            && !Namespace.IsTalk(TheArticle.Name))
        {
            lbAlerts.Items.Add("No category (may be one in a template)");
        }

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Replace_nofootnotes_with_morefootnote_if_references_exists
        if ((hasAlertsOn || alertPreferences.Contains(7))
            && TheArticle.NameSpaceKey == Namespace.Article
            && TheArticle.HasMorefootnotesAndManyReferences)
        {
            lbAlerts.Items.Add(
                "Has 'No/More footnotes' template yet many references");
        }
    }

    // TODO(Twain): Replace numeric alert identifiers with named alert types
    // and move article-structure alert evaluation into a shared alert service.
    //
    // TODO: Review whether DEFAULTSORT and See also alerts should depend on
    // the double-pipe-link alert being enabled. They are currently nested
    // inside alert 10 processing and therefore are not evaluated independently.

    /// <summary>
    /// Evaluates article structure and reference-related alerts.
    /// </summary>
    /// <param name="articleText">
    /// The current article text.
    /// </param>
    /// <param name="templates">
    /// The template markup extracted from the current article.
    /// </param>
    /// <param name="hasAlertsOn">
    /// <see langword="true"/> when all alerts are enabled because no
    /// individual alert preferences are selected.
    /// </param>
    private void EvaluateArticleStructureAlerts(
        string articleText,
        string templates,
        bool hasAlertsOn)
    {
        if (IsAlertEnabled(hasAlertsOn, 16) &&
            TheArticle.NameSpaceKey == Namespace.Article &&
            articleText.StartsWith("=="))
        {
            lbAlerts.Items.Add("Starts with heading");
        }

        if (IsAlertEnabled(hasAlertsOn, 17))
        {
            _unbalancedBrackets =
                TheArticle.UnbalancedBrackets();

            if (_unbalancedBrackets.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Unbalanced brackets ({_unbalancedBrackets.Count})");
            }
        }

        if (IsAlertEnabled(hasAlertsOn, 11))
        {
            _targetlessLinks =
                TheArticle.TargetlessLinks();

            if (_targetlessLinks.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Links with no target ({_targetlessLinks.Count})");
            }
        }

        if (IsAlertEnabled(hasAlertsOn, 10))
        {
            _doublePipeLinks =
                TheArticle.DoublepipeLinks();

            if (_doublePipeLinks.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Links with double pipes ({_doublePipeLinks.Count})");
            }

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Detect_multiple_DEFAULTSORT
            if (IsAlertEnabled(hasAlertsOn, 13) &&
                WikiRegexes.Defaultsort.Matches(templates).Count > 1)
            {
                lbAlerts.Items.Add("Multiple DEFAULTSORTs");
            }

            if (IsAlertEnabled(hasAlertsOn, 15) &&
                TheArticle.HasSeeAlsoAfterNotesReferencesOrExternalLinks)
            {
                lbAlerts.Items.Add(
                    "See also section out of place");

                AddSeeAlsoHeadingError(articleText);
            }
        }
    }

    /// <summary>
    /// Locates the See also heading in the supplied article text and records
    /// its position for editor highlighting when it has not already been added.
    /// </summary>
    /// <param name="articleText">
    /// The article text to search.
    /// </param>
    private void AddSeeAlsoHeadingError(string articleText)
    {
        // Performance: fetching all headings and filtering them is faster than
        // applying WikiRegexes.SeeAlso directly to the entire article.
        Match seeAlsoHeading =
            WikiRegexes.Headings
                .Matches(articleText)
                .OfType<Match>()
                .FirstOrDefault(
                    heading =>
                        WikiRegexes.SeeAlso.IsMatch(
                            heading.Value));

        if (seeAlsoHeading != null &&
            !_otherErrors.ContainsKey(seeAlsoHeading.Index))
        {
            _otherErrors.Add(
                seeAlsoHeading.Index,
                seeAlsoHeading.Length);
        }
    }

    // TODO(Twain): Replace numeric alert identifiers with named alert types
    // and move citation/URL alert evaluation into a shared alert service.

    /// <summary>
    /// Evaluates citation and URL-related article alerts.
    /// </summary>
    /// <param name="hasAlertsOn">
    /// <see langword="true"/> when all alerts are enabled because no
    /// individual alert preferences are selected.
    /// </param>
    private void EvaluateCitationAndUrlAlerts(bool hasAlertsOn)
    {
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Some_additional_edits
        if (IsAlertEnabled(hasAlertsOn, 4))
        {
            _deadLinks = TheArticle.DeadLinks();

            if (_deadLinks.Any())
            {
                lbAlerts.Items.Add(
                    $"Dead links ({_deadLinks.Count})");
            }
        }

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#.28Yet.29_more_reference_related_changes.
        if (IsAlertEnabled(hasAlertsOn, 6) &&
            TheArticle.HasRefAfterReflist)
        {
            lbAlerts.Items.Add(
                @"Has a <ref> after <references/>");
        }

        if (IsAlertEnabled(hasAlertsOn, 3) &&
            TheArticle.IsDisambiguationPageWithRefs)
        {
            lbAlerts.Items.Add(
                @"DAB page with <ref>s");
        }

        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Format_references
        if (IsAlertEnabled(hasAlertsOn, 19) &&
            TheArticle.HasBareReferences)
        {
            lbAlerts.Items.Add(
                "Unformatted references");
        }

        if (IsAlertEnabled(hasAlertsOn, 1))
        {
            _ambiguousCiteDates =
                TheArticle.AmbiguousCiteTemplateDates();

            if (_ambiguousCiteDates.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Ambiguous citation dates ({_ambiguousCiteDates.Count})");
            }
        }

        if (IsAlertEnabled(hasAlertsOn, 20))
        {
            _unknownMultipleIssuesParameters =
                TheArticle.UnknownMultipleIssuesParameters();

            if (_unknownMultipleIssuesParameters.Count > 0)
            {
                string warning =
                    $"Unknown parameters in Multiple issues ({_unknownMultipleIssuesParameters.Count}): " +
                    string.Join(
                        ", ",
                        _unknownMultipleIssuesParameters);

                lbAlerts.Items.Add(warning);
            }
        }

        if (IsAlertEnabled(hasAlertsOn, 8))
        {
            _wikilinkedHeaders =
                TheArticle.WikiLinkedHeaders();

            if (_wikilinkedHeaders.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Header(s) with wikilinks ({_wikilinkedHeaders.Count})");
            }
        }

        if (IsAlertEnabled(hasAlertsOn, 18))
        {
            _unclosedTags =
                TheArticle.UnclosedTags();

            if (_unclosedTags.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Unclosed tag(s) ({_unclosedTags.Count})");
            }
        }

        if (IsAlertEnabled(hasAlertsOn, 9))
        {
            _badCiteParameters =
                TheArticle.BadCiteParameters();

            if (_badCiteParameters.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Invalid citation parameter(s) ({_badCiteParameters.Count})");
            }
        }
    }

    /// <summary>
    /// Evaluates alerts related to WikiProject banner parameters on talk pages
    /// and links to user namespaces in article content.
    /// </summary>
    /// <param name="hasAlertsOn">
    /// <see langword="true"/> when all alerts are enabled because no
    /// individual alert preferences are selected.
    /// </param>
    private void EvaluateTalkAndUserNamespaceAlerts(bool hasAlertsOn)
    {
        if (IsAlertEnabled(hasAlertsOn, 5))
        {
            _duplicateBannerShellParameters =
                TheArticle.DuplicateWikiProjectBannerShellParameters();

            if (_duplicateBannerShellParameters.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Duplicate parameter(s) in WPBannerShell ({_duplicateBannerShellParameters.Count})");
            }
        }

        if (IsAlertEnabled(hasAlertsOn, 21))
        {
            _unknownWikiProjectBannerShellParameters =
                TheArticle.UnknownWikiProjectBannerShellParameters();

            if (_unknownWikiProjectBannerShellParameters.Count > 0)
            {
                string warning =
                    $"Unknown parameters in WikiProject banner shell ({_unknownWikiProjectBannerShellParameters.Count}): " +
                    string.Join(", ", _unknownWikiProjectBannerShellParameters);

                lbAlerts.Items.Add(warning);
            }
        }

        // TODO(Twain): Replace numeric alert identifiers with named alert types
        // and move alert evaluation out of MainForm into a shared alert service.
        if (IsAlertEnabled(hasAlertsOn, 22) &&
            TheArticle.NameSpaceKey == Namespace.Article)
        {
            _userSignatures =
                TheArticle.UserSignature();

            if (_userSignatures.Count > 0)
            {
                lbAlerts.Items.Add(
                    $"Editor's signature or link to user space ({_userSignatures.Count})");
            }
        }
    }

    /// <summary>
    /// Determines whether the specified alert is enabled by the current alert
    /// configuration.
    /// </summary>
    /// <param name="hasAlertsOn">
    /// <see langword="true"/> when all alerts are enabled.
    /// </param>
    /// <param name="alertId">
    /// The alert identifier to evaluate.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the alert should be evaluated; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool IsAlertEnabled(
        bool hasAlertsOn,
        int alertId)
    {
        return hasAlertsOn ||
            alertPreferences.Contains(alertId);
    }

    // TODO (Editor Architecture):
    // Centralize conversion between editor selection positions and article-text
    // offsets so newline normalization does not require local compensation.
    /// <summary>
    /// Moves the editor selection to the next recorded alert after the current
    /// caret position, wrapping to the first alert when no later alert exists.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void lbAlerts_Click(object sender, EventArgs e)
    {
        EditBoxTab.SelectedTab = tpEdit;

        int caretPosition = txtEdit.SelectionStart;
        string textBeforeCaret = txtEdit.Text[..caretPosition];

        // Alert positions account for newline characters that are normalized by
        // the editor control.
        int newlineOffset =
            WikiRegexes.Newline.Matches(textBeforeCaret).Count;

        int adjustedCaretPosition =
            caretPosition + newlineOffset;

        if (TrySelectNextAlert(adjustedCaretPosition))
        {
            return;
        }

        // No alert remains after the caret, so wrap to the first alert.
        TrySelectNextAlert(0);
    }

    /// <summary>
    /// Selects the first recorded alert occurring after the specified position.
    /// </summary>
    /// <param name="position">
    /// The article-text position after which to find an alert.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when an alert was selected; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool TrySelectNextAlert(int position)
    {
        foreach (KeyValuePair<int, int> error in
                 _errors.OrderBy(error => error.Key))
        {
            if (error.Key <= position ||
                error.Key >= txtEdit.Text.Length)
            {
                continue;
            }

            RedSelection(error.Key, error.Value);
            txtEdit.ScrollToCaret();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the selected duplicate wikilink in the editor, refreshes the article
    /// analysis, and restores the duplicate-link selection.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void lbDuplicateWikilinks_Click(object sender, EventArgs e)
    {
        EditBoxTab.SelectedTab = tpEdit;

        int selectedIndex = lbDuplicateWikilinks.SelectedIndex;

        UpdateDuplicateWikilinkSearch(selectedIndex);

        ArticleInfo(false);

        RestoreDuplicateWikilinkSelection(selectedIndex);

        _oldSelection = selectedIndex;
    }

    /// <summary>
    /// Clears the current search results, resets any search highlighting in the
    /// editor, and updates the Find button state.
    /// </summary>
    /// <param name="sender">
    /// The control that initiated the reset. When the sender is a
    /// <see cref="RichTextBox"/>, its formatting is restored.
    /// </param>
    /// <param name="e">
    /// The event data associated with the reset operation.
    /// </param>
    /// <remarks>
    /// Under Mono, resetting the editor formatting can trigger the search text
    /// change handler recursively. The handler is temporarily detached to prevent
    /// unnecessary re-entrant processing before being restored.
    /// </remarks>
    private void ResetFind(object sender, EventArgs e)
    {
        txtEdit.ResetFind();

        if (sender is RichTextBox richTextBox)
        {
            bool detachTextChanged = Globals.UsingMono;

            if (detachTextChanged)
            {
                txtFind.TextChanged -= ResetFind;
            }

            try
            {
                richTextBox.ResetFormatting();
            }
            finally
            {
                if (detachTextChanged)
                {
                    txtFind.TextChanged += ResetFind;
                }
            }
        }

        btnFind.Enabled = txtFind.TextLength > 0;

        if (!btnFind.Enabled)
        {
            btnFind.BackColor = SystemColors.ButtonFace;
        }
    }

    /// <summary>
    /// Updates the editor search for the currently selected duplicate wikilink.
    /// </summary>
    /// <param name="selectedIndex">
    /// The selected duplicate-wikilink list index, or <c>-1</c> when no item is
    /// selected.
    /// </param>
    private void UpdateDuplicateWikilinkSearch(int selectedIndex)
    {
        if (selectedIndex != _oldSelection)
        {
            txtEdit.ResetFind();
        }

        if (selectedIndex < 0)
        {
            ClearDuplicateWikilinkSearch();
            return;
        }

        string selectedItem =
            lbDuplicateWikilinks.SelectedItem?.ToString() ?? string.Empty;

        string link =
            ExtractDuplicateWikilink(selectedItem);

        if (string.IsNullOrEmpty(link))
        {
            ClearDuplicateWikilinkSearch();
            return;
        }

        string searchPattern =
            BuildDuplicateWikilinkSearchPattern(link);

        txtEdit.Find(
            searchPattern,
            true,
            true,
            TheArticle.Name);

        btnRemove.Enabled = true;
    }

    /// <summary>
    /// Removes the appended duplicate count from a displayed duplicate wikilink.
    /// </summary>
    /// <param name="selectedItem">
    /// The duplicate-wikilink display text.
    /// </param>
    /// <returns>
    /// The wikilink text without the appended duplicate count.
    /// </returns>
    private static string ExtractDuplicateWikilink(
        string selectedItem)
    {
        return Regex.Replace(
            selectedItem,
            @" \(\d+\)$",
            string.Empty);
    }

    /// <summary>
    /// Clears the duplicate-wikilink search and disables link removal.
    /// </summary>
    private void ClearDuplicateWikilinkSearch()
    {
        txtEdit.ResetFind();
        btnRemove.Enabled = false;
    }

    /// <summary>
    /// Builds the regular expression used to locate a duplicate wikilink while
    /// allowing the first character of the link target to differ by case.
    /// </summary>
    /// <param name="link">The wikilink target to locate.</param>
    /// <returns>A regular expression matching the corresponding wikilink.</returns>
    private static string BuildDuplicateWikilinkSearchPattern(string link)
    {
        string firstCharacter =
            Regex.Escape(link[0].ToString());

        string remainingCharacters =
            Regex.Escape(link[1..]);

        return
            "\\[\\[(?i)" +
            firstCharacter +
            "(?-i)" +
            remainingCharacters +
            "(\\|.*?)?\\]\\]";
    }

    /// <summary>
    /// Restores the duplicate-wikilink list selection after article analysis
    /// rebuilds the list contents.
    /// </summary>
    /// <param name="selectedIndex">
    /// The list index selected before the article analysis was refreshed.
    /// </param>
    private void RestoreDuplicateWikilinkSelection(int selectedIndex)
    {
        if (lbDuplicateWikilinks.Items.Count == 0)
        {
            return;
        }

        try
        {
            if (lbDuplicateWikilinks.Items.Count != selectedIndex + 2)
            {
                lbDuplicateWikilinks.SelectedIndex = selectedIndex + 2;
            }
            else
            {
                lbDuplicateWikilinks.SelectedIndex = selectedIndex + 1;
            }

            lbDuplicateWikilinks.SelectedIndex = selectedIndex;
        }
        catch (ArgumentOutOfRangeException)
        {
            lbDuplicateWikilinks.SelectedIndex =
                lbDuplicateWikilinks.Items.Count - 1;
        }
    }

    /// <summary>
    /// Clears the current editor search state when the article text changes.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void txtEdit_TextChanged(object sender, EventArgs e)
    {
        txtEdit.ResetFind();
    }

    /// <summary>
    /// Searches the editor for the text or regular expression entered in the Find
    /// box using the selected case-sensitivity and regular-expression options.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void btnFind_Click(object sender, EventArgs e)
    {
        if (txtFind.TextLength == 0)
        {
            return;
        }

        EditBoxTab.SelectedTab = tpEdit;

        txtEdit.Find(
            txtFind.Text,
            chkFindRegex.Checked,
            chkFindCaseSensitive.Checked,
            TheArticle?.Name ?? string.Empty);
    }

    /// <summary>
    /// Clears the toolbar text box when its placeholder text is clicked.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void toolStripTextBox2_Click(object sender, EventArgs e)
    {
        if (toolStripTextBox2.Text == "Placeholder text")
        {
            toolStripTextBox2.Clear();
        }
    }

    /// <summary>
    /// Restricts the Go To Line text box to numeric input and navigates to the
    /// requested line when the Enter key is pressed.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// Information about the pressed key.
    /// </param>
    private void toolStripTextBox2_KeyPress(
        object sender,
        KeyPressEventArgs e)
    {
        // Allow digits, Backspace, and Enter only.
        if (!char.IsDigit(e.KeyChar) &&
            e.KeyChar != '\b' &&
            e.KeyChar != '\r')
        {
            e.Handled = true;
            return;
        }

        if (e.KeyChar != '\r')
        {
            return;
        }

        e.Handled = true;

        if (int.TryParse(
                toolStripTextBox2.Text,
                out int lineNumber))
        {
            txtEdit.GoToLine(lineNumber);
            mnuTextBox.Hide();
        }
    }

    /// <summary>
    /// Enables AWB diagnostic features, exposes debugging menu commands, ensures
    /// the AWB sandbox is available in the article list, and initializes profiling
    /// when compiled in the Debug configuration.
    /// </summary>
    private void EnableDebugMode()
    {
        Tools.WriteDebugEnabled = true;

        EnsureDebugSandboxIsAvailable();

        lblOnlyBots.Visible = false;
        bypassAllRedirectsToolStripMenuItem.Enabled = true;

        profileTyposToolStripMenuItem.Visible = true;
        toolStripSeparator29.Visible = true;
        invalidateCacheToolStripMenuItem.Visible = true;
        toolStripSeparator32.Visible = true;
        cEvalToolStripMenuItem.Visible = true;

        InitializeDebugProfiler();
    }

    /// <summary>
    /// Adds the AWB sandbox to the current list when neither recognized sandbox
    /// title is already present.
    /// </summary>
    private void EnsureDebugSandboxIsAvailable()
    {
        bool containsSandbox =
            listMaker.Contains(@"Wikipedia:AutoWikiBrowser/Sandbox") ||
            listMaker.Contains(@"Project:AutoWikiBrowser/Sandbox");

        if (!containsSandbox)
        {
            listMaker.Add("Project:AutoWikiBrowser/Sandbox");
        }
    }

    /// <summary>
    /// Initializes the debug profiler, preferring the application directory and
    /// falling back to the user data directory when that location is unavailable.
    /// </summary>
    private static void InitializeDebugProfiler()
    {
        string applicationPath =
            Path.Combine(Application.StartupPath, "profiling.txt");

        try
        {
            Variables.Profiler = new Profiler(applicationPath, true);
        }
        catch (UnauthorizedAccessException)
        {
            string userDataPath =
                Path.Combine(ApplicationPaths.UserData, "profiling.txt");

            Variables.Profiler = new Profiler(userDataPath, true);
        }
    }

    /// <summary>
    /// Performs release-only UI initialization.
    /// </summary>
    /// <remarks>
    /// This method is compiled only for release builds.
    /// </remarks>
    [Conditional("RELEASE")]
    private void Release()
    {
        if (MainTab.Contains(tpBots) && !Globals.UsingMono)
        {
            MainTab.Controls.Remove(tpBots);
        }
    }

    #endregion

    #region set variables

    /// <summary>
    /// Opens the Preferences dialog.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void PreferencesToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        OpenPreferences(false);
    }

    List<int> alertPreferences = new();

    // TODO: Replace the manual preference mapping between Main and MyPreferences
    // with a dedicated settings model or mapper to reduce duplication and make
    // future preference additions less error-prone.
    //
    // TODO: Move project-change side effects out of the preferences UI workflow
    // so project switching can be handled consistently from other entry points.

    /// <summary>
    /// Opens the preferences dialog, applies any accepted preference changes,
    /// and updates project-dependent state when the selected wiki changes.
    /// </summary>
    /// <param name="focusSiteTab">
    /// <see langword="true"/> to open the preferences dialog with the site
    /// settings tab focused; otherwise, <see langword="false"/>.
    /// </param>
    private void OpenPreferences(bool focusSiteTab)
    {
        MyPreferences myPrefs =
            CreatePreferencesDialog(
                focusSiteTab);

        if (myPrefs.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        bool projectChanged =
            HasPreferencesProjectChanged(myPrefs);

        ApplyAcceptedPreferences(myPrefs);

        if (projectChanged)
        {
            ApplyPreferencesProjectChange(myPrefs);
        }
    }

    /// <summary>
    /// Creates and initializes the preferences dialog from the current
    /// application settings.
    /// </summary>
    /// <param name="focusSiteTab">
    /// <see langword="true"/> to focus the site settings tab; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <returns>The initialized preferences dialog.</returns>
    private MyPreferences CreatePreferencesDialog(bool focusSiteTab)
    {
        return new MyPreferences(
            Variables.LangCode,
            Variables.Project,
            Variables.CustomProject,
            Variables.Protocol)
        {
            TextBoxFont = txtEdit.Font,
            LowThreadPriority = LowThreadPriority,
            PrefFlash = _flash,
            PrefBeep = _beep,
            PrefMinimize = _minimize,
            PrefSaveArticleList = _saveArticleList,

            PrefAutoSaveEditBoxEnabled = _autoSaveEditBoxEnabled,
            PrefAutoSaveEditBoxFile = _autoSaveEditBoxFile,
            PrefAutoSaveEditBoxPeriod = AutoSaveEditBoxPeriod,

            PrefIgnoreNoBots = _ignoreNoBots,
            PrefClearPageListOnProjectChange = _clearPageListOnProjectChange,

            PrefShowTimer = ShowMovingAverageTimer,
            PrefAddUsingAWBOnArticleAction = Article.AddUsingAWBOnArticleAction,
            PrefSuppressUsingAWB = _suppressUsingAWB,

            PrefListComparerUseCurrentArticleList =
                _listComparerUseCurrentArticleList,
            PrefListSplitterUseCurrentArticleList =
                _listSplitterUseCurrentArticleList,
            PrefDBScannerUseCurrentArticleList =
                _dbScannerUseCurrentArticleList,

            PrefDiffInBotMode = _doDiffInBotMode,
            PrefOnLoad = GetSupportedActionOnLoadValue(),

            EnableLogging = _loggingEnabled,
            FocusSiteTab = focusSiteTab,

            PrefDomain = Variables.LoginDomain,

            AlertPreferences = alertPreferences
        };
    }

    /// <summary>
    /// Returns the supported preferences value for the current on-load action,
    /// mapping the obsolete "show edit page" value to the default action.
    /// </summary>
    /// <returns>The supported on-load action value.</returns>
    private int GetSupportedActionOnLoadValue()
    {
        // TODO: Remove this compatibility mapping once the obsolete
        // "show edit page" persisted value is no longer supported.
        return _actionOnLoad == 2
            ? 0
            : _actionOnLoad;
    }

    /// <summary>
    /// Applies preference values accepted by the user to the current
    /// application state.
    /// </summary>
    /// <param name="myPrefs">
    /// The accepted preferences dialog containing the updated settings.
    /// </param>
    private void ApplyAcceptedPreferences(MyPreferences myPrefs)
    {
        txtEdit.Font = myPrefs.TextBoxFont;
        LowThreadPriority = myPrefs.LowThreadPriority;
        _flash = myPrefs.PrefFlash;
        _beep = myPrefs.PrefBeep;
        _minimize = myPrefs.PrefMinimize;
        _saveArticleList = myPrefs.PrefSaveArticleList;
        _autoSaveEditBoxEnabled = myPrefs.PrefAutoSaveEditBoxEnabled;

        if (EditBoxSaveTimer.Enabled &&
            !_autoSaveEditBoxEnabled)
        {
            EditBoxSaveTimer.Enabled = false;
        }

        AutoSaveEditBoxPeriod =
            myPrefs.PrefAutoSaveEditBoxPeriod;

        _autoSaveEditBoxFile =
            myPrefs.PrefAutoSaveEditBoxFile;

        _suppressUsingAWB =
            myPrefs.PrefSuppressUsingAWB;

        Article.AddUsingAWBOnArticleAction =
            myPrefs.PrefAddUsingAWBOnArticleAction;

        _ignoreNoBots =
            myPrefs.PrefIgnoreNoBots;

        _clearPageListOnProjectChange =
            myPrefs.PrefClearPageListOnProjectChange;

        ShowMovingAverageTimer =
            myPrefs.PrefShowTimer;

        _listComparerUseCurrentArticleList =
            myPrefs.PrefListComparerUseCurrentArticleList;

        _listSplitterUseCurrentArticleList =
            myPrefs.PrefListSplitterUseCurrentArticleList;

        _dbScannerUseCurrentArticleList =
            myPrefs.PrefDBScannerUseCurrentArticleList;

        _doDiffInBotMode =
            myPrefs.PrefDiffInBotMode;

        _actionOnLoad =
            myPrefs.PrefOnLoad;

        _loggingEnabled =
            myPrefs.EnableLogging;

        Variables.LoginDomain =
            myPrefs.PrefDomain;

        alertPreferences =
            myPrefs.AlertPreferences;
    }

    /// <summary>
    /// Determines whether the accepted preferences select a different wiki
    /// project or connection configuration.
    /// </summary>
    /// <param name="myPrefs">
    /// The accepted preferences to compare with the current project.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the project settings changed; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool HasPreferencesProjectChanged(
        MyPreferences myPrefs)
    {
        return myPrefs.Language != Variables.LangCode ||
            myPrefs.Project != Variables.Project ||
            myPrefs.CustomProject != Variables.CustomProject ||
            myPrefs.Protocol != Variables.Protocol;
    }

    /// <summary>
    /// Applies project-dependent state changes after the selected wiki changes.
    /// </summary>
    /// <param name="myPrefs">
    /// The accepted preferences containing the new project settings.
    /// </param>
    private void ApplyPreferencesProjectChange(
        MyPreferences myPrefs)
    {
        SetProject(
            myPrefs.Language,
            myPrefs.Project,
            myPrefs.CustomProject,
            myPrefs.Protocol);

        BotMode = false;
        lblOnlyBots.Visible = true;

        if (_clearPageListOnProjectChange)
        {
            listMaker.Clear();
        }

        DisableButtons();
    }

    /// <summary>
    /// Reloads the current wiki status and refreshes cached project data that has
    /// already been loaded during the current session.
    /// </summary>
    /// <param name="sender">The menu item that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void reloadToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        // Refresh login status and reload the checklist.
        CheckStatus(false);

        // Refresh the typo list.
        LoadTypos(true);

        WikiDiff.ResetCustomStyles();

        // Refresh optional data only when it was previously loaded.
        if (_userTalkWarningsLoaded)
            LoadUserTalkWarnings();

        if (_templateRedirectsLoaded)
            LoadTemplateRedirects();

        if (_datedTemplatesLoaded)
            LoadDatedTemplates();

        if (_renamedTemplateParametersLoaded)
            LoadRenameTemplateParameters();
    }

    /// <summary>
    /// Records the current usage statistics when necessary and resets the
    /// edit, skip, page, and processing counters.
    /// </summary>
    /// <param name="sender">The menu item that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void resetEditSkippedCountToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {

        _sessionCounters.NumberOfEdits = 0;
        _sessionCounters.NumberOfIgnoredEdits = 0;
        _sessionCounters.NumberOfEditsPerMinute = 0;
        _sessionCounters.NumberOfNewPages = 0;
        _sessionCounters.NumberOfPagesPerMinute = 0;
        _sessionCounters.NumberOfPagesParsed = 0;
    }

    /// <summary>
    /// Loads the selected wiki project and updates project-dependent application
    /// state and user-interface settings.
    /// </summary>
    /// <param name="code">The project language code.</param>
    /// <param name="project">The project type.</param>
    /// <param name="customProject">The custom project URL or identifier.</param>
    /// <param name="protocol">The protocol used to access the project.</param>
    private void SetProject(
        string code,
        ProjectEnum project,
        string customProject,
        string protocol)
    {
        _splashScreen.SetProgress(81);

        if (!TryLoadProject(
                code,
                project,
                customProject,
                protocol))
        {
            return;
        }

        ShowRestrictedWikiMessageIfRequired();
        ConfigureInterWikiOrder(project);
        ConfigureProjectSpecificUi();
        UpdateProjectLabel();

        _userTalkWarningsLoaded = false;
        _templateRedirectsLoaded = false;

        ResetTypoStats();
    }

    /// <summary>
    /// Attempts to load the selected wiki project, prompting for authentication
    /// and retrying once when the server returns an unauthorized response.
    /// </summary>
    /// <param name="code">The project language code.</param>
    /// <param name="project">The project type.</param>
    /// <param name="customProject">The custom project URL or identifier.</param>
    /// <param name="protocol">The protocol used to access the project.</param>
    /// <returns>
    /// <see langword="true"/> when the project loads successfully; otherwise,
    /// <see langword="false"/> when a known project-loading error is shown to the
    /// user.
    /// </returns>
    /// <exception cref="WebException">
    /// Thrown when the legacy project-loading path fails with a network error other
    /// than an unauthorized response.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when an HTTP-based project-loading path fails with a network error
    /// other than an unauthorized response.
    /// </exception>
    private bool TryLoadProject(
        string code,
        ProjectEnum project,
        string customProject,
        string protocol)
    {
        try
        {
            LoadProject(
                code,
                project,
                customProject,
                protocol);

            return true;
        }
        catch (Exception ex) when (IsUnauthorizedResponse(ex))
        {
            ShowLogin();

            LoadProject(
                code,
                project,
                customProject,
                protocol);

            return true;
        }
        catch (UriFormatException)
        {
            MessageBox.Show(
                "Check the site url you entered is valid, and try again!");

            return false;
        }
        catch (ArgumentNullException)
        {
            MessageBox.Show(
                "The interwiki list didn't load correctly. Please check your internet connection, and then restart the application.");

            return false;
        }
    }

    /// <summary>
    /// Loads the selected project into the shared application variables.
    /// </summary>
    /// <param name="code">The project language code.</param>
    /// <param name="project">The project type.</param>
    /// <param name="customProject">The custom project URL or identifier.</param>
    /// <param name="protocol">The protocol used to access the project.</param>
    private static void LoadProject(
        string code,
        ProjectEnum project,
        string customProject,
        string protocol)
    {
        Variables.SetProject(
            code,
            project,
            customProject,
            protocol);
    }

    /// <summary>
    /// Determines whether an exception or one of its inner exceptions represents
    /// an HTTP 401 Unauthorized response.
    /// </summary>
    /// <param name="exception">
    /// The exception at the beginning of the exception chain to inspect.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the exception chain contains an unauthorized
    /// HTTP response; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Supports both the legacy <see cref="WebException"/> response model and the
    /// modern <see cref="HttpRequestException"/> status-code model. Legacy support
    /// can be removed after the remaining project-loading paths have been migrated
    /// from <c>HttpWebRequest</c> to <c>HttpClient</c>.
    /// </remarks>
    private static bool IsUnauthorizedResponse(
        Exception exception)
    {
        for (Exception? current = exception;
             current != null;
             current = current.InnerException)
        {
            if (current is WebException
                {
                    Response: HttpWebResponse
                    {
                        StatusCode: HttpStatusCode.Unauthorized
                    }
                })
            {
                return true;
            }

            if (current is HttpRequestException
                {
                    StatusCode: HttpStatusCode.Unauthorized
                })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Displays a message when project loading has been deferred until after the
    /// user authenticates with a restricted wiki.
    /// </summary>
    private static void ShowRestrictedWikiMessageIfRequired()
    {
        if (!Variables.TryLoadingAgainAfterLogin)
            return;

        MessageBox.Show(
            "You seem to be accessing a private wiki. Project loading will be attempted again after login.",
            "Restricted Wiki");
    }

    /// <summary>
    /// Configures the parser's interwiki ordering for the current language and
    /// project.
    /// </summary>
    /// <param name="project">The currently selected project.</param>
    private void ConfigureInterWikiOrder(
        ProjectEnum project)
    {
        _parser.InterWikiOrder =
            GetInterWikiOrder(
                Variables.LangCode);

        if (project == ProjectEnum.commons)
        {
            _parser.InterWikiOrder =
                InterWikiOrderEnum.Alphabetical;
        }
    }

    /// <summary>
    /// Gets the interwiki ordering appropriate for the specified language code.
    /// </summary>
    /// <param name="languageCode">The wiki language code.</param>
    /// <returns>The interwiki ordering used by the parser.</returns>
    private static InterWikiOrderEnum GetInterWikiOrder(
        string languageCode)
    {
        return languageCode switch
        {
            "en" or "lb" or "pl" or "no" or "sv" or "simple"
                => InterWikiOrderEnum.LocalLanguageAlpha,

            "he" or "hu" or "te" or "yi"
                => InterWikiOrderEnum.AlphabeticalEnFirst,

            "ms" or "et" or "nn" or "fi" or "vi" or "ur"
                => InterWikiOrderEnum.LocalLanguageFirstWord,

            _ => InterWikiOrderEnum.Alphabetical
        };
    }

    /// <summary>
    /// Updates controls whose availability depends on whether the current project
    /// is English Wikipedia.
    /// </summary>
    private void ConfigureProjectSpecificUi()
    {
        bool isEnglishWikipedia =
            Variables.IsWikipediaEN;

        humanNameDisambigTagToolStripMenuItem.Visible =
            isEnglishWikipedia;

        birthdeathCatsToolStripMenuItem.Visible =
            isEnglishWikipedia;

        if (!isEnglishWikipedia)
        {
            chkAutoTagger.Checked = false;
        }
    }

    /// <summary>
    /// Updates the project label using the current project type, language code,
    /// and configured wiki URL.
    /// </summary>
    private void UpdateProjectLabel()
    {
        if (!Variables.IsCustomProject
            && !Variables.IsWikia
            && !Variables.IsWikimediaMonolingualProject)
        {
            lblProject.Text =
                Variables.LangCode
                + "."
                + Variables.Project;

            return;
        }

        lblProject.Text =
            Variables.IsWikimediaMonolingualProject
                ? Variables.Project.ToString()
                : Variables.URL;
    }
    #endregion

    // TODO: Cleanup/refactor UI update functions.
    #region Enabling/Disabling of buttons

    /// <summary>
    /// Updates the availability of list-related commands and refreshes the
    /// displayed article count.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void UpdateButtons(object sender, EventArgs e)
    {
        SetStartButton(listMaker.NumberOfArticles > 0);

        lbltsNumberofItems.Text =
            "Pages: " + listMaker.NumberOfArticles;

        specialFilterToolStripMenuItem1.Enabled =
            saveListToTextFileToolStripMenuItem.Enabled =
            clearCurrentListToolStripMenuItem.Enabled =
            convertFromTalkPagesToolStripMenuItem.Enabled =
            convertToTalkPagesToolStripMenuItem.Enabled =
            listMaker.NumberOfArticles > 0;
    }

    /// <summary>
    /// Enables or disables the Start controls without raising redundant
    /// <see cref="Control.EnabledChanged"/> events.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable the Start controls; otherwise,
    /// <see langword="false"/>.
    /// </param>
    private void SetStartButton(bool enabled)
    {
        // Avoid raising EnabledChanged when the requested state is already set.
        // Some plugins subscribe to this event.
        if (btnStart.Enabled != enabled)
        {
            btnStart.Enabled = enabled;
        }

        if (btntsStart.Enabled != enabled)
        {
            btntsStart.Enabled = enabled;
        }
    }

    /// <summary>
    /// Disables editing and article-processing controls.
    /// </summary>
    private void DisableButtons()
    {
        SetStartButton(false);
        SetButtons(false);

        if (listMaker.NumberOfArticles == 0)
        {
            btnIgnore.Enabled = false;
        }

        if (cmboEditSummary.Focused)
        {
            txtEdit.Focus();
        }

        txtEdit.Enabled =
            txtReviewEditSummary.Enabled =
            false;
    }

    /// <summary>
    /// Enables editing and article-processing controls and refreshes their
    /// current state.
    /// </summary>
    private void EnableButtons()
    {
        UpdateButtons(null, null);
        SetButtons(true);

        txtEdit.Enabled =
            txtReviewEditSummary.Enabled =
            true;
    }

    /// <summary>
    /// Enables or disables article-processing controls based on the current
    /// application, article, page, and user state.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable controls when their individual
    /// conditions are satisfied; otherwise, <see langword="false"/>.
    /// </param>
    private void SetButtons(bool enabled)
    {
        SetPrimaryProcessingControlsEnabled(enabled);

        btnSave.Enabled =
            CanSaveCurrentArticle(enabled);

        btnProtect.Enabled =
            CanProtectCurrentPage(enabled);

        btnMove.Enabled =
            CanMoveCurrentPage(enabled);

        btnDelete.Enabled =
            btntsDelete.Enabled =
            CanDeleteCurrentPage(enabled);

        UpdateFindButtonState();
    }

    /// <summary>
    /// Applies the common enabled state to primary processing controls.
    /// </summary>
    /// <param name="enabled">
    /// The enabled state to apply.
    /// </param>
    private void SetPrimaryProcessingControlsEnabled(bool enabled)
    {
        btnIgnore.Enabled =
            btnPreview.Enabled =
            btnDiff.Enabled =
            btntsPreview.Enabled =
            btntsChanges.Enabled =
            /* listMaker.MakeListEnabled = */
            btntsSave.Enabled =
            btntsIgnore.Enabled =
            btnWatch.Enabled =
            findGroup.Enabled =
            enabled;
    }

    /// <summary>
    /// Determines whether the current article can be saved.
    /// </summary>
    /// <param name="enabled">
    /// Whether article-processing controls are generally enabled.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if saving should be enabled; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool CanSaveCurrentArticle(bool enabled)
    {
        return enabled &&
            TheArticle != null &&
            !string.IsNullOrEmpty(TheSession.Page.Title);
    }

    /// <summary>
    /// Determines whether the current page can be protected.
    /// </summary>
    /// <param name="enabled">
    /// Whether article-processing controls are generally enabled.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if protection should be enabled; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool CanProtectCurrentPage(bool enabled)
    {
        // Allow protection of a non-existent page (salting).
        return enabled &&
            TheSession.User.CanProtectPage(TheSession.Page) &&
            TheArticle != null;
    }

    /// <summary>
    /// Determines whether the current page can be moved.
    /// </summary>
    /// <param name="enabled">
    /// Whether article-processing controls are generally enabled.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if moving should be enabled; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool CanMoveCurrentPage(bool enabled)
    {
        return CanProtectCurrentPage(enabled) &&
            TheSession.Page.Exists;
    }

    /// <summary>
    /// Determines whether the current page can be deleted.
    /// </summary>
    /// <param name="enabled">
    /// Whether article-processing controls are generally enabled.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if deletion should be enabled; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool CanDeleteCurrentPage(bool enabled)
    {
        return enabled &&
            TheSession.User.CanDeletePage(TheSession.Page) &&
            TheArticle != null &&
            TheSession.Page.Exists;
    }

    /// <summary>
    /// Updates the Find button availability and highlights it when matches
    /// exist in the current article.
    /// </summary>
    private void UpdateFindButtonState()
    {
        btnFind.Enabled =
            txtFind.TextLength > 0;

        btnFind.BackColor =
            HasCurrentFindMatches()
                ? Color.Yellow
                : SystemColors.ButtonFace;
    }

    /// <summary>
    /// Determines whether the current Find expression matches the current article.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if matching text exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool HasCurrentFindMatches()
    {
        return btnFind.Enabled &&
            TheArticle != null &&
            txtEdit.FindAll(
                txtFind.Text,
                chkFindRegex.Checked,
                chkFindCaseSensitive.Checked,
                TheArticle.Name).Any();
    }

    #endregion

    #region Timers

    private int _restartDelay = 5, _startInSeconds = 5;

    /// <summary>
    /// Counts down to an automatic restart and starts processing when the
    /// countdown reaches zero.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void DelayedRestart(object sender, EventArgs e)
    {
        StopDelayedAutoSaveTimer();

        StatusLabelText =
            "Restarting in " +
            (_startInSeconds > 60
                ? "over a minute"
                : _startInSeconds.ToString());

        if (_startInSeconds == 0)
        {
            StopDelayedRestartTimer();
            Start();
        }
        else
        {
            _startInSeconds--;
        }
    }

    /// <summary>
    /// Increases the automatic restart delay and starts the restart timer.
    /// </summary>
    private void StartDelayedRestartTimer()
    {
        // Increase the restart delay each time. The delay is reduced by one
        // after each successful save.
        int delay = _restartDelay + 5;

        if (delay > 60)
        {
            delay = 60;
        }

        _restartDelay = delay;
        StartDelayedRestartTimer(delay);
    }

    /// <summary>
    /// Starts the delayed restart countdown using the specified delay.
    /// </summary>
    /// <param name="delay">The restart delay, in seconds.</param>
    private void StartDelayedRestartTimer(int delay)
    {
        _startInSeconds = delay;

        Ticker -= DelayedRestart;
        Ticker += DelayedRestart;
    }

    /// <summary>
    /// Stops the delayed restart countdown and resets the remaining time.
    /// </summary>
    private void StopDelayedRestartTimer()
    {
        Ticker -= DelayedRestart;
        _startInSeconds = _restartDelay;
    }

    /// <summary>
    /// Updates the bot-mode timer display.
    /// </summary>
    private void UpdateBotTimer()
    {
        lblBotTimer.Text =
            chkAutoMode.Checked
                ? $"Bot timer: {_intTimer}"
                : string.Empty;
    }

    /// <summary>
    /// Stops the delayed auto-save countdown and resets the bot timer.
    /// </summary>
    private void StopDelayedAutoSaveTimer()
    {
        Ticker -= DelayedAutoSave;
        _intTimer = 0;
    }

    /// <summary>
    /// Starts the delayed auto-save countdown.
    /// </summary>
    private void StartDelayedAutoSaveTimer()
    {
        Ticker -= DelayedAutoSave;
        Ticker += DelayedAutoSave;
    }

    int _intTimer;

    /// <summary>
    /// Advances the delayed auto-save countdown, saves the current article
    /// when the delay expires, and stops bot mode when the configured edit
    /// limit is reached.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void DelayedAutoSave(object sender, EventArgs e)
    {
        if (_intTimer < nudBotSpeed.Value)
        {
            _intTimer++;

            lblBotTimer.BackColor =
                _intTimer == 1
                    ? Color.Red
                    : DefaultBackColor;
        }
        else
        {
            StopDelayedAutoSaveTimer();
            SaveArticle();
        }

        UpdateBotTimer();

        if (botEditsStop.Value > 0 &&
            _sessionCounters.NumberOfEdits >= botEditsStop.Value)
        {
            Stop();

            StatusLabelText =
                $"Stopped: {botEditsStop.Value} edits reached";
        }
    }

    /// <summary>
    /// Updates the visibility of the moving-average timer and resets the
    /// current save interval.
    /// </summary>
    private void ShowTimer()
    {
        lblTimer.Visible = ShowMovingAverageTimer;
        StopSaveInterval();
    }

    int _intStartTimer;

    /// <summary>
    /// Advances the save interval and updates the timer display.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void SaveInterval(object sender, EventArgs e)
    {
        _intStartTimer++;
        lblTimer.Text = $"Timer: {_intStartTimer}";
    }

    /// <summary>
    /// Stops and resets the save interval timer.
    /// </summary>
    private void StopSaveInterval()
    {
        _intStartTimer = 0;
        lblTimer.Text = "Timer: 0";
        Ticker -= SaveInterval;
    }

    /// <summary>
    /// Occurs when the application's periodic timer advances.
    /// </summary>
    public event EventHandler Ticker;

    /// <summary>
    /// Raises the application ticker event and updates edit statistics once
    /// per minute.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void Timer_Tick(object sender, EventArgs e)
    {
        Ticker?.Invoke(this, EventArgs.Empty);

        _seconds++;

        if (_seconds == 60)
        {
            _seconds = 0;
            GenerateEditStatistics();
        }
    }

    int _seconds, _lastEditsTotal, _lastPagesTotal;

    /// <summary>
    /// Calculates edit and page-processing rates for the most recent reporting
    /// interval.
    /// </summary>
    private void GenerateEditStatistics()
    {
        // Edits completed during the last minute.
        _sessionCounters.NumberOfEditsPerMinute =
            _sessionCounters.NumberOfEdits - _lastEditsTotal;

        // Pages processed during the last minute. This includes edits and
        // skipped pages in normal mode, or pages parsed in pre-parse mode.
        _sessionCounters.NumberOfPagesPerMinute = Math.Max(
            _sessionCounters.NumberOfEdits +
            _sessionCounters.NumberOfIgnoredEdits +
            _sessionCounters.NumberOfPagesParsed -
            _lastPagesTotal,
            0);

        _lastEditsTotal =
            _sessionCounters.NumberOfEdits;

        _lastPagesTotal =
            _sessionCounters.NumberOfEdits +
            _sessionCounters.NumberOfIgnoredEdits +
            _sessionCounters.NumberOfPagesParsed;
    }

    #endregion

    #region menus and buttons

    /// <summary>
    /// Displays the custom module editor.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void makeModuleToolStripMenuItem_Click(object sender, EventArgs e)
    {
        _customModule.Show();
    }

    /// <summary>
    /// Displays additional skip options.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnMoreSkip_Click(object sender, EventArgs e)
    {
        _skip.ShowDialog();
    }

    /// <summary>
    /// Retrieves and displays a preview of the current article.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnPreview_Click(object sender, EventArgs e)
    {
        GetPreview();
    }

    /// <summary>
    /// Retrieves and displays a diff for the current article.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnDiff_Click(object sender, EventArgs e)
    {
        GetDiff();
    }

    /// <summary>
    /// Records the current article as a false positive.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void FalsePositiveClick(object sender, EventArgs e)
    {
        if (TheArticle != null && TheArticle.Name.Length > 0)
        {
            Tools.WriteTextFileAbsolutePath(
                "#[[" + TheArticle.Name + "]]\r\n",
                Path.Combine(ApplicationPaths.UserData, @"False positives.txt"),
                true);
        }
    }

    /// <summary>
    /// Begins processing the current article list.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnStart_Click(object sender, EventArgs e)
    {
        BeginProcess();
    }

    // TODO(Twain): Move login and processing-start validation into the shared
    // processing workflow once session and background processing services are extracted.
    /// <summary>
    /// Begins article processing after ensuring that the user is logged in
    /// and that no background process is currently running.
    /// </summary>
    private void BeginProcess()
    {
        if (!TheSession.User.IsLoggedIn)
        {
            _profiles.ShowDialog();

            if (!TheSession.User.IsLoggedIn)
            {
                return;
            }
        }
        else if (IsPageProcessingBackgroundRequestRunning())
        {
            StatusLabelText = "Background process running";
            return;
        }

        _stopProcessing = false;
        Start();
    }

    /// <summary>
    /// Stops article processing after confirming that any unsaved manual
    /// changes in the edit box may be discarded.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnStop_Click(object sender, EventArgs e)
    {
        // Ask for confirmation when the edit box contains manual changes that
        // differ from the current article text.
        if (TheArticle == null ||
            TheArticle.ArticleText.Equals(txtEdit.Text) ||
            txtEdit.Text.Length == 0 ||
            MessageBox.Show(
                "There are manual changes to the page text in the edit box, " +
                "are you sure you want to stop?",
                "Confirm stop",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            Stop();
        }
    }

    /// <summary>
    /// Saves the current article.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnSave_Click(object sender, EventArgs e)
    {
        Save();
    }

    /// <summary>
    /// Skips the current article at the user's request.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnIgnore_Click(object sender, EventArgs e)
    {
        SkipPage("user");
    }

    /// <summary>
    /// Moves the current article and then continues processing.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnMove_Click(object sender, EventArgs e)
    {
        MoveArticle();
        Start();
    }

    /// <summary>
    /// Deletes the current article and then continues processing.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnDelete_Click(object sender, EventArgs e)
    {
        DeleteArticle();
        Start();
    }

    /// <summary>
    /// Protects the current article and then continues processing.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnProtect_Click(object sender, EventArgs e)
    {
        ProtectArticle();
        Start();
    }

    /// <summary>
    /// Enables or disables automatic filtering of non-mainspace pages and,
    /// when enabled, immediately removes non-mainspace articles from the list.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void filterOutNonMainSpaceToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listMaker.FilterNonMainAuto =
            filterOutNonMainSpaceToolStripMenuItem.Checked;

        if (filterOutNonMainSpaceToolStripMenuItem.Checked)
        {
            listMaker.FilterNonMainArticles();
        }
    }

    /// <summary>
    /// Enables or disables duplicate filtering and, when enabled, immediately
    /// removes duplicate entries from the current list.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void removeDuplicatesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listMaker.FilterDuplicates =
            removeDuplicatesToolStripMenuItem.Checked;

        if (removeDuplicatesToolStripMenuItem.Checked)
        {
            listMaker.RemoveListDuplicates();
        }
    }

    /// <summary>
    /// Opens or applies the special list filter.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void specialFilterToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listMaker.Filter();
    }

    /// <summary>
    /// Converts entries in the current list to their corresponding talk pages.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void convertToTalkPagesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listMaker.ConvertToTalkPages();
    }

    /// <summary>
    /// Converts talk-page entries in the current list to their corresponding
    /// subject pages.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void convertFromTalkPagesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listMaker.ConvertFromTalkPages();
    }

    /// <summary>
    /// Enables or disables automatic alphabetical sorting and, when enabled,
    /// immediately sorts the current list.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void sortAlphabeticallyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listMaker.AutoAlpha =
            sortAlphabeticallyToolStripMenuItem.Checked;

        if (sortAlphabeticallyToolStripMenuItem.Checked)
        {
            listMaker.AlphaSortList();
        }
    }

    /// <summary>
    /// Saves the current list to a text file.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void saveListToTextFileToolStripMenuItem_Click(object sender, EventArgs e)
    {
        listMaker.SaveList();
    }

    // TODO(Twain): Replace numeric article-list preference values with a named
    // mode and move ListComparer creation behind a shared service.

    /// <summary>
    /// Opens the List Comparer and optionally initializes it with the current
    /// article list, depending on the user's preferences.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void launchListComparerToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        bool useCurrentArticleList =
            ShouldListComparerUseCurrentArticleList();

        _comparer =
            CreateListComparer(
                useCurrentArticleList);

        _comparer.Show(this);
    }

    /// <summary>
    /// Determines whether the List Comparer should use the current article list,
    /// prompting the user when the configured preference requires confirmation.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> to use the current article list; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool ShouldListComparerUseCurrentArticleList()
    {
        switch (_listComparerUseCurrentArticleList)
        {
            case 0: // Ask
                return listMaker.Any() &&
                    MessageBox.Show(
                        "Would you like to copy your current Article List to the ListComparer?",
                        "Copy Article List?",
                        MessageBoxButtons.YesNo) == DialogResult.Yes;

            case 1: // Always
                return true;

            case 2: // Never
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Creates the List Comparer configured for the requested article-list
    /// behavior.
    /// </summary>
    /// <param name="useCurrentArticleList">
    /// <see langword="true"/> to initialize the comparer with the current
    /// article list; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>The configured List Comparer.</returns>
    private ListComparer CreateListComparer(
        bool useCurrentArticleList)
    {
        if (useCurrentArticleList)
        {
            return new ListComparer(
                listMaker,
                listMaker.GetArticleList());
        }

        return new ListComparer(
            listMaker);
    }

    // TODO(Twain): Replace numeric article-list preference values with a named
    // mode and move ListSplitter creation behind a shared service.

    /// <summary>
    /// Opens the List Splitter and optionally initializes it with the current
    /// article list, depending on the user's preferences.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void launchListSplitterToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        bool useCurrentArticleList =
            ShouldListSplitterUseCurrentArticleList();

        _splitter =
            CreateListSplitter(
                useCurrentArticleList);

        _splitter.Show(this);
    }

    /// <summary>
    /// Determines whether the List Splitter should use the current article list,
    /// prompting the user when the configured preference requires confirmation.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> to use the current article list; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool ShouldListSplitterUseCurrentArticleList()
    {
        switch (_listSplitterUseCurrentArticleList)
        {
            case 0: // Ask
                return listMaker.Any() &&
                    MessageBox.Show(
                        "Would you like to copy your current Article List to the ListSplitter?",
                        "Copy Article List?",
                        MessageBoxButtons.YesNo) == DialogResult.Yes;

            case 1: // Always
                return true;

            case 2: // Never
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Creates the List Splitter configured for the requested article-list
    /// behavior.
    /// </summary>
    /// <param name="useCurrentArticleList">
    /// <see langword="true"/> to initialize the splitter with the current
    /// article list; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>The configured List Splitter.</returns>
    private ListSplitter CreateListSplitter(
        bool useCurrentArticleList)
    {
        if (useCurrentArticleList)
        {
            return new ListSplitter(
                MakePrefs(),
                listMaker.GetArticleList());
        }

        return new ListSplitter(
            MakePrefs());
    }

    /// <summary>
    /// Opens the database dump searcher.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void launchDumpSearcherToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        LaunchDumpSearcher();
    }

    // TODO(Twain): Replace numeric article-list preference values with a named
    // mode and move dump-searcher creation behind a shared service.

    /// <summary>
    /// Opens the database dump searcher and configures whether its results
    /// are added to the current article list based on the user's preferences.
    /// </summary>
    private void LaunchDumpSearcher()
    {
        bool useCurrentArticleList =
            ShouldDumpSearcherUseCurrentArticleList();

        _dataBaseScanner =
            CreateDumpSearcher(
                useCurrentArticleList);

        _dataBaseScanner.Show();
        UpdateButtons(null, null);
    }

    /// <summary>
    /// Determines whether the dump searcher should use the current article list,
    /// prompting the user when the configured preference requires confirmation.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> to use the current article list; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool ShouldDumpSearcherUseCurrentArticleList()
    {
        switch (_dbScannerUseCurrentArticleList)
        {
            case 0: // Ask
                return MessageBox.Show(
                    "Would you like the results to be added to the ListMaker Article List?",
                    "Add to ListMaker?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;

            case 1: // Always
                return true;

            case 2: // Never
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Creates the database dump searcher configured for the requested
    /// article-list behavior.
    /// </summary>
    /// <param name="useCurrentArticleList">
    /// <see langword="true"/> to create a scanner connected to the current
    /// article list; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>The configured database dump searcher.</returns>
    private Twain.Core.DBScanner.DatabaseScanner CreateDumpSearcher(
        bool useCurrentArticleList)
    {
        if (useCurrentArticleList)
        {
            return listMaker.DBScanner();
        }

        return new Twain.Core.DBScanner.DatabaseScanner();
    }

    /// <summary>
    /// Updates the parser setting that controls alphabetical sorting of
    /// interwiki links.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void alphaSortInterwikiLinksToolStripMenuItem_CheckStateChanged(
        object sender,
        EventArgs e)
    {
        _parser.SortInterwikis =
            alphaSortInterwikiLinksToolStripMenuItem.Checked;
    }

    // TODO(Twain): Extract keyboard shortcut handling into a dedicated
    // command/shortcut service so the shortcuts can be shared between the
    // WinForms and Avalonia user interfaces.
    //
    // TODO(Twain): Replace the keyboard shortcut conditional chain with a
    // command map to simplify adding and maintaining shortcuts.

    /// <summary>
    /// Handles keyboard shortcuts for common editing and processing commands.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The keyboard event data.</param>
    private void MainForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && btnStop.Enabled)
        {
            Stop();
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Modifiers != Keys.Control)
        {
            return;
        }

        if (HandleControlShortcut(e.KeyCode))
        {
            e.SuppressKeyPress = true;
        }
    }

    /// <summary>
    /// Handles supported Control-key shortcuts.
    /// </summary>
    /// <param name="keyCode">The key pressed with the Control modifier.</param>
    /// <returns>
    /// <see langword="true"/> if the shortcut was handled; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool HandleControlShortcut(Keys keyCode)
    {
        switch (keyCode)
        {
            case Keys.S:
                HandleSaveOrStartShortcut();
                return true;

            case Keys.G:
                BeginProcess();
                return true;

            case Keys.I:
                if (btnIgnore.Enabled)
                {
                    SkipPage("user");
                    return true;
                }

                return false;

            case Keys.D:
                if (btnDiff.Enabled)
                {
                    GetDiff();
                    return true;
                }

                return false;

            case Keys.N:
                if (btnPreview.Enabled)
                {
                    GetPreview();
                    return true;
                }

                return false;

            case Keys.F:
                FindCurrentArticleText();
                return true;

            case Keys.B:
                ShowAlerts();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Executes the Control+S behavior by saving the current article when
    /// possible or starting processing when saving is unavailable.
    /// </summary>
    private void HandleSaveOrStartShortcut()
    {
        if (btnSave.Enabled)
        {
            Save();
        }
        else if (btnStart.Enabled)
        {
            Start();
        }
    }

    /// <summary>
    /// Searches the current article text using the active Find settings.
    /// </summary>
    private void FindCurrentArticleText()
    {
        if (TheArticle == null)
        {
            return;
        }

        txtEdit.Find(
            txtFind.Text,
            chkFindRegex.Checked,
            chkFindCaseSensitive.Checked,
            TheArticle.Name);
    }

    /// <summary>
    /// Displays the current alerts.
    /// </summary>
    private void ShowAlerts()
    {
        // TODO(Twain): Move alert presentation behind a dedicated command or
        // service so callers do not invoke UI event handlers directly.
        lbAlerts_Click(null, null);
    }

    /// <summary>
    /// Handles keyboard input for the edit summary control, including adding
    /// new summaries and selecting all text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The keyboard event data.</param>
    private void cmbEditSummary_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter &&
            !cmboEditSummary.Items.Contains(cmboEditSummary.Text))
        {
            e.SuppressKeyPress = true;
            cmboEditSummary.Items.Add(cmboEditSummary.Text);
        }

        if (e.Modifiers == Keys.Control &&
            e.KeyCode == Keys.A)
        {
            cmboEditSummary.SelectAll();
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }

    /// <summary>
    /// Converts the selected HTML unordered list into wiki list markup.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void listToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.SelectedText =
            Tools.HTMLListToWiki(txtEdit.SelectedText, "*");
    }

    /// <summary>
    /// Converts the selected HTML ordered list into wiki list markup.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void listToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        txtEdit.SelectedText =
            Tools.HTMLListToWiki(txtEdit.SelectedText, "#");
    }

    /// <summary>
    /// Cuts the selected text to the clipboard.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void cutToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.Cut();
    }

    /// <summary>
    /// Copies the selected text to the clipboard.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void copyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.Copy();
    }

    /// <summary>
    /// Pastes plain text from the clipboard.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DataFormats.Format plainText =
            DataFormats.GetFormat(DataFormats.Text);

        txtEdit.Paste(plainText);
    }

    /// <summary>
    /// Selects all text in the editor.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.SelectAll();
    }

    /// <summary>
    /// Undoes the most recent edit.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void undoToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.Undo();
    }

    /// <summary>
    /// Inserts a human-name disambiguation template using the current
    /// article information.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void humanNameDisambigTagToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (TheArticle != null)
        {
            txtEdit.SelectedText =
                "{{Hndis|name=" +
                Tools.MakeHumanCatKey(
                    TheArticle.Name,
                    TheArticle.ArticleText) +
                "}}";
        }
    }

    /// <summary>
    /// Prepends the Wikify maintenance template to the current article.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void wikifyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.Text =
            "{{Wikify|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}\r\n\r\n"
            + txtEdit.Text;
    }

    /// <summary>
    /// Prepends the Cleanup maintenance template to the current article.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void cleanupToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.Text =
            "{{cleanup|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}\r\n\r\n"
            + txtEdit.Text;
    }

    // TODO(Twain): Move speedy deletion template generation and user-facing
    // text into wiki-specific configuration so non-Wikipedia wikis can define
    // their own deletion workflow.
    /// <summary>
    /// Prompts the user for a speedy deletion reason and prepends the
    /// corresponding deletion template to the current article text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void speedyDeleteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Rectangle scrn = Screen.GetWorkingArea(this);

        var res = Twain.Core.Controls.InputBox.Show(
            "Enter a reason. Leave blank if you'll edit the reason in the AWB text box",
            "Speedy deletion",
            "",
            null,
            scrn.Width / 2,
            scrn.Height / 3);

        if (res.OK)
        {
            txtEdit.Text =
                "{{db|" + res.Text.Trim() + "}}\r\n\r\n" +
                txtEdit.Text;
        }
    }

    /// <summary>
    /// Inserts the <c>{{subst:clear}}</c> template at the current selection.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void clearToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.SelectedText = "{{subst:clear}}";
    }

    /// <summary>
    /// Inserts the standard disambiguation template at the current selection.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void disambiguationToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.SelectedText = "{{Disambiguation}}";
    }

    /// <summary>
    /// Inserts the Uncategorized maintenance template with the current month
    /// and year at the current selection.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void uncategorisedToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.SelectedText =
            "{{Uncategorized|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}";
    }

    // TODO(Twain): Replace the blocking redirect-processing workflow with an
    // asynchronous operation that does not block the UI thread.

    /// <summary>
    /// Replaces links to redirects in the current article with direct links
    /// after confirming the operation with the user.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void bypassAllRedirectsToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (MessageBox.Show(
            "Replacement of links to redirects with direct links is strongly discouraged, " +
            "however it could be useful in some circumstances. Are you sure you want to continue?",
            "Bypass redirects",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        BackgroundRequest request = new();

        Enabled = false;

        try
        {
            request.BypassRedirects(
                txtEdit.Text,
                TheSession.Editor.SynchronousEditor.Clone());

            request.Wait();

            txtEdit.Text = (string)request.Result;
        }
        finally
        {
            Enabled = true;
        }
    }

    /// <summary>
    /// Converts supported character entities in the selected text to their
    /// Unicode equivalents.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void unicodifyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        string text = txtEdit.SelectedText;
        text = _parser.Unicodify(text);
        txtEdit.SelectedText = text;
    }

    /// <summary>
    /// Inserts a DEFAULTSORT value based on the current article's human-name
    /// category key.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void humanNameCategoryKeyToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (TheArticle != null)
        {
            txtEdit.SelectedText =
                "{{DEFAULTSORT:" +
                Tools.MakeHumanCatKey(
                    TheArticle.Name,
                    TheArticle.ArticleText) +
                "}}";
        }
    }

    /// <summary>
    /// Matches four-digit years beginning with 1 or 2.
    /// </summary>
    private readonly Regex RegexDates =
        new("[12][0-9]{3}", RegexOptions.Compiled);

    // TODO(Twain): Move birth/death category detection and generation into
    // shared article metadata processing once this workflow leaves MainForm.
    //
    // TODO: Improve birth/death year detection so life dates are distinguished
    // from unrelated years appearing in article text.

    /// <summary>
    /// Generates birth and death categories from the article text and inserts
    /// them at the current selection.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void birthdeathCatsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (TheArticle == null)
            return;

        try
        {
            string articleTextLocal =
                PrepareBirthDeathCategoryText(
                    txtEdit.Text);

            MatchCollection m =
                RegexDates.Matches(articleTextLocal);

            if (m.Count == 0)
            {
                MessageBox.Show(
                    "No four-digit year was found in the article text.",
                    "Birth and death categories",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            string name =
                Tools.MakeHumanCatKey(
                    TheArticle.Name,
                    TheArticle.ArticleText);

            string categories =
                BuildBirthDeathCategories(
                    m,
                    name);

            txtEdit.SelectedText = categories;

            bool noChange;

            txtEdit.Text =
                Parsers.ChangeToDefaultSort(
                    txtEdit.Text,
                    TheArticle.Name,
                    out noChange,
                    restrictDefaultsortChangesToolStripMenuItem.Checked);

            // Sort metadata when DEFAULTSORT was added to ensure correct placement.
            if (!noChange)
            {
                txtEdit.Text =
                    _parser.SortMetaData(
                        txtEdit.Text,
                        TheArticle.Name);
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Prepares article text for birth and death year detection by removing
    /// content whose dates should not be considered.
    /// </summary>
    /// <param name="articleText">The article text to process.</param>
    /// <returns>
    /// Article text with file links and dated maintenance templates excluded
    /// from year detection.
    /// </returns>
    private static string PrepareBirthDeathCategoryText(
        string articleText)
    {
        // Ignore dates in file captions and related file-link content.
        articleText =
            Tools.ReplaceWithSpaces(
                articleText,
                WikiRegexes.FileNamespaceLink.Matches(articleText));

        articleText =
            RemoveDatedTemplates(
                articleText,
                WikiRegexes.NestedTemplates);

        articleText =
            RemoveDatedTemplates(
                articleText,
                WikiRegexes.TemplateMultiline);

        return articleText;
    }

    /// <summary>
    /// Removes templates containing a date parameter from the supplied text.
    /// </summary>
    /// <param name="articleText">The article text to process.</param>
    /// <param name="templateRegex">
    /// The regular expression used to identify candidate templates.
    /// </param>
    /// <returns>
    /// The article text with dated templates removed.
    /// </returns>
    private static string RemoveDatedTemplates(
        string articleText,
        Regex templateRegex)
    {
        foreach (Match m2 in templateRegex.Matches(articleText))
        {
            if (Tools.GetTemplateParameterValue(
                m2.Value,
                "date").Length > 0)
            {
                articleText =
                    articleText.Replace(
                        m2.Value,
                        string.Empty);
            }
        }

        return articleText;
    }

    /// <summary>
    /// Builds birth and, when appropriate, death category markup from the
    /// detected article years.
    /// </summary>
    /// <param name="matches">
    /// The detected four-digit year matches.
    /// </param>
    /// <param name="name">
    /// The category sort key used for the generated categories.
    /// </param>
    /// <returns>The generated category markup.</returns>
    private static string BuildBirthDeathCategories(
        MatchCollection matches,
        string name)
    {
        string births = string.Empty;
        string deaths = string.Empty;

        if (matches.Count >= 1)
            births = matches[0].Value;

        if (matches.Count >= 2)
            deaths = matches[1].Value;

        if (string.IsNullOrEmpty(deaths) ||
            int.Parse(deaths) < int.Parse(births) + 20)
        {
            return
                "[[Category:" +
                births +
                " births|" +
                name +
                "]]";
        }

        return
            "[[Category:" +
            births +
            " births|" +
            name +
            "]]\r\n[[Category:" +
            deaths +
            " deaths|" +
            name +
            "]]";
    }

    /// <summary>
    /// Inserts the configured stub text at the current editor selection.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void stubToolStripMenuItem_Click(object sender, EventArgs e)
    {
        txtEdit.SelectedText = toolStripTextBox1.Text;
    }

    /// <summary>
    /// Updates edit-box context menu commands before the menu is displayed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// A <see cref="CancelEventArgs"/> that can be used to cancel the menu opening.
    /// </param>
    private void mnuTextBox_Opening(object sender, CancelEventArgs e)
    {
        txtEdit.Focus();

        cutToolStripMenuItem.Enabled =
            copyToolStripMenuItem.Enabled =
            openSelectionInBrowserToolStripMenuItem.Enabled =
            !string.IsNullOrEmpty(txtEdit.SelectedText);

        undoToolStripMenuItem.Enabled = txtEdit.CanUndo;

        openPageInBrowserToolStripMenuItem.Enabled =
            openHistoryMenuItem.Enabled =
            openTalkPageInBrowserToolStripMenuItem.Enabled =
            TheArticle != null &&
            !string.IsNullOrEmpty(TheArticle.Name);

        replaceTextWithLastEditToolStripMenuItem.Enabled =
            !string.IsNullOrEmpty(_lastArticle);
    }

    /// <summary>
    /// Opens the current article in the default web browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void openPageInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (TheArticle == null)
        {
            return;
        }

        TheSession.Site.OpenPageInBrowser(TheArticle.Name);
    }

    /// <summary>
    /// Opens the talk page associated with the current article in the default
    /// web browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void openTalkPageInBrowserToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (TheArticle == null)
        {
            return;
        }

        TheSession.Site.OpenPageInBrowser(
            Tools.ConvertToTalk(TheArticle));
    }

    /// <summary>
    /// Opens the revision history of the current article in the default
    /// web browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void openHistoryMenuItem_Click(object sender, EventArgs e)
    {
        if (TheArticle == null)
        {
            return;
        }

        TheSession.Site.OpenPageHistoryInBrowser(TheArticle.Name);
    }

    // TODO(Twain): Move URL-versus-wiki-page resolution into shared navigation
    // logic so browser commands use consistent destination handling.
    /// <summary>
    /// Opens the selected editor text as either a URL or a wiki page in the
    /// default web browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void openSelectionInBrowserToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser#Open_text_selection_in_browser
        // User feedback indicates that invalid HTTP links should still be
        // treated as URLs rather than opened as wiki pages.
        string selectedText = txtEdit.SelectedText.Trim();

        if (ShouldOpenSelectionAsUrl(selectedText))
        {
            Tools.OpenURLInBrowser(selectedText);
        }
        else
        {
            TheSession.Site.OpenPageInBrowser(txtEdit.SelectedText);
        }
    }

    /// <summary>
    /// Determines whether the selected text should be opened as a URL rather
    /// than interpreted as a wiki page title.
    /// </summary>
    /// <param name="selectedText">The selected text to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the text should be treated as a URL;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool ShouldOpenSelectionAsUrl(string selectedText)
    {
        return WikiRegexes.UrlValidator.IsMatch(selectedText) ||
            selectedText.StartsWith(
                "http",
                StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Updates general-fix related options when general parsing is enabled
    /// or disabled.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void chkGeneralParse_CheckedChanged(object sender, EventArgs e)
    {
        alphaSortInterwikiLinksToolStripMenuItem.Enabled =
            chkSkipGeneralFixes.Enabled =
            chkSkipMinorGeneralFixes.Enabled =
            chkGeneralFixes.Checked;

        if (chkSkipGeneralFixes.Checked)
        {
            chkSkipMinorGeneralFixes.Enabled = false;
        }
    }

    /// <summary>
    /// Shows or hides the advanced replace window.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnFindAndReplaceAdvanced_Click(object sender, EventArgs e)
    {
        if (!_replaceSpecial.Visible)
        {
            _replaceSpecial.Show(ntfyTray.Text + " – Replace Special");
        }
        else
        {
            _replaceSpecial.Hide();
        }
    }

    /// <summary>
    /// Opens the additional find-and-replace options dialog.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnMoreFindAndReplce_Click(object sender, EventArgs e)
    {
        _findAndReplace.ShowDialog(this);
    }

    // TODO: Investigate cases where processing does not stop reliably,
    // including background work, editor operations, and timer-driven workflows.
    //
    // TODO(Twain): Replace legacy abort-based cancellation with a coordinated,
    // cancellation-aware processing shutdown once background services are extracted.

    /// <summary>
    /// Stops the current processing workflow, cancels active background work,
    /// resets timers, and updates the user interface to the stopped state.
    /// </summary>
    private void Stop()
    {
        ResetProcessingStopState();
        StopBackgroundProcessing();

        DisableButtons();

        if (_intTimer > 0)
        {
            StopDelayedAutoSaveTimer();
            EnableButtons();
            return;
        }

        StopProcessingTimers();
        StopActiveProcessing();

        FinishStoppedState();
    }

    /// <summary>
    /// Resets processing state used when stopping the current workflow.
    /// </summary>
    private void ResetProcessingStopState()
    {
        _retries = 0;
        _stopProcessing = true;
        _pageReload = false;

        NudgeTimer.Stop();
    }

    /// <summary>
    /// Aborts the active page-processing background request when one exists.
    /// </summary>
    private void StopBackgroundProcessing()
    {
        if (_runProcessPageBackground != null)
        {
            _runProcessPageBackground.Abort();
        }
    }

    /// <summary>
    /// Stops timers associated with save and restart processing.
    /// </summary>
    private void StopProcessingTimers()
    {
        StopSaveInterval();
        StopDelayedRestartTimer();

        if (_autoSaveEditBoxEnabled)
        {
            EditBoxSaveTimer.Enabled = false;
        }
    }

    /// <summary>
    /// Stops active editor and article-list processing.
    /// </summary>
    private void StopActiveProcessing()
    {
        TheSession.Editor.Abort();
        listMaker.Stop();
    }

    /// <summary>
    /// Updates the application to its final stopped state.
    /// </summary>
    private void FinishStoppedState()
    {
        StatusLabelText = "Stopped";
        ClearBrowser();
        UpdateButtons(null, null);
    }

    /// <summary>
    /// Opens the Twain user manual in the default web browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        Tools.OpenENArticleInBrowser(
            "Wikipedia:AutoWikiBrowser/User manual",
            false);
    }

    #region Edit Box Menu

    /// <summary>
    /// Reparses the current article text and refreshes the edit box state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void reparseToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ReparseEditBox();
    }

    // TODO(Twain): Replace the legacy BackgroundRequest/event-based reparse
    // workflow with an asynchronous, cancellation-aware processing service.

    /// <summary>
    /// Reparses the current edit box contents by preparing the current article
    /// and starting page processing in a background request.
    /// </summary>
    /// <remarks>
    /// User changes are copied from the edit box into the current article before
    /// background processing begins. Remaining processing is performed when the
    /// background request raises its completion event.
    /// </remarks>
    private void ReparseEditBox()
    {
        if (TheArticle == null)
        {
            return;
        }

        if (IsPageProcessingBackgroundRequestRunning())
        {
            StatusLabelText = "Background process running";
            return;
        }

        StatusLabelText = "Processing page";
        StartProgressBar();

        // Refresh the article text to include any manual changes from the editor.
        TheArticle.AWBChangeArticleText(
            "Reparse",
            txtEdit.Text,
            false);

        StartReparseBackgroundRequest();
    }

    /// <summary>
    /// Determines whether the page-processing background request is currently
    /// running.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the background request is running or marked as
    /// a background thread; otherwise, <see langword="false"/>.
    /// </returns>
    private bool IsPageProcessingBackgroundRequestRunning()
    {
        if (_runProcessPageBackground == null)
        {
            return false;
        }

        ThreadState threadState =
            _runProcessPageBackground.ThreadStatus();

        return threadState == ThreadState.Running ||
            threadState == ThreadState.Background;
    }

    /// <summary>
    /// Creates and starts the background request used to complete edit-box
    /// reparsing.
    /// </summary>
    private void StartReparseBackgroundRequest()
    {
        _runProcessPageBackground = new BackgroundRequest();
        _runProcessPageBackground.Complete += ReparseEditBoxComplete;
        _runProcessPageBackground.Execute(ReparseEditBoxBackground);
    }

    /// <summary>
    /// Runs ProcessPage and UpdateCurrentTypoStats as background jobs for reparse edit box
    /// </summary>
    private void ReparseEditBoxBackground()
    {
        ProcessPage(TheArticle, false);

        ErrorHandler.CurrentPage = string.Empty;
    }

    /// <summary>
    /// Completes edit-box reparsing by updating article statistics, refreshing
    /// editor content and highlighting, generating the diff, and restoring the
    /// editor to its ready state.
    /// </summary>
    private void ReparseEditBoxPart2()
    {
        UpdateCurrentTypoStats();
        ArticleInfo(false);

        RefreshReparsedEditBox();

        GetDiff();

        RestoreEditBoxFocus();

        StopProgressBar();
        StatusLabelText = "Ready to save";
    }

    // TODO(Twain): Move editor highlighting and selection restoration behind
    // the editor abstraction so reparse completion does not depend directly
    // on WinForms editor controls.
    /// <summary>
    /// Refreshes the editor with the reparsed article text and reapplies
    /// configured find, alert, and syntax highlighting.
    /// </summary>
    private void RefreshReparsedEditBox()
    {
        txtEdit.Text = TheArticle.ArticleText;
        txtEdit.Visible = false;

        // Clear highlighting from previous alerts before applying the current
        // highlighting rules.
        txtEdit.SelectAll();
        txtEdit.SelectionBackColor = Color.White;

        if (highlightAllFindToolStripMenuItem.Checked)
        {
            HighlightAllFind();
        }

        _errors.Clear();

        if (scrollToAlertsToolStripMenuItem.Checked)
        {
            HighlightErrors();
        }

        if (syntaxHighlightEditBoxToolStripMenuItem.Checked)
        {
            HighlightSyntax();
        }

        txtEdit.Visible = true;
    }

    /// <summary>
    /// Restores the editor selection, scroll position, and Save button focus
    /// after reparsing completes.
    /// </summary>
    private void RestoreEditBoxFocus()
    {
        if (!focusAtEndOfEditTextBoxToolStripMenuItem.Checked)
        {
            txtEdit.SetEditBoxSelection(0, 0);
            txtEdit.Select(0, 0);
        }
        else
        {
            txtEdit.Select(
                txtEdit.Text.Length,
                0);
        }

        txtEdit.ScrollToCaret();
        btnSave.Select();
    }

    /// <summary>
    /// Handles completion of the edit-box reparse operation and updates the
    /// remaining alerts and page state on the UI thread.
    /// </summary>
    /// <param name="req">
    /// The completed background request.
    /// </param>
    private void ReparseEditBoxComplete(
        BackgroundRequest req)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(
                new System.Windows.Forms.MethodInvoker(
                    ReparseEditBoxPart2));

            return;
        }

        ReparseEditBoxPart2();
    }

    /// <summary>
    /// Replaces the current editor contents with the previously stored article
    /// text and optionally refreshes the diff view.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void replaceTextWithLastEditToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (_lastArticle.Length > 0)
        {
            txtEdit.Text = _lastArticle;

            if (_actionOnLoad == 0)
            {
                GetDiff();
            }
        }
    }

    #region PasteMore

    /// <summary>
    /// Inserts the text stored in the selected Paste More menu item at the
    /// current editor selection and closes the context menu.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void PasteMore_Click(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item &&
            item.Tag is string text)
        {
            txtEdit.SelectedText = text;
        }

        mnuTextBox.Hide();
    }

    // TODO(Twain): Replace the fixed ten-item Paste More model with a collection-
    // based configuration model so the UI is not tied to individually named slots.

    /// <summary>
    /// Opens the Paste More configuration dialog and applies any accepted
    /// text changes to the configured Paste More menu items.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void configureToolStripMenuItem_Click(object sender, EventArgs e)
    {
        string[] pasteMoreItems =
            GetPasteMoreTexts();

        using ConfigurePasteMoreItems dialog = new(
            pasteMoreItems[0],
            pasteMoreItems[1],
            pasteMoreItems[2],
            pasteMoreItems[3],
            pasteMoreItems[4],
            pasteMoreItems[5],
            pasteMoreItems[6],
            pasteMoreItems[7],
            pasteMoreItems[8],
            pasteMoreItems[9]);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ApplyPasteMoreTexts(
            GetPasteMoreDialogTexts(dialog));
    }

    /// <summary>
    /// Gets the currently configured Paste More text values.
    /// </summary>
    /// <returns>The configured Paste More text values.</returns>
    private string[] GetPasteMoreTexts()
    {
        return
        [
            (string)PasteMore1.Tag,
        (string)PasteMore2.Tag,
        (string)PasteMore3.Tag,
        (string)PasteMore4.Tag,
        (string)PasteMore5.Tag,
        (string)PasteMore6.Tag,
        (string)PasteMore7.Tag,
        (string)PasteMore8.Tag,
        (string)PasteMore9.Tag,
        (string)PasteMore10.Tag
        ];
    }

    /// <summary>
    /// Gets the Paste More text values entered in the configuration dialog.
    /// </summary>
    /// <param name="dialog">The Paste More configuration dialog.</param>
    /// <returns>The configured text values from the dialog.</returns>
    private static string[] GetPasteMoreDialogTexts(
        ConfigurePasteMoreItems dialog)
    {
        return
        [
            dialog.String1,
        dialog.String2,
        dialog.String3,
        dialog.String4,
        dialog.String5,
        dialog.String6,
        dialog.String7,
        dialog.String8,
        dialog.String9,
        dialog.String10
        ];
    }

    /// <summary>
    /// Applies the supplied Paste More text values to the configured menu items.
    /// </summary>
    /// <param name="pasteMoreItems">The Paste More text values to apply.</param>
    private void ApplyPasteMoreTexts(
        string[] pasteMoreItems)
    {
        for (int i = 0; i < pasteMoreItems.Length; i++)
        {
            SetPasteMoreText(
                i,
                pasteMoreItems[i]);
        }
    }
    #endregion

    // TODO: Move article text transformation workflows into a dedicated
    // formatting service as part of the Twain.Core modernization.
    /// <summary>
    /// Removes excess whitespace from the current article while preserving
    /// protected regions that should not be modified.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void removeAllExcessWhitespaceToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        string text = _removeText.Hide(txtEdit.Text);

        text = Parsers.RemoveAllWhiteSpace(text);

        txtEdit.Text = _removeText.AddBack(text);
    }
    #endregion

    /// <summary>
    /// Selects all text in the primary new-category text box.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void txtNewCategory_DoubleClick(object sender, EventArgs e)
    {
        txtNewCategory.SelectAll();
    }

    /// <summary>
    /// Selects all text in the secondary new-category text box.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void txtNewCategory2_DoubleClick(object sender, EventArgs e)
    {
        txtNewCategory2.SelectAll();
    }

    /// <summary>
    /// Updates the edit summary tooltip based on the current article and
    /// review edit summary.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event data.</param>
    private void cmboEditSummary_MouseMove(object sender, MouseEventArgs e)
    {
        if (TheArticle != null && string.IsNullOrEmpty(TheArticle.EditSummary))
        {
            ToolTip.SetToolTip(cmboEditSummary, "");
        }
        else
        {
            ToolTip.SetToolTip(cmboEditSummary, txtReviewEditSummary.Text);
        }
    }

    /// <summary>
    /// Refreshes the editable edit summary when the default edit summary
    /// changes and the review summary is currently enabled.
    /// </summary>
    /// <remarks>
    /// Any custom changes made to the editable edit summary are replaced when
    /// the default summary changes.
    /// </remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void cmboEditSummary_TextChanged(object sender, EventArgs e)
    {
        if (txtReviewEditSummary.Enabled)
        {
            txtReviewEditSummary.Text = MakeDefaultEditSummary();
        }
    }

    /// <summary>
    /// Updates the available command buttons when the selected tab changes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateButtons(null, null);
    }

    bool _loadingTypos;

    /// <summary>
    /// Enables or disables regular expression typo fixing and loads typo rules
    /// when the feature is enabled.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void chkRegExTypo_CheckedChanged(object sender, EventArgs e)
    {
        if (_loadingTypos)
        {
            return;
        }

        if (!chkRegExTypo.Checked)
        {
            chkSkipIfNoRegexTypo.Checked =
                chkSkipIfNoRegexTypo.Enabled =
                false;

            return;
        }

        chkSkipIfNoRegexTypo.Enabled = true;

        if (chkRegExTypo.Checked && BotMode)
        {
            MessageBox.Show(
                "RegExTypoFix cannot be used with bot mode on.\r\n" +
                "Bot mode will now be turned off, and typos loaded.",
                "Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);

            BotMode = false;
        }

        LoadTypos(false);
    }

    /// <summary>
    /// Loads or reloads the regular-expression typo rules when typo fixing is
    /// enabled.
    /// </summary>
    /// <param name="reload">
    /// <see langword="true"/> to discard the currently loaded typo rules and
    /// force them to be reloaded; otherwise, <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// When the user is not logged in, the method attempts to determine a
    /// configured typo-list location before falling back to the existing
    /// default location.
    /// </remarks>
    public void LoadTypos(bool reload)
    {
        // During a settings change, LoadTypos may be called more than once:
        // first from LoadPrefs/SetProject to indicate that typo rules must be
        // reloaded, and again after typo fixing has been enabled.
        //
        // Clear RegexTypos whenever reload is requested, even if typo fixing is
        // not currently enabled. This also supports callers of SetProject outside
        // the normal LoadPrefs workflow.
        if (reload)
        {
            _regexTypos = null;
        }

        if (chkRegExTypo.Checked && _regexTypos == null)
        {
            _loadingTypos = true;
            chkRegExTypo.Checked = false;

            StatusLabelText = "Loading typos";

            ResolveConfiguredTypoListLocation();

            string message =
                BuildTypoLoadingWarningMessage(
                    Variables.RetfPath);

            MessageBox.Show(
                message,
                "Attention",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            _regexTypos = new RegExTypoFix();
            _regexTypos.Complete += RegexTyposComplete;
        }
    }

    /// <summary>
    /// Attempts to resolve a custom typo-list location from the current wiki
    /// configuration when the default location may need to be overridden.
    /// </summary>
    private void ResolveConfiguredTypoListLocation()
    {
        if (TheSession.User.IsLoggedIn ||
            Variables.IsWikipediaEN ||
            !Variables.RetfPath.EndsWith("AutoWikiBrowser/Typos"))
        {
            return;
        }

        try
        {
            // TODO: Verify whether ConfigJSONText should take precedence over
            // retrieving the current configuration from ConfigUrl.
            if (!string.IsNullOrEmpty(TheSession.ConfigJSONText))
            {
                Session.TypoLink(
                    Tools.GetJObjectFromText(
                        TheSession.ConfigJSONText));
            }
            else
            {
                Session.TypoLink(
                    Tools.GetJObjectFromUrl(
                        Session.ConfigUrl));
            }
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                "LoadTypos",
                "Unable to load the configured typo-list location: " +
                ex.Message);

            // TODO: Determine whether a failed custom typo-list lookup should
            // explicitly restore Project:AutoWikiBrowser/Typos as the fallback
            // location.
        }
    }

    // TODO(Twain): Move typo-source resolution and rule loading into the shared
    // typo/language service so MainForm only coordinates the UI workflow.
    /// <summary>
    /// Builds the warning displayed before regular-expression typo rules are
    /// downloaded.
    /// </summary>
    /// <param name="typoListPath">
    /// The configured typo-list page or URL.
    /// </param>
    /// <returns>The warning message to display.</returns>
    private static string BuildTypoLoadingWarningMessage(
        string typoListPath)
    {
        string message =
            "Check each edit before you make it. Although this has been " +
            "built to be very accurate there will be errors.";

        string s = typoListPath;

        bool isHttpUrl =
            s.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase);

        if (!isHttpUrl)
        {
            // TODO(Twain): Replace this with a wiki-aware URL builder when
            // navigation services are extracted from MainForm.
            s = Variables.NonPrettifiedURL(s);
        }

        return message +
            "\r\n\r\nThe newest typos will now be downloaded from " +
            s +
            " when you press OK.";
    }

    // TODO(Twain): Replace the BackgroundRequest callback and WinForms
    // InvokeRequired/BeginInvoke flow with an async completion path that
    // returns the typo-loading result explicitly.

    /// <summary>
    /// Completes regular-expression typo loading and applies the resulting
    /// typo state to the user interface.
    /// </summary>
    /// <param name="req">
    /// The completed background request.
    /// </param>
    private void RegexTyposComplete(BackgroundRequest req)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(
                new BackgroundRequestComplete(RegexTyposComplete),
                req);

            return;
        }

        ApplyRegexTypoLoadResult();
    }

    // TODO(Twain): Replace direct control updates with a typo-load result model
    // when typo loading is moved behind the shared diagnostics/language service.
    /// <summary>
    /// Applies the completed typo-loading state to the related controls,
    /// statistics, and cached typo data.
    /// </summary>
    private void ApplyRegexTypoLoadResult()
    {
        chkRegExTypo.Checked =
            chkSkipIfNoRegexTypo.Enabled =
                _regexTypos.TyposLoaded;

        if (_regexTypos.TyposLoaded)
        {
            StatusLabelText =
                _regexTypos.TypoCount + " typos loaded";

            if (!EditBoxTab.TabPages.Contains(tpTypos))
            {
                EditBoxTab.TabPages.Add(tpTypos);
            }

            ResetTypoStats();
        }
        else
        {
            _regexTypos = null;

            if (EditBoxTab.TabPages.Contains(tpTypos))
            {
                EditBoxTab.TabPages.Remove(tpTypos);
            }
        }

        _loadingTypos = false;
    }

    /// <summary>
    /// Opens the Twain typo documentation page in the default
    /// web browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The link-click event data.</param>
    private void ProfileToLoad_LinkClicked(
        object sender,
        LinkLabelLinkClickedEventArgs e)
    {
        Tools.OpenENArticleInBrowser(
            "Wikipedia:AutoWikiBrowser/Typos",
            false);
    }

    // TODO(Twain): Move edit-summary normalization and persistence into shared
    // settings logic so MainForm only manages the summary editor UI.
    /// <summary>
    /// Opens the edit summary editor, applies accepted summary changes,
    /// and restores the previously selected summary when possible.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void summariesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using SummaryEditor se = new();

        string[] summaries = new string[cmboEditSummary.Items.Count];
        cmboEditSummary.Items.CopyTo(summaries, 0);

        se.Summaries.Lines = summaries;
        se.Summaries.Select(0, 0);

        string prevSummary = cmboEditSummary.SelectedText;

        if (se.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ApplyEditSummaries(
            se.Summaries.Lines,
            prevSummary);
    }

    /// <summary>
    /// Applies the supplied edit summaries to the summary control and restores
    /// the previous selection when it remains available.
    /// </summary>
    /// <param name="summaries">The edit summaries to apply.</param>
    /// <param name="prevSummary">The previously selected edit summary.</param>
    private void ApplyEditSummaries(
        IEnumerable<string> summaries,
        string prevSummary)
    {
        cmboEditSummary.Items.Clear();

        foreach (string s in NormalizeEditSummaries(summaries))
        {
            cmboEditSummary.Items.Add(s);
        }

        if (cmboEditSummary.Items.Contains(prevSummary))
        {
            cmboEditSummary.SelectedText = prevSummary;
        }
        else if (cmboEditSummary.Items.Count > 0)
        {
            cmboEditSummary.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Removes blank edit summaries and trims surrounding whitespace.
    /// </summary>
    /// <param name="summaries">The edit summaries to normalize.</param>
    /// <returns>The normalized edit summaries.</returns>
    private static IEnumerable<string> NormalizeEditSummaries(
        IEnumerable<string> summaries)
    {
        foreach (string s in summaries)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                yield return s.Trim();
            }
        }
    }

    /// <summary>
    /// Toggles the visibility of the associated panel.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void showHidePanelToolStripMenuItem_Click(object sender, EventArgs e)
    {
        PanelShowHide();
    }

    /// <summary>
    /// Toggles the parameter controls to enlarge or restore the edit area.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void enlargeEditAreaToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ParametersShowHide();
    }

    #endregion

    #region tool bar stuff

    /// <summary>
    /// Shows or hides the associated panel.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnShowHide_Click(object sender, EventArgs e)
    {
        PanelShowHide();
    }

    /// <summary>
    /// Shows or hides the parameter controls.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btntsShowHideParameters_Click(object sender, EventArgs e)
    {
        ParametersShowHide();
    }

    /// <summary>
    /// Begins processing the current article list.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btntsStart_Click(object sender, EventArgs e)
    {
        BeginProcess();
    }

    /// <summary>
    /// Saves the current article.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btntsSave_Click(object sender, EventArgs e)
    {
        Save();
    }

    /// <summary>
    /// Skips the current article at the user's request.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btntsIgnore_Click(object sender, EventArgs e)
    {
        SkipPage("user");
    }

    /// <summary>
    /// Stops the current processing workflow.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btntsStop_Click(object sender, EventArgs e)
    {
        Stop();
    }

    /// <summary>
    /// Retrieves and displays a preview of the current article.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btntsPreview_Click(object sender, EventArgs e)
    {
        GetPreview();
    }

    /// <summary>
    /// Retrieves and displays the changes made to the current article.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btntsChanges_Click(object sender, EventArgs e)
    {
        GetDiff();
    }

    // TODO(Twain): Replace manual browser sizing calculations with layout-managed
    // resizing when the browser panel is migrated from WinForms.

    /// <summary>
    /// Updates the embedded browser size to reflect the current toolbar and
    /// panel visibility.
    /// </summary>
    private void SetBrowserSize()
    {
        GetBrowserLayout(
            toolStrip.Visible,
            panel1.Visible,
            panel1.Location.Y,
            StatusMain.Location.Y,
            out int top,
            out int height);

        webBrowser.Location =
            new Point(
                webBrowser.Location.X,
                top);

        webBrowser.Height = height;
    }

    /// <summary>
    /// Calculates the embedded browser position and height from the current
    /// toolbar and panel visibility state.
    /// </summary>
    /// <param name="toolBarVisible">
    /// Whether the main toolbar is visible.
    /// </param>
    /// <param name="panelVisible">
    /// Whether the lower panel is visible.
    /// </param>
    /// <param name="panelTop">
    /// The vertical position of the lower panel.
    /// </param>
    /// <param name="statusTop">
    /// The vertical position of the main status bar.
    /// </param>
    /// <param name="top">
    /// The calculated vertical position of the embedded browser.
    /// </param>
    /// <param name="height">
    /// The calculated height of the embedded browser.
    /// </param>
    private static void GetBrowserLayout(
        bool toolBarVisible,
        bool panelVisible,
        int panelTop,
        int statusTop,
        out int top,
        out int height)
    {
        top = toolBarVisible ? 48 : 25;

        height =
            (panelVisible ? panelTop : statusTop) -
            top;
    }

    /// <summary>
    /// Enables or disables the main toolbar.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void enableTheToolbarToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        EnableToolBar = enableTheToolbarToolStripMenuItem.Checked;
    }

    /// <summary>
    /// Gets or sets whether the main toolbar is visible.
    /// </summary>
    private bool EnableToolBar
    {
        get
        {
            return toolStrip.Visible;
        }

        set
        {
            toolStrip.Visible =
                enableTheToolbarToolStripMenuItem.Checked =
                value;

            SetBrowserSize();
        }
    }

    #endregion

    #region Images

    // TODO: Replace image-operation index values with named modes during the
    // next cleanup pass.

    /// <summary>
    /// Updates image replacement controls based on the selected image operation.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void cmboImages_SelectedIndexChanged(object sender, EventArgs e)
    {
        GetImageOperationState(
            cmboImages.SelectedIndex,
            out string labelText,
            out bool imageWithEnabled,
            out bool imageReplaceEnabled,
            out bool skipNoImageChangeEnabled);

        lblImageWith.Text = labelText;
        txtImageWith.Enabled = imageWithEnabled;
        txtImageReplace.Enabled = imageReplaceEnabled;
        chkSkipNoImgChange.Enabled = skipNoImageChangeEnabled;
    }

    /// <summary>
    /// Determines the control state associated with the specified image operation.
    /// </summary>
    /// <param name="selectedIndex">
    /// The selected image-operation index.
    /// </param>
    /// <param name="labelText">
    /// The label text associated with the selected operation.
    /// </param>
    /// <param name="imageWithEnabled">
    /// Whether the replacement image text box should be enabled.
    /// </param>
    /// <param name="imageReplaceEnabled">
    /// Whether the image-to-replace text box should be enabled.
    /// </param>
    /// <param name="skipNoImageChangeEnabled">
    /// Whether the skip-if-no-image-change option should be enabled.
    /// </param>
    private static void GetImageOperationState(
        int selectedIndex,
        out string labelText,
        out bool imageWithEnabled,
        out bool imageReplaceEnabled,
        out bool skipNoImageChangeEnabled)
    {
        switch (selectedIndex)
        {
            case 0:
                labelText = string.Empty;
                imageWithEnabled = false;
                imageReplaceEnabled = false;
                skipNoImageChangeEnabled = false;
                break;

            case 1:
                labelText =
                    "&With " + Variables.Namespaces[Namespace.File];

                imageWithEnabled = true;
                imageReplaceEnabled = true;
                skipNoImageChangeEnabled = true;
                break;

            case 2:
                labelText = string.Empty;
                imageWithEnabled = false;
                imageReplaceEnabled = true;
                skipNoImageChangeEnabled = true;
                break;

            case 3:
                labelText = "Comment:";
                imageWithEnabled = true;
                imageReplaceEnabled = true;
                skipNoImageChangeEnabled = true;
                break;

            default:
                labelText = string.Empty;
                imageWithEnabled = false;
                imageReplaceEnabled = false;
                skipNoImageChangeEnabled = false;
                break;
        }
    }

    /// <summary>
    /// Removes a leading file namespace prefix from the image replacement text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void txtImageReplace_Leave(object sender, EventArgs e)
    {
        string fileNamespace =
            Regex.Escape(Variables.Namespaces[Namespace.File]);

        txtImageReplace.Text = Regex.Replace(
            txtImageReplace.Text,
            "^" + fileNamespace,
            string.Empty,
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Removes a leading file namespace prefix from the replacement image text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void txtImageWith_Leave(object sender, EventArgs e)
    {
        string fileNamespace =
            Regex.Escape(Variables.Namespaces[Namespace.File]);

        txtImageWith.Text = Regex.Replace(
            txtImageWith.Text,
            "^" + fileNamespace,
            string.Empty,
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Starts or stops the progress indicator based on the list maker's busy state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void SetProgressBar(object sender, EventArgs e)
    {
        if (listMaker.BusyStatus)
        {
            StartProgressBar();
        }
        else
        {
            StopProgressBar();
        }
    }

    #endregion

    #region ArticleActions
    /// <summary>
    /// Moves the current article to a new title and updates the article list,
    /// action log, and user interface to reflect the result.
    /// </summary>
    private void MoveArticle()
    {
        if (TheArticle == null)
        {
            DisableButtons();
            return;
        }

        // TODO(Twain): Move article relocation into the shared article-action
        // service once move, delete, and protect workflows are consolidated.
        try
        {
            if (!TheSession.Page.Exists)
            {
                MessageBox.Show("Cannot move a non-existent page");
                return;
            }

            if (!TheSession.User.CanMovePage(TheSession.Page))
            {
                MessageBox.Show(
                    "Current user doesn't have enough rights to move \"" +
                    TheSession.Page.Title +
                    "\"",
                    "User rights not sufficient");

                return;
            }

            bool succeed =
                TryMoveArticle(
                    TheArticle,
                    TheSession,
                    out string newTitle,
                    out string msg);

            if (succeed)
            {
                Article replacementArticle = new(newTitle);

                listMaker.ReplaceArticle(
                    TheArticle,
                    replacementArticle);
            }

            articleActionLogControl1.LogArticleAction(
                TheArticle.Name,
                succeed,
                ArticleAction.Move,
                msg);

            StatusLabelText = msg;
        }
        catch (ApiErrorException ae)
        {
            // TODO: Replace string-based API error handling with strongly typed
            // move results or error classifications where practical.
            switch (ae.ErrorCode)
            {
                case "missingtitle":
                    StatusLabelText =
                        "Article deleted, cannot move";

                    listMaker.Remove(TheArticle);

                    articleActionLogControl1.LogArticleAction(
                        TheArticle.Name,
                        false,
                        ArticleAction.Move,
                        "Article already deleted, cannot move");
                    break;

                case "articleexists":
                    StatusLabelText =
                        "Target exists, cannot move";

                    MessageBox.Show(
                        "The destination article already exists and is not a " +
                        "redirect to the source article.\r\nMove not completed",
                        "Target exists");

                    articleActionLogControl1.LogArticleAction(
                        TheArticle.Name,
                        false,
                        ArticleAction.Move,
                        "Target exists");
                    break;

                default:
                    ErrorHandler.HandleException(ae);
                    break;
            }
        }
        catch (ApiException ex)
            when (ex is InvalidTitleException ||
                  ex is InterwikiException)
        {
            MessageBox.Show(
                ex.Message,
                "Invalid target page");
        }
        catch (ApiException ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Attempts to move the specified article and creates a message describing
    /// the result.
    /// </summary>
    /// <param name="article">The article to move.</param>
    /// <param name="session">The session used to perform the move.</param>
    /// <param name="newTitle">The destination title returned by the move operation.</param>
    /// <param name="msg">The message describing the move result.</param>
    /// <returns>
    /// <see langword="true"/> if the move succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool TryMoveArticle(
        Article article,
        Session session,
        out string newTitle,
        out string msg)
    {
        bool succeed =
            article.Move(
                session,
                out newTitle);

        if (succeed)
        {
            msg =
                "Moved " +
                article.Name +
                " to " +
                newTitle;
        }
        else
        {
            msg =
                "Move of " +
                article.Name +
                " failed!";
        }

        return succeed;
    }

    /// <summary>
    /// Deletes the current article and updates the article list, action log,
    /// and user interface to reflect the result.
    /// </summary>
    private void DeleteArticle()
    {
        if (TheArticle == null)
        {
            DisableButtons();
            return;
        }

        // TODO(Twain): Move article deletion into the shared article-action
        // service once protection, deletion, and move workflows are consolidated.
        try
        {
            if (!TheSession.Page.Exists)
            {
                MessageBox.Show("Cannot delete a non-existent page");
                return;
            }

            if (!TheSession.User.CanDeletePage(TheSession.Page))
            {
                MessageBox.Show(
                    "Current user doesn't have enough rights to delete \"" +
                    TheSession.Page.Title +
                    "\"",
                    "User rights not sufficient");

                return;
            }

            bool succeed =
                TryDeleteArticle(
                    TheArticle,
                    TheSession,
                    out string msg);

            if (succeed)
            {
                listMaker.Remove(TheArticle);
            }

            StatusLabelText = msg;

            articleActionLogControl1.LogArticleAction(
                TheArticle.Name,
                succeed,
                ArticleAction.Delete,
                msg);
        }
        catch (ApiErrorException ae)
        {
            // TODO: Replace string-based API error handling with strongly typed
            // delete results or error classifications where practical.
            if (ae.ErrorCode == "missingtitle")
            {
                StatusLabelText = "Article already deleted";

                listMaker.Remove(TheArticle);

                articleActionLogControl1.LogArticleAction(
                    TheArticle.Name,
                    false,
                    ArticleAction.Delete,
                    "Article already deleted");

                return;
            }

            if (ae.ErrorCode == "bigdelete")
            {
                StatusLabelText =
                    "You can't delete this page because it has more than 5,000 revisions";

                listMaker.Remove(TheArticle);

                articleActionLogControl1.LogArticleAction(
                    TheArticle.Name,
                    false,
                    ArticleAction.Delete,
                    "Article can't be deleted");

                return;
            }

            ErrorHandler.HandleException(ae);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Attempts to delete the specified article and creates a message describing
    /// the result.
    /// </summary>
    /// <param name="article">The article to delete.</param>
    /// <param name="session">The session used to perform the deletion.</param>
    /// <param name="msg">The message describing the deletion result.</param>
    /// <returns>
    /// <see langword="true"/> if deletion succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool TryDeleteArticle(
        Article article,
        Session session,
        out string msg)
    {
        bool succeed = article.Delete(session);

        if (succeed)
        {
            msg = "Deleted " + article.Name;
        }
        else
        {
            msg =
                "Deletion of " +
                article.Name +
                " failed!";
        }

        return succeed;
    }

    /// <summary>
    /// Protects the current article and updates the action log and user
    /// interface to reflect the result.
    /// </summary>
    private void ProtectArticle()
    {
        if (TheArticle == null)
        {
            DisableButtons();
            return;
        }

        // TODO(Twain): Move article protection into the shared article-action
        // service once protection, deletion, and move workflows are consolidated.
        try
        {
            if (!TheSession.User.IsSysop)
            {
                MessageBox.Show(
                    "Current user doesn't have enough rights to protect \"" +
                    TheSession.Page.Title +
                    "\"",
                    "User rights not sufficient");

                return;
            }

            bool succeed =
                TryProtectArticle(
                    TheArticle,
                    TheSession,
                    out string msg);

            articleActionLogControl1.LogArticleAction(
                TheArticle.Name,
                succeed,
                ArticleAction.Protect,
                msg);

            StatusLabelText = msg;
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Attempts to protect the specified article and creates a message describing
    /// the result.
    /// </summary>
    /// <param name="article">The article to protect.</param>
    /// <param name="session">The session used to perform the protection.</param>
    /// <param name="msg">The message describing the protection result.</param>
    /// <returns>
    /// <see langword="true"/> if protection succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool TryProtectArticle(
        Article article,
        Session session,
        out string msg)
    {
        bool succeed = article.Protect(session);

        if (succeed)
        {
            msg = "Protected " + article.Name;
        }
        else
        {
            msg =
                "Protection of " +
                article.Name +
                " failed!";
        }

        return succeed;
    }
    #endregion

    /// <summary>
    /// Opens the template substitution dialog.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnSubst_Click(object sender, EventArgs e)
    {
        _substTemplates.ShowDialog();
    }

    /// <summary>
    /// Opens the Regex Tester and optionally transfers the currently selected
    /// article text into the tester.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void launchRegexTester(object sender, EventArgs e)
    {
        if (_regexTester == null || _regexTester.IsDisposed)
        {
            _regexTester = new RegexTester();
        }

        if (txtEdit.SelectionLength > 0 &&
            MessageBox.Show(
                "Would you like to transfer the currently selected article text to the Regex Tester?",
                "Transfer Article Text?",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            _regexTester.ArticleText = txtEdit.SelectedText;
        }

        _regexTester.Show();
        _regexTester.BringToFront();
    }

    /// <summary>
    /// Updates the edit summary controls when the summary lock option
    /// changes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void chkLock_CheckedChanged(object sender, EventArgs e)
    {
        cmboEditSummary.Visible = !chkLock.Checked;
        lblSummary.Text = cmboEditSummary.Text;
        lblSummary.Visible = chkLock.Checked;
    }

    /// <summary>
    /// Enables or disables the link loading button based on whether a
    /// disambiguation page name has been entered.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void txtDabLink_TextChanged(object sender, EventArgs e)
    {
        btnLoadLinks.Enabled =
            !string.IsNullOrWhiteSpace(txtDabLink.Text);
    }

    /// <summary>
    /// Initializes the disambiguation page text box with the current list
    /// maker source when it is empty.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void txtDabLink_Enter(object sender, EventArgs e)
    {
        if (txtDabLink.Text.Length == 0)
        {
            txtDabLink.Text = listMaker.SourceText;
        }
    }

    /// <summary>
    /// Enables or disables the disambiguation controls.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void chkEnableDab_CheckedChanged(object sender, EventArgs e)
    {
        panelDab.Enabled = chkEnableDab.Checked;
    }

    // TODO(Twain): Move disambiguation link retrieval into a shared list service
    // so MainForm only coordinates user input and displays the resulting titles.

    /// <summary>
    /// Loads links from the specified disambiguation page or pages and
    /// populates the variants list, excluding likely year articles.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnLoadLinks_Click(object sender, EventArgs e)
    {
        try
        {
            string[] linkTitles =
                ParseDisambiguationLinkTitles(
                    txtDabLink.Text);

            txtDabVariants.Text = string.Empty;

            IEnumerable<Article> articles =
                new LinksOnPageListProvider().MakeList(linkTitles);

            txtDabVariants.Text =
                BuildDisambiguationVariantsText(articles);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Splits the disambiguation page input into individual page titles.
    /// </summary>
    /// <param name="text">The disambiguation page input text.</param>
    /// <returns>The parsed page titles.</returns>
    private static string[] ParseDisambiguationLinkTitles(string text)
    {
        return text.Split(
            new[] { '|' },
            StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Builds the disambiguation variants text from the supplied articles,
    /// excluding likely year articles.
    /// </summary>
    /// <param name="articles">The articles to process.</param>
    /// <returns>
    /// The article titles formatted as newline-separated text.
    /// </returns>
    private static string BuildDisambiguationVariantsText(
        IEnumerable<Article> articles)
    {
        StringBuilder builder = new();

        foreach (Article article in articles)
        {
            // Exclude likely year articles.
            if (uint.TryParse(article.Name, out uint year) &&
                year < 2100)
            {
                continue;
            }

            builder.AppendLine(article.Name);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Loads disambiguation links when Enter is pressed in the page input.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The key press event data.</param>
    private void txtDabLink_KeyPress(object sender, KeyPressEventArgs e)
    {
        switch (e.KeyChar)
        {
            case '\r':
                e.Handled = true;
                btnLoadLinks_Click(this, null);
                break;
        }
    }

    /// <summary>
    /// Executes the current Find operation when Enter is pressed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The key press event data.</param>
    private void txtFind_KeyPress(object sender, KeyPressEventArgs e)
    {
        switch (e.KeyChar)
        {
            case '\r':
                e.Handled = true;
                btnFind_Click(this, null);
                break;
        }
    }

    #region Notify Tray
    /// <summary>
    /// Restores the main window from the notification area.
    /// </summary>
    /// <remarks>
    /// This handler is also used when the notification area icon is
    /// double-clicked.
    /// </remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void showToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Visible = true;
        WindowState = _lastState;
    }

    /// <summary>
    /// Hides the main window to the notification area.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void hideToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Visible = false;
    }

    /// <summary>
    /// Updates the notification area context menu before it is displayed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// A <see cref="CancelEventArgs"/> that can be used to cancel the menu
    /// opening.
    /// </param>
    private void mnuNotify_Opening(object sender, CancelEventArgs e)
    {
        SetMenuVisibility(Visible);
    }

    /// <summary>
    /// Updates the enabled state of the notification area Show and Hide commands.
    /// </summary>
    /// <param name="visible">
    /// <see langword="true"/> if the main window is currently visible;
    /// otherwise, <see langword="false"/>.
    /// </param>
    private void SetMenuVisibility(bool visible)
    {
        showToolStripMenuItem.Enabled =
            !visible ||
            WindowState == FormWindowState.Minimized;

        hideToolStripMenuItem.Enabled = visible;
    }

    /// <summary>
    /// Displays a notification balloon from the notification area icon.
    /// </summary>
    /// <param name="message">
    /// The message to display.
    /// </param>
    /// <param name="icon">
    /// The icon displayed with the notification.
    /// </param>
    public void NotifyBalloon(string message, ToolTipIcon icon)
    {
        ntfyTray.BalloonTipText = message;
        ntfyTray.BalloonTipIcon = icon;
        ntfyTray.ShowBalloonTip(10000);
    }
    #endregion

    // TODO(Twain): Move wiki-link text transformation into shared text-processing
    // logic once editor transformations are extracted from MainForm.
    /// <summary>
    /// Removes wiki-link markup from the selected editor text while preserving
    /// the most appropriate display text when possible.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnRemove_Click(object sender, EventArgs e)
    {
        EditBoxTab.SelectedTab = tpEdit;

        string selectedtext = txtEdit.SelectedText;

        if (!TryRemoveWikiLinkMarkup(
            selectedtext,
            out string replacementText))
        {
            MessageBox.Show(
                "Select a link to remove either manually or by clicking " +
                "a link in the list above.");

            return;
        }

        if (replacementText == selectedtext)
        {
            MessageBox.Show(
                "The selected link could not be removed.");

            return;
        }

        txtEdit.SelectedText = replacementText;
        txtEdit.ResetFind();
    }

    /// <summary>
    /// Attempts to remove wiki-link markup from the supplied text while
    /// preserving the most appropriate display text.
    /// </summary>
    /// <param name="selectedtext">The selected wiki-link text to process.</param>
    /// <param name="replacementText">
    /// The text that should replace the original selection.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the supplied text represents a wiki link;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryRemoveWikiLinkMarkup(
        string selectedtext,
        out string replacementText)
    {
        replacementText = selectedtext;

        if (!selectedtext.StartsWith("[[") ||
            !selectedtext.EndsWith("]]"))
        {
            return false;
        }

        replacementText =
            selectedtext.Trim('[').Trim(']');

        if (replacementText.EndsWith("|"))
        {
            if (replacementText.Contains("(") &&
                replacementText.Contains(")"))
            {
                replacementText = replacementText.Substring(
                    0,
                    replacementText.IndexOf(
                        "(",
                        StringComparison.Ordinal));
            }

            if (replacementText.Contains(":"))
            {
                replacementText = replacementText.Substring(
                    replacementText.IndexOf(
                        ":",
                        StringComparison.Ordinal))
                    .TrimEnd('|');
            }

            if (selectedtext ==
                "[[" + replacementText + "]]")
            {
                replacementText = selectedtext;
            }
        }
        else if (replacementText.Contains("|"))
        {
            replacementText = replacementText.Substring(
                replacementText.IndexOf(
                    "|",
                    StringComparison.Ordinal) + 1);
        }

        return true;
    }

    /// <summary>
    /// Resets the accumulated nudge counters and updates the displayed count.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnResetNudges_Click(object sender, EventArgs e)
    {
        Nudges = 0;
        _sameArticleNudges = 0;
        lblNudges.Text = NudgeTimerString + "0";
    }

    #region "Nudge timer"

    /// <summary>
    /// Prefix displayed with the accumulated nudge count.
    /// </summary>
    private const string NudgeTimerString = "Total nudges: ";

    // TODO(Twain): Move plugin nudge notifications behind the plugin service
    // rather than iterating the plugin registry directly from MainForm.
    //
    // TODO(Twain): Move nudge/retry policy out of MainForm once processing
    // orchestration is extracted into a shared service.

    /// <summary>
    /// Handles a nudge timer event by allowing plugins to cancel the nudge,
    /// updating nudge statistics, and restarting or skipping processing as
    /// appropriate.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// The nudge timer event data, including the ability to cancel the nudge.
    /// </param>
    private void NudgeTimer_Tick(
        object sender,
        NudgeTimer.NudgeTimerEventArgs e)
    {
        if (!BotMode)
        {
            return;
        }

        if (PluginsCancelNudge())
        {
            e.Cancel = true;
            return;
        }

        Nudges++;
        lblNudges.Text = NudgeTimerString + Nudges;

        NudgeTimer.Stop();

        ProcessNudgeRetry();

        NotifyPluginsNudged();
    }

    /// <summary>
    /// Notifies plugins that a nudge is about to occur and determines whether
    /// any plugin requests that the nudge be cancelled.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a plugin cancels the nudge; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool PluginsCancelNudge()
    {
        foreach (KeyValuePair<string, IAWBPlugin> a in Twain.Core.Plugin.PluginManager.AWBPlugins)
        {
            bool cancel;
            a.Value.Nudge(out cancel);

            if (cancel)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies the retry behavior for the current nudge, either skipping the
    /// page after repeated failures or restarting processing.
    /// </summary>
    private void ProcessNudgeRetry()
    {
        if (chkNudgeSkip.Checked && _sameArticleNudges > 0)
        {
            _sameArticleNudges = 0;
            SkipPage("There was an error saving the page twice");
            return;
        }

        _sameArticleNudges++;
        Stop();
        _stopProcessing = false;
        Start();
    }

    /// <summary>
    /// Notifies all registered plugins that the nudge has completed.
    /// </summary>
    private void NotifyPluginsNudged()
    {
        foreach (KeyValuePair<string, IAWBPlugin> a in Twain.Core.Plugin.PluginManager.AWBPlugins)
        {
            a.Value.Nudged(Nudges);
        }
    }

    /// <summary>
    /// Gets the number of nudges recorded for the current session.
    /// </summary>
    public int Nudges { get; private set; }

    #endregion

    #region Edit Box Saver

    /// <summary>
    /// Saves the current edit box contents when automatic saving is configured.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void EditBoxSaveTimer_Tick(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_autoSaveEditBoxFile))
        {
            SaveEditBoxText(_autoSaveEditBoxFile);
        }
    }

    /// <summary>
    /// Saves the current edit box contents to the specified file.
    /// </summary>
    /// <param name="path">The absolute path of the file to write.</param>
    private void SaveEditBoxText(string path)
    {
        Tools.WriteTextFileAbsolutePath(txtEdit.Text, path, false);
    }

    #endregion

    /// <summary>
    /// Prompts the user for a file and saves the current edit box contents.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void saveTextToFileToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (saveListDialog.ShowDialog() == DialogResult.OK)
        {
            SaveEditBoxText(saveListDialog.FileName);
        }
    }

    // TODO(Twain): Move user-talk template parsing into a shared parser/
    // configuration service once this logic is moved out of MainForm.

    /// <summary>
    /// Loads the configured user talk templates from the wiki and generates
    /// the corresponding template-matching regular expression.
    /// </summary>
    private void LoadUserTalkWarnings()
    {
        Regex userTalkTemplate = new(
            @"# ?\[\[" +
            Variables.NamespacesCaseInsensitive[Namespace.Template] +
            @"(.*?)\]\]");

        _userTalkTemplatesRegex = null;

        // Prevent repeated loading attempts on every page.
        _userTalkWarningsLoaded = true;

        try
        {
            string text = LoadWikiConfigurationText(
                "Project:AutoWikiBrowser/User talk templates",
                "LoadUserTalkWarnings",
                "Unable to load user talk templates: ");

            if (text.Length == 0)
            {
                return;
            }

            List<string> userTalkTemplates =
                ParseUserTalkTemplates(
                    text,
                    userTalkTemplate);

            if (userTalkTemplates.Any())
            {
                _userTalkTemplatesRegex =
                    Tools.NestedTemplateRegex(userTalkTemplates);
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
            _userTalkWarningsLoaded = false;
        }
    }

    /// <summary>
    /// Extracts user talk template names from the supplied configuration text.
    /// </summary>
    /// <param name="text">The configuration text to parse.</param>
    /// <param name="userTalkTemplate">
    /// The regular expression used to locate template entries.
    /// </param>
    /// <returns>The extracted template names.</returns>
    private static List<string> ParseUserTalkTemplates(
        string text,
        Regex userTalkTemplate)
    {
        List<string> userTalkTemplates = new();

        foreach (Match match in userTalkTemplate.Matches(text))
        {
            userTalkTemplates.Add(match.Groups[1].Value);
        }

        return userTalkTemplates;
    }

    // TODO(Twain): Move wiki-based parser configuration loading out of MainForm
    // once parser configuration services are extracted.
    /// <summary>
    /// Loads the list of template redirects to bypass from the configured
    /// Twain template redirects page.
    /// </summary>
    private void LoadTemplateRedirects()
    {
        _templateRedirectsLoaded = true;

        string text = LoadWikiConfigurationText(
            "Project:AutoWikiBrowser/Template redirects",
            "LoadTemplateRedirects",
            "Unable to load template redirects: ");

        // Always update the parser state, even when no text was loaded.
        // This ensures redirects from a previously selected project are cleared.
        WikiRegexes.TemplateRedirects =
            Parsers.LoadTemplateRedirects(text);
    }

    /// <summary>
    /// Loads the configured dated-template definitions from the wiki and
    /// updates the corresponding parser expressions when data is available.
    /// </summary>
    private void LoadDatedTemplates()
    {
        _datedTemplatesLoaded = true;

        string text = LoadWikiConfigurationText(
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
    /// Loads renamed template-parameter definitions from the wiki and updates
    /// the corresponding parser expressions when data is available.
    /// </summary>
    private void LoadRenameTemplateParameters()
    {
        _renamedTemplateParametersLoaded = true;

        string text = LoadWikiConfigurationText(
            "Project:AutoWikiBrowser/Rename template parameters",
            "LoadRenameTemplateParameters",
            "Unable to load renamed template parameters: ");

        if (text.Length > 0)
        {
            WikiRegexes.RenamedTemplateParameters =
                Parsers.LoadRenamedTemplateParameters(text);
        }
    }

    // TODO(Twain): Move wiki-based parser configuration loading out of MainForm
    // once parser configuration services are extracted.
    /// <summary>
    /// Loads configuration text from the specified wiki page and logs failures.
    /// </summary>
    /// <param name="pageTitle">The wiki page containing the configuration text.</param>
    /// <param name="logSource">The source name used for debug logging.</param>
    /// <param name="errorMessage">The message prefix used when loading fails.</param>
    /// <returns>
    /// The loaded configuration text, or an empty string when the page cannot
    /// be loaded.
    /// </returns>
    private string LoadWikiConfigurationText(
        string pageTitle,
        string logSource,
        string errorMessage)
    {
        try
        {
            return TheSession.Editor.SynchronousEditor.Clone().Open(
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

    /// <summary>
    /// Restores the editor contents to the article text as originally loaded.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void undoAllChangesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (TheArticle == null)
            return;

        txtEdit.Text = TheArticle.OriginalArticleText;
    }

    #region History

    /// <summary>
    /// Loads history or incoming-link information when the corresponding
    /// editor tab is selected.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void tabControl2_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (TheArticle == null)
            return;

        if (EditBoxTab.SelectedTab == tpHistory)
            NewHistory(TheArticle.Name);
        else if (EditBoxTab.SelectedTab == tpLinks)
            NewWhatLinksHere(TheArticle.Name);
    }

    /// <summary>
    /// Loads the printable revision history for the specified page into the
    /// embedded history browser.
    /// </summary>
    /// <param name="pageTitle">
    /// The title of the page whose revision history should be displayed.
    /// </param>
    private void NewHistory(string pageTitle)
    {
        // TODO(Twain): Consolidate embedded browser navigation and error handling
        // into a shared browser helper once all browser workflows have been reviewed.
        try
        {
            if (EditBoxTab.SelectedTab != tpHistory ||
                string.IsNullOrEmpty(pageTitle))
            {
                webBrowserHistory.Navigate("about:blank");
                return;
            }

            string encodedTitle =
                WebUtility.UrlEncode(pageTitle);

            string url =
                BuildHistoryUrl(
                    Variables.URLIndex,
                    encodedTitle);

            Uri targetUri = new(url);

            if (webBrowserHistory.Url != targetUri)
            {
                webBrowserHistory.Navigate(url);
            }
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                "NewHistory",
                "Unable to load page history: " +
                ex.Message);

            webBrowserHistory.Navigate("about:blank");

            HtmlDocument document = webBrowserHistory.Document;

            if (document != null)
            {
                document.Write(
                    "<html><body><p>Unable to load history</p></body></html>");
            }
        }
    }

    // TODO: Replace the legacy WebBrowser control with the
    // modern browser implementation used by Twain.
    /// <summary>
    /// Processes the loaded history page before displaying it in the embedded
    /// browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// Information about the completed document load.
    /// </param>
    private void webBrowserHistory_DocumentCompleted(
        object sender,
        WebBrowserDocumentCompletedEventArgs e)
    {
        if (webBrowserHistory.Document != null &&
            webBrowserHistory.Document.Body != null)
        {
            webBrowserHistory.Document.Body.InnerHtml =
                ProcessHTMLForDisplay(webBrowserHistory.DocumentText);
        }
    }

    /// <summary>
    /// Loads the printable "What Links Here" page for the specified article into
    /// the embedded links browser.
    /// </summary>
    /// <param name="title">
    /// The title of the page whose incoming links should be displayed.
    /// </param>
    private void NewWhatLinksHere(string title)
    {
        // TODO(Twain): Consolidate embedded browser navigation and error handling
        // into a shared browser helper once all browser workflows have been reviewed.
        try
        {
            if (EditBoxTab.SelectedTab != tpLinks ||
                string.IsNullOrEmpty(title))
            {
                webBrowserLinks.Navigate("about:blank");
                return;
            }

            string url =
                BuildWhatLinksHereUrl(
                    Variables.URLIndex,
                    title);

            Uri targetUri = new(url);

            if (webBrowserLinks.Url != targetUri)
            {
                webBrowserLinks.Navigate(url);
            }
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                "NewWhatLinksHere",
                "Unable to load What Links Here: " +
                ex.Message);

            webBrowserLinks.Navigate("about:blank");

            HtmlDocument document = webBrowserLinks.Document;

            if (document != null)
            {
                document.Write(
                    "<html><body><p>Unable to load What Links Here</p></body></html>");
            }
        }
    }

    /// <summary>
    /// Builds the printable "What Links Here" URL for the specified page.
    /// </summary>
    /// <param name="urlIndex">
    /// The wiki index URL.
    /// </param>
    /// <param name="pageTitle">
    /// The page title.
    /// </param>
    /// <returns>
    /// The printable "What Links Here" URL.
    /// </returns>
    private static string BuildWhatLinksHereUrl(
        string urlIndex,
        string pageTitle)
    {
        string encodedTitle =
            WebUtility.UrlEncode(pageTitle);

        return
            urlIndex +
            "?title=Special:WhatLinksHere/" +
            encodedTitle +
            "&printable=yes";
    }

    /// <summary>
    /// Processes the loaded links page before displaying it in the embedded
    /// browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// Information about the completed document load.
    /// </param>
    private void webBrowserLinks_DocumentCompleted(
        object sender,
        WebBrowserDocumentCompletedEventArgs e)
    {
        if (webBrowserLinks.Document != null &&
            webBrowserLinks.Document.Body != null)
        {
            webBrowserLinks.Document.Body.InnerHtml =
                ProcessHTMLForDisplay(webBrowserLinks.DocumentText);
        }
    }

    /// <summary>
    /// HTML marker indicating the beginning of the page content.
    /// </summary>
    private const string StartMark = "<!-- start content -->";

    /// <summary>
    /// HTML marker indicating the end of the page content.
    /// </summary>
    private const string EndMark = "<!-- end content -->";

    // TODO(Twain): Extract embedded-browser HTML processing from MainForm so
    // content transformation can be reused independently of the WinForms browser.
    /// <summary>
    /// Prepares wiki HTML for display in the embedded browser by extracting the
    /// main content, forcing links and forms to open externally, and prepending
    /// the current article title.
    /// </summary>
    /// <param name="linksHtml">The HTML content to prepare for display.</param>
    /// <returns>The processed HTML content.</returns>
    private string ProcessHTMLForDisplay(string linksHtml)
    {
        if (linksHtml.Contains(StartMark) &&
            linksHtml.Contains(EndMark))
        {
            linksHtml =
                Tools.StringBetween(
                    linksHtml,
                    StartMark,
                    EndMark);
        }

        linksHtml =
            linksHtml.Replace(
                "<A ",
                "<a target=\"_blank\" ");

        linksHtml =
            linksHtml.Replace(
                "<FORM ",
                "<form target=\"_blank\" ");

        string articleName =
            TheArticle?.Name ?? string.Empty;

        return "<h3>" + articleName + "</h3>" + linksHtml;
    }

    /// <summary>
    /// Opens the current article's revision history in the default web browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void openInBrowserToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (TheArticle == null)
        {
            return;
        }

        Tools.OpenArticleHistoryInBrowser(
            TheArticle.Name);
    }

    /// <summary>
    /// Reloads the current article's printable revision history in the embedded
    /// history browser.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void refreshHistoryToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (TheArticle == null)
        {
            return;
        }

        try
        {
            string url =
                BuildHistoryUrl(
                    Variables.URLIndex,
                    TheArticle.URLEncodedName);

            webBrowserHistory.Navigate(url);
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                "RefreshHistory",
                "Unable to refresh page history: " +
                ex.Message);

            webBrowserHistory.Navigate("about:blank");

            HtmlDocument document =
                webBrowserHistory.Document;

            if (document != null)
            {
                document.Write(
                    "<html><body><p>Unable to load history</p></body></html>");
            }
        }
    }

    // TODO(Twain): Move wiki history URL construction into the site/navigation
    // layer so MainForm does not construct MediaWiki URLs directly.
    /// <summary>
    /// Builds the printable revision-history URL for the specified page.
    /// </summary>
    /// <param name="urlIndex">The wiki index URL.</param>
    /// <param name="encodedPageName">
    /// The URL-encoded page name.
    /// </param>
    /// <returns>The printable revision-history URL.</returns>
    private static string BuildHistoryUrl(
        string urlIndex,
        string encodedPageName)
    {
        return urlIndex +
            "?title=" +
            encodedPageName +
            "&action=history&printable=yes";
    }

    /// <summary>
    /// Updates the History menu before it is displayed by enabling or
    /// disabling commands based on whether an article is currently loaded.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// A <see cref="CancelEventArgs"/> that can be used to cancel the event.
    /// </param>
    private void mnuHistory_Opening(object sender, CancelEventArgs e)
    {
        openInBrowserToolStripMenuItem.Enabled =
            refreshHistoryToolStripMenuItem.Enabled =
                (TheArticle != null);
    }
    #endregion

    /// <summary>
    /// Opens the profile management dialog unless the application is in the
    /// process of shutting down.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void profilesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!_shuttingDown)
        {
            _profiles.ShowDialog(this);
        }
    }

    /// <summary>
    /// Loads the settings associated with the currently selected user
    /// profile.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void UserDefaultSettingsLoadRequired(object sender, EventArgs e)
    {
        LoadPrefs(_profiles.SettingsToLoad);
    }

    /// <summary>
    /// Refreshes project, session, article, and user-interface state after a
    /// profile successfully logs in.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void ProfileLoggedIn(object sender, EventArgs e)
    {
        // TODO(Twain): Extract post-login session and project refresh logic from
        // MainForm so login completion is not responsible for UI orchestration.
        if (string.IsNullOrEmpty(_profiles.SettingsToLoad) &&
            Variables.TryLoadingAgainAfterLogin)
        {
            SetProject(
                Variables.ReloadProjectSettings.langCode,
                Variables.ReloadProjectSettings.projectName,
                Variables.ReloadProjectSettings.customProject,
                Variables.ReloadProjectSettings.protocol);
        }

        if (TheSession.IsBusy)
        {
            TheSession.Editor.Abort();
        }

        // English Wikipedia does not use {{Wikify}}.
        wikifyToolStripMenuItem.Visible =
            !Variables.IsWikipediaEN;

        TheArticle = null;
        txtEdit.Text = string.Empty;
        _templateRedirectsLoaded = false;

        CheckStatus(true);
        UpdateStatusUI();

        StopProgressBar();
        DisableButtons();

        if (TheSession.User.HasMessages)
        {
            WeHaveNewMessages();
        }

        UpdateUserNotifications();
    }

    /// <summary>
    /// Synchronizes the "Mark all edits as minor" menu option with the
    /// Minor Edit check box.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void chkMinor_CheckedChanged(object sender, EventArgs e)
    {
        markAllAsMinorToolStripMenuItem.Checked = chkMinor.Checked;
    }

    /// <summary>
    /// Synchronizes the Minor Edit check box with the "Mark all edits as
    /// minor" menu option.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void markAllAsMinorToolStripMenuItem_Click(object sender, EventArgs e)
    {
        chkMinor.Checked = markAllAsMinorToolStripMenuItem.Checked;
    }

    /// <summary>
    /// Displays the login dialog.
    /// </summary>
    private void ShowLogin()
    {
        using Login login = new();

        login.ShowDialog(this);
    }

    #region Shutdown

    /// <summary>
    /// Enables or disables the shutdown options based on the current
    /// shutdown setting.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void chkShutdown_CheckedChanged(object sender, EventArgs e)
    {
        EnableDisableShutdownControls(chkShutdown.Checked);
    }

    /// <summary>
    /// Enables or disables the available shutdown actions and sets the
    /// shutdown option as the default when enabled.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable shutdown controls; otherwise,
    /// <see langword="false"/>.
    /// </param>
    private void EnableDisableShutdownControls(bool enabled)
    {
        radShutdown.Enabled =
            radStandby.Enabled =
            radRestart.Enabled =
            radHibernate.Enabled =
            radShutdown.Checked =
            enabled;
    }

    /// <summary>
    /// Gets a value indicating whether the application is currently allowed
    /// to shut down the computer.
    /// </summary>
    private bool CanShutdown
    {
        get
        {
            return chkShutdown.Checked && !listMaker.Any();
        }
    }

    /// <summary>
    /// Initiates the configured shutdown workflow, displays the shutdown
    /// confirmation dialog, and performs or cancels the requested action.
    /// </summary>
    private void Shutdown()
    {
        if (!CanShutdown)
        {
            return;
        }

        // TODO(Twain): Extract shutdown orchestration from MainForm so timer,
        // confirmation, and platform shutdown behavior can be managed separately.
        ShutdownTimer.Enabled = true;
        ShutdownTimer.Start();

        using ShutdownNotification shutdownNotification = new()
        {
            ShutdownType = GetShutdownType()
        };

        switch (shutdownNotification.ShowDialog(this))
        {
            case DialogResult.Cancel:
                ShutdownTimer.Stop();
                ShutdownTimer.Enabled = false;

                MessageBox.Show(
                    GetShutdownType() + " aborted!");

                return;

            case DialogResult.OK:
                ShutdownComputer();
                break;
        }
    }

    /// <summary>
    /// Gets the currently selected shutdown action.
    /// </summary>
    /// <returns>
    /// The selected shutdown action name, or an empty string if no shutdown
    /// action is selected.
    /// </returns>
    private string GetShutdownType()
    {
        if (radShutdown.Checked)
        {
            return "Shutdown";
        }

        if (radStandby.Checked)
        {
            return "Standby";
        }

        if (radRestart.Checked)
        {
            return "Restart";
        }

        return radHibernate.Checked ? "Hibernate" : "";
    }

    /// <summary>
    /// Starts the Windows shutdown process using the specified command-line
    /// arguments.
    /// </summary>
    /// <param name="arguments">
    /// The arguments passed to the Windows shutdown command.
    /// </param>
    private void StartShutdownProcess(string arguments)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = arguments,
                UseShellExecute = true
            });
    }

    /// <summary>
    /// Performs the configured shutdown action when the shutdown timer is
    /// active.
    /// </summary>
    private void ShutdownComputer()
    {
        if (!ShutdownTimer.Enabled)
        {
            return;
        }

        Stop();

        ShutdownTimer.Stop();
        ShutdownTimer.Enabled = false;

        if (radHibernate.Checked)
        {
            Application.SetSuspendState(
                PowerState.Hibernate,
                true,
                true);
        }
        else if (radRestart.Checked)
        {
            StartShutdownProcess("-r");
        }
        else if (radShutdown.Checked)
        {
            StartShutdownProcess("-s");
        }
        else if (radStandby.Checked)
        {
            Application.SetSuspendState(
                PowerState.Suspend,
                true,
                true);
        }
    }

    /// <summary>
    /// Shuts down the computer when the shutdown timer elapses.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void ShutdownTimer_Tick(object sender, EventArgs e)
    {
        ShutdownComputer();
    }

    #endregion

    #region EditToolbar

    /// <summary>
    /// Applies bold wiki markup to the current selection or inserts bold
    /// placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgBold_Click(object sender, EventArgs e)
    {
        EditToolBarAction("'''Bold text'''", 12, 9, "'''");
    }

    /// <summary>
    /// Applies italic wiki markup to the current selection or inserts italic
    /// placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgItalics_Click(object sender, EventArgs e)
    {
        EditToolBarAction("''Italic text''", 13, 11, "''");
    }

    /// <summary>
    /// Applies internal-link wiki markup to the current selection or inserts
    /// link placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgLink_Click(object sender, EventArgs e)
    {
        EditToolBarAction("[[Link title]]", 12, 10, "[[", "]]");
    }

    /// <summary>
    /// Applies external-link markup to the current selection or inserts
    /// external-link placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgExtlink_Click(object sender, EventArgs e)
    {
        EditToolBarAction(
            "[http://www.example.com link title]",
            34,
            33,
            "[",
            "]");
    }

    /// <summary>
    /// Applies math markup to the current selection or inserts formula
    /// placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgMath_Click(object sender, EventArgs e)
    {
        EditToolBarAction(
            "<math>Insert formula here</math>",
            26,
            19,
            "<math>",
            "</math>");
    }

    /// <summary>
    /// Applies nowiki markup to the current selection or inserts
    /// non-formatted placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgNowiki_Click(object sender, EventArgs e)
    {
        EditToolBarAction(
            "<nowiki>Insert non-formatted text here</nowiki>",
            39,
            30,
            "<nowiki>",
            "</nowiki>");
    }

    /// <summary>
    /// Appends a horizontal rule to the current selection.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgHr_Click(object sender, EventArgs e)
    {
        txtEdit.SelectedText += "\r\n----\r\n";
    }

    /// <summary>
    /// Applies redirect markup using the current wiki's configured redirect
    /// magic word.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgRedirect_Click(object sender, EventArgs e)
    {
        string redirect = Variables.MagicWords["redirect"][0].ToUpper();

        EditToolBarAction(
            redirect + " [[Insert text]]",
            13,
            11,
            redirect + " [[",
            "]]");
    }

    /// <summary>
    /// Applies strikethrough markup to the current selection or inserts
    /// strikethrough placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgStrike_Click(object sender, EventArgs e)
    {
        EditToolBarAction(
            "<s>Strike-through text</s>",
            23,
            19,
            "<s>",
            "</s>");
    }

    /// <summary>
    /// Applies superscript markup to the current selection or inserts
    /// superscript placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgSup_Click(object sender, EventArgs e)
    {
        EditToolBarAction(
            "<sup>Superscript text</sup>",
            22,
            16,
            "<sup>",
            "</sup>");
    }

    /// <summary>
    /// Applies subscript markup to the current selection or inserts
    /// subscript placeholder text.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgSub_Click(object sender, EventArgs e)
    {
        EditToolBarAction(
            "<sub>Subscript text</sub>",
            20,
            14,
            "<sub>",
            "</sub>");
    }

    /// <summary>
    /// Applies HTML comment markup to the current selection or inserts a
    /// comment placeholder.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void imgComment_Click(object sender, EventArgs e)
    {
        EditToolBarAction("<!-- Comment -->", 11, 7, "<!-- ", " -->");
    }

    /// <summary>
    /// Applies EditToolBar button action
    /// </summary>
    /// <param name="noSelection">String to display if no text already select</param>
    /// <param name="selectionStartOffset">Start position to highlight from end of noSelection</param>
    /// <param name="selectionLength">Length of selection of text to replace</param>
    /// <param name="selectionBeforeAfter">String if there is a selection to display before and after selected text</param>
    private void EditToolBarAction(string noSelection, int selectionStartOffset, int selectionLength,
                                   string selectionBeforeAfter)
    {
        EditToolBarAction(noSelection, selectionStartOffset, selectionLength, selectionBeforeAfter, selectionBeforeAfter);
    }

    /// <summary>
    /// Applies EditToolBar button action
    /// </summary>
    /// <param name="noSelection">String to display if no text already select</param>
    /// <param name="selectionStartOffset">Start position to highlight from end of noSelection</param>
    /// <param name="selectionLength">Length of selection of text to replace</param>
    /// <param name="selectionBefore">String to display before user selected text</param>
    /// <param name="selectionAfter">String to display after user selected text</param>
    private void EditToolBarAction(string noSelection, int selectionStartOffset, int selectionLength,
                                   string selectionBefore, string selectionAfter)
    {
        if (txtEdit.SelectionLength == 0)
        {
            txtEdit.SelectedText = noSelection;
            txtEdit.SelectionStart = txtEdit.SelectionStart - selectionStartOffset;
            txtEdit.SelectionLength = selectionLength;
        }
        else
        {
            txtEdit.SelectedText = selectionBefore + txtEdit.SelectedText + selectionAfter;
        }
    }

    /// <summary>
    /// Enables or disables all edit toolbar commands.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable the edit toolbar; otherwise,
    /// <see langword="false"/>.
    /// </param>
    private void SetEditToolBarEnabled(bool enabled)
    {
        imgBold.Enabled =
            imgExtlink.Enabled =
            imgHr.Enabled =
            imgItalics.Enabled =
            imgLink.Enabled =
            imgMath.Enabled =
            imgNowiki.Enabled =
            imgRedirect.Enabled =
            imgStrike.Enabled =
            imgSub.Enabled =
            imgSup.Enabled =
            imgComment.Enabled =
            enabled;
    }

    /// <summary>
    /// Gets or sets whether the edit toolbar is visible and adjusts the edit
    /// summary control to use the available space.
    /// </summary>
    private bool EditToolBarVisible
    {
        get
        {
            return imgBold.Visible;
        }

        set
        {
            if (imgBold.Visible == value)
            {
                return;
            }

            // TODO(Twain): Replace manual toolbar layout calculations with
            // layout-managed positioning when the editor UI is migrated.
            if (value)
            {
                // Edit toolbar visible.
                txtReviewEditSummary.Location =
                    new Point(
                        (int)Math.Round(
                            txtEdit.Location.X +
                            imgBold.Width * 12.4),
                        txtReviewEditSummary.Location.Y);

                txtReviewEditSummary.Size =
                    new Size(
                        (int)Math.Round(
                            txtEdit.Size.Width -
                            imgBold.Width * 12.4),
                        txtReviewEditSummary.Size.Height);
            }
            else
            {
                txtReviewEditSummary.Location =
                    new Point(
                        txtEdit.Location.X,
                        txtReviewEditSummary.Location.Y);

                txtReviewEditSummary.Size =
                    new Size(
                        txtEdit.Size.Width,
                        txtReviewEditSummary.Size.Height);
            }

            imgBold.Visible =
                imgExtlink.Visible =
                imgHr.Visible =
                imgItalics.Visible =
                imgLink.Visible =
                imgMath.Visible =
                imgNowiki.Visible =
                imgRedirect.Visible =
                imgStrike.Visible =
                imgSub.Visible =
                imgSup.Visible =
                imgComment.Visible =
                value;

            showHideEditToolbarToolStripMenuItem.Checked = value;
        }
    }

    /// <summary>
    /// Shows or hides the edit toolbar based on the current menu item state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void showHideEditToolbarToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        EditToolBarVisible = !showHideEditToolbarToolStripMenuItem.Checked;
    }

    #endregion

    #region various menus and event handlers

    /// <summary>
    /// Updates the Find text box tooltip to display its current contents.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void txtFind_MouseHover(object sender, EventArgs e)
    {
        ToolTip.SetToolTip(txtFind, txtFind.Text);
    }

    /// <summary>
    /// Watches or unwatches the current article when no editor operation is active.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnWatch_Click(object sender, EventArgs e)
    {
        if (TheArticle == null)
        {
            DisableButtons();
            return;
        }

        if (TheSession.Editor.IsActive)
        {
            return;
        }

        btnWatch.Enabled = false;

        if (PageWatched)
        {
            TheSession.Editor.Unwatch(TheArticle.Name);
        }
        else
        {
            TheSession.Editor.Watch(TheArticle.Name);
        }

        PageWatched = !PageWatched;
        btnWatch.Enabled = true;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the current page is marked
    /// as watched in the user interface.
    /// </summary>
    private bool PageWatched
    {
        get { return btnWatch.Text != "Watch"; }
        set { btnWatch.Text = value ? "Unwatch" : "Watch"; }
    }

    /// <summary>
    /// Compares two regular-expression entries by their integer keys in
    /// descending order.
    /// </summary>
    /// <param name="x">The first regular-expression entry to compare.</param>
    /// <param name="y">The second regular-expression entry to compare.</param>
    /// <returns>
    /// A value indicating the relative sort order of <paramref name="x"/>
    /// and <paramref name="y"/>.
    /// </returns>
    private static int CompareRegexPairs(
        KeyValuePair<int, string> x,
        KeyValuePair<int, string> y)
    {
        return y.Key.CompareTo(x.Key);
    }

    /// <summary>
    /// Profiles the currently loaded regular-expression typo rules against the
    /// current article text and writes the timing results to a diagnostic file.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void profileTyposToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (_regexTypos == null)
        {
            MessageBox.Show(
                "No typos loaded",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        List<KeyValuePair<Regex, string>> typos =
            _regexTypos.GetTypos();

        if (!typos.Any())
        {
            MessageBox.Show(
                "No typos loaded",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        string text = txtEdit.Text;

        if (!txtEdit.Enabled || text.Length == 0)
        {
            MessageBox.Show(
                "No article text",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        if (MessageBox.Show(
            "Test typo rules for performance (this takes up to 5 minutes)?",
            "Test typos",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        // TODO(Twain): Move typo-rule profiling into Twain.Diagnostics once
        // diagnostic services are separated from MainForm.
        StringBuilder builder =
            ProfileTypoRules(
                typos,
                text,
                TheArticle.Name);

        Tools.WriteTextFile(
            builder,
            "typos.txt",
            false);

        MessageBox.Show(
            "Results are saved in the file 'typos.txt'",
            "Profiling complete",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    /// <summary>
    /// Profiles the supplied typo rules against the specified article text and
    /// returns a formatted timing report.
    /// </summary>
    /// <param name="typos">The regular-expression typo rules to profile.</param>
    /// <param name="text">The article text used for profiling.</param>
    /// <param name="articleName">The article name included in the report.</param>
    /// <returns>A formatted profiling report.</returns>
    private static StringBuilder ProfileTypoRules(
        List<KeyValuePair<Regex, string>> typos,
        string text,
        string articleName)
    {
        int iterations = 1000000 / text.Length;

        if (iterations > 500)
        {
            iterations = 500;
        }

        List<KeyValuePair<int, string>> times = new();

        foreach (KeyValuePair<Regex, string> p in typos)
        {
            Stopwatch watch = new();
            watch.Start();

            for (int i = 0; i < iterations; i++)
            {
                p.Key.IsMatch(text);
            }

            times.Add(
                new KeyValuePair<int, string>(
                    (int)watch.ElapsedMilliseconds,
                    p.Key + " > " + p.Value));
        }

        times.Sort(CompareRegexPairs);

        StringBuilder builder = new();

        builder.AppendLine(
            "Profiling " +
            iterations +
            @" iterations of """ +
            articleName +
            @"""");

        foreach (KeyValuePair<int, string> p in times)
        {
            builder.AppendLine(p.ToString());
        }

        return builder;
    }

    /// <summary>
    /// Opens the plugin loader and allows the user to load a new plugin.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void loadPluginToolStripMenuItem_Click(object sender, EventArgs e)
    {
        PluginManager.LoadNewPlugin(this);
    }

    /// <summary>
    /// Opens the plugin management dialog.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void managePluginsToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        using PluginManager pluginManager = new(this);

        pluginManager.ShowDialog(this);
    }

    /// <summary>
    /// Undoes the most recent edit in the list maker input text box.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void menuitemMakeFromTextBoxUndo_Click(object sender, EventArgs e)
    {
        listMaker.UserInputTextBox.Undo();
    }

    /// <summary>
    /// Cuts the selected text from the list maker input text box.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void menuitemMakeFromTextBoxCut_Click(object sender, EventArgs e)
    {
        listMaker.UserInputTextBox.Cut();
    }

    /// <summary>
    /// Copies the selected text from the list maker input text box.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void menuitemMakeFromTextBoxCopy_Click(object sender, EventArgs e)
    {
        listMaker.UserInputTextBox.Copy();
    }

    /// <summary>
    /// Pastes clipboard contents into the list maker input text box.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void menuitemMakeFromTextBoxPaste_Click(object sender, EventArgs e)
    {
        listMaker.UserInputTextBox.Paste();
    }

    /// <summary>
    /// Opens the C# evaluation dialog.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void cEvalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using CSharpEval cs = new();

        cs.ShowDialog();
    }

    // TODO: Determine category-source behavior from the selected source type
    // rather than matching the displayed source text.
    /// <summary>
    /// Updates list maker controls when the selected list source changes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void ListMakerSourceSelectHandler(object sender, EventArgs e)
    {
        toolStripSeparatorMakeFromTextBox.Visible =
            listMaker.cmboSourceSelect.Text.Contains("Category");
    }

    /// <summary>
    /// Displays the external program processing window.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void externalProcessingToolStripMenuItem_Click(object sender, EventArgs e)
    {
        _externalProgram.Show();
    }

    /// <summary>
    /// Dialog used to obtain a category name from the user.
    /// </summary>
    private readonly CategoryNameForm _catName = new();

    /// <summary>
    /// Prompts the user for a category, verifies that the category exists,
    /// and adds it to the current article when confirmed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void categoryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DialogResult dires = _catName.ShowDialog();

        if (string.IsNullOrEmpty(_catName.CategoryName) ||
            dires != DialogResult.OK)
        {
            return;
        }

        bool pageExists;

        try
        {
            pageExists = CategoryExists(_catName.CategoryName);
        }
        catch
        {
            MessageBox.Show(
                "Unable to check whether the category exists.");

            return;
        }

        if (!pageExists &&
            MessageBox.Show(
                _catName.CategoryName +
                " does not exist. Add it to the page anyway?",
                "Non-existent category",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        txtEdit.Text =
            AddCategoryToArticleText(
                txtEdit.Text,
                _catName.CategoryName);

        ReparseEditBox();
    }

    // TODO(Twain): Move category existence checks and article category
    // manipulation out of MainForm once wiki/API services are extracted.
    /// <summary>
    /// Determines whether the specified category page exists on the current wiki.
    /// </summary>
    /// <param name="categoryName">The category page name to check.</param>
    /// <returns>
    /// <see langword="true"/> if the category page exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool CategoryExists(string categoryName)
    {
        // TODO: Replace this page load with an API-level PageExists helper so
        // callers can check existence without retrieving page content.
        IApiEdit editor =
            TheSession.Editor.SynchronousEditor.Clone();

        editor.Open(categoryName, false);

        return editor.Page.Exists;
    }

    /// <summary>
    /// Adds the specified category to article text and removes any
    /// Uncategorized maintenance template.
    /// </summary>
    /// <param name="articleText">The article text to update.</param>
    /// <param name="categoryName">The category page name to add.</param>
    /// <returns>The updated article text.</returns>
    private static string AddCategoryToArticleText(
        string articleText,
        string categoryName)
    {
        articleText +=
            "\r\n\r\n[[" +
            categoryName +
            "]]";

        // Remove any {{uncategorised}} tag now. The tagger still counts
        // categories based on the saved page revision.
        return WikiRegexes.Uncategorized.Replace(
            articleText,
            string.Empty);
    }

    /// <summary>
    /// Starts the main progress indicator, marshaling to the UI thread when
    /// necessary.
    /// </summary>
    private void StartProgressBar()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(
                new System.Windows.Forms.MethodInvoker(
                    StartProgressBar));

            return;
        }

        MainFormProgressBar.MarqueeAnimationSpeed = 100;
        MainFormProgressBar.Style = ProgressBarStyle.Marquee;
    }

    /// <summary>
    /// Stops the main progress indicator, marshaling to the UI thread when
    /// necessary.
    /// </summary>
    private void StopProgressBar()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(
                new System.Windows.Forms.MethodInvoker(
                    StopProgressBar));

            return;
        }

        MainFormProgressBar.MarqueeAnimationSpeed = 0;
        MainFormProgressBar.Style = ProgressBarStyle.Continuous;
    }

    // TODO: Replace the legacy Crystal Clear icon with Twain branding and
    // update or remove this link as appropriate.
    /// <summary>
    /// Opens the Wikimedia Commons page for the bot icon displayed in the
    /// application.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void BotImage_Click(object sender, EventArgs e)
    {
        Tools.OpenURLInBrowser(
            "https://commons.wikimedia.org/wiki/File:Crystal_Clear_action_run.png");
    }

    /// <summary>
    /// Enables or disables logging of ignored matches (false positives)
    /// based on the current menu item state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void displayfalsePositivesButtonToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        AddIgnoredToLogFile =
            displayfalsePositivesButtonToolStripMenuItem.Checked;
    }

    /// <summary>
    /// Highlights a range of text in the editor using a red background,
    /// adjusting for RichTextBox newline indexing differences.
    /// </summary>
    /// <param name="index">
    /// The zero-based article-text index to highlight.
    /// </param>
    /// <param name="length">
    /// The number of characters to highlight.
    /// </param>
    private void RedSelection(int index, int length)
    {
        if (!txtEdit.Enabled)
        {
            return;
        }

        string text = txtEdit.Text;

        if (index < 0 ||
            length < 0 ||
            index > text.Length ||
            length > text.Length - index)
        {
            Tools.WriteDebug(
                "RedSelection",
                "Ignored invalid highlight range. " +
                "Index: " + index +
                ", Length: " + length +
                ", Text length: " + text.Length);

            return;
        }

        // RichTextBox indexes differ from article-text indexes because
        // RichTextBox stores line endings as CRLF while article text uses LF.
        // Adjust the requested selection by accounting for newline expansion.
        int newlinesToIndex =
            WikiRegexes.Newline.Matches(
                text.Substring(0, index)).Count;

        int newlinesInSelection =
            WikiRegexes.Newline.Matches(
                text.Substring(index, length)).Count;

        txtEdit.SetEditBoxSelection(
            index - newlinesToIndex,
            length - newlinesInSelection,
            false);

        txtEdit.SelectionBackColor = Color.Tomato;
    }

    // TODO: Replace editor-specific selection highlighting with an
    // abstraction that supports both the legacy editor and Monaco.
    /// <summary>
    /// Highlights the specified range of text in the edit box using a yellow
    /// background.
    /// </summary>
    /// <param name="index">
    /// The zero-based starting index of the text to highlight.
    /// </param>
    /// <param name="length">
    /// The number of characters to highlight.
    /// </param>
    private void YellowSelection(int index, int length)
    {
        txtEdit.SetEditBoxSelection(index, length);
        txtEdit.SelectionBackColor = Color.Yellow;
    }

    /// <summary>
    /// Highlights all matches for the current Find expression in the editor
    /// and then restores the selection to the beginning of the document.
    /// </summary>
    private void HighlightAllFind()
    {
        if (string.IsNullOrEmpty(txtFind.Text) ||
            TheArticle == null)
        {
            return;
        }

        Dictionary<int, int> found =
            txtEdit.FindAll(
                txtFind.Text,
                chkFindRegex.Checked,
                chkFindCaseSensitive.Checked,
                TheArticle.Name);

        foreach (KeyValuePair<int, int> match in found)
        {
            YellowSelection(
                match.Key,
                match.Value);
        }

        txtEdit.SetEditBoxSelection(0, 0);
        txtEdit.Select(0, 0);
        txtEdit.ScrollToCaret();
    }

    /// <summary>
    /// Prevents configuration files opened from the Internet cache from being
    /// loaded directly and warns the user about the risk of executing untrusted
    /// custom module code.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    /// The file dialog event data used to cancel loading of an unsafe file.
    /// </param>
    private void openXML_FileOk(object sender, CancelEventArgs e)
    {
        if (openXML.FileName.StartsWith(
            Environment.GetFolderPath(
                Environment.SpecialFolder.InternetCache)))
        {
            // What, no <big>, <font color="red"> and <blink>?
            MessageBox.Show(
                this,
                "Please review the custom module code and save the config on your PC manually.\r\n" +
                "DON'T TRUST ANYTHING YOU FIND ON THE INTERNET UNLESS YOU UNDERSTAND WHAT IT DOES.\r\n" +
                "Failure to abide by this may result in arbitrary code execution on your machine.",
                "Security warning - READ THIS",
                MessageBoxButtons.OK,
                MessageBoxIcon.Hand);

            e.Cancel = true;
        }
    }

    /// <summary>
    /// Invalidates the global object cache, forcing cached data to be
    /// reloaded as needed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void invalidateCacheToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ObjectCache.Global.Invalidate();
    }

    // TODO: Consider centralizing common confirmation dialogs to provide a
    // consistent user experience throughout the application.
    /// <summary>
    /// Prompts the user to confirm clearing the current page list and, if
    /// confirmed, removes all entries from the active list.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void clearCurrentListToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listMaker.Any() &&
            MessageBox.Show(
                this,
                "Do you want to clear the current list?",
                "Clear current list",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
        {
            listMaker.Clear();
        }
    }

    /// <summary>
    /// Logs out the current user, clears the active article state, and updates
    /// the user interface to reflect the completed logout.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void logOutToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (TheSession.IsBusy)
        {
            TheSession.Editor.Abort();
        }

        TheArticle = null;
        txtEdit.Text = string.Empty;

        TheSession.Editor.Logout();

        // Logout runs asynchronously through AsyncApiEdit. Wait here so
        // CheckStatus and UpdateStatusUI observe the completed logged-out
        // editor state rather than the previous logged-in state.
        TheSession.Editor.Wait();

        CheckStatus(true);
        UpdateStatusUI();

        StopProgressBar();
        DisableButtons();
    }

    /// <summary>
    /// Opens the profile management dialog.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void lblUserName_Click(object sender, EventArgs e)
    {
        _profiles.ShowDialog(this);
    }

    /// <summary>
    /// Opens the current user's notifications page in the default browser
    /// when notifications are enabled and the user is logged in.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void lblUserNotifications_Click(object sender, EventArgs e)
    {
        if (Variables.NotificationsEnabled && TheSession.User.IsLoggedIn)
        {
            Tools.OpenArticleInBrowser("Special:Notifications");
        }
    }

    /// <summary>
    /// Displays context-sensitive help for status bar items.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void statusBar_MouseHover(
        object sender,
        EventArgs e)
    {
        if (sender is not ToolStripStatusLabel item)
        {
            return;
        }

        AWBToolTip toolTip = new();

        string text = string.Empty;

        switch (item.Name)
        {
            case "lblUserName":
                text = "Click to switch user";
                break;

            case "lblProject":
                text = "Click to switch project";
                break;

            case "lblUserNotifications":
                text = "User notifications";
                break;
        }

        toolTip.Show(text, item.Owner);
    }

    /// <summary>
    /// Displays context-sensitive help for toolbar buttons.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void editToolBar_MouseHover(
        object sender,
        EventArgs e)
    {
        if (sender is not ToolStripButton item)
        {
            return;
        }

        AWBToolTip tt = new();

        string text = string.Empty;

        switch (item.Name)
        {
            case "btntsDelete":
                text = "Delete this page";
                break;
            case "btntsIgnore":
                text = "Skip this page without saving and continue on the next";
                break;
            case "btntsSave":
                text = "Save your changes and continue";
                break;
            case "btntsChanges":
                text = "Preview your changes; please use this before saving.";
                break;
            case "btntsPreview":
                text = "Preview your changes";
                break;
            case "btntsStop":
                text = "Stops everything";
                break;
            case "btntsStart":
                text = "Start processing pages";
                break;
            case "btntsShowHideParameters":
                text = "Make the edit box span bottom of window";
                break;
            case "btntsShowHide":
                text = "Show or hide the panel";
                break;
            case "btntsFalsePositive":
                text = "Add to false positives file";
                break;
        }

        tt.Show(text, item.Owner);
    }

    /// <summary>
    /// Opens the Preferences dialog with the project settings page selected.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void lblProject_Click(object sender, EventArgs e)
    {
        OpenPreferences(true);
    }
    /// <summary>
    /// Gets the number of edits completed during the current session.
    /// </summary>
    public int NumberOfEdits
    {
        get => _sessionCounters.NumberOfEdits;
        private set
        {
            _sessionCounters.NumberOfEdits = value;
            lblEditCount.Text = $"Edits: {value}";
        }
    }

    /// <summary>
    /// Gets the number of new pages processed during the current session.
    /// </summary>
    public int NumberOfNewPages
    {
        get => _sessionCounters.NumberOfNewPages;
        private set
        {
            _sessionCounters.NumberOfNewPages = value;
            lblNewArticles.Text = $"New: {value}";
        }
    }

    /// <summary>
    /// Gets the number of edits skipped during the current session.
    /// </summary>
    public int NumberOfIgnoredEdits
    {
        get => _sessionCounters.NumberOfIgnoredEdits;
        private set
        {
            _sessionCounters.NumberOfIgnoredEdits = value;
            lblIgnoredArticles.Text = $"Skipped: {value}";
        }
    }

    /// <summary>
    /// Gets the current number of edits completed per minute.
    /// </summary>
    public int NumberOfEditsPerMinute
    {
        get => _sessionCounters.NumberOfEditsPerMinute;
        private set
        {
            _sessionCounters.NumberOfEditsPerMinute = value;
            lblEditsPerMin.Text = $"Edits/min: {value}";
        }
    }

    /// <summary>
    /// Gets the current number of pages processed per minute.
    /// </summary>
    public int NumberOfPagesPerMinute
    {
        get => _sessionCounters.NumberOfPagesPerMinute;
        private set
        {
            _sessionCounters.NumberOfPagesPerMinute = value;
            lblPagesPerMin.Text = $"Pages/min: {value}";
        }
    }
}
    #endregion