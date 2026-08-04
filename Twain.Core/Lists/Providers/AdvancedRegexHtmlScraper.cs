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
using WikiFunctions.Controls;

namespace WikiFunctions.Lists.Providers;

/// <summary>
/// List provider to extract page titles from HTML based on a user-provided regex.
/// User specifies regex options and which group to take as the value of the page name.
/// All matches of regex are extracted to a list of pages.
/// </summary>
public partial class AdvancedRegexHtmlScraper : Form, IListProvider
{
    private Regex _regexToUse;

    private int _groupNumber;

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
    /// dialog is already visible, is cancelled, or no matches are found.
    /// </returns>
    public List<Article> MakeList(
        params string[] searchCriteria)
    {
        if (Visible)
        {
            return new List<Article>();
        }

        ArgumentNullException.ThrowIfNull(searchCriteria);

        List<Article> articles = new();

        if (ShowDialog() != DialogResult.OK)
        {
            return articles;
        }

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

            foreach (Match match in _regexToUse.Matches(html))
            {
                Group articleNameGroup =
                    match.Groups[_groupNumber];

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

    private void AdvancedRegexHtmlScraper_FormClosing(object sender, FormClosingEventArgs e)
    {
        RegexOptions opts = RegexOptions.Compiled;

        if (CaseSensitiveCheckBox.Checked)
            opts |= RegexOptions.IgnoreCase;

        if (SingleLineCheckBox.Checked)
            opts |= RegexOptions.Singleline;

        if (MultiLineCheckBox.Checked)
            opts |= RegexOptions.Multiline;

        if (_regexToUse == null || _regexToUse.ToString() != RegexTextBox.Text || _regexToUse.Options != opts)
        {
            try
            {
                _regexToUse = new Regex(RegexTextBox.Text, opts);
            }
            catch (ArgumentException ae)
            {
                _regexToUse = null;
                e.Cancel = true;
                MessageBox.Show(ae.Message, "Bad Regex");
            }
        }

        _groupNumber = (int)GroupNumber.Value;
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