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

        string[] articleListModes =
        [
            "Ask",
            "Always",
            "Never"
        ];

        ListComparerModeComboBox.ItemsSource =
            articleListModes;

        ListSplitterModeComboBox.ItemsSource =
            articleListModes;

        DatabaseScannerModeComboBox.ItemsSource =
            articleListModes;
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
    /// Updates the available languages when the selected wiki project changes.
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
}