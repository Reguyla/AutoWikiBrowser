/*
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

using System.Globalization;
using System.Windows.Forms;

namespace Twain.Core.Logging;

/// <summary>
/// Represents a log entry for an article processed by AutoWikiBrowser.
/// </summary>
/// <remarks>
/// The listener receives processing information through the AWB logging
/// interfaces and formats that information as a <see cref="ListViewItem"/>
/// suitable for display in the processed or skipped article lists.
///
/// TODO: Separate the logging data model from the WinForms
/// <see cref="ListViewItem"/> representation so that core logging does not
/// depend on UI-specific types.
/// </remarks>
[Serializable]
public class AWBLogListener : ListViewItem, IAWBTraceListener
{
    /// <summary>
    /// Default edit summary used when adding an individual log entry.
    /// </summary>
    public const string UploadingLogEntryDefaultEditSummary = "Adding log entry";

    /// <summary>
    /// Default edit summary used when uploading a log.
    /// </summary>
    public const string UploadingLogDefaultEditSummary = "Uploading log";

    /// <summary>
    /// Log message indicating that logging has been initialized.
    /// </summary>
    public const string LoggingStartButtonClicked = "Initialising log.";

    /// <summary>
    /// Identifies an action initiated by the user.
    /// </summary>
    public const string StringUser = "User";

    /// <summary>
    /// Describes an article skipped manually by the user.
    /// </summary>
    public const string StringUserSkipped = "Clicked skip";

    /// <summary>
    /// Identifies an action initiated by a plugin.
    /// </summary>
    public const string StringPlugin = "Plugin";

    /// <summary>
    /// Describes an article skipped by a plugin.
    /// </summary>
    public const string StringPluginSkipped = "Plugin sent skip event";

    /// <summary>
    /// Gets the prefix used for AWB logging edit summaries.
    /// </summary>
    public static string AWBLoggingEditSummary =>
        "(" + Variables.WPAWB + " Logging) ";

    private bool Datestamped;
    private bool HaveSkipInfo;

    /// <summary>
    /// Database revision ID created after the article was edited.
    /// </summary>
    public int NewId;

    /// <summary>
    /// Long-form URL associated with the processed article.
    /// </summary>
    public string URLLong;

    #region AWB Interface

    /// <summary>
    /// Gets a value indicating whether the article was skipped.
    /// </summary>
    public bool Skipped { get; internal set; }

    /// <summary>
    /// Initializes a new log listener for the specified article.
    /// </summary>
    /// <param name="articleTitle">
    /// The full title of the article being processed.
    /// </param>
    public AWBLogListener(string articleTitle)
    {
        Text = articleTitle;
        ArticleTitle = articleTitle;
    }

    /// <summary>
    /// Records that the current article was skipped by the user.
    /// </summary>
    public void UserSkipped()
    {
        Skip(StringUser, StringUserSkipped);
    }

    /// <summary>
    /// Records that the current article was skipped by AutoWikiBrowser.
    /// </summary>
    /// <param name="reason">The reason the article was skipped.</param>
    public void AWBSkipped(string reason)
    {
        Skip("AWB", reason);
    }

    /// <summary>
    /// Records that the current article was skipped by a plugin.
    /// </summary>
    public void PluginSkipped()
    {
        Skip(StringPlugin, StringPluginSkipped);
    }

    /// <summary>
    /// Opens the current article in the user's web browser.
    /// </summary>
    public void OpenInBrowser()
    {
        Tools.OpenArticleInBrowser(ArticleTitle);
    }

    /// <summary>
    /// Opens the revision history of the current article in the user's
    /// web browser.
    /// </summary>
    public void OpenHistoryInBrowser()
    {
        Tools.OpenArticleHistoryInBrowser(ArticleTitle);
    }

    /// <summary>
    /// Opens the edited revision diff in the user's web browser.
    /// </summary>
    public void OpenDiffInBrowser()
    {
        Tools.OpenDiffInBrowser(URLLong, NewId);
    }

    /// <summary>
    /// Adds a timestamp to this entry and inserts it at the beginning of the
    /// specified list view.
    /// </summary>
    /// <param name="listView">
    /// The list view to which the log entry should be added.
    /// </param>
    public void AddAndDateStamp(ListView listView)
    {
        var dateStamp = new ListViewSubItem
        {
            Text = DateTime.Now.ToString(CultureInfo.InvariantCulture)
        };

        base.SubItems.Insert(1, dateStamp);

        // Historical issue:
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#ArgumentException_in_AWBLogListener.AddAndDateStamp
        //
        // TODO: Determine and prevent the underlying ListView insertion
        // failure rather than suppressing the exception.
        listView.BeginUpdate();

        try
        {
            listView.Items.Insert(0, this);
        }
        catch (ArgumentException)
        {
            // Preserve legacy behavior until the underlying insertion
            // condition can be identified and prevented.
        }
        finally
        {
            listView.EndUpdate();
        }

        Datestamped = true;
    }

    /// <summary>
    /// Formats this log entry for the specified output format.
    /// </summary>
    /// <param name="logFileType">
    /// The format in which the log entry should be returned.
    /// </param>
    /// <returns>The formatted log entry.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="logFileType"/> is not a recognized log file type.
    /// </exception>
    public string Output(LogFileType logFileType)
    {
        switch (logFileType)
        {
            case LogFileType.AnnotatedWikiText:
                string output =
                    "*" + TimeStamp + ": [[" + ArticleTitle + "]]\r\n";

                if (Skipped)
                {
                    output +=
                        "'''Skipped''' by: " +
                        SkippedBy +
                        "\r\n" +
                        "Skip reason: " +
                        SkipReason +
                        "\r\n";
                }

                return output + ToolTipText + "\r\n";

            case LogFileType.PlainText:
                return ArticleTitle;

            case LogFileType.WikiText:
                return "#[[:" + ArticleTitle + "]]";

            default:
                throw new ArgumentOutOfRangeException(nameof(logFileType));
        }
    }

    /// <summary>
    /// Gets the title of the article represented by this log entry.
    /// </summary>
    public string ArticleTitle { get; private set; }

    /// <summary>
    /// Gets or sets the reason the article was skipped.
    /// </summary>
    public string SkipReason
    {
        get => GetSubItemText(SubItem.SkippedReason);
        protected set => SetSubItemText(SubItem.SkippedReason, value);
    }

    /// <summary>
    /// Gets the timestamp associated with this log entry.
    /// </summary>
    public string TimeStamp =>
        GetSubItemText(SubItem.TimeStamp);

    /// <summary>
    /// Gets or sets the component or user that caused the article to be
    /// skipped.
    /// </summary>
    public string SkippedBy
    {
        get => GetSubItemText(SubItem.SkippedBy);
        protected set => SetSubItemText(SubItem.SkippedBy, value);
    }

    #endregion

    #region IMyTraceListener Members

    void IMyTraceListener.Close()
    {
    }

    void IMyTraceListener.Flush()
    {
    }

    void IMyTraceListener.ProcessingArticle(string fullArticleTitle, int ns)
    {
    }

    void IMyTraceListener.WriteComment(string line)
    {
    }

    void IMyTraceListener.WriteCommentAndNewLine(string line)
    {
    }

    void IMyTraceListener.SkippedArticle(string skippedBy, string reason)
    {
        Skip(skippedBy, reason);
    }

    void IMyTraceListener.SkippedArticleBadTag(
        string skippedBy,
        string fullArticleTitle,
        int ns)
    {
        Skip(skippedBy, "Bad tag");
    }

    void IMyTraceListener.SkippedArticleRedlink(
        string skippedBy,
        string fullArticleTitle,
        int ns)
    {
        Skip(skippedBy, "Red link (article deleted)");
    }

    void IMyTraceListener.WriteArticleActionLine(
        string line,
        string pluginName,
        bool verboseOnly)
    {
        if (!verboseOnly)
        {
            WriteLine(line, pluginName);
        }
    }

    void IMyTraceListener.WriteArticleActionLine(
        string line,
        string pluginName)
    {
        WriteLine(line, pluginName);
    }

    void IMyTraceListener.WriteBulletedLine(
        string line,
        bool bold,
        bool verboseOnly)
    {
        if (!verboseOnly)
        {
            Write(line);
        }
    }

    void IMyTraceListener.WriteBulletedLine(
        string line,
        bool bold,
        bool verboseOnly,
        bool dateStamp)
    {
        if (!verboseOnly)
        {
            Write(line);
        }
    }

    void IMyTraceListener.WriteLine(string line)
    {
        Write(line);
    }

    void IMyTraceListener.WriteTemplateAdded(
        string template,
        string pluginName)
    {
        WriteLine("{{" + template + "}} added", pluginName);
    }

    /// <summary>
    /// Adds text to the log entry's tooltip.
    /// </summary>
    /// <param name="text">The text to add.</param>
    /// <remarks>
    /// New log messages are prepended so that the most recent message appears
    /// first.
    /// </remarks>
    public void Write(string text)
    {
        if (string.IsNullOrWhiteSpace(ToolTipText))
        {
            ToolTipText = text;
        }
        else
        {
            ToolTipText =
                text +
                Environment.NewLine +
                ToolTipText;
        }
    }

    /// <summary>
    /// Adds a sender-qualified message to the log entry.
    /// </summary>
    /// <param name="text">The message to add.</param>
    /// <param name="sender">The component that generated the message.</param>
    public void WriteLine(string text, string sender)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            Write(sender + ": " + text);
        }
    }

    #endregion

    private enum SubItem
    {
        SkippedBy,
        SkippedReason,
        TimeStamp
    }

    /// <summary>
    /// Returns the <see cref="ListViewItem.SubItems"/> index for the specified
    /// piece of log information.
    /// </summary>
    /// <param name="subItem">
    /// The logical subitem whose index should be returned.
    /// </param>
    /// <returns>
    /// The subitem index, or <c>-1</c> when the requested subitem does not
    /// exist.
    /// </returns>
    private int GetSubItemNumber(SubItem subItem)
    {
        switch (subItem)
        {
            case SubItem.SkippedBy:
                return Datestamped ? 2 : 1;

            case SubItem.SkippedReason:
                return Datestamped ? 3 : 2;

            case SubItem.TimeStamp:
                return Datestamped ? 1 : -1;

            default:
                throw new ArgumentOutOfRangeException(nameof(subItem));
        }
    }

    /// <summary>
    /// Returns the text for the specified subitem.
    /// </summary>
    /// <param name="subItem">
    /// The subitem whose text should be returned.
    /// </param>
    /// <returns>
    /// The subitem text, or <see cref="string.Empty"/> when the requested
    /// information is not available.
    /// </returns>
    private string GetSubItemText(SubItem subItem)
    {
        switch (subItem)
        {
            case SubItem.SkippedBy:
            case SubItem.SkippedReason:
                return HaveSkipInfo
                    ? base.SubItems[GetSubItemNumber(subItem)].Text
                    : string.Empty;

            case SubItem.TimeStamp:
                return Datestamped
                    ? base.SubItems[1].Text
                    : string.Empty;

            default:
                return base.SubItems[GetSubItemNumber(subItem)].Text;
        }
    }

    /// <summary>
    /// Stores text for the specified logical log subitem.
    /// </summary>
    /// <param name="subItem">The subitem to update.</param>
    /// <param name="value">The value to store.</param>
    private void SetSubItemText(SubItem subItem, string value)
    {
        if ((subItem == SubItem.SkippedBy ||
             subItem == SubItem.SkippedReason) &&
            !HaveSkipInfo)
        {
            base.SubItems.Add("SkippedBy");
            base.SubItems.Add("SkipReason");
            HaveSkipInfo = true;
        }

        base.SubItems[GetSubItemNumber(subItem)].Text = value;
    }

    /// <summary>
    /// Marks the article as skipped and records the source and reason.
    /// </summary>
    /// <param name="mSkippedBy">
    /// The user or component responsible for skipping the article.
    /// </param>
    /// <param name="mSkipReason">
    /// The reason the article was skipped.
    /// </param>
    protected void Skip(string mSkippedBy, string mSkipReason)
    {
        SetSubItemText(SubItem.SkippedBy, mSkippedBy);
        SetSubItemText(SubItem.SkippedReason, mSkipReason);

        WriteLine(SkipReason, SkippedBy);

        Skipped = true;
    }

    /// <summary>
    /// Prevents consumers of <see cref="AWBLogListener"/> from directly
    /// accessing the underlying subitem collection.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// Always thrown because callers should use the strongly named log
    /// properties instead.
    /// </exception>
    public static new ListViewSubItemCollection SubItems =>
        throw new NotImplementedException(
            "The SubItems property should not be accessed directly");
}