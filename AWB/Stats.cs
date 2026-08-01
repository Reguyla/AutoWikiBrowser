/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

#undef INSTASTATS // turn on here and in Main.cs to make AWB log (empty) stats at startup (The scope of a symbol created by using #define is the file in which it was defined)

using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Xml;
using WikiFunctions;
using WikiFunctions.Plugin;

namespace AutoWikiBrowser;

partial class MainForm
{
    // Unfortunately, NotifyIcon is sealed, otherwise I would inherit from that and do tooltiptext/stats management there
    // Even more unfortunately, it seems it's tooltip is limited to 64 chars. Stinking great, Microsoft!
    // T-O-D-O: Maybe an alternative approach using mouse events? - doesn't seem to be a reliable way of doing even that! see e.g. http://64.233.183.104/search?q=cache:34QVls9xRoUJ:www.experts-exchange.com/Programming/Languages/.NET/Visual_Basic.NET/Q_21161863.html+notifyicon+mouseover&hl=en&ct=clnk&cd=1&gl=uk&lr=lang_en
    private int NoEdits;
    public int NumberOfEdits
    {
        get { return NoEdits; }
        private set
        {
            NoEdits = value;
            lblEditCount.Text = "Edits: " + value;
            // UpdateNotifyIconTooltip();
            if (value == 100 || (value > 0 && value % 1000 == 0)) // we'll first report to remote db when we have 100 saves or app is exiting, whichever comes first; we'll also update db at 1000 and each 1000 thereafter
                UsageStats.Do(false);
        }
    }

    private int NoNewPages;
    public int NumberOfNewPages
    {
        get { return NoNewPages; }
        private set
        {
            NoNewPages = value;
            lblNewArticles.Text = "New: " + value;
        }
    }

    private int NoIgnoredEdits;
    public int NumberOfIgnoredEdits
    {
        get { return NoIgnoredEdits; }
        private set
        {
            NoIgnoredEdits = value;
            lblIgnoredArticles.Text = "Skipped: " + value;
        }
    }

    private int NoEditsPerMin;
    public int NumberOfEditsPerMinute
    {
        get { return NoEditsPerMin; }
        private set
        {
            NoEditsPerMin = value;
            lblEditsPerMin.Text = "Edits/min: " + value;
        }
    }

    private int NoPagesPerMin;
    public int NumberOfPagesPerMinute
    {
        get { return NoPagesPerMin; }
        private set
        {
            NoPagesPerMin = value;
            lblPagesPerMin.Text = "Pages/min: " + value;
        }
    }

    /// <summary>
    /// Holds the number of pages parsed when AWB is in pre-parse mode
    /// </summary>
    public int NumberOfPagesParsed;
}

/// <summary>
/// A class to collect and submit some non-invasive usage stats, to help AWB developers track usage and plan development
/// </summary>
/// <remarks>
/// Stats can be viewed at https://awb.toolforge.org/stats/
/// Tool Labs access is needed to access files/database
/// </remarks>
internal static class UsageStats
{
    // TODO: Add other stuff we'd like to track

    private const string StatsURL = "https://awb.toolforge.org/stats/";

    private static int RecordId,
        SecretNumber,
        LastEditCount;

    private static bool SentUserName;

    private static readonly List<IAWBPlugin> NewAWBPlugins = new List<IAWBPlugin>();
    private static readonly List<IAWBBasePlugin> NewAWBBasePlugins = new List<IAWBBasePlugin>();
    private static readonly List<IListMakerPlugin> NewListMakerPlugins = new List<IListMakerPlugin>();

    private static string UserName
    {
        get { return Variables.MainForm.TheSession.User.Name; }
    }

    #region Public

    /// <summary>
    /// Call this when it's time to consider submitting some data
    /// Don't try to send stats if no edits/new pages
    /// </summary>
    internal static void Do(bool appexit)
    {
        // no stats to send if no edits
        if (Program.AWB.NumberOfEdits == 0 && Program.AWB.NumberOfNewPages == 0)
            return;

        try
        {
            bool statsSent;

            if (EstablishedContact)
            {
                if (Program.AWB.NumberOfEdits > LastEditCount ||
                    NewPluginsAdded ||
                    HaveUserNameToSend)
                {
                    statsSent = SubsequentContact();
                }
                else
                {
                    statsSent = true;
                }
            }
            else
            {
                statsSent = FirstContact();
            }

            if (statsSent)
            {
                LastEditCount = Program.AWB.NumberOfEdits;
            }
        }
        catch (Exception ex)
        {
            if (appexit) ErrorHandler.HandleException(ex); // else try again later
        }
    }

    static bool NewPluginsAdded
    {
        get
        {
            return NewAWBPlugins.Count > 0 || NewAWBBasePlugins.Count > 0
                   || NewListMakerPlugins.Count > 0;
        }
    }

