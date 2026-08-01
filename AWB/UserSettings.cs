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

using AutoWikiBrowser.Plugins;
using AutoWikiBrowser.Services.Settings;
using System.Windows.Forms;
using WikiFunctions;
using WikiFunctions.AWBSettings;
using WikiFunctions.Plugin;

namespace AutoWikiBrowser;

partial class MainForm
{
    /// <summary>
    /// Prompts the user to save the current application settings as the
    /// default settings.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void saveAsDefaultToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (MessageBox.Show(
                "Are you sure you want to save these settings as the default settings?",
                "Save as default?",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            SavePrefs();
        }
    }

    // TODO: Consider moving settings-file persistence into a dedicated service
    // that performs atomic saves and can restore the backup if replacement fails.
    /// <summary>
    /// Saves the current application settings to the active settings file.
    /// </summary>
    /// <remarks>
    /// If the active settings file already exists, the user is prompted before
    /// it is replaced. The existing file is copied to a file with the
    /// <c>.old</c> extension before the current settings are saved.
    ///
    /// If no settings file is currently active, the user may save the current
    /// settings as the defaults or continue to the Save As workflow.
    /// </remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void saveSettingsToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (!string.IsNullOrEmpty(SettingsFile))
        {
            if (File.Exists(SettingsFile))
            {
                if (MessageBox.Show(
                        "Replace existing file?",
                        "File exists - " + SettingsFile,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1) == DialogResult.No)
                {
                    return;
                }

                // Preserve the previous settings file in case the new settings
                // cannot be saved successfully.
                File.Copy(
                    SettingsFile,
                    SettingsFile + ".old",
                    overwrite: true);
            }

            SavePrefs(SettingsFile);
        }
        else if (MessageBox.Show(
                     "No settings file currently loaded. Save as Default?",
                     "Save current settings as Default?",
                     MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            SavePrefs();
        }
        else
        {
            saveSettingsAsToolStripMenuItem_Click(null, null);
        }
    }

    /// <summary>
    /// Opens the dialog used to load an application settings file.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void loadSettingsToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        LoadSettingsDialog();
    }

    /// <summary>
    /// Prompts the user to restore the original default application settings.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void loadDefaultSettingsToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (MessageBox.Show(
                "Would you really like to load the original default settings?",
                "Reset settings to default?",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            ResetSettings();
        }
    }

    /// <summary>
    /// Prompts the user to select a file for saving the current settings.
    /// </summary>
    /// <remarks>
    /// The selected file becomes the active settings file. The actual save
    /// operation is performed elsewhere in the settings workflow.
    /// </remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void saveSettingsAsToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        if (SettingsFile != AwbDirs.DefaultSettings)
        {
            saveXML.FileName = SettingsFile;
        }

        if (saveXML.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        SettingsFile = saveXML.FileName;
    }

    // TODO: Consider handling plugin reset failures individually so one faulty
    // plugin does not prevent subsequent plugins from being reset.
    //
    /// <summary>
    /// Restores the application settings to the default values defined by
    /// <see cref="UserPrefs"/>.
    /// </summary>
    /// <remarks>
    /// Plugin settings are reset separately so that a plugin failure does not
    /// prevent the core application settings from being restored.
    /// </remarks>
    private void ResetSettings()
    {
        try
        {
            LoadPrefs(new UserPrefs());

            try
            {
                foreach (KeyValuePair<string, IAWBPlugin> plugin in Plugin.AWBPlugins)
                {
                    plugin.Value.Reset();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "A problem occurred while resetting a plugin."
                    + Environment.NewLine
                    + Environment.NewLine
                    + ex.Message,
                    "Plugin reset error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            CModule.ModuleEnabled = false;
            Text = Program.Name;
            StatusLabelText = "Default settings loaded.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error loading settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // TODO: Review the built-in edit summaries to make them wiki-aware rather
    // than English Wikipedia-specific. Consider providing defaults based on the
    // connected wiki (for example, Wikipedia, Wiktionary, Commons, or third-party
    // MediaWiki installations) and allowing projects to supply or override their
    // own default edit summaries.
    /// <summary>
    /// Adds the built-in edit summaries to the edit-summary selection list.
    /// </summary>
    /// <remarks>
    /// This method does not clear existing entries before adding the defaults.
    /// Callers should ensure that it is not invoked repeatedly unless duplicate
    /// entries are acceptable.
    /// </remarks>
    private void LoadDefaultEditSummaries()
    {
        cmboEditSummary.Items.AddRange(
        [
         "[[Wikipedia:AutoWikiBrowser/General fixes|Genfixes]], [[Wikipedia:AutoWikiBrowser/Typos|Typo fixing]] and clean up",
        "Re-categorisation per [[Wikipedia:Categories for discussion|CFD]]",
        "Re-categorisation per [[Wikipedia:Categories for discussion|CFD]] and cleanup",
        "Removing category per [[Wikipedia:Categories for discussion|CFD]]",
        "[[Wikipedia:Template substitution|subst:'ing]]",
        "[[Wikipedia:WikiProject Stub sorting|stub sorting]]",
        "[[Wikipedia:AutoWikiBrowser/Typos|Typo fixing]]",
        "Bad link repair",
        "Fixing [[Wikipedia:Disambiguation pages with links|links to disambiguation pages]]",
        "Unicodifying",
        "Updates to WikiProjects and/or WikiprojectBannerShell"
        ]);
    }

    /// <summary>
    /// Displays the settings-file selection dialog and loads the selected settings
    /// file when the user confirms the selection.
    /// </summary>
    private void LoadSettingsDialog()
    {
        if (openXML.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        LoadPrefs(openXML.FileName);
    }

    // TODO: Consider replacing the delimiter-based recent settings registry value
    // with structured application settings storage that can safely represent file
    // paths and support validation, deduplication, and migration.
    /// <summary>
    /// Loads the recently used settings files from the registry and updates the
    /// corresponding user-interface entries.
    /// </summary>
    /// <remarks>
    /// Splash-screen progress is advanced to 70 even if registry access or recent
    /// settings processing fails.
    /// </remarks>
    private void LoadRecentSettingsList()
    {
        SplashScreen.SetProgress(63);

        try
        {
            string recentSettingsValue =
                RegistryUtils.GetValue("\\RecentList", "");

            string[] recentSettings = recentSettingsValue.Split('|');

            UpdateRecentList(recentSettings);
        }
        finally
        {
            SplashScreen.SetProgress(70);
        }
    }

    // TODO: Consider enforcing a maximum number of recent settings entries
    // to keep the Recent menu and registry value from growing indefinitely.
    /// <summary>
    /// Replaces the current recent settings list with the specified collection
    /// of settings file paths.
    /// </summary>
    /// <param name="list">
    /// The collection of recent settings file paths.
    /// </param>
    private void UpdateRecentList(IEnumerable<string> list)
    {
        RecentList.Clear();

        foreach (string path in list)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                RecentList.Add(path);
            }
        }

        UpdateRecentSettingsMenu();
    }

    /// <summary>
    /// Moves the specified settings file to the top of the recent settings list.
    /// </summary>
    /// <param name="path">
    /// The settings file path to add to the recent settings list.
    /// </param>
    private void UpdateRecentList(string path)
    {
        RecentList.Remove(path);
        RecentList.Insert(0, path);

        UpdateRecentSettingsMenu();
    }

    /// <summary>
    /// Removes obsolete default-settings entries from the recent settings list
    /// and limits the list to the five most recent entries.
    /// </summary>
    private void FixupObsoleteRecentSettings()
    {
        RecentList.RemoveAll(path =>
            string.Equals(
                path,
                "Default.xml",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                path,
                AwbDirs.DefaultSettings,
                StringComparison.OrdinalIgnoreCase));

        while (RecentList.Count > 5)
        {
            RecentList.RemoveAt(5);
        }
    }

    /// <summary>
    /// Rebuilds the Recent Settings menu from the current recent settings list.
    /// </summary>
    /// <remarks>
    /// The menu always includes the default settings entry. Recent settings files
    /// are added after a separator, and the menu is shown only when at least one
    /// recent settings file is available.
    /// </remarks>
    private void UpdateRecentSettingsMenu()
    {
        FixupObsoleteRecentSettings();

        recentToolStripMenuItem.DropDownItems.Clear();

        ToolStripItem defaultSettingsItem =
            recentToolStripMenuItem.DropDownItems.Add("Default settings");

        defaultSettingsItem.Click += DefaultSettingsClick;

        if (RecentList.Count > 0)
        {
            recentToolStripMenuItem.DropDownItems.Add(
                new ToolStripSeparator());
        }

        foreach (string fileName in RecentList)
        {
            ToolStripItem recentSettingsItem =
                recentToolStripMenuItem.DropDownItems.Add(fileName);

            recentSettingsItem.Click += RecentSettingsClick;
        }

        recentToolStripMenuItem.Visible = RecentList.Count > 0;
    }

    /// <summary>
    /// Saves the recent settings file list to the registry.
    /// </summary>
    private void SaveRecentSettingsList()
    {
        RegistryUtils.SetValue(
            string.Empty,
            "RecentList",
            string.Join("|", RecentList));
    }

    /// <summary>
    /// Loads the settings file selected from the Recent Settings menu.
    /// </summary>
    /// <param name="sender">The selected menu item.</param>
    /// <param name="e">The event data.</param>
    private void RecentSettingsClick(
        object sender,
        EventArgs e)
    {
        if (sender is ToolStripItem item)
        {
            LoadPrefs(item.Text);
        }
    }

    /// <summary>
    /// Loads the application's default settings file.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void DefaultSettingsClick(
        object sender,
        EventArgs e)
    {
        LoadPrefs(AwbDirs.DefaultSettings);
    }

    /// <summary>
    /// Saves the current preferences as the application's default settings.
    /// </summary>
    private void SavePrefs()
    {
        SavePrefs(AwbDirs.DefaultSettings);
    }

    /// <summary>
    /// Saves the current user preferences to the specified settings file.
    /// </summary>
    /// <param name="path">
    /// The destination settings file.
    /// </param>
    /// <remarks>
    /// File persistence and backup recovery are delegated to
    /// <see cref="SettingsPersistenceService"/>. This method remains responsible
    /// for updating application state and displaying errors to the user.
    /// </remarks>
    private void SavePrefs(string path)
    {
        SettingsSaveResult result =
            _settingsPersistenceService.Save(
                MakePrefs(),
                path);

        if (result.Succeeded)
        {
            UpdateRecentList(path);
            SettingsFile = path;
            return;
        }

        if (result.Failure ==
            SettingsSaveFailure.UnauthorizedAccess)
        {
            MessageBox.Show(
                "Saving settings failed due to insufficient permissions.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        if (result.Exception != null)
        {
            ErrorHandler.HandleException(result.Exception);
        }
    }

    /// <summary>
    /// Make preferences object from current settings
    /// </summary>
    private UserPrefs MakePrefs()
    {
        return new UserPrefs(

            new FaRPrefs(FindAndReplace, RplcSpecial, SubstTemplates)
            {
                Enabled = chkFindandReplace.Checked,
            },

            new EditPrefs
            {
                GeneralFixes = chkGeneralFixes.Checked,
                Tagger = chkAutoTagger.Checked,
                Unicodify = chkUnicodifyWhole.Checked,
                Recategorisation = cmboCategorise.SelectedIndex,
                NewCategory = txtNewCategory.Text,
                NewCategory2 = txtNewCategory2.Text,
                ReImage = cmboImages.SelectedIndex,
                ImageFind = txtImageReplace.Text,
                Replace = txtImageWith.Text,
                SkipIfNoCatChange = chkSkipNoCatChange.Checked,
                RemoveSortKey = chkRemoveSortKey.Checked,
                SkipIfNoImgChange = chkSkipNoImgChange.Checked,
                AppendText = chkAppend.Checked,
                AppendTextMetaDataSort = chkAppendMetaDataSort.Checked,
                Append = !rdoPrepend.Checked,
                Text = txtAppendMessage.Text,
                Newlines = (int)udNewlineChars.Value,
                AutoDelay = (int)nudBotSpeed.Value,
                BotMaxEdits = (int)botEditsStop.Value,
                SupressTag = chkSuppressTag.Checked,
                RegexTypoFix = chkRegExTypo.Checked
            },

            new ListPrefs(listMaker, _saveArticleList),

            new SkipPrefs
            {
                SkipNonexistent = radSkipNonExistent.Checked,
                Skipexistent = radSkipExistent.Checked,
                SkipDontCare = radSkipNone.Checked,
                SkipWhenNoChanges = chkSkipNoChanges.Checked,
                SkipSpamFilterBlocked = chkSkipSpamFilter.Checked,
                SkipInuse = chkSkipIfInuse.Checked,
                SkipDoes = skipIfContains.CheckEnabled,
                SkipDoesText = skipIfContains.CheckText,
                SkipDoesRegex = skipIfContains.IsRegex,
                SkipDoesCaseSensitive = skipIfContains.IsCaseSensitive,
                SkipDoesAfterProcessing = skipIfContains.After,
                SkipDoesNot = skipIfNotContains.CheckEnabled,
                SkipDoesNotText = skipIfNotContains.CheckText,
                SkipDoesNotRegex = skipIfNotContains.IsRegex,
                SkipDoesNotCaseSensitive = skipIfNotContains.IsCaseSensitive,
                SkipDoesNotAfterProcessing = skipIfNotContains.After,
                SkipNoFindAndReplace = chkSkipWhenNoFAR.Checked,
                SkipMinorFindAndReplace = chkSkipOnlyMinorFaR.Checked,
                SkipNoRegexTypoFix = chkSkipIfNoRegexTypo.Checked,
                SkipNoDisambiguation = chkSkipNoDab.Checked,
                GeneralSkipList = Skip.SelectedItems,
                SkipWhenOnlyWhitespaceChanged = chkSkipWhitespace.Checked,
                SkipOnlyCasingChanged = chkSkipCasing.Checked,
                SkipOnlyGeneralFixChanges = chkSkipGeneralFixes.Checked,
                SkipOnlyMinorGeneralFixChanges = chkSkipMinorGeneralFixes.Checked,
                SkipOnlyCosmetic = chkSkipCosmetic.Checked,
                SkipNoLinksOnPage = chkSkipNoPageLinks.Checked,
                SkipIfRedirect = chkSkipIfRedirect.Checked,
                SkipIfNoAlerts = chkSkipIfNoAlerts.Checked
            },

            new GeneralPrefs(cmboEditSummary.Items)
            {
                SaveArticleList = _saveArticleList,
                IgnoreNoBots = IgnoreNoBots,
                ClearPageListOnProjectChange = ClearPageListOnProjectChange,
                SelectedSummary = cmboEditSummary.Text,

                PasteMore = new[]
                                    {
                                        (string) PasteMore1.Tag,
                                        (string) PasteMore2.Tag,
                                        (string) PasteMore3.Tag,
                                        (string) PasteMore4.Tag,
                                        (string) PasteMore5.Tag,
                                        (string) PasteMore6.Tag,
                                        (string) PasteMore7.Tag,
                                        (string) PasteMore8.Tag,
                                        (string) PasteMore9.Tag,
                                        (string) PasteMore10.Tag
                                    },
                FindText = txtFind.Text,
                FindRegex = chkFindRegex.Checked,
                FindCaseSensitive = chkFindCaseSensitive.Checked,
                WordWrap = wordWrapToolStripMenuItem1.Checked,
                ToolBarEnabled = EnableToolBar,
                BypassRedirect = followRedirectsToolStripMenuItem.Checked,
                AutoSaveSettings = autoSaveSettingsToolStripMenuItem.Checked,
                PreParseMode = preParseModeToolStripMenuItem.Checked,
                noSectionEditSummary = noSectionEditSummaryToolStripMenuItem.Checked,
                restrictDefaultsortAddition = restrictDefaultsortChangesToolStripMenuItem.Checked,
                restrictOrphanTagging = restrictOrphanTaggingToolStripMenuItem.Checked,
                noMOSComplianceFixes = noMOSComplianceFixesToolStripMenuItem.Checked,
                syntaxHighlightEditBox = syntaxHighlightEditBoxToolStripMenuItem.Checked,
                highlightAllFind = highlightAllFindToolStripMenuItem.Checked,
                NoAutoChanges = !automaticallyDoAnythingToolStripMenuItem.Checked,
                OnLoadAction = actionOnLoad,
                DiffInBotMode = doDiffInBotMode,
                Minor = chkMinor.Checked,
                AddToWatchlist = addToWatchList.SelectedIndex,
                TimerEnabled = ShowMovingAverageTimer,
                SortListAlphabetically = sortAlphabeticallyToolStripMenuItem.Checked,
                AddIgnoredToLog = Article.AddUsingAWBOnArticleAction,
                TextBoxSize = (int)txtEdit.Font.Size,
                TextBoxFont = txtEdit.Font.Name,
                LowThreadPriority = LowThreadPriority,
                Beep = _beep,
                Flash = _flash,
                Minimize = _minimize,
                AutoSaveEdit = new EditBoxAutoSavePrefs
                {
                    Enabled = _autoSaveEditBoxEnabled,
                    SavePeriod = AutoSaveEditBoxPeriod,
                    SaveFile = _autoSaveEditBoxFile
                },
                LockSummary = chkLock.Checked,
                EditToolbarEnabled = EditToolBarVisible,
                SuppressUsingAWB = _suppressUsingAWB,
                AddUsingAWBToActionSummaries = Article.AddUsingAWBOnArticleAction,
                filterNonMainSpace = filterOutNonMainSpaceToolStripMenuItem.Checked,
                AutoFilterDuplicates = removeDuplicatesToolStripMenuItem.Checked,
                FocusAtEndOfEditBox = focusAtEndOfEditTextBoxToolStripMenuItem.Checked,
                scrollToUnbalancedBrackets = scrollToAlertsToolStripMenuItem.Checked,

                SortInterWikiOrder = alphaSortInterwikiLinksToolStripMenuItem.Checked,
                ReplaceReferenceTags = replaceReferenceTagsToolStripMenuItem.Checked,
                LoggingEnabled = loggingEnabled,
                AlertPreferences = alertPreferences
            },

            new DabPrefs
            {
                Enabled = chkEnableDab.Checked,
                Link = txtDabLink.Text,
                Variants = txtDabVariants.Lines,
                ContextChars = (int)udContextChars.Value
            },

            new ModulePrefs
            {
                Enabled = CModule.ModuleEnabled,
                Language = CModule.Language,
                Code = CModule.Code
            },

            ExtProgram.Settings,
            listMaker.SpecialFilterSettings,

            new ToolsPrefs
            {
                ListComparerUseCurrentArticleList = _listComparerUseCurrentArticleList,
                ListSplitterUseCurrentArticleList = _listSplitterUseCurrentArticleList,
                DatabaseScannerUseCurrentArticleList = _dbScannerUseCurrentArticleList
            },

            Plugin.AWBPlugins
            )
        {
            LoginDomain = Variables.LoginDomain
        };
    }

    /// <summary>
    /// Load default preferences
    /// </summary>
    private void LoadPrefs()
    {
        SplashScreen.SetProgress(50);

        if (!string.IsNullOrEmpty(SettingsFile))
            LoadPrefs(SettingsFile);
        else
            if (File.Exists(AwbDirs.DefaultSettings))
            LoadPrefs(AwbDirs.DefaultSettings);
        else
        {
            LoadPrefs(new UserPrefs());
            SettingsFile = "";
        }

        SplashScreen.SetProgress(59);
    }

    /// <summary>
    /// Load preferences from file
    /// </summary>
    private void LoadPrefs(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            LoadPrefs(UserPrefs.LoadPrefs(path));

            SettingsFile = path;
            StatusLabelText = "Settings successfully loaded";
            UpdateRecentList(path);

            if (removeDuplicatesToolStripMenuItem.Checked)
                listMaker.RemoveListDuplicates();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Load preferences object
    /// </summary>
    private void LoadPrefs(UserPrefs p)
    {
        chkRegExTypo.Checked = false;
        SetProject(p.LanguageCode, p.Project, p.CustomProject, p.Protocol);
        chkRegExTypo.Checked = p.Editprefs.RegexTypoFix;
        Variables.LoginDomain = p.LoginDomain;

        FindAndReplace.Clear();
        chkFindandReplace.Checked = p.FindAndReplace.Enabled;
        FindAndReplace.IgnoreLinks = p.FindAndReplace.IgnoreSomeText;
        FindAndReplace.IgnoreMore = p.FindAndReplace.IgnoreMoreText;
        FindAndReplace.AppendToSummary = p.FindAndReplace.AppendSummary;
        FindAndReplace.AddNew(p.FindAndReplace.Replacements);

        RplcSpecial.Clear();
        RplcSpecial.AddNewRule(p.FindAndReplace.AdvancedReps);

        SubstTemplates.Clear();
        SubstTemplates.TemplateList = p.FindAndReplace.SubstTemplates;
        SubstTemplates.ExpandRecursively = p.FindAndReplace.ExpandRecursively;
        SubstTemplates.IgnoreUnformatted = p.FindAndReplace.IgnoreUnformatted;
        SubstTemplates.IncludeComments = p.FindAndReplace.IncludeComments;

        FindAndReplace.MakeList();

        listMaker.SourceText = p.List.ListSource;
        listMaker.SelectedProvider = p.List.SelectedProvider;

        _saveArticleList = p.General.SaveArticleList;

        IgnoreNoBots = p.General.IgnoreNoBots;
        ClearPageListOnProjectChange = p.General.ClearPageListOnProjectChange;

        chkGeneralFixes.Checked = p.Editprefs.GeneralFixes;
        chkAutoTagger.Checked = p.Editprefs.Tagger;
        chkUnicodifyWhole.Checked = p.Editprefs.Unicodify;

        cmboCategorise.SelectedIndex = p.Editprefs.Recategorisation;
        txtNewCategory.Text = p.Editprefs.NewCategory;
        txtNewCategory2.Text = p.Editprefs.NewCategory2;

        cmboImages.SelectedIndex = p.Editprefs.ReImage;
        txtImageReplace.Text = p.Editprefs.ImageFind;
        txtImageWith.Text = p.Editprefs.Replace;

        chkSkipNoCatChange.Checked = p.Editprefs.SkipIfNoCatChange;
        chkRemoveSortKey.Checked = p.Editprefs.RemoveSortKey;
        chkSkipNoImgChange.Checked = p.Editprefs.SkipIfNoImgChange;

        chkAppend.Checked = p.Editprefs.AppendText;
        chkAppendMetaDataSort.Checked = p.Editprefs.AppendTextMetaDataSort;
        rdoAppend.Checked = p.Editprefs.Append;
        rdoPrepend.Checked = !p.Editprefs.Append;
        txtAppendMessage.Text = p.Editprefs.Text;
        udNewlineChars.Value = p.Editprefs.Newlines;

        nudBotSpeed.Value = p.Editprefs.AutoDelay;
        botEditsStop.Value = p.Editprefs.BotMaxEdits;
        chkSuppressTag.Checked = p.Editprefs.SupressTag;

        radSkipNonExistent.Checked = p.SkipOptions.SkipNonexistent;
        radSkipExistent.Checked = p.SkipOptions.Skipexistent;
        radSkipNone.Checked = p.SkipOptions.SkipDontCare;
        chkSkipNoChanges.Checked = p.SkipOptions.SkipWhenNoChanges;
        chkSkipSpamFilter.Checked = p.SkipOptions.SkipSpamFilterBlocked;
        chkSkipIfInuse.Checked = p.SkipOptions.SkipInuse;
        chkSkipWhitespace.Checked = p.SkipOptions.SkipWhenOnlyWhitespaceChanged;
        chkSkipCasing.Checked = p.SkipOptions.SkipOnlyCasingChanged;
        chkSkipGeneralFixes.Checked = p.SkipOptions.SkipOnlyGeneralFixChanges;
        chkSkipMinorGeneralFixes.Checked = p.SkipOptions.SkipOnlyMinorGeneralFixChanges;
        chkSkipCosmetic.Checked = p.SkipOptions.SkipOnlyCosmetic;
        chkSkipIfRedirect.Checked = p.SkipOptions.SkipIfRedirect;
        chkSkipIfNoAlerts.Checked = p.SkipOptions.SkipIfNoAlerts;

        skipIfContains.CheckEnabled = p.SkipOptions.SkipDoes;
        skipIfContains.CheckText = p.SkipOptions.SkipDoesText;
        skipIfContains.IsRegex = p.SkipOptions.SkipDoesRegex;
        skipIfContains.IsCaseSensitive = p.SkipOptions.SkipDoesCaseSensitive;
        skipIfContains.After = p.SkipOptions.SkipDoesAfterProcessing;

        skipIfNotContains.CheckEnabled = p.SkipOptions.SkipDoesNot;
        skipIfNotContains.CheckText = p.SkipOptions.SkipDoesNotText;
        skipIfNotContains.IsRegex = p.SkipOptions.SkipDoesNotRegex;
        skipIfNotContains.IsCaseSensitive = p.SkipOptions.SkipDoesNotCaseSensitive;
        skipIfNotContains.After = p.SkipOptions.SkipDoesNotAfterProcessing;

        chkSkipWhenNoFAR.Checked = p.SkipOptions.SkipNoFindAndReplace;
        chkSkipOnlyMinorFaR.Checked = p.SkipOptions.SkipMinorFindAndReplace;
        chkSkipIfNoRegexTypo.Checked = p.SkipOptions.SkipNoRegexTypoFix;
        Skip.SelectedItems = p.SkipOptions.GeneralSkipList;
        chkSkipNoDab.Checked = p.SkipOptions.SkipNoDisambiguation;
        chkSkipNoPageLinks.Checked = p.SkipOptions.SkipNoLinksOnPage;

        cmboEditSummary.Items.Clear();

        if (p.General.Summaries.Count == 0)
            LoadDefaultEditSummaries();
        else
            foreach (string s in p.General.Summaries)
                cmboEditSummary.Items.Add(s);

        chkLock.Checked = p.General.LockSummary;
        EditToolBarVisible = p.General.EditToolbarEnabled;

        cmboEditSummary.Text = p.General.SelectedSummary;

        if (chkLock.Checked)
            lblSummary.Text = p.General.SelectedSummary;

        for (int i = 0; i < 10; ++i)
            SetPasteMoreText(i, p.General.PasteMore[i]);

        txtFind.Text = p.General.FindText;
        chkFindRegex.Checked = p.General.FindRegex;
        chkFindCaseSensitive.Checked = p.General.FindCaseSensitive;

        wordWrapToolStripMenuItem1.Checked = p.General.WordWrap;
        EnableToolBar = p.General.ToolBarEnabled;
        followRedirectsToolStripMenuItem.Checked = p.General.BypassRedirect;
        autoSaveSettingsToolStripMenuItem.Checked = p.General.AutoSaveSettings;
        preParseModeToolStripMenuItem.Checked = p.General.PreParseMode;
        noSectionEditSummaryToolStripMenuItem.Checked = p.General.noSectionEditSummary;
        restrictDefaultsortChangesToolStripMenuItem.Checked = p.General.restrictDefaultsortAddition;
        restrictOrphanTaggingToolStripMenuItem.Checked = p.General.restrictOrphanTagging;
        noMOSComplianceFixesToolStripMenuItem.Checked = p.General.noMOSComplianceFixes;
        syntaxHighlightEditBoxToolStripMenuItem.Checked = p.General.syntaxHighlightEditBox;
        highlightAllFindToolStripMenuItem.Checked = p.General.highlightAllFind;
        automaticallyDoAnythingToolStripMenuItem.Checked = !p.General.NoAutoChanges;
        actionOnLoad = p.General.OnLoadAction;
        doDiffInBotMode = p.General.DiffInBotMode;
        chkMinor.Checked = p.General.Minor;
        addToWatchList.SelectedIndex = p.General.AddToWatchlist;
        ShowMovingAverageTimer = p.General.TimerEnabled;
        alertPreferences = p.General.AlertPreferences;

        sortAlphabeticallyToolStripMenuItem.Checked = listMaker.AutoAlpha = p.General.SortListAlphabetically;
        displayfalsePositivesButtonToolStripMenuItem.Checked = p.General.AddIgnoredToLog;

        _autoSaveEditBoxEnabled = p.General.AutoSaveEdit.Enabled;
        AutoSaveEditBoxPeriod = p.General.AutoSaveEdit.SavePeriod;
        _autoSaveEditBoxFile = p.General.AutoSaveEdit.SaveFile;

        _suppressUsingAWB = p.General.SuppressUsingAWB;
        Article.AddUsingAWBOnArticleAction = p.General.AddUsingAWBToActionSummaries;

        filterOutNonMainSpaceToolStripMenuItem.Checked = p.General.filterNonMainSpace;
        removeDuplicatesToolStripMenuItem.Checked = listMaker.FilterDuplicates = p.General.AutoFilterDuplicates;

        alphaSortInterwikiLinksToolStripMenuItem.Checked = p.General.SortInterWikiOrder;
        replaceReferenceTagsToolStripMenuItem.Checked = p.General.ReplaceReferenceTags;
        focusAtEndOfEditTextBoxToolStripMenuItem.Checked = p.General.FocusAtEndOfEditBox;
        scrollToAlertsToolStripMenuItem.Checked = p.General.scrollToUnbalancedBrackets;

        txtEdit.Font = new System.Drawing.Font(p.General.TextBoxFont, p.General.TextBoxSize);

        loggingEnabled = p.General.LoggingEnabled;

        LowThreadPriority = p.General.LowThreadPriority;
        _flash = p.General.Flash;
        _beep = p.General.Beep;

        _minimize = p.General.Minimize;

        chkEnableDab.Checked = p.Disambiguation.Enabled;
        txtDabLink.Text = p.Disambiguation.Link;
        txtDabVariants.Lines = p.Disambiguation.Variants;
        udContextChars.Value = p.Disambiguation.ContextChars;

        listMaker.SpecialFilterSettings = p.Special;
        // ensure listmaker is only populated once listmaker filter settings (remove non-mainspace etc.) have been loaded
        listMaker.Add(p.List.ArticleList);

        CModule.Language = p.Module.Language;
        CModule.Code = (p.Module.Code ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", "\r\n");
        // Don't enable custom module until code loaded, prevents phantom compile error
        CModule.ModuleEnabled = p.Module.Enabled;
        if (!CModule.ModuleEnabled)
            CModule.SetModuleNotBuilt();

        ExtProgram.Settings = p.ExternalProgram;

        _listComparerUseCurrentArticleList = p.Tool.ListComparerUseCurrentArticleList;
        _listSplitterUseCurrentArticleList = p.Tool.ListSplitterUseCurrentArticleList;
        _dbScannerUseCurrentArticleList = p.Tool.DatabaseScannerUseCurrentArticleList;

        foreach (PluginPrefs pp in p.Plugin)
        {
            IAWBPlugin plugin;
            if (Plugin.AWBPlugins.TryGetValue(pp.Name, out plugin))
                plugin.LoadSettings(pp.PluginSettings);
        }
    }

    private void SetPasteMoreText(int item, string s)
    {
        if (item < _pasteMoreItems.Length)
        {
            _pasteMoreItems[item].Tag = s;
            _pasteMoreItems[item].Text = _pasteMoreItemsPrefixes[item] +
                                        (string.IsNullOrEmpty(s) ? "" : s.Replace("&", "&&"));
            _pasteMoreItems[item].Visible = !string.IsNullOrEmpty(s);
        }
    }
}