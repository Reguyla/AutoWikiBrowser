using Avalonia.Controls;
using Avalonia.Interactivity;
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
}