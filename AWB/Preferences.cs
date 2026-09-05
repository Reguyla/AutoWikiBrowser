/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2008 Stephen Kennedy

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

using Twain.Core;
using Twain.Core.Alerts;
using Twain.Core.Parse;
using Twain.Core.Settings;

namespace AutoWikiBrowser;

internal sealed partial class MyPreferences : Form
{
    /// <summary>
    /// Initializes the preferences dialog using the current wiki connection
    /// and application settings.
    /// </summary>
    /// <param name="lang">
    /// The language code to select in the language list.
    /// </param>
    /// <param name="proj">
    /// The currently selected Wikimedia or supported wiki project.
    /// </param>
    /// <param name="customproj">
    /// The custom wiki host or project name to display when a custom project
    /// is selected.
    /// </param>
    /// <param name="protocol">
    /// The currently selected connection protocol, normally
    /// <c>http://</c> or <c>https://</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="lang"/>, <paramref name="customproj"/>,
    /// or <paramref name="protocol"/> is <see langword="null"/>.
    /// </exception>
    public MyPreferences(
        string lang,
        ProjectEnum proj,
        string customproj,
        string protocol)
    {
        ArgumentNullException.ThrowIfNull(lang);
        ArgumentNullException.ThrowIfNull(customproj);
        ArgumentNullException.ThrowIfNull(protocol);

        InitializeComponent();

        InitializeProjectSelection(proj);
        InitializeLanguageSelection(lang);
        InitializeCustomProjects(customproj);
        InitializeStoredPreferences();
        ApplyPlatformRestrictions();
        InitializeProtocolSelection(protocol);
    }

    /// <summary>
    /// Populates the project selector and applies the supplied project selection.
    /// </summary>
    private void InitializeProjectSelection(ProjectEnum project)
    {
        cmboProject.Items.Clear();
        cmboProject.Items.AddRange(
            Enum.GetValues<ProjectEnum>()
                .Cast<object>()
                .ToArray());

        cmboProject.SelectedItem = project;
        UpdateProjectSelection();
    }

    /// <summary>
    /// Selects the supplied wiki language code.
    /// </summary>
    private void InitializeLanguageSelection(string language)
    {
        cmboLang.SelectedItem =
            UserSettingsHelper.NormalizeLanguageCode(
                language);
    }

    /// <summary>
    /// Populates the custom-project selector and applies the current value.
    /// </summary>
    private void InitializeCustomProjects(string customProject)
    {
        IReadOnlyList<string> customWikis =
            UserSettingsHelper.ParseCustomWikis(
                Properties.Settings.Default.CustomWikis);

        cmboCustomProject.Items.Clear();
        cmboCustomProject.Items.AddRange(
            customWikis.Cast<object>().ToArray());

        cmboCustomProject.Text = customProject;
    }

    /// <summary>
    /// Applies preferences stored in the application settings.
    /// </summary>
    private void InitializeStoredPreferences()
    {
        chkAlwaysConfirmExit.Checked =
            Properties.Settings.Default.AskForTerminate;

        chkPrivacy.Checked =
            UserSettingsHelper.GetPrivacyCheckboxState(
                Properties.Settings.Default.Privacy);
    }

    /// <summary>
    /// Applies platform-specific restrictions to the preference controls.
    /// </summary>
    private void ApplyPlatformRestrictions()
    {
        if (!Globals.UsingMono)
        {
            return;
        }

        // Flashing the application window is not supported under Mono.
        chkFlash.Enabled = false;
        chkFlash.Checked = false;
    }

    /// <summary>
    /// Applies the supplied protocol selection.
    /// </summary>
    private void InitializeProtocolSelection(string protocol)
    {
        cmboProtocol.SelectedIndex =
            UserSettingsHelper.GetProtocolSelectionIndex(
                protocol);
    }

    #region Language and project

    /// <summary>
    /// Gets the selected wiki language code.
    /// </summary>
    /// <value>
    /// The selected language code, or an empty string when no language is selected.
    /// </value>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Language =>
        cmboLang.SelectedItem?.ToString() ?? string.Empty;

