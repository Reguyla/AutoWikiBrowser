/*
Copyright (C) 2009 Max Semenik

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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Serialization;
using Twain.Core.API;

namespace Twain.Core;

/// <summary>
/// Stores core metadata and configuration information for a MediaWiki site.
/// </summary>
/// <remarks>
/// Instances contain the site's script path, namespaces, namespace aliases,
/// magic words, raw site-information response, and base URI. The class also
/// supports XML serialization for legacy configuration persistence.
/// </remarks>
[Serializable]
public class SiteInfo : IXmlSerializable
{
    /// <summary>
    /// API editor used to retrieve site metadata.
    /// </summary>
    private readonly IApiEdit Editor;

    /// <summary>
    /// Base MediaWiki script path in a form such as
    /// <c>https://en.wikipedia.org/w/</c>.
    /// </summary>
    private string scriptPath;

    /// <summary>
    /// Maps namespace identifiers to their canonical namespace names.
    /// </summary>
    private readonly Dictionary<int, string> namespaces = new();

    /// <summary>
    /// Maps namespace identifiers to their configured alias names.
    /// </summary>
    private Dictionary<int, List<string>> namespaceAliases = new();

    /// <summary>
    /// Maps MediaWiki magic-word identifiers to their recognized aliases.
    /// </summary>
    private readonly Dictionary<string, List<string>> magicWords = new();

    /// <summary>
    /// Raw site-information response used to populate the current instance.
    /// </summary>
    private string siteinfoOutput;

    /// <summary>
    /// Base URI associated with the current wiki.
    /// </summary>
    private readonly Uri uri;

    /// <summary>
    /// Initializes an empty <see cref="SiteInfo"/> instance for XML
    /// deserialization.
    /// </summary>
    internal SiteInfo()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SiteInfo"/> class and loads
    /// metadata for the wiki associated with the supplied API editor.
    /// </summary>
    /// <param name="editor">
    /// The API editor used to identify the wiki and retrieve its site metadata.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="editor"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="WikiUrlException">
    /// The wiki could not be identified, contacted, or initialized from the
    /// returned site information.
    /// </exception>
    /// <exception cref="ReadApiDeniedException">
    /// The wiki reports that read access to the API is not permitted.
    /// </exception>
    public SiteInfo(IApiEdit editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        Editor = editor;
        ScriptPath = editor.URL;
        uri = new Uri(ScriptPath);

        try
        {
            if (!LoadSiteInfo())
            {
                WikiException? apiException =
                    ParseErrorFromSiteInfoOutput();

                if (apiException != null)
                {
                    throw apiException;
                }

                throw new WikiUrlException();
            }
        }
        catch (Exception ex)
            when (ex is WikiException
                or WebException
                or HttpRequestException
                or UriChangedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WikiUrlException(ex);
        }
    }

    /// <summary>
    /// Builds the object-cache key used to store site information for a wiki.
    /// </summary>
    /// <param name="scriptPath">
    /// The normalized MediaWiki script path associated with the site.
    /// </param>
    /// <returns>
    /// The cache key used for the corresponding <see cref="SiteInfo"/> instance.
    /// </returns>
    private static string Key(string scriptPath)
    {
        return $"SiteInfo({scriptPath})@";
    }

    /// <summary>
    /// Returns cached site information for the supplied API editor when a valid
    /// cached entry exists; otherwise, retrieves and caches fresh site information.
    /// </summary>
    /// <param name="editor">
    /// The API editor identifying the wiki whose site information is required.
    /// </param>
    /// <returns>
    /// A valid <see cref="SiteInfo"/> instance for the editor's wiki.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="editor"/> is <see langword="null"/>.
    /// </exception>
    public static SiteInfo CreateOrLoad(IApiEdit editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        string cacheKey = Key(editor.URL);

        SiteInfo? siteInfo =
            ObjectCache.Global.Get<SiteInfo>(cacheKey) as SiteInfo;

        if (siteInfo != null &&
            Namespace.VerifyNamespaces(siteInfo.Namespaces))
        {
            return siteInfo;
        }

        siteInfo = new SiteInfo(editor);

        ObjectCache.Global[cacheKey] = siteInfo;

        return siteInfo;
    }

    /// <summary>
    /// Gets the API path in format https://en.wikipedia.org/w/api.php or https://en.wikipedia.org/w/api.php5
    /// </summary>
    /// <value>The API path.</value>
    private string ApiPath
    {
        get { return scriptPath + "api.php" + (Editor.PHP5 ? "5" : ""); }
    }

    /// <summary>
    /// Ensures URL ends with /
    /// </summary>
    /// <returns>The updated URL</returns>
    /// <param name="url">URL.</param>
    public static string NormalizeURL(string url)
    {
        return !url.EndsWith("/") ? url + "/" : url;
    }

    /// <summary>
    /// Loads siteinfo XML from Global cache on disk if available
    /// </summary>
    /// <returns><c>true</c>, if loaded from cache successfully, <c>false</c> otherwise.</returns>
    private bool LoadFromCache()
    {
        var cacheResult = ObjectCache.Global.Get<string>("SiteInfo:" + scriptPath);

        // simple (string) cast of line above fails under Mono so do more verbosely
        siteinfoOutput = (cacheResult == null ? "" : cacheResult.ToString());

        return !string.IsNullOrEmpty(siteinfoOutput);
    }

    /// <summary>
    /// Retrieves MediaWiki site-information XML from the current wiki.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when site information was retrieved successfully;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Successful responses are cached for later reuse. A missing response or a
    /// MediaWiki <c>readapidenied</c> error is treated as a failed load so the
    /// caller can handle private wikis that require authentication before API
    /// queries are permitted.
    /// </remarks>
    private bool LoadFromNetwork()
    {
        siteinfoOutput =
            Editor.HttpGet(
                ApiPath +
                "?action=query&meta=siteinfo" +
                "&siprop=general|namespaces|namespacealiases|statistics|magicwords" +
                "&format=xml");

        if (string.IsNullOrEmpty(siteinfoOutput) ||
            siteinfoOutput.Contains(
                "readapidenied",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        ObjectCache.Global.Set(
            "SiteInfo:" + scriptPath,
            siteinfoOutput);

        return true;
    }

    /// <summary>
    /// Loads site information from the local cache or MediaWiki API and applies
    /// the returned configuration to the current <see cref="SiteInfo"/> instance.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the site-information response was loaded and
    /// parsed successfully; otherwise, <see langword="false"/>.
    /// </returns>
    public bool LoadSiteInfo()
    {
        if (!LoadFromCache())
        {
            LoadFromNetwork();
        }

        XmlDocument document = new();
        document.LoadXml(siteinfoOutput);

        XmlElement? query = GetSiteInfoQueryElement(document);

        if (query == null)
        {
            return false;
        }

        if (!LoadGeneralSiteInformation(query))
        {
            return false;
        }

        if (!LoadNamespaces(query))
        {
            return false;
        }

        if (!LoadMagicWords(query))
        {
            return false;
        }

        LoadAWBTag();

        return true;
    }

    /// <summary>
    /// Retrieves the MediaWiki <c>query</c> element from a site-information
    /// response.
    /// </summary>
    /// <param name="document">
    /// The parsed site-information XML document.
    /// </param>
    /// <returns>
    /// The <c>query</c> element when the expected response structure is present;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    private static XmlElement? GetSiteInfoQueryElement(
        XmlDocument document)
    {
        XmlElement? api = document["api"];

        if (api == null)
        {
            return null;
        }

        return api["query"];
    }

    /// <summary>
    /// Loads general wiki metadata from the MediaWiki site-information response.
    /// </summary>
    /// <param name="query">
    /// The MediaWiki <c>query</c> element containing site information.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the required general metadata is present;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool LoadGeneralSiteInformation(
        XmlElement query)
    {
        XmlElement? general = query["general"];

        if (general == null)
        {
            return false;
        }

        ArticleUrl =
            Host +
            general.GetAttribute("articlepath");

        Language =
            general.GetAttribute("lang");

        IsRightToLeft =
            general.HasAttribute("rtl");

        CapitalizeFirstLetter =
            general.GetAttribute("case") == "first-letter";

        MediaWikiVersion =
            general.GetAttribute("generator")
                .Replace(
                    "MediaWiki ",
                    string.Empty);

        CategoryCollation =
            general.GetAttribute("categorycollation");

        return true;
    }

    /// <summary>
    /// Loads namespace names and aliases from the MediaWiki site-information
    /// response.
    /// </summary>
    /// <param name="query">
    /// The MediaWiki <c>query</c> element containing namespace metadata.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when namespace information was loaded successfully;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="Exception">
    /// The returned namespace collection does not contain the required namespaces.
    /// </exception>
    private bool LoadNamespaces(
        XmlElement query)
    {
        XmlElement? namespacesElement =
            query["namespaces"];

        XmlElement? aliasesElement =
            query["namespacealiases"];

        if (namespacesElement == null ||
            aliasesElement == null)
        {
            return false;
        }

        foreach (XmlNode namespaceNode in
                 namespacesElement.GetElementsByTagName("ns"))
        {
            int id =
                int.Parse(
                    namespaceNode.Attributes!["id"]!.Value,
                    CultureInfo.InvariantCulture);

            if (id != 0)
            {
                namespaces[id] =
                    namespaceNode.InnerText + ":";
            }
        }

        if (!Namespace.VerifyNamespaces(namespaces))
        {
            throw new Exception(
                "Error loading namespaces from " +
                ApiPath);
        }

        namespaceAliases =
            Variables.PrepareAliases(namespaces);

        foreach (XmlNode aliasNode in
                 aliasesElement.GetElementsByTagName("ns"))
        {
            int id =
                int.Parse(
                    aliasNode.Attributes!["id"]!.Value,
                    CultureInfo.InvariantCulture);

            if (id != 0 &&
                Variables.Namespaces.ContainsKey(id))
            {
                namespaceAliases[id].Add(
                    aliasNode.InnerText);
            }
        }

        return true;
    }

    /// <summary>
    /// Loads MediaWiki magic-word aliases from the site-information response.
    /// </summary>
    /// <param name="query">
    /// The MediaWiki <c>query</c> element containing magic-word metadata.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when magic-word information was present and loaded;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool LoadMagicWords(
        XmlElement query)
    {
        XmlElement? magicWordsElement =
            query["magicwords"];

        if (magicWordsElement == null)
        {
            return false;
        }

        foreach (XmlNode magicWordNode in
                 magicWordsElement.GetElementsByTagName("magicword"))
        {
            List<string> aliases = new();

            XmlNode? aliasesNode =
                magicWordNode["aliases"];

            if (aliasesNode != null)
            {
                foreach (XmlNode aliasNode in
                         aliasesNode.ChildNodes)
                {
                    if (aliasNode.Name == "alias")
                    {
                        aliases.Add(
                            aliasNode.InnerText);
                    }
                }
            }

            string? name =
                magicWordNode.Attributes?["name"]?.Value;

            if (!string.IsNullOrEmpty(name))
            {
                magicWords[name] = aliases;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the current wiki defines an active edit tag named
    /// <c>AWB</c> in Special:Tags.
    /// </summary>
    /// <remarks>
    /// A positive result is cached globally. Negative or unknown results are
    /// rechecked so newly created tags can be detected without restarting the
    /// application.
    /// </remarks>
    private void LoadAWBTag()
    {
        bool? awbTagDefined =
            (bool?)ObjectCache.Global.Get<bool>(
                "AWBTagDefined:" + scriptPath);

        // Recheck false or unknown results in case the tag has since been created.
        if (awbTagDefined is null or false)
        {
            string response =
                Editor.HttpGet(
                    ApiPath +
                    "?format=json&action=query&list=tags&tgprop=active&tglimit=max");

            if (string.IsNullOrWhiteSpace(response))
            {
                return;
            }

            awbTagDefined =
                IsAwbTagDefined(response);

            if (!awbTagDefined.HasValue)
            {
                return;
            }

            ObjectCache.Global.Set(
                "AWBTagDefined:" + scriptPath,
                awbTagDefined.Value);
        }

        IsAWBTagDefined = awbTagDefined == true;
    }

    /// <summary>
    /// Parses a MediaWiki tags API response and determines whether it contains an
    /// active edit tag named <c>AWB</c>.
    /// </summary>
    /// <param name="responseJson">
    /// The JSON response returned by the MediaWiki tags API.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when an active <c>AWB</c> tag is present;
    /// <see langword="false"/> when the response is valid but no active tag is
    /// present; or <see langword="null"/> when the response cannot be parsed or
    /// does not contain the expected API structure.
    /// </returns>
    private static bool? IsAwbTagDefined(
        string responseJson)
    {
        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    responseJson,
                    new JsonDocumentOptions
                    {
                        MaxDepth = 32
                    });

            JsonElement root =
                document.RootElement;

            if (root.TryGetProperty(
                    "error",
                    out _))
            {
                return null;
            }

            if (!root.TryGetProperty(
                    "query",
                    out JsonElement query) ||
                !query.TryGetProperty(
                    "tags",
                    out JsonElement tags) ||
                tags.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return tags.EnumerateArray()
                .Any(tag =>
                    tag.ValueKind == JsonValueKind.Object &&
                    tag.TryGetProperty(
                        "name",
                        out JsonElement name) &&
                    name.ValueKind == JsonValueKind.String &&
                    string.Equals(
                        name.GetString(),
                        "AWB",
                        StringComparison.Ordinal) &&
                    tag.TryGetProperty(
                        "active",
                        out _));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Examines the stored site-information response for a recognized MediaWiki
    /// API error.
    /// </summary>
    /// <returns>
    /// A corresponding <see cref="WikiException"/> when the API response contains
    /// a recognized error code; otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="XmlException">
    /// The stored site-information response is not valid XML.
    /// </exception>
    private WikiException? ParseErrorFromSiteInfoOutput()
    {
        if (string.IsNullOrEmpty(siteinfoOutput))
        {
            return null;
        }

        XmlDocument document = new();
        document.LoadXml(siteinfoOutput);

        XmlElement? error =
            document["api"]?["error"];

        if (error == null)
        {
            return null;
        }

        string errorCode =
            error.GetAttribute("code");

        return errorCode switch
        {
            "readapidenied" => new ReadApiDeniedException(),
            _ => null
        };
    }

    /// <summary>
    /// Gets or sets the base MediaWiki script path for the current wiki.
    /// </summary>
    /// <value>
    /// A normalized script path in a form such as
    /// <c>https://en.wikipedia.org/w/</c>.
    /// </value>
    /// <remarks>
    /// The setter must remain public because the legacy object-cache serializer
    /// requires public property setters when restoring <see cref="SiteInfo"/>
    /// instances.
    /// </remarks>
    public string ScriptPath
    {
        get => scriptPath;
        set => scriptPath = NormalizeURL(value);
    }

    /// <summary>
    /// Gets the scheme and host portion of the current wiki URI.
    /// </summary>
    /// <value>
    /// The wiki origin, such as <c>https://en.wikipedia.org</c>.
    /// </value>
    public string Host =>
        uri.Scheme +
        Uri.SchemeDelimiter +
        uri.Host;

    /// <summary>
    /// Contains namespaces for this wiki mapped by their IDs
    /// </summary>
    public Dictionary<int, string> Namespaces
    { get { return namespaces; } }

    /// <summary>
    /// Alternative names of namespaces
    /// </summary>
    public Dictionary<int, List<string>> NamespaceAliases
    { get { return namespaceAliases; } }

    /// <summary>
    /// Magic words used by parser, with alternative variants
    /// </summary>
    public Dictionary<string, List<string>> MagicWords
    { get { return magicWords; } }

    /// <summary>
    /// Prettified URL of pages on server, $1 should be replaced with page title
    /// </summary>
    public string ArticleUrl
    { get; private set; }

    /// <summary>
    /// Version of MediaWiki this site is running on
    /// </summary>
    public string MediaWikiVersion { get; private set; }

    /// <summary>
    /// ISO code of current language
    /// </summary>
    public string Language
    { get; private set; }

    /// <summary>
    /// Is the wiki RTL?
    /// </summary>
    public bool IsRightToLeft
    { get; private set; }

    public bool CapitalizeFirstLetter
    { get; private set; }

    /// <summary>
    /// Category Collation ($wgCategoryCollation) of the wiki
    /// </summary>
    public string CategoryCollation { get; private set; }

    /// <summary>
    /// Returns whether an AWB tag has been defined on Special:Tags
    /// </summary>
    public bool IsAWBTagDefined { get; private set; }

    /// <summary>
    /// Retrieves localized MediaWiki system messages for the current wiki.
    /// </summary>
    /// <param name="names">
    /// The names of the MediaWiki messages to retrieve.
    /// </param>
    /// <returns>
    /// A dictionary keyed by message name containing the localized message text.
    /// Returns an empty dictionary when no usable messages are returned.
    /// </returns>
    /// <remarks>
    /// This method is used only when the wiki language is not English. Localized
    /// messages are optional during initialization, so missing or malformed
    /// message data falls back to the default English messages.
    /// </remarks>
    public Dictionary<string, string> GetMessages(
        params string[] names)
    {
        if (names.Length == 0)
        {
            return [];
        }

        string messageNames =
            Uri.EscapeDataString(
                string.Join("|", names));

        string response =
            Editor.HttpGet(
                $"{ApiPath}?format=json&action=query&meta=allmessages" +
                $"&continue=&ammessages={messageNames}");

        return ParseMessagesResponse(response);
    }

    /// <summary>
    /// Parses a MediaWiki <c>allmessages</c> API response into a dictionary of
    /// localized system messages.
    /// </summary>
    /// <param name="response">
    /// The JSON response returned by the MediaWiki API.
    /// </param>
    /// <returns>
    /// A dictionary keyed by message name containing the localized message text.
    /// An empty dictionary is returned when the response is empty, malformed,
    /// contains an API error, or does not contain the expected message data.
    /// </returns>
    private static Dictionary<string, string> ParseMessagesResponse(
        string response)
    {
        if (!TryParseJsonObject(
                response,
                "The allmessages API response",
                out JsonObject? json))
        {
            return [];
        }

        if (json["error"] != null)
        {
            return [];
        }

        if (json["query"] is not JsonObject query ||
            query["allmessages"] is not JsonArray messages)
        {
            return [];
        }

        Dictionary<string, string> result = new();

        foreach (JsonObject message in
                 messages.OfType<JsonObject>())
        {
            string? name =
                TryGetStringValue(
                    message,
                    "name");

            string? text =
                TryGetStringValue(
                    message,
                    "*");

            if (string.IsNullOrEmpty(name) ||
                text == null)
            {
                continue;
            }

            result[name] = text;
        }

        return result;
    }

    /// <summary>
    /// Reads a string value from a JSON object when the specified property contains
    /// a compatible JSON string value.
    /// </summary>
    /// <param name="jsonObject">
    /// The JSON object containing the property.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property to read.
    /// </param>
    /// <returns>
    /// The string value when present and valid; otherwise,
    /// <see langword="null"/>.
    /// </returns>
    private static string? TryGetStringValue(
        JsonObject jsonObject,
        string propertyName)
    {
        return jsonObject[propertyName] is JsonValue value &&
               value.TryGetValue(out string? result)
            ? result
            : null;
    }

    /// <summary>
    /// Attempts to parse JSON text as an object using a bounded maximum depth.
    /// </summary>
    /// <param name="jsonText">The JSON text to parse.</param>
    /// <param name="sourceName">
    /// A descriptive name for the JSON source used in debug output.
    /// </param>
    /// <param name="json">
    /// Contains the parsed JSON object when parsing succeeds; otherwise
    /// <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> when the text contains a valid JSON object; otherwise
    /// <c>false</c>.
    /// </returns>
    private static bool TryParseJsonObject(
        string jsonText,
        string sourceName,
        out JsonObject? json)
    {
        json = null;

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Tools.WriteDebug(
                nameof(GetMessages),
                sourceName + " returned no JSON content.");

            return false;
        }

        try
        {
            JsonNode node =
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
                    nameof(GetMessages),
                    sourceName + " did not contain a JSON object.");

                return false;
            }

            json = jsonObject;
            return true;
        }
        catch (JsonException ex)
        {
            Tools.WriteDebug(
                nameof(GetMessages),
                sourceName + " contained invalid JSON: " + ex.Message);

            return false;
        }
    }

    #region Helpers
    /// <summary>
    /// Opens the specified wiki page in the user's default web browser.
    /// </summary>
    /// <param name="title">
    /// The title of the wiki page to open.
    /// </param>
    public void OpenPageInBrowser(string title)
    {
        Tools.OpenArticleInBrowser(title);
    }

    /// <summary>
    /// Opens the revision history for the specified wiki page in the user's
    /// default web browser.
    /// </summary>
    /// <param name="title">
    /// The title of the wiki page whose history should be opened.
    /// </param>
    public void OpenPageHistoryInBrowser(string title)
    {
        Tools.OpenArticleHistoryInBrowser(title);
    }

    #endregion

    #region IXmlSerializable Members

    /// <summary>
    /// Returns the XML schema associated with this type.
    /// </summary>
    /// <returns>
    /// Always <see langword="null"/>, because this type does not expose a custom
    /// XML schema.
    /// </returns>
    public System.Xml.Schema.XmlSchema? GetSchema()
    {
        return null;
    }

    /// <summary>
    /// Reads the XML representation of this instance.
    /// </summary>
    /// <param name="reader">
    /// The XML reader positioned at the serialized representation.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// XML deserialization is not implemented for <see cref="SiteInfo"/>.
    /// </exception>
    public void ReadXml(XmlReader reader)
    {
        throw new NotSupportedException(
            "XML deserialization is not implemented for SiteInfo.");
    }

    /// <summary>
    /// Writes the XML representation of this instance.
    /// </summary>
    /// <param name="writer">
    /// The XML writer used to serialize the current site information.
    /// </param>
    /// <remarks>
    /// The serialized data currently includes the normalized wiki script URL,
    /// the legacy PHP5 capability flag, and the configured namespace mappings.
    /// The surrounding object element is written by the serializer.
    /// </remarks>
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("url", scriptPath);

        writer.WriteAttributeString("php5", Editor.PHP5 ? "1" : "0");

        writer.WriteStartElement("Namespaces");

        foreach (KeyValuePair<int, string> pair in namespaces)
        {
            writer.WriteStartElement("Namespace");

            writer.WriteAttributeString("id",
                pair.Key.ToString(
                    CultureInfo.InvariantCulture));

            writer.WriteValue(pair.Value);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    #endregion
}
    /// <summary>
    /// Provides the base class for exceptions representing errors encountered
    /// while interacting with a wiki.
    /// </summary>
    public abstract class WikiException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WikiException"/> class
        /// with the specified error message.
        /// </summary>
        /// <param name="text">
        /// The message that describes the error.
        /// </param>
        protected WikiException(string text)
            : base(text)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WikiException"/> class
        /// with the specified error message and underlying exception.
        /// </summary>
        /// <param name="text">
        /// The message that describes the error.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the current exception.
        /// </param>
        protected WikiException(
            string text,
            Exception innerException)
            : base(text, innerException)
        {
        }
    }

    /// <summary>
    /// Represents an error encountered while connecting to or resolving a
    /// configured wiki site.
    /// </summary>
    public class WikiUrlException : WikiException
    {
        private const string ExceptionMessage =
            "Can't connect to given wiki site.";

        /// <summary>
        /// Initializes a new instance of the <see cref="WikiUrlException"/> class.
        /// </summary>
        public WikiUrlException()
            : base(ExceptionMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WikiUrlException"/> class
        /// with the exception that caused the connection failure.
        /// </summary>
        /// <param name="innerException">
        /// The exception that caused the current exception.
        /// </param>
        public WikiUrlException(Exception innerException)
            : base(ExceptionMessage, innerException)
        {
        }
    }

    /// <summary>
    /// Represents an error returned when the current user does not have
    /// permission to read from the MediaWiki API.
    /// </summary>
    public class ReadApiDeniedException : WikiException
    {
        private const string ExceptionMessage =
            "Read permission is required to use this module.";

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="ReadApiDeniedException"/> class.
        /// </summary>
        public ReadApiDeniedException()
            : base(ExceptionMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="ReadApiDeniedException"/> class with the exception that
        /// caused the permission failure.
        /// </summary>
        /// <param name="innerException">
        /// The exception that caused the current exception.
        /// </param>
        public ReadApiDeniedException(Exception innerException)
            : base(ExceptionMessage, innerException)
        {
        }
    }