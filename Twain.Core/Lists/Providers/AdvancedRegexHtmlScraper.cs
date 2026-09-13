/*

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

using System.Windows.Forms;
using Twain.Core.Controls;

namespace Twain.Core.Lists.Providers;

/// <summary>
/// List provider to extract page titles from HTML based on a user-provided regex.
/// User specifies regex options and which group to take as the value of the page name.
/// All matches of regex are extracted to a list of pages.
/// </summary>
public partial class AdvancedRegexHtmlScraper : Form, IListProvider
{
    private ScraperSettings? _settings;

    /// <summary>
    /// Describes the regex configuration used by the advanced HTML scraper.
    /// </summary>
    public sealed record ScraperSettings(
        string Pattern,
        int GroupNumber,
        bool CaseSensitive,
        bool SingleLine,
        bool MultiLine);

    /// <summary>
    /// Gets or sets the asynchronous presenter used to configure the advanced
    /// regex HTML scraper.
    /// </summary>
    public static Func<
        ScraperSettings?,
        Task<ScraperSettings?>>?
        ShowDialogAsync
    { get; set; }

    /// <summary>
    /// Creates the compiled regular expression used by the HTML scraper.
    /// </summary>
    /// <param name="settings">
    /// The regex pattern and options selected by the user.
    /// </param>
    /// <returns>
    /// The compiled regular expression.
    /// </returns>
    private static Regex CreateRegex(
        ScraperSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        RegexOptions options =
            RegexOptions.Compiled;

        if (!settings.CaseSensitive)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (settings.SingleLine)
        {
            options |= RegexOptions.Singleline;
        }

        if (settings.MultiLine)
        {
            options |= RegexOptions.Multiline;
        }

        return new Regex(
            settings.Pattern,
            options);
    }

    /// <summary>
    /// Validates the supplied scraper settings.
    /// </summary>
    /// <param name="settings">
    /// The scraper settings to validate.
    /// </param>
    /// <param name="errorMessage">
    /// Receives the regex validation error when validation fails; otherwise,
    /// an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the settings contain a valid regular
    /// expression; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryValidateSettings(
        ScraperSettings settings,
        out string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            _ = CreateRegex(
                settings);

            errorMessage =
                string.Empty;

            return true;
        }
        catch (ArgumentException exception)
        {
            errorMessage =
                exception.Message;

            return false;
        }
    }

    /// <summary>
    /// Reads the current scraper settings from the dialog controls.
    /// </summary>
    /// <returns>
    /// The scraper configuration selected by the user.
    /// </returns>
    private ScraperSettings GetScraperSettings()
    {
        return new ScraperSettings(
            RegexTextBox.Text,
            (int)GroupNumber.Value,
            CaseSensitiveCheckBox.Checked,
            SingleLineCheckBox.Checked,
            MultiLineCheckBox.Checked);
    }

    public AdvancedRegexHtmlScraper()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Displays the list-building dialog and creates articles from matches found
    /// in the supplied web locations.
    /// </summary>
    /// <param name="searchCriteria">
    /// The URLs or host names whose HTML content should be searched.
    /// </param>
    /// <returns>
    /// A list of articles created from matching values, or an empty list when the
    /// dialog is cancelled, no presenter is available, or no matches are found.
    /// </returns>
    public List<Article> MakeList(
        params string[] searchCriteria)
    {
        ArgumentNullException.ThrowIfNull(searchCriteria);

        if (ShowDialogAsync is null)
        {
            return new List<Article>();
        }

        ScraperSettings? settings =
            ShowDialogAsync(
                    _settings)
                .GetAwaiter()
                .GetResult();

        if (settings is null)
        {
            return new List<Article>();
        }

        if (!TryValidateSettings(
                settings,
                out _))
        {
            return new List<Article>();
        }

        Regex regex =
            CreateRegex(
                settings);

        _settings =
            settings;

        return ScrapeArticles(
            searchCriteria,
            regex,
            settings.GroupNumber);
    }

    /// <summary>
    /// Downloads HTML from the supplied locations and creates articles from
    /// values matched by the configured regular expression.
    /// </summary>
    /// <param name="searchCriteria">
    /// The URLs or host names whose HTML content should be searched.
    /// </param>
    /// <param name="regex">
    /// The regular expression used to locate article names.
    /// </param>
    /// <param name="groupNumber">
    /// The capture group containing the article name.
    /// </param>
    /// <returns>
    /// The articles created from matching values.
    /// </returns>
    private static List<Article> ScrapeArticles(
        IEnumerable<string> searchCriteria,
        Regex regex,
        int groupNumber)
    {
        ArgumentNullException.ThrowIfNull(searchCriteria);
        ArgumentNullException.ThrowIfNull(regex);

        List<Article> articles =
            new();

        foreach (string searchLocation in searchCriteria)
        {
            if (string.IsNullOrWhiteSpace(searchLocation))
            {
                continue;
            }

            string url =
                searchLocation.StartsWith(
                    "http://",
                    StringComparison.OrdinalIgnoreCase) ||
                searchLocation.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase)
                    ? searchLocation
                    : $"http://{searchLocation}";

            string html =
                Tools.GetHTML(url);

            foreach (Match match in regex.Matches(html))
            {
                Group articleNameGroup =
                    match.Groups[groupNumber];

                if (!articleNameGroup.Success ||
                    articleNameGroup.Length == 0)
                {
                    continue;
                }

                articles.Add(
                    new Article(
                        ModifyArticleName(
                            articleNameGroup.Value)));
            }
        }

        return articles;
    }

    private static string ModifyArticleName(string title)
    {
        title = Regex.Replace(title, @"&#0?39;|&#146;|&amp;#0?39;|&amp;#146;|[`’]", "'");

        title = title.Replace(@"&amp;", "&");
        title = title.Replace(@"&quot;", @"""");
        return title.Replace("<br />", "");
    }

    public string DisplayText
    {
        get { return "HTML Scraper (advanced regex)"; }
    }

    public string UserInputTextBoxText
    {
        get { return "URL:"; }
    }

    public bool UserInputTextBoxEnabled
    {
        get { return true; }
    }

    public void Selected()
    {
    }

    public bool RunOnSeparateThread
    {
        get { return true; }
    }

    public virtual bool StripUrl
    {
        get { return false; }
    }

    private void cutToolStripMenuItem_Click(object sender, EventArgs e)
    {
        RegexTextBox.Cut();
    }

    private void copyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        RegexTextBox.Copy();
    }

    private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        RegexTextBox.Paste();
    }

    private void copyToRegexTesterToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using (RegexTester t = new RegexTester(true))
        {
            t.Find = RegexTextBox.Text;
            t.IgnoreCase = !CaseSensitiveCheckBox.Checked;
            t.Multiline = MultiLineCheckBox.Checked;
            t.Singleline = SingleLineCheckBox.Checked;

            if (t.ShowDialog(this) != DialogResult.OK) return;

            RegexTextBox.Text = t.Find;
            CaseSensitiveCheckBox.Checked = t.IgnoreCase;
            MultiLineCheckBox.Checked = t.Multiline;
            SingleLineCheckBox.Checked = t.Singleline;
        }
    }

    private void AdvancedRegexHtmlScraper_FormClosing(
        object sender,
        FormClosingEventArgs e)
    {
        ScraperSettings settings =
            GetScraperSettings();

        try
        {
            _ = CreateRegex(
                settings);

            _settings =
                settings;
        }
        catch (ArgumentException exception)
        {
            _settings = null;
            e.Cancel = true;

            MessageBox.Show(
                exception.Message,
                "Bad Regex");
        }
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        Close();
    }
}