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
    /// Creates an instance of the class
    /// </summary>
    public SiteInfo(IApiEdit editor)
    {
        Editor = editor;
        ScriptPath = editor.URL;
        uri = new Uri(ScriptPath);

        try
        {
            if (!LoadSiteInfo())
            {
                var ret = ParseErrorFromSiteInfoOutput();
                if (ret is bool && !(bool)ret)
                {
                    throw new WikiUrlException();
                }

                var ex = ret as Exception;
                if (ex != null)
                {
                    throw ex;
                }
            }
        }
        catch (WikiException)
        {
            throw;
        }
        catch (Exception ex)
            when (ex is WebException or HttpRequestException)
        {
            throw;
        }
        catch (UriChangedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WikiUrlException(ex);
        }
    }

    /// <summary>
    /// For object caching support
    /// </summary>
    private static string Key(string scriptPath)
    {
        return "SiteInfo(" + scriptPath + ")@";
    }

    public static SiteInfo CreateOrLoad(IApiEdit editor)
    {
        SiteInfo si = (SiteInfo)ObjectCache.Global.Get<SiteInfo>(Key(editor.URL));
        if (si != null
            && Namespace.VerifyNamespaces(si.Namespaces))
        {
            return si;
        }

        si = new SiteInfo(editor);
        ObjectCache.Global[Key(editor.URL)] = si;

        return si;
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
    /// Loads siteinfo XML from network via API call
    /// </summary>
    /// <returns><c>true</c>, if loaded from network successfully, <c>false</c> otherwise.</returns>
    private bool LoadFromNetwork()
    {
        siteinfoOutput = Editor.HttpGet(ApiPath + "?action=query&meta=siteinfo&siprop=general|namespaces|namespacealiases|statistics|magicwords&format=xml");

        // readapidenied API error check for private wikis that require login for any query
        if (string.IsNullOrEmpty(siteinfoOutput) || siteinfoOutput.Contains("readapidenied"))
            return false;

        // cache successful result
        ObjectCache.Global.Set("SiteInfo:" + scriptPath, siteinfoOutput);

        return true;
    }

    /// <summary>
    /// Loads SiteInfo from local cache or API call, processes data returned
    /// </summary>
    /// <returns></returns>
    public bool LoadSiteInfo()
    {
        if (!LoadFromCache())
            LoadFromNetwork();

        XmlDocument xd = new XmlDocument();
        xd.LoadXml(siteinfoOutput);

        var api = xd["api"];
        if (api == null) return false;

        var query = api["query"];
        if (query == null) return false;

        var general = query["general"];
        if (general == null) return false;

        ArticleUrl = Host + general.GetAttribute("articlepath");
        Language = general.GetAttribute("lang");
        IsRightToLeft = general.HasAttribute("rtl");
        CapitalizeFirstLetter = general.GetAttribute("case") == "first-letter";
        MediaWikiVersion = general.GetAttribute("generator").Replace("MediaWiki ", "");

        CategoryCollation = general.GetAttribute("categorycollation");

        if (query["namespaces"] == null || query["namespacealiases"] == null)
            return false;

        foreach (XmlNode xn in query["namespaces"].GetElementsByTagName("ns"))
        {
            int id = int.Parse(xn.Attributes["id"].Value, CultureInfo.InvariantCulture);

            if (id != 0) namespaces[id] = xn.InnerText + ":";
        }

        if (!Namespace.VerifyNamespaces(namespaces))
            throw new Exception("Error loading namespaces from " + ApiPath);

        namespaceAliases = Variables.PrepareAliases(namespaces);

        foreach (XmlNode xn in query["namespacealiases"].GetElementsByTagName("ns"))
        {
            int id = int.Parse(xn.Attributes["id"].Value, CultureInfo.InvariantCulture);

            if (id != 0 && Variables.Namespaces.ContainsKey(id)) namespaceAliases[id].Add(xn.InnerText);
        }

        if (query["magicwords"] == null)
            return false;

        foreach (XmlNode xn in query["magicwords"].GetElementsByTagName("magicword"))
        {
            List<string> alias = new();

            foreach (XmlNode xin in xn["aliases"].GetElementsByTagName("alias"))
            {
                alias.Add(xin.InnerText);
            }

            magicWords.Add(xn.Attributes["name"].Value, alias);
        }

        LoadAWBTag();

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

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(
                        response,
                        new JsonDocumentOptions
                        {
                            MaxDepth = 32
                        });

                JsonElement root = document.RootElement;

                if (root.TryGetProperty("error", out _))
                {
                    return;
                }

                if (!root.TryGetProperty(
                        "query",
                        out JsonElement query) ||
                    !query.TryGetProperty(
                        "tags",
                        out JsonElement tags) ||
                    tags.ValueKind != JsonValueKind.Array)
                {
                    return;
                }

                awbTagDefined =
                    tags.EnumerateArray()
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

                ObjectCache.Global.Set(
                    "AWBTagDefined:" + scriptPath,
                    awbTagDefined.Value);
            }
            catch (JsonException)
            {
                return;
            }
        }

        IsAWBTagDefined = awbTagDefined == true;
    }

    // TODO: Replace the object return type with a structured site-info error result.
    // This method currently returns false when no recognized API error is present,
    // true when an unclassified API error is present, or a WikiException for a
    // recognized error code. Review all callers before changing this legacy contract.

    /// <summary>
    /// Examines the stored site-information response for a MediaWiki API error.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when no API error is present or the error code is
    /// not recognized; <see langword="true"/> when an API error is present without
    /// a recognized code; or a corresponding <see cref="WikiException"/> when the
    /// error code maps to a known wiki error.
    /// </returns>
    public object ParseErrorFromSiteInfoOutput()
    {
        if (string.IsNullOrEmpty(siteinfoOutput))
        {
            return false;
        }

        XmlDocument document = new();
        document.LoadXml(siteinfoOutput);

        XmlElement? api = document["api"];

        if (api == null)
        {
            return false;
        }

        XmlElement? error = api["error"];

        if (error == null)
        {
            return false;
        }

        string errorCode = error.GetAttribute("code");

        if (string.IsNullOrEmpty(errorCode))
        {
            return true;
        }

        return errorCode switch
        {
            "readapidenied" => new ReadApiDeniedException(),
            _ => false
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
    /// Returns an empty dictionary when the messages cannot be retrieved or parsed.
    /// </returns>
    /// <remarks>
    /// This method is used only when the wiki language is not English. Localized
    /// messages are optional during initialization, so API failures fall back to
    /// the default English messages.
    /// </remarks>
    public Dictionary<string, string> GetMessages(params string[] names)
    {
        if (names.Length == 0)
        {
            return new();
        }

        string messageNames =
            Uri.EscapeDataString(
                string.Join("|", names));

        string response =
            Editor.HttpGet(
                $"{ApiPath}?format=json&action=query&meta=allmessages" +
                $"&continue=&ammessages={messageNames}");

        if (!TryParseJsonObject(
                response,
                "The allmessages API response",
                out JsonObject? json))
        {
            return new();
        }

        if (json["error"] != null)
        {
            return new();
        }

        if (json["query"] is not JsonObject query ||
            query["allmessages"] is not JsonArray messages)
        {
            return new();
        }

        Dictionary<string, string> result = new();

        foreach (JsonObject message in messages.OfType<JsonObject>())
        {
            string? name =
                message["name"]?.GetValue<string>();

            string? text =
                message["*"]?.GetValue<string>();

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
    public void OpenPageInBrowser(string title)
    {
        Tools.OpenArticleInBrowser(title);
    }

    public void OpenPageHistoryInBrowser(string title)
    {
        Tools.OpenArticleHistoryInBrowser(title);
    }

    #endregion

    #region IXmlSerializable Members

    public System.Xml.Schema.XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public void WriteXml(XmlWriter writer)
    {
        // writer.WriteStartElement("site");
        writer.WriteAttributeString("url", scriptPath);
        writer.WriteAttributeString("php5", Editor.PHP5 ? "1" : "0");
        {
            writer.WriteStartElement("Namespaces");
            {
                foreach (KeyValuePair<int, string> p in namespaces)
                {
                    writer.WriteStartElement("Namespace");
                    writer.WriteAttributeString("id", p.Key.ToString());
                    writer.WriteValue(p.Value);
                    writer.WriteEndElement();
                }
            }
        }
        // writer.WriteEndElement();
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