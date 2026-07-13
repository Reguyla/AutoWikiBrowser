using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
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
            throw new NotImplementedException(
                "HTTP GET migration has not been implemented yet.");
        }

        /// <summary>
        /// Sends an HTTP POST request using
        /// application/x-www-form-urlencoded data.
        /// </summary>
        /// <param name="values">Form values to send.</param>
        /// <param name="url">Destination URL.</param>
        /// <returns>The response body.</returns>
        public static string PostForm(
            NameValueCollection values,
            string url)
        {
            throw new NotImplementedException(
               "HTTP form POST migration has not been implemented yet.");
        }

        /// <summary>
        /// Refreshes the cached system proxy used by outgoing HTTP requests.
        /// The proxy is disabled when the current wiki URL bypasses it.
        /// </summary>
        public static void RefreshProxy()
        {
            IWebProxy proxy = HttpClient.DefaultProxy;

            if (proxy == null ||
                proxy.IsBypassed(new Uri(Variables.URL)))
            {
                _systemProxy = null;
                return;
            }

            proxy.Credentials = CredentialCache.DefaultCredentials;
            _systemProxy = proxy;
        }

        /// <summary>
        /// Creates a configured <see cref="HttpClient"/> using AWB's current
        /// proxy, decompression, credential, timeout, and user-agent settings.
        /// </summary>
        /// <param name="userAgent">
        /// Optional user-agent value. The standard AWB user agent is used when omitted.
        /// </param>
        /// <returns>A configured HTTP client.</returns>
        private static HttpClient CreateClient(string userAgent = null)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate,

                UseDefaultCredentials = true,

                Proxy = _systemProxy,
                UseProxy = _systemProxy != null
            };

            HttpClient client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                string.IsNullOrEmpty(userAgent)
                    ? Tools.DefaultUserAgentString
                    : userAgent);

            return client;
        }
    }
}