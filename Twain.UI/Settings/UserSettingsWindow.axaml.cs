using System;
using Avalonia.Interactivity;
using Twain.Core;
using Twain.Core.Settings;

namespace Twain.UI.Settings;

/// <summary>
/// Displays and edits user-configurable application settings.
/// </summary>
public partial class UserSettingsWindow : Avalonia.Controls.Window
{
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
    public UserSettingsWindow(
        string language,
        ProjectEnum project,
        string customProject,
        string protocol)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(customProject);
        ArgumentNullException.ThrowIfNull(protocol);

        InitializeComponent();

        InitializeSelectors();
        InitializeSiteSettings(
            language,
            project,
            customProject,
            protocol);
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
}