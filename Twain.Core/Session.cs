/*
Copyright (C) 2009

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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Windows.Forms;
using Twain.Core.API;

namespace Twain.Core;

/// <summary>
/// This class controls editing process in one wiki
/// </summary>
public class Session
{
    #region Properties
    public AsyncApiEdit Editor { get; private set; }

    public UserInfo User => Editor.User;

    public PageInfo Page => Editor.Page;

    public SiteInfo Site { get; private set; }

    public bool IsBusy => Editor.IsActive;

    public bool IsBot { get; private set; }

    public bool IsSysop => Editor.User.IsSysop;

    /// <summary>
    /// Gets the check page JSON Text.
    /// </summary>
    /// <value>The check page JSON Text.</value>
    public string CheckPageJSONText { get; private set; }

    /// <summary>
    /// Config Page JSON text
    /// </summary>
    public string ConfigJSONText { get; set; }

    /// <summary>
    /// Gets the JSON of version check page.
    /// </summary>
    /// <value>The JSON of version check page.</value>
    public string VersionCheckPage { get; private set; }

    #endregion

    private readonly Control _parentControl;

    public Session(Control parent)
    {
        _parentControl = parent;
        UpdateProject(true);
    }

    private AsyncApiEdit CreateEditor(string url)
    {
        AsyncApiEdit edit = new(url, _parentControl)
        {
            NewMessageThrows = false
        };

        edit.OpenComplete += OnOpenComplete;
        edit.SaveComplete += OnSaveComplete;
        edit.PreviewComplete += OnPreviewComplete;
        edit.ExceptionCaught += OnExceptionCaught;
        edit.MaxlagExceeded += OnMaxlagExceeded;
        edit.LoggedOff += OnLoggedOff;
        edit.StateChanged += OnStateChanged;
        edit.Aborted += OnAborted;

        return edit;
    }

    #region Events

    public event AsyncOpenEditHandler OpenComplete;
    public event AsyncSaveEventHandler SaveComplete;
    public event AsyncStringEventHandler PreviewComplete;

    public event AsyncExceptionEventHandler ExceptionCaught;
    public event AsyncMaxlagEventHandler MaxlagExceeded;
    public event AsyncEventHandler LoggedOff;

    public event AsyncEventHandler StateChanged;

    public event AsyncEventHandler Aborted;

    private void OnOpenComplete(
        AsyncApiEdit sender,
        PageInfo pageInfo) =>
        OpenComplete?.Invoke(sender, pageInfo);

    private void OnSaveComplete(
        AsyncApiEdit sender,
        SaveInfo saveInfo) =>
        SaveComplete?.Invoke(sender, saveInfo);

    private void OnPreviewComplete(
        AsyncApiEdit sender,
        string result) =>
        PreviewComplete?.Invoke(sender, result);

    private void OnExceptionCaught(
        AsyncApiEdit sender,
        Exception exception) =>
        ExceptionCaught?.Invoke(sender, exception);

    private void OnMaxlagExceeded(
        AsyncApiEdit sender,
        double maxlag,
        int retryAfter) =>
        MaxlagExceeded?.Invoke(sender, maxlag, retryAfter);

    private void OnLoggedOff(AsyncApiEdit sender) =>
        LoggedOff?.Invoke(sender);

    private void OnStateChanged(AsyncApiEdit sender) =>
        StateChanged?.Invoke(sender);

    private void OnAborted(AsyncApiEdit sender) =>
        Aborted?.Invoke(sender);

    #endregion

    /// <summary>
    /// Default template of what would exist at Project:AutoWikiBrowser/Config, to be used in case of it not existing
    /// </summary>
    private const string DefaultWikiConfig = "{ 'typolink': '', 'allusersenabled': true, 'allusersenabledusermode': true, 'messages': [], 'underscoretitles': [], 'nogenfixes': [], 'noregextypofix': [] }";

    private WikiStatusResult _status;

    public WikiStatusResult Status
    {
        get
        {
            if (_status == WikiStatusResult.PendingUpdate)
                Update();

            return _status;
        }
        private set => _status = value;
    }

    // Allows fork/rebrand development builds to continue through the normal
    // CheckPageJSON, config, login, and registration checks even when the
    // public AWB version page marks this AWB version as disabled.
    //
    // This bypass is intentionally narrow: it only bypasses the public AWB
    // version gate. It does not bypass login, CheckPageJSON, local config,
    // bad-name, registration, or bot/user-mode checks.
    private const bool AllowForkReleaseVersionGateBypass = true;

    public bool UpdateProject(bool delayLoading)
    {
        // recreate only if project changed, to prevent losing login information
        if (Editor == null || Editor.URL != Variables.URLLong)
        {
            Editor = CreateEditor(Variables.URLLong);
        }

        if (delayLoading)
        {
            return true;
        }
        try
        {
            LoadProjectOptions();
            RequireUpdate();
            return true;
        }
        catch (ReadApiDeniedException)
        {
            throw;
        }
        catch (UriChangedException ex)
        {
            // TODO:
            // If the server redirects to a different URI scheme (for example,
            // HTTP -> HTTPS), offer to update the configured wiki URL,
            // recreate the editor using the redirected URI, and retry
            // project initialization before reporting a failure.
            MessageBox.Show(
                _parentControl,
                ex.Message,
                ex.Header,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
        catch (HttpRequestException ex)
        {
            if (ShouldRethrowProjectLoadHttpException(ex))
                throw;

            return false;
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(UpdateProject),
                ex.ToString());

            return false;
        }
    }

    /// <summary>
    /// Determines whether a project-loading HTTP failure should be propagated to
    /// the caller instead of being treated as a recoverable load failure.
    /// </summary>
    /// <param name="exception">The HTTP request exception to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when the response status is HTTP 401 Unauthorized;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Unauthorized responses are propagated so the main form can request
    /// authentication and retry project loading.
    /// </remarks>
    private static bool ShouldRethrowProjectLoadHttpException(
        HttpRequestException exception)
    {
        return exception.StatusCode == HttpStatusCode.Unauthorized;
    }

    /// <summary>
    ///
    /// </summary>
    public void RequireUpdate()
    {
        _status = WikiStatusResult.PendingUpdate;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public WikiStatusResult Update()
    {
        Status = UpdateWikiStatus();
        return Status;
    }

    /// <summary>
    ///
    /// </summary>
    public static string AWBVersion
    { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }

    public static string ConfigUrl
    {
        get { return Variables.URLIndex + "?title=Project:AutoWikiBrowser/Config&action=raw"; }
    }

    private static bool IsPublicAwbVersionDisabled(
       Updater.AWBEnabledStatus versionStatus)
    {
        return (versionStatus & Updater.AWBEnabledStatus.Disabled) ==
               Updater.AWBEnabledStatus.Disabled;
    }

    private static bool ShouldEnforcePublicAwbVersionGate()
    {
#if DEBUG
        // Debug builds have historically been allowed to continue through the
        // remaining status/config/user checks for regression testing.
        return false;
#else
            return !AllowForkReleaseVersionGateBypass;
#endif
    }

    /// <summary>
    /// Attempts to parse a JSON object using bounded reader settings and
    /// reports malformed or missing content through the debug log.
    ///
    /// This helper centralizes JSON parsing for remotely downloaded
    /// configuration documents so callers receive consistent validation
    /// and error handling.
    /// </summary>
    /// <param name="jsonText">
    /// The JSON text to parse.
    /// </param>
    /// <param name="sourceName">
    /// A descriptive name for the JSON source used in debug output.
    /// </param>
    /// <param name="json">
    /// When this method returns, contains the parsed JSON object if
    /// parsing succeeded; otherwise <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the JSON was successfully parsed; otherwise
    /// <c>false</c>.
    /// </returns>
    private static bool TryParseJsonObject(
    string jsonText,
    string sourceName,
    out JObject json)
    {
        json = null;

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Tools.WriteDebug(
                nameof(UpdateWikiStatus),
                sourceName + " returned no JSON content.");

            return false;
        }

        try
        {
            using (var stringReader = new StringReader(jsonText))
            using (var jsonReader = new JsonTextReader(stringReader)
            {
                MaxDepth = 32,
                DateParseHandling = DateParseHandling.None
            })
            {
                json = JObject.Load(jsonReader);
            }

            return true;
        }
        catch (JsonException ex)
        {
            Tools.WriteDebug(
                nameof(UpdateWikiStatus),
                sourceName + " contained invalid JSON: " + ex.Message);

            return false;
        }
    }

    /// <summary>
    /// Reads a Boolean property from a JSON object.
    ///
    /// Returns the supplied default value when the property is missing
    /// or is not a Boolean.
    /// </summary>
    /// <param name="json">
    /// The JSON object containing the property.
    /// </param>
    /// <param name="propertyName">
    /// The name of the Boolean property to read.
    /// </param>
    /// <param name="defaultValue">
    /// The value to return when the property is missing or invalid.
    /// </param>
    /// <returns>
    /// The Boolean property value, or <paramref name="defaultValue"/>
    /// if the property is unavailable.
    /// </returns>
    private static bool ReadBoolean(
        JObject json,
        string propertyName,
        bool defaultValue = false)
    {
        if (json == null)
            return defaultValue;

        JToken token = json[propertyName];

        return token?.Type == JTokenType.Boolean
            ? token.Value<bool>()
            : defaultValue;
    }

    /// <summary>
    /// Attempts to load the local AWB registration configuration from the
    /// current wiki's <c>CheckPageJSON</c> page.
    /// </summary>
    /// <returns>
    /// The raw JSON text from the local <c>CheckPageJSON</c> page when it is
    /// successfully retrieved. If the page is unavailable because the wiki
    /// does not provide it or denies access (for example, HTTP 403 or 404),
    /// an empty string is returned.
    /// </returns>
    /// <remarks>
    /// Many third-party MediaWiki installations do not host the optional
    /// <c>Project:AutoWikiBrowser/CheckPageJSON</c> page used by Wikimedia
    /// projects. In those cases, AWB continues using the normal registration
    /// logic without a local user list rather than treating the missing page
    /// as a fatal error.
    ///
    /// Only expected "page unavailable" responses are handled here. Other
    /// network failures, such as DNS resolution, TLS negotiation, or server
    /// errors, are allowed to propagate so they can be reported to the user.
    /// </remarks>
    private string LoadCheckPageJson()
    {
        if (!TryBuildAwbProjectPageUrl(
                "AutoWikiBrowser/CheckPageJSON",
                out string checkPageUrl))
        {
            Tools.WriteDebug(
                nameof(LoadCheckPageJson),
                "The current wiki does not define a usable project namespace. " +
                "Skipping the optional CheckPageJSON lookup.");

            return string.Empty;
        }

        try
        {
            return Editor.SynchronousEditor.HttpGet(
                checkPageUrl);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode is
                HttpStatusCode.Forbidden or
                HttpStatusCode.NotFound)
        {
            Tools.WriteDebug(
                nameof(LoadCheckPageJson),
                "The optional CheckPageJSON page is unavailable. " +
                $"Status: {ex.StatusCode}; URL: {checkPageUrl}");

            return string.Empty;
        }
    }

    /// <summary>
    /// Reads an array of strings from a JSON object.
    ///
    /// Non-string values, empty strings, and missing properties are
    /// ignored.
    /// </summary>
    /// <param name="json">
    /// The JSON object containing the array.
    /// </param>
    /// <param name="propertyName">
    /// The name of the array property to read.
    /// </param>
    /// <returns>
    /// A list containing the string values in the array. If the property
    /// is missing or is not an array, an empty list is returned.
    /// </returns>
    private static List<string> ReadStringArray(
        JObject json,
        string propertyName)
    {
        if (json?[propertyName] is not JArray values)
            return new List<string>();

        return values
            .Where(token => token.Type == JTokenType.String)
            .Select(token => token.Value<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    /// <summary>
    /// Applies the loaded site metadata to the shared project settings.
    /// </summary>
    private void ApplySiteInformation()
    {
        Variables.RTL = Site.IsRightToLeft;
        Variables.CapitalizeFirstLetter =
            Site.CapitalizeFirstLetter;

        Variables.UnicodeCategoryCollation =
            !Variables.IsCustomProject &&
            Regex.IsMatch(
                Site.CategoryCollation,
                "[a-z-]*uca-");

        if (Variables.IsCustomProject ||
            Variables.IsWikia)
        {
            Variables.LangCode = Site.Language;
        }

        Variables.TagEdits = Site.IsAWBTagDefined;
    }

    /// <summary>
    /// Attempts to retrieve the project namespace defined by the current wiki.
    /// </summary>
    /// <param name="projectNamespace">
    /// When this method returns, contains the project namespace, including its
    /// trailing colon, when one is available; otherwise, an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the wiki defines a usable project namespace;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// MediaWiki normally assigns namespace ID 4 to the project namespace.
    /// Some third-party wikis do not define a usable project namespace, so
    /// callers must not assume that the canonical <c>Project:</c> alias exists.
    /// </remarks>
    private bool TryGetProjectNamespace(
        out string projectNamespace)
    {
        projectNamespace = string.Empty;

        if (Site?.Namespaces == null ||
            !Site.Namespaces.TryGetValue(
                4,
                out string namespaceName) ||
            string.IsNullOrWhiteSpace(namespaceName))
        {
            return false;
        }

        projectNamespace = namespaceName;
        return true;
    }

    /// <summary>
    /// Attempts to build the raw-page URL for an optional AWB configuration
    /// page on the current wiki.
    /// </summary>
    /// <param name="subpage">
    /// The AWB subpage relative to the wiki's project namespace, such as
    /// <c>AutoWikiBrowser/CheckPageJSON</c>.
    /// </param>
    /// <param name="url">
    /// When successful, contains the URL used to retrieve the page content;
    /// otherwise, an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the current wiki defines a project namespace
    /// and the URL could be constructed; otherwise, <see langword="false"/>.
    /// </returns>
    private bool TryBuildAwbProjectPageUrl(
        string subpage,
        out string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subpage);

        url = string.Empty;

        if (!TryGetProjectNamespace(
                out string projectNamespace))
        {
            return false;
        }

        string pageTitle =
            projectNamespace +
            subpage;

        url =
            Variables.URLIndex +
            "?title=" +
            Uri.EscapeDataString(pageTitle) +
            "&action=raw";

        return true;
    }

    /// <summary>
    /// Validates and parses the downloaded global version metadata.
    /// </summary>
    /// <param name="versionJson">
    /// Contains the parsed version metadata when successful.
    /// </param>
    /// <returns>
    /// <c>true</c> when the metadata was parsed successfully; otherwise
    /// <c>false</c>.
    /// </returns>
    private static bool TryLoadGlobalVersionJson(
        out JObject versionJson)
    {
        return TryParseJsonObject(
            Updater.GlobalVersionPage,
            "The global version page",
            out versionJson);
    }

    /// <summary>
    /// Loads the local wiki configuration, falling back to the built-in
    /// default when the remote configuration page is unavailable.
    /// </summary>
    /// <param name="configJson">
    /// Contains the parsed configuration when successful.
    /// </param>
    /// <returns>
    /// <c>true</c> when a valid configuration was loaded; otherwise
    /// <c>false</c>.
    /// </returns>
    private bool TryLoadWikiConfiguration(
        out JObject configJson)
    {
        string downloadedConfig = string.Empty;

        if (TryBuildAwbProjectPageUrl(
                "AutoWikiBrowser/Config",
                out string configUrl))
        {
            try
            {
                downloadedConfig =
                    Editor.SynchronousEditor.HttpGet(
                        configUrl);
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode is
                    HttpStatusCode.Forbidden or
                    HttpStatusCode.NotFound)
            {
                Tools.WriteDebug(
                    nameof(TryLoadWikiConfiguration),
                    "The optional wiki configuration page is unavailable. " +
                    $"Status: {ex.StatusCode}; URL: {configUrl}");
            }
        }
        else
        {
            Tools.WriteDebug(
                nameof(TryLoadWikiConfiguration),
                "The current wiki does not define a usable project namespace. " +
                "Using the default configuration.");
        }

        if (!string.IsNullOrWhiteSpace(
                downloadedConfig))
        {
            ConfigJSONText = downloadedConfig;
        }
        else
        {
            ConfigJSONText = DefaultWikiConfig;
        }

        if (!TryParseJsonObject(
                ConfigJSONText,
                "The wiki configuration page",
                out configJson))
        {
            return false;
        }

        JSONMessages(configJson["messages"]);
        TypoLink(configJson);

        Variables.LoadUnderscores(
            ReadStringArray(
                    configJson,
                    "underscoretitles")
                .Select(value => value.Trim())
                .ToArray());

        NoGenfixes =
            ReadStringArray(
                configJson,
                "nogenfixes");

        NoRETF =
            ReadStringArray(
                configJson,
                "noregextypofix");

        return true;
    }

    /// <summary>
    /// Determines whether the current user is permitted to operate on
    /// the wiki and whether bot mode should be enabled.
    /// </summary>
    private WikiStatusResult DetermineRegistrationStatus(
        JObject versionJson,
        JObject configJson)
    {
        if (string.IsNullOrEmpty(CheckPageJSONText) ||
            ReadBoolean(
                configJson,
                "allusersenabled"))
        {
            IsBot = true;
            return WikiStatusResult.Registered;
        }

        if (!TryParseJsonObject(
                CheckPageJSONText,
                "The CheckPageJSON page",
                out JObject checkPageJson))
        {
            return WikiStatusResult.Error;
        }

        List<string> enabledUsers =
            ReadStringArray(
                checkPageJson,
                "enabledusers");

        List<string> enabledBots =
            ReadStringArray(
                checkPageJson,
                "enabledbots");

        var usernameComparer =
            new UsernameComparer();

        bool isBotEnabled =
            enabledBots.Contains(
                User.Name,
                usernameComparer);

        if (ReadBoolean(
                configJson,
                "allusersenabledusermode") ||
            (IsSysop &&
             Variables.Project != ProjectEnum.wikia) ||
            isBotEnabled ||
            enabledUsers.Contains(
                User.Name,
                usernameComparer))
        {
            IsBot = isBotEnabled;
            return WikiStatusResult.Registered;
        }

        if (Variables.Project != ProjectEnum.custom)
        {
            foreach (string globalUser in
                ReadStringArray(
                    versionJson,
                    "globalusers"))
            {
                if (User.Name == globalUser)
                {
                    return WikiStatusResult.Registered;
                }
            }
        }

        return WikiStatusResult.NotRegistered;
    }

    /// <summary>
    /// Refreshes the current wiki session state by loading site
    /// information, validating the running AWB version, downloading
    /// configuration pages, and determining the user's operational
    /// status.
    ///
    /// Returns a <see cref="WikiStatusResult"/> describing whether
    /// editing may proceed.
    /// </summary>
    /// <summary>
    /// Refreshes the current wiki session state and determines whether
    /// editing may proceed.
    /// </summary>
    /// <returns>
    /// A <see cref="WikiStatusResult"/> describing the current session
    /// and registration status.
    /// </returns>
    private WikiStatusResult UpdateWikiStatus()
    {
        try
        {
            IsBot = false;

            Site = new SiteInfo(Editor.SynchronousEditor);
            ApplySiteInformation();

            if (Updater.Result == Updater.AWBEnabledStatus.None)
            {
                Updater.CheckForUpdates();
            }

            Updater.WaitForCompletion();

            Updater.AWBEnabledStatus versionStatus = Updater.Result;
            VersionCheckPage = Updater.GlobalVersionPage;

            if (IsPublicAwbVersionDisabled(versionStatus) &&
                ShouldEnforcePublicAwbVersionGate())
            {
                return WikiStatusResult.OldVersion;
            }

            CheckPageJSONText = LoadCheckPageJson();

            if (!User.IsLoggedIn)
            {
                return WikiStatusResult.NotLoggedIn;
            }

            // MediaWiki no longer exposes the legacy "writeapi" user right.
            // Editing permission is validated through normal API responses.
            //
            // TODO:
            // Reassess the default Maxlag value after the .NET 8 networking
            // migration is complete.
            Editor.Maxlag = -1;

            if (!TryLoadGlobalVersionJson(out JObject versionJson))
            {
                return WikiStatusResult.Error;
            }

            if (IsGloballyBlockedUsername(versionJson))
            {
                return WikiStatusResult.NotRegistered;
            }

            JSONMessages(versionJson["messages"]);

            if (!TryLoadWikiConfiguration(out JObject configJson))
            {
                return WikiStatusResult.Error;
            }

            WikiStatusResult registrationStatus =
                DetermineRegistrationStatus(
                    versionJson,
                    configJson);

            Tools.WriteDebug(
                nameof(UpdateWikiStatus),
                $"Registration status: {registrationStatus}; " +
                $"IsBot: {IsBot}; " +
                $"Check page empty: {string.IsNullOrEmpty(CheckPageJSONText)}; " +
                $"Logged in: {User.IsLoggedIn}");

            return registrationStatus;
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                nameof(UpdateWikiStatus),
                ex.ToString());

            IsBot = false;
            return WikiStatusResult.Error;
        }
    }

    /// <summary>
    /// Determines whether the current username matches any of the
    /// globally configured bad-name regular expressions.
    ///
    /// The expressions are supplied by the remote global version page,
    /// so each match is evaluated with a timeout and invalid expressions
    /// are ignored after being written to the debug log.
    /// </summary>
    /// <param name="versionJson">
    /// The parsed JSON object from the global version page.
    /// </param>
    /// <returns>
    /// <c>true</c> if the current username matches a configured bad-name
    /// expression; otherwise <c>false</c>.
    /// </returns>
    private bool IsGloballyBlockedUsername(JObject versionJson)
    {
        if (string.IsNullOrEmpty(User.Name))
            return false;

        if (versionJson["badnames"] is not JArray badNames)
            return false;

        foreach (JToken badNameToken in badNames)
        {
            if (badNameToken.Type != JTokenType.String)
                continue;

            string badName = badNameToken.Value<string>();

            if (string.IsNullOrWhiteSpace(badName))
                continue;

            try
            {
                if (Regex.IsMatch(
                        User.Name,
                        badName,
                        RegexOptions.IgnoreCase |
                        RegexOptions.Multiline,
                        TimeSpan.FromSeconds(1)))
                {
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                Tools.WriteDebug(
                    nameof(IsGloballyBlockedUsername),
                    "A global bad-name regular expression exceeded the timeout.");
            }
            catch (ArgumentException)
            {
                Tools.WriteDebug(
                    nameof(IsGloballyBlockedUsername),
                    "The global version page contained an invalid bad-name regular expression.");
            }
        }

        return false;
    }

    public static void TypoLink(JObject configJson)
    {
        // don't update Variables.RetfPath if typolink is empty
        var typoLink = configJson["typolink"].ToString();
        if (!string.IsNullOrEmpty(typoLink))
        {
            Variables.RetfPath = typoLink;
            Tools.WriteDebug("UpdateWikiStatus", "RETF Path set from typolink as " + Variables.RetfPath);
        }
    }

    /// <summary>
    /// Gets a list of pages that shouldn't have genfixes run on them
    /// </summary>
    /// <returns>List of pages that shouldn't receive genfixes</returns>
    public List<string> NoGenfixes { get; private set; }

    /// <summary>
    /// Gets a list of pages that shouldn't be processed for typofixing
    /// </summary>
    /// <returns>List of pages that shouldn't receive typo fixing</returns>
    public List<string> NoRETF { get; private set; }

    private static void JSONMessages(JToken json)
    {
        if (json is not JArray messages)
            return;

        foreach (JToken message in messages)
        {
            JToken version = message["version"];

            if (version is JArray versions)
            {
                foreach (JToken item in versions)
                {
                    JSONMessage(
                        item.ToString(),
                        message);
                }
            }
            else if (version != null)
            {
                JSONMessage(
                    version.ToString(),
                    message);
            }
        }
    }

    /// <summary>
    /// Determines whether an automated JSON message applies to the
    /// currently running AWB version.
    /// </summary>
    /// <param name="versionString">
    /// The version identifier from the JSON configuration. Supports
    /// "*" to match all versions and numeric AWB version strings.
    /// </param>
    /// <returns>
    /// <c>true</c> if the message should be shown for the current
    /// AWB version; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Uses numeric <see cref="Version"/> comparison instead of string
    /// equality so equivalent version formats, such as 6.5 and 6.5.0.0,
    /// are treated as the same release.
    /// </remarks>
    private static bool MessageAppliesToVersion(string versionString)
    {
        if (versionString == "*")
            return true;

        if (!Version.TryParse(versionString, out Version messageVersion) ||
            !Version.TryParse(AWBVersion, out Version currentVersion))
        {
            return string.Equals(
                versionString,
                AWBVersion,
                StringComparison.OrdinalIgnoreCase);
        }

        return messageVersion.Major == currentVersion.Major &&
               messageVersion.Minor == currentVersion.Minor &&
               NormalizeVersionPart(messageVersion.Build) ==
                   NormalizeVersionPart(currentVersion.Build) &&
               NormalizeVersionPart(messageVersion.Revision) ==
                   NormalizeVersionPart(currentVersion.Revision);
    }

    private static int NormalizeVersionPart(int versionPart) =>
        versionPart < 0 ? 0 : versionPart;

    private static void JSONMessage(
        string versionString,
        JToken message)
    {
        if (!MessageAppliesToVersion(versionString) ||
            message["text"] == null)
        {
            return;
        }

        if (message["enabled"] != null &&
            !(bool)message["enabled"])
        {
            return;
        }

        // TODO: Replace the direct MessageBox dependency with a Session
        // event handled by the application UI.
        MessageBox.Show(
            message["text"].ToString().Trim(),
            "Automated message",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    /// <summary>
    /// Loads the namespace, magic-word, and localized month-name configuration
    /// for the current wiki.
    /// </summary>
    private void LoadProjectOptions()
    {
        try
        {
            Site = new SiteInfo(Editor.SynchronousEditor);

            LoadLocalizedMonthNames();
            ApplySiteConfiguration();
        }
        catch (ReadApiDeniedException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            ShowProjectLoadHttpException(ex);
            throw;
        }
        catch (Exception ex)
        {
            ShowProjectLoadException(ex);
            throw;
        }
    }

    /// <summary>
    /// Loads localized month names from the current wiki when the active project
    /// is not the English Wikipedia.
    /// </summary>
    private void LoadLocalizedMonthNames()
    {
        string[] months = (string[])Variables.ENLangMonthNames.Clone();

        for (int i = 0; i < months.Length; i++)
        {
            months[i] += "-gen";
        }

        if (Variables.IsWikipediaEN)
            return;

        Dictionary<string, string> messages = Site.GetMessages(months);

        if (messages.Count != months.Length)
            return;

        for (int i = 0; i < months.Length; i++)
        {
            months[i] = messages[months[i]];
        }

        Variables.MonthNames = months;
    }

    /// <summary>
    /// Applies the namespace, namespace-alias, and magic-word information loaded
    /// from the current wiki to the shared project configuration.
    /// </summary>
    private void ApplySiteConfiguration()
    {
        Variables.Namespaces = Site.Namespaces;
        Variables.NamespaceAliases = Site.NamespaceAliases;
        Variables.MagicWords = Site.MagicWords;
    }

    /// <summary>
    /// Displays an error message for an HTTP failure encountered while loading
    /// project information.
    /// </summary>
    /// <param name="exception">The HTTP request exception to report.</param>
    private static void ShowProjectLoadHttpException(
        HttpRequestException exception)
    {
        if (exception.StatusCode == HttpStatusCode.Unauthorized)
            return;

        MessageBox.Show(
            BuildProjectLoadHttpExceptionMessage(exception),
            "Error connecting to wiki",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    /// <summary>
    /// Builds the user-facing message for a project-loading HTTP failure.
    /// </summary>
    /// <param name="exception">The HTTP request exception to describe.</param>
    /// <returns>A message describing the HTTP failure.</returns>
    private static string BuildProjectLoadHttpExceptionMessage(
        HttpRequestException exception)
    {
        if (exception.InnerException == null)
            return exception.Message;

        if (exception.InnerException is AuthenticationException)
        {
            return $"{exception.Message} {exception.InnerException.Message}";
        }

        return exception.InnerException.Message;
    }

    /// <summary>
    /// Logs and displays an error encountered while loading project information.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    private static void ShowProjectLoadException(
        Exception exception)
    {
        (string message, string guidance) =
            GetProjectLoadErrorDetails(exception);

        Tools.WriteDebug(
            nameof(LoadProjectOptions),
            exception.ToString());

        MessageBox.Show(
            $"{guidance}\r\n\r\nError description: {message}",
            "Error connecting to wiki",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    /// <summary>
    /// Gets the user-facing error message and guidance for a project-loading
    /// exception.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>
    /// A tuple containing the underlying error message and guidance for resolving
    /// the problem.
    /// </returns>
    private static (string Message, string Guidance)
        GetProjectLoadErrorDetails(Exception exception)
    {
        return exception switch
        {
            WikiUrlException => (
                exception.InnerException?.Message ?? exception.Message,
                "The wiki URL or project configuration could not be recognized. " +
                "Enter the URL in the format \"en.wikipedia.org/w/\", including " +
                "the path containing index.php and api.php."),

            UriFormatException => (
                exception.Message,
                "The wiki URL is not valid. Check the protocol, host name, " +
                "and path, then try again."),

            AuthenticationException => (
                exception.Message,
                "A secure connection could not be established. Check the site's " +
                "TLS certificate and confirm that the wiki supports a compatible " +
                "HTTPS configuration."),

            Newtonsoft.Json.JsonException => (
                exception.Message,
                "The wiki returned malformed or unexpected JSON while loading " +
                "project configuration."),

            FormatException => (
                exception.Message,
                "The wiki returned project information in an unexpected format."),

            _ => (
                exception.Message,
                "An unexpected error occurred while loading project information.")
        };
    }
}