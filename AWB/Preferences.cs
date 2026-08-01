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

        foreach (ProjectEnum project in Enum.GetValues<ProjectEnum>())
        {
            cmboProject.Items.Add(project);
        }

        cmboProject.SelectedItem = proj;

        // TODO: Extract the reusable project-selection logic from the event
        // handler into a dedicated helper. Event handlers should not normally
        // be invoked directly with null event arguments.
        cmboProject_SelectedIndexChanged(null, null);

        cmboLang.SelectedItem = lang.ToLowerInvariant();

        cmboCustomProject.Items.Clear();

        foreach (string customWiki in Properties.Settings.Default.CustomWikis
                     .Split(
                         '|',
                         StringSplitOptions.RemoveEmptyEntries |
                         StringSplitOptions.TrimEntries)
                     .Distinct(StringComparer.Ordinal))
        {
            cmboCustomProject.Items.Add(customWiki);
        }

        cmboCustomProject.Text = customproj;

        // TODO: Compare the selected ProjectEnum value directly rather than
        // relying on the combo box display text. Confirm the corresponding
        // ProjectEnum member names before changing this behavior.
        chkSupressAWB.Enabled =
            cmboProject.Text == "custom" ||
            cmboProject.Text == "wikia" ||
            cmboProject.Text == "fandom";

        chkAlwaysConfirmExit.Checked =
            Properties.Settings.Default.AskForTerminate;

        // The persisted Privacy setting has inverse semantics relative to the
        // checkbox state.
        // TODO: Investigate whether this setting can be renamed or migrated to
        // remove the inversion during a future settings-model redesign.
        chkPrivacy.Checked = !Properties.Settings.Default.Privacy;

        if (Globals.UsingMono)
        {
            // Flashing the application window is not supported under Mono.
            chkFlash.Enabled = false;
            chkFlash.Checked = false;
        }

        // Index 1 represents HTTP; index 0 represents HTTPS.
        // TODO: Replace index-based protocol selection with value-based
        // selection so that future ComboBox item reordering is safe.
        cmboProtocol.SelectedIndex = string.Equals(
            protocol,
            "http://",
            StringComparison.Ordinal)
            ? 1
            : 0;
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

    private static readonly Regex CustomProjectRegex =
        new Regex(@"^.*?://(?:([\w/\.-]+?)/(?:index|api).php|([\w/\.-]+)).*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private void FixCustomProject()
    {
        string proj = CustomProjectRegex.Replace(cmboCustomProject.Text.Trim(), "$1$2");

        proj = proj.TrimEnd('/');
        if (Project.Equals(ProjectEnum.custom)) // we shouldn't screw up Wikia/Fandom
        {
            proj += "/";
        }

        cmboCustomProject.Text = proj;
    }

    private void cmboProject_SelectedIndexChanged(object sender, EventArgs e)
    {
        ProjectEnum prj = Project;

        //disable language selection for single language projects
        cmboLang.Enabled = prj < ProjectEnum.species;

        string temp = (cmboLang.SelectedItem != null) ? cmboLang.SelectedItem.ToString() : "";

        cmboLang.Items.Clear();
        List<string> langs;

        switch (prj)
        {
            case ProjectEnum.wikipedia:
                langs = SiteMatrix.WikipediaLanguages;
                break;

            case ProjectEnum.wiktionary:
                langs = SiteMatrix.WiktionaryLanguages;
                break;

            case ProjectEnum.wikibooks:
                langs = SiteMatrix.WikibooksLanguages;
                break;

            case ProjectEnum.wikinews:
                langs = SiteMatrix.WikinewsLanguages;
                break;

            case ProjectEnum.wikiquote:
                langs = SiteMatrix.WikiquoteLanguages;
                break;

            case ProjectEnum.wikisource:
                langs = SiteMatrix.WikisourceLanguages;
                break;

            case ProjectEnum.wikiversity:
                langs = SiteMatrix.WikiversityLanguages;
                break;

            default:
                langs = SiteMatrix.Languages;
                break;
        }

        cmboLang.Items.AddRange(langs.ToArray());

        if (!string.IsNullOrEmpty(temp))
        {
            cmboLang.SelectedIndex = cmboLang.Items.IndexOf(temp);
        }

        chkSupressAWB.Enabled = cmboProtocol.Enabled = DomainEnabled = prj.Equals(ProjectEnum.custom);
        if (prj.Equals(ProjectEnum.custom) || prj.Equals(ProjectEnum.wikia) || prj.Equals(ProjectEnum.fandom))
        {
            cmboProtocol.Visible = true;

            cmboCustomProject.Visible = true;
            cmboLang.Visible = false;
            if (prj.Equals(ProjectEnum.wikia) || prj.Equals(ProjectEnum.fandom))
            {
                cmboProtocol.SelectedIndex = 0;
            }

            if (prj.Equals(ProjectEnum.wikia))
            {
                lblPostfix.Text = ".wikia.com";
            }
            else if (prj.Equals(ProjectEnum.fandom))
            {
                lblPostfix.Text = ".fandom.com";
            }
            else
            {
                lblPostfix.Text = "";
            }

            cmboCustomProjectChanged(null, null);

            return;
        }

        cmboProtocol.Visible = false;
        lblPostfix.Text = "";
        cmboCustomProject.Visible = false;
        cmboLang.Visible = true;
        btnOK.Enabled = true;
        chkSupressAWB.Enabled = false;
    }

    private void cmboCustomProjectChanged(object sender, EventArgs e)
    {
        ProjectEnum prj = (ProjectEnum)Enum.Parse(typeof(ProjectEnum), cmboProject.SelectedItem.ToString());
        if (prj.Equals(ProjectEnum.custom) || prj.Equals(ProjectEnum.wikia) || prj.Equals(ProjectEnum.fandom))
            btnOK.Enabled = !string.IsNullOrEmpty(cmboCustomProject.Text);
        else
            btnOK.Enabled = true;
    }

    #endregion

    #region Other

    public Font TextBoxFont;

    private bool DomainEnabled
    {
        get { return chkDomain.Enabled; }
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

    private void btnTextBoxFont_Click(object sender, EventArgs e)
    {
        fontDialog.Font = TextBoxFont;

        if (fontDialog.ShowDialog() == DialogResult.OK)
            TextBoxFont = fontDialog.Font;
    }

    /// <summary>
    /// Gets or sets whether AWB attribution should be suppressed.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefSuppressUsingAWB
    {
        get { return chkSupressAWB.Checked; }
        set { chkSupressAWB.Checked = chkSupressAWB.Enabled && value; }
    }

    /// <summary>
    /// Gets or sets whether AWB attribution is added to article-action summaries.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefAddUsingAWBOnArticleAction
    {
        get { return chkAddUsingAWBToActionSummaries.Checked; }
        set { chkAddUsingAWBToActionSummaries.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether AWB should run with low thread priority.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LowThreadPriority
    {
        get { return chkLowPriority.Checked; }
        set { chkLowPriority.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether the application should flash for alerts.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefFlash
    {
        get { return chkFlash.Checked; }
        set { chkFlash.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether the application should beep for alerts.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefBeep
    {
        get { return chkBeep.Checked; }
        set { chkBeep.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether the application should minimize when appropriate.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefMinimize
    {
        get { return chkMinimize.Checked; }
        set { chkMinimize.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether the article list should be saved.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefSaveArticleList
    {
        get { return chkSaveArticleList.Checked; }
        set { chkSaveArticleList.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether automatic saving of the edit box is enabled.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefAutoSaveEditBoxEnabled
    {
        get { return chkAutoSaveEdit.Checked; }
        set
        {
            chkAutoSaveEdit.Checked =
                btnSetFile.Enabled =
                nudEditBoxAutosave.Enabled =
                txtAutosave.Enabled =
                lblAutosaveFile.Enabled = value;
        }
    }

    /// <summary>
    /// Gets or sets the edit-box automatic-save interval.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal PrefAutoSaveEditBoxPeriod
    {
        get { return nudEditBoxAutosave.Value; }
        set { nudEditBoxAutosave.Value = value; }
    }

    /// <summary>
    /// Gets or sets the edit-box automatic-save file.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PrefAutoSaveEditBoxFile
    {
        get { return txtAutosave.Text; }
        set { txtAutosave.Text = value; }
    }

    /// <summary>
    /// Gets or sets whether logging is enabled.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EnableLogging
    {
        get { return chkEnableLogging.Checked; }
        set { chkEnableLogging.Checked = value; }
    }

    // TODO: Reinstate or remove this property.
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> PrefCustomWikis
    {
        get
        {
            List<string> temp = new List<string>
        {
            cmboCustomProject.Text
        };

            temp.AddRange(
                from object a in cmboCustomProject.Items
                select a.ToString());

            return temp;
        }
        set
        {
            cmboCustomProject.Items.Clear();

            foreach (string temp in value)
                cmboCustomProject.Items.Add(temp);
        }
    }

    /// <summary>
    /// Gets or sets whether the nobots template should be ignored.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefIgnoreNoBots
    {
        get { return chkIgnoreNoBots.Checked; }
        set { chkIgnoreNoBots.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether the timer should be displayed.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefShowTimer
    {
        get { return chkShowTimer.Checked; }
        set { chkShowTimer.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether List Comparer uses the current article list.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PrefListComparerUseCurrentArticleList
    {
        get { return cmboListComparer.SelectedIndex; }
        set { cmboListComparer.SelectedIndex = value; }
    }

    /// <summary>
    /// Gets or sets whether List Splitter uses the current article list.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PrefListSplitterUseCurrentArticleList
    {
        get { return cmboListSplitter.SelectedIndex; }
        set { cmboListSplitter.SelectedIndex = value; }
    }

    /// <summary>
    /// Gets or sets whether Database Scanner uses the current article list.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PrefDBScannerUseCurrentArticleList
    {
        get { return cmboDBScanner.SelectedIndex; }
        set { cmboDBScanner.SelectedIndex = value; }
    }

    /// <summary>
    /// Gets or sets the action performed when an article is loaded.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PrefOnLoad
    {
        get
        {
            // Showing the edit page is no longer available as an option.
            return cmboOnLoad.SelectedIndex == 2
                ? 0
                : cmboOnLoad.SelectedIndex;
        }
        set { cmboOnLoad.SelectedIndex = value; }
    }

    /// <summary>
    /// Gets or sets whether a diff is generated while running in bot mode.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefDiffInBotMode
    {
        get { return chkDiffInBotMode.Checked; }
        set { chkDiffInBotMode.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether the page list is cleared when the project changes.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PrefClearPageListOnProjectChange
    {
        get { return chkEmptyOnProjectChange.Checked; }
        set { chkEmptyOnProjectChange.Checked = value; }
    }

    /// <summary>
    /// Gets or sets the enabled alert identifiers.
    /// </summary>
    /// <remarks>
    /// When no stored alert preferences exist, all alerts are treated as enabled.
    /// This property is managed by AWB's settings system and is not serialized by
    /// the Windows Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<int> AlertPreferences
    {
        get
        {
            List<int> alerts = new List<int>();

            bool anyChecked = false;

            for (int a = 0; a < alertListBox.Items.Count; a++)
            {
                if (alertListBox.GetItemChecked(a))
                {
                    anyChecked = true;
                    break;
                }
            }

            for (int i = 0; i < alertListBox.Items.Count; i++)
            {
                if (alertListBox.GetItemChecked(i) || !anyChecked)
                {
                    CheckedBoxItem cbi =
                        (CheckedBoxItem)alertListBox.Items[i];

                    alerts.Add(cbi.ID);
                }
            }

            return alerts;
        }
        set
        {
            alertListBox.BeginUpdate();
            alertListBox.Items.Clear();

            foreach (KeyValuePair<int, string> kvp in alertDescriptions)
            {
                alertListBox.Items.Add(
                    new CheckedBoxItem
                    {
                        ID = kvp.Key,
                        Description = kvp.Value,
                    },
                    value.Contains(kvp.Key) || !value.Any());
            }

            alertListBox.EndUpdate();
        }
    }

    #endregion

    private void chkAutoSaveEdit_CheckedChanged(object sender, EventArgs e)
    {
        PrefAutoSaveEditBoxEnabled = chkAutoSaveEdit.Checked;
    }

    private void btnSetFile_Click(object sender, EventArgs e)
    {
        saveFile.InitialDirectory = Application.StartupPath;
        saveFile.ShowDialog();
        if (!string.IsNullOrEmpty(saveFile.FileName))
        {
            txtAutosave.Text = saveFile.FileName;
        }
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        bool save = false;

        if (chkAutoSaveEdit.Checked && string.IsNullOrEmpty(txtAutosave.Text))
        {
            chkAutoSaveEdit.Checked = false;
        }

        if (cmboProject.Text.Equals("custom") && !string.IsNullOrEmpty(cmboCustomProject.Text))
        {
            FixCustomProject();
            cmboCustomProject.Items.Add(cmboCustomProject.Text);
        }

        StringBuilder customs = new StringBuilder();
        foreach (string s in from string s in cmboCustomProject.Items
                             where !string.IsNullOrEmpty(s.Trim())
                             select s)
        {
            customs.Append(s + "|");
        }

        string tmp = customs.ToString();
        Properties.Settings.Default.CustomWikis =
            string.IsNullOrEmpty(tmp) ? "" : tmp.Substring(0, tmp.LastIndexOf('|'));

        if (!string.IsNullOrEmpty(Properties.Settings.Default.CustomWikis))
        {
            save = true;
        }

        if (Properties.Settings.Default.AskForTerminate != chkAlwaysConfirmExit.Checked)
        {
            Properties.Settings.Default.AskForTerminate = chkAlwaysConfirmExit.Checked;
            save = true;
        }

        if (Properties.Settings.Default.Privacy.Equals(chkPrivacy.Checked))
        {
            Properties.Settings.Default.Privacy = !chkPrivacy.Checked;
            save = true;
        }

        if (save)
        {
            Properties.Settings.Default.Save();
        }
    }

    private void cmboOnLoad_SelectedIndexChanged(object sender, EventArgs e)
    {
        chkDiffInBotMode.Enabled = (cmboOnLoad.SelectedIndex.Equals(0));
    }

    public bool FocusSiteTab = false;

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (FocusSiteTab)
            tbPrefs.SelectTab(1);
    }

    private void chkDomain_CheckedChanged(object sender, EventArgs e)
    {
        txtDomain.Enabled = chkDomain.Checked;
    }

    private readonly Dictionary<int, string> alertDescriptions = new Dictionary<int, string>
    {
        {1, "Ambiguous citation dates"},
        {2, "Contains 'sic' tag"},
        {3, "DAB page with <ref>s"},
        {4, "Dead links"},
        {5, "Duplicate parameters in WPBannerShell"},
        {6, "Has <ref> after </references>"},
        {7, "Has 'No/More footnotes' template yet many references"},
        {8, "Headers with wikilinks"},
        {9, "Invalid citation parameters"},
        {10, "Links with double pipes"},
        {11, "Links with no target"},
        {12, "Long article with stub tag"},
        {13, "Multiple DEFAULTSORT"},
        {14, "No category (may be one in a template)"},
        {15, "See also section out of place"},
        {16, "Starts with heading"},
        {17, "Unbalanced brackets"},
        {18, "Unclosed tags"},
        {19, "Unformatted references"},
        {20, "Unknown parameters in multiple issues"},
        {21, "Unknown parameters in WikiProject banner shell"},
        {22, "Editor's signature or link to user space"}
    };
}