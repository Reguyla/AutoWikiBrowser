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

using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WikiFunctions;
using WikiFunctions.Parse;

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
        cmboLang.SelectedItem = NormalizeLanguageCode(language);
    }

    /// <summary>
    /// Normalizes a wiki language code for selection.
    /// </summary>
    private static string NormalizeLanguageCode(string language)
    {
        return language.ToLowerInvariant();
    }

    /// <summary>
    /// Populates the custom-project selector and applies the current value.
    /// </summary>
    private void InitializeCustomProjects(string customProject)
    {
        IReadOnlyList<string> customWikis = ParseCustomWikis(
            Properties.Settings.Default.CustomWikis);

        cmboCustomProject.Items.Clear();
        cmboCustomProject.Items.AddRange(
            customWikis.Cast<object>().ToArray());

        cmboCustomProject.Text = customProject;
    }

    /// <summary>
    /// Parses the pipe-delimited custom wiki setting.
    /// </summary>
    private static IReadOnlyList<string> ParseCustomWikis(
        string storedCustomWikis)
    {
        if (string.IsNullOrWhiteSpace(storedCustomWikis))
        {
            return Array.Empty<string>();
        }

        return storedCustomWikis
            .Split(
                '|',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Applies preferences stored in the application settings.
    /// </summary>
    private void InitializeStoredPreferences()
    {
        chkAlwaysConfirmExit.Checked =
            Properties.Settings.Default.AskForTerminate;

        chkPrivacy.Checked =
            GetPrivacyCheckboxState(Properties.Settings.Default.Privacy);
    }

    /// <summary>
    /// Converts the persisted privacy value to its checkbox representation.
    /// </summary>
    // TODO: Rename or migrate the persisted Privacy setting during a future
    // settings-model redesign so its meaning is not inverted relative to the UI.
    private static bool GetPrivacyCheckboxState(bool privacyEnabled)
    {
        return !privacyEnabled;
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
            GetProtocolSelectionIndex(protocol);
    }

    /// <summary>
    /// Gets the protocol selector index for a protocol value.
    /// </summary>
    // TODO: Replace index-based protocol selection with a named value or enum so
    // behavior is not coupled to ComboBox item ordering.
    private static int GetProtocolSelectionIndex(string protocol)
    {
        return string.Equals(
            protocol,
            "http://",
            StringComparison.Ordinal)
            ? 1
            : 0;
    }

    /// <summary>
    /// Determines whether AWB attribution suppression is available for a project.
    /// </summary>
    private static bool SupportsAwbAttributionSuppression(
        ProjectEnum project)
    {
        return project == ProjectEnum.custom ||
               project == ProjectEnum.wikia ||
               project == ProjectEnum.fandom;
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

    // TODO: Investigate replacing the legacy regular-expression URL parsing with
    // Uri.TryCreate. Preserve support for host names and paths entered without a
    // URI scheme, and add tests for ports, localhost, IPv6, query strings, and
    // index.php/api.php paths before changing the existing behavior.
    /// <summary>
    /// Matches a custom wiki URL and captures its host and optional script path,
    /// excluding a trailing <c>index.php</c> or <c>api.php</c> filename.
    /// </summary>
    /// <remarks>
    /// Text without a URI scheme does not match and is preserved by the
    /// replacement operation.
    /// </remarks>
    private static readonly Regex CustomProjectRegex = new(
        @"^.*?://(?:([\w/\.-]+?)/(?:index|api)\.php|([\w/\.-]+)).*$",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Normalizes the custom-project entry by removing its URI scheme, trimming
    /// a trailing API filename, and applying the expected trailing-slash format.
    /// </summary>
    private void FixCustomProject()
    {
        string customProject = CustomProjectRegex.Replace(
            cmboCustomProject.Text.Trim(),
            "$1$2");

        customProject = customProject.TrimEnd('/');

        // Generic custom projects require a trailing slash. Wikia and Fandom
        // project values retain their existing non-custom formatting.
        if (customProject.Length > 0 &&
            Project == ProjectEnum.custom)
        {
            customProject += "/";
        }

        cmboCustomProject.Text = customProject;
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

        // TODO: Replace this enum-order comparison with an explicit determination
        // of whether the selected project supports multiple languages. The current
        // behavior depends on the numeric ordering of ProjectEnum members.
        cmboLang.Enabled = project < ProjectEnum.species;

        string selectedLanguage =
            cmboLang.SelectedItem?.ToString() ?? string.Empty;

        cmboLang.Items.Clear();

        List<string> languages = project switch
        {
            ProjectEnum.wikipedia => SiteMatrix.WikipediaLanguages,
            ProjectEnum.wiktionary => SiteMatrix.WiktionaryLanguages,
            ProjectEnum.wikibooks => SiteMatrix.WikibooksLanguages,
            ProjectEnum.wikinews => SiteMatrix.WikinewsLanguages,
            ProjectEnum.wikiquote => SiteMatrix.WikiquoteLanguages,
            ProjectEnum.wikisource => SiteMatrix.WikisourceLanguages,
            ProjectEnum.wikiversity => SiteMatrix.WikiversityLanguages,
            _ => SiteMatrix.Languages
        };

        cmboLang.Items.AddRange(languages.ToArray());

        if (!string.IsNullOrEmpty(selectedLanguage))
        {
            cmboLang.SelectedIndex =
                cmboLang.Items.IndexOf(selectedLanguage);
        }

        bool isCustomProject = project == ProjectEnum.custom;
        bool isWikiaProject = project == ProjectEnum.wikia;
        bool isFandomProject = project == ProjectEnum.fandom;
        bool usesCustomProjectControls =
            isCustomProject || isWikiaProject || isFandomProject;

        chkSupressAWB.Enabled = isCustomProject;
        cmboProtocol.Enabled = isCustomProject;
        DomainEnabled = isCustomProject;

        if (usesCustomProjectControls)
        {
            cmboProtocol.Visible = true;
            cmboCustomProject.Visible = true;
            cmboLang.Visible = false;

            if (isWikiaProject || isFandomProject)
            {
                // Wikia and Fandom projects always use HTTPS.
                cmboProtocol.SelectedIndex = 0;
            }

            lblPostfix.Text = project switch
            {
                ProjectEnum.wikia => ".wikia.com",
                ProjectEnum.fandom => ".fandom.com",
                _ => string.Empty
            };

            // TODO: Extract the reusable logic from cmboCustomProjectChanged into
            // a dedicated helper rather than invoking an event handler directly.
            cmboCustomProjectChanged(null, null);

            return;
        }

        cmboProtocol.Visible = false;
        lblPostfix.Text = string.Empty;
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
        ProjectEnum project = Project;

        bool requiresCustomProject =
            project == ProjectEnum.custom ||
            project == ProjectEnum.wikia ||
            project == ProjectEnum.fandom;

        btnOK.Enabled =
            !requiresCustomProject ||
            !string.IsNullOrWhiteSpace(cmboCustomProject.Text);
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

            ProjectEnum prj = (ProjectEnum)Enum.Parse(
                typeof(ProjectEnum),
                cmboProject.SelectedItem.ToString());

            DomainEnabled =
                !string.IsNullOrEmpty(value) &&
                prj.Equals(ProjectEnum.custom);
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
    public int PrefListComparerUseCurrentArticleList
    {
        get => cmboListComparer.SelectedIndex;
        set => cmboListComparer.SelectedIndex = value;
    }
    /// <summary>
    /// Gets or sets the List Splitter article-list source selection.
    /// </summary>
    /// <remarks>
    /// The value corresponds to the selected index in the List Splitter
    /// source selector.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PrefListSplitterUseCurrentArticleList
    {
        get => cmboListSplitter.SelectedIndex;
        set => cmboListSplitter.SelectedIndex = value;
    }

    /// <summary>
    /// Gets or sets the Database Scanner article-list source selection.
    /// </summary>
    /// <remarks>
    /// The value corresponds to the selected index in the Database Scanner
    /// source selector.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PrefDBScannerUseCurrentArticleList
    {
        get => cmboDBScanner.SelectedIndex;
        set => cmboDBScanner.SelectedIndex = value;
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
        get => cmboOnLoad.SelectedIndex == 2
            ? 0
            : cmboOnLoad.SelectedIndex;

        set => cmboOnLoad.SelectedIndex = value == 2
            ? 0
            : value;
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
        List<CheckedBoxItem> alertItems = alertListBox.Items
            .Cast<CheckedBoxItem>()
            .ToList();

        bool anyChecked = Enumerable.Range(0, alertListBox.Items.Count)
            .Any(alertListBox.GetItemChecked);

        return alertItems
            .Where((_, index) =>
                alertListBox.GetItemChecked(index) || !anyChecked)
            .Select(alert => alert.ID)
            .ToList();
    }

    /// <summary>
    /// Rebuilds the alert list using the supplied enabled alert identifiers.
    /// </summary>
    private void PopulateAlertPreferences(
        IReadOnlyCollection<int> enabledAlertIds)
    {
        bool enableAllAlerts = enabledAlertIds.Count == 0;
        var enabledAlerts = enabledAlertIds.ToHashSet();

        alertListBox.BeginUpdate();

        try
        {
            alertListBox.Items.Clear();

            foreach (KeyValuePair<int, string> alert in _alertDescriptions)
            {
                alertListBox.Items.Add(
                    new CheckedBoxItem
                    {
                        ID = alert.Key,
                        Description = alert.Value
                    },
                    enableAllAlerts || enabledAlerts.Contains(alert.Key));
            }
        }
        finally
        {
            alertListBox.EndUpdate();
        }
    }

    /// <summary>
    /// Gets an alert item from the checked list.
    /// </summary>
    /// <param name="index">
    /// The zero-based item index.
    /// </param>
    /// <returns>
    /// The alert item at the specified index.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked-list item is not a
    /// <see cref="CheckedBoxItem"/>.
    /// </exception>
    private CheckedBoxItem GetAlertItem(int index)
    {
        return alertListBox.Items[index] is CheckedBoxItem alertItem
            ? alertItem
            : throw new InvalidOperationException(
                $"Alert item at index {index} is not a {nameof(CheckedBoxItem)}.");
    }

    /// <summary>
    /// Represents an alert identifier and its selected state independently of
    /// the Windows Forms controls.
    /// </summary>
    private readonly record struct AlertSelection(
        int Id,
        bool IsChecked);

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
        if (chkAutoSaveEdit.Checked &&
            string.IsNullOrWhiteSpace(txtAutosave.Text))
        {
            chkAutoSaveEdit.Checked = false;
        }
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

        string customProject = cmboCustomProject.Text;

        if (string.IsNullOrWhiteSpace(customProject))
        {
            return;
        }

        bool alreadyExists = cmboCustomProject.Items
            .Cast<object>()
            .Select(item => item?.ToString())
            .Any(item => string.Equals(
                item,
                customProject,
                StringComparison.Ordinal));

        if (!alreadyExists)
        {
            cmboCustomProject.Items.Add(customProject);
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
        bool privacySetting = !chkPrivacy.Checked;

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
        return string.Join(
            "|",
            cmboCustomProject.Items
                .Cast<object>()
                .Select(item => item?.ToString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim()));
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

    /// <summary>
    /// Maps article-alert identifiers to their user-facing descriptions.
    /// </summary>
    /// <remarks>
    /// The numeric identifiers must remain synchronized with the alert values
    /// used by the article-checking and preferences logic.
    /// </remarks>
    // TODO: Replace the numeric alert identifiers with a named enum after
    // confirming whether these values are persisted or used outside this form.
    //
    // TODO: Move user-facing alert descriptions to application resources if the
    // preferences interface is localized in the future.
    private static readonly IReadOnlyDictionary<int, string> _alertDescriptions =
        new Dictionary<int, string>
        {
        { 1, "Ambiguous citation dates" },
        { 2, "Contains 'sic' tag" },
        { 3, "DAB page with <ref>s" },
        { 4, "Dead links" },
        { 5, "Duplicate parameters in WPBannerShell" },
        { 6, "Has <ref> after </references>" },
        { 7, "Has 'No/More footnotes' template yet many references" },
        { 8, "Headers with wikilinks" },
        { 9, "Invalid citation parameters" },
        { 10, "Links with double pipes" },
        { 11, "Links with no target" },
        { 12, "Long article with stub tag" },
        { 13, "Multiple DEFAULTSORT" },
        { 14, "No category (may be one in a template)" },
        { 15, "See also section out of place" },
        { 16, "Starts with heading" },
        { 17, "Unbalanced brackets" },
        { 18, "Unclosed tags" },
        { 19, "Unformatted references" },
        { 20, "Unknown parameters in multiple issues" },
        { 21, "Unknown parameters in WikiProject banner shell" },
        { 22, "Editor's signature or link to user space" }
        };
}