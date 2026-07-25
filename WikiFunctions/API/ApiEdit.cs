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
using System.Reflection;
using System.Threading;
using System.Xml;
using WikiFunctions.Controls;

namespace WikiFunctions.API;

// TODO: Migrate remaining API operations from duplicate XmlReader parsing
// to validated XmlDocument access.
// TODO: generalize edit token retrieval
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
    /// 
    /// </summary>
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

        URL = url;
        PHP5 = usePHP5;
        ApiURL = URL + "api.php" + (PHP5 ? "5" : "");
        Maxlag = 5;

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
    /// Path to scripts on server e.g. https://en.wikipedia.org/w/ for en-wiki
    /// </summary>
    public string URL { get; private set; }

    /// <summary>
    /// Path to api.php e.g. https://en.wikipedia.org/w/api.php for en-wiki
    /// </summary>
    public string ApiURL { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    private string Server
    {
        get { return "https://" + new Uri(URL).Host; }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool PHP5 { get; private set; }

    /// <summary>
    /// Maxlag parameter of every request (https://www.mediawiki.org/wiki/Manual:Maxlag_parameter)
    /// </summary>
    public int Maxlag { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool NewMessageThrows { get; set; }

    /// <summary>
    /// Action for which we have edit token
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Name of the page currently being edited
    /// </summary>
    public PageInfo Page { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public string HtmlHeaders { get; private set; }

    /// <summary>
    /// Cookies stored between requests
    /// </summary>
    public CookieContainer Cookies { get; private set; }

    /// <summary>
    /// Whether we should pass the intoken parameter to the API
    /// </summary>
    private static bool UseInToken = true;

    #endregion

    /// <summary>
    /// Resets all internal variables, discarding edit tokens and so on,
    /// but does not logs off
    /// </summary>
    public void Reset()
    {
        Action = null;
        Page = new PageInfo();
        Aborting = false;
        Request = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Abort()
    {
        Aborting = true;

        HttpWebRequest request = Request;

        if (request != null)
            request.Abort();

        Thread.Sleep(1);
        Aborting = false;
    }

    /// <summary>
    /// This is a hack required for some multilingual Wikimedia projects,
    /// where CentralAuth returns cookies with a redundant domain restriction.
    /// </summary>
    private void AdjustCookies()
    {
        Uri uri = new Uri(URL);
        string host = uri.Host;
        var newCookies = new CookieContainer();
        var urls = new[] { uri, new Uri(uri.Scheme + Uri.SchemeDelimiter + "fnord." + host) };
        foreach (var u in urls)
        {
            foreach (Cookie c in Cookies.GetCookies(u))
            {
                c.Domain = host;
                newCookies.Add(c);
            }
        }

        Cookies = newCookies;
    }

    #region URL stuff

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
        for (int i = 0; i < titles.Length; i++)
            titles[i] = Tools.WikiEncode(titles[i]);

        if (titles.Length == 0)
            return string.Empty;

        return "&titles=" + string.Join("|", titles);
    }

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
    protected static string NamedTitles(string paramName, params string[] titles)
    {
        for (int i = 0; i < titles.Length; i++)
            titles[i] = Tools.WikiEncode(titles[i]);

        if (titles.Length == 0)
            return string.Empty;

        return "&" + paramName + "=" + string.Join("|", titles);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="options"></param>
    protected void AppendOptions(Dictionary<string, string> request, ActionOptions options)
    {
        if ((options & ActionOptions.CheckMaxlag) > 0 && Maxlag > 0)
        {
            request.Add("maxlag", Maxlag.ToString());
        }

        if ((options & ActionOptions.RequireLogin) > 0)
        {
            request.Add("assert", "user");
        }

        if (request.ContainsKey("action") && request["action"] == "query"
            && ((options & ActionOptions.CheckNewMessages) > 0))
        {
            if (request.ContainsKey("meta"))
            {
                request["meta"] += "|userinfo";
            }
            else
            {
                request.Add("meta", "userinfo");
            }
            if (Variables.NotificationsEnabled && User.HasReadNotificationsRight())
            {
                request["meta"] += "|notifications";
            }
            request.Add("uiprop", "hasmsg");
            request.Add("notprop", "count");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    protected string BuildUrl(Dictionary<string, string> request, ActionOptions options)
    {
        AppendOptions(request, options);
        return ApiURL + "?format=xml" + BuildQuery(request);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected string BuildUrl(Dictionary<string, string> request)
    {
        return BuildUrl(request, ActionOptions.None);
    }

    #endregion

    #region Network access

    private static readonly Dictionary<string, IWebProxy> ProxyCache = new Dictionary<string, IWebProxy>();
    private IWebProxy ProxySettings;

    private static readonly string UserAgent = string.Format("WikiFunctions ApiEdit/{0} ({1}; .NET CLR {2})",
        Assembly.GetExecutingAssembly().GetName().Version,
        Environment.OSVersion.VersionString,
        Environment.Version);

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

    private bool Aborting;
    private HttpWebRequest Request;

    private readonly object CancellationSyncRoot = new object();
    private bool CancellationScopeActive;
    private CancellationToken ActiveCancellationToken;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    protected string GetResponseString(HttpWebRequest req)
    {
        Request = req;

        if (!string.IsNullOrEmpty(Variables.HttpAuthUsername) && !string.IsNullOrEmpty(Variables.HttpAuthPassword))
        {
            NetworkCredential login = new NetworkCredential
            {
                UserName = Variables.HttpAuthUsername,
                Password = Variables.HttpAuthPassword,
                // Domain = "",
            };

            CredentialCache myCache = new CredentialCache
    {
        {new Uri(URL), "Basic", login}
    };
            req.Credentials = myCache;

            req = (HttpWebRequest)SetBasicAuthHeader(req, login.UserName, login.Password);
        }

        try
        {
            using (IDisposable requestCancellation =
                RegisterRequestCancellation(req))
            using (WebResponse resp = req.GetResponse())
            {
                // T357908: A custom wiki may redirect HTTP requests to HTTPS.
                // The current check prevents later requests from continuing with a mismatched
                // protocol, but it occurs after the redirect has already happened.
                //
                // TODO: Before login or any POST request, resolve the canonical API endpoint.
                // After user confirmation, update the complete active wiki/session URL state
                // and retry the operation using that endpoint.
                if (req.RequestUri.Scheme != resp.ResponseUri.Scheme)
                {
                    throw new UriChangedException(req.RequestUri.Scheme, resp.ResponseUri.Scheme);
                }

                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch (WebException ex)
        {
            ThrowIfModernRequestCancellation(ex);

            var resp = (HttpWebResponse)ex.Response;
            if (resp == null) throw;
            switch (resp.StatusCode)
            {
                case HttpStatusCode.Unauthorized: // 401
                    break;

                case HttpStatusCode.NotFound: // 404
                    Tools.WriteDebug(
                        nameof(ApiEdit),
                        $"HTTP 404 returned for '{req.RequestUri}'.");

                    return string.Empty;
            }

            throw;
        }
        finally
        {
            if (object.ReferenceEquals(Request, req))
                Request = null;
        }
    }

    private Dictionary<string, string> lastPostParameters;
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
                ? "<removed>"
                : parameter.Value;
        }

        return safeCopy;
    }

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="userName"></param>
    /// <param name="userPassword"></param>
    /// <returns></returns>
    /// <remarks>
    /// Source: http://blog.kowalczyk.info/article/Forcing-basic-http-authentication-for-HttpWebReq.html
    /// </remarks>
    protected WebRequest SetBasicAuthHeader(WebRequest req, string userName, string userPassword)
    {
        string authInfo = userName + ":" + userPassword;
        authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
        req.Headers["Authorization"] = "Basic " + authInfo;
        return req;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="get"></param>
    /// <param name="post"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    protected string HttpPost(Dictionary<string, string> get, Dictionary<string, string> post, ActionOptions options)
    {
        string url = BuildUrl(get, options);
        Tools.WriteDebug("ApiEdit::HttpPost", url);

        lastGetUrl = url;

        // Keep only a redacted copy for exception/debug diagnostics.
        // The original post dictionary is still used to send the real request.
        lastPostParameters = CreateSafeDiagnosticCopy(post);

        string query = BuildQuery(post);
        byte[] postData = Encoding.UTF8.GetBytes(query);

        HttpWebRequest req = CreateRequest(url);
        req.Method = "POST";
        req.ContentType = "application/x-www-form-urlencoded";
        req.ContentLength = postData.Length;

        Request = req;

        try
        {
            using (IDisposable requestCancellation =
                RegisterRequestCancellation(req))
            using (Stream rs = req.GetRequestStream())
            {
                rs.Write(postData, 0, postData.Length);
            }

            return GetResponseString(req);
        }
        catch (WebException ex)
        {
            ThrowIfModernRequestCancellation(ex);
            throw;
        }
        finally
        {
            if (object.ReferenceEquals(Request, req))
                Request = null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="get"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected string HttpPost(Dictionary<string, string> get, Dictionary<string, string> post)
    {
        return HttpPost(get, post, ActionOptions.None);
    }

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
    /// Performs a HTTP request
    /// </summary>
    /// <param name="request"></param>
    /// <returns>Text received</returns>
    protected string HttpGet(Dictionary<string, string> request)
    {
        return HttpGet(request, ActionOptions.None);
    }

    /// <summary>
    /// Performs a HTTP request
    /// </summary>
    /// <param name="url"></param>
    /// <returns>Text received</returns>
    public string HttpGet(string url)
    {
        Tools.WriteDebug("ApiEdit::HttpGet", url);
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
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic",
                encodedCredentials);
    }



    #endregion

    #region Login / user props

    public void Login(string username, string password)
    {
        Login(username, password, "");
    }

    public void Login(string username, string password, string domain)
    {
        if (string.IsNullOrEmpty(username)) throw new ArgumentException("Username required", "username");
        // if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password required", "password");

        Reset();
        User = new UserInfo(); // we don't know for sure what will be our status in case of exception
        Cookies = new CookieContainer();

        // first see if we can get a login token via the new MediaWiki way using action=query&meta=tokens&type=login
        string result = HttpPost(
            new Dictionary<string, string>
            {
                {"action", "query"},
                {"meta", "tokens"},
                {"type", "login"}
            },
            new Dictionary<string, string>());

        Tools.WriteDebug("API::Edit meta/tokens", "Received login-token response.");

        /* Result format: <query><tokens logintoken="b0fc31b291ebf9999a8e9a4bfac8ef0456c44116+\"/></query> */
        XmlDocument document = CheckForErrors(result, "query");

        XmlNode tokenNode =
            document.SelectSingleNode("/api/query/tokens");

        string token = tokenNode == null || tokenNode.Attributes == null
            ? null
            : tokenNode.Attributes["logintoken"] == null
                ? null
                : tokenNode.Attributes["logintoken"].Value;

        // If not a bot, use the clientlogin API, which gives an opportunity to supply a OTC
        if (!username.Contains("@") && !string.IsNullOrEmpty(token))
        {
            ClientLogin(username, password, token);
        }
        else
        {
            // This is legacy code, now used for non-en or bots. Also, it's unlikely that modern wikis will have
            // failed to provide a token above, but leaving the relevant code in doesn't hurt.
            //
            // first log in. If we got a logintoken then use it, this should be our only action=login in that case
            bool domainSet = !string.IsNullOrEmpty(domain);
            var post = new Dictionary<string, string>
            {
                {"lgname", username},
                {"lgpassword", password},
            };
            post.AddIfTrue(domainSet, "lgdomain", domain);
            post.AddIfTrue(!string.IsNullOrEmpty(token), "lgtoken", token);

            result = HttpPost(
                new Dictionary<string, string>
                {
                {"action", "login"}
                },
                post
                );

            XmlDocument loginDocument = LoadApiXmlDocument(result);

            XmlNode loginNode =
                loginDocument.SelectSingleNode("/api/login");

            if (loginNode == null)
                throw new Exception("Cannot find <login> element");

            Tools.WriteDebug("API::Edit action/login", "Received login-token response.");

            // Select the direct API result rather than navigating through any
            // similarly named element that may appear inside warnings.
            string status =
                XmlResponseHelpers.RequireAttributeValue(
                    loginNode,
                    "result");

            // Older MediaWiki versions can return NeedToken on the first
            // action=login response. Retry with the returned token.
            if (string.IsNullOrEmpty(token) &&
                status.Equals(
                    "NeedToken",
                    StringComparison.InvariantCultureIgnoreCase))
            {
                AdjustCookies();

                token =
                    XmlResponseHelpers.RequireAttributeValue(
                        loginNode,
                        "token");

                post.Add("lgtoken", token);

                result = HttpPost(
                    new Dictionary<string, string>
                    {
                       {"action", "login"}
                    },
                    post
                );

                Tools.WriteDebug(
                    "API::Edit action/login NeedToken",
                    result);

                loginDocument = LoadApiXmlDocument(result);

                loginNode =
                    loginDocument.SelectSingleNode("/api/login");

                if (loginNode == null)
                    throw new Exception("Cannot find <login> element");

                status =
                    XmlResponseHelpers.RequireAttributeValue(
                        loginNode,
                        "result");
            }
            if (status != null && !status.Equals("Success", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new LoginException(this, status);
            }
        }

        CheckForErrors(result, "login");
        AdjustCookies();

        RefreshUserInfo();
    }

    public void ClientLogin(string username, string password, string token)
    {
        Dictionary<string, string> postparams = new Dictionary<string, string>
{
    {"password", password},
    {"logintoken", token}
};

        string result = HttpPost(
            new Dictionary<string, string>
            {
        {"action", "clientlogin"},
        {"username", username},
        {"loginreturnurl", "https://en.wikipedia.org/"} // Not used but required by API
            },
            postparams
        );

        Tools.WriteDebug(
            "API::Edit action/clientlogin",
            "Received ClientLogin response.");

        // ClientLogin can return a valid UI status for follow-up authentication,
        // so validate API-level errors without treating the status itself as an
        // action-specific success or failure result.
        XmlDocument clientLoginDocument = CheckForErrors(result);

        XmlNode clientLoginNode =
            clientLoginDocument.SelectSingleNode("/api/clientlogin");

        if (clientLoginNode == null)
            throw new Exception("Cannot find <clientlogin> element");

        string status =
            XmlResponseHelpers.RequireAttributeValue(
                clientLoginNode,
                "status").ToUpperInvariant();

        if (status == "PASS")
            return;

        // Handle 2FA using EmailAuth.
        // OATHAuth should work the same way, using the OATHToken parameter,
        // but that path has not been tested.
        if (status != "UI")
            throw new LoginException(this, status);

        string message = "";

        if (clientLoginNode.Attributes != null)
        {
            XmlAttribute messageAttribute =
                clientLoginNode.Attributes["message"];

            if (messageAttribute != null)
                message = messageAttribute.Value;
        }

        // Makes the (unverified) assumption that the email will be in
        // parentheses in all localizations.
        Match emailMatch = Regex.Match(message, @"\(.+?@.+?\)");

        if (!emailMatch.Success)
            throw new LoginException(this, status);

        postparams.Clear();

        result = HttpPost(
            new Dictionary<string, string>
            {
        {"action", "query"},
        {"meta", "siteinfo"},
        {"siprop", "extensions"}
            },
            postparams
        );

        XmlDocument siteInfoDocument = CheckForErrors(result, "query");

        XmlNode emailAuthExtension =
            siteInfoDocument.SelectSingleNode(
                "/api/query/extensions/ext[@name='EmailAuth']");

        if (emailAuthExtension != null)
        {
            // The message itself is too long for InputBox. Continue to assume
            // the current email-address syntax.
            InputBoxResult coderesult = InputBox.Show(
                "Enter the code sent to your email " + emailMatch.Value + '.',
                "Enter One-Time-Code",
                "",
                ClientLoginValidator);

            if (!coderesult.OK)
                throw new LoginException(this, "Login cancelled");

            postparams.Add("logintoken", token);

            result = HttpPost(
                new Dictionary<string, string>
                {
            {"action", "clientlogin"},
            {"logincontinue", "1"},
            {"token", coderesult.Text}
                },
                postparams
            );

            Tools.WriteDebug(
                "API::Edit action/clientlogin2",
                "Received ClientLogin continuation response.");

            clientLoginDocument = CheckForErrors(result);

            clientLoginNode =
                clientLoginDocument.SelectSingleNode("/api/clientlogin");

            if (clientLoginNode == null)
                throw new Exception("Cannot find <clientlogin> element");

            status =
                XmlResponseHelpers.RequireAttributeValue(
                    clientLoginNode,
                    "status").ToUpperInvariant();

            if (status == "PASS")
                return;

            // If status is UI, the entered code was likely incorrect. We do not
            // loop indefinitely because repeated attempts could become confusing.
            // The API's message is server-localized, so preserve the existing
            // generic LoginException behavior below.
        }

        throw new LoginException(this, status);
    }

    private static void ClientLoginValidator(object sender, InputBoxValidatingArgs e)
    {
        // Currently (July 2025) the OTC is six digits, and there is "no plan" to change that
        if (e.Text == null || !Regex.IsMatch(e.Text, @"^\d{6}$"))
        {
            e.Cancel = true;
            e.Message = "Code must be six digits";
        }
    }

    /// <summary>
    /// Logs out of the current wiki session.
    ///
    /// MediaWiki requires logout requests to use a CSRF token and be sent as a
    /// POST request. The token is requested before any local session state is
    /// cleared so that a failed logout does not leave AWB believing it is logged
    /// out while the server session remains active.
    /// </summary>
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

    public void Watch(string title)
    {
        WatchAction(title, false);
    }

    public void WatchAction(string title, bool unwatch)
    {
        if (string.IsNullOrEmpty(title)) throw new ArgumentException("Page name required", "title");

        if (string.IsNullOrEmpty(Page.WatchToken))
        {
            // Token needed as of 1.18
            string result = HttpGet(
                new Dictionary<string, string>
                {
                    {"action", "query"},
                    {"prop", "info"},
                    {"meta", "tokens"}, // Since 1.24
                    {"type", "watch"},
                    {"intoken", "watch"}, // Pre 1.24 compat
                    {"titles", title}
                },
                ActionOptions.All);

            XmlDocument document = CheckForErrors(result);

            try
            {
                // MediaWiki 1.24+ returns the token in <tokens>. Older versions
                // return it on the queried <page> element.
                XmlNode tokenSource =
                    document.SelectSingleNode("/api/query/tokens") ??
                    document.SelectSingleNode("/api/query/pages/page");

                if (tokenSource == null)
                    throw new Exception("Cannot find <tokens> or <page> element");

                Page.WatchToken =
                    XmlResponseHelpers.RequireAttributeValue(
                        tokenSource,
                        "watchtoken");
            }
            catch (Exception ex)
            {
                throw new BrokenXmlException(this, ex);
            }
        }

        if (Aborting) throw new AbortedException(this);

        var watchParameters = new Dictionary<string, string>
        {
            {"title", title},
            {"token", Page.WatchToken}
        };

        if (unwatch)
        {
            watchParameters.Add("unwatch", null);
        }

        var result2 = HttpPost(
            new Dictionary<string, string>
            {
               {"action", "watch"}
            },
            watchParameters,
            ActionOptions.All);

        CheckForErrors(result2, "watch");
    }

    public void Unwatch(string title)
    {
        WatchAction(title, true);
    }

    public UserInfo User { get; private set; }

    public void RefreshUserInfo()
    {
        Reset();
        User = new UserInfo();

        string result = HttpPost(
            new() { { "action", "query" } },
            new()
            {
                { "meta", "userinfo" },
                { "uiprop", "blockinfo|hasmsg|groups|rights" }
            });

        var xml = CheckForErrors(result, "userinfo");

        User = new UserInfo(xml);
    }

    public void ClearNewMessages()
    {
        HttpPost(
            new() { { "action", "clearhasmsg" } },
            new());
    }
    #endregion

    #region Page modification

    /// <summary>
    /// Opens the wiki page for editing
    /// </summary>
    /// <param name="title">The wiki page title</param>
    /// <returns>The current content of the wiki page</returns>
    public string Open(string title)
    {
        return Open(title, false);
    }

    /// <summary>
    /// Opens the wiki page for editing
    /// </summary>
    /// <param name="title">The wiki page title</param>
    /// <param name="resolveRedirects"></param>
    /// <returns>The current content of the wiki page</returns>
    public string Open(string title, bool resolveRedirects)
    {
        if (string.IsNullOrEmpty(title))
            throw new ArgumentException("Page name required", "title");

        if (!User.IsLoggedIn)
            throw new LoggedOffException(this);

        Reset();

        /* converttitles: API doc says "converttitles - Convert titles to other variants if necessary. Only works if the wiki's content language supports variant conversion.
           Languages that support variant conversion include gan, iu, kk, ku, shi, sr, tg, uz, zh"
         * Example with and without converttitles: zh-wiki page 龙门飞甲
         * https://zh.wikipedia.org/w/api.php?action=query&prop=info|revisions&titles=龙门飞甲&rvprop=timestamp|user|comment|content
         * https://zh.wikipedia.org/w/api.php?action=query&converttitles&prop=info|revisions&titles=龙门飞甲&rvprop=timestamp|user|comment|content
         If convertitles is not set, API doesn't find the page
         */
        var query = new Dictionary<string, string>
        {
            {"action", "query"},
            {"converttitles", null},
            {"prop", "info|revisions"},
            {"meta", "tokens"}, // Since 1.24
            {"type", "csrf|watch|rollback"}, // CSRF is for most actions
            {"intoken", "edit|protect|delete|move|watch"}, // Pre 1.24 compat
            {"titles", title},
            {"inprop", "protection|watched|displaytitle"},
            {"rvprop", "content|timestamp"}, // timestamp|user|comment|
            {"curtimestamp", null}
        };
        query.AddIfTrue(resolveRedirects, "redirects", null);

        string result = HttpGet(query, ActionOptions.All);

        XmlDocument document = CheckForErrors(result, "query");

        try
        {
            Page = new PageInfo(document);

            Action = "edit";
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }

        return Page.Text;
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

        string result = HttpGet(query, ActionOptions.None);
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

        var get = new Dictionary<string, string>
    {
        { "action", "edit" },
        { "title", Page.Title },
        { "watchlist", WatchOptionsToParam(watch) }
    };

        get.AddIfTrue(minor, "minor", null);
        get.AddIfTrue(User.IsBot, "bot", null);

        var post = new Dictionary<string, string>
    {
        // Parameter order matters. See Wikimedia Phabricator task T16210.
        { "md5", MD5(pageText) },
        { "summary", summary },
        { "basetimestamp", Page.Timestamp },
        { "text", pageText },
        { "starttimestamp", Page.TokenTimestamp }
    };

        post.AddIfTrue(Variables.TagEdits, "tags", "AWB");
        post.AddIfTrue(
            contentModel != "wikitext",
            "contentmodel",
            contentModel);

        post.Add("token", Page.EditToken);

        string result = HttpPost(
            get,
            post,
            ActionOptions.All);

        XmlDocument xml = CheckForErrors(result, "edit");

        Reset();

        return new SaveInfo(xml);
    }

    public void Delete(string title, string reason)
    {
        Delete(title, reason, false);
    }

    public void Delete(string title, string reason, bool watch)
    {
        if (string.IsNullOrEmpty(title)) throw new ArgumentException("Page name required", "title");
        if (string.IsNullOrEmpty(reason)) throw new ArgumentException("Deletion reason required", "reason");

        // Reset();
        Action = "delete";

        if (string.IsNullOrEmpty(Page.DeleteToken))
        {
            var result = HttpGet(
                new Dictionary<string, string>
                {
                    {"action", "query"},
                    {"prop", "info"},
                    {"meta", "tokens"}, // Since 1.24
                    {"type", "csrf"},
                    {"intoken", "delete"}, // Pre 1.24 compat
                    {"titles", title}
                },
                ActionOptions.All);

            XmlDocument document = CheckForErrors(result);

            try
            {
                // MediaWiki 1.24+ returns the CSRF token in <tokens>.
                // Older compatibility responses can return deletetoken on <page>.
                XmlNode tokenSource =
                    document.SelectSingleNode("/api/query/tokens") ??
                    document.SelectSingleNode("/api/query/pages/page");

                if (tokenSource == null)
                    throw new Exception("Cannot find <tokens> or <page> element");

                string tokenAttribute =
                    tokenSource.Name == "tokens"
                        ? "csrftoken"
                        : "deletetoken";

                Page.DeleteToken =
                    XmlResponseHelpers.RequireAttributeValue(
                        tokenSource,
                        tokenAttribute);
            }
            catch (Exception ex)
            {
                throw new BrokenXmlException(this, ex);
            }
        }

        if (Aborting) throw new AbortedException(this);

        var post = new Dictionary<string, string>
        {
            {"title", title},
            {"token", Page.DeleteToken},
            {"reason", reason},
        };

        // post.AddIfTrue(User.IsBot, "bot", null);
        post.AddIfTrue(watch, "watch", null);
        var result2 = HttpPost(
            new Dictionary<string, string>
            {
                {"action", "delete"}
            },
            post,
            ActionOptions.All);

        CheckForErrors(result2);

        Reset();
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

        var post = BuildProtectPostData(
            title,
            reason,
            expiry,
            expiryvalue,
            protections,
            cascade,
            watch);

        var get = new Dictionary<string, string>
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

        string result = HttpGet(
            new Dictionary<string, string>
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

    /// <summary>
    /// Builds the protection-level value for an existing or nonexistent page.
    /// </summary>
    /// <param name="edit">The edit or creation protection level.</param>
    /// <param name="move">The move protection level.</param>
    /// <returns>The protection-level value accepted by MediaWiki.</returns>
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

    /// <summary>
    /// Builds the POST parameters for a page-protection request.
    /// </summary>
    private Dictionary<string, string> BuildProtectPostData(
        string title,
        string reason,
        string expiry,
        string expiryvalue,
        string protections,
        bool cascade,
        bool watch)
    {
        var post = new Dictionary<string, string>
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

    public void Move(string title, string newTitle, string reason, bool moveTalk, bool noRedirect, bool watch)
    {
        if (string.IsNullOrEmpty(title)) throw new ArgumentException("Page title required", "title");
        if (string.IsNullOrEmpty(newTitle)) throw new ArgumentException("Target page title required", "newTitle");
        if (string.IsNullOrEmpty(reason)) throw new ArgumentException("Page rename reason required", "reason");

        if (title == newTitle) throw new ArgumentException("Page cannot be moved to the same title");

        //Reset();
        Action = "move";

        if (string.IsNullOrEmpty(Page.MoveToken))
        {
            string result = HttpGet(
                new Dictionary<string, string>
                {
                    {"action", "query"},
                    {"prop", "info"},
                    {"meta", "tokens"}, // Since 1.24
                    {"type", "csrf"},
                    {"intoken", "move"}, // Pre 1.24 compat
                    {"titles", title + "|" + newTitle}
                },
                ActionOptions.All);

            XmlDocument document = CheckForErrors(result, "query");

            try
            {
                XmlNode invalidPage =
                    document.SelectSingleNode("/api/query/pages/page[@invalid]");

                if (invalidPage != null)
                {
                    throw new ApiException(
                        this,
                        "invalidnewtitle",
                        new ArgumentException(
                            "Target page invalid",
                            "newTitle"));
                }

                XmlNode sourcePage =
                    document.SelectSingleNode("/api/query/pages/page");

                if (sourcePage == null)
                    throw new Exception("Cannot find <page> element");

                XmlNode tokenSource =
                    document.SelectSingleNode("/api/query/tokens") ??
                    sourcePage;

                string tokenAttribute =
                    tokenSource.Name == "tokens"
                        ? "csrftoken"
                        : "movetoken";

                Page.MoveToken =
                    XmlResponseHelpers.RequireAttributeValue(
                        tokenSource,
                        tokenAttribute);
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

        if (Aborting) throw new AbortedException(this);

        var post = new Dictionary<string, string>
        {
            {"from", title},
            {"to", newTitle},
            {"token", Page.MoveToken},
            {"reason", reason},
            {"protections", ""},
        };

        post.AddIfTrue(moveTalk, "movetalk", null);
        post.AddIfTrue(noRedirect, "noredirect", null);
        //post.AddIfTrue(User.IsBot, "bot", null);
        post.AddIfTrue(watch, "watch", null);

        var result2 = HttpPost(
            new Dictionary<string, string>
            {
                {"action", "move"}
            },
            post,
            ActionOptions.All);

        CheckForErrors(result2, "move");

        Reset();
    }

    #endregion

    #region Query Api

    public string QueryApi(string queryParameters)
    {
        if (string.IsNullOrEmpty(queryParameters))
            throw new ArgumentException("queryParamters cannot be null/empty", "queryParamters");

        string result = HttpGet(ApiURL + "?action=query&format=xml&" + queryParameters);
        //Should we be checking for maxlag?

        CheckForErrors(result, "query");

        return result;
    }

    public string QueryApiJson(string queryParameters)
    {
        if (string.IsNullOrEmpty(queryParameters))
            throw new ArgumentException("queryParamters cannot be null/empty", "queryParamters");

        string result = HttpGet(ApiURL + "?action=query&format=json&" + queryParameters);
        // TODO: Validate JSON API errors, including maxlag, without changing the
        // successful raw JSON response returned to callers.

        return result;
    }

    #endregion

    #region Parse Api

    public string ParseApi(Dictionary<string, string> queryParameters)
    {
        string result = HttpPost(
            new Dictionary<string, string>
            {
                {"action", "parse"},
                {"format", "xml"},
                {"prop", "text|displaytitle|langlinks|categories"}
            },
            queryParameters); // TODO: Decide whether this generic API method should opt into the configured
                              // Maxlag policy. Raw query-string methods currently bypass ActionOptions.

        CheckForErrors(result, "parse");

        return result;
    }

    #endregion

    #region Wikitext operations

    private string ExpandRelativeUrls(string html)
    {
        // wikilinks
        html = html.Replace(@" href=""/wiki/", @" href=""" + Server + @"/wiki/");

        // relative links (to images, scripts etc.)
        html = html.Replace(@" href=""/w/", @" href=""" + Server + @"/w/");

        html = html.Replace(@" href=""//", @" href=""https://");
        return html.Replace(@" src=""//", @" src=""https://");
    }

    private static readonly Regex ExtractCssAndJs = new Regex(@"("
                                                              + @"<!--\[if .*?-->"
                                                              + @"|<style\b.*?>.*?</style>"
                                                              + @"|<link rel=""stylesheet"".*?/\s?>"
                                                              // + @"|<script type=""text/javascript"".*?</script>"
                                                              + ")",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Loads wiki's UI HTML and scrapes everything we need to make correct previews
    /// </summary>
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
        StringBuilder extracted = new StringBuilder(2048);

        foreach (Match m in ExtractCssAndJs.Matches(result))
        {
            extracted.Append(m.Value);
            extracted.Append("\n");
        }

        HtmlHeaders = ExpandRelativeUrls(extracted.ToString());

        /*
         * T117870: The legacy WinForms WebBrowser rendering engine may apply the
         * browser-default italic style to <cite> elements even when MediaWiki's
         * styles are expected to override it. Add an explicit citation-class rule
         * so previews match the rendered wiki page more closely.
         */
        HtmlHeaders += @" <style> .citation { font-style: normal; } </style>";
    }

    public string Preview(string title, string text)
    {
        EnsureHtmlHeadersLoaded();

        string result = HttpPost(
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

        XmlDocument document = CheckForErrors(result, "parse");

        try
        {
            XmlNode textNode = document.SelectSingleNode("/api/parse/text");

            if (textNode == null)
                throw new Exception("Cannot find <text> element");

            string previewHtml = textNode.InnerText;

            // Extract parse warnings, such as duplicate template parameters, and
            // place them above the preview in the existing warning style.
            XmlNodeList warningNodes =
                document.SelectNodes("/api/parse/parsewarnings/pw");

            if (warningNodes != null && warningNodes.Count > 0)
            {
                StringBuilder warnings = new StringBuilder();

                foreach (XmlNode warningNode in warningNodes)
                {
                    warnings.Append(warningNode.InnerText);
                    warnings.Append("<p>");
                }

                previewHtml =
                    @"<div class=""previewnote"" style=""color:#d33"">" +
                    warnings +
                    "</div>" +
                    previewHtml;
            }

            return ExpandRelativeUrls(previewHtml);
        }
        catch (Exception ex)
        {
            throw new BrokenXmlException(this, ex);
        }
    }

    public void Rollback(string title, string user)
    {
        if (string.IsNullOrEmpty(title))
            throw new ArgumentException("Page name required", "title");

        if (string.IsNullOrEmpty(user))
            throw new ArgumentException("User name required", "user");

        if (string.IsNullOrEmpty(Page.RollbackToken))
        {
            string result = HttpGet(
                new Dictionary<string, string>
                {
                    {"action", "query"},
                    {"prop", "revisions"},
                    {"meta", "tokens"}, // Since 1.24
                    {"type", "rollback"},
                    {"rvtoken", "rollback"}, // Pre 1.24 compat
                    {"titles", title}
                },
                ActionOptions.All);

            XmlDocument document = CheckForErrors(result, "query");

            try
            {
                // MediaWiki 1.24+ returns the rollback token in <tokens>.
                // Older compatibility responses can return it on <page>.
                XmlNode tokenSource =
                    document.SelectSingleNode("/api/query/tokens") ??
                    document.SelectSingleNode("/api/query/pages/page");

                if (tokenSource == null)
                    throw new Exception("Cannot find <tokens> or <page> element");

                Page.RollbackToken =
                    XmlResponseHelpers.RequireAttributeValue(
                        tokenSource,
                        "rollbacktoken");
            }
            catch (Exception ex)
            {
                throw new BrokenXmlException(this, ex);
            }
        }

        var result2 = HttpPost(
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

        CheckForErrors(result2, "rollback");
    }

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

        XmlDocument document = CheckForErrors(result, "expandtemplates");

        try
        {
            XmlNode expandedTextNode =
                document.SelectSingleNode("/api/expandtemplates");

            if (expandedTextNode == null)
                throw new Exception("Cannot find <expandtemplates> element");

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
    /// Checks the XML returned by the server for error codes and throws an appropriate exception
    /// </summary>
    /// <param name="xml">Server output</param>
    private XmlDocument CheckForErrors(string xml)
    {
        return CheckForErrors(xml, null);
    }

    private static readonly Regex MaxLag = new Regex(@": (\d+(?:\.\d+)?) seconds lagged",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Checks the XML returned by the server for error codes and throws an appropriate exception
    /// </summary>
    /// <param name="xml">Server output</param>
    /// <param name="action">The action performed, null if don't check</param>
    private XmlDocument CheckForErrors(string xml, string action)
    {
        if (string.IsNullOrEmpty(xml)) throw new ApiBlankException(this);

        XmlDocument doc;

        try
        {
            doc = LoadApiXmlDocument(xml);
        }
        catch (XmlException xe)
        {
            Tools.WriteDebug("ApiEdit::CheckForErrors", xml);

            string postParams = "";

            if (lastPostParameters != null)
            {
                postParams = BuildQuery(lastPostParameters);
            }

            throw new ApiXmlException(this, xe, lastGetUrl, postParams, xml);
        }

        var errors = doc.GetElementsByTagName("error");

        if (errors.Count > 0)
        {
            var error = errors[0];
            string errorCode = XmlResponseHelpers.RequireAttributeValue(error, "code");

            string errorMessage = XmlResponseHelpers.RequireAttributeValue(error, "info");

            switch (errorCode.ToLower())
            {
                case "maxlag": // guessing
                    double maxlag;
                    double.TryParse(MaxLag.Match(xml).Groups[1].Value, out maxlag);
                    throw new MaxlagException(this, maxlag, 10);
                case "wrnotloggedin":
                    throw new LoggedOffException(this);
                case "spamdetected":
                    throw new SpamlistException(this, errorMessage);
                case "spamblacklist":
                    throw new SpamlistException(this, errorMessage);
                case "spamprotectiontext":
                    throw new SpamlistException(this, errorMessage);
                case "fileexists-sharedrepo-perm":
                    throw new SharedRepoException(this, errorMessage);
                case "hookaborted":
                    throw new MediaWikiSaysNoException(this, errorMessage);
                case "readonly":
                    throw new MediaWikiReadOnlyException(this, errorMessage + "\r\n\r\nReason: " + XmlResponseHelpers.RequireAttributeValue(error, "readonlyreason"));

                //case "confirmemail":
                //
                default:
                    if (errorCode.Contains("disabled"))
                    {
                        throw new FeatureDisabledException(this, errorCode, errorMessage);
                    }
                    if (errorMessage == "Unknown error: \"tpt-target-page\"")
                    {
                        throw new TranslationPageEditException(this);
                    }

                    throw new ApiErrorException(this, errorCode, errorMessage);
            }
        }

        // look at warnings: are notifications enabled on wiki
        var warnings = doc.GetElementsByTagName("warnings");
        if (warnings.Count > 0)
        {
            var xmlNode = warnings.Item(0);
            if (xmlNode != null)
            {
                StringBuilder warningBuilder = new StringBuilder();
                foreach (XmlNode childNode in xmlNode.ChildNodes)
                {
                    // use Contains as warnings may be in a single XML block
                    if (childNode.InnerText.Contains("Unrecognized value for parameter 'meta': notifications"))
                    {
                        Variables.NotificationsEnabled = false;
                    }
                    else if (childNode.InnerText.Contains("The parameter \"intoken\" has been deprecated.") ||
                             childNode.InnerText.Contains("Unrecognized parameter: intoken."))
                    {
                        UseInToken = false;
                    }
                    warningBuilder.AppendLine(childNode.InnerText);
                }
                if (warningBuilder.Length > 0)
                {
                    Tools.WriteDebug("ApiEdit::CheckForErrors warnings", warningBuilder.ToString());
                }
            }
        }

        if (string.IsNullOrEmpty(action))
            return CompleteSuccessfulResponseValidation(doc, action);

        var api = doc["api"];

        if (api == null)
            return CompleteSuccessfulResponseValidation(doc, action);

        var redirects = api.GetElementsByTagName("r");
        if (action == "query" && redirects.Count > 0) //We have redirects
        {
            // Workaround for https://phabricator.wikimedia.org/T41492
            string redirectTarget = XmlResponseHelpers.RequireAttributeValue(redirects[redirects.Count - 1], "to");

            if (Namespace.IsSpecial(Namespace.Determine(redirectTarget)))
            {
                throw new RedirectToSpecialPageException(this);
            }
        }

        ThrowIfInvalidTitle(api);
        ThrowIfInterwikiRedirect(api);

        var actionElement = api[action];

        if (actionElement == null)
        {
            return CompleteSuccessfulResponseValidation(doc, action);
        }

        ThrowIfAssertionFailed(actionElement);
        ThrowIfSpamBlacklisted(actionElement);
        ThrowIfCaptchaRequired(actionElement);
        ThrowIfActionFailed(actionElement, action, xml);

        return CompleteSuccessfulResponseValidation(doc, action);
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

    private CancellationToken GetActiveCancellationToken()
    {
        lock (CancellationSyncRoot)
        {
            return CancellationScopeActive
                ? ActiveCancellationToken
                : CancellationToken.None;
        }
    }

    private IDisposable RegisterRequestCancellation(HttpWebRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CancellationToken cancellationToken = GetActiveCancellationToken();

        if (!cancellationToken.CanBeCanceled)
            return null;

        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);

        return cancellationToken.Register(
            delegate
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

    private void ThrowIfModernRequestCancellation(WebException ex)
    {
        if (ex == null || ex.Status != WebExceptionStatus.RequestCanceled)
            return;

        CancellationToken cancellationToken = GetActiveCancellationToken();

        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);

        throw new AbortedException(this);
    }

    private sealed class CancellationScope : IDisposable
    {
        private ApiEdit Editor;
        private readonly bool PreviousScopeActive;
        private readonly CancellationToken PreviousCancellationToken;

        public CancellationScope(
            ApiEdit editor,
            CancellationToken cancellationToken)
        {
            if (editor == null)
                throw new ArgumentNullException("editor");

            Editor = editor;

            lock (editor.CancellationSyncRoot)
            {
                PreviousScopeActive = editor.CancellationScopeActive;
                PreviousCancellationToken = editor.ActiveCancellationToken;

                editor.CancellationScopeActive = true;
                editor.ActiveCancellationToken = cancellationToken;
            }
        }

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
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    protected static string BoolToParam(bool value)
    {
        return value ? "1" : "0";
    }

    protected static string WatchOptionsToParam(WatchOptions watch)
    {
        switch (watch)
        {
            case WatchOptions.UsePreferences:
                return "preferences";
            case WatchOptions.Watch:
                return "watch";
            case WatchOptions.Unwatch:
                return "unwatch";
            default:
                return "nochange";
        }
    }

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

    // Checks the API response for an invalid page title.
    //
    // MediaWiki returns a <page> element with an "invalid" attribute
    // when the requested title cannot be used or does not meet title rules.
    private void ThrowIfInvalidTitle(System.Xml.XmlElement api)
    {
        // Look for page elements in the API response.
        System.Xml.XmlNodeList pages = api.GetElementsByTagName("page");

        // No page element means there is no invalid-title response to handle.
        if (pages.Count == 0)
        {
            return;
        }

        // XmlNodeList contains XmlNode objects, so safely cast the first
        // page node to an XmlElement before checking its attributes.
        System.Xml.XmlElement pageElement =
            pages[0] as System.Xml.XmlElement;

        // Continue normal processing unless this page explicitly has
        // MediaWiki's "invalid" response attribute.
        if (pageElement == null || !pageElement.HasAttribute("invalid"))
        {
            return;
        }

        // GetAttribute returns an empty string if title is unexpectedly absent,
        // avoiding a NullReferenceException from direct attribute access.
        string title = pageElement.GetAttribute("title");

        throw new InvalidTitleException(this, title);
    }

    // Checks whether MediaWiki redirected the request to an interwiki target.
    //
    // AWB handles these separately because the requested action cannot proceed
    // against the current wiki when the target belongs to another wiki.
    private void ThrowIfInterwikiRedirect(System.Xml.XmlElement api)
    {
        if (api.GetElementsByTagName("interwiki").Count > 0)
        {
            throw new InterwikiException(this);
        }
    }

    // Checks whether the API rejected the request because an assertion failed.
    //
    // The most common current case is assert=user, which means the user is no
    // longer logged in or the login session is no longer valid.
    private void ThrowIfAssertionFailed(System.Xml.XmlElement actionElement)
    {
        // No assert attribute means MediaWiki did not report an assertion failure.
        if (!actionElement.HasAttribute("assert"))
        {
            return;
        }

        string assertion = actionElement.GetAttribute("assert");

        // Preserve the existing specialized exception for a lost login session.
        if (assertion == "user")
        {
            throw new LoggedOffException(this);
        }

        // Other assertion types are still useful to expose to the caller.
        throw new AssertionFailedException(this, assertion);
    }

    // Checks whether the attempted edit matched the wiki's spam blacklist.
    //
    // The spamblacklist attribute typically contains the pattern or URL fragment
    // that caused MediaWiki to reject the request.
    private void ThrowIfSpamBlacklisted(System.Xml.XmlElement actionElement)
    {
        if (!actionElement.HasAttribute("spamblacklist"))
        {
            return;
        }

        throw new SpamlistException(
            this,
            actionElement.GetAttribute("spamblacklist"));
    }

    // Checks whether MediaWiki requires a CAPTCHA response before it will
    // complete the requested edit or action.
    private void ThrowIfCaptchaRequired(System.Xml.XmlElement actionElement)
    {
        if (actionElement.GetElementsByTagName("captcha").Count > 0)
        {
            throw new CaptchaException(this);
        }
    }

    // Checks the action result for an unsuccessful API response.
    //
    // Successful actions either have result="Success" or, for some response
    // shapes, no result attribute at all. All other result values are treated
    // as failed operations.
    private void ThrowIfActionFailed(
        System.Xml.XmlElement actionElement,
        string action,
        string xml)
    {
        string result = actionElement.GetAttribute("result");

        // No result value, or an explicit Success value, means this method
        // has nothing to reject.
        if (string.IsNullOrEmpty(result) || result == "Success")
        {
            return;
        }

        // Read the MediaWiki error code once so it can be checked for
        // specialized failures before falling back to a general exception.
        string errorCode = actionElement.GetAttribute("code");

        // AbuseFilter failures have a dedicated exception because the warning
        // attribute contains the useful user-facing explanation.
        if (errorCode.IndexOf(
                "abusefilter",
                System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new MediaWikiSaysNoException(
                this,
                actionElement.GetAttribute("warning"));
        }

        // Preserve the original general failure behavior for every other
        // non-successful MediaWiki action response.
        throw new OperationFailedException(this, action, result, xml);
    }

    #endregion
}

public enum WatchOptions
{
    NoChange,
    UsePreferences,
    Watch,
    Unwatch
}

[Flags]
public enum ActionOptions
{
    None = 0,
    CheckMaxlag = 1,
    RequireLogin = 2,
    CheckNewMessages = 4,

    All = CheckMaxlag | RequireLogin | CheckNewMessages
}