    /// <summary>
    /// Gets the selected wiki project.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the project selector does not contain a valid
    /// <see cref="ProjectEnum"/> value.
    /// </exception>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ProjectEnum Project =>
        cmboProject.SelectedItem is ProjectEnum project
            ? project
            : throw new InvalidOperationException(
                "No valid wiki project is selected.");

    /// <summary>
    /// Gets the normalized custom wiki project name.
    /// </summary>
    /// <remarks>
    /// Reading this property currently calls <c>FixCustomProject</c>, which may
    /// update the custom-project control before returning its text.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CustomProject
    {
        get
        {
            // TODO: Remove the side effect from this getter by normalizing the
            // custom project when the value is entered or when the dialog is
            // accepted.
            FixCustomProject();

            return cmboCustomProject.Text;
        }
    }

    /// <summary>
    /// Gets the connection protocol selected for custom wiki projects.
    /// </summary>
    /// <value>
    /// The selected protocol string, typically <c>https://</c> or
    /// <c>http://</c>.
    /// </value>
    /// <remarks>
    /// Wikimedia Foundation, Wikia, and Fandom projects always use HTTPS,
    /// regardless of the value returned by this property.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Protocol => cmboProtocol.Text;

    /// <summary>
    /// Normalizes the custom project name when the user leaves the field.
    /// </summary>
    private void txtCustomProject_Leave(object sender, EventArgs e)
    {
        FixCustomProject();
    }

    /// <summary>
    /// Normalizes the custom-project entry by removing its URI scheme, trimming
    /// a trailing API filename, and applying the expected trailing-slash format.
    /// </summary>
    private void FixCustomProject()
    {
        cmboCustomProject.Text =
            UserSettingsHelper.NormalizeCustomProject(
                cmboCustomProject.Text,
                Project);
    }

