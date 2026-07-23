using System.Collections.Specialized;
using System.Net.Http;
using System.Threading;
using WikiFunctions.Plugin;

namespace WikiFunctions.Networking
{
    /// <summary>
    /// Provides the centralized HTTP networking infrastructure used by
    /// AutoWikiBrowser, including proxy configuration, request creation,
    /// and common HTTP operations.
    /// </summary>
    internal static class AwbHttpClient
    {
        /// <summary>
        /// Cached system proxy used for outgoing HTTP requests.
        /// Null when no proxy is configured or the target bypasses the proxy.
        /// </summary>
        private static IWebProxy _systemProxy;

        /// <summary>
        /// Retrieves the contents of an HTTP resource using the configured
        /// networking settings.
        /// </summary>
        /// <param name="url">The resource URL.</param>
        /// <param name="encoding">The text encoding to use when reading the response.</param>
        /// <param name="responseUrl">Receives the final resolved response URL after redirects.</param>
        /// <param name="awb">
        /// Optional AutoWikiBrowser instance used when authenticated requests
        /// require additional configuration.
        /// </param>
        /// <returns>The response body.</returns>
        public static string GetString(
            string url,
            Encoding encoding,
            out string responseUrl,
            IAutoWikiBrowser awb = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException(
                    "A URL is required.",
                    nameof(url));

            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            if (Globals.UnitTestMode)
            {
                throw new InvalidOperationException(
                    "You shouldn't access Wikipedia from unit tests.");
            }

            Tools.WriteDebug("AwbHttpClient::GetString", url);

            using HttpClient client = CreateClient(url, awb);

            while (true)
            {
                using HttpRequestMessage request =
                    new HttpRequestMessage(HttpMethod.Get, url);

                using HttpResponseMessage response = client.Send(
                    request,
                    HttpCompletionOption.ResponseHeadersRead);

                int retrySeconds = ParseRetry(response);

                if (retrySeconds >= 0)
                {
                    if (retrySeconds > 0)
                    {
                        Tools.WriteDebug(
                            "AwbHttpClient::GetString",
                            $"HTTP {(int)response.StatusCode} and Retry-After " +
                            $"{retrySeconds}; pausing to allow retry");

                        Thread.Sleep(retrySeconds * 1000);
                    }

                    continue;
                }

                response.EnsureSuccessStatusCode();

                responseUrl =
                    response.RequestMessage?.RequestUri?.ToString() ?? url;

                using Stream responseStream =
                    response.Content.ReadAsStream();

                using StreamReader reader =
                    new StreamReader(responseStream, encoding);

                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Sends an HTTP POST request using form URL-encoded data.
        /// </summary>
        /// <param name="values">The form values to submit.</param>
        /// <param name="url">The destination URL.</param>
        /// <returns>The response body.</returns>
        public static string PostForm(
            NameValueCollection values,
            string url)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException(
                    "A URL is required.",
                    nameof(url));

            if (Globals.UnitTestMode)
            {
                throw new InvalidOperationException(
                    "You shouldn't access Wikipedia from unit tests.");
            }

            Tools.WriteDebug("AwbHttpClient::PostForm", url);

            string postData = Tools.BuildPostDataString(values);

            using HttpClient client = CreateClient(url);

            using StringContent content = new StringContent(
                postData,
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            using HttpResponseMessage response = client
                .PostAsync(url, content)
                .GetAwaiter()
                .GetResult();

            string responseText = response.Content
                .ReadAsStringAsync()
                .GetAwaiter()
                .GetResult();

            if (!response.IsSuccessStatusCode)
            {
                string action = values["action"] ?? "(unknown)";
                string title = values["title"];

                Tools.WriteDebug(
                    "AwbHttpClient::PostForm",
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} " +
                    $"for action '{action}'" +
                    (string.IsNullOrEmpty(title) ? string.Empty : $" on '{title}'") +
                    $" at '{url}'." +
                    Environment.NewLine +
                    $"Response: {responseText}");

                throw new HttpRequestException(
                    $"POST request failed with HTTP {(int)response.StatusCode} "
                    + $"{response.ReasonPhrase}.");
            }

            return responseText;
        }

        /// <summary>
        /// Refreshes the cached system proxy used by outgoing HTTP requests.
        /// The proxy is disabled when the current wiki URL bypasses it.
        /// </summary>
        public static void RefreshProxy()
        {
            IWebProxy proxy = HttpClient.DefaultProxy;

            if (proxy == null)
            {
                _systemProxy = null;
                return;
            }

            if (Uri.TryCreate(Variables.URL, UriKind.Absolute, out Uri wikiUri) &&
                proxy.IsBypassed(wikiUri))
            {
                _systemProxy = null;
                return;
            }

            proxy.Credentials = CredentialCache.DefaultCredentials;
            _systemProxy = proxy;
        }

        /// <summary>
        /// Creates a configured <see cref="HttpClient"/> using AWB's current
        /// proxy, cookies, decompression, credentials, timeout, and user-agent
        /// settings.
        /// </summary>
        /// <param name="url">
        /// The destination URL used to select the appropriate session cookies.
        /// </param>
        /// <param name="awb">
        /// Optional AutoWikiBrowser instance containing the current session.
        /// </param>
        /// <param name="userAgent">
        /// Optional explicit user-agent. When omitted, the appropriate AWB
        /// user-agent is selected from the supplied session.
        /// </param>
        /// <returns>A configured HTTP client.</returns>
        private static HttpClient CreateClient(
            string url,
            IAutoWikiBrowser awb = null,
            string userAgent = null)
        {
            CookieContainer cookies = Tools.GetCookieContainer(url, awb);

            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate,

                UseDefaultCredentials = true,

                Proxy = _systemProxy,
                UseProxy = _systemProxy != null,

                UseCookies = true,
                CookieContainer = cookies
            };

            HttpClient client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            string requestUserAgent = string.IsNullOrEmpty(userAgent)
                ? Tools.GetRequestUserAgent(awb)
                : userAgent;

            client.DefaultRequestHeaders.UserAgent.ParseAdd(requestUserAgent);

            return client;
        }

        /// <summary>
        /// Determines whether an HTTP response requests a retry.
        /// </summary>
        /// <param name="response">The HTTP response to inspect.</param>
        /// <returns>
        /// The number of seconds to wait, zero for an immediate retry,
        /// or -1 when no retry is requested.
        /// </returns>
        private static int ParseRetry(HttpResponseMessage response)
        {
            int statusCode = (int)response.StatusCode;
            var retryAfter = response.Headers.RetryAfter;

            if (statusCode != 429 &&
                statusCode != 503 &&
                retryAfter == null)
            {
                return -1;
            }

            int retrySeconds;

            if (retryAfter?.Delta != null)
            {
                retrySeconds = Convert.ToInt32(
                    Math.Ceiling(retryAfter.Delta.Value.TotalSeconds));
            }
            else if (retryAfter?.Date != null)
            {
                retrySeconds = Convert.ToInt32(
                    (retryAfter.Date.Value.UtcDateTime - DateTime.UtcNow)
                    .TotalSeconds);
            }
            else
            {
                retrySeconds = statusCode == 503 ? 60 : 5;
            }

            return Math.Max(retrySeconds, 0);
        }
    }
}