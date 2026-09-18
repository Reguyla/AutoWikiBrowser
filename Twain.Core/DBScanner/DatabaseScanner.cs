/*
Database Scanner
Copyright (C) 2007 Martin Richards

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
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Twain.Core.Background;
using Twain.Core.Controls.Lists;
using Twain.Core.Lists;

namespace Twain.Core.DBScanner;

//TODO:Load proper protection levels
/// <summary>
/// Provides a form and functions for searching XML data dumps
/// </summary>
public partial class DatabaseScanner : Form
{
    private MainProcess Main;
    private TimeSpan StartTime;
    private readonly ListMaker LMaker;

    private readonly ListFilterForm SpecialFilter;

    private ThreadPriority priority = ThreadPriority.Normal;
    ThreadPriority Priority
    {
        get { return priority; }
        set
        {
            if (Main != null)
                Main.Priority = value;
            priority = value;
        }
    }

    int Matches;
    int Limit = 100000;

    public DatabaseScanner()
    {
        InitializeComponent();
        SpecialFilter = new ListFilterForm(lbArticles);
    }

    public DatabaseScanner(ListMaker lm)
        : this()
    {
        LMaker = lm;
    }

    private void DatabaseScanner_Load(object sender, EventArgs e)
    {
        cmboLength.SelectedIndex = 0;
        cmboLinks.SelectedIndex = 0;
        cmboWords.SelectedIndex = 0;
    }

    #region main process

    private void StartButton()
    {
        try
        {
            if (Main != null)
                return;

            if (FileName.Length == 0)
            {
                MessageBox.Show("Please open an \"Pages\" XML data-dump file\r\n\r\nSee the About menu for where to download this file.", "Problem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lbArticles.Items.Clear();
            lblCount.Text = string.Empty;

            Matches = 0;
            lblPercentageComplete.Text = "0%";

            UpdateControls(true);

            Start();

            timerProgessUpdate.Enabled = true;
        }
        catch (Exception ex)
        {
            StopButton(false);
            ErrorHandler.HandleException(ex);
            UpdateControls(false);
        }
    }

    private readonly CrossThreadQueue<string> Queue = new CrossThreadQueue<string>();

    /// <summary>
    /// Captures the current database scanner settings from the user interface.
    /// </summary>
    private DatabaseScannerOptions CreateOptions()
    {
        return new DatabaseScannerOptions
        {
            FileName = FileName,
            StartFrom = Tools.TurnFirstToUpper(txtStartFrom.Text),

            Priority = Priority,
            ResultLimit = (int)nudLimitResults.Value,

            IgnoreRedirects = chkIgnoreRedirects.Checked,
            IgnoreComments = chkIgnoreComments.Checked,

            Namespaces = pageNamespaces.GetSelectedNamespaces().ToList(),

            TitleContainsEnabled = chkTitleContains.Checked,
            TitleContains = txtTitleContains.Text,
            TitleDoesNotContainEnabled = chkTitleDoesNotContain.Checked,
            TitleDoesNotContain = txtTitleNotContains.Text,
            TitleRegex = chkTitleRegex.Checked,
            TitleCaseSensitive = chkTitleCaseSensitive.Checked,

            ArticleContainsEnabled = chkArticleDoesContain.Checked,
            ArticleContains = txtArticleDoesContain.Text,
            ArticleDoesNotContainEnabled = chkArticleDoesNotContain.Checked,
            ArticleDoesNotContain = txtArticleDoesNotContain.Text,
            ArticleRegex = chkArticleRegex.Checked,
            ArticleCaseSensitive = chkArticleCaseSensitive.Checked,
            ArticleRegexMultiline = chkMulti.Checked,
            ArticleRegexSingleline = chkSingle.Checked,

            SearchDates = chkSearchDates.Checked,
            DateFrom = dtpFrom.Value,
            DateTo = dtpTo.Value,

            CheckProtection = chkProtection.Checked,
            EditProtectionLevel = MoveDelete.EditProtectionLevel,
            MoveProtectionLevel = MoveDelete.MoveProtectionLevel,

            LengthComparison = cmboLength.SelectedIndex,
            Length = (int)nudLength.Value,

            LinkComparison = cmboLinks.SelectedIndex,
            Links = (int)nudLinks.Value,

            WordComparison = cmboWords.SelectedIndex,
            Words = (int)nudWords.Value,

            CheckBadLinks = chkBadLinks.Checked,
            CheckNoBoldTitle = chkNoBold.Checked,
            CheckCiteTemplateDates = chkCiteTemplateDates.Checked,
            CheckReorderReferences = chkReorderReferences.Checked,
            CheckPeopleCategories = chkPeopleCategories.Checked,
            CheckUnbalancedBrackets = chkUnbalancedBrackets.Checked,
            CheckSimpleLinks = chkSimpleLinks.Checked,
            CheckHtmlEntities = chkHasHTML.Checked,
            CheckSectionErrors = chkHeaderError.Checked,
            CheckUnbulletedLinks = chkUnbulletedLinks.Checked,
            CheckTypos = chkTypo.Checked,
            CheckMissingDefaultSort = chkDefaultSort.Checked
        };
    }

    private void Start()
    {
        DatabaseScannerOptions options = CreateOptions();

        StartTime = new TimeSpan(
            DateTime.Now.Day,
            DateTime.Now.Hour,
            DateTime.Now.Minute,
            DateTime.Now.Second,
            DateTime.Now.Millisecond);

        Limit = options.ResultLimit;

        List<Scan> scanners =
            DatabaseScannerProcessor.CreateScanners(options);

        Main = new MainProcess(
            scanners,
            options.FileName,
            options.Priority,
            options.IgnoreComments,
            options.StartFrom)
        {
            OutputQueue = Queue
        };

        progressBar.Value = 0;
        Main.StoppedEvent += Stopped;
        Main.Start();
    }

    private void AddArticle(string article)
    {
        if (string.IsNullOrEmpty(article))
            return;

        Article a = new Article(article);
        lbArticles.Items.Add(a);

        if (LMaker != null)
            LMaker.AddWithoutUpdate(a);

        Matches++;

        if (Matches >= Limit)
            Main.Stop();
    }

    private void Stopped()
    {
        try
        {
            if (Main != null)
            {
                Main.StoppedEvent -= Stopped;
            }

            timerProgessUpdate.Enabled = false;

            UpdateList();

            UpdateDBScannerArticleCount();
            UpdateProgressBar();

            if (Main != null && Main.Message)
            {
                TimeSpan endTime = new TimeSpan(DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute,
                            DateTime.Now.Second, DateTime.Now.Millisecond).Subtract(StartTime);
                lblCount.Text += " in " + endTime.ToString().TrimEnd('0');

                // advise user if stopped due to reaching article match limit
                if (Matches >= Limit)
                    lblCount.Text += " limit of " + Limit + " reached";
            }

            Main = null;
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
        finally
        {
            UpdateControls(false);
        }
    }

    private void UpdateListMakerCount()
    {
        if (LMaker != null) LMaker.UpdateNumberOfArticles();
    }

    private void RemoveDBScannerListItemsFromListMaker()
    {
        if (LMaker != null)
        {
            foreach (Article article in lbArticles)
                LMaker.RemoveWithoutUpdate(article);
        }
    }

    #endregion

    # region other

    private void WikifyToList()
    {
        StringBuilder strbList = new StringBuilder();
        string s, l = string.Empty;
        int intHeadingSpace = System.Convert.ToInt32(nudHeadingSpace.Value);

        string strBullet = rdoHash.Checked ? "#" : "*";

        if (chkHeading.Checked)
        {
            int intSection = 0, intSectionNumber = 0, i = 0;

            strbList.AppendLine("==0==");
            intSectionNumber++;

            foreach (Article a in lbArticles.Items)
            {
                i++;
                s = a.ToString().Replace("&amp;", "&");
                if (a.NameSpaceKey == Namespace.File)
                {
                    s = ":" + s; //images should be inlined
                }

                strbList.AppendLine(strBullet + " [[" + s + "]]");

                intSection++;
                if (intSection == intHeadingSpace && i != lbArticles.Items.Count)
                {
                    strbList.AppendLine("\r\n==" + intSectionNumber + "==");
                    intSectionNumber++;
                    intSection = 0;
                }
            }
        }
        else if (chkABCHeader.Checked)
        {
            foreach (Article a in lbArticles.Items)
            {
                s = a.ToString().Replace("&amp;", "&");

                string sr = (s.Length > 1) ? s.Remove(1) : s;

                sr = Tools.RemoveDiacritics(sr);

                if (sr != l)
                    strbList.AppendLine("\r\n== " + sr + " ==");

                strbList.AppendLine(strBullet + " [[" + s + "]]");

                l = sr;
            }
        }
        else
        {
            foreach (Article a in lbArticles.Items)
            {
                strbList.AppendLine(strBullet + " [[" + a.ToString().Replace("&amp;", "&") + "]]");
            }
        }

        txtList.Text = strbList.ToString().Trim();
    }

    private string File = string.Empty;
    private string FileName
    {
        get { return File; }
        set
        {
            File = value;

            if (value.Length > 0)
            {
                Text = "Wiki Database Scanner - " + Path.GetFileName(FileName);
            }
            else
            {
                Text = "Wiki Database Scanner";
            }
        }
    }

    private void SaveConvertedList()
    {
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            Tools.WriteTextFileAbsolutePath(txtList.Text, saveFileDialog.FileName, false);
        }
    }

    private void btnSaveTxtList_Click(object sender, EventArgs e)
    {
        SaveConvertedList();
    }

    private void chkRegex_CheckedChanged(object sender, EventArgs e)
    {
        bool regex = chkArticleRegex.Checked;
        chkMulti.Enabled = regex;
        chkSingle.Enabled = regex;
    }

    private void btnClear_Click(object sender, EventArgs e)
    {
        txtList.Clear();
    }

    private void btnCopy_Click(object sender, EventArgs e)
    {
        txtList.SelectAll();
        txtList.Copy();
        txtList.Select(0, 0);
    }

    private void chkDoesContain_CheckedChanged(object sender, EventArgs e)
    {
        txtArticleDoesContain.Enabled = chkArticleDoesContain.Checked;
    }

    private void chkDoesNotContain_CheckedChanged(object sender, EventArgs e)
    {
        txtArticleDoesNotContain.Enabled = chkArticleDoesNotContain.Checked;
    }

    private void cmboLength_SelectedIndexChanged(object sender, EventArgs e)
    {
        nudLength.Enabled = (cmboLength.SelectedIndex != 0);
    }

    private void cmboWords_SelectedIndexChanged(object sender, EventArgs e)
    {
        nudWords.Enabled = (cmboWords.SelectedIndex != 0);
    }

    private void cmboLinks_SelectedIndexChanged(object sender, EventArgs e)
    {
        nudLinks.Enabled = (cmboLinks.SelectedIndex != 0);
    }

    private void chkHeading_CheckedChanged(object sender, EventArgs e)
    {
        nudHeadingSpace.Enabled = chkHeading.Checked;

        if (chkHeading.Checked)
            chkABCHeader.Checked = false;
    }

    private void chkABCHeader_CheckedChanged(object sender, EventArgs e)
    {
        if (chkABCHeader.Checked)
            chkHeading.Checked = false;
    }

    private void btnTransfer_Click(object sender, EventArgs e)
    {
        WikifyToList();
    }

    private void btnFilter_Click(object sender, EventArgs e)
    {
        if (LMaker != null)
        {
            LMaker.BeginUpdate();
            RemoveDBScannerListItemsFromListMaker();
        }

        SpecialFilter.ShowDialog(this);

        if (LMaker != null)
        {
            foreach (Article article in lbArticles.Items)
                LMaker.AddWithoutUpdate(article);

            LMaker.EndUpdate();
        }

        UpdateDBScannerArticleCount();
        UpdateListMakerCount();
    }

    private void lbClear_Click(object sender, EventArgs e)
    {
        lblCount.Text = string.Empty;
        lbArticles.Items.Clear();
    }

    private void copyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Tools.Copy(lbArticles);
    }

    private void openInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if ((lbArticles.SelectedItems.Count < 10) || (MessageBox.Show("Opening " + lbArticles.SelectedItems.Count + " articles in your browser at once could cause your system to run slowly, and even stop responding.\r\nAre you sure you want to continue?", "Continue?", MessageBoxButtons.YesNo) == DialogResult.Yes))
            LoadArticlesInBrowser();
    }

    private void LoadArticlesInBrowser()
    {
        Article[] articles = new Article[lbArticles.SelectedItems.Count];
        lbArticles.SelectedItems.CopyTo(articles, 0);

        foreach (Article item in articles)
        {
            Variables.MainForm.TheSession.Site.OpenPageInBrowser(item.Name);
        }
    }

    private void removeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (LMaker != null)
        {
            LMaker.BeginUpdate();

            foreach (Article article in lbArticles.SelectedItems)
                LMaker.RemoveWithoutUpdate(article);

            LMaker.EndUpdate();
        }

        lbArticles.RemoveSelected(true);

        UpdateDBScannerArticleCount();
        UpdateListMakerCount();
    }

    private void UpdateDBScannerArticleCount()
    {
        lblCount.Text = lbArticles.Items.Count + " matches";
    }

    private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
    {
        copyToolStripMenuItem.Enabled = removeToolStripMenuItem.Enabled =
        openInBrowserToolStripMenuItem.Enabled = (lbArticles.SelectedIndex >= 0);
    }

    private static string Convert(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    private void txtTitleContains_Leave(object sender, EventArgs e)
    {
        txtTitleContains.Text = Convert(txtTitleContains.Text);
    }

    private void txtTitleNotContains_Leave(object sender, EventArgs e)
    {
        txtTitleNotContains.Text = Convert(txtTitleNotContains.Text);
    }

    private void btnStart_Click(object sender, EventArgs e)
    {
        //btnPause.Enabled = (btnStart.Text == "Start");

        if (btnStart.Text == "Start")
            StartButton();
        else
            StopButton();
    }

    private void btnPause_Click(object sender, EventArgs e)
    {
    }

    private void StopButton()
    {
        StopButton(true);
    }

    private void StopButton(bool showMatchesMessage)
    {
        timerProgessUpdate.Enabled = false;
        if (Main != null)
        {
            Main.Stop();
            Main = null;
        }

        if (showMatchesMessage)
        {
            TimeSpan endTime = new TimeSpan(DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
            endTime = endTime.Subtract(StartTime);
            MessageBox.Show(lbArticles.Items.Count + " matches in " + endTime.ToString().TrimEnd('0'));
        }
    }

    private void DatabaseScanner_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to close the Database Scanner?", "Close Scanner?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
        {
            e.Cancel = true;
        }
        else if (Main != null)
        {
            Main.Message = false;
            Main.Stop();
            Main = null;
        }
    }

    private void highestToolStripMenuItem_Click(object sender, EventArgs e)
    {
        aboveNormalToolStripMenuItem.Checked = normalToolStripMenuItem.Checked =
        belowNormalToolStripMenuItem.Checked = lowestToolStripMenuItem.Checked = false;

        Priority = ThreadPriority.Highest;
        threadPriorityButton.Text = highestToolStripMenuItem.Text;
    }

    private void aboveNormalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        highestToolStripMenuItem.Checked = normalToolStripMenuItem.Checked =
        belowNormalToolStripMenuItem.Checked = lowestToolStripMenuItem.Checked = false;

        Priority = ThreadPriority.AboveNormal;
        threadPriorityButton.Text = aboveNormalToolStripMenuItem.Text;
    }

    private void normalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        highestToolStripMenuItem.Checked = aboveNormalToolStripMenuItem.Checked =
        belowNormalToolStripMenuItem.Checked = lowestToolStripMenuItem.Checked = false;

        Priority = ThreadPriority.Normal;
        threadPriorityButton.Text = normalToolStripMenuItem.Text;
    }

    private void belowNormalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        highestToolStripMenuItem.Checked = aboveNormalToolStripMenuItem.Checked =
        normalToolStripMenuItem.Checked = lowestToolStripMenuItem.Checked = false;

        Priority = ThreadPriority.BelowNormal;
        threadPriorityButton.Text = belowNormalToolStripMenuItem.Text;
    }

    private void lowestToolStripMenuItem_Click(object sender, EventArgs e)
    {
        highestToolStripMenuItem.Checked = aboveNormalToolStripMenuItem.Checked =
        normalToolStripMenuItem.Checked = belowNormalToolStripMenuItem.Checked = false;

        Priority = ThreadPriority.Lowest;
        threadPriorityButton.Text = lowestToolStripMenuItem.Text;
    }

    private void nudLimitResults_ValueChanged(object sender, EventArgs e)
    {
        Limit = (int)nudLimitResults.Value;
    }

    #endregion

    #region properties

    private void btnReset_Click(object sender, EventArgs e)
    {
        ResetSettings();
    }

    private void ResetSettings()
    {
        //menu
        chkIgnoreRedirects.Checked = true;
        chkIgnoreComments.Checked = false;

        pageNamespaces.Reset();

        //contains
        txtArticleDoesContain.Text = string.Empty;
        txtArticleDoesNotContain.Text = string.Empty;
        chkArticleDoesContain.Checked = false;
        chkArticleDoesNotContain.Checked = false;

        chkArticleRegex.Checked = false;
        chkArticleCaseSensitive.Checked = false;
        chkMulti.Checked = false;
        chkSingle.Checked = false;

        //title
        txtTitleContains.Text = string.Empty;
        txtTitleNotContains.Text = string.Empty;
        chkTitleContains.Checked = false;
        chkTitleDoesNotContain.Checked = false;
        chkTitleRegex.Checked = false;

        //characters and links
        cmboLength.SelectedIndex = 0;
        cmboLinks.SelectedIndex = 0;
        nudLength.Value = 1000;
        nudLinks.Value = 5;

        //Restriction
        chkProtection.Checked = false;
        MoveDelete.Reset();

        //extra
        foreach (CheckBox c in flwAWB.Controls)
            c.Checked = false;

        chkSearchDates.Checked = false;

        //results
        chkHeading.Checked = false;
        nudHeadingSpace.Value = 25;
        nudLimitResults.Value = 30000;
        rdoBullet.Checked = false;
        rdoHash.Checked = true;

        FileName = string.Empty;
        txtDumpLocation.Text = string.Empty;
        txtSitename.Text = string.Empty;
        lnkBase.Text = string.Empty;
        txtGenerator.Text = string.Empty;
        txtCase.Text = string.Empty;
    }

    private void UpdateControls(bool busy)
    {
        gbText.Enabled = tabTitle.Enabled = tabConvert.Enabled = gbAWBSpecific.Enabled = tabNamespace.Enabled =
            tabRev.Enabled = gbProperties.Enabled = btnFilter.Enabled = nudLimitResults.Enabled = txtStartFrom.Enabled =
            btnReset.Enabled = btnBrowse.Enabled = !busy;

        btnStart.Text = busy ? "Stop" : "Start";
    }

    #endregion

    private void timerProgessUpdate_Tick(object sender, EventArgs e)
    {
        UpdateProgressBar();
        UpdateList();
    }

    private void UpdateList()
    {
        if (Queue.Count == 0) return;
        bool locked = false;

        if (Queue.Count > 1)
        {
            locked = true;
            lbArticles.BeginUpdate();

            if (LMaker != null)
                LMaker.BeginUpdate();
        }

        while (Queue.Count > 0)
        {
            AddArticle(Queue.Remove());
        }

        if (locked)
        {
            lbArticles.EndUpdate();

            if (LMaker != null)
                LMaker.EndUpdate();
        }

        lblCount.Text = Matches.ToString();
        lblCount.Text += " match";
        if (Matches > 1)
            lblCount.Text += "es";

        UpdateListMakerCount();
    }

    /// <summary>
    /// Updates progress bar, detailed % reading (3 dp) and ETC to indicate progress through scan
    /// </summary>
    private void UpdateProgressBar()
    {
        double matchesByLimit = ((double)Matches / Limit), completion = 0, newValue;

        if (Main != null)
            completion = Main.PercentageComplete;

        /* indicate progress based on either fraction of matches compared to user-requested match limit
         or overall fraction of file scanned, whichever is greater */
        if (matchesByLimit > completion)
            newValue = matchesByLimit * progressBar.Maximum;
        else
            newValue = completion * progressBar.Maximum;

        // show progress bar to nearest %, and detailed percentage to 3 dp
        progressBar.Value = ((int)newValue < progressBar.Maximum) ? (int)newValue : progressBar.Maximum;
        lblPercentageComplete.Text = string.Format("{0:f3}", newValue / 2) + "%"; // show percentage progress to 3 dp

        // estimate an ETC. based on elapsed time and scan progress so far
        if (completion > 0.001)
        {
            TimeSpan elapsedtime = new TimeSpan(DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute,
                                DateTime.Now.Second, DateTime.Now.Millisecond).Subtract(StartTime);

            int minutesLeft = (int)(((elapsedtime.Ticks * (1 / completion)) - elapsedtime.Ticks) / TimeSpan.TicksPerMinute);

            if (minutesLeft > 0)
                lblPercentageComplete.Text += " ETC: " + minutesLeft + " mins,";
        }
    }

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        try
        {
            if (openXMLDialog.ShowDialog() == DialogResult.OK)
            {
                var fileName = openXMLDialog.FileName;
                var extension = Path.GetExtension(fileName);

                if (extension != null && extension.ToLower() != ".xml")
                {
                    MessageBox.Show("The Database Scanner works with XML dump files. Please extract any compressed files (gz, bz2, 7z) and try again using the XML file from archive", "Wrong extension");
                    return;
                }

                if (new FileInfo(fileName).Length == 0)
                {
                    MessageBox.Show("The file you are trying to open seems to be empty. Is it still being downloaded?", "Empty File?");
                    return;
                }

                int dataFound = 0;
                using (XmlTextReader reader = new XmlTextReader(fileName))
                {
                    while (reader.Read())
                    {
                        if (reader.Name.Length == 0)
                        {
                            continue;
                        }

                        if (reader.Name == "sitename")
                        {
                            txtSitename.Text = reader.ReadString();
                            dataFound++;
                        }
                        else if (reader.Name == "base")
                        {
                            lnkBase.Text = reader.ReadString();
                            dataFound++;
                        }
                        else if (reader.Name == "generator")
                        {
                            txtGenerator.Text = reader.ReadString();
                            dataFound++;
                        }
                        else if (reader.Name == "case")
                        {
                            txtCase.Text = reader.ReadString();
                            dataFound++;
                        }

                        if (dataFound == 4)
                        {
                            break;
                        }
                        if (dataFound > 100)
                        {
                            MessageBox.Show("This doesn't look like an XML dump from MediaWiki");
                            return;
                        }
                    }
                }

                FileName = fileName;
                txtDumpLocation.Text = fileName;

                try
                {
                    if (new Uri(lnkBase.Text).Host != Variables.Host)
                    {
                        MessageBox.Show(
                            "The project of the loaded dump doesn't match the project AWB is currently setup for.\r\nIt is recommended you change the current project to that of the database dump.\r\nThis will ensure the namespaces are correct.",
                            "Project mismatch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (UriFormatException)
                {
                    MessageBox.Show(
                        "Apparent error with the host URI.\r\nChances are that probably means you're loading the wrong file type. Is it a MediaWiki XML Dump?",
                        "Invalid Document Uri");
                }
            }
        }
        catch (Exception ex) { ErrorHandler.HandleException(ex); }
    }

    private void lnkGenDump_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Tools.OpenURLInBrowser("https://www.mediawiki.org/wiki/Manual:DumpBackup.php");
    }

    private void lnkBase_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Tools.OpenURLInBrowser(lnkBase.Text);
    }

    private void lnkWikiaDumps_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Tools.OpenURLInBrowser("https://www.wikia.com/wiki/Database_download");
    }

    private void lnkWmfDumps_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Tools.OpenURLInBrowser("https://dumps.wikimedia.org/");
    }

    private void lnkWikiPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Tools.OpenENArticleInBrowser("Wikipedia:AutoWikiBrowser/Database_Scanner", false);
    }

    private void chkSearchDates_CheckedChanged(object sender, EventArgs e)
    {
        dtpFrom.Enabled = dtpTo.Enabled = chkSearchDates.Checked;
    }

    private void btnSaveArticleList_Click(object sender, EventArgs e)
    {
        lbArticles.SaveList();
    }

    private void chkProtection_CheckedChanged(object sender, EventArgs e)
    {
        MoveDelete.Enabled = chkProtection.Checked;
    }

    private void chkTitleContains_CheckedChanged(object sender, EventArgs e)
    {
        txtTitleContains.Enabled = chkTitleContains.Checked;
    }

    private void chkTitleDoesNotContain_CheckedChanged(object sender, EventArgs e)
    {
        txtTitleNotContains.Enabled = chkTitleDoesNotContain.Checked;
    }
}