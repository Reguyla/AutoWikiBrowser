using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Linq;
using Twain.Core;
using Twain.Core.Alerts;
using Twain.Core.Settings;

namespace Twain.UI.Settings;

/// <summary>
/// Displays and edits user-configurable application settings.
/// </summary>
public partial class UserSettingsWindow : Avalonia.Controls.Window
{
    private readonly Dictionary<int, Avalonia.Controls.CheckBox> _alertCheckBoxes =
        new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsWindow"/> class
    /// using preview-safe default values.
    /// </summary>
    public UserSettingsWindow()
        : this(
            string.Empty,
            ProjectEnum.wikipedia,
            string.Empty,
            "https://")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsWindow"/> class.
    /// </summary>
    /// <param name="language">
    /// The selected wiki language code.
    /// </param>
    /// <param name="project">
    /// The selected wiki project.
    /// </param>
    /// <param name="customProject">
    /// The selected custom wiki project.
    /// </param>
    /// <param name="protocol">
    /// The selected connection protocol.
    /// </param>
    /// <param name="enabledAlertIds">
    /// The stored enabled alert identifiers. An empty or unspecified collection
    /// represents the legacy default in which all alerts are enabled.
    /// </param>
    public UserSettingsWindow(
        string language,
        ProjectEnum project,
        string customProject,
        string protocol,
        IEnumerable<int>? enabledAlertIds = null)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(customProject);
        ArgumentNullException.ThrowIfNull(protocol);

        InitializeComponent();

        InitializeSelectors();
        InitializeAlerts(enabledAlertIds);

