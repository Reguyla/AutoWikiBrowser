using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Linq;
using Twain.Core;
using Twain.Core.Settings;

namespace Twain.UI.Preferences;

/// <summary>
/// Displays and edits application preferences.
/// </summary>
public partial class PreferencesWindow : Window
{
    /// <summary>
    /// Initializes a new preferences window.
    /// </summary>
    public PreferencesWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Accepts the current preferences and closes the window.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)

    {
        ValidateAutoSaveSettings();

        if (ProjectComboBox.SelectedItem is ProjectEnum project &&
            UserSettingsHelper.UsesCustomProjectControls(project))
        {
            CustomProjectComboBox.Text =
                UserSettingsHelper.NormalizeCustomProject(
                    CustomProjectComboBox.Text,
                    project);
        }

        UpdateOkButtonState();

        if (!OkButton.IsEnabled)
        {
            return;
        }

        Close(true);
    }

    /// <summary>
    /// Cancels preference changes and closes the window.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    /// <summary>
    /// Initializes a new preferences window with the supplied site settings.
    /// </summary>
    /// <param name="language">
    /// The currently selected wiki language.
    /// </param>
    /// <param name="project">
    /// The currently selected wiki project.
    /// </param>
    /// <param name="customProject">
    /// The currently configured custom project.
    /// </param>
    /// <param name="protocol">
    /// The currently selected connection protocol.
    /// </param>
    public PreferencesWindow(
        string language,
        ProjectEnum project,
        string customProject,
        string protocol)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(customProject);
        ArgumentNullException.ThrowIfNull(protocol);

        InitializeComponent();

        InitializeProjectSelection(project);
        InitializeLanguageSelection(language);
        InitializeCustomProject(customProject);
        InitializeProtocolSelection(protocol);
    }

    /// <summary>
    /// Populates the project selector and selects the supplied project.
    /// </summary>
    private void InitializeProjectSelection(
        ProjectEnum project)
    {
        ProjectComboBox.ItemsSource =
            Enum.GetValues<ProjectEnum>();

        ProjectComboBox.SelectedItem =
            project;

        UpdateProjectSelection();
    }

    /// <summary>
    /// Updates the site controls for the currently selected wiki project.
    /// </summary>
    private void UpdateProjectSelection()
    {
        if (ProjectComboBox.SelectedItem is not ProjectEnum project)
        {
            return;
        }

        UserSettingsHelper.ProjectSettingsState state =
            UserSettingsHelper.GetProjectSettingsState(
                project);

        string? selectedLanguage =
            LanguageComboBox.SelectedItem?.ToString();

        LanguageComboBox.ItemsSource =
            state.Languages;

        if (!string.IsNullOrEmpty(selectedLanguage) &&
            state.Languages.Contains(selectedLanguage))
        {
            LanguageComboBox.SelectedItem =
                selectedLanguage;
        }

        LanguageComboBox.IsEnabled =
            state.SupportsLanguageSelection;

        LanguageLabel.IsVisible =
            !state.UsesCustomProjectControls;

        LanguageComboBox.IsVisible =
            !state.UsesCustomProjectControls;

        CustomProjectLabel.IsVisible =
            state.UsesCustomProjectControls;

        CustomProjectComboBox.IsVisible =
            state.UsesCustomProjectControls;

        ProtocolLabel.IsVisible =
            state.UsesCustomProjectControls;

        ProtocolComboBox.IsVisible =
            state.UsesCustomProjectControls;

        PostfixLabel.IsVisible =
            state.UsesCustomProjectControls;

        PostfixLabel.Text =
            state.ProjectPostfix;

        ProtocolComboBox.IsEnabled =
            state.SupportsCustomConnectionSettings;

        UpdateDomainControls();

        SuppressAwbCheckBox.IsEnabled =
            state.SupportsCustomConnectionSettings;

        UpdateOkButtonState();
    }

    /// <summary>
    /// Updates project-specific controls when the selected wiki project changes.
    /// </summary>
    private void ProjectComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        UpdateProjectSelection();
    }

    /// <summary>
    /// Populates the language selector for the selected project and selects the
    /// supplied language.
    /// </summary>
    private void InitializeLanguageSelection(
        string language)
    {
        string normalizedLanguage =
            UserSettingsHelper.NormalizeLanguageCode(
                language);

        UserSettingsHelper.ProjectSettingsState state =
            UserSettingsHelper.GetProjectSettingsState(
                project: Project);

        LanguageComboBox.ItemsSource =
            state.Languages;

        LanguageComboBox.SelectedItem =
            normalizedLanguage;
    }

    /// <summary>
    /// Initializes the custom-project selector.
    /// </summary>
    private void InitializeCustomProject(
        string customProject)
    {
        CustomProjectComboBox.Text =
            customProject;
    }

    /// <summary>
    /// Initializes the protocol selector.
    /// </summary>
    private void InitializeProtocolSelection(
        string protocol)
    {
        ProtocolComboBox.SelectedIndex =
            UserSettingsHelper.GetProtocolSelectionIndex(
                protocol);
    }

    /// <summary>
    /// Gets the currently selected wiki project.
    /// </summary>
    public ProjectEnum Project
    {
        get
        {
            if (ProjectComboBox.SelectedItem is ProjectEnum project)
            {
                return project;
            }

            throw new InvalidOperationException(
                "No wiki project is currently selected.");
        }
    }

    /// <summary>
    /// Updates the domain controls for the current project and checkbox state.
    /// </summary>
    private void UpdateDomainControls()
    {
        if (ProjectComboBox.SelectedItem is not ProjectEnum project)
        {
            DomainCheckBox.IsEnabled = false;
            DomainTextBox.IsEnabled = false;
            return;
        }

        UserSettingsHelper.ProjectSettingsState state =
            UserSettingsHelper.GetProjectSettingsState(
                project);

        DomainCheckBox.IsEnabled =
            state.SupportsCustomConnectionSettings;

        DomainTextBox.IsEnabled =
            state.SupportsCustomConnectionSettings &&
            DomainCheckBox.IsChecked == true;
    }

    /// <summary>
    /// Updates the custom domain field when its checkbox state changes.
    /// </summary>
    private void DomainCheckBox_IsCheckedChanged(
        object? sender,
        RoutedEventArgs e)
    {
        UpdateDomainControls();
    }

    /// <summary>
    /// Updates whether the current preference settings can be accepted.
    /// </summary>
    private void UpdateOkButtonState()
    {
        if (ProjectComboBox.SelectedItem is not ProjectEnum project)
        {
            OkButton.IsEnabled = false;
            return;
        }

        UserSettingsHelper.ProjectSettingsState state =
            UserSettingsHelper.GetProjectSettingsState(
                project);

        OkButton.IsEnabled =
            !state.RequiresCustomProject ||
            !string.IsNullOrWhiteSpace(
                CustomProjectComboBox.Text);
    }

    /// <summary>
    /// Updates preference validation after custom-project editing completes.
    /// </summary>
    private void CustomProjectComboBox_LostFocus(
        object? sender,
        RoutedEventArgs e)
    {
        UpdateOkButtonState();
    }

    /// <summary>
    /// Gets the currently selected wiki language.
    /// </summary>
    public string Language =>
        LanguageComboBox.SelectedItem?.ToString() ??
        string.Empty;

    /// <summary>
    /// Gets the currently configured custom project.
    /// </summary>
    public string CustomProject =>
        UserSettingsHelper.NormalizeCustomProject(
            CustomProjectComboBox.Text,
            Project);

    /// <summary>
    /// Gets the currently selected connection protocol.
    /// </summary>
    /// <summary>
    /// Gets the currently selected connection protocol.
    /// </summary>
    public string Protocol =>
        ProtocolComboBox.SelectedIndex == 1
            ? "http://"
            : "https://";

    /// <summary>
    /// Gets or sets whether AWB identification should be suppressed.
    /// </summary>
    public bool PrefSuppressUsingAWB
    {
        get => SuppressAwbCheckBox.IsChecked == true;
        set => SuppressAwbCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets the custom domain.
    /// </summary>
    public string PrefDomain
    {
        get => DomainTextBox.Text ?? string.Empty;

        set
        {
            DomainTextBox.Text =
                value ?? string.Empty;

            DomainCheckBox.IsChecked =
                !string.IsNullOrEmpty(value);

            UpdateDomainControls();
        }
    }

    /// <summary>
    /// Updates settings that depend on the selected startup behavior.
    /// </summary>
    private void UpdateOnLoadState()
    {
        int selection =
            UserSettingsHelper.NormalizeOnLoadSelection(
                OnLoadComboBox.SelectedIndex);

        DiffInBotModeCheckBox.IsEnabled =
            UserSettingsHelper.SupportsBotModeDiff(
                selection);
    }

    /// <summary>
    /// Updates dependent settings when the startup behavior changes.
    /// </summary>
    private void OnLoadComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        UpdateOnLoadState();
    }

    /// <summary>
    /// Gets or sets whether application logging is enabled.
    /// </summary>
    public bool EnableLogging
    {
        get => LoggingCheckBox.IsChecked == true;
        set => LoggingCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the application should minimize when appropriate.
    /// </summary>
    public bool PrefMinimize
    {
        get => MinimizeCheckBox.IsChecked == true;
        set => MinimizeCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the application should use low thread priority.
    /// </summary>
    public bool LowThreadPriority
    {
        get => LowThreadPriorityCheckBox.IsChecked == true;
        set => LowThreadPriorityCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the article list should be saved.
    /// </summary>
    public bool PrefSaveArticleList
    {
        get => SaveArticleListCheckBox.IsChecked == true;
        set => SaveArticleListCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether confirmation is required before exiting.
    /// </summary>
    public bool PrefAskForTerminate
    {
        get => ConfirmExitCheckBox.IsChecked == true;
        set => ConfirmExitCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the page list should be cleared when the project changes.
    /// </summary>
    public bool PrefClearPageListOnProjectChange
    {
        get => ClearPageListOnProjectChangeCheckBox.IsChecked == true;
        set => ClearPageListOnProjectChangeCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets the startup behavior selection.
    /// </summary>
    public int PrefOnLoad
    {
        get =>
            UserSettingsHelper.NormalizeOnLoadSelection(
                OnLoadComboBox.SelectedIndex);

        set
        {
            OnLoadComboBox.SelectedIndex =
                UserSettingsHelper.NormalizeOnLoadSelection(
                    value);

            UpdateOnLoadState();
        }
    }

    /// <summary>
    /// Gets or sets whether differences should be shown in bot mode.
    /// </summary>
    public bool PrefDiffInBotMode
    {
        get => DiffInBotModeCheckBox.IsChecked == true;
        set => DiffInBotModeCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the timer should be displayed.
    /// </summary>
    public bool PrefShowTimer
    {
        get => ShowTimerCheckBox.IsChecked == true;
        set => ShowTimerCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Updates controls associated with automatic edit-box saving.
    /// </summary>
    private void UpdateAutoSaveControls()
    {
        bool enabled =
            AutoSaveEditCheckBox.IsChecked == true;

        AutoSaveFileTextBox.IsEnabled =
            enabled;

        SetAutoSaveFileButton.IsEnabled =
            enabled;

        AutoSavePeriodLabel.IsEnabled =
            enabled;

        AutoSavePeriodNumericUpDown.IsEnabled =
            enabled;
    }

    /// <summary>
    /// Updates autosave controls when automatic saving is enabled or disabled.
    /// </summary>
    private void AutoSaveEditCheckBox_IsCheckedChanged(
        object? sender,
        RoutedEventArgs e)
    {
        UpdateAutoSaveControls();
    }

    /// <summary>
    /// Gets or sets whether AWB identification should be added on article actions.
    /// </summary>
    public bool PrefAddUsingAWBOnArticleAction
    {
        get => AddUsingAwbCheckBox.IsChecked == true;
        set => AddUsingAwbCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether nobots restrictions should be ignored.
    /// </summary>
    public bool PrefIgnoreNoBots
    {
        get => IgnoreNoBotsCheckBox.IsChecked == true;
        set => IgnoreNoBotsCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the window should flash when attention is required.
    /// </summary>
    public bool PrefFlash
    {
        get => FlashCheckBox.IsChecked == true;
        set => FlashCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether a beep should sound when attention is required.
    /// </summary>
    public bool PrefBeep
    {
        get => BeepCheckBox.IsChecked == true;
        set => BeepCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether automatic edit-box saving is enabled.
    /// </summary>
    public bool PrefAutoSaveEditBoxEnabled
    {
        get => AutoSaveEditCheckBox.IsChecked == true;

        set
        {
            AutoSaveEditCheckBox.IsChecked = value;
            UpdateAutoSaveControls();
        }
    }

    /// <summary>
    /// Gets or sets the autosave interval in minutes.
    /// </summary>
    public int PrefAutoSaveEditBoxPeriod
    {
        get =>
            Convert.ToInt32(
                AutoSavePeriodNumericUpDown.Value ?? 1);

        set =>
            AutoSavePeriodNumericUpDown.Value =
                value;
    }

    /// <summary>
    /// Gets or sets the autosave file path.
    /// </summary>
    public string PrefAutoSaveEditBoxFile
    {
        get => AutoSaveFileTextBox.Text ?? string.Empty;
        set => AutoSaveFileTextBox.Text = value ?? string.Empty;
    }

    /// <summary>
    /// Validates the automatic edit-box saving configuration.
    /// </summary>
    private void ValidateAutoSaveSettings()
    {
        PrefAutoSaveEditBoxEnabled =
            UserSettingsHelper.NormalizeAutoSaveEnabled(
                PrefAutoSaveEditBoxEnabled,
                PrefAutoSaveEditBoxFile);
    }

    /// <summary>
    /// Prompts for the file used to automatically save the edit-box contents.
    /// </summary>
    private async void SetAutoSaveFileButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Select autosave file",
                    SuggestedFileName = "autosave.txt",
                    DefaultExtension = "txt",
                    FileTypeChoices =
                    [
                        new FilePickerFileType("Text files")
                    {
                        Patterns = ["*.txt"]
                    },
                    FilePickerFileTypes.All
                    ]
                });

        if (file is null)
        {
            return;
        }

        PrefAutoSaveEditBoxFile =
            file.Path.LocalPath;
    }

    /// <summary>
    /// Gets or sets how List Comparer uses the current article list.
    /// </summary>
    public int PrefListComparerUseCurrentArticleList
    {
        get => ListComparerArticleListComboBox.SelectedIndex;
        set => ListComparerArticleListComboBox.SelectedIndex = value;
    }

    /// <summary>
    /// Gets or sets how List Splitter uses the current article list.
    /// </summary>
    public int PrefListSplitterUseCurrentArticleList
    {
        get => ListSplitterArticleListComboBox.SelectedIndex;
        set => ListSplitterArticleListComboBox.SelectedIndex = value;
    }

    /// <summary>
    /// Gets or sets how Database Scanner uses the current article list.
    /// </summary>
    public int PrefDBScannerUseCurrentArticleList
    {
        get => DatabaseScannerArticleListComboBox.SelectedIndex;
        set => DatabaseScannerArticleListComboBox.SelectedIndex = value;
    }
}