    /// <summary>
    /// Updates the available languages and project-specific controls when the
    /// selected wiki project changes.
    /// </summary>
    private void cmboProject_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateProjectSelection();
    }

    /// <summary>
    /// Configures the language list and project-specific controls for the
    /// currently selected wiki project.
    /// </summary>
    private void UpdateProjectSelection()
    {
        ProjectEnum project = Project;

        cmboLang.Enabled =
            UserSettingsHelper.SupportsLanguageSelection(
                project);

        string selectedLanguage =
            cmboLang.SelectedItem?.ToString() ?? string.Empty;

        cmboLang.Items.Clear();

        IReadOnlyList<string> languages =
            UserSettingsHelper.GetLanguagesForProject(
                project);

        cmboLang.Items.AddRange(
            languages.ToArray());

        if (!string.IsNullOrEmpty(selectedLanguage))
        {
            cmboLang.SelectedIndex =
                cmboLang.Items.IndexOf(
                    selectedLanguage);
        }

        bool usesCustomProjectControls =
            UserSettingsHelper.UsesCustomProjectControls(
                project);

        bool supportsCustomConnectionSettings =
            UserSettingsHelper.SupportsCustomConnectionSettings(
                project);

        chkSupressAWB.Enabled =
            supportsCustomConnectionSettings;

        cmboProtocol.Enabled =
            supportsCustomConnectionSettings;

        DomainEnabled =
            supportsCustomConnectionSettings;

        if (usesCustomProjectControls)
        {
            cmboProtocol.Visible = true;
            cmboCustomProject.Visible = true;
            cmboLang.Visible = false;

            if (UserSettingsHelper.RequiresHttps(
                    project))
            {
                cmboProtocol.SelectedIndex = 0;
            }

            lblPostfix.Text =
                UserSettingsHelper.GetProjectPostfix(
                    project);

            // TODO: Extract the reusable logic from cmboCustomProjectChanged into
            // a dedicated helper rather than invoking an event handler directly.
            cmboCustomProjectChanged(
                null,
                null);

            return;
        }

        cmboProtocol.Visible = false;
        lblPostfix.Text =
            UserSettingsHelper.GetProjectPostfix(
                project);
        cmboCustomProject.Visible = false;
        cmboLang.Visible = true;
        btnOK.Enabled = true;
        chkSupressAWB.Enabled = false;
    }

    /// <summary>
    /// Updates whether the current project settings contain enough information
    /// for the dialog to be accepted.
    /// </summary>
    private void UpdateOkButtonState()
    {
        btnOK.Enabled =
            !UserSettingsHelper.RequiresCustomProject(
                Project) ||
            !string.IsNullOrWhiteSpace(
                cmboCustomProject.Text);
    }

    /// <summary>
    /// Updates the dialog validation state when the custom project value changes.
    /// </summary>
    private void cmboCustomProjectChanged(object sender, EventArgs e)
    {
        UpdateOkButtonState();
    }

    #endregion

    #region Other

    /// <summary>
    /// Gets or sets the font used by editable text controls in the preferences dialog.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Font? TextBoxFont { get; set; }

    // TODO:
    // Investigate whether the enabled state of the domain controls should be
    // updated from a single helper method shared with chkDomain_CheckedChanged
    // to ensure all domain UI state changes remain synchronized.
    /// <summary>
    /// Gets or sets whether manual domain selection is available.
    /// </summary>
    /// <remarks>
    /// Disabling this property also disables the domain text box. When enabled,
    /// the text box is only enabled if the associated check box is checked.
    /// </remarks>
    private bool DomainEnabled
    {
        get => chkDomain.Enabled;

        set
        {
            chkDomain.Enabled = value;
            txtDomain.Enabled = value && chkDomain.Checked;
        }
    }

    /// <summary>
    /// Gets or sets the preferred wiki domain.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PrefDomain
    {
        get { return txtDomain.Text; }
        set
        {
            txtDomain.Text = value;

            DomainEnabled =
                !string.IsNullOrEmpty(value) &&
                UserSettingsHelper.SupportsCustomConnectionSettings(
                    Project);
        }
    }

    /// <summary>
    /// Opens the font selection dialog and updates the editor font when the user
    /// confirms a new selection.
    /// </summary>
    private void btnTextBoxFont_Click(object sender, EventArgs e)
    {
        fontDialog.Font = TextBoxFont;

        if (fontDialog.ShowDialog() == DialogResult.OK)
        {
            TextBoxFont = fontDialog.Font;

            // TODO: If the dialog does not immediately reflect font changes,
            // apply the updated font to the relevant controls here.
        }
    }

    /// <summary>
    /// Gets or sets whether AWB attribution should be suppressed.
    /// </summary>
    /// <remarks>
    /// The value is only applied when the suppression option is enabled for the
    /// currently selected project.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefSuppressUsingAWB
    {
        get => chkSupressAWB.Checked;
        set => chkSupressAWB.Checked = chkSupressAWB.Enabled && value;
    }

    /// <summary>
    /// Gets or sets whether AWB attribution is added to article-action summaries.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefAddUsingAWBOnArticleAction
    {
        get => chkAddUsingAWBToActionSummaries.Checked;
        set => chkAddUsingAWBToActionSummaries.Checked = value;
    }

    /// <summary>
    /// Gets or sets whether AWB should run with low thread priority.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LowThreadPriority
    {
        get => chkLowPriority.Checked;
        set => chkLowPriority.Checked = value;
    }

    /// <summary>
    /// Gets or sets whether the application should flash for alerts.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefFlash
    {
        get => chkFlash.Checked;
        set => chkFlash.Checked = value;
    }

    /// <summary>
    /// Gets or sets whether the application should beep for alerts.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefBeep
    {
        get => chkBeep.Checked;
        set => chkBeep.Checked = value;
    }

    /// <summary>
    /// Gets or sets whether the application should minimize when appropriate.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefMinimize
    {
        get => chkMinimize.Checked;
        set => chkMinimize.Checked = value;
    }

    /// <summary>
    /// Gets or sets whether the article list should be saved.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefSaveArticleList
    {
        get => chkSaveArticleList.Checked;
        set => chkSaveArticleList.Checked = value;
    }

    /// <summary>
    /// Gets or sets whether automatic saving of the edit box is enabled.
    /// </summary>
    /// <remarks>
    /// Setting this property also updates the enabled state of the autosave
    /// interval, file-path, label, and file-selection controls.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefAutoSaveEditBoxEnabled
    {
        get => chkAutoSaveEdit.Checked;

        set
        {
            chkAutoSaveEdit.Checked = value;
            btnSetFile.Enabled = value;
            nudEditBoxAutosave.Enabled = value;
            txtAutosave.Enabled = value;
            lblAutosaveFile.Enabled = value;
        }
    }

    /// <summary>
    /// Gets or sets the edit-box automatic-save interval.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal PrefAutoSaveEditBoxPeriod
    {
        get => nudEditBoxAutosave.Value;
        set => nudEditBoxAutosave.Value = value;
    }

    /// <summary>
    /// Gets or sets the path of the edit-box automatic-save file.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PrefAutoSaveEditBoxFile
    {
        get => txtAutosave.Text;
        set => txtAutosave.Text = value;
    }

    /// <summary>
    /// Gets or sets whether logging is enabled.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EnableLogging
    {
        get => chkEnableLogging.Checked;
        set => chkEnableLogging.Checked = value;
    }

    /// <summary>
    /// Gets or sets the custom wiki values available in the custom-project list.
    /// </summary>
    /// <remarks>
    /// The getter includes the currently entered custom-project text followed by
    /// the values stored in the combo-box item collection.
    /// </remarks>
    // TODO: Determine whether this property is still used by settings
    // serialization or external callers. Remove it if unused; otherwise define
    // whether empty and duplicate custom-wiki values should be preserved.
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> PrefCustomWikis
    {
        get
        {
            var customWikis = new List<string>
        {
            cmboCustomProject.Text
        };

            customWikis.AddRange(
                cmboCustomProject.Items
                    .Cast<object>()
                    .Select(item => item?.ToString() ?? string.Empty));

            return customWikis;
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            cmboCustomProject.Items.Clear();

            foreach (string customWiki in value)
            {
                cmboCustomProject.Items.Add(customWiki);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the <c>{{nobots}}</c> template should be ignored.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefIgnoreNoBots
    {
        get => chkIgnoreNoBots.Checked;
        set => chkIgnoreNoBots.Checked = value;
    }

    /// <summary>
    /// Gets or sets whether the processing timer should be displayed.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefShowTimer
    {
        get => chkShowTimer.Checked;
        set => chkShowTimer.Checked = value;
    }

    // TODO: Replace persisted ComboBox indexes with named enum values so that
    // reordering or inserting UI options does not change existing preferences.

    /// <summary>
    /// Gets or sets the List Comparer article-list source selection.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CurrentArticleListMode PrefListComparerUseCurrentArticleList
    {
        get => (CurrentArticleListMode)cmboListComparer.SelectedIndex;
        set => cmboListComparer.SelectedIndex = (int)value;
    }

    /// <summary>
    /// Gets or sets the List Splitter article-list source selection.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CurrentArticleListMode PrefListSplitterUseCurrentArticleList
    {
        get => (CurrentArticleListMode)cmboListSplitter.SelectedIndex;
        set => cmboListSplitter.SelectedIndex = (int)value;
    }

    /// <summary>
    /// Gets or sets the Database Scanner article-list source selection.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CurrentArticleListMode PrefDBScannerUseCurrentArticleList
    {
        get => (CurrentArticleListMode)cmboDBScanner.SelectedIndex;
        set => cmboDBScanner.SelectedIndex = (int)value;
    }

    /// <summary>
    /// Gets or sets the action performed when an article is loaded.
    /// </summary>
    /// <remarks>
    /// Legacy option index <c>2</c>, which represented showing the edit page,
    /// is no longer supported and is mapped to the default action at index
    /// <c>0</c>.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PrefOnLoad
    {
        get =>
            UserSettingsHelper.NormalizeOnLoadSelection(
                cmboOnLoad.SelectedIndex);

        set =>
            cmboOnLoad.SelectedIndex =
                UserSettingsHelper.NormalizeOnLoadSelection(
                    value);
    }

    /// <summary>
    /// Gets or sets whether a diff is generated while running in bot mode.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefDiffInBotMode
    {
        get => chkDiffInBotMode.Checked;
        set => chkDiffInBotMode.Checked = value;
    }

    /// <summary>
    /// Gets or sets whether the page list is cleared when the selected project
    /// changes.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefClearPageListOnProjectChange
    {
        get => chkEmptyOnProjectChange.Checked;
        set => chkEmptyOnProjectChange.Checked = value;
    }

    /// <summary>
    /// Gets or sets the enabled alert identifiers.
    /// </summary>
    /// <remarks>
    /// When no stored alert preferences exist, all alerts are treated as enabled.
    /// This property is managed by AWB's settings system and is not serialized by
    /// the Windows Forms designer.
    /// </remarks>
    // TODO: Determine whether users should be able to disable every alert.
    // The current settings format treats an empty selection as "enable all,"
    // so it cannot distinguish that state from an uninitialized preference.
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<int> AlertPreferences
    {
        get => GetEnabledAlertIds();

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            PopulateAlertPreferences(value);
        }
    }

    /// <summary>
    /// Gets the checked alert identifiers, or all available identifiers when no
    /// alert is checked.
    /// </summary>
    private List<int> GetEnabledAlertIds()
    {
        List<int> availableAlertIds =
            alertListBox.Items
                .Cast<CheckedBoxItem>()
                .Select(alert => alert.ID)
                .ToList();

        List<int> selectedAlertIds =
            Enumerable.Range(
                    0,
                    alertListBox.Items.Count)
                .Where(alertListBox.GetItemChecked)
                .Select(index =>
                    ((CheckedBoxItem)alertListBox.Items[index]).ID)
                .ToList();

        return ArticleAlertHelper.ResolveEnabledAlertIds(
            availableAlertIds,
            selectedAlertIds);
    }

    /// <summary>
    /// Rebuilds the alert preference list using the supplied enabled alert identifiers.
    /// </summary>
    /// <param name="enabledAlertIds">
    /// The identifiers of alerts that should be enabled. An empty collection
    /// indicates that all alerts should be enabled.
    /// </param>
    private void PopulateAlertPreferences(
        IReadOnlyCollection<int> enabledAlertIds)
    {
        alertListBox.BeginUpdate();

        try
        {
            alertListBox.Items.Clear();

            foreach (KeyValuePair<ArticleAlertId, string> alert in
                     ArticleAlertHelper.AlertDescriptions)
            {
                int alertId =
                    (int)alert.Key;

                alertListBox.Items.Add(
                    new CheckedBoxItem
                    {
                        ID = alertId,
                        Description = alert.Value
                    },
                    ArticleAlertHelper.IsAlertEnabled(
                        enabledAlertIds,
                        alertId));
            }
        }
        finally
        {
            alertListBox.EndUpdate();
        }
    }

    #endregion

    /// <summary>
    /// Updates the autosave controls when the autosave option is enabled or
    /// disabled.
    /// </summary>
    private void chkAutoSaveEdit_CheckedChanged(object sender, EventArgs e)
    {
        PrefAutoSaveEditBoxEnabled = chkAutoSaveEdit.Checked;
    }

    /// <summary>
    /// Prompts the user to choose the autosave file location.
    /// </summary>
    private void btnSetFile_Click(object sender, EventArgs e)
    {
        // TODO: Consider initializing the dialog to the directory containing the
        // current autosave file or the last directory selected by the user.
        saveFile.InitialDirectory = Application.StartupPath;

        if (saveFile.ShowDialog() == DialogResult.OK)
        {
            txtAutosave.Text = saveFile.FileName;
        }
    }

    /// <summary>
    /// Validates the current preferences, updates application settings, and saves
    /// any persistent values that have changed.
    /// </summary>
    private void btnOk_Click(object sender, EventArgs e)
    {
        ValidateAutoSaveSettings();
        AddCurrentCustomProject();

        if (UpdatePersistentSettings())
        {
            Properties.Settings.Default.Save();
        }
    }

    /// <summary>
    /// Disables edit-box autosave when no autosave file has been specified.
    /// </summary>
    private void ValidateAutoSaveSettings()
    {
        chkAutoSaveEdit.Checked =
            UserSettingsHelper.NormalizeAutoSaveEnabled(
                chkAutoSaveEdit.Checked,
                txtAutosave.Text);
    }

    /// <summary>
    /// Normalizes and adds the current custom project to the saved project list.
    /// </summary>
    private void AddCurrentCustomProject()
    {
        if (Project != ProjectEnum.custom ||
            string.IsNullOrWhiteSpace(cmboCustomProject.Text))
        {
            return;
        }

        FixCustomProject();

        string customProject =
            cmboCustomProject.Text;

        IEnumerable<string?> existingCustomWikis =
            cmboCustomProject.Items
                .Cast<object>()
                .Select(item =>
                    item?.ToString());

        if (UserSettingsHelper.ShouldAddCustomWiki(
                customProject,
                existingCustomWikis))
        {
            cmboCustomProject.Items.Add(
                customProject);
        }
    }

    // TODO: Move preferences persistence into a dedicated settings service or
    // preferences model so the form is responsible only for presenting and
    // collecting user input.
    /// <summary>
    /// Updates persistent application settings from the current form values.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when at least one persistent setting changed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool UpdatePersistentSettings()
    {
        bool settingsChanged = false;

        string customWikis = BuildCustomWikisSetting();

        if (!string.Equals(
                Properties.Settings.Default.CustomWikis,
                customWikis,
                StringComparison.Ordinal))
        {
            Properties.Settings.Default.CustomWikis = customWikis;
            settingsChanged = true;
        }

        if (Properties.Settings.Default.AskForTerminate !=
            chkAlwaysConfirmExit.Checked)
        {
            Properties.Settings.Default.AskForTerminate =
                chkAlwaysConfirmExit.Checked;

            settingsChanged = true;
        }

        // The persisted Privacy value has inverse semantics relative to the
        // checkbox state.
        bool privacySetting =
            UserSettingsHelper.GetPrivacySetting(
                chkPrivacy.Checked);

        if (Properties.Settings.Default.Privacy != privacySetting)
        {
            Properties.Settings.Default.Privacy = privacySetting;
            settingsChanged = true;
        }

        return settingsChanged;
    }

    /// <summary>
    /// Creates the pipe-delimited custom wiki value stored in application
    /// settings.
    /// </summary>
    /// <returns>
    /// A pipe-delimited list of nonempty custom wiki entries.
    /// </returns>
    private string BuildCustomWikisSetting()
    {
        return UserSettingsHelper.BuildCustomWikisSetting(
            cmboCustomProject.Items
                .Cast<object>()
                .Select(item =>
                    item?.ToString()));
    }

    /// <summary>
    /// Updates the bot-mode diff option when the article-load action changes.
    /// </summary>
    private void cmboOnLoad_SelectedIndexChanged(object sender, EventArgs e)
    {
        // TODO: Replace the raw article-load selection index with a named enum
        // value so this logic is not coupled to ComboBox item ordering.
        chkDiffInBotMode.Enabled = cmboOnLoad.SelectedIndex == 0;
    }

    /// <summary>
    /// Gets or sets whether the site preferences tab should be selected when the
    /// preferences form is activated.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool FocusSiteTab { get; set; }

    /// <summary>
    /// Selects the site preferences tab when requested after the form becomes
    /// active.
    /// </summary>
    /// <param name="e">
    /// The event data associated with form activation.
    /// </param>
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (FocusSiteTab)
        {
            tbPrefs.SelectTab(1);
        }
    }

    /// <summary>
    /// Updates the enabled state of the manual domain controls.
    /// </summary>
    private void UpdateDomainControls()
    {
        txtDomain.Enabled =
            chkDomain.Enabled &&
            chkDomain.Checked;
    }

    /// <summary>
    /// Updates the domain text box when the manual-domain option changes.
    /// </summary>
    private void chkDomain_CheckedChanged(object sender, EventArgs e)
    {
        UpdateDomainControls();
    }
}