        InitializeSiteSettings(
            language,
            project,
            customProject,
            protocol);
    }

    /// <summary>
    /// Gets or sets the enabled alert identifiers.
    /// </summary>
    /// <remarks>
    /// An empty or unspecified selection represents the legacy default in
    /// which all available alerts are enabled.
    /// </remarks>
    public List<int> AlertPreferences
    {
        get =>
            _alertCheckBoxes
                .Where(item => item.Value.IsChecked == true)
                .Select(item => item.Key)
                .ToList();

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            List<int> enabledAlertIds =
                ArticleAlertHelper.ResolveEnabledAlertIds(
                    _alertCheckBoxes.Keys,
                    value);

            foreach (KeyValuePair<int, Avalonia.Controls.CheckBox> alert in
                     _alertCheckBoxes)
            {
                alert.Value.IsChecked =
                    ArticleAlertHelper.IsAlertEnabled(
                        enabledAlertIds,
                        alert.Key);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether privacy mode is enabled.
    /// </summary>
    public bool PrivacyEnabled
    {
        get =>
            UserSettingsHelper.GetPrivacySetting(
                PrivacyCheckBox.IsChecked == true);

        set =>
            PrivacyCheckBox.IsChecked =
                UserSettingsHelper.GetPrivacyCheckboxState(
                    value);
    }

    /// <summary>
    /// Gets or sets the selected article-load mode.
    /// </summary>
    public int OnLoadSelection
    {
        get =>
            UserSettingsHelper.NormalizeOnLoadSelection(
                OnLoadComboBox.SelectedIndex);

        set
        {
            OnLoadComboBox.SelectedIndex =
                UserSettingsHelper.NormalizeOnLoadSelection(
                    value);

            DiffInBotModeCheckBox.IsEnabled =
                UserSettingsHelper.SupportsBotModeDiff(
                    OnLoadComboBox.SelectedIndex);
        }
    }

    /// <summary>
    /// Gets or sets whether differences are shown in bot mode.
    /// </summary>
    public bool DiffInBotMode
    {
        get =>
            DiffInBotModeCheckBox.IsChecked == true;

        set =>
            DiffInBotModeCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets the List Comparer article-list behavior.
    /// </summary>
    public CurrentArticleListMode ListComparerMode
    {
        get =>
            ListComparerModeComboBox.SelectedItem is CurrentArticleListMode mode
                ? mode
                : CurrentArticleListMode.Ask;

        set =>
            ListComparerModeComboBox.SelectedItem = value;
    }

    /// <summary>
    /// Gets or sets the List Splitter article-list behavior.
    /// </summary>
    public CurrentArticleListMode ListSplitterMode
    {
        get =>
            ListSplitterModeComboBox.SelectedItem is CurrentArticleListMode mode
                ? mode
                : CurrentArticleListMode.Ask;

        set =>
            ListSplitterModeComboBox.SelectedItem = value;
    }

    /// <summary>
    /// Gets or sets the Database Scanner article-list behavior.
    /// </summary>
    public CurrentArticleListMode DatabaseScannerMode
    {
        get =>
            DatabaseScannerModeComboBox.SelectedItem is CurrentArticleListMode mode
                ? mode
                : CurrentArticleListMode.Ask;

        set =>
            DatabaseScannerModeComboBox.SelectedItem = value;
    }

    /// <summary>
    /// Populates selectors whose available values are fixed by the application.
    /// </summary>
    private void InitializeSelectors()
    {
        ProjectComboBox.ItemsSource =
            Enum.GetValues<ProjectEnum>();

        CurrentArticleListMode[] articleListModes =
            Enum.GetValues<CurrentArticleListMode>();

        ListComparerModeComboBox.ItemsSource =
            articleListModes;

        ListSplitterModeComboBox.ItemsSource =
            articleListModes;

        DatabaseScannerModeComboBox.ItemsSource =
            articleListModes;

        OnLoadComboBox.ItemsSource =
            new[]
            {
                "Show changes",
                "Show preview"
            };
    }

    /// <summary>
    /// Populates the alert selector from the shared alert metadata.
    /// </summary>
    private void InitializeAlerts(
        IEnumerable<int>? selectedAlertIds)
    {
        AlertsPanel.Children.Clear();
        _alertCheckBoxes.Clear();

        IReadOnlyCollection<int> availableAlertIds =
            ArticleAlertHelper.AlertDescriptions
                .Keys
                .Select(id => (int)id)
                .ToArray();

        IReadOnlyCollection<int> enabledAlertIds =
            ArticleAlertHelper.ResolveEnabledAlertIds(
                availableAlertIds,
                selectedAlertIds ?? Array.Empty<int>());

        foreach (KeyValuePair<ArticleAlertId, string> alert in
                 ArticleAlertHelper.AlertDescriptions)
        {
            int alertId = (int)alert.Key;

            Avalonia.Controls.CheckBox checkBox =
                new()
                {
                    Content = alert.Value,
                    IsChecked =
                        ArticleAlertHelper.IsAlertEnabled(
                            enabledAlertIds,
                            alertId)
                };

            _alertCheckBoxes.Add(
                alertId,
                checkBox);

            AlertsPanel.Children.Add(
                checkBox);
        }
    }

    /// <summary>
    /// Applies the current site-selection settings.
    /// </summary>
    private void InitializeSiteSettings(
        string language,
        ProjectEnum project,
        string customProject,
        string protocol)
    {
        ProjectComboBox.SelectedItem =
            project;

        PopulateLanguages(
            project,
            UserSettingsHelper.NormalizeLanguageCode(
                language));

        CustomProjectComboBox.Text =
            customProject;

        ProtocolComboBox.SelectedIndex =
            UserSettingsHelper.GetProtocolSelectionIndex(
                protocol);

        UpdateProjectControls(project);
    }

    /// <summary>
    /// Populates the language selector for the specified wiki project.
    /// </summary>
    /// <param name="project">
    /// The selected wiki project.
    /// </param>
    /// <param name="selectedLanguage">
    /// The language code to preserve when possible.
    /// </param>
    private void PopulateLanguages(
        ProjectEnum project,
        string? selectedLanguage = null)
    {
        selectedLanguage ??=
            LanguageComboBox.SelectedItem?.ToString();

        LanguageComboBox.ItemsSource =
            UserSettingsHelper.GetLanguagesForProject(
                project);

        if (!string.IsNullOrEmpty(selectedLanguage))
        {
            LanguageComboBox.SelectedItem =
                selectedLanguage;
        }
    }

    /// <summary>
    /// Updates the available site settings when the selected wiki project changes.
    /// </summary>
    private void ProjectComboBox_SelectionChanged(
        object? sender,
        Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (ProjectComboBox.SelectedItem is not ProjectEnum project)
        {
            return;
        }

        PopulateLanguages(project);
        UpdateProjectControls(project);
    }

    /// <summary>
    /// Updates processing options that depend on the selected article-load mode.
    /// </summary>
    private void OnLoadComboBox_SelectionChanged(
        object? sender,
        Avalonia.Controls.SelectionChangedEventArgs e)
    {
        DiffInBotModeCheckBox.IsEnabled =
            UserSettingsHelper.SupportsBotModeDiff(
                OnLoadComboBox.SelectedIndex);
    }

    /// <summary>
    /// Accepts the current settings and closes the window.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Discards changes and closes the window.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    /// <summary>
    /// Gets or sets whether AWB attribution is added to action summaries.
    /// </summary>
    public bool AddUsingAwbToActionSummaries
    {
        get =>
            AddUsingAwbToActionSummariesCheckBox.IsChecked == true;

        set =>
            AddUsingAwbToActionSummariesCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether confirmation is required before exiting.
    /// </summary>
    public bool AlwaysConfirmExit
    {
        get => AlwaysConfirmExitCheckBox.IsChecked == true;
        set => AlwaysConfirmExitCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the application runs at low process priority.
    /// </summary>
    public bool LowPriority
    {
        get => LowPriorityCheckBox.IsChecked == true;
        set => LowPriorityCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the application flashes when processing completes.
    /// </summary>
    public bool Flash
    {
        get => FlashCheckBox.IsChecked == true;
        set => FlashCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the application beeps when processing completes.
    /// </summary>
    public bool Beep
    {
        get => BeepCheckBox.IsChecked == true;
        set => BeepCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the application minimizes after processing begins.
    /// </summary>
    public bool Minimize
    {
        get => MinimizeCheckBox.IsChecked == true;
        set => MinimizeCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether logging is enabled.
    /// </summary>
    public bool LoggingEnabled
    {
        get => LoggingCheckBox.IsChecked == true;
        set => LoggingCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the article list is saved.
    /// </summary>
    public bool SaveArticleList
    {
        get => SaveArticleListCheckBox.IsChecked == true;
        set => SaveArticleListCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether no-bots restrictions are ignored.
    /// </summary>
    public bool IgnoreNoBots
    {
        get => IgnoreNoBotsCheckBox.IsChecked == true;
        set => IgnoreNoBotsCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the processing timer is displayed.
    /// </summary>
    public bool ShowTimer
    {
        get => ShowTimerCheckBox.IsChecked == true;
        set => ShowTimerCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether the page list is cleared when the project changes.
    /// </summary>
    public bool ClearPageListOnProjectChange
    {
        get => ClearPageListCheckBox.IsChecked == true;
        set => ClearPageListCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether article text is automatically saved while editing.
    /// </summary>
    public bool AutoSaveEdit
    {
        get => AutoSaveEditCheckBox.IsChecked == true;
        set => AutoSaveEditCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets the autosave interval in seconds.
    /// </summary>
    public int AutoSavePeriod
    {
        get => (int)(AutoSavePeriodNumericUpDown.Value ?? 30);

        set =>
            AutoSavePeriodNumericUpDown.Value =
                Math.Clamp(value, 30, 300);
    }

    /// <summary>
    /// Gets or sets the file used for automatic article-text saves.
    /// </summary>
    public string AutoSaveFile
    {
        get => AutoSaveFileTextBox.Text ?? string.Empty;
        set => AutoSaveFileTextBox.Text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the selected wiki project.
    /// </summary>
    public ProjectEnum Project
    {
        get =>
            ProjectComboBox.SelectedItem is ProjectEnum project
                ? project
                : ProjectEnum.wikipedia;

        set
        {
            ProjectComboBox.SelectedItem = value;
            PopulateLanguages(value);
            UpdateProjectControls(value);
        }
    }

    /// <summary>
    /// Gets or sets the selected wiki language code.
    /// </summary>
    public string Language
    {
        get =>
            LanguageComboBox.SelectedItem?.ToString() ??
            string.Empty;

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            string normalizedLanguage =
                UserSettingsHelper.NormalizeLanguageCode(
                    value);

            if (ProjectComboBox.SelectedItem is ProjectEnum project)
            {
                PopulateLanguages(
                    project,
                    normalizedLanguage);
            }
        }
    }

    /// <summary>
    /// Gets or sets the custom wiki project value.
    /// </summary>
    public string CustomProject
    {
        get =>
            CustomProjectComboBox.Text ??
            string.Empty;

        set =>
            CustomProjectComboBox.Text =
                value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the selected connection protocol.
    /// </summary>
    public string Protocol
    {
        get =>
            ProtocolComboBox.SelectedIndex == 1
                ? "http://"
                : "https://";

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            ProtocolComboBox.SelectedIndex =
                UserSettingsHelper.GetProtocolSelectionIndex(
                    value);
        }
    }

    /// <summary>
    /// Updates domain editing when the custom-domain option changes.
    /// </summary>
    private void UseCustomDomainCheckBox_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (ProjectComboBox.SelectedItem is not ProjectEnum project)
        {
            return;
        }

        DomainTextBox.IsEnabled =
            UserSettingsHelper.SupportsCustomConnectionSettings(
                project) &&
            UseCustomDomainCheckBox.IsChecked == true;
    }

    /// <summary>
    /// Updates validation when the custom-project value changes.
    /// </summary>
    private void CustomProjectComboBox_PropertyChanged(
        object? sender,
        Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property ==
            Avalonia.Controls.ComboBox.TextProperty)
        {
            UpdateOkButtonState();
        }
    }

    /// <summary>
    /// Updates project-specific control visibility and availability.
    /// </summary>
    private void UpdateProjectControls(
        ProjectEnum project)
    {
        bool usesCustomProjectControls =
            UserSettingsHelper.UsesCustomProjectControls(
                project);

        bool supportsCustomConnectionSettings =
            UserSettingsHelper.SupportsCustomConnectionSettings(
                project);

        LanguageComboBox.IsEnabled =
            UserSettingsHelper.SupportsLanguageSelection(
                project);

        LanguageComboBox.IsVisible =
            !usesCustomProjectControls;

        CustomProjectComboBox.IsVisible =
            usesCustomProjectControls;

        ProjectPostfixTextBlock.IsVisible =
            usesCustomProjectControls;

        ProjectPostfixTextBlock.Text =
            UserSettingsHelper.GetProjectPostfix(
                project);

        ProtocolComboBox.IsVisible =
            usesCustomProjectControls;

        ProtocolComboBox.IsEnabled =
            supportsCustomConnectionSettings;

        UseCustomDomainCheckBox.IsEnabled =
            supportsCustomConnectionSettings;

        DomainTextBox.IsEnabled =
            supportsCustomConnectionSettings &&
            UseCustomDomainCheckBox.IsChecked == true;

        SuppressAwbCheckBox.IsEnabled =
            supportsCustomConnectionSettings;

        if (UserSettingsHelper.RequiresHttps(project))
        {
            ProtocolComboBox.SelectedIndex = 0;
        }

        UpdateOkButtonState();
    }

    /// <summary>
    /// Gets or sets whether a custom domain is used.
    /// </summary>
    public bool UseCustomDomain
    {
        get =>
            UseCustomDomainCheckBox.IsChecked == true;

        set
        {
            UseCustomDomainCheckBox.IsChecked = value;

            if (ProjectComboBox.SelectedItem is ProjectEnum project)
            {
                DomainTextBox.IsEnabled =
                    UserSettingsHelper.SupportsCustomConnectionSettings(
                        project) &&
                    value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the custom domain.
    /// </summary>
    public string Domain
    {
        get =>
            DomainTextBox.Text ??
            string.Empty;

        set =>
            DomainTextBox.Text =
                value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets whether AWB attribution is suppressed for supported custom sites.
    /// </summary>
    public bool SuppressAwb
    {
        get =>
            SuppressAwbCheckBox.IsChecked == true;

        set =>
            SuppressAwbCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Updates whether the current site settings contain enough information
    /// for the dialog to be accepted.
    /// </summary>
    private void UpdateOkButtonState()
    {
        if (ProjectComboBox.SelectedItem is not ProjectEnum project)
        {
            OkButton.IsEnabled = false;
            return;
        }

        OkButton.IsEnabled =
            !UserSettingsHelper.RequiresCustomProject(project) ||
            !string.IsNullOrWhiteSpace(CustomProjectComboBox.Text);
    }
}