    /// <summary>
    /// Call when a plugin was added *after* application startup
    /// </summary>
    internal static void AddedPlugin(IAWBPlugin plugin)
    {
        // if we've already written to the remote database, we'll need to add details of this plugin when we next contact it, otherwise do nothing
        if (EstablishedContact) NewAWBPlugins.Add(plugin);
    }

    /// <summary>
    /// Call when a plugin was added *after* application startup
    /// </summary>
    internal static void AddedPlugin(IAWBBasePlugin plugin)
    {
        if (EstablishedContact) NewAWBBasePlugins.Add(plugin);
    }

    /// <summary>
    /// Call when a plugin was added *after* application startup
    /// </summary>
    internal static void AddedPlugin(IListMakerPlugin plugin)
    {
        if (EstablishedContact) NewListMakerPlugins.Add(plugin);
    }
    #endregion

    #region Server Contact
    /// <summary>
    /// Send usage stats to server
    /// </summary>
    private static bool FirstContact()
    {
#if !DEBUG && !INSTASTATS
        if (Program.AWB.NumberOfEdits == 0) return false;
#endif
        NameValueCollection postvars = new NameValueCollection
                                           {
                                               {"Action", "Hello"},
                                               {"Version", Program.VersionString}
                                           };

        // Greetings and AWB version:

        // Site/project name:
        // TODO: Here or in PHP: tl.wikipedia.org      CUS: Translate to site name/lang code any Wikimedia site set up as custom
        if (Variables.IsCustomProject || Variables.IsWikia)
            postvars.Add("Wiki", Variables.Host);
        else
            postvars.Add("Wiki", Variables.Project.ToString());
        // This returns a short string such as "Wikipedia"; may want to convert to int and then to string so we store less in the db

        // Language code:
        if (Variables.IsWikia)
        {
            postvars.Add("Language", "WIK");
        }
        else if (Variables.IsCustomProject || Variables.IsWikimediaMonolingualProject)
        {
            postvars.Add("Language", "CUS");
        }
        else
        {
            postvars.Add("Language", Variables.LangCode);
        }

        // UI culture:
        postvars.Add("Culture", System.Threading.Thread.CurrentThread.CurrentCulture.ToString());

        // Username:
        bool userFieldIncluded = ProcessUsername(postvars);

        // Other details:
        postvars.Add("Saves", Program.AWB.NumberOfEdits.ToString());
        postvars.Add("OS", Environment.OSVersion.VersionString);
#if DEBUG
        postvars.Add("Debug", "Y");
#else
        postvars.Add("Debug", "N");
#endif
        EnumeratePlugins(postvars,
                         Plugins.Plugin.AWBPlugins.Values,
                         Plugins.Plugin.AWBBasePlugins.Values,
                         Plugins.Plugin.ListMakerPlugins.Values);

        string response;

        if (!TryPostData(postvars, out response))
        {
            return false;
        }

        ReadXml(response);

        if (userFieldIncluded)
        {
            SentUserName = true;
        }

        return true;
    }

    /// <summary>
    /// Send updated usage stats to server
    /// </summary>
    private static bool SubsequentContact()
    {
        NameValueCollection postvars = new NameValueCollection
                                           {
                                               {"Action", "Update"},
                                               {"RecordID", RecordId.ToString()},
                                               {"Verify", SecretNumber.ToString()}
                                           };

        EnumeratePlugins(postvars, NewAWBPlugins, NewAWBBasePlugins, NewListMakerPlugins);

        bool userFieldIncluded = ProcessUsername(postvars);

        if (Program.AWB.NumberOfEdits > LastEditCount)
            postvars.Add("Saves", Program.AWB.NumberOfEdits.ToString());

        string response;

        if (!TryPostData(postvars, out response))
        {
            return false;
        }

        if (userFieldIncluded)
        {
            SentUserName = true;
        }

        // Clear lists only after the update was successfully sent.
        NewAWBPlugins.Clear();
        NewAWBBasePlugins.Clear();
        NewListMakerPlugins.Clear();

        return true;
    }

    /// <summary>
    /// Returns true if we've sent initial stats to server
    /// </summary>
    private static bool EstablishedContact
    { get { return (RecordId > 0); } }

    /// <summary>
    /// Attempts to post usage statistics to the server.
    /// </summary>
    /// <param name="postvars">The values to send.</param>
    /// <param name="response">
    /// The server response when the request succeeds; otherwise,
    /// <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the request completes successfully; otherwise,
    /// <see langword="false"/> when a recognized network or I/O failure occurs.
    /// </returns>
    private static bool TryPostData(
        NameValueCollection postvars,
        out string? response)
    {
        response = null;

        try
        {
            Program.AWB.StartProgressBar();
            StatusLabelText = "Contacting stats server...";
            Program.AWB.Form.Cursor =
                System.Windows.Forms.Cursors.WaitCursor;

            response = Tools.PostData(postvars, StatsURL);
            return true;
        }
        catch (Exception ex)
            when (ex is WebException
                or HttpRequestException
                or IOException)
        {
            Tools.WriteDebug("UsageStats", ex.Message);
            return false;
        }
        finally
        {
            Program.AWB.StopProgressBar();
            StatusLabelText = "";
            Program.AWB.Form.Cursor =
                System.Windows.Forms.Cursors.Default;
        }
    }
    #endregion

