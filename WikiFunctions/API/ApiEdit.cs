/*
Copyright (C) 2008 Max Semenik

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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Xml;
using WikiFunctions.Controls;

namespace WikiFunctions.API;

// TODO (API Response Modernization):
// Replace remaining duplicate XmlReader-based response parsing with the
// validated XmlDocument response path used by CheckForErrors().
//
// TODO (Token Modernization):
// Consolidate edit, delete, move, protect, and rollback token acquisition into
// a shared token service once legacy MediaWiki token compatibility is removed.
/// <summary>
/// This class edits MediaWiki sites using api.php
/// </summary>
/// <remarks>
/// MediaWiki API manual: https://www.mediawiki.org/wiki/API
/// Site prerequisites: MediaWiki 1.13+ with the following settings:
/// * $wgEnableAPI = true; (enabled by default in DefaultSettings.php)
/// * $wgEnableWriteAPI = true; (removed in 1.32.0)
/// * AssertEdit extension installed (https://www.mediawiki.org/wiki/Extension:Assert_Edit)
/// </remarks>
public class ApiEdit : IApiEdit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEdit"/> class with the
    /// default state required for MediaWiki API operations.
    /// </summary>
    /// <remarks>
    /// The constructor creates a new cookie container for session management,
    /// initializes the user information object, and enables exceptions for new
    /// user messages by default. Additional state is configured as API requests
    /// are performed.
    /// </remarks>
    private ApiEdit()
    {
        Cookies = new CookieContainer();
        User = new UserInfo();
        NewMessageThrows = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEdit" /> class.
    /// </summary>
    /// <param name="url">Path to scripts on server</param>
    public ApiEdit(string url)
        : this(url, false)
    {
    }

    // TODO (MediaWiki Compatibility):
    // Review whether the legacy api.php5 endpoint remains necessary once AWB's
    // minimum supported MediaWiki and PHP versions are formally established.
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEdit"/> class.
    /// </summary>
    /// <param name="url">The absolute path to the wiki scripts directory.</param>
    /// <param name="usePHP5">
    /// Whether API script requests should use the legacy <c>.php5</c>
    /// extension.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is empty, whitespace, or not a valid absolute URI.
    /// </exception>
    public ApiEdit(string url, bool usePHP5)
        : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri projectUri))
        {
            throw new ArgumentException(
                "A valid absolute wiki URL is required.",
                nameof(url));
        }

        URL = url.EndsWith("/", StringComparison.Ordinal)
            ? url
            : url + "/";

        PHP5 = usePHP5;
        ApiURL = URL + "api.php" + (PHP5 ? "5" : "");

        if (ProxyCache.TryGetValue(url, out IWebProxy proxy))
        {
            ProxySettings = proxy;
        }
        // System proxy discovery can cause a long timeout on Linux because
        // Windows Internet Options are not available there.
        else if (!Globals.UsingLinux)
        {
            IWebProxy systemProxy = WebRequest.GetSystemWebProxy();

            ProxySettings = systemProxy.IsBypassed(projectUri)
                ? null
                : systemProxy;

            ProxyCache[url] = ProxySettings;
        }
    }

    // TODO (Session State Modernization):
    // Define the intended Clone() contract. The current implementation shares
    // Cookies, ProxySettings, and User, but does not copy transient state such as
    // Page, Action, HtmlHeaders, Request, or a changed NewMessageThrows value.
    // Decide which state should be shared, copied, or reset before introducing
    // independent mutable session objects.
    /// <summary>
    /// Creates a copy of the current API editor.
    /// </summary>
    /// <returns>
    /// A new <see cref="IApiEdit"/> instance initialized with the current
    /// connection settings and session state.
    /// </returns>
    /// <remarks>
    /// This method performs a shallow copy. Shared reference-type members,
    /// including the cookie container, proxy settings, and user information,
    /// are reused by the cloned instance.
    /// </remarks>
    public IApiEdit Clone()
    {
        return new ApiEdit
        {
            URL = URL,
            ApiURL = ApiURL,
            PHP5 = PHP5,
            Maxlag = Maxlag,
            Cookies = Cookies,
            ProxySettings = ProxySettings,
            User = User
        };
    }

    #region Properties

    /// <summary>
    /// Gets the base URL for the wiki's MediaWiki installation, including the
    /// trailing slash.
    /// </summary>
    /// <example>
    /// <c>https://en.wikipedia.org/w/</c>
    /// </example>
    public string URL { get; private set; }

    /// <summary>
    /// Gets the full URL of the wiki's MediaWiki API endpoint.
    /// </summary>
    /// <example>
    /// <c>https://en.wikipedia.org/w/api.php</c>
    /// </example>
    public string ApiURL { get; private set; }

    /// <summary>
    /// Gets the scheme and host of the configured wiki server.
    /// </summary>
    /// <example>
    /// For <see cref="URL"/> equal to <c>https://en.wikipedia.org/w/</c>,
    /// this property returns <c>https://en.wikipedia.org</c>.
    /// </example>
    private string Server =>
        new Uri(URL).GetLeftPart(UriPartial.Authority);

    /// <summary>
    /// Gets a value indicating whether the connected wiki uses the legacy
    /// PHP 5 compatibility behavior expected by this API implementation.
    /// </summary>
    public bool PHP5 { get; private set; }

    /// <summary>
    /// Gets or sets the maximum replication lag, in seconds, permitted for API
    /// requests that use maxlag checking.
    /// </summary>
    /// <remarks>
    /// A value accepted by the MediaWiki <c>maxlag</c> parameter is added to
    /// requests when the selected <see cref="ActionOptions"/> require it.
    /// </remarks>
    public int Maxlag { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether detection of new user messages
    /// should cause an exception.
    /// </summary>
    public bool NewMessageThrows { get; set; }

    /// <summary>
    /// Gets the API action for which the current page token was obtained.
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Gets information about the page currently loaded for an API operation.
    /// </summary>
    public PageInfo Page { get; private set; }

    /// <summary>
    /// Gets the HTTP response headers captured from the most recent request.
    /// </summary>
    public string HtmlHeaders { get; private set; }

    /// <summary>
    /// Cookies stored between requests
    /// </summary>
    public CookieContainer Cookies { get; private set; }

    // TODO (MediaWiki Compatibility):
    // Review whether intoken capability should be tracked per wiki or ApiEdit
    // instance rather than globally. A warning from one server currently disables
    // intoken for all API sessions in the process.
    /// <summary>
    /// Whether we should pass the intoken parameter to the API
    /// </summary>
    private static bool UseInToken = true;

    #endregion

    // TODO (Session State Modernization):
    // Review whether Reset() should also clear cached response state such as
    // HtmlHeaders once its consumers and lifetime requirements are documented.
    /// <summary>
    /// Resets transient API operation state without logging out or discarding the
    /// current session cookies.
    /// </summary>
    /// <remarks>
    /// The current page information, action, active request reference, and abort
    /// state are cleared. Authentication cookies and user information are retained.
    /// </remarks>
    public void Reset()
    {
        Action = null;
        Page = new PageInfo();
        Aborting = false;
        Request = null;
    }

    /// <summary>
    /// Aborts the current API request.
    /// </summary>
    /// <remarks>
    /// This method cancels the active legacy <see cref="HttpWebRequest"/> by
    /// calling <see cref="HttpWebRequest.Abort"/>. Modern task-based operations
    /// use <see cref="CancellationToken"/> instead.
    /// </remarks>
    public void Abort()
    {
        // TODO (HTTP Modernization):
        // Remove Aborting, Request, HttpWebRequest.Abort(), and the Thread.Sleep()
        // coordination workaround when all API requests use CancellationToken-based
        // HttpClient operations.

        Aborting = true;

        try
        {
            HttpWebRequest request = Request;
            request?.Abort();
            Thread.Sleep(1);
        }
        finally
        {
            Aborting = false;
        }
    }

    // TODO (Authentication Modernization):
    // Re-evaluate this CentralAuth cookie-domain workaround against current
    // Wikimedia authentication behavior. If it remains necessary, copy cookies
    // into new Cookie instances rather than mutating objects returned by the
    // existing CookieContainer.
    /// <summary>
    /// This is a hack required for some multilingual Wikimedia projects,
    /// where CentralAuth returns cookies with a redundant domain restriction.
    /// </summary>
    private void AdjustCookies()
    {
        Uri uri = new Uri(URL);
        string host = uri.Host;

        CookieContainer newCookies = new();

        Uri alternateUri = new UriBuilder(uri)
        {Host = "fnord." + host}.Uri;

        Uri[] urls = { uri, alternateUri };

        foreach (Uri currentUri in urls)
        {
            foreach (Cookie cookie in Cookies.GetCookies(currentUri))
            {
                cookie.Domain = host;
                newCookies.Add(cookie);
            }
        }

        Cookies = newCookies;
    }

    #region URL stuff

    // TODO (Request Construction):
    // Make BuildQuery() non-mutating. It currently removes "intoken" from the
    // caller's dictionary when legacy token support is disabled, which can affect
    // retries or later reuse of the same request parameters.
    /// <summary>
    /// Builds a URL-encoded query string from the specified request parameters.
    /// </summary>
    /// <param name="request">
    /// The request parameters to include in the query string.
    /// </param>
    /// <returns>
    /// A query string containing the encoded request parameters. Each parameter
    /// is prefixed with an ampersand.
    /// </returns>
    /// <remarks>
    /// Empty parameter names are ignored. Empty parameter values are preserved
    /// by emitting the parameter name followed by an equals sign.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    protected static string BuildQuery(Dictionary<string, string> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!UseInToken)
            request.Remove("intoken");

        var sb = new StringBuilder();

        foreach (KeyValuePair<string, string> kvp in request)
        {
            if (string.IsNullOrEmpty(kvp.Key))
                continue;

            sb.Append('&');
            sb.Append(kvp.Key);

            if (kvp.Key.Contains('='))
                Tools.WriteDebug(kvp.Key, "Api key parameter includes =");

            // Always emit an equals sign so empty values remain valid parameters,
            // including boolean parameters passed in the POST portion of a request.
            sb.Append('=');

            if (!string.IsNullOrEmpty(kvp.Value))
                sb.Append(WebUtility.UrlEncode(kvp.Value));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a MediaWiki query string containing one or more page titles.
    /// </summary>
    /// <param name="titles">
    /// The page titles to encode and include in the query.
    /// </param>
    /// <returns>
    /// A query string beginning with <c>&amp;titles=</c>, or an empty string
    /// when no titles are supplied.
    /// </returns>
    protected static string Titles(params string[] titles)
    {
        ArgumentNullException.ThrowIfNull(titles);

        if (titles.Length == 0)
            return string.Empty;

        string[] encodedTitles = new string[titles.Length];

        for (int i = 0; i < titles.Length; i++)
        {
            encodedTitles[i] = Tools.WikiEncode(titles[i]);
        }

        return "&titles=" + string.Join("|", encodedTitles);
    }

    // TODO (Request Construction):
    // Review whether Titles() and NamedTitles() should share a common helper.
    // Both methods perform identical title encoding and differ only in the
    // generated query parameter name.
    /// <summary>
    /// Builds a MediaWiki query string containing one or more page titles using
    /// the specified parameter name.
    /// </summary>
    /// <param name="paramName">
    /// The name of the query parameter.
    /// </param>
    /// <param name="titles">
    /// The page titles to encode and include in the query.
    /// </param>
    /// <returns>
    /// A query string beginning with the specified parameter name, or an empty
    /// string when no titles are supplied.
    /// </returns>
    protected static string NamedTitles(
        string paramName,
        params string[] titles)
    {
        ArgumentNullException.ThrowIfNull(paramName);
        ArgumentNullException.ThrowIfNull(titles);

        if (titles.Length == 0)
            return string.Empty;

        string[] encodedTitles = new string[titles.Length];

        for (int i = 0; i < titles.Length; i++)
        {
            encodedTitles[i] = Tools.WikiEncode(titles[i]);
        }

        return "&" + paramName + "=" + string.Join("|", encodedTitles);
    }

    // TODO (Request Construction):
    // Consider returning a new request dictionary rather than mutating the
    // supplied collection once request construction is centralized.
    /// <summary>
    /// Adds the selected API behavior parameters to the request.
    /// </summary>
    /// <param name="request">
    /// The request parameters to update.
    /// </param>
    /// <param name="options">
    /// The optional behaviors to apply to the request.
    /// </param>
    /// <remarks>
    /// This method modifies <paramref name="request"/> directly. Maxlag and login
    /// assertion parameters are added when requested. New-message properties are
    /// added only to query requests.
    /// </remarks>
    protected void AppendOptions(
        Dictionary<string, string> request,
        ActionOptions options)
    {
        if ((options & ActionOptions.CheckMaxlag) != 0 && Maxlag > 0)
            request.Add("maxlag", Maxlag.ToString());

        if ((options & ActionOptions.RequireLogin) != 0)
            request.Add("assert", "user");

        if (!request.TryGetValue("action", out string action) ||
            action != "query" ||
            (options & ActionOptions.CheckNewMessages) == 0)
        {
            return;
        }

        AppendNewMessageOptions(request);
    }

    // TODO (Request Construction):
    // Review whether these parameters should merge with existing values rather than
    // assuming they are absent from every query request.
    /// <summary>
    /// Adds the API parameters required to check for new messages and
    /// notifications.
    /// </summary>
    /// <param name="request">
    /// The query request parameters to update.
    /// </param>
    private void AppendNewMessageOptions(
        Dictionary<string, string> request)
    {
        if (request.TryGetValue("meta", out string meta))
            request["meta"] = meta + "|userinfo";
        else
            request.Add("meta", "userinfo");

        if (Variables.NotificationsEnabled &&
            User.HasReadNotificationsRight())
        {
            request["meta"] += "|notifications";
        }

        request.Add("uiprop", "hasmsg");
        request.Add("notprop", "count");
    }

    // TODO (Networking Modernization):
    // Review thread safety if ApiEdit begins creating requests concurrently.
    // The current Dictionary assumes single-threaded proxy cache access.
    /// <summary>
    /// Builds the URL for an XML API request using the specified action options.
    /// </summary>
    /// <param name="request">
    /// The request parameters to include in the URL.
    /// </param>
    /// <param name="options">
    /// The optional behaviors whose parameters should be added to the request.
    /// </param>
    /// <returns>
    /// The complete MediaWiki API URL.
    /// </returns>
    /// <remarks>
    /// This method modifies <paramref name="request"/> by passing it to
    /// <see cref="AppendOptions(Dictionary{string, string}, ActionOptions)"/>.
    /// </remarks>
    protected string BuildUrl(
        Dictionary<string, string> request,
        ActionOptions options)
    {
        AppendOptions(request, options);

        return $"{ApiURL}?format=xml{BuildQuery(request)}";
    }

    /// <summary>
    /// Builds the URL for an XML API request without adding optional behavior
    /// parameters.
    /// </summary>
    /// <param name="request">
    /// The request parameters to include in the URL.
    /// </param>
    /// <returns>
    /// The complete MediaWiki API URL.
    /// </returns>
    protected string BuildUrl(
        Dictionary<string, string> request)
    {
        return BuildUrl(request, ActionOptions.None);
    }

    #endregion

    #region Network access

    /// <summary>
    /// Caches proxy instances by their configuration key so equivalent proxy
    /// settings can reuse the same <see cref="IWebProxy"/> instance.
    /// </summary>
    private static readonly Dictionary<string, IWebProxy> ProxyCache = new();

    /// <summary>
    /// Stores the proxy configuration used for outgoing API requests.
    /// </summary>
    private IWebProxy ProxySettings;

    // TODO (HTTP Modernization):
    // Review the generated User-Agent once the legacy HttpWebRequest pipeline is
    // removed. Verify that the runtime identifier accurately reflects modern .NET
    // while preserving any MediaWiki compatibility expectations.
    /// <summary>
    /// Identifies WikiFunctions, the host operating system, and the active
    /// .NET runtime in outgoing HTTP requests.
    /// </summary>

    private static readonly string UserAgent =
        $"WikiFunctions ApiEdit/{Assembly.GetExecutingAssembly().GetName().Version} " +
        $"({Environment.OSVersion.VersionString}; .NET CLR {Environment.Version})";

    /// <summary>
    /// Creates a configured HTTP request message.
    /// </summary>
    /// <param name="method">The HTTP method used for the request.</param>
    /// <param name="url">The destination URL.</param>
    /// <returns>A configured HTTP request message.</returns>
    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url)
    {
        EnsureNetworkAccessAllowed();

        var request = new HttpRequestMessage(method, url);

        ConfigureBasicAuthentication(request);

        return request;
    }

    /// <summary>
    /// Creates and configures a legacy HTTP request using AWB networking settings.
    /// </summary>
    /// <param name="url">The absolute URL to request.</param>
    /// <returns>A configured HTTP request.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when network access is attempted during unit tests.
    /// </exception>
    protected HttpWebRequest CreateRequest(string url)
    {
        EnsureNetworkAccessAllowed();

        ConfigureLegacyTransportSecurity();

        HttpWebRequest request =
            (HttpWebRequest)WebRequest.Create(url);

        ConfigureConnectionSettings(request);
        ConfigureProxy(request);
        ConfigureRequestHeaders(request);
        ConfigureCookies(request, url);

        return request;
    }

    /// <summary>
    /// Prevents live network access while unit tests are running.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when network access is attempted during unit tests.
    /// </exception>
    private static void EnsureNetworkAccessAllowed()
    {
        if (Globals.UnitTestMode)
        {
            throw new InvalidOperationException(
                "Wikipedia must not be accessed during unit tests.");
        }
    }

    /// <summary>
    /// Applies the transport settings required by the legacy request pipeline.
    /// </summary>
    private static void ConfigureLegacyTransportSecurity()
    {
        ServicePointManager.Expect100Continue = false;

        // TODO (HTTP Modernization):
        // Re-evaluate whether explicit ServicePointManager configuration remains
        // necessary after the HttpClient migration.
        ServicePointManager.SecurityProtocol |=
            SecurityProtocolType.Tls11 |
            SecurityProtocolType.Tls12 |
            SecurityProtocolType.Tls13;
    }

    /// <summary>
    /// Configures connection reuse and disables the Expect: 100-continue behavior.
    /// </summary>
    private static void ConfigureConnectionSettings(
        HttpWebRequest request)
    {
        request.KeepAlive = true;
        request.ServicePoint.Expect100Continue = false;
        request.Expect = string.Empty;
    }

    /// <summary>
    /// Applies the configured proxy settings to the request.
    /// </summary>
    private void ConfigureProxy(HttpWebRequest request)
    {
        if (ProxySettings == null)
        {
            request.Proxy = null;
            return;
        }

        request.Proxy = ProxySettings;
        request.UseDefaultCredentials = true;
    }

    /// <summary>
    /// Applies the AWB user agent and supported response decompression methods.
    /// </summary>
    private void ConfigureRequestHeaders(HttpWebRequest request)
    {
        request.UserAgent = UserAgent;

        request.AutomaticDecompression =
            DecompressionMethods.Deflate |
            DecompressionMethods.GZip;
    }

    /// <summary>
    /// Attaches the session cookie container only to requests targeting the
    /// current wiki, preventing cookies from being sent to third-party sites.
    /// Cookies intentionally withheld from requests targeting 
    /// other hosts to avoid leaking authenticated session cookies.
    /// </summary>
    private void ConfigureCookies(
        HttpWebRequest request,
        string url)
    {
        if (IsCurrentWikiRequest(url))
        {
            request.CookieContainer = Cookies;
        }
    }

    /// <summary>
    /// Determines whether a request is being sent to the configured wiki.
    /// Cookies are sent only to requests with the same scheme, host, and port.
    /// </summary>
    /// <param name="requestUrl">The absolute URL of the outgoing request.</param>
    /// <returns>
    /// <c>true</c> when the request targets the configured wiki; otherwise,
    /// <c>false</c>.
    /// </returns>
    private bool IsCurrentWikiRequest(string requestUrl)
    {
        Uri requestUri;
        Uri wikiUri;

        if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out requestUri) ||
            !Uri.TryCreate(URL, UriKind.Absolute, out wikiUri))
        {
            return false;
        }

        return string.Equals(
                   requestUri.Scheme,
                   wikiUri.Scheme,
                   StringComparison.OrdinalIgnoreCase)
               && string.Equals(
                   requestUri.Host,
                   wikiUri.Host,
                   StringComparison.OrdinalIgnoreCase)
               && requestUri.Port == wikiUri.Port;
    }

    /// <summary>
    /// Indicates whether a legacy API request is currently being aborted.
    /// </summary>
    /// <remarks>
    /// This flag is used by the existing synchronous request workflow to
    /// distinguish an explicit abort from cancellation requested through a
    /// modern <see cref="CancellationToken"/>.
    /// </remarks>
    private bool Aborting;

    /// <summary>
    /// Stores the currently active legacy HTTP request so it can be aborted.
    /// </summary>
    /// <remarks>
    /// The request is shared with the legacy abort workflow and may be
    /// <see langword="null"/> when no request is active.
    /// </remarks>
    // TODO: Review this field during the HttpWebRequest-to-HttpClient migration.
    // Replace direct request storage and HttpWebRequest.Abort() with cooperative
    // cancellation through CancellationToken where possible.
    private HttpWebRequest Request;

    /// <summary>
    /// Synchronizes access to the active cancellation-scope state.
    /// </summary>
    private readonly object CancellationSyncRoot = new();

    /// <summary>
    /// Indicates whether a modern cancellation scope is currently active.
    /// </summary>
    private bool CancellationScopeActive;

    /// <summary>
    /// Stores the cancellation token associated with the active modern request
    /// scope.
    /// </summary>
    private CancellationToken ActiveCancellationToken;

    /// <summary>
    /// Executes an HTTP request and returns the response body.
    /// </summary>
    /// <param name="req">
    /// The configured HTTP request to execute.
    /// </param>
    /// <returns>
    /// The response body returned by the server, or an empty string when the
    /// server returns HTTP 404.
    /// </returns>
    /// <remarks>
    /// This method tracks the active request, configures HTTP Basic
    /// authentication when required, registers cancellation, validates protocol
    /// redirects, handles selected HTTP errors, and clears the active request
    /// reference when processing completes.
    /// </remarks>
    protected string GetResponseString(HttpWebRequest req)
    {
        Request = req;

        req = ConfigureBasicAuthentication(req);

        try
        {
            return ExecuteRequest(req);
        }
        catch (WebException ex)
        {
            ThrowIfModernRequestCancellation(ex);

            if (ex.Response is not HttpWebResponse resp)
                throw;

            switch (resp.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    break;

                case HttpStatusCode.NotFound:
                    Tools.WriteDebug(
                        nameof(ApiEdit),
                        $"HTTP 404 returned for '{req.RequestUri}'.");

                    return string.Empty;
            }

            throw;
        }
        finally
        {
            if (ReferenceEquals(Request, req))
                Request = null;
        }
    }

    /// <summary>
    /// Configures HTTP Basic authentication for the request when credentials
    /// have been provided.
    /// </summary>
    /// <param name="req">
    /// The request to configure.
    /// </param>
    /// <returns>
    /// The request containing the configured authentication credentials and
    /// authorization header.
    /// </returns>
    private HttpWebRequest ConfigureBasicAuthentication(HttpWebRequest req)
    {
        if (string.IsNullOrEmpty(Variables.HttpAuthUsername) ||
            string.IsNullOrEmpty(Variables.HttpAuthPassword))
        {
            return req;
        }

        NetworkCredential login = new()
        {
            UserName = Variables.HttpAuthUsername,
            Password = Variables.HttpAuthPassword
        };

        CredentialCache myCache = new()
    {
        { new Uri(URL), "Basic", login }
    };

        req.Credentials = myCache;

        return (HttpWebRequest)SetBasicAuthHeader(
            req,
            login.UserName,
            login.Password);
    }

    /// <summary>
    /// Executes the request, validates the response endpoint, and returns the
    /// response body.
    /// </summary>
    /// <param name="req">
    /// The configured request to execute.
    /// </param>
    /// <returns>
    /// The response body returned by the server.
    /// </returns>
    private string ExecuteRequest(HttpWebRequest req)
    {
        using IDisposable requestCancellation =
            RegisterRequestCancellation(req);

        using WebResponse resp = req.GetResponse();

        ValidateResponseScheme(req, resp);

        return ReadResponseString(resp);
    }

    /// <summary>
    /// Verifies that the response did not redirect the request to a different
    /// URI scheme.
    /// </summary>
    /// <param name="req">
    /// The original request.
    /// </param>
    /// <param name="resp">
    /// The response returned by the server.
    /// </param>
    /// <exception cref="UriChangedException">
    /// Thrown when the request and response use different URI schemes.
    /// </exception>
    private static void ValidateResponseScheme(
        HttpWebRequest req,
        WebResponse resp)
    {
        // T357908: A custom wiki may redirect HTTP requests to HTTPS.
        // The current check prevents later requests from continuing with a
        // mismatched protocol, but it occurs after the redirect has happened.
        //
        // TODO: During the networking modernization, resolve the canonical API
        // endpoint before authentication or POST requests so HTTP-to-HTTPS
        // redirects can update the active session state before the request is sent.
        if (req.RequestUri.Scheme != resp.ResponseUri.Scheme)
        {
            throw new UriChangedException(
                req.RequestUri.Scheme,
                resp.ResponseUri.Scheme);
        }
    }

    /// <summary>
    /// Reads the complete response body as a string.
    /// </summary>
    /// <param name="resp">
    /// The response whose content should be read.
    /// </param>
    /// <returns>
    /// The complete response body.
    /// </returns>
    private static string ReadResponseString(WebResponse resp)
    {
        using var sr = new StreamReader(resp.GetResponseStream());

        return sr.ReadToEnd();
    }

    /// <summary>
    /// Stores the POST parameters from the most recent API request.
    /// </summary>
    /// <remarks>
    /// Retained for diagnostics, logging, or retry scenarios.
    /// </remarks>
    private Dictionary<string, string> lastPostParameters;

    /// <summary>
    /// Stores the URL used for the most recent HTTP GET request.
    /// </summary>
    /// <remarks>
    /// Retained for diagnostics, logging, or retry scenarios.
    /// </remarks>
    private string lastGetUrl;

    /// <summary>
    /// Creates a copy of request parameters that is safe to include in
    /// debug logs, exception details, or diagnostic reports.
    ///
    /// Parameter values whose names indicate credentials or API tokens are
    /// replaced with <c>&lt;removed&gt;</c>. The original parameter dictionary
    /// is not changed and can still be used to send the actual request.
    /// </summary>
    /// <param name="parameters">
    /// The original request parameters.
    /// </param>
    /// <returns>
    /// A new dictionary containing the original parameter names and either the
    /// original value or a redacted placeholder for sensitive values.
    /// </returns>
    private static Dictionary<string, string> CreateSafeDiagnosticCopy(
        IDictionary<string, string> parameters)
    {
        Dictionary<string, string> safeCopy = new(StringComparer.OrdinalIgnoreCase);

        if (parameters == null)
            return safeCopy;

        foreach (KeyValuePair<string, string> parameter in parameters)
        {
            safeCopy[parameter.Key] = IsSensitiveParameter(parameter.Key)
                ? "<redacted>"
                : parameter.Value;
        }

        return safeCopy;
    }

    // =====================================================
    // TODO (Request Construction):
    // Consider extracting the low-level request-building helpers into a dedicated
    // MediaWiki request builder once the HttpClient migration is complete.
    // =====================================================

    /// <summary>
    /// Determines whether a request parameter name is likely to contain a
    /// password, login credential, edit token, CSRF token, or similar secret.
    ///
    /// This checks for the words <c>password</c> and <c>token</c> anywhere in
    /// the parameter name so that MediaWiki-specific names such as
    /// <c>lgpassword</c>, <c>lgtoken</c>, and <c>logintoken</c> are also redacted.
    /// </summary>
    /// <param name="parameterName">
    /// The request parameter name to inspect.
    /// </param>
    /// <returns>
    /// <c>true</c> when the parameter value should be removed from diagnostics;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool IsSensitiveParameter(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return false;

        return parameterName.IndexOf(
                   "password",
                   StringComparison.OrdinalIgnoreCase) >= 0
            || parameterName.IndexOf(
                   "token",
                   StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // TODO (Authentication Modernization):
    // Consolidate Basic Authentication header generation so the legacy
    // HttpWebRequest and modern HttpRequestMessage pipelines use the same
    // credential encoding and header construction behavior.
    //
    // TODO: Review the character encoding used for Basic Authentication during
    // the HttpWebRequest-to-HttpClient migration. Encoding.Default depends on the
    // local system code page and may not produce consistent credentials across environments.
    /// <summary>
    /// Adds an HTTP Basic Authentication header to the specified request.
    /// </summary>
    /// <param name="req">
    /// The request whose authorization header should be configured.
    /// </param>
    /// <param name="userName">
    /// The username to include in the Basic Authentication credentials.
    /// </param>
    /// <param name="userPassword">
    /// The password to include in the Basic Authentication credentials.
    /// </param>
    /// <returns>
    /// The same request instance with its <c>Authorization</c> header set.
    /// </returns>
    /// <remarks>
    /// The username and password are combined using the Basic Authentication
    /// credential format and encoded as Base64 before being added to the request.
    ///
    /// Source:
    /// http://blog.kowalczyk.info/article/Forcing-basic-http-authentication-for-HttpWebReq.html
    /// </remarks>
    protected WebRequest SetBasicAuthHeader(
        WebRequest req,
        string userName,
        string userPassword)
    {
        string credentials = $"{userName}:{userPassword}";
        string encodedCredentials =
            Convert.ToBase64String(Encoding.Default.GetBytes(credentials));

        req.Headers["Authorization"] = $"Basic {encodedCredentials}";

        return req;
    }

    /// <summary>
    /// Sends an HTTP POST request to the API and returns the response body.
    /// </summary>
    /// <param name="get">
    /// The query-string parameters to append to the API URL.
    /// </param>
    /// <param name="post">
    /// The form parameters to include in the POST request body.
    /// </param>
    /// <param name="options">
    /// Options that control URL construction and request behavior.
    /// </param>
    /// <returns>
    /// The response body returned by the server.
    /// </returns>
    /// <remarks>
    /// A redacted copy of the request parameters is retained for diagnostics.
    /// The original parameter collection is used to construct and send the request.
    /// </remarks>
    protected string HttpPost(
        Dictionary<string, string> get,
        Dictionary<string, string> post,
        ActionOptions options)
    {
        string url = BuildUrl(get, options);
        Tools.WriteDebug("ApiEdit::HttpPost", url);

        RecordPostDiagnostics(url, post);

        byte[] postData = BuildPostData(post);
        HttpWebRequest req = CreatePostRequest(url, postData.Length);

        Request = req;

        try
        {
            WritePostData(req, postData);

            return GetResponseString(req);
        }
        catch (WebException ex)
        {
            ThrowIfModernRequestCancellation(ex);
            throw;
        }
        finally
        {
            if (ReferenceEquals(Request, req))
                Request = null;
        }
    }

    // TODO (Diagnostics Modernization):
    // Replace the separate lastGetUrl and lastPostParameters fields with one
    // immutable request-diagnostics record if ApiEdit begins supporting concurrent
    // requests.
    /// <summary>
    /// Records redacted request information for later diagnostics.
    /// </summary>
    /// <param name="url">
    /// The URL used for the request.
    /// </param>
    /// <param name="post">
    /// The POST parameters whose safe diagnostic copy should be retained.
    /// </param>
    private void RecordPostDiagnostics(
        string url,
        Dictionary<string, string> post)
    {
        lastGetUrl = url;

        // Keep only a redacted copy for exception/debug diagnostics.
        // The original post dictionary is still used to send the real request.
        lastPostParameters = CreateSafeDiagnosticCopy(post);
    }

    /// <summary>
    /// Encodes the POST parameters as a UTF-8 form request body.
    /// </summary>
    /// <param name="post">
    /// The POST parameters to encode.
    /// </param>
    /// <returns>
    /// The encoded request body.
    /// </returns>
    private static byte[] BuildPostData(
        Dictionary<string, string> post)
    {
        // TODO (Request Construction):
        // Separate form-body encoding from URL query construction when request-building
        // responsibilities move into a dedicated component.
        string query = BuildQuery(post);

        return Encoding.UTF8.GetBytes(query);
    }

    /// <summary>
    /// Creates and configures an HTTP request for a form-encoded POST operation.
    /// </summary>
    /// <param name="url">
    /// The destination URL.
    /// </param>
    /// <param name="contentLength">
    /// The size of the encoded request body in bytes.
    /// </param>
    /// <returns>
    /// The configured POST request.
    /// </returns>
    private HttpWebRequest CreatePostRequest(
        string url,
        int contentLength)
    {
        HttpWebRequest req = CreateRequest(url);

        req.Method = "POST";
        req.ContentType = "application/x-www-form-urlencoded";
        req.ContentLength = contentLength;

        return req;
    }

    // TODO (HTTP Modernization):
    // Replace direct request-stream writing with HttpContent when the legacy
    // HttpWebRequest POST pipeline is retired.
    /// <summary>
    /// Writes the encoded POST body to the request stream.
    /// </summary>
    /// <param name="req">
    /// The request whose body should be written.
    /// </param>
    /// <param name="postData">
    /// The encoded request body.
    /// </param>
    private void WritePostData(
        HttpWebRequest req,
        byte[] postData)
    {
        using IDisposable requestCancellation =
            RegisterRequestCancellation(req);

        using Stream rs = req.GetRequestStream();

        rs.Write(postData, 0, postData.Length);
    }

    /// <summary>
    /// Sends an HTTP POST request to the API using the default action options.
    /// </summary>
    /// <param name="get">
    /// The query-string parameters to append to the API URL.
    /// </param>
    /// <param name="post">
    /// The form parameters to include in the POST request body.
    /// </param>
    /// <returns>
    /// The response body returned by the server.
    /// </returns>
    /// <remarks>
    /// This overload delegates to
    /// <see cref="HttpPost(Dictionary{string, string}, Dictionary{string, string}, ActionOptions)"/>
    /// using <see cref="ActionOptions.None"/>.
    /// </remarks>
    protected string HttpPost(
        Dictionary<string, string> get,
        Dictionary<string, string> post) =>
        HttpPost(get, post, ActionOptions.None);

    /// <summary>
    /// Performs a HTTP request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="options"></param>
    /// <returns>Text received</returns>
    protected string HttpGet(Dictionary<string, string> request, ActionOptions options)
    {
        string url = BuildUrl(request, options);
        lastGetUrl = url;

        return HttpGet(url);
    }

    /// <summary>
    /// Sends an HTTP GET request using the specified API parameters and action options.
    /// </summary>
    /// <param name="request">
    /// The query parameters to include in the API URL.
    /// </param>
    /// <param name="options">
    /// Options that control URL construction and request behavior.
    /// </param>
    /// <returns>
    /// The response body returned by the server.
    /// </returns>
    protected string HttpGet(Dictionary<string, string> request)
    {
        return HttpGet(request, ActionOptions.None);
    }

    /// <summary>
    /// Sends an HTTP GET request to the specified URL and returns the response body.
    /// </summary>
    /// <param name="url">
    /// The complete URL to request.
    /// </param>
    /// <returns>
    /// The response body returned by the server.
    /// </returns>
    public string HttpGet(string url)
    {
        Tools.WriteDebug("ApiEdit::HttpGet", url);

        // TODO (HTTP Resilience):
        // Replace this unbounded retry loop with an explicit retry policy that documents
        // retryable failures, delay or backoff behavior, maximum attempts, and
        // cancellation handling.
        while (true)
        {
            try
            {
                return GetResponseString(CreateRequest(url));
            }
            catch (WebException ex)
            {
                if (!Tools.HandleHttpException(ex))
                    throw;
            }
        }

    }

    // TODO (HTTP Modernization):
    // Centralize HttpClient and handler lifetime instead of creating a new client
    // per request. Preserve per-session proxy, cookie, credential, and destination
    // isolation while allowing connection pooling and deterministic disposal.
    /// <summary>
    /// Creates an HTTP client configured with the current ApiEdit session's proxy,
    /// cookies, decompression, credentials, and user-agent settings.
    /// </summary>
    /// <param name="url">
    /// The destination URL. Session cookies are included only when the destination
    /// targets the currently configured wiki.
    /// </param>
    /// <returns>A configured HTTP client.</returns>
    private HttpClient CreateHttpClient(string url)
    {
        EnsureNetworkAccessAllowed();

        bool useProxy = ProxySettings != null;

        var handler = new HttpClientHandler
        {
            AutomaticDecompression =
                DecompressionMethods.Deflate |
                DecompressionMethods.GZip,

            Proxy = ProxySettings,
            UseProxy = useProxy,

            // Preserve the behavior of the legacy request configuration.
            UseDefaultCredentials = useProxy,

            UseCookies = true,
            CookieContainer = IsCurrentWikiRequest(url)
                ? Cookies
                : new CookieContainer()
        };

        var client = new HttpClient(handler);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        return client;
    }

    /// <summary>
    /// Adds the configured HTTP Basic authentication header to a request when
    /// credentials have been supplied.
    /// </summary>
    /// <param name="request">The request to configure.</param>
    private static void ConfigureBasicAuthentication(
        HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(Variables.HttpAuthUsername) ||
            string.IsNullOrEmpty(Variables.HttpAuthPassword))
        {
            return;
        }

        string authenticationText =
            Variables.HttpAuthUsername +
            ":" +
            Variables.HttpAuthPassword;

        string encodedCredentials =
            Convert.ToBase64String(
                Encoding.Default.GetBytes(authenticationText));

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                encodedCredentials);
    }

    #endregion

    #region Login / user props

    /// <summary>
    /// Authenticates with the wiki using the supplied username and password.
    /// </summary>
    /// <param name="username">
    /// The username to authenticate.
    /// </param>
    /// <param name="password">
    /// The password associated with the specified username.
    /// </param>
    /// <remarks>
    /// This overload performs a standard login without supplying an
    /// authentication domain.
    /// </remarks>
    public void Login(string username, string password) =>
        Login(username, password, string.Empty);

    /// <summary>
    /// Authenticates with the wiki using the supplied credentials and optional
    /// authentication domain.
    /// </summary>
    /// <param name="username">
    /// The username to authenticate.
    /// </param>
    /// <param name="password">
    /// The password associated with the specified username.
    /// </param>
    /// <param name="domain">
    /// The optional authentication domain used by legacy login endpoints.
    /// </param>
    /// <remarks>
    /// The method first requests a modern MediaWiki login token. Standard user
    /// accounts are authenticated through the client-login API, while bot
    /// passwords and legacy configurations use the action-login workflow.
    /// </remarks>
    public void Login(
        string username,
        string password,
        string domain)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentException("Username required", nameof(username));

        PrepareForLogin();

        string result = RequestLoginToken(out string token);

        if (ShouldUseClientLogin(username, token))
        {
            ClientLogin(username, password, token);
        }
        else
        {
            result = PerformLegacyLogin(
                username,
                password,
                domain,
                token);
        }

        CheckForErrors(result, "login");
        AdjustCookies();

        RefreshUserInfo();
    }

    /// <summary>
    /// Resets the current API state and prepares a new cookie and user context
    /// for authentication.
    /// </summary>
    private void PrepareForLogin()
    {
        Reset();

        // The final user state is unknown until authentication completes.
        User = new UserInfo();
        Cookies = new CookieContainer();
    }

    /// <summary>
    /// Requests a login token using the modern MediaWiki token API.
    /// </summary>
    /// <param name="token">
    /// Receives the login token returned by the API, or <see langword="null"/>
    /// when no token was supplied.
    /// </param>
    /// <returns>
    /// The raw API response containing the token request result.
    /// </returns>
    private string RequestLoginToken(out string token)
    {
        string result = HttpPost(
            new()
                {
                { "action", "query" },
                { "meta", "tokens" },
                { "type", "login" }
                },
            new());

        Tools.WriteDebug(
            "API::Edit meta/tokens",
            "Received login-token response.");

        // TODO (API Response Modernization):
        // Replace the XML login-token parsing with the shared response model or parser
        // selected for the broader MediaWiki API response modernization.
        XmlDocument document = CheckForErrors(result, "query");

        XmlNode tokenNode =
            document.SelectSingleNode("/api/query/tokens");

        token = tokenNode?.Attributes?["logintoken"]?.Value;

        return result;
    }

    /// <summary>
    /// Determines whether the modern client-login workflow should be used.
    /// </summary>
    /// <param name="username">
    /// The username being authenticated.
    /// </param>
    /// <param name="token">
    /// The login token returned by the API.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when client login should be used; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool ShouldUseClientLogin(
        string username,
        string token) =>

        /// <remarks>
        /// Usernames containing <c>@</c> are treated as bot-password style credentials
        /// and continue through the legacy action-login workflow.
        /// </remarks>
        // TODO (Authentication Modernization):
        // Review the username-based bot-password detection rule once the supported
        // MediaWiki authentication baseline is finalized.
        !username.Contains('@') &&
        !string.IsNullOrEmpty(token);

    /// <summary>
    /// Authenticates using the legacy MediaWiki action-login workflow.
    /// </summary>
    /// <param name="username">
    /// The username to authenticate.
    /// </param>
    /// <param name="password">
    /// The password associated with the username.
    /// </param>
    /// <param name="domain">
    /// The optional authentication domain.
    /// </param>
    /// <param name="token">
    /// The login token returned by the modern token API, when available.
    /// </param>
    /// <returns>
    /// The raw response from the final login request.
    /// </returns>
    private string PerformLegacyLogin(
        string username,
        string password,
        string domain,
        string token)
    {
        Dictionary<string, string> post = BuildLegacyLoginParameters(
            username,
            password,
            domain,
            token);

        string result = HttpPost(
            new()
            {
            { "action", "login" }
            },
            post);

        XmlNode loginNode = GetLoginResponseNode(result);

        Tools.WriteDebug(
            "API::Edit action/login",
            "Received login response.");

        string status =
            XmlResponseHelpers.RequireAttributeValue(
                loginNode,
                "result");

        // TODO (MediaWiki Compatibility):
        // Remove the NeedToken retry path when the minimum supported MediaWiki
        // version no longer requires the legacy two-step action-login workflow.
        if (string.IsNullOrEmpty(token) &&
            status.Equals(
                "NeedToken",
                StringComparison.OrdinalIgnoreCase))
        {
            result = RetryLegacyLoginWithToken(
                post,
                loginNode,
                out status);
        }

        if (status != null &&
            !status.Equals(
                "Success",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new LoginException(this, status);
        }

        return result;
    }

    /// <summary>
    /// Builds the form parameters required by the legacy login API.
    /// </summary>
    /// <param name="username">
    /// The username to authenticate.
    /// </param>
    /// <param name="password">
    /// The password associated with the username.
    /// </param>
    /// <param name="domain">
    /// The optional authentication domain.
    /// </param>
    /// <param name="token">
    /// The login token to include when one is available.
    /// </param>
    /// <returns>
    /// The legacy login form parameters.
    /// </returns>
    private static Dictionary<string, string> BuildLegacyLoginParameters(
        string username,
        string password,
        string domain,
        string token)
    {
        bool domainSet = !string.IsNullOrEmpty(domain);

        Dictionary<string, string> post = new()
    {
        { "lgname", username },
        { "lgpassword", password }
    };

        post.AddIfTrue(domainSet, "lgdomain", domain);
        post.AddIfTrue(
            !string.IsNullOrEmpty(token),
            "lgtoken",
            token);

        return post;
    }

    /// <summary>
    /// Loads a login API response and returns its primary login result element.
    /// </summary>
    /// <param name="result">
    /// The raw XML response returned by the login request.
    /// </param>
    /// <returns>
    /// The response's <c>login</c> element.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response does not contain a <c>login</c> element.
    /// </exception>
    private static XmlNode GetLoginResponseNode(string result)
    {
        XmlDocument loginDocument =
            LoadApiXmlDocument(result);

        XmlNode loginNode =
            loginDocument.SelectSingleNode("/api/login");

        if (loginNode == null)
        {
            throw new InvalidOperationException(
                "The API response does not contain a <login> element.");
        }

        return loginNode;
    }

    /// <summary>
    /// Retries a legacy login request using the token returned by an initial
    /// <c>NeedToken</c> response.
    /// </summary>
    /// <param name="post">
    /// The existing legacy login parameters.
    /// </param>
    /// <param name="loginNode">
    /// The login element returned by the initial request.
    /// </param>
    /// <param name="status">
    /// Receives the result status from the retry response.
    /// </param>
    /// <returns>
    /// The raw response returned by the retry request.
    /// </returns>
    private string RetryLegacyLoginWithToken(
        Dictionary<string, string> post,
        XmlNode loginNode,
        out string status)
    {
        AdjustCookies();

        string token =
            XmlResponseHelpers.RequireAttributeValue(
                loginNode,
                "token");

        post.Add("lgtoken", token);

        string result = HttpPost(
            new()
            {
            { "action", "login" }
            },
            post);

        Tools.WriteDebug(
            "API::Edit action/login NeedToken",
            "Received login retry response.");

        loginNode = GetLoginResponseNode(result);

        status =
            XmlResponseHelpers.RequireAttributeValue(
                loginNode,
                "result");

        return result;
    }

    // TODO (Authentication Modernization):
    // Replace the EmailAuth-specific continuation logic with a generalized
    // client-login continuation handler if additional authentication mechanisms
    // such as OATHAuth are supported.
    /// <summary>
    /// Authenticates a user through the MediaWiki client-login workflow.
    /// </summary>
    /// <param name="username">
    /// The username to authenticate.
    /// </param>
    /// <param name="password">
    /// The password associated with the specified username.
    /// </param>
    /// <param name="token">
    /// The login token returned by the MediaWiki token API.
    /// </param>
    /// <exception cref="LoginException">
    /// Thrown when authentication fails, requires an unsupported continuation,
    /// or is cancelled by the user.
    /// </exception>
    public void ClientLogin(
        string username,
        string password,
        string token)
    {
        Dictionary<string, string> postparams =
            BuildClientLoginParameters(password, token);

        string result =
            SendInitialClientLoginRequest(
                username,
                postparams);

        XmlNode clientLoginNode =
            GetClientLoginResponseNode(result);

        string status =
            GetClientLoginStatus(clientLoginNode);

        if (status == "PASS")
            return;

        // Handle two-factor authentication through EmailAuth.
        // OATHAuth should use a similar continuation flow through the OATHToken
        // parameter, but that path has not been tested.
        if (status != "UI")
            throw new LoginException(this, status);

        Match emailMatch =
            GetClientLoginEmailMatch(
                clientLoginNode,
                status);

        postparams.Clear();

        if (SupportsEmailAuthentication())
        {
            status =
                ContinueClientLoginWithEmailCode(
                    token,
                    emailMatch,
                    postparams);

            if (status == "PASS")
                return;

            // A UI response here probably indicates an incorrect code. Preserve
            // the existing single-attempt behavior rather than repeatedly prompting.
        }

        throw new LoginException(this, status);
    }

    /// <summary>
    /// Builds the form parameters required by the initial client-login request.
    /// </summary>
    /// <param name="password">
    /// The password associated with the user.
    /// </param>
    /// <param name="token">
    /// The login token returned by the API.
    /// </param>
    /// <returns>
    /// The client-login form parameters.
    /// </returns>
    private static Dictionary<string, string> BuildClientLoginParameters(
        string password,
        string token) =>
        new()
        {
            { "password", password },
            { "logintoken", token }
        };

    /// <summary>
    /// Sends the initial MediaWiki client-login request.
    /// </summary>
    /// <param name="username">
    /// The username to authenticate.
    /// </param>
    /// <param name="postparams">
    /// The client-login form parameters.
    /// </param>
    /// <returns>
    /// The raw API response.
    /// </returns>
    private string SendInitialClientLoginRequest(
        string username,
        Dictionary<string, string> postparams)
    {
        string result = HttpPost(
            new()
            {
                { "action", "clientlogin" },
                { "username", username },

                // TODO (Authentication Modernization):
                // Replace the hard-coded login return URL with an appropriate URI derived
                // from the active wiki or a documented neutral callback value.
                // Not used by AWB, but required by the MediaWiki API.
                { "loginreturnurl", "https://en.wikipedia.org/" }
            },
            postparams);

        Tools.WriteDebug(
            "API::Edit action/clientlogin",
            "Received ClientLogin response.");

        return result;
    }

    /// <summary>
    /// Validates a client-login response and returns its primary result element.
    /// </summary>
    /// <param name="result">
    /// The raw API response.
    /// </param>
    /// <returns>
    /// The response's <c>clientlogin</c> element.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response does not contain a <c>clientlogin</c> element.
    /// </exception>
    private XmlNode GetClientLoginResponseNode(string result)
    {
        // ClientLogin can return a valid UI status for follow-up authentication,
        // so validate API-level errors without treating the status itself as an
        // action-specific success or failure result.
        XmlDocument document = CheckForErrors(result);

        XmlNode clientLoginNode =
            document.SelectSingleNode("/api/clientlogin");

        if (clientLoginNode == null)
        {
            throw new InvalidOperationException(
                "The API response does not contain a <clientlogin> element.");
        }

        return clientLoginNode;
    }

    /// <summary>
    /// Gets the normalized status from a client-login response.
    /// </summary>
    /// <param name="clientLoginNode">
    /// The response's <c>clientlogin</c> element.
    /// </param>
    /// <returns>
    /// The client-login status normalized to uppercase using invariant casing.
    /// </returns>
    private static string GetClientLoginStatus(
        XmlNode clientLoginNode) =>
        XmlResponseHelpers
            .RequireAttributeValue(
                clientLoginNode,
                "status")
            .ToUpperInvariant();

    /// <summary>
    /// Extracts the email-address text from a client-login continuation message.
    /// </summary>
    /// <param name="clientLoginNode">
    /// The response's <c>clientlogin</c> element.
    /// </param>
    /// <param name="status">
    /// The current client-login status.
    /// </param>
    /// <returns>
    /// The email-address match found in the response message.
    /// </returns>
    /// <exception cref="LoginException">
    /// Thrown when the response does not contain the expected email-address syntax.
    /// </exception>
    private Match GetClientLoginEmailMatch(
        XmlNode clientLoginNode,
        string status)
    {
        string message =
            clientLoginNode.Attributes?["message"]?.Value ??
            string.Empty;

        // TODO (Authentication Modernization):
        // Replace localized-message parsing with structured client-login response data
        // if MediaWiki exposes the EmailAuth destination independently of message text.
        // Preserve the existing unverified assumption that the email address
        // appears inside parentheses in every localization.
        Match emailMatch =
            Regex.Match(message, @"\(.+?@.+?\)");

        if (!emailMatch.Success)
            throw new LoginException(this, status);

        return emailMatch;
    }

    // TODO (ApiEdit Decomposition):
    // Obtain extension availability from shared site information when API metadata
    // and capability discovery are moved out of the authentication workflow.
    /// <summary>
    /// Determines whether the connected wiki reports support for the EmailAuth
    /// extension.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the EmailAuth extension is present; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private bool SupportsEmailAuthentication()
    {
        string result = HttpPost(
            new()
            {
                { "action", "query" },
                { "meta", "siteinfo" },
                { "siprop", "extensions" }
            },
            new());

        XmlDocument siteInfoDocument =
            CheckForErrors(result, "query");

        XmlNode emailAuthExtension =
            siteInfoDocument.SelectSingleNode(
                "/api/query/extensions/ext[@name='EmailAuth']");

        return emailAuthExtension != null;
    }

    /// <summary>
    /// Prompts for an email authentication code and submits the client-login
    /// continuation request.
    /// </summary>
    /// <param name="token">
    /// The original login token.
    /// </param>
    /// <param name="emailMatch">
    /// The email-address text extracted from the API response.
    /// </param>
    /// <param name="postparams">
    /// The form parameters to use for the continuation request.
    /// </param>
    /// <returns>
    /// The normalized status returned by the continuation request.
    /// </returns>
    /// <exception cref="LoginException">
    /// Thrown when the user cancels the authentication prompt.
    /// </exception>
    private string ContinueClientLoginWithEmailCode(
        string token,
        Match emailMatch,
        Dictionary<string, string> postparams)
    {
        // The complete server message is too long for InputBox. Continue using
        // the email-address text extracted from the message.
        InputBoxResult coderesult = InputBox.Show(
            $"Enter the code sent to your email {emailMatch.Value}.",
            "Enter One-Time-Code",
            string.Empty,
            ClientLoginValidator);

        if (!coderesult.OK)
            throw new LoginException(this, "Login cancelled");

        postparams.Add("logintoken", token);

        // TODO (Security):
        // Review whether the one-time authentication code can be submitted in the POST
        // body so it is not retained in URL, debug, or exception diagnostics.
        string result = HttpPost(
            new()
            {
                { "action", "clientlogin" },
                { "logincontinue", "1" },
                { "token", coderesult.Text }
            },
            postparams);

        Tools.WriteDebug(
            "API::Edit action/clientlogin2",
            "Received ClientLogin continuation response.");

        XmlNode clientLoginNode =
            GetClientLoginResponseNode(result);

        return GetClientLoginStatus(clientLoginNode);
    }

    /// <summary>
    /// Matches the six-digit one-time code currently required during client login.
    /// </summary>
    /// <remarks>
    /// TODO (MediaWiki Compatibility): Review this pattern if the supported
    /// authentication providers allow a different one-time-code format.
    /// </remarks>
    private static readonly Regex ClientLoginCodeRegex =
        new(@"^\d{6}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates the one-time code entered during client login.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the validation event.
    /// </param>
    /// <param name="e">
    /// Contains the entered text and allows the validation to be canceled.
    /// </param>
    /// <remarks>
    /// The one-time code must consist of exactly six numeric digits.
    /// </remarks>
    private static void ClientLoginValidator(
        object sender,
        InputBoxValidatingArgs e)
    {
        if (e.Text == null || !ClientLoginCodeRegex.IsMatch(e.Text))
        {
            e.Cancel = true;
            e.Message = "Code must be six digits";
        }
    }

    // TODO (Token Modernization):
    // Retrieve the logout CSRF token through the shared token service once token
    // acquisition and caching are extracted from ApiEdit.
    /// <summary>
    /// Logs out of the current wiki session.
    /// </summary>
    /// <remarks>
    /// MediaWiki requires logout requests to use a CSRF token and be sent as a
    /// POST request. The token is requested before any local session state is
    /// cleared so that a failed logout does not leave AWB believing it is logged
    /// out while the server session remains active.
    /// </remarks>
    public void Logout()
    {
        // Obtain an authenticated CSRF token before clearing local session state.
        string tokenResult = HttpGet(
            new Dictionary<string, string>
            {
                {"action", "query"},
                {"meta", "tokens"}
            });

        XmlDocument document = CheckForErrors(tokenResult, "query");

        string csrfToken;

        try
        {
            XmlNode tokenNode =
                document.SelectSingleNode("/api/query/tokens");

            if (tokenNode == null)
                throw new Exception("Cannot find <tokens> element");

            csrfToken =
                XmlResponseHelpers.RequireAttributeValue(
                    tokenNode,
                    "csrftoken");
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }

        // Logout is a state-changing API action and therefore requires POST plus
        // the CSRF token obtained above.
        string result = HttpPost(
            new Dictionary<string, string>
            {
               {"action", "logout"}
            },
            new Dictionary<string, string>
            {
               {"token", csrfToken}
            });

        CheckForErrors(result, "logout");

        // Clear local state only after the server confirms the logout succeeded.
        Reset();
        User = new UserInfo();
        Cookies = new CookieContainer();
    }

    /// <summary>
    /// Adds the specified page to the authenticated user's watchlist.
    /// </summary>
    /// <param name="title">
    /// The title of the page to watch.
    /// </param>
    /// <remarks>
    /// This overload performs a standard watch operation without requesting
    /// any additional watch action options.
    /// </remarks>
    public void Watch(string title) =>
        WatchAction(title, false);

    /// <summary>
    /// Adds or removes the specified page from the authenticated user's watchlist.
    /// </summary>
    /// <param name="title">
    /// The title of the page to watch or unwatch.
    /// </param>
    /// <param name="unwatch">
    /// <see langword="true"/> to remove the page from the watchlist; otherwise,
    /// <see langword="false"/> to add it.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> is null or empty.
    /// </exception>
    /// <exception cref="AbortedException">
    /// Thrown when the current operation has been aborted.
    /// </exception>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the API response does not contain valid watch-token data.
    /// </exception>
    public void WatchAction(string title, bool unwatch)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);

        EnsureWatchToken(title);

        if (Aborting)
            throw new AbortedException(this);

        Dictionary<string, string> watchParameters =
            BuildWatchParameters(title, unwatch);

        string result = HttpPost(
            new()
            {
                { "action", "watch" }
            },
            watchParameters,
            ActionOptions.All);

        CheckForErrors(result, "watch");
    }

    /// <summary>
    /// Ensures that the current page has a watch token available.
    /// </summary>
    /// <param name="title">
    /// The title used when requesting page and token information.
    /// </param>
    /// <remarks>
    /// MediaWiki 1.24 and later return the token in the <c>tokens</c> element.
    /// Older versions return it on the queried <c>page</c> element.
    /// </remarks>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the API response does not contain valid watch-token data.
    /// </exception>
    private void EnsureWatchToken(string title)
    {
        if (!string.IsNullOrEmpty(Page.WatchToken))
            return;

        string result = HttpGet(
            new()
            {
                { "action", "query" },
                { "prop", "info" },
                { "meta", "tokens" },
                { "type", "watch" },
                { "intoken", "watch" },
            { "titles", title }
            },
            ActionOptions.All);

        XmlDocument document = CheckForErrors(result);

        try
        {
            Page.WatchToken = GetWatchToken(document);
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    /// <summary>
    /// Extracts the watch token from a MediaWiki query response.
    /// </summary>
    /// <param name="document">
    /// The validated API response document.
    /// </param>
    /// <returns>
    /// The watch token returned by the API.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response does not contain a <c>tokens</c> or <c>page</c>
    /// element.
    /// </exception>
    private static string GetWatchToken(XmlDocument document)
    {
        // TODO (MediaWiki Compatibility):
        // Remove the pre-1.24 intoken and page-element watch-token fallback once
        // the minimum supported MediaWiki version no longer requires them.
        XmlNode tokenSource =
            document.SelectSingleNode("/api/query/tokens") ??
            document.SelectSingleNode("/api/query/pages/page");

        if (tokenSource == null)
        {
            throw new InvalidOperationException(
                "The API response does not contain a <tokens> or <page> element.");
        }

        return XmlResponseHelpers.RequireAttributeValue(
            tokenSource,
            "watchtoken");
    }

    /// <summary>
    /// Builds the form parameters for a watch or unwatch request.
    /// </summary>
    /// <param name="title">
    /// The title of the page to modify in the watchlist.
    /// </param>
    /// <param name="unwatch">
    /// <see langword="true"/> to request removal from the watchlist; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The form parameters required by the watch API.
    /// </returns>
    private Dictionary<string, string> BuildWatchParameters(
        string title,
        bool unwatch)
    {
        Dictionary<string, string> watchParameters = new()
        {
            { "title", title },
            { "token", Page.WatchToken }
        };

        if (unwatch)
            watchParameters.Add("unwatch", null);

        return watchParameters;
    }

    /// <summary>
    /// Removes the specified page from the authenticated user's watchlist.
    /// </summary>
    /// <param name="title">
    /// The title of the page to remove from the watchlist.
    /// </param>
    /// <remarks>
    /// This overload removes the page from the watchlist.
    /// </remarks>
    public void Unwatch(string title) =>
        WatchAction(title, true);

    /// <summary>
    /// Gets information about the currently authenticated user.
    /// </summary>
    /// <remarks>
    /// The property is refreshed after login and reset to an empty user-information
    /// instance when the session is cleared.
    /// </remarks>
    public UserInfo User { get; private set; }

    /// <summary>
    /// Refreshes information about the currently authenticated user.
    /// </summary>
    /// <remarks>
    /// Clears any cached user state, retrieves the latest user information from
    /// the MediaWiki API, validates the response, and updates the
    /// <see cref="User"/> property.
    /// </remarks>
    public void RefreshUserInfo()
    {
        // TODO (Session State Modernization):
        // Review whether refreshing user information requires a full ApiEdit reset or
        // only invalidation of user-related cached state.
        Reset();
        User = new UserInfo();

        string result = HttpPost(
            new()
            {
                { "action", "query" }
            },
            new()
            {
                { "meta", "userinfo" },
                { "uiprop", "blockinfo|hasmsg|groups|rights" }
            });

        XmlDocument xml =
            CheckForErrors(result, "userinfo");

        User = new UserInfo(xml);
    }

    /// <summary>
    /// Clears the authenticated user's "new messages" notification.
    /// </summary>
    /// <remarks>
    /// Sends the MediaWiki <c>clearhasmsg</c> action to acknowledge outstanding
    /// user talk page notifications.
    /// </remarks>
    public void ClearNewMessages()
    {
        string result = HttpPost(
            new()
            {
            { "action", "clearhasmsg" }
            },
            new());

        CheckForErrors(result, "clearhasmsg");
    }
    #endregion

    #region Page modification

    /// <summary>
    /// Opens the specified wiki page for editing without resolving redirects.
    /// </summary>
    /// <param name="title">
    /// The title of the page to open.
    /// </param>
    /// <returns>
    /// The current content of the opened page.
    /// </returns>
    public string Open(string title) =>
        Open(title, false);

    /// <summary>
    /// Opens the specified wiki page for editing.
    /// </summary>
    /// <param name="title">
    /// The title of the page to open.
    /// </param>
    /// <param name="resolveRedirects">
    /// <see langword="true"/> to resolve redirects before opening the page;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The current content of the opened page.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> is null or empty.
    /// </exception>
    /// <exception cref="LoggedOffException">
    /// Thrown when no authenticated user session is available.
    /// </exception>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the API response cannot be interpreted as valid page
    /// information.
    /// </exception>
    public string Open(string title, bool resolveRedirects)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);

        if (!User.IsLoggedIn)
            throw new LoggedOffException(this);

        Reset();

        Dictionary<string, string> query =
            BuildOpenQuery(title, resolveRedirects);

        string result =
            HttpGet(query, ActionOptions.All);

        XmlDocument document =
            CheckForErrors(result, "query");

        InitializeOpenedPage(document);

        return Page.Text;
    }

    /// <summary>
    /// Builds the MediaWiki query parameters required to open a page for editing.
    /// </summary>
    /// <param name="title">
    /// The title of the page to open.
    /// </param>
    /// <param name="resolveRedirects">
    /// <see langword="true"/> to request redirect resolution; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The query parameters required to retrieve the page content, metadata, and
    /// editing tokens.
    /// </returns>
    /// <remarks>
    /// Title-variant conversion is requested so valid variant titles resolve
    /// correctly on wikis whose content language supports variant conversion.
    ///
    /// Both modern token parameters and the legacy <c>intoken</c> parameter are
    /// included for compatibility with older MediaWiki versions.
    /// </remarks>
    private static Dictionary<string, string> BuildOpenQuery(
        string title,
        bool resolveRedirects)
    {
        Dictionary<string, string> query = new()
    {
        { "action", "query" },
        { "converttitles", null },
        { "prop", "info|revisions" },
        { "meta", "tokens" },
        { "type", "csrf|watch|rollback"
     },

        // TODO (MediaWiki Compatibility):
        // Remove intoken once the minimum supported MediaWiki version no
        // longer requires the pre-1.24 token workflow.
        { "intoken", "edit|protect|delete|move|watch" },

        { "titles", title },
        { "inprop", "protection|watched|displaytitle" },
        { "rvprop", "content|timestamp" },
        { "curtimestamp", null }
    };

        query.AddIfTrue(
            resolveRedirects,
            "redirects",
            null);

        return query;
    }

    /// <summary>
    /// Initializes the current page state from a validated MediaWiki query
    /// response.
    /// </summary>
    /// <param name="document">
    /// The validated API response containing the page content and metadata.
    /// </param>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the response cannot be interpreted as valid page information.
    /// </exception>
    private void InitializeOpenedPage(XmlDocument document)
    {
        try
        {
            Page = new PageInfo(document);
            Action = "edit";
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    /// <summary>
    /// Determines whether the specified page exists.
    /// </summary>
    /// <param name="title">
    /// The title of the page to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the page exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> is null or empty.
    /// </exception>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the API response cannot be interpreted as valid page information.
    /// </exception>
    public bool PageExists(string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);

        var query = new Dictionary<string, string>
        {
            { "action", "query" },
            { "prop", "info" },
            { "titles", title }
        };

        string result = HttpGet(query);
        XmlDocument document = CheckForErrors(result, "query");

        try
        {
            return new PageInfo(document).Exists;
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    /// <summary>
    /// Saves the current page text to the wiki.
    /// </summary>
    /// <param name="pageText">
    /// The complete text to save to the page.
    /// </param>
    /// <param name="summary">
    /// The edit summary describing the changes.
    /// </param>
    /// <param name="minor">
    /// <see langword="true"/> to mark the edit as minor; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="watch">
    /// The watchlist behavior to apply to the edited page.
    /// </param>
    /// <param name="contentModel">
    /// The content model to assign to the page. The default is
    /// <c>wikitext</c>.
    /// </param>
    /// <returns>
    /// Information returned by the wiki after the page is saved.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an empty new page is submitted.
    /// </exception>
    /// <exception cref="ApiException">
    /// Thrown when the page was not opened for editing or no edit token is
    /// available.
    /// </exception>
    public SaveInfo Save(
        string pageText,
        string summary,
        bool minor,
        WatchOptions watch,
        string contentModel = "wikitext")
    {
        if (string.IsNullOrEmpty(pageText) && !Page.Exists)
        {
            throw new ArgumentException(
                "Can't save empty pages.",
                nameof(pageText));
        }

        if (Action != "edit")
        {
            throw new ApiException(
                this,
                "This page is not opened properly for editing.");
        }

        if (string.IsNullOrEmpty(Page.EditToken))
        {
            throw new ApiException(
                this,
                "An edit token is required to edit pages.");
        }

        pageText = Tools.ConvertFromLocalLineEndings(pageText);

        Dictionary<string, string> get =
            BuildSaveQueryParameters(minor, watch);

        Dictionary<string, string> post =
            BuildSavePostParameters(
                pageText,
                summary,
                contentModel);

        string result = HttpPost(
            get,
            post,
            ActionOptions.All);

        XmlDocument xml =
            CheckForErrors(result, "edit");

        Reset();

        return new SaveInfo(xml);
    }

    /// <summary>
    /// Builds the query parameters required by the edit API.
    /// </summary>
    /// <param name="minor">
    /// <see langword="true"/> to mark the edit as minor.
    /// </param>
    /// <param name="watch">
    /// The watchlist behavior to apply to the edited page.
    /// </param>
    /// <returns>
    /// The edit request query parameters.
    /// </returns>
    private Dictionary<string, string> BuildSaveQueryParameters(
        bool minor,
        WatchOptions watch)
       {
        var get = new Dictionary<string, string>
        {
            { "action", "edit" },
            { "title", Page.Title },
            { "watchlist", WatchOptionsToParam(watch) }
        };

        get.AddIfTrue(minor, "minor", null);
        get.AddIfTrue(User.IsBot, "bot", null);

        return get;
    }

    /// <summary>
    /// Builds the form parameters containing the page content and edit metadata.
    /// </summary>
    /// <param name="pageText">
    /// The normalized page text to save.
    /// </param>
    /// <param name="summary">
    /// The edit summary describing the changes.
    /// </param>
    /// <param name="contentModel">
    /// The content model to assign to the page.
    /// </param>
    /// <returns>
    /// The edit request form parameters.
    /// </returns>
    private Dictionary<string, string> BuildSavePostParameters(
        string pageText,
        string summary,
        string contentModel)
    {
        var post = new Dictionary<string, string>
        {
            // Parameter order intentionally matches MediaWiki expectations.
            // See Wikimedia Phabricator task T16210.
            { "md5", MD5(pageText) },
            { "summary", summary },
            { "basetimestamp", Page.Timestamp },
            { "text", pageText },
            { "starttimestamp", Page.TokenTimestamp }
        };

        post.AddIfTrue(
            Variables.TagEdits,
            "tags",
            "AWB");

        post.AddIfTrue(
            contentModel != "wikitext",
            "contentmodel",
            contentModel);

        post.Add(
            "token",
            Page.EditToken);

        return post;
    }

    /// <summary>
    /// Deletes the specified page using the supplied deletion reason.
    /// </summary>
    /// <param name="title">
    /// The title of the page to delete.
    /// </param>
    /// <param name="reason">
    /// The reason to record in the deletion log.
    /// </param>
    /// <remarks>
    /// This overload performs a standard deletion without requesting the page
    /// to be watched after the operation.
    /// </remarks>
    public void Delete(string title, string reason) =>
        Delete(title, reason, false);

    /// <summary>
    /// Deletes the specified page from the wiki.
    /// </summary>
    /// <param name="title">
    /// The title of the page to delete.
    /// </param>
    /// <param name="reason">
    /// The reason to record in the deletion log.
    /// </param>
    /// <param name="watch">
    /// <see langword="true"/> to add the deleted page to the watchlist;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> or <paramref name="reason"/> is empty.
    /// </exception>
    /// <exception cref="AbortedException">
    /// Thrown when the operation is aborted before the deletion request is sent.
    /// </exception>
    public void Delete(
        string title,
        string reason,
        bool watch)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(reason);

        Action = "delete";

        EnsureDeleteToken(title);

        if (Aborting)
            throw new AbortedException(this);

        Dictionary<string, string> post =
            BuildDeletePostParameters(
                title,
                reason,
                watch);

        string result = HttpPost(
            new()
            {
            { "action", "delete" }
            },
            post,
            ActionOptions.All);

        CheckForErrors(result);

        Reset();
    }

    /// <summary>
    /// Ensures that a deletion token is available for the specified page.
    /// </summary>
    /// <param name="title">
    /// The title of the page that will be deleted.
    /// </param>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the token response does not contain the expected XML structure.
    /// </exception>
    private void EnsureDeleteToken(string title)
    {
        if (!string.IsNullOrEmpty(Page.DeleteToken))
            return;

        string result = HttpGet(
            new()
            {
                // TODO (MediaWiki Compatibility):
                // Remove the pre-1.24 intoken compatibility path once the minimum supported
                // MediaWiki version no longer requires it.
                { "action", "query" },
                { "prop", "info" },
                { "meta", "tokens" },       // MediaWiki 1.24+
                { "type", "csrf" },
                { "intoken", "delete" },    // Pre-1.24 compatibility
                { "titles", title }
            },
            ActionOptions.All);

        XmlDocument document = CheckForErrors(result);

        try
        {
            Page.DeleteToken = GetDeleteToken(document);
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    /// <summary>
    /// Extracts a deletion token from a MediaWiki token response.
    /// </summary>
    /// <param name="document">
    /// The validated API response document.
    /// </param>
    /// <returns>
    /// The deletion token returned by the wiki.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when the response contains neither a modern token element nor a
    /// legacy page element.
    /// </exception>
    private static string GetDeleteToken(XmlDocument document)
    {
        // TODO (MediaWiki Compatibility):
        // Remove the pre-1.24 intoken compatibility path once the minimum supported
        // MediaWiki version no longer requires it.
        //
        // MediaWiki 1.24+ returns the CSRF token in <tokens>.
        // Older compatibility responses can return deletetoken on <page>.
        XmlNode tokenSource =
            document.SelectSingleNode("/api/query/tokens") ??
            document.SelectSingleNode("/api/query/pages/page");

        if (tokenSource == null)
        {
            throw new InvalidOperationException(
                "The API response does not contain a <tokens> or <page> element.");
        }

        string tokenAttribute =
            tokenSource.Name == "tokens"
                ? "csrftoken"
                : "deletetoken";

        return XmlResponseHelpers.RequireAttributeValue(
            tokenSource,
            tokenAttribute);
    }

    /// <summary>
    /// Builds the form parameters required by the delete API.
    /// </summary>
    /// <param name="title">
    /// The title of the page to delete.
    /// </param>
    /// <param name="reason">
    /// The reason to record in the deletion log.
    /// </param>
    /// <param name="watch">
    /// <see langword="true"/> to add the page to the watchlist.
    /// </param>
    /// <returns>
    /// The deletion request form parameters.
    /// </returns>
    private Dictionary<string, string> BuildDeletePostParameters(
        string title,
        string reason,
        bool watch)
    {
        Dictionary<string, string> post = new()
    {
        { "title", title },
        { "token", Page.DeleteToken },
        { "reason", reason }
    };

        // TODO (MediaWiki Compatibility):
        // Review whether the bot parameter should be restored for authenticated bot accounts.
        post.AddIfTrue(watch, "watch", null);

        return post;
    }

    /// <summary>
    /// Protects the specified page using a time-span expiry.
    /// </summary>
    /// <param name="title">The title of the page to protect.</param>
    /// <param name="reason">The reason for applying protection.</param>
    /// <param name="expiry">The duration of the protection.</param>
    /// <param name="edit">The required protection level for editing.</param>
    /// <param name="move">The required protection level for moving.</param>
    public void Protect(
        string title,
        string reason,
        TimeSpan expiry,
        string edit,
        string move)
    {
        Protect(
            title,
            reason,
            expiry.ToString(),
            edit,
            move,
            false,
            false);
    }

    /// <summary>
    /// Protects the specified page using the supplied expiry value.
    /// </summary>
    /// <param name="title">The title of the page to protect.</param>
    /// <param name="reason">The reason for applying protection.</param>
    /// <param name="expiry">
    /// The protection expiry value accepted by the MediaWiki API.
    /// </param>
    /// <param name="edit">The required protection level for editing.</param>
    /// <param name="move">The required protection level for moving.</param>
    public void Protect(
        string title,
        string reason,
        string expiry,
        string edit,
        string move)
    {
        Protect(
            title,
            reason,
            expiry,
            edit,
            move,
            false,
            false);
    }

    /// <summary>
    /// Protects the specified page using a time-span expiry and the specified
    /// cascading and watchlist options.
    /// </summary>
    /// <param name="title">The title of the page to protect.</param>
    /// <param name="reason">The reason for applying protection.</param>
    /// <param name="expiry">The duration of the protection.</param>
    /// <param name="edit">The required protection level for editing.</param>
    /// <param name="move">The required protection level for moving.</param>
    /// <param name="cascade">
    /// <see langword="true"/> to apply cascading protection; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="watch">
    /// <see langword="true"/> to add the page to the watchlist; otherwise,
    /// <see langword="false"/>.
    /// </param>
    public void Protect(
        string title,
        string reason,
        TimeSpan expiry,
        string edit,
        string move,
        bool cascade,
        bool watch)
    {
        Protect(
            title,
            reason,
            expiry.ToString(),
            edit,
            move,
            cascade,
            watch);
    }

    /// <summary>
    /// Protects the specified page using the supplied protection levels and options.
    /// </summary>
    /// <param name="title">The title of the page to protect.</param>
    /// <param name="reason">The reason for applying protection.</param>
    /// <param name="expiry">
    /// The protection expiry value accepted by the MediaWiki API.
    /// </param>
    /// <param name="edit">The required protection level for editing.</param>
    /// <param name="move">The required protection level for moving.</param>
    /// <param name="cascade">
    /// <see langword="true"/> to apply cascading protection; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="watch">
    /// <see langword="true"/> to add the page to the watchlist; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> or <paramref name="reason"/> is null
    /// or empty.
    /// </exception>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the protection token cannot be read from the API response.
    /// </exception>
    /// <exception cref="AbortedException">
    /// Thrown when the operation has been aborted.
    /// </exception>
    public void Protect(
        string title,
        string reason,
        string expiry,
        string edit,
        string move,
        bool cascade,
        bool watch)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(reason);

        Action = "protect";

        EnsureProtectToken(title);

        if (Aborting)
            throw new AbortedException(this);

        string protections = BuildProtectionLevels(edit, move);
        string expiryvalue = BuildProtectionExpiry(expiry);

        Dictionary<string, string> post = BuildProtectPostData(
            title,
            reason,
            expiry,
            expiryvalue,
            protections,
            cascade,
            watch);

        Dictionary<string, string> get = new()
        {
            { "action", "protect" }
        };

        string result = HttpPost(
            get,
            post,
            ActionOptions.All);

        CheckForErrors(result);

        Reset();
    }

    /// <summary>
    /// Ensures that a protection token is available for the specified page.
    /// </summary>
    /// <param name="title">The title of the page being protected.</param>
    private void EnsureProtectToken(string title)
    {
        if (!string.IsNullOrEmpty(Page.ProtectToken))
            return;

        // TODO (MediaWiki Compatibility):
        // Remove the pre-1.24 intoken compatibility path once the minimum supported
        // MediaWiki version no longer requires it.
        string result = HttpGet(
            new()
            {
                { "action", "query" },
                { "prop", "info" },
                { "meta", "tokens" },       // MediaWiki 1.24+
                { "type", "csrf" },
                { "intoken", "protect" },   // Pre-1.24 compatibility
                { "titles", title }
            },
            ActionOptions.All);

        XmlDocument document = CheckForErrors(result);

        try
        {
            Page.ProtectToken = GetProtectToken(document);
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    /// <summary>
    /// Reads a protection token from a MediaWiki API response.
    /// </summary>
    /// <param name="document">The API response document.</param>
    /// <returns>The protection token returned by the API.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response does not contain a <c>tokens</c> or
    /// <c>page</c> element.
    /// </exception>
    private static string GetProtectToken(XmlDocument document)
    {
        // MediaWiki 1.24+ returns the CSRF token in <tokens>.
        // Older compatibility responses return protecttoken on <page>.
        XmlNode tokenSource =
            document.SelectSingleNode("/api/query/tokens") ??
            document.SelectSingleNode("/api/query/pages/page");

        if (tokenSource == null)
        {
            throw new InvalidOperationException(
                "Cannot find a <tokens> or <page> element in the API response.");
        }

        string tokenAttribute =
            tokenSource.Name == "tokens"
                ? "csrftoken"
                : "protecttoken";

        return XmlResponseHelpers.RequireAttributeValue(
            tokenSource,
            tokenAttribute);
    }

    /// <summary>Builds the protection-level value for an existing or nonexistent page.</summary>
    /// <param name="edit">The edit or creation protection level.</param>
    /// <param name="move">The move protection level.</param>
    /// <returns>The protection-level string accepted by the MediaWiki API.</returns>
    private string BuildProtectionLevels(string edit, string move)
    {
        // Protecting a nonexistent page, commonly called salting, requires only
        // create protection.
        return Page.Exists
            ? $"edit={edit}|move={move}"
            : $"create={edit}";
    }

    /// <summary>
    /// Builds the protection-expiry value for an existing or nonexistent page.
    /// </summary>
    /// <param name="expiry">The requested expiry value.</param>
    /// <returns>The expiry value accepted by MediaWiki.</returns>
    private string BuildProtectionExpiry(string expiry)
    {
        if (string.IsNullOrEmpty(expiry))
            return string.Empty;

        return Page.Exists
            ? $"{expiry}|{expiry}"
            : expiry;
    }

    /// <summary>Builds the POST parameters required by the protect API.</summary>
    /// <param name="title">The page to protect.</param>
    /// <returns>The POST parameters for the protection request.</returns>
    private Dictionary<string, string> BuildProtectPostData(
        string title,
        string reason,
        string expiry,
        string expiryvalue,
        string protections,
        bool cascade,
        bool watch)
    {
        Dictionary<string, string> post = new()
    {
        { "title", title },
        { "token", Page.ProtectToken },
        { "reason", reason },
        { "protections", protections }
    };

        post.AddIfTrue(
            !string.IsNullOrEmpty(expiry),
            "expiry",
            expiryvalue);

        post.AddIfTrue(cascade, "cascade", null);
        post.AddIfTrue(watch, "watch", null);

        return post;
    }

    /// <summary>
    /// Moves a page using the default move options.
    /// </summary>
    /// <param name="title">The title of the page to move.</param>
    /// <param name="newTitle">The new title for the page.</param>
    /// <param name="reason">The reason for the move.</param>
    public void Move(
        string title,
        string newTitle,
        string reason)
    {
        Move(
            title,
            newTitle,
            reason,
            true,
            false,
            false);
    }

    /// <summary>
    /// Moves a page using the specified talk-page and redirect options.
    /// </summary>
    /// <param name="title">The title of the page to move.</param>
    /// <param name="newTitle">The new title for the page.</param>
    /// <param name="reason">The reason for the move.</param>
    /// <param name="moveTalk">
    /// <see langword="true"/> to move the associated talk page; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="noRedirect">
    /// <see langword="true"/> to suppress creation of a redirect; otherwise,
    /// <see langword="false"/>.
    /// </param>
    public void Move(
        string title,
        string newTitle,
        string reason,
        bool moveTalk,
        bool noRedirect)
    {
        Move(
            title,
            newTitle,
            reason,
            moveTalk,
            noRedirect,
            false);
    }

    /// <summary>
    /// Moves the specified page to a new title.
    /// </summary>
    /// <param name="title">The current title of the page.</param>
    /// <param name="newTitle">The destination title for the page.</param>
    /// <param name="reason">The reason for moving the page.</param>
    /// <param name="moveTalk">
    /// <see langword="true"/> to move the associated talk page; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="noRedirect">
    /// <see langword="true"/> to suppress creation of a redirect; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="watch">
    /// <see langword="true"/> to add the destination page to the watchlist;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when a required argument is null or empty, the target title is
    /// invalid, or the source and target titles are the same.
    /// </exception>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the move token cannot be read from the API response.
    /// </exception>
    /// <exception cref="AbortedException">
    /// Thrown when the operation has been aborted.
    /// </exception>
    public void Move(
        string title,
        string newTitle,
        string reason,
        bool moveTalk,
        bool noRedirect,
        bool watch)
    {
        ValidateMoveArguments(title, newTitle, reason);

        Action = "move";

        EnsureMoveToken(title, newTitle);

        if (Aborting)
            throw new AbortedException(this);

        Dictionary<string, string> post = BuildMovePostData(
            title,
            newTitle,
            reason,
            moveTalk,
            noRedirect,
            watch);

        Dictionary<string, string> get = new()
            {
                { "action", "move" }
            };

        string result = HttpPost(
            get,
            post,
            ActionOptions.All);

        CheckForErrors(result, "move");

        Reset();
    }

    /// <summary>
    /// Validates the arguments required to move a page.
    /// </summary>
    /// <param name="title">
    /// The current title of the page.
    /// </param>
    /// <param name="newTitle">
    /// The destination title for the page.
    /// </param>
    /// <param name="reason">
    /// The reason to record in the move log.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when a required argument is empty or the source and destination
    /// titles are identical.
    /// </exception>
    private static void ValidateMoveArguments(
        string title,
        string newTitle,
        string reason)
    {
        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException(
                "Page title required",
                nameof(title));
        }

        if (string.IsNullOrEmpty(newTitle))
        {
            throw new ArgumentException(
                "Target page title required",
                nameof(newTitle));
        }

        if (string.IsNullOrEmpty(reason))
        {
            throw new ArgumentException(
                "Page rename reason required",
                nameof(reason));
        }

        if (title == newTitle)
        {
            throw new ArgumentException(
                "Page cannot be moved to the same title",
                nameof(newTitle));
        }
    }

    /// <summary>
    /// Ensures that a move token is available for the specified page and validates
    /// that the requested destination can be used.
    /// </summary>
    /// <param name="title">
    /// The current title of the page.
    /// </param>
    /// <param name="newTitle">
    /// The proposed destination title.
    /// </param>
    /// <exception cref="ApiException">
    /// Thrown when the API reports that the move target is invalid.
    /// </exception>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the token response does not contain the expected XML structure.
    /// </exception>
    private void EnsureMoveToken(
        string title,
        string newTitle)
    {
        if (!string.IsNullOrEmpty(Page.MoveToken))
            return;

        // TODO (MediaWiki Compatibility):
        // Remove the pre-1.24 intoken compatibility path once the minimum supported
        // MediaWiki version no longer requires it.
        string result = HttpGet(
            new()
            {
                { "action", "query" },
                { "prop", "info" },
                { "meta", "tokens" },      // MediaWiki 1.24+
                { "type", "csrf" },
                { "intoken", "move" },     // Pre-1.24 compatibility
                { "titles", $"{title}|{newTitle}" }
            },
            ActionOptions.All);

        XmlDocument document =
            CheckForErrors(result, "query");

        try
        {
            ValidateMoveTarget(document, newTitle);

            Page.MoveToken =
                GetMoveToken(document);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    /// <summary>
    /// Verifies that the proposed move target was accepted by the API.
    /// </summary>
    /// <param name="document">
    /// The validated API response containing information about the requested move.
    /// </param>
    /// <exception cref="ApiException">
    /// Thrown when the API reports that the destination title is invalid.
    /// </exception>
    private void ValidateMoveTarget(
        XmlDocument document,
        string newTitle)
    {
        XmlNode invalidPage =
            document.SelectSingleNode("/api/query/pages/page[@invalid]");

        if (invalidPage == null)
            return;

        throw new ApiException(
            this,
            "invalidnewtitle",
            new ArgumentException(
                "Target page invalid",
                nameof(newTitle)));
    }

    /// <summary>
    /// Reads the move token from a MediaWiki API response.
    /// </summary>
    /// <param name="document">
    /// The validated API response containing the move token.
    /// </param>
    /// <returns>
    /// The CSRF or legacy move token returned by the wiki.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response does not contain the expected page element.
    /// </exception>
    private static string GetMoveToken(XmlDocument document)
    {
        XmlNode sourcePage =
            document.SelectSingleNode("/api/query/pages/page");

        if (sourcePage == null)
        {
            throw new InvalidOperationException(
                "Cannot find a <page> element in the API response.");
        }

        // MediaWiki 1.24+ returns the CSRF token in <tokens>.
        // Older compatibility responses return movetoken on <page>.
        XmlNode tokenSource =
            document.SelectSingleNode("/api/query/tokens") ??
            sourcePage;

        string tokenAttribute =
            tokenSource.Name == "tokens"
                ? "csrftoken"
                : "movetoken";

        return XmlResponseHelpers.RequireAttributeValue(
            tokenSource,
            tokenAttribute);
    }

    /// <summary>
    /// Builds the POST parameters required for a page move request.
    /// </summary>
    /// <param name="title">
    /// The current title of the page.
    /// </param>
    /// <param name="newTitle">
    /// The destination title for the page.
    /// </param>
    /// <param name="reason">
    /// The reason to record in the move log.
    /// </param>
    /// <param name="moveTalk">
    /// <see langword="true"/> to move the associated talk page when possible.
    /// </param>
    /// <param name="noRedirect">
    /// <see langword="true"/> to suppress creation of a redirect from the
    /// original title.
    /// </param>
    /// <param name="watch">
    /// <see langword="true"/> to add the moved page to the watchlist.
    /// </param>
    /// <returns>
    /// The form parameters required by the MediaWiki move API.
    /// </returns>
    private Dictionary<string, string> BuildMovePostData(
        string title,
        string newTitle,
        string reason,
        bool moveTalk,
        bool noRedirect,
        bool watch)
    {
        Dictionary<string, string> post = new()
    {
        { "from", title },
        { "to", newTitle },
        { "token", Page.MoveToken },
        { "reason", reason },

    // Required by the MediaWiki API even when no protection changes are
    // requested.
    //
    // TODO (MediaWiki Compatibility):
    // Verify whether this parameter remains necessary on supported versions.
    { "protections", string.Empty }
};

        post.AddIfTrue(moveTalk, "movetalk", null);
        post.AddIfTrue(noRedirect, "noredirect", null);
        post.AddIfTrue(watch, "watch", null);

        return post;
    }
    #endregion

    #region Query Api

    /// <summary>
    /// Executes a MediaWiki query request and returns the raw XML response.
    /// </summary>
    /// <param name="queryParameters">
    /// The query parameters to append to the API request.
    /// </param>
    /// <returns>
    /// The raw XML response returned by the MediaWiki API.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="queryParameters"/> is null or empty.
    /// </exception>
    /// <exception cref="ApiException">
    /// Thrown when the API response contains an error.
    /// </exception>
    public string QueryApi(string queryParameters)
    {
        ArgumentException.ThrowIfNullOrEmpty(queryParameters);

        // TODO (API Request Modernization):
        // Replace raw query-string parameters with structured parameter collections
        // so encoding is handled consistently.
        string result = HttpGet(
            $"{ApiURL}?action=query&format=xml&{queryParameters}");

        CheckForErrors(result, "query");

        return result;
    }

    /// <summary>
    /// Executes a MediaWiki query request and returns the raw JSON response.
    /// </summary>
    /// <param name="queryParameters">
    /// The query parameters to append to the API request.
    /// </param>
    /// <returns>
    /// The raw JSON response returned by the MediaWiki API.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="queryParameters"/> is null or empty.
    /// </exception>
    /// <remarks>
    /// JSON API errors are not currently validated. The raw response is returned
    /// unchanged to preserve existing behavior.
    /// </remarks>
    public string QueryApiJson(string queryParameters)
    {
        ArgumentException.ThrowIfNullOrEmpty(queryParameters);

        // TODO (API Request Modernization):
        // Replace raw query-string parameters with structured parameter collections
        // so encoding is handled consistently.
        return HttpGet(
            $"{ApiURL}?action=query&format=json&{queryParameters}");
    }

    #endregion

    /// <summary>
    /// Executes a MediaWiki parse request and returns the raw XML response.
    /// </summary>
    /// <param name="queryParameters">
    /// The parse parameters to send in the POST body.
    /// </param>
    /// <returns>
    /// The raw XML response returned by the MediaWiki API.
    /// </returns>
    /// <exception cref="ApiException">
    /// Thrown when the API response contains an error.
    /// </exception>
    public string ParseApi(Dictionary<string, string> queryParameters)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        // TODO (HTTP Modernization):
        // Decide whether this generic API method should use the configured
        // maxlag policy. Raw query-string methods currently bypass ActionOptions.
        string result = HttpPost(
            new()
            {
                { "action", "parse" },
                { "format", "xml" },
                { "prop", "text|displaytitle|langlinks|categories" }
            },
            queryParameters);

        CheckForErrors(result, "parse");

        return result;
    }

    #region Wikitext operations

    // TODO: Review whether URL expansion should use HTML parsing instead of
    // exact string replacement when the preview pipeline is modernized.
    /// <summary>
    /// Converts wiki-relative and protocol-relative resource URLs in rendered HTML
    /// to absolute HTTPS URLs.
    /// </summary>
    /// <param name="html">
    /// The rendered HTML containing relative links and resource references.
    /// </param>
    /// <returns>
    /// The HTML with supported relative URLs expanded.
    /// </returns>
    private string ExpandRelativeUrls(string html)
    {
        // Wiki article links.
        html = html.Replace(
            @" href=""/wiki/",
            $@" href=""{Server}/wiki/");

        // Relative links to wiki resources such as stylesheets and scripts.
        html = html.Replace(
            @" href=""/w/",
            $@" href=""{Server}/w/");

        // Protocol-relative links.
        html = html.Replace(
            @" href=""//",
            @" href=""https://");

        return html.Replace(
            @" src=""//",
            @" src=""https://");
    }

    /// <summary>
    /// Matches conditional comments, embedded style blocks, and stylesheet links
    /// that must be extracted from rendered wiki HTML.
    /// </summary>
    /// <remarks>
    /// JavaScript extraction is currently disabled even though the historical field
    /// name still refers to both CSS and JavaScript.
    /// </remarks>
    private static readonly Regex ExtractCssAndJs = new(
        @"("
        + @"<!--\[if .*?-->"
        + @"|<style\b.*?>.*?</style>"
        + @"|<link rel=""stylesheet"".*?/\s?>"
        // + @"|<script type=""text/javascript"".*?</script>"
        + ")",
        RegexOptions.Singleline |
        RegexOptions.Compiled);

    /// <summary>
    /// Loads and caches the wiki HTML header resources required to render
    /// accurate article previews.
    /// </summary>
    /// <remarks>
    /// The headers are downloaded only once per session and reused for subsequent
    /// preview generation.
    /// </remarks>
    private void EnsureHtmlHeadersLoaded()
    {
        if (!string.IsNullOrEmpty(HtmlHeaders)) return;

        string result = HttpGet(
            new Dictionary<string, string>
            {
                {"action", "parse"},
                {"prop", "headhtml"},
                {"title", "a"},
                {"text", "a"}
            },
            ActionOptions.None
            );

        result = Tools.StringBetween(Tools.UnescapeXML(result), "<head>", "</head>");
        StringBuilder extracted = new(2048);

        foreach (Match m in ExtractCssAndJs.Matches(result))
        {
            extracted.Append(m.Value);
            extracted.AppendLine();
        }

        HtmlHeaders = ExpandRelativeUrls(extracted.ToString());

        /*
         * T117870: The legacy WinForms WebBrowser rendering engine may apply the
         * browser-default italic style to <cite> elements even when MediaWiki's
         * styles are expected to override it. Add an explicit citation-class rule
         * so previews match the rendered wiki page more closely.
         * 
         * TODO (.NET modernization): Re-evaluate this workaround after the preview
         * pipeline has fully transitioned away from the legacy WinForms WebBrowser.
         * WebView2 or a future preview renderer may no longer require this override.
         */
        HtmlHeaders += @" <style> .citation { font-style: normal; } </style>";
    }

    /// <summary>
    /// Generates rendered preview HTML for the supplied page text.
    /// </summary>
    /// <param name="title">
    /// The title used as the parsing context.
    /// </param>
    /// <param name="text">
    /// The wiki text to render.
    /// </param>
    /// <returns>
    /// The rendered preview HTML with parse warnings and expanded resource URLs.
    /// </returns>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the API response does not contain the expected XML structure.
    /// </exception>
    public string Preview(string title, string text)
    {
        EnsureHtmlHeadersLoaded();

        // TODO (Preview Modernization):
        // Re-evaluate whether HtmlHeaders must be loaded before every preview request
        // once the preview renderer has fully transitioned away from the legacy
        // WebBrowser implementation.
        string result = RequestPreview(title, text);
        XmlDocument document = CheckForErrors(result, "parse");

        try
        {
            // TODO (Preview Modernization):
            // Consider moving the preview warning styling into the shared preview
            // stylesheet once the preview HTML pipeline is modernized.
            string previewHtml = BuildPreviewHtml(document);

            return ExpandRelativeUrls(previewHtml);
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    /// <summary>
    /// Requests rendered preview content from the MediaWiki parse API.
    /// </summary>
    /// <param name="title">
    /// The title used as the parsing context.
    /// </param>
    /// <param name="text">
    /// The wiki text to render.
    /// </param>
    /// <returns>
    /// The raw API response.
    /// </returns>
    private string RequestPreview(
        string title,
        string text) =>
        HttpPost(
            new()
            {
            { "action", "parse" },
            { "prop", "text|parsewarnings" }
            },
            new()
            {
            { "title", title },
            { "text", text },
            { "pst", null },
            { "disablelimitreport", null }
            });

    /// <summary>
    /// Extracts the rendered preview HTML and prepends any parse warnings returned
    /// by the API.
    /// </summary>
    /// <param name="document">
    /// The validated parse API response.
    /// </param>
    /// <returns>
    /// The rendered preview HTML, including any formatted parse warnings.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response does not contain the expected text element.
    /// </exception>
    private static string BuildPreviewHtml(XmlDocument document)
    {
        XmlNode textNode =
            document.SelectSingleNode("/api/parse/text");

        if (textNode == null)
        {
            throw new InvalidOperationException(
                "Cannot find <text> element");
        }

        string previewHtml = textNode.InnerText;

        XmlNodeList warningNodes =
            document.SelectNodes("/api/parse/parsewarnings/pw");

        if (warningNodes == null || warningNodes.Count == 0)
            return previewHtml;

        StringBuilder warnings = new();

        // TODO (Preview Modernization):
        // Review how parse warnings are rendered. The current implementation inserts
        // the API response directly into the preview HTML and relies on implicit HTML
        // normalization by appending opening <p> tags. Verify that the warning content
        // and generated markup are still appropriate after the preview renderer is
        // modernized.
        foreach (XmlNode warningNode in warningNodes)
        {
            warnings.Append(warningNode.InnerText);
            warnings.Append("<p>");
        }

        return
            @"<div class=""previewnote"" style=""color:#d33"">" +
            warnings +
            "</div>" +
            previewHtml;
    }

    /// <summary>
    /// Rolls back the most recent edits made by the specified user.
    /// </summary>
    /// <param name="title">
    /// The title of the page to roll back.
    /// </param>
    /// <param name="user">
    /// The username whose edits should be reverted.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> or <paramref name="user"/> is empty.
    /// </exception>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the rollback token response does not contain the expected XML
    /// structure.
    /// </exception>
    public void Rollback(
        string title,
        string user)
    {
        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException(
                "Page name required",
                nameof(title));
        }

        if (string.IsNullOrEmpty(user))
        {
            throw new ArgumentException(
                "User name required",
                nameof(user));
        }

        EnsureRollbackToken(title);

        string result = HttpPost(
            new()
            {
            { "action", "rollback" }
            },
            new()
            {
            { "title", title },
            { "user", user },
            { "token", Page.RollbackToken }
            });

        CheckForErrors(result, "rollback");
    }

    /// <summary>
    /// Ensures that a rollback token is available for the specified page.
    /// </summary>
    /// <param name="title">
    /// The title of the page to roll back.
    /// </param>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the token response does not contain the expected XML structure.
    /// </exception>
    private void EnsureRollbackToken(string title)
    {
        if (!string.IsNullOrEmpty(Page.RollbackToken))
            return;

        // TODO (MediaWiki Compatibility):
        // Re-evaluate whether the legacy rollback token request
        // (rvtoken=rollback) is still required once AWB's minimum supported
        // MediaWiki version is finalized. Modern MediaWiki versions obtain
        // rollback tokens through the CSRF token API.
        string result = HttpGet(
            new()
            {
            { "action", "query" },
            { "prop", "revisions" },
            { "meta", "tokens" },          // MediaWiki 1.24+
            { "type", "rollback" },
            { "rvtoken", "rollback" },     // Pre-1.24 compatibility
            { "titles", title }
            },
            ActionOptions.All);

        XmlDocument document =
            CheckForErrors(result, "query");

        try
        {
            Page.RollbackToken =
                GetRollbackToken(document);
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    /// <summary>
    /// Reads the rollback token from a MediaWiki API response.
    /// </summary>
    /// <param name="document">
    /// The validated API response containing the rollback token.
    /// </param>
    /// <returns>
    /// The rollback token returned by the wiki.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response contains neither a modern token element nor a
    /// legacy page element.
    /// </exception>
    private static string GetRollbackToken(
        XmlDocument document)
    {
        // MediaWiki 1.24+ returns the rollback token in <tokens>.
        // Older compatibility responses can return it on <page>.
        XmlNode tokenSource =
            document.SelectSingleNode("/api/query/tokens") ??
            document.SelectSingleNode("/api/query/pages/page");

        if (tokenSource == null)
        {
            throw new InvalidOperationException(
                "Cannot find <tokens> or <page> element in the API response.");
        }

        return XmlResponseHelpers.RequireAttributeValue(
            tokenSource,
            "rollbacktoken");
    }

    // TODO (MediaWiki Compatibility):
    // Verify whether the expandtemplates API supports additional parameters that
    // should be exposed once AWB's supported MediaWiki version baseline is
    // finalized.
    /// <summary>
    /// Expands templates within the supplied wiki text using the MediaWiki API.
    /// </summary>
    /// <param name="title">
    /// The page title used as the expansion context.
    /// </param>
    /// <param name="text">
    /// The wiki text whose templates should be expanded.
    /// </param>
    /// <returns>
    /// The expanded wiki text returned by the API.
    /// </returns>
    /// <exception cref="BrokenXmlException">
    /// Thrown when the API response does not contain the expected XML structure.
    /// </exception>
    public string ExpandTemplates(string title, string text)
    {
        string result = HttpPost(
            new()
            {
            { "action", "expandtemplates" },
            { "prop", "wikitext" }
            },
            new()
            {
            { "title", title },
            { "text", text }
            });

        XmlDocument document =
            CheckForErrors(result, "expandtemplates");

        try
        {
            XmlNode expandedTextNode =
                document.SelectSingleNode("/api/expandtemplates");

            if (expandedTextNode == null)
            {
                throw new InvalidOperationException(
                    "Cannot find <expandtemplates> element.");
            }

            return expandedTextNode.InnerText;
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    #endregion

    #region Error handling

    /// <summary>
    /// Checks the XML returned by the server for API errors.
    /// </summary>
    /// <param name="xml">
    /// The XML returned by the server.
    /// </param>
    /// <returns>
    /// A validated <see cref="XmlDocument"/>.
    /// </returns>
    private XmlDocument CheckForErrors(string xml) =>
        CheckForErrors(xml, null);

    private static readonly Regex MaxLag = new Regex(@": (\d+(?:\.\d+)?) seconds lagged",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses and validates XML returned by the server, translating API errors,
    /// warnings, and invalid response states into the appropriate exceptions.
    /// </summary>
    /// <param name="xml">
    /// The XML returned by the server.
    /// </param>
    /// <param name="action">
    /// The API action expected in the response, or <see langword="null"/> when
    /// action-specific validation is not required.
    /// </param>
    /// <returns>
    /// The validated API response document.
    /// </returns>
    private XmlDocument CheckForErrors(
        string xml,
        string action)
    {
        if (string.IsNullOrEmpty(xml))
            throw new ApiBlankException(this);

        XmlDocument document = ParseApiXmlDocument(xml);

        ThrowIfApiError(document, xml);
        ProcessApiWarnings(document);

        if (string.IsNullOrEmpty(action))
            return CompleteSuccessfulResponseValidation(document, action);

        ValidateActionResponse(document, action, xml);

        return CompleteSuccessfulResponseValidation(document, action);
    }

    /// <summary>
    /// Parses an API response into an XML document.
    /// </summary>
    /// <param name="xml">
    /// The raw XML returned by the server.
    /// </param>
    /// <returns>
    /// The parsed API response document.
    /// </returns>
    /// <exception cref="ApiXmlException">
    /// Thrown when the server response is not valid XML.
    /// </exception>
    private XmlDocument ParseApiXmlDocument(string xml)
    {
        try
        {
            return LoadApiXmlDocument(xml);
        }
        catch (XmlException ex)
        {
            Tools.WriteDebug(
                "ApiEdit::CheckForErrors",
                xml);

            string postParams = lastPostParameters == null
                ? string.Empty
                : BuildQuery(lastPostParameters);

            throw new ApiXmlException(
                this,
                ex,
                lastGetUrl,
                postParams,
                xml);
        }
    }

    /// <summary>
    /// Examines the API response for an error element and throws the corresponding
    /// typed exception when one is present.
    /// </summary>
    /// <param name="document">
    /// The parsed API response.
    /// </param>
    /// <param name="xml">
    /// The raw XML response, used to extract additional error details.
    /// </param>
    private void ThrowIfApiError(
        XmlDocument document,
        string xml)
    {
        XmlNodeList errors =
            document.GetElementsByTagName("error");

        if (errors.Count == 0)
            return;

        XmlNode error = errors[0];

        string errorCode =
            XmlResponseHelpers.RequireAttributeValue(
                error,
                "code");

        string errorMessage =
            XmlResponseHelpers.RequireAttributeValue(
                error,
                "info");

        ThrowApiError(
            error,
            errorCode,
            errorMessage,
            xml);
    }

    /// <summary>
    /// Translates an API error code into the appropriate exception.
    /// </summary>
    /// <param name="error">
    /// The API error element.
    /// </param>
    /// <param name="errorCode">
    /// The error code returned by MediaWiki.
    /// </param>
    /// <param name="errorMessage">
    /// The accompanying error message.
    /// </param>
    /// <param name="xml">
    /// The raw XML response.
    /// </param>
    private void ThrowApiError(
        XmlNode error,
        string errorCode,
        string errorMessage,
        string xml)
    {
        switch (errorCode.ToLowerInvariant())
        {
            case "maxlag":
                double.TryParse(
                    MaxLag.Match(xml).Groups[1].Value,
                    out double maxlag);

                throw new MaxlagException(
                    this,
                    maxlag,
                    10);

            case "wrnotloggedin":
                throw new LoggedOffException(this);

            case "spamdetected":
            case "spamblacklist":
            case "spamprotectiontext":
                throw new SpamlistException(
                    this,
                    errorMessage);

            case "fileexists-sharedrepo-perm":
                throw new SharedRepoException(
                    this,
                    errorMessage);

            case "hookaborted":
                throw new MediaWikiSaysNoException(
                    this,
                    errorMessage);

            case "readonly":
                string readOnlyReason =
                    XmlResponseHelpers.RequireAttributeValue(
                        error,
                        "readonlyreason");

                throw new MediaWikiReadOnlyException(
                    this,
                    errorMessage +
                    "\r\n\r\nReason: " +
                    readOnlyReason);

            default:
                ThrowUnrecognizedApiError(
                    errorCode,
                    errorMessage);

                return;
        }
    }

    /// <summary>
    /// Handles API errors that do not have a dedicated switch case.
    /// </summary>
    /// <param name="errorCode">
    /// The error code returned by MediaWiki.
    /// </param>
    /// <param name="errorMessage">
    /// The accompanying error message.
    /// </param>
    private void ThrowUnrecognizedApiError(
        string errorCode,
        string errorMessage)
    {
        if (errorCode.Contains("disabled"))
        {
            throw new FeatureDisabledException(
                this,
                errorCode,
                errorMessage);
        }

        if (errorMessage == "Unknown error: \"tpt-target-page\"")
            throw new TranslationPageEditException(this);

        throw new ApiErrorException(
            this,
            errorCode,
            errorMessage);
    }

    /// <summary>
    /// Processes API warnings, updates compatibility settings, and writes warning
    /// details to the debug log.
    /// </summary>
    /// <param name="document">
    /// The parsed API response.
    /// </param>
    private void ProcessApiWarnings(XmlDocument document)
    {
        XmlNodeList warnings =
            document.GetElementsByTagName("warnings");

        if (warnings.Count == 0)
            return;

        XmlNode warningsNode = warnings.Item(0);

        if (warningsNode == null)
            return;

        StringBuilder warningBuilder = new();

        foreach (XmlNode childNode in warningsNode.ChildNodes)
        {
            ProcessApiWarning(childNode.InnerText);
            warningBuilder.AppendLine(childNode.InnerText);
        }

        if (warningBuilder.Length > 0)
        {
            Tools.WriteDebug(
                "ApiEdit::CheckForErrors warnings",
                warningBuilder.ToString());
        }
    }

    /// <summary>
    /// Applies compatibility changes indicated by an API warning.
    /// </summary>
    /// <param name="warning">
    /// The warning text returned by MediaWiki.
    /// </param>
    private void ProcessApiWarning(string warning)
    {
        // Contains is intentional because multiple warnings may be returned in a
        // single XML block.
        if (warning.Contains(
            "Unrecognized value for parameter 'meta': notifications"))
        {
            Variables.NotificationsEnabled = false;
        }
        else if (
            warning.Contains(
                "The parameter \"intoken\" has been deprecated.") ||
            warning.Contains(
                "Unrecognized parameter: intoken."))
        {
            UseInToken = false;
        }
    }

    /// <summary>
    /// Performs validation that depends on the expected API action.
    /// </summary>
    /// <param name="document">
    /// The parsed API response.
    /// </param>
    /// <param name="action">
    /// The expected API action.
    /// </param>
    /// <param name="xml">
    /// The raw XML response.
    /// </param>
    private void ValidateActionResponse(
        XmlDocument document,
        string action,
        string xml)
    {
        XmlElement api = document["api"];

        if (api == null)
            return;

        ThrowIfRedirectToSpecialPage(
            api,
            action);

        ThrowIfInvalidTitle(api);
        ThrowIfInterwikiRedirect(api);

        XmlElement actionElement = api[action];

        if (actionElement == null)
            return;

        ThrowIfAssertionFailed(actionElement);
        ThrowIfSpamBlacklisted(actionElement);
        ThrowIfCaptchaRequired(actionElement);
        ThrowIfActionFailed(
            actionElement,
            action,
            xml);
    }

    /// <summary>
    /// Rejects query redirects that target a special page.
    /// </summary>
    /// <param name="api">
    /// The root API response element.
    /// </param>
    /// <param name="action">
    /// The expected API action.
    /// </param>
    private void ThrowIfRedirectToSpecialPage(
        XmlElement api,
        string action)
    {
        if (action != "query")
            return;

        XmlNodeList redirects =
            api.GetElementsByTagName("r");

        if (redirects.Count == 0)
            return;

        // Workaround for https://phabricator.wikimedia.org/T41492
        string redirectTarget =
            XmlResponseHelpers.RequireAttributeValue(
                redirects[redirects.Count - 1],
                "to");

        if (Namespace.IsSpecial(
            Namespace.Determine(redirectTarget)))
        {
            throw new RedirectToSpecialPageException(this);
        }
    }

    /// <summary>
    /// Begins a scoped cancellation context for modern task-based ApiEdit callers.
    ///
    /// Existing synchronous callers do not enter this scope and therefore preserve
    /// their current behavior. AsyncApiEditModern enters this scope through
    /// ApiEditModernOperations so requests created during that operation can be
    /// aborted when the operation token is canceled.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token for the current modern operation.
    /// </param>
    /// <returns>
    /// An object that restores the previous cancellation scope when disposed.
    /// </returns>
    internal IDisposable BeginCancellationScope(
        CancellationToken cancellationToken)
    {
        return new CancellationScope(this, cancellationToken);
    }

    /// <summary>
    /// Gets the cancellation token associated with the active modern API operation.
    /// </summary>
    /// <returns>
    /// The active operation's cancellation token, or
    /// <see cref="CancellationToken.None"/> when no cancellation scope is active.
    /// </returns>
    private CancellationToken GetActiveCancellationToken()
    {
        lock (CancellationSyncRoot)
        {
            return CancellationScopeActive
                ? ActiveCancellationToken
                : CancellationToken.None;
        }
    }

    /// <summary>
    /// Registers cancellation of an HTTP request with the active modern operation.
    /// </summary>
    /// <param name="request">
    /// The request to abort when cancellation is requested.
    /// </param>
    /// <returns>
    /// A registration that must be disposed when the request completes, or
    /// <see langword="null"/> when no cancellable token is active.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when cancellation has already been requested.
    /// </exception>
    private IDisposable RegisterRequestCancellation(HttpWebRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CancellationToken cancellationToken = GetActiveCancellationToken();

        if (!cancellationToken.CanBeCanceled)
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        return cancellationToken.Register(
            () =>
            {
                try
                {
                    request.Abort();
                }
                catch (WebException)
                {
                    // Request.Abort should not normally throw WebException, but keep
                    // cancellation callbacks defensive so they never crash the caller.
                }
                catch (ObjectDisposedException)
                {
                    // The request may already have completed or been disposed.
                }
            });
    }

    /// <summary>
    /// Converts a canceled HTTP request into the appropriate cancellation exception.
    /// </summary>
    /// <param name="ex">The exception raised by the HTTP request.</param>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the active modern cancellation token requested cancellation.
    /// </exception>
    /// <exception cref="AbortedException">
    /// Thrown when the request was canceled by the legacy abort mechanism.
    /// </exception>
    private void ThrowIfModernRequestCancellation(WebException ex)
    {
        if (ex == null || ex.Status != WebExceptionStatus.RequestCanceled)
            return;

        CancellationToken cancellationToken = GetActiveCancellationToken();

        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);

        throw new AbortedException(this);
    }

    /// <summary>
    /// Temporarily associates a cancellation token with an <see cref="ApiEdit"/>
    /// operation and restores the previous cancellation state when disposed.
    /// </summary>
    private sealed class CancellationScope : IDisposable
    {
        private ApiEdit Editor;
        private readonly bool PreviousScopeActive;
        private readonly CancellationToken PreviousCancellationToken;

        /// <summary>
        /// Initializes a new cancellation scope.
        /// </summary>
        /// <param name="editor">
        /// The API editor whose cancellation state will be updated.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token to associate with the scope.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="editor"/> is <see langword="null"/>.
        /// </exception>
        public CancellationScope(
            ApiEdit editor,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(editor);

            Editor = editor;

            lock (editor.CancellationSyncRoot)
            {
                PreviousScopeActive = editor.CancellationScopeActive;
                PreviousCancellationToken = editor.ActiveCancellationToken;

                editor.CancellationScopeActive = true;
                editor.ActiveCancellationToken = cancellationToken;
            }
        }

        /// <summary>
        /// Restores the cancellation state that was active before this scope began.
        /// </summary>
        public void Dispose()
        {
            ApiEdit editor = Editor;

            if (editor == null)
                return;

            lock (editor.CancellationSyncRoot)
            {
                editor.CancellationScopeActive = PreviousScopeActive;
                editor.ActiveCancellationToken = PreviousCancellationToken;
            }

            Editor = null;
        }
    }

    /// <summary>
    /// Updates cached user state after a successful API response and stops
    /// processing when the response newly reports pending user messages.
    /// </summary>
    /// <param name="document">The validated API response document.</param>
    /// <param name="action">The API action that produced the response.</param>
    /// <returns>The same validated response document.</returns>
    private XmlDocument CompleteSuccessfulResponseValidation(
        XmlDocument document,
        string action)
    {
        bool previouslyHadMessages = User.HasMessages;

        User.Update(document);

        if (ShouldThrowForNewMessages(action, previouslyHadMessages))
            throw new NewMessagesException(this);

        return document;
    }

    /// <summary>
    /// Determines whether a successful API response has newly reported pending
    /// messages and should interrupt normal processing.
    ///
    /// Login and explicit userinfo responses update cached user state but do not
    /// interrupt the workflow with a NewMessagesException.
    /// </summary>
    /// <param name="action">The API action that produced the response.</param>
    /// <param name="previouslyHadMessages">
    /// Whether pending messages were already known before this response.
    /// </param>
    /// <returns>
    /// <c>true</c> when processing should stop because new messages were detected;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool ShouldThrowForNewMessages(
        string action,
        bool previouslyHadMessages)
    {
        if (!NewMessageThrows ||
            previouslyHadMessages ||
            !User.HasMessages)
        {
            return false;
        }

        return !string.Equals(
                   action,
                   "login",
                   StringComparison.OrdinalIgnoreCase)
               && !string.Equals(
                   action,
                   "clientlogin",
                   StringComparison.OrdinalIgnoreCase)
               && !string.Equals(
                   action,
                   "userinfo",
                   StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Converts a Boolean value to the numeric string representation expected by
    /// the MediaWiki API.
    /// </summary>
    /// <param name="value">
    /// The Boolean value to convert.
    /// </param>
    /// <returns>
    /// <c>"1"</c> when <paramref name="value"/> is <see langword="true"/>;
    /// otherwise, <c>"0"</c>.
    /// </returns>
    protected static string BoolToParam(bool value) =>
        value ? "1" : "0";

    /// <summary>
    /// Converts a <see cref="WatchOptions"/> value to the corresponding
    /// MediaWiki API parameter.
    /// </summary>
    /// <param name="watch">
    /// The watch option to convert.
    /// </param>
    /// <returns>
    /// The parameter value expected by the MediaWiki API.
    /// </returns>
    protected static string WatchOptionsToParam(WatchOptions watch) =>
        watch switch
        {
            WatchOptions.UsePreferences => "preferences",
            WatchOptions.Watch => "watch",
            WatchOptions.Unwatch => "unwatch",
            _ => "nochange"
        };

    /// <summary>
    /// Computes the MD5 sum of a string
    /// </summary>
    /// <param name="input">String to get MD5 sum of</param>
    /// <returns>MD5 sum</returns>
    protected static string MD5(string input)
    {
        return MD5(Encoding.UTF8.GetBytes(input));
    }

    /// <summary>
    /// Computes the MD5 sum of a byte array
    /// </summary>
    /// <param name="input">Byte array to get MD5 sum of</param>
    /// <returns>MD5 sum</returns>
    protected static string MD5(byte[] input)
    {
        var summer = System.Security.Cryptography.MD5.Create();
        StringBuilder sb = new StringBuilder(20);
        foreach (byte t in summer.ComputeHash(input))
        {
            sb.Append(t.ToString("x2"));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates XML reader settings suitable for MediaWiki API responses.
    ///
    /// DTD processing and external resource resolution are disabled because AWB
    /// does not require them when reading API responses.
    /// </summary>
    /// <returns>A new XML reader settings instance for one API response.</returns>
    private static XmlReaderSettings CreateSafeXmlReaderSettings()
    {
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
    }

    /// <summary>
    /// Loads a MediaWiki API response into an XML document using the shared
    /// safe XML reader settings.
    /// </summary>
    /// <param name="xml">The API response XML to load.</param>
    /// <returns>The parsed XML document.</returns>
    private static XmlDocument LoadApiXmlDocument(string xml)
    {
        XmlDocument document = new XmlDocument
        {
            XmlResolver = null
        };

        using (StringReader stringReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(
            stringReader,
            CreateSafeXmlReaderSettings()))
        {
            document.Load(reader);
        }

        return document;
    }

    /// <summary>
    /// Creates a forward-only XML reader for a MediaWiki API response using the
    /// shared safe XML reader settings.
    /// </summary>
    /// <param name="result">The API response XML to read.</param>
    /// <returns>An XML reader positioned before the response document.</returns>
    protected XmlReader CreateXmlReader(string result)
    {
        return XmlReader.Create(
            new StringReader(result),
            CreateSafeXmlReaderSettings());
    }

    /// <summary>
    /// Throws when the API response reports an invalid page title.
    /// </summary>
    /// <param name="api">
    /// The root API response element.
    /// </param>
    /// <exception cref="InvalidTitleException">
    /// Thrown when MediaWiki marks the returned page element as invalid.
    /// </exception>
    private void ThrowIfInvalidTitle(XmlElement api)
    {
        XmlNodeList pages =
            api.GetElementsByTagName("page");

        if (pages.Count == 0)
            return;

        XmlElement pageElement =
            pages[0] as XmlElement;

        if (pageElement == null ||
            !pageElement.HasAttribute("invalid"))
        {
            return;
        }

        string title =
            pageElement.GetAttribute("title");

        throw new InvalidTitleException(this, title);
    }

    /// <summary>
    /// Throws when the API response contains an interwiki redirect.
    /// </summary>
    /// <param name="api">
    /// The root API response element.
    /// </param>
    /// <exception cref="InterwikiException">
    /// Thrown when the requested title resolves to another wiki.
    /// </exception>
    private void ThrowIfInterwikiRedirect(XmlElement api)
    {
        if (api.GetElementsByTagName("interwiki").Count > 0)
            throw new InterwikiException(this);
    }

    /// <summary>
    /// Throws when MediaWiki reports that an API assertion failed.
    /// </summary>
    /// <param name="actionElement">
    /// The action-specific API response element.
    /// </param>
    /// <exception cref="LoggedOffException">
    /// Thrown when the failed assertion indicates that the user is no longer
    /// logged in.
    /// </exception>
    /// <exception cref="AssertionFailedException">
    /// Thrown for any other failed assertion.
    /// </exception>
    private void ThrowIfAssertionFailed(
        XmlElement actionElement)
    {
        if (!actionElement.HasAttribute("assert"))
            return;

        string assertion =
            actionElement.GetAttribute("assert");

        if (assertion == "user")
            throw new LoggedOffException(this);

        throw new AssertionFailedException(
            this,
            assertion);
    }

    /// <summary>
    /// Throws when MediaWiki reports that the attempted action matched the
    /// wiki's spam blacklist.
    /// </summary>
    /// <param name="actionElement">
    /// The action-specific API response element.
    /// </param>
    /// <exception cref="SpamlistException">
    /// Thrown when the response contains a spam blacklist match.
    /// </exception>
    private void ThrowIfSpamBlacklisted(
        XmlElement actionElement)
    {
        if (!actionElement.HasAttribute("spamblacklist"))
            return;

        throw new SpamlistException(
            this,
            actionElement.GetAttribute("spamblacklist"));
    }

    /// <summary>
    /// Throws when MediaWiki requires a CAPTCHA before completing the action.
    /// </summary>
    /// <param name="actionElement">
    /// The action-specific API response element.
    /// </param>
    /// <exception cref="CaptchaException">
    /// Thrown when the API response contains a CAPTCHA challenge.
    /// </exception>
    private void ThrowIfCaptchaRequired(
        XmlElement actionElement)
    {
        if (actionElement.GetElementsByTagName("captcha").Count > 0)
            throw new CaptchaException(this);
    }

    /// <summary>
    /// Throws when the action-specific API response reports an unsuccessful
    /// result.
    /// </summary>
    /// <param name="actionElement">
    /// The action-specific API response element.
    /// </param>
    /// <param name="action">
    /// The API action being validated.
    /// </param>
    /// <param name="xml">
    /// The raw XML response used when reporting a general operation failure.
    /// </param>
    /// <exception cref="MediaWikiSaysNoException">
    /// Thrown when the action was rejected by an AbuseFilter rule.
    /// </exception>
    /// <exception cref="OperationFailedException">
    /// Thrown for any other unsuccessful action result.
    /// </exception>
    private void ThrowIfActionFailed(
        XmlElement actionElement,
        string action,
        string xml)
    {
        string result =
            actionElement.GetAttribute("result");

        if (string.IsNullOrEmpty(result) ||
            result == "Success")
        {
            return;
        }

        string errorCode =
            actionElement.GetAttribute("code");

        if (errorCode.IndexOf(
                "abusefilter",
                StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new MediaWikiSaysNoException(
                this,
                actionElement.GetAttribute("warning"));
        }

        throw new OperationFailedException(
            this,
            action,
            result,
            xml);
    }

    #endregion
}

/// <summary>
/// Specifies how an operation should affect the watchlist status of a page.
/// </summary>
public enum WatchOptions
{
    /// <summary>
    /// Leave the current watchlist status unchanged.
    /// </summary>
    NoChange,

    /// <summary>
    /// Use the watchlist behavior configured in the user's preferences.
    /// </summary>
    UsePreferences,

    /// <summary>
    /// Add the page to the watchlist.
    /// </summary>
    Watch,

    /// <summary>
    /// Remove the page from the watchlist.
    /// </summary>
    Unwatch
}

/// <summary>
/// Specifies optional behaviors applied when executing an API request.
/// </summary>
[Flags]
public enum ActionOptions
{
    /// <summary>
    /// No additional processing.
    /// </summary>
    None = 0,

    /// <summary>
    /// Check the server's maxlag status before executing the request.
    /// </summary>
    CheckMaxlag = 1,

    /// <summary>
    /// Ensure the user is logged in before executing the request.
    /// </summary>
    RequireLogin = 2,

    /// <summary>
    /// Check for new user messages after the request completes.
    /// </summary>
    CheckNewMessages = 4,

    /// <summary>
    /// Enable all available request behaviors.
    /// </summary>
    All = CheckMaxlag | RequireLogin | CheckNewMessages
}
