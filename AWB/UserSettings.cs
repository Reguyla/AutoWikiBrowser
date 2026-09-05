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

using AutoWikiBrowser.Services.Settings;
using Twain.Core;
using Twain.Core.AWBSettings;
using Twain.Core.Plugin;
using Twain.Core.Settings;

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

    /// <summary>
    /// Saves the current application settings to the active settings file.
    /// </summary>
    /// <remarks>
    /// If the active settings file already exists, the user is prompted before it
    /// is replaced.
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
            if (File.Exists(SettingsFile) &&
                MessageBox.Show(
                    "Replace existing file?",
                    "File exists - " + SettingsFile,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1) == DialogResult.No)
            {
                return;
            }

            SavePrefs(SettingsFile);
            return;
        }

        if (MessageBox.Show(
                "No settings file currently loaded. Save as Default?",
                "Save current settings as Default?",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            SavePrefs();
            return;
        }

        saveSettingsAsToolStripMenuItem_Click(
            sender,
            e);
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
        if (SettingsFile != ApplicationPaths.DefaultSettings)
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
                foreach (KeyValuePair<string, IAWBPlugin> plugin in Twain.Core.Plugin.PluginManager.AWBPlugins)
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

            _customModule.ModuleEnabled = false;
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
        _splashScreen.SetProgress(63);

        try
        {
            string recentSettingsValue =
                RegistryUtils.GetValue("\\RecentList", "");

            string[] recentSettings = recentSettingsValue.Split('|');

            UpdateRecentList(recentSettings);
        }
        finally
        {
            _splashScreen.SetProgress(70);
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
        _recentList.Clear();

        foreach (string path in list)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                _recentList.Add(path);
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
        _recentList.Remove(path);
        _recentList.Insert(0, path);

        UpdateRecentSettingsMenu();
    }

    /// <summary>
    /// Removes obsolete default-settings entries from the recent settings list
    /// and limits the list to the five most recent entries.
    /// </summary>
    private void FixupObsoleteRecentSettings()
    {
        _recentList.RemoveAll(path =>
            string.Equals(
                path,
                "Default.xml",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                path,
                ApplicationPaths.DefaultSettings,
                StringComparison.OrdinalIgnoreCase));

        while (_recentList.Count > 5)
        {
            _recentList.RemoveAt(5);
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

        if (_recentList.Count > 0)
        {
            recentToolStripMenuItem.DropDownItems.Add(
                new ToolStripSeparator());
        }

        foreach (string fileName in _recentList)
        {
            ToolStripItem recentSettingsItem =
                recentToolStripMenuItem.DropDownItems.Add(fileName);

            recentSettingsItem.Click += RecentSettingsClick;
        }

        recentToolStripMenuItem.Visible = _recentList.Count > 0;
    }

    /// <summary>
    /// Saves the recent settings file list to the registry.
    /// </summary>
    private void SaveRecentSettingsList()
    {
        RegistryUtils.SetValue(
            string.Empty,
            "RecentList",
            string.Join("|", _recentList));
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
        LoadPrefs(ApplicationPaths.DefaultSettings);
    }

    /// <summary>
    /// Saves the current preferences as the application's default settings.
    /// </summary>
    private void SavePrefs()
    {
        SavePrefs(ApplicationPaths.DefaultSettings);
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

    // TODO: Consider introducing a settings view model or mapper so preference
    // creation no longer depends directly on MainForm controls. This would make
    // settings conversion easier to test and support a future non-WinForms UI.
    /// <summary>
    /// Creates a user preferences object from the current application and
    /// user-interface settings.
    /// </summary>
    /// <returns>
    /// A preferences object containing the current settings.
    /// </returns>
    private UserPrefs MakePrefs()
    {
        return new UserPrefs(
            MakeFindAndReplacePrefs(),
            MakeEditPrefs(),
            new ListPrefs(listMaker, _saveArticleList),
            MakeSkipPrefs(),
            MakeGeneralPrefs(),
            MakeDabPrefs(),
            MakeModulePrefs(),
            _externalProgram.Settings,
            listMaker.SpecialFilterSettings,
            MakeToolsPrefs(),
            Twain.Core.Plugin.PluginManager.AWBPlugins)
        {
            LoginDomain = Variables.LoginDomain
        };
    }

    /// <summary>
    /// Creates the find-and-replace preferences from the current controls.
    /// </summary>
    /// <returns>
    /// The current find-and-replace preferences.
    /// </returns>
    private FaRPrefs MakeFindAndReplacePrefs()
    {
        return new FaRPrefs(
            _findAndReplace,
            _replaceSpecial,
            _substTemplates)
        {
            Enabled = chkFindandReplace.Checked
        };
    }

    /// <summary>
    /// Creates the article editing preferences from the current controls.
    /// </summary>
    /// <returns>
    /// The current article editing preferences.
    /// </returns>
    private EditPrefs MakeEditPrefs()
    {
        return new EditPrefs
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
        };
    }

    /// <summary>
    /// Creates the article skip preferences from the current controls.
    /// </summary>
    /// <returns>
    /// The current article skip preferences.
    /// </returns>
    private SkipPrefs MakeSkipPrefs()
    {
        return new SkipPrefs
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
            GeneralSkipList = _skip.SelectedItems,
            SkipWhenOnlyWhitespaceChanged = chkSkipWhitespace.Checked,
            SkipOnlyCasingChanged = chkSkipCasing.Checked,
            SkipOnlyGeneralFixChanges = chkSkipGeneralFixes.Checked,
            SkipOnlyMinorGeneralFixChanges = chkSkipMinorGeneralFixes.Checked,
            SkipOnlyCosmetic = chkSkipCosmetic.Checked,
            SkipNoLinksOnPage = chkSkipNoPageLinks.Checked,
            SkipIfRedirect = chkSkipIfRedirect.Checked,
            SkipIfNoAlerts = chkSkipIfNoAlerts.Checked
        };
    }

    /// <summary>
    /// Creates the general application preferences from the current controls and
    /// application state.
    /// </summary>
    /// <returns>
    /// The current general application preferences.
    /// </returns>
    private GeneralPrefs MakeGeneralPrefs()
    {
        return new GeneralPrefs(cmboEditSummary.Items)
        {
            SaveArticleList = _saveArticleList,
            IgnoreNoBots = _ignoreNoBots,
            ClearPageListOnProjectChange = _clearPageListOnProjectChange,
            SelectedSummary = cmboEditSummary.Text,
            PasteMore = GetPasteMoreValues(),
            FindText = txtFind.Text,
            FindRegex = chkFindRegex.Checked,
            FindCaseSensitive = chkFindCaseSensitive.Checked,
            WordWrap = wordWrapToolStripMenuItem.Checked,
            ToolBarEnabled = EnableToolBar,
            BypassRedirect = followRedirectsToolStripMenuItem.Checked,
            AutoSaveSettings = autoSaveSettingsToolStripMenuItem.Checked,
            PreParseMode = preParseModeToolStripMenuItem.Checked,
            noSectionEditSummary = noSectionEditSummaryToolStripMenuItem.Checked,
            restrictDefaultsortAddition =
                restrictDefaultsortChangesToolStripMenuItem.Checked,
            restrictOrphanTagging =
                restrictOrphanTaggingToolStripMenuItem.Checked,
            noMOSComplianceFixes =
                noMOSComplianceFixesToolStripMenuItem.Checked,
            syntaxHighlightEditBox =
                syntaxHighlightEditBoxToolStripMenuItem.Checked,
            highlightAllFind = highlightAllFindToolStripMenuItem.Checked,
            NoAutoChanges =
                !automaticallyDoAnythingToolStripMenuItem.Checked,
            OnLoadAction = _actionOnLoad,
            DiffInBotMode = _doDiffInBotMode,
            Minor = chkMinor.Checked,
            AddToWatchlist = addToWatchList.SelectedIndex,
            TimerEnabled = ShowMovingAverageTimer,
            SortListAlphabetically =
                sortAlphabeticallyToolStripMenuItem.Checked,
            AddIgnoredToLog = Article.AddUsingAWBOnArticleAction,
            TextBoxSize = (int)txtEdit.Font.Size,
            TextBoxFont = txtEdit.Font.Name,
            LowThreadPriority = LowThreadPriority,
            Beep = _beep,
            Flash = _flash,
            Minimize = _minimize,
            AutoSaveEdit = MakeEditBoxAutoSavePrefs(),
            LockSummary = chkLock.Checked,
            EditToolbarEnabled = EditToolBarVisible,
            SuppressUsingAWB = _suppressUsingAWB,
            AddUsingAWBToActionSummaries =
                Article.AddUsingAWBOnArticleAction,
            filterNonMainSpace =
                filterOutNonMainSpaceToolStripMenuItem.Checked,
            AutoFilterDuplicates =
                removeDuplicatesToolStripMenuItem.Checked,
            FocusAtEndOfEditBox =
                focusAtEndOfEditTextBoxToolStripMenuItem.Checked,
            scrollToUnbalancedBrackets =
                scrollToAlertsToolStripMenuItem.Checked,
            SortInterWikiOrder =
                alphaSortInterwikiLinksToolStripMenuItem.Checked,
            ReplaceReferenceTags =
                replaceReferenceTagsToolStripMenuItem.Checked,
            LoggingEnabled = _loggingEnabled,
            AlertPreferences = alertPreferences
        };
    }

    /// <summary>
    /// Gets the configured Paste More menu values.
    /// </summary>
    /// <returns>
    /// The ten configured Paste More values, in menu order.
    /// </returns>
    private string[] GetPasteMoreValues()
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
    /// Creates the editor auto-save preferences from the current application
    /// settings.
    /// </summary>
    /// <returns>
    /// The current editor auto-save preferences.
    /// </returns>
    private EditBoxAutoSavePrefs MakeEditBoxAutoSavePrefs()
    {
        return new EditBoxAutoSavePrefs
        {
            Enabled = _autoSaveEditBoxEnabled,
            SavePeriod = AutoSaveEditBoxPeriod,
            SaveFile = _autoSaveEditBoxFile
        };
    }

    /// <summary>
    /// Creates the disambiguation preferences from the current controls.
    /// </summary>
    /// <returns>
    /// The current disambiguation preferences.
    /// </returns>
    private DabPrefs MakeDabPrefs()
    {
        return new DabPrefs
        {
            Enabled = chkEnableDab.Checked,
            Link = txtDabLink.Text,
            Variants = txtDabVariants.Lines,
            ContextChars = (int)udContextChars.Value
        };
    }

    /// <summary>
    /// Creates the custom module preferences from the current module state.
    /// </summary>
    /// <returns>
    /// The current custom module preferences.
    /// </returns>
    private ModulePrefs MakeModulePrefs()
    {
        return new ModulePrefs
        {
            Enabled = _customModule.ModuleEnabled,
            Language = _customModule.Language,
            Code = _customModule.Code
        };
    }

    /// <summary>
    /// Creates the auxiliary tool preferences from the current application state.
    /// </summary>
    /// <returns>
    /// The current auxiliary tool preferences.
    /// </returns>
    private ToolsPrefs MakeToolsPrefs()
    {
        return new ToolsPrefs
        {
            ListComparerUseCurrentArticleList =
                (int)_listComparerUseCurrentArticleList,

            ListSplitterUseCurrentArticleList =
                (int)_listSplitterUseCurrentArticleList,

            DatabaseScannerUseCurrentArticleList =
                (int)_dbScannerUseCurrentArticleList
        };
    }

    // TODO: Consider moving the final splash-screen progress update into a
    // finally block so startup progress continues if preference initialization
    // fails before or outside the file-loading overload.
    /// <summary>
    /// Loads the active settings file, the default settings file, or a new set of
    /// default preferences when no settings file is available.
    /// </summary>
    /// <remarks>
    /// The active settings file is preferred when one has already been selected.
    /// Otherwise, the application's default settings file is loaded when it exists.
    /// If neither is available, a new <see cref="UserPrefs"/> instance is applied.
    /// </remarks>
    private void LoadPrefs()
    {
        _splashScreen.SetProgress(50);

        if (!string.IsNullOrEmpty(SettingsFile))
        {
            LoadPrefs(SettingsFile);
        }
        else if (File.Exists(ApplicationPaths.DefaultSettings))
        {
            LoadPrefs(ApplicationPaths.DefaultSettings);
        }
        else
        {
            LoadPrefs(new UserPrefs());
            SettingsFile = string.Empty;
        }

        _splashScreen.SetProgress(59);
    }

    /// <summary>
    /// Loads application preferences from the specified settings file.
    /// </summary>
    /// <param name="path">
    /// The path of the settings file to load.
    /// </param>
    /// <remarks>
    /// After the settings are loaded successfully, the file becomes the active
    /// settings file and is moved to the top of the recent settings list.
    /// Duplicate article-list entries are also removed when that option is enabled.
    /// </remarks>
    private void LoadPrefs(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            UserPrefs preferences = UserPrefs.LoadPrefs(path);

            LoadPrefs(preferences);

            SettingsFile = path;
            StatusLabelText = "Settings successfully loaded";
            UpdateRecentList(path);

            if (removeDuplicatesToolStripMenuItem.Checked)
            {
                listMaker.RemoveListDuplicates();
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Applies the specified preferences to the current application state and
    /// user-interface controls.
    /// </summary>
    /// <param name="preferences">
    /// The preferences to apply.
    /// </param>
    /// <remarks>
    /// Preference groups are applied in a deliberate order because some controls
    /// raise events when their values change and some settings depend on earlier
    /// initialization steps.
    /// </remarks>
    private void LoadPrefs(UserPrefs preferences)
    {
        LoadProjectPreferences(preferences);
        LoadFindAndReplacePreferences(preferences.FindAndReplace);
        LoadListPreferences(preferences);
        LoadEditPreferences(preferences.Editprefs);
        LoadSkipPreferences(preferences.SkipOptions);
        LoadGeneralPreferences(preferences.General);
        LoadDisambiguationPreferences(preferences.Disambiguation);
        LoadSpecialFilterAndArticleListPreferences(preferences);
        LoadModulePreferences(preferences.Module);
        LoadExternalProgramPreferences(preferences);
        LoadToolPreferences(preferences.Tool);
        LoadPluginPreferences(preferences.Plugin);
    }

    /// <summary>
    /// Applies the project and login-domain preferences.
    /// </summary>
    /// <param name="preferences">
    /// The preferences containing the project configuration.
    /// </param>
    private void LoadProjectPreferences(UserPrefs preferences)
    {
        chkRegExTypo.Checked = false;

        SetProject(
            preferences.LanguageCode,
            preferences.Project,
            preferences.CustomProject,
            preferences.Protocol);

        chkRegExTypo.Checked =
            preferences.Editprefs.RegexTypoFix;

        Variables.LoginDomain =
            preferences.LoginDomain;
    }

    /// <summary>
    /// Applies the find-and-replace, advanced replacement, and template
    /// substitution preferences.
    /// </summary>
    /// <param name="preferences">
    /// The find-and-replace preferences to apply.
    /// </param>
    private void LoadFindAndReplacePreferences(
        FaRPrefs preferences)
    {
        _findAndReplace.Clear();
        chkFindandReplace.Checked = preferences.Enabled;
        _findAndReplace.IgnoreLinks = preferences.IgnoreSomeText;
        _findAndReplace.IgnoreMore = preferences.IgnoreMoreText;
        _findAndReplace.AppendToSummary = preferences.AppendSummary;
        _findAndReplace.AddNew(preferences.Replacements);

        _replaceSpecial.Clear();
        _replaceSpecial.AddNewRule(preferences.AdvancedReps);

        _substTemplates.Clear();
        _substTemplates.TemplateList = preferences.SubstTemplates;
        _substTemplates.ExpandRecursively =
            preferences.ExpandRecursively;
        _substTemplates.IgnoreUnformatted =
            preferences.IgnoreUnformatted;
        _substTemplates.IncludeComments =
            preferences.IncludeComments;

        _findAndReplace.MakeList();
    }

    /// <summary>
    /// Applies the article-list source and general list preferences.
    /// </summary>
    /// <param name="preferences">
    /// The preferences containing the list and general settings.
    /// </param>
    private void LoadListPreferences(UserPrefs preferences)
    {
        listMaker.SourceText =
            preferences.List.ListSource;

        listMaker.SelectedProvider =
            preferences.List.SelectedProvider;

        _saveArticleList =
            preferences.General.SaveArticleList;

        _ignoreNoBots =
            preferences.General.IgnoreNoBots;

        _clearPageListOnProjectChange =
            preferences.General.ClearPageListOnProjectChange;
    }

    /// <summary>
    /// Applies article editing, categorization, image replacement, append-text,
    /// and bot timing preferences.
    /// </summary>
    /// <param name="preferences">
    /// The edit preferences to apply.
    /// </param>
    private void LoadEditPreferences(
        EditPrefs preferences)
    {
        chkGeneralFixes.Checked = preferences.GeneralFixes;
        chkAutoTagger.Checked = preferences.Tagger;
        chkUnicodifyWhole.Checked = preferences.Unicodify;

        cmboCategorise.SelectedIndex =
            preferences.Recategorisation;

        txtNewCategory.Text =
            preferences.NewCategory;

        txtNewCategory2.Text =
            preferences.NewCategory2;

        cmboImages.SelectedIndex =
            preferences.ReImage;

        txtImageReplace.Text =
            preferences.ImageFind;

        txtImageWith.Text =
            preferences.Replace;

        chkSkipNoCatChange.Checked =
            preferences.SkipIfNoCatChange;

        chkRemoveSortKey.Checked =
            preferences.RemoveSortKey;

        chkSkipNoImgChange.Checked =
            preferences.SkipIfNoImgChange;

        chkAppend.Checked =
            preferences.AppendText;

        chkAppendMetaDataSort.Checked =
            preferences.AppendTextMetaDataSort;

        rdoAppend.Checked =
            preferences.Append;

        rdoPrepend.Checked =
            !preferences.Append;

        txtAppendMessage.Text =
            preferences.Text;

        udNewlineChars.Value =
            preferences.Newlines;

        nudBotSpeed.Value =
            preferences.AutoDelay;

        botEditsStop.Value =
            preferences.BotMaxEdits;

        chkSuppressTag.Checked =
            preferences.SupressTag;
    }

    /// <summary>
    /// Applies article skip conditions and skip-list preferences.
    /// </summary>
    /// <param name="preferences">
    /// The skip preferences to apply.
    /// </param>
    private void LoadSkipPreferences(
        SkipPrefs preferences)
    {
        radSkipNonExistent.Checked =
            preferences.SkipNonexistent;

        radSkipExistent.Checked =
            preferences.Skipexistent;

        radSkipNone.Checked =
            preferences.SkipDontCare;

        chkSkipNoChanges.Checked =
            preferences.SkipWhenNoChanges;

        chkSkipSpamFilter.Checked =
            preferences.SkipSpamFilterBlocked;

        chkSkipIfInuse.Checked =
            preferences.SkipInuse;

        chkSkipWhitespace.Checked =
            preferences.SkipWhenOnlyWhitespaceChanged;

        chkSkipCasing.Checked =
            preferences.SkipOnlyCasingChanged;

        chkSkipGeneralFixes.Checked =
            preferences.SkipOnlyGeneralFixChanges;

        chkSkipMinorGeneralFixes.Checked =
            preferences.SkipOnlyMinorGeneralFixChanges;

        chkSkipCosmetic.Checked =
            preferences.SkipOnlyCosmetic;

        chkSkipIfRedirect.Checked =
            preferences.SkipIfRedirect;

        chkSkipIfNoAlerts.Checked =
            preferences.SkipIfNoAlerts;

        LoadContainsSkipPreferences(preferences);
        LoadAdditionalSkipPreferences(preferences);
    }

    /// <summary>
    /// Applies the contains and does-not-contain skip conditions.
    /// </summary>
    /// <param name="preferences">
    /// The skip preferences containing text-match conditions.
    /// </param>
    private void LoadContainsSkipPreferences(
        SkipPrefs preferences)
    {
        skipIfContains.CheckEnabled =
            preferences.SkipDoes;

        skipIfContains.CheckText =
            preferences.SkipDoesText;

        skipIfContains.IsRegex =
            preferences.SkipDoesRegex;

        skipIfContains.IsCaseSensitive =
            preferences.SkipDoesCaseSensitive;

        skipIfContains.After =
            preferences.SkipDoesAfterProcessing;

        skipIfNotContains.CheckEnabled =
            preferences.SkipDoesNot;

        skipIfNotContains.CheckText =
            preferences.SkipDoesNotText;

        skipIfNotContains.IsRegex =
            preferences.SkipDoesNotRegex;

        skipIfNotContains.IsCaseSensitive =
            preferences.SkipDoesNotCaseSensitive;

        skipIfNotContains.After =
            preferences.SkipDoesNotAfterProcessing;
    }

    /// <summary>
    /// Applies the remaining processing-specific skip preferences.
    /// </summary>
    /// <param name="preferences">
    /// The skip preferences to apply.
    /// </param>
    private void LoadAdditionalSkipPreferences(
        SkipPrefs preferences)
    {
        chkSkipWhenNoFAR.Checked =
            preferences.SkipNoFindAndReplace;

        chkSkipOnlyMinorFaR.Checked =
            preferences.SkipMinorFindAndReplace;

        chkSkipIfNoRegexTypo.Checked =
            preferences.SkipNoRegexTypoFix;

        _skip.SelectedItems =
            preferences.GeneralSkipList;

        chkSkipNoDab.Checked =
            preferences.SkipNoDisambiguation;

        chkSkipNoPageLinks.Checked =
            preferences.SkipNoLinksOnPage;
    }

    /// <summary>
    /// Applies general application, edit-summary, editor, toolbar, logging, and
    /// behavior preferences.
    /// </summary>
    /// <param name="preferences">
    /// The general preferences to apply.
    /// </param>
    private void LoadGeneralPreferences(
        GeneralPrefs preferences)
    {
        LoadEditSummaryPreferences(preferences);
        LoadFindPreferences(preferences);
        LoadGeneralMenuPreferences(preferences);
        LoadGeneralApplicationPreferences(preferences);
        LoadEditorPreferences(preferences);
    }

    /// <summary>
    /// Applies edit-summary and Paste More preferences.
    /// </summary>
    /// <param name="preferences">
    /// The general preferences containing summary settings.
    /// </param>
    private void LoadEditSummaryPreferences(
        GeneralPrefs preferences)
    {
        cmboEditSummary.Items.Clear();

        if (preferences.Summaries.Count == 0)
        {
            LoadDefaultEditSummaries();
        }
        else
        {
            foreach (string summary in preferences.Summaries)
            {
                cmboEditSummary.Items.Add(summary);
            }
        }

        chkLock.Checked =
            preferences.LockSummary;

        EditToolBarVisible =
            preferences.EditToolbarEnabled;

        cmboEditSummary.Text =
            preferences.SelectedSummary;

        if (chkLock.Checked)
        {
            lblSummary.Text =
                preferences.SelectedSummary;
        }

        for (int index = 0; index < 10; index++)
        {
            SetPasteMoreText(
                index,
                preferences.PasteMore[index]);
        }
    }

    /// <summary>
    /// Applies the editor find-text preferences.
    /// </summary>
    /// <param name="preferences">
    /// The general preferences containing find settings.
    /// </param>
    private void LoadFindPreferences(
        GeneralPrefs preferences)
    {
        txtFind.Text =
            preferences.FindText;

        chkFindRegex.Checked =
            preferences.FindRegex;

        chkFindCaseSensitive.Checked =
            preferences.FindCaseSensitive;
    }

    /// <summary>
    /// Applies menu-based workflow and processing preferences.
    /// </summary>
    /// <param name="preferences">
    /// The general preferences to apply.
    /// </param>
    private void LoadGeneralMenuPreferences(
        GeneralPrefs preferences)
    {
        wordWrapToolStripMenuItem.Checked =
            preferences.WordWrap;

        EnableToolBar =
            preferences.ToolBarEnabled;

        followRedirectsToolStripMenuItem.Checked =
            preferences.BypassRedirect;

        autoSaveSettingsToolStripMenuItem.Checked =
            preferences.AutoSaveSettings;

        preParseModeToolStripMenuItem.Checked =
            preferences.PreParseMode;

        noSectionEditSummaryToolStripMenuItem.Checked =
            preferences.noSectionEditSummary;

        restrictDefaultsortChangesToolStripMenuItem.Checked =
            preferences.restrictDefaultsortAddition;

        restrictOrphanTaggingToolStripMenuItem.Checked =
            preferences.restrictOrphanTagging;

        noMOSComplianceFixesToolStripMenuItem.Checked =
            preferences.noMOSComplianceFixes;

        syntaxHighlightEditBoxToolStripMenuItem.Checked =
            preferences.syntaxHighlightEditBox;

        highlightAllFindToolStripMenuItem.Checked =
            preferences.highlightAllFind;

        automaticallyDoAnythingToolStripMenuItem.Checked =
            !preferences.NoAutoChanges;

        sortAlphabeticallyToolStripMenuItem.Checked =
            listMaker.AutoAlpha =
                preferences.SortListAlphabetically;

        displayfalsePositivesButtonToolStripMenuItem.Checked =
            preferences.AddIgnoredToLog;

        filterOutNonMainSpaceToolStripMenuItem.Checked =
            preferences.filterNonMainSpace;

        removeDuplicatesToolStripMenuItem.Checked =
            listMaker.FilterDuplicates =
                preferences.AutoFilterDuplicates;

        alphaSortInterwikiLinksToolStripMenuItem.Checked =
            preferences.SortInterWikiOrder;

        replaceReferenceTagsToolStripMenuItem.Checked =
            preferences.ReplaceReferenceTags;

        focusAtEndOfEditTextBoxToolStripMenuItem.Checked =
            preferences.FocusAtEndOfEditBox;

        scrollToAlertsToolStripMenuItem.Checked =
            preferences.scrollToUnbalancedBrackets;
    }

    /// <summary>
    /// Applies general application state and processing preferences.
    /// </summary>
    /// <param name="preferences">
    /// The general preferences to apply.
    /// </param>
    private void LoadGeneralApplicationPreferences(
        GeneralPrefs preferences)
    {
        _actionOnLoad =
            preferences.OnLoadAction;

        _doDiffInBotMode =
            preferences.DiffInBotMode;

        chkMinor.Checked =
            preferences.Minor;

        addToWatchList.SelectedIndex =
            preferences.AddToWatchlist;

        ShowMovingAverageTimer =
            preferences.TimerEnabled;

        alertPreferences =
            preferences.AlertPreferences;

        _autoSaveEditBoxEnabled =
            preferences.AutoSaveEdit.Enabled;

        AutoSaveEditBoxPeriod =
            preferences.AutoSaveEdit.SavePeriod;

        _autoSaveEditBoxFile =
            preferences.AutoSaveEdit.SaveFile;

        _suppressUsingAWB =
            preferences.SuppressUsingAWB;

        Article.AddUsingAWBOnArticleAction =
            preferences.AddUsingAWBToActionSummaries;

        _loggingEnabled =
            preferences.LoggingEnabled;

        LowThreadPriority =
            preferences.LowThreadPriority;

        _flash =
            preferences.Flash;

        _beep =
            preferences.Beep;

        _minimize =
            preferences.Minimize;
    }

    /// <summary>
    /// Applies editor appearance preferences.
    /// </summary>
    /// <param name="preferences">
    /// The general preferences containing editor settings.
    /// </param>
    private void LoadEditorPreferences(
        GeneralPrefs preferences)
    {
        txtEdit.Font = new Font(
            preferences.TextBoxFont,
            preferences.TextBoxSize);
    }

    /// <summary>
    /// Applies disambiguation processing preferences.
    /// </summary>
    /// <param name="preferences">
    /// The disambiguation preferences to apply.
    /// </param>
    private void LoadDisambiguationPreferences(
        DabPrefs preferences)
    {
        chkEnableDab.Checked =
            preferences.Enabled;

        txtDabLink.Text =
            preferences.Link;

        txtDabVariants.Lines =
            preferences.Variants;

        udContextChars.Value =
            preferences.ContextChars;
    }

    /// <summary>
    /// Applies list filtering preferences before populating the article list.
    /// </summary>
    /// <param name="preferences">
    /// The preferences containing filter settings and article-list entries.
    /// </param>
    /// <remarks>
    /// Filter settings must be applied before the article list is populated so
    /// filtering options such as non-mainspace removal affect the loaded list
    /// correctly.
    /// </remarks>
    private void LoadSpecialFilterAndArticleListPreferences(
        UserPrefs preferences)
    {
        listMaker.SpecialFilterSettings =
            preferences.Special;

        listMaker.Add(
            preferences.List.ArticleList);
    }

    /// <summary>
    /// Applies custom module language, source code, and enabled state.
    /// </summary>
    /// <param name="preferences">
    /// The custom module preferences to apply.
    /// </param>
    /// <remarks>
    /// Module code must be loaded before the module is enabled to avoid compiling
    /// incomplete or stale source code.
    /// </remarks>
    private void LoadModulePreferences(
        ModulePrefs preferences)
    {
        _customModule.Language =
            preferences.Language;

        _customModule.Code = NormalizeLineEndings(
            preferences.Code);

        _customModule.ModuleEnabled =
            preferences.Enabled;

        if (!_customModule.ModuleEnabled)
        {
            _customModule.SetModuleNotBuilt();
        }
    }

    /// <summary>
    /// Normalizes text to Windows-style CRLF line endings.
    /// </summary>
    /// <param name="text">
    /// The text to normalize.
    /// </param>
    /// <returns>
    /// The normalized text, or an empty string when <paramref name="text"/> is
    /// <see langword="null"/>.
    /// </returns>
    private static string NormalizeLineEndings(
        string? text)
    {
        return (text ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", "\r\n");
    }

    /// <summary>
    /// Applies external program preferences.
    /// </summary>
    /// <param name="preferences">
    /// The preferences containing external program settings.
    /// </param>
    private void LoadExternalProgramPreferences(
        UserPrefs preferences)
    {
        _externalProgram.Settings =
            preferences.ExternalProgram;
    }

    /// <summary>
    /// Applies auxiliary tool preferences.
    /// </summary>
    /// <param name="preferences">
    /// The tool preferences to apply.
    /// </param>
    private void LoadToolPreferences(
        ToolsPrefs preferences)
    {
        _listComparerUseCurrentArticleList =
            (CurrentArticleListMode)
            preferences.ListComparerUseCurrentArticleList;

        _listSplitterUseCurrentArticleList =
            (CurrentArticleListMode)
            preferences.ListSplitterUseCurrentArticleList;

        _dbScannerUseCurrentArticleList =
            (CurrentArticleListMode)
            preferences.DatabaseScannerUseCurrentArticleList;
    }

    /// <summary>
    /// Applies saved preferences to each currently available plugin.
    /// </summary>
    /// <param name="preferences">
    /// The saved plugin preferences.
    /// </param>
    /// <remarks>
    /// Settings for plugins that are not currently installed or loaded are
    /// ignored.
    /// </remarks>
    private static void LoadPluginPreferences(
        IEnumerable<PluginPrefs> preferences)
    {
        foreach (PluginPrefs pluginPreferences in preferences)
        {
            if (Twain.Core.Plugin.PluginManager.AWBPlugins.TryGetValue(
                    pluginPreferences.Name,
                    out IAWBPlugin plugin))
            {
                plugin.LoadSettings(
                    pluginPreferences.PluginSettings);
            }
        }
    }

    /// <summary>
    /// Updates the text, stored value, and visibility of a Paste More menu item.
    /// </summary>
    /// <param name="item">
    /// The zero-based Paste More menu item index.
    /// </param>
    /// <param name="text">
    /// The text to associate with the menu item.
    /// </param>
    /// <remarks>
    /// Ampersands are escaped before being displayed so they appear literally
    /// rather than being interpreted as WinForms mnemonic markers.
    /// </remarks>
    private void SetPasteMoreText(
        int item,
        string text)
    {
        if (item >= _pasteMoreItems.Length)
        {
            return;
        }

        _pasteMoreItems[item].Tag = text;
        _pasteMoreItems[item].Text =
            _pasteMoreItemsPrefixes[item] +
            (string.IsNullOrEmpty(text)
                ? string.Empty
                : text.Replace("&", "&&"));

        _pasteMoreItems[item].Visible = !string.IsNullOrEmpty(text);
    }
}