    #region Helper routines
    private static string StatusLabelText { set { Program.AWB.StatusLabelText = value; } }

    private static void EnumeratePlugins(NameValueCollection postvars, ICollection<IAWBPlugin> awbPlugins, ICollection<IAWBBasePlugin> awbBasePlugins, ICollection<IListMakerPlugin> listMakerPlugins)
    {
        int i = 0;

        postvars.Add("PluginCount", (awbPlugins.Count + awbBasePlugins.Count + listMakerPlugins.Count).ToString());

        foreach (IAWBPlugin plugin in awbPlugins)
        {
            i++;
            string p = "P" + i;
            postvars.Add(p + "N", plugin.Name);
            postvars.Add(p + "V", Plugins.Plugin.GetPluginVersionString(plugin));
            postvars.Add(p + "T", "0");
        }

        foreach (IListMakerPlugin plugin in listMakerPlugins)
        {
            i++;
            string p = "P" + i;
            postvars.Add(p + "N", plugin.Name);
            postvars.Add(p + "V", Plugins.Plugin.GetPluginVersionString(plugin));
            postvars.Add(p + "T", "1");
        }

        foreach (IAWBBasePlugin plugin in awbBasePlugins)
        {
            i++;
            string p = "P" + i;
            postvars.Add(p + "N", plugin.Name);
            postvars.Add(p + "V", Plugins.Plugin.GetPluginVersionString(plugin));
            postvars.Add(p + "T", "2");
        }
    }

    /// <summary>
    /// Reads the record identifier and verification number returned by the
    /// usage statistics server.
    /// </summary>
    /// <param name="xml">
    /// The XML response returned by the usage statistics server.
    /// </param>
    /// <exception cref="XmlException">
    /// Thrown when the response does not contain a single valid
    /// <c>DB</c> element with numeric <c>Record</c> and <c>Verify</c> attributes.
    /// </exception>
    private static void ReadXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return;
        }

        try
        {
            XmlDocument document = new()
            {
                XmlResolver = null
            };

            document.LoadXml(xml);

            XmlNodeList nodes = document.GetElementsByTagName("DB");

            if (nodes.Count != 1 || nodes[0] is not XmlElement dbElement)
            {
                throw CreateUsageStatsXmlException();
            }

            string? recordValue = dbElement.GetAttribute("Record");
            string? verifyValue = dbElement.GetAttribute("Verify");

            if (!int.TryParse(recordValue, out int recordId) ||
                !int.TryParse(verifyValue, out int secretNumber))
            {
                throw CreateUsageStatsXmlException();
            }

            RecordId = recordId;
            SecretNumber = secretNumber;
        }
        catch (XmlException)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is InvalidOperationException ||
            ex is ArgumentException)
        {
            throw CreateUsageStatsXmlException(ex);
        }
    }

    /// <summary>
    /// Creates a standardized exception for an invalid usage statistics response.
    /// </summary>
    /// <param name="innerException">
    /// The exception that caused parsing to fail, if available.
    /// </param>
    /// <returns>
    /// An exception describing the invalid server response.
    /// </returns>
    private static XmlException CreateUsageStatsXmlException(
        Exception? innerException = null)
    {
        const string message =
            "Error parsing XML returned from the usage statistics server.";

        return innerException is null
            ? new XmlException(message)
            : new XmlException(message, innerException);
    }

    /// <summary>
    /// Adds the username or privacy marker to a pending statistics request when
    /// it has not yet been successfully sent.
    /// </summary>
    /// <param name="postvars">The request values being prepared.</param>
    /// <returns>
    /// <c>true</c> when this request includes a User field that should be marked
    /// as sent only after the request succeeds; otherwise, <c>false</c>.
    /// </returns>
    private static bool ProcessUsername(NameValueCollection postvars)
    {
        if (SentUserName)
            return false;

        if (Properties.Settings.Default.Privacy)
        {
            postvars.Add("User", "<Withheld>");
            return true;
        }

        if (!string.IsNullOrEmpty(UserName))
        {
            postvars.Add("User", UserName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether the user's name should be included in the
    /// next usage statistics submission.
    /// </summary>
    private static bool HaveUserNameToSend =>
        !SentUserName &&
        (Properties.Settings.Default.Privacy ||
         !string.IsNullOrWhiteSpace(UserName));

    #endregion

    /// <summary>
    /// Opens the usage statistics information page in the user's default web browser.
    /// </summary>
    internal static void OpenUsageStatsURL()
    {
        Tools.OpenURLInBrowser(StatsURL);
    }
}