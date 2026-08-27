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

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using Twain.Core.API;

namespace Twain.Core;

/// <summary>
/// Manages the editing session, site configuration, registration state,
/// and editor integration for the currently selected wiki.
/// </summary>
/// <remarks>
/// <para>
/// A session owns the active <see cref="AsyncApiEdit"/> instance and exposes
/// information about the current user, page, site, and editing state.
/// </para>
/// <para>
/// It also coordinates loading wiki metadata, optional AutoWikiBrowser
/// configuration pages, global version information, and user registration
/// rules.
/// </para>
/// </remarks>
public class Session
{
    // TODO: Separate session/domain behavior from WinForms dependencies.
    // Session currently owns a Control and directly displays MessageBox UI.
    // Move user-facing notifications to events or another UI-neutral
    // abstraction so this class can remain in Twain.Core.

    // TODO: Reduce Session's direct dependency on global Variables state.
    // Site and project configuration should eventually be represented by
    // explicit session/configuration objects that can be passed to consumers.

    // TODO: Separate remote configuration/registration policy from the editing
    // session. CheckPageJSON, Config JSON, global version information, and
    // registration evaluation are distinct responsibilities that can later
    // become independently testable services.

    #region Properties

    /// <summary>
    /// Gets the API editor used by the current wiki session.
    /// </summary>
    public AsyncApiEdit Editor { get; private set; }

    /// <summary>
    /// Gets information about the currently authenticated user.
    /// </summary>
    public UserInfo User => Editor.User;

    /// <summary>
    /// Gets information about the page currently loaded by the editor.
    /// </summary>
    public PageInfo Page => Editor.Page;

    /// <summary>
    /// Gets metadata describing the current wiki.
    /// </summary>
    public SiteInfo Site { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the API editor is currently performing
    /// an asynchronous operation.
    /// </summary>
    public bool IsBusy => Editor.IsActive;

    /// <summary>
    /// Gets a value indicating whether the current user should operate in bot
    /// mode for this session.
    /// </summary>
    public bool IsBot { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current user has sysop rights.
    /// </summary>
    public bool IsSysop => Editor.User.IsSysop;

    /// <summary>
    /// Gets the raw JSON text retrieved from the current wiki's optional
    /// <c>CheckPageJSON</c> page.
    /// </summary>
    public string CheckPageJSONText { get; private set; }

    /// <summary>
    /// Gets or sets the raw JSON text used for the current wiki's AWB
    /// configuration.
    /// </summary>
    public string ConfigJSONText { get; set; }

    /// <summary>
    /// Gets the raw JSON text retrieved from the global version-check page.
    /// </summary>
    public string VersionCheckPage { get; private set; }

    #endregion

    private readonly Control _parentControl;

    /// <summary>
    /// Initializes a new editing session associated with the specified parent
    /// control.
    /// </summary>
    /// <param name="parentControl">
    /// The UI control used as the owner for session-related dialogs and editor
    /// operations.
    /// </param>
    public Session(Control parentControl)
    {
        _parentControl = parentControl;
        UpdateProject(true);
    }

    /// <summary>
    /// Creates and configures an API editor for the specified wiki URL.
    /// </summary>
    /// <param name="wikiUrl">
    /// The base URL used by the API editor.
    /// </param>
    /// <returns>
    /// The configured editor instance.
    /// </returns>
    private AsyncApiEdit CreateEditor(string wikiUrl)
    {
        AsyncApiEdit edit = new(wikiUrl, _parentControl)
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

    /// <summary>
    /// Occurs when an asynchronous page-open operation completes.
    /// </summary>
    public event AsyncOpenEditHandler OpenComplete;

    /// <summary>
    /// Occurs when an asynchronous page-save operation completes.
    /// </summary>
    public event AsyncSaveEventHandler SaveComplete;

    /// <summary>
    /// Occurs when an asynchronous preview operation completes.
    /// </summary>
    public event AsyncStringEventHandler PreviewComplete;

    /// <summary>
    /// Occurs when the underlying editor reports an exception.
    /// </summary>
    public event AsyncExceptionEventHandler ExceptionCaught;

    /// <summary>
    /// Occurs when the MediaWiki maxlag threshold is exceeded.
    /// </summary>
    public event AsyncMaxlagEventHandler MaxlagExceeded;

    /// <summary>
    /// Occurs when the current user is logged off.
    /// </summary>
    public event AsyncEventHandler LoggedOff;

    /// <summary>
    /// Occurs when the state of the underlying editor changes.
    /// </summary>
    public event AsyncEventHandler StateChanged;

    /// <summary>
    /// Occurs when an editor operation is aborted.
    /// </summary>
    public event AsyncEventHandler Aborted;

    /// <summary>
    /// Forwards an editor open-complete event to session subscribers.
    /// </summary>
    /// <param name="sender">
    /// The editor that completed the operation.
    /// </param>
    /// <param name="pageInfo">
    /// Information about the opened page.
    /// </param>
    private void OnOpenComplete(
        AsyncApiEdit sender,
        PageInfo pageInfo) =>
        OpenComplete?.Invoke(sender, pageInfo);

    /// <summary>
    /// Forwards an editor save-complete event to session subscribers.
    /// </summary>
    /// <param name="sender">
    /// The editor that completed the operation.
    /// </param>
    /// <param name="saveInfo">
    /// Information about the completed save.
    /// </param>
    private void OnSaveComplete(
        AsyncApiEdit sender,
        SaveInfo saveInfo) =>
        SaveComplete?.Invoke(sender, saveInfo);

    /// <summary>
    /// Forwards an editor preview-complete event to session subscribers.
    /// </summary>
    /// <param name="sender">
    /// The editor that completed the operation.
    /// </param>
    /// <param name="result">
    /// The generated preview result.
    /// </param>
    private void OnPreviewComplete(
        AsyncApiEdit sender,
        string result) =>
        PreviewComplete?.Invoke(sender, result);

    /// <summary>
    /// Forwards an editor exception event to session subscribers.
    /// </summary>
    /// <param name="sender">
    /// The editor that reported the exception.
    /// </param>
    /// <param name="exception">
    /// The exception reported by the editor.
    /// </param>
    private void OnExceptionCaught(
        AsyncApiEdit sender,
        Exception exception) =>
        ExceptionCaught?.Invoke(sender, exception);

    /// <summary>
    /// Forwards an editor maxlag event to session subscribers.
    /// </summary>
    /// <param name="sender">
    /// The editor that encountered the maxlag condition.
    /// </param>
    /// <param name="maxlag">
    /// The maximum lag value reported by the server.
    /// </param>
    /// <param name="retryAfter">
    /// The suggested delay before retrying the operation.
    /// </param>
    private void OnMaxlagExceeded(
        AsyncApiEdit sender,
        double maxlag,
        int retryAfter) =>
        MaxlagExceeded?.Invoke(sender, maxlag, retryAfter);

    /// <summary>
    /// Forwards an editor logged-off event to session subscribers.
    /// </summary>
    /// <param name="sender">
    /// The editor that reported the event.
    /// </param>
    private void OnLoggedOff(AsyncApiEdit sender) =>
        LoggedOff?.Invoke(sender);

    /// <summary>
    /// Forwards an editor state-change event to session subscribers.
    /// </summary>
    /// <param name="sender">
    /// The editor whose state changed.
    /// </param>
    private void OnStateChanged(AsyncApiEdit sender) =>
        StateChanged?.Invoke(sender);

    /// <summary>
    /// Forwards an editor aborted event to session subscribers.
    /// </summary>
    /// <param name="sender">
    /// The editor whose operation was aborted.
    /// </param>
    private void OnAborted(AsyncApiEdit sender) =>
        Aborted?.Invoke(sender);

    #endregion

    /// <summary>
    /// Default configuration used when the current wiki does not provide an
    /// accessible <c>Project:AutoWikiBrowser/Config</c> page.
    /// </summary>
    private const string DefaultWikiConfig =
        "{ \"typolink\": \"\", \"allusersenabled\": true, " +
        "\"allusersenabledusermode\": true, \"messages\": [], " +
        "\"underscoretitles\": [], \"nogenfixes\": [], " +
        "\"noregextypofix\": [] }";

    private WikiStatusResult _status;

    /// <summary>
    /// Gets the current registration and operational status of the session.
    /// </summary>
    /// <remarks>
    /// Accessing this property triggers a status refresh when an update has
    /// previously been requested through <see cref="RequireUpdate"/>.
    /// </remarks>
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

    /// <summary>
    /// Updates the editor and optionally loads configuration information for
    /// the currently selected wiki.
    /// </summary>
    /// <param name="deferLoading">
    /// <see langword="true"/> to create or update the editor without immediately
    /// loading project options; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the editor was updated and any requested
    /// project information loaded successfully; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <exception cref="ReadApiDeniedException">
    /// Propagated when the current wiki denies the required read API access.
    /// </exception>
    /// <remarks>
    /// An existing editor is retained when its URL still matches the current
    /// project so that authentication state is not unnecessarily discarded.
    /// </remarks>
    public bool UpdateProject(bool deferLoading)
    {
        // Recreate only if project changed, to prevent losing login information.
        if (Editor == null || Editor.URL != Variables.URLLong)
        {
            Editor = CreateEditor(Variables.URLLong);
        }

        if (deferLoading)
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
    /// Determines whether a project-loading HTTP failure should be propagated
    /// to the caller instead of being treated as a recoverable load failure.
    /// </summary>
    /// <param name="exception">
    /// The HTTP request exception to inspect.
    /// </param>
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
    /// Marks the session status as requiring a refresh.
    /// </summary>
    public void RequireUpdate()
    {
        _status = WikiStatusResult.PendingUpdate;
    }

    /// <summary>
    /// Refreshes the session's wiki status.
    /// </summary>
    /// <returns>
    /// The updated registration and operational status.
    /// </returns>
    public WikiStatusResult Update()
    {
        Status = UpdateWikiStatus();
        return Status;
    }

    /// <summary>
    /// Gets the version of the assembly containing the current session
    /// implementation.
    /// </summary>
    // TODO: Replace the legacy AWB-specific name when compatibility naming can
    // be changed without affecting consumers.
    public static string AWBVersion
    {
        get
        {
            return Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version
                .ToString();
        }
    }

    /// <summary>
    /// Gets the legacy URL for the current wiki's AWB configuration page.
    /// </summary>
    /// <remarks>
    /// New configuration loading uses the wiki's actual project namespace.
    /// This property is retained for compatibility with existing callers.
    /// </remarks>
    // TODO: Determine whether external callers still require this property.
    // If not, remove it when legacy Project: assumptions are eliminated.
    public static string ConfigUrl
    {
        get
        {
            return Variables.URLIndex +
                   "?title=Project:AutoWikiBrowser/Config&action=raw";
        }
    }

    /// <summary>
    /// Determines whether the global version result marks the current AWB
    /// version as disabled.
    /// </summary>
    /// <param name="versionStatus">
    /// The version status flags returned by the updater.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the disabled flag is set; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool IsPublicAwbVersionDisabled(
        Updater.AWBEnabledStatus versionStatus)
    {
        return (versionStatus & Updater.AWBEnabledStatus.Disabled) ==
               Updater.AWBEnabledStatus.Disabled;
    }

    /// <summary>
    /// Determines whether the legacy public AWB version gate should prevent
    /// the current application from continuing.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the public AWB version gate should be
    /// enforced; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool ShouldEnforcePublicAwbVersionGate()
    {
#if DEBUG
        return false;
#else
    return !AllowForkReleaseVersionGateBypass;
#endif
    }

    /// <summary>
    /// Attempts to parse JSON text as an object using a bounded maximum depth and
    /// reports malformed or missing content through the debug log.
    /// </summary>
    /// <param name="jsonText">
    /// The JSON text to parse.
    /// </param>
    /// <param name="sourceName">
    /// A descriptive name for the JSON source used in debug output.
    /// </param>
    /// <param name="parsedJson">
    /// When this method returns, contains the parsed JSON object if parsing
    /// succeeded; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the JSON was successfully parsed as an object;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryParseJsonObject(
        string jsonText,
        string sourceName,
        [NotNullWhen(true)] out JsonObject? parsedJson)
    {
        parsedJson = null;

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Tools.WriteDebug(
                nameof(UpdateWikiStatus),
                sourceName + " returned no JSON content.");

            return false;
        }

        try
        {
            JsonNode? node =
                JsonNode.Parse(
                    jsonText,
                    documentOptions:
                        new JsonDocumentOptions
                        {
                            MaxDepth = 32
                        });

            if (node is not JsonObject jsonObject)
            {
                Tools.WriteDebug(
                    nameof(UpdateWikiStatus),
                    sourceName + " did not contain a JSON object.");

                return false;
            }

            parsedJson = jsonObject;
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
    /// </summary>
    /// <param name="jsonObject">
    /// The JSON object containing the property.
    /// </param>
    /// <param name="propertyName">
    /// The name of the Boolean property to read.
    /// </param>
    /// <param name="defaultValue">
    /// The value to return when the property is missing or invalid.
    /// </param>
    /// <returns>
    /// The Boolean property value, or <paramref name="defaultValue"/> if the
    /// property is unavailable or is not a Boolean value.
    /// </returns>
    private static bool ReadBoolean(
        JsonObject? jsonObject,
        string propertyName,
        bool defaultValue = false)
    {
        if (jsonObject?[propertyName] is not JsonValue value ||
            !value.TryGetValue(out bool result))
        {
            return defaultValue;
        }

        return result;
    }

    /// <summary>
    /// Attempts to load the local AWB registration configuration from the
    /// current wiki's <c>CheckPageJSON</c> page.
    /// </summary>
    /// <returns>
    /// The raw JSON text from the local <c>CheckPageJSON</c> page when it is
    /// successfully retrieved. If the page is unavailable because the wiki
    /// does not provide it or denies access, an empty string is returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Many third-party MediaWiki installations do not host the optional
    /// <c>Project:AutoWikiBrowser/CheckPageJSON</c> page used by Wikimedia
    /// projects. In those cases, AWB continues using the normal registration
    /// logic without a local user list rather than treating the missing page
    /// as a fatal error.
    /// </para>
    /// <para>
    /// Only expected page-unavailable responses are handled here. Other
    /// network failures are allowed to propagate.
    /// </para>
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
    /// </summary>
    /// <param name="jsonObject">
    /// The JSON object containing the array.
    /// </param>
    /// <param name="propertyName">
    /// The name of the array property to read.
    /// </param>
    /// <returns>
    /// A list containing the string values in the array. If the property is
    /// missing or is not an array, an empty list is returned.
    /// </returns>
    /// <remarks>
    /// Non-string values, empty strings, and missing properties are ignored.
    /// </remarks>
    private static List<string> ReadStringArray(
        JsonObject? jsonObject,
        string propertyName)
    {
        if (jsonObject?[propertyName] is not JsonArray values)
        {
            return new();
        }

        return values
            .OfType<JsonValue>()
            .Select(value =>
                value.TryGetValue(out string? text)
                    ? text
                    : null)
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
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
    /// <param name="pageUrl">
    /// When successful, contains the URL used to retrieve the page content;
    /// otherwise, an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the current wiki defines a project namespace
    /// and the URL could be constructed; otherwise, <see langword="false"/>.
    /// </returns>
    private bool TryBuildAwbProjectPageUrl(
        string subpage,
        out string pageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subpage);

        pageUrl = string.Empty;

        if (!TryGetProjectNamespace(
                out string projectNamespace))
        {
            return false;
        }

        string pageTitle =
            projectNamespace +
            subpage;

        pageUrl =
            Variables.URLIndex +
            "?title=" +
            Uri.EscapeDataString(pageTitle) +
            "&action=raw";

        return true;
    }

    // TODO(Twain Policy): Replace the dependency on Updater.GlobalVersionPage with
    // a structured version-policy result supplied by the future per-wiki Twain
    // compatibility and policy service. This helper currently reparses the raw
    // global Wikipedia VersionJSON downloaded by the legacy version check.
    /// <summary>
    /// Validates and parses the downloaded global version metadata.
    /// </summary>
    /// <param name="versionJson">
    /// Contains the parsed version metadata when successful.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the metadata was parsed successfully;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryLoadGlobalVersionJson(
        [NotNullWhen(true)] out JsonObject? versionJson)
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
    /// <see langword="true"/> when a valid configuration was loaded;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool TryLoadWikiConfiguration(
        [NotNullWhen(true)] out JsonObject? configJson)
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

    // TODO(Twain Policy): Replace the dependency on global VersionJSON
    // authorization data with the future per-wiki Twain policy and capability
    // service. Preserve the existing enabled-user, enabled-bot, administrator,
    // global-user, and custom-wiki behavior until equivalent policy rules have
    // been defined and tested.
    /// <summary>
    /// Determines whether the current user is permitted to operate on the wiki
    /// and whether bot mode should be enabled.
    /// </summary>
    /// <param name="versionJson">
    /// The parsed global version configuration.
    /// </param>
    /// <param name="configJson">
    /// The parsed configuration for the current wiki.
    /// </param>
    /// <returns>
    /// The registration status determined from the available configuration.
    /// </returns>
    private WikiStatusResult DetermineRegistrationStatus(
        JsonObject versionJson,
        JsonObject configJson)
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
                out JsonObject? checkPageJson))
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

    // TODO(Twain Policy): Replace the global VersionJSON dependencies in wiki
    // status evaluation with structured policy data from the future per-wiki
    // Twain policy and capability service. Preserve global username blocking,
    // configured messages, and registration behavior until equivalent policy
    // rules have been defined and tested.
    /// <summary>
    /// Refreshes the current wiki session state and determines whether editing
    /// may proceed.
    /// </summary>
    /// <returns>
    /// A <see cref="WikiStatusResult"/> describing the current session and
    /// registration status.
    /// </returns>
    private WikiStatusResult UpdateWikiStatus()
    {
        try
        {
            IsBot = false;

            Site = new SiteInfo(Editor.SynchronousEditor);
            ApplySiteInformation();

            // TODO: Move update/version-policy checks out of Session when the
            // legacy updater is replaced.
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
            // Reassess the default Maxlag value after the networking
            // modernization is complete.
            Editor.Maxlag = -1;

            if (!TryLoadGlobalVersionJson(
                    out JsonObject? versionJson))
            {
                return WikiStatusResult.Error;
            }

            if (IsGloballyBlockedUsername(versionJson))
            {
                return WikiStatusResult.NotRegistered;
            }

            JSONMessages(versionJson["messages"]);

            if (!TryLoadWikiConfiguration(
                    out JsonObject? configJson))
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
    /// Determines whether the current username matches any of the globally
    /// configured bad-name regular expressions.
    /// </summary>
    /// <param name="versionJson">
    /// The parsed JSON object from the global version page.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the current username matches a configured
    /// bad-name expression; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The expressions are supplied by the remote global version page, so each
    /// match is evaluated with a timeout and invalid expressions are ignored
    /// after being written to the debug log.
    /// </remarks>
    private bool IsGloballyBlockedUsername(JsonObject versionJson)
    {
        if (string.IsNullOrEmpty(User.Name))
            return false;

        if (versionJson["badnames"] is not JsonArray badNames)
            return false;

        foreach (JsonNode? badNameNode in badNames)
        {
            if (badNameNode is not JsonValue badNameValue ||
                !badNameValue.TryGetValue(out string? badName) ||
                string.IsNullOrWhiteSpace(badName))
            {
                continue;
            }

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

    /// <summary>
    /// Applies the configured typo-rule source URL when the current wiki
    /// configuration provides one.
    /// </summary>
    /// <param name="configJson">
    /// The parsed wiki configuration containing the optional
    /// <c>typolink</c> property.
    /// </param>
    // TODO: Move typo-rule source selection out of Session and into the
    // dedicated typo/language-quality configuration workflow. Different wikis
    // and languages should be able to select an appropriate rule source.
    public static void TypoLink(JsonObject configJson)
    {
        string? typoLink =
            configJson["typolink"]?.GetValue<string>();

        // Don't update Variables.RetfPath if typolink is empty.
        if (!string.IsNullOrEmpty(typoLink))
        {
            Variables.RetfPath = typoLink;

            Tools.WriteDebug(
                "UpdateWikiStatus",
                "RETF Path set from typolink as " +
                Variables.RetfPath);
        }
    }

    /// <summary>
    /// Gets the list of pages that should not receive general fixes.
    /// </summary>
    public List<string> NoGenfixes { get; private set; }

    /// <summary>
    /// Gets the list of pages that should not be processed by regex typo
    /// fixing.
    /// </summary>
    public List<string> NoRETF { get; private set; }

    /// <summary>
    /// Processes automated messages contained in a JSON message array.
    /// </summary>
    /// <param name="messagesNode">
    /// The JSON node expected to contain the configured message array.
    /// </param>
    private static void JSONMessages(JsonNode? messagesNode)
    {
        if (messagesNode is not JsonArray messages)
        {
            return;
        }

        foreach (JsonNode? messageNode in messages)
        {
            if (messageNode is not JsonObject message)
            {
                continue;
            }

            JsonNode? version = message["version"];

            if (version is JsonArray versions)
            {
                foreach (JsonNode? item in versions)
                {
                    if (item != null)
                    {
                        JSONMessage(
                            item.ToString(),
                            message);
                    }
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
    /// Determines whether an automated JSON message applies to the currently
    /// running AWB version.
    /// </summary>
    /// <param name="versionString">
    /// The version identifier from the JSON configuration. Supports
    /// <c>*</c> to match all versions and numeric AWB version strings.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the message should be shown for the current
    /// AWB version; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Uses numeric <see cref="Version"/> comparison instead of string equality
    /// so equivalent version formats, such as 6.5 and 6.5.0.0, are treated as
    /// the same release.
    /// </remarks>
    private static bool MessageAppliesToVersion(string versionString)
    {
        if (versionString == "*")
            return true;

        if (!Version.TryParse(
                versionString,
                out Version messageVersion) ||
            !Version.TryParse(
                AWBVersion,
                out Version currentVersion))
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

    /// <summary>
    /// Normalizes an unspecified version component to zero.
    /// </summary>
    /// <param name="versionPart">
    /// The version component to normalize.
    /// </param>
    /// <returns>
    /// Zero when <paramref name="versionPart"/> is negative; otherwise, the
    /// original value.
    /// </returns>
    private static int NormalizeVersionPart(int versionPart) =>
        versionPart < 0 ? 0 : versionPart;

    /// <summary>
    /// Displays an enabled automated message when it applies to the currently
    /// running application version.
    /// </summary>
    /// <param name="versionString">
    /// The version selector associated with the message.
    /// </param>
    /// <param name="message">
    /// The JSON object describing the automated message.
    /// </param>
    private static void JSONMessage(
        string versionString,
        JsonObject message)
    {
        if (!MessageAppliesToVersion(versionString))
        {
            return;
        }

        string? text =
            message["text"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        bool enabled = true;

        if (message["enabled"] is JsonValue enabledValue &&
            enabledValue.TryGetValue(out bool configuredEnabled))
        {
            enabled = configuredEnabled;
        }

        if (!enabled)
        {
            return;
        }

        // TODO: Replace the direct MessageBox dependency with a Session
        // event handled by the application UI.
        MessageBox.Show(
            text.Trim(),
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
    /// Loads localized month names from the current wiki when the active
    /// project is not the English Wikipedia.
    /// </summary>
    private void LoadLocalizedMonthNames()
    {
        string[] months =
            (string[])Variables.ENLangMonthNames.Clone();

        for (int i = 0; i < months.Length; i++)
        {
            months[i] += "-gen";
        }

        if (Variables.IsWikipediaEN)
            return;

        Dictionary<string, string> messages =
            Site.GetMessages(months);

        if (messages.Count != months.Length)
            return;

        for (int i = 0; i < months.Length; i++)
        {
            months[i] = messages[months[i]];
        }

        Variables.MonthNames = months;
    }

    /// <summary>
    /// Applies the namespace, namespace-alias, and magic-word information
    /// loaded from the current wiki to the shared project configuration.
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
    /// <param name="exception">
    /// The HTTP request exception to report.
    /// </param>
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
    /// <param name="exception">
    /// The HTTP request exception to describe.
    /// </param>
    /// <returns>
    /// A message describing the HTTP failure.
    /// </returns>
    private static string BuildProjectLoadHttpExceptionMessage(
        HttpRequestException exception)
    {
        if (exception.InnerException == null)
            return exception.Message;

        if (exception.InnerException is AuthenticationException)
        {
            return
                $"{exception.Message} " +
                $"{exception.InnerException.Message}";
        }

        return exception.InnerException.Message;
    }

    /// <summary>
    /// Logs and displays an error encountered while loading project
    /// information.
    /// </summary>
    /// <param name="exception">
    /// The exception to report.
    /// </param>
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
    /// <param name="exception">
    /// The exception to classify.
    /// </param>
    /// <returns>
    /// A tuple containing the underlying error message and guidance for
    /// resolving the problem.
    /// </returns>
    private static (string Message, string Guidance)
        GetProjectLoadErrorDetails(Exception exception)
    {
        return exception switch
        {
            WikiUrlException => (
                exception.InnerException?.Message ??
                exception.Message,
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

            JsonException => (
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