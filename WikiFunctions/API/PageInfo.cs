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

using System.Xml;

namespace WikiFunctions.API;

/// <summary>
/// This class represents information about the page currently being edited
/// </summary>
public sealed class PageInfo
{
    internal PageInfo()
    {
    }

    // TODO: adopt for retrieval of information for protection, deletion, etc.
    internal PageInfo(string xml)
        : this(CreateXmlDocument(xml))
    {
    }

    internal PageInfo(XmlDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        using XmlReader reader = new XmlNodeReader(doc);

        string normalizedFrom = null, redirectFrom = null;

        var redirects = doc.GetElementsByTagName("r");

        if (redirects.Count > 0) // We have redirects
        {
            var first = redirects[0].Attributes;
            var last = redirects[redirects.Count - 1].Attributes;
            if (first != null && last != null && (first["from"].Value == last["to"].Value ||
                                                  last["from"].Value == last["to"].Value))
            {
                // Redirect loop
                TitleChangedStatus = PageTitleStatus.RedirectLoop;
                OriginalTitle = Title = first["from"].Value;
                Exists = true;
                Text = "";
                return; // We're not going to have any page text as there is a redirect loop
            }

            redirectFrom = first != null ? first["from"].Value : "";
            // Valid redirects
            TitleChangedStatus = redirects.Count == 1
                                     ? PageTitleStatus.Redirected
                                     : PageTitleStatus.MultipleRedirects;
        }
        else
        {
            TitleChangedStatus = PageTitleStatus.NoChange;
        }

        string currentTimestamp = "";
        if (reader.ReadToFollowing("api"))
            currentTimestamp = reader.GetAttribute("currentTimestamp");

        if (!reader.ReadToFollowing("page"))
        {
            if (redirects.Count > 0)
            {
                // If there are redirects, but no page element, chances are it's a redirect to IW or something
                // similar
                return;
            }
            throw new Exception("Cannot find <page> element");
        }

        // Normalised before redirect, so would be root. Could still be multiple redirects, or looped
        var normalized = doc.GetElementsByTagName("n");

        if (normalized.Count > 0 && normalized[0].Attributes != null)
        {
            normalizedFrom = normalized[0].Attributes["from"].Value;

            if (TitleChangedStatus == PageTitleStatus.NoChange)
                TitleChangedStatus = PageTitleStatus.Normalised;
            else
                TitleChangedStatus |= PageTitleStatus.Normalised;
        }

        // Normalization occurs before redirection, so if that exists, that is the title passed to the API
        if (!string.IsNullOrEmpty(normalizedFrom))
        {
            OriginalTitle = normalizedFrom;
        }
        else if (!string.IsNullOrEmpty(redirectFrom))
        {
            OriginalTitle = redirectFrom;
        }

        Exists = (reader.GetAttribute("missing") == null); //if null, page exists
        IsWatched = (reader.GetAttribute("watched") != null);

        var tokens = doc.GetElementsByTagName("tokens");
        if (tokens.Count == 0)
        {
            // Token support for < 1.24
            EditToken = reader.GetAttribute("edittoken");
            ProtectToken = reader.GetAttribute("protecttoken");
            DeleteToken = reader.GetAttribute("deletetoken");
            MoveToken = reader.GetAttribute("movetoken");
            WatchToken = reader.GetAttribute("watchtoken");
        }
        else if (tokens[0].Attributes != null)
        {
            EditToken = tokens[0].Attributes["csrftoken"].Value;
            ProtectToken = tokens[0].Attributes["csrftoken"].Value;
            DeleteToken = tokens[0].Attributes["csrftoken"].Value;
            MoveToken = tokens[0].Attributes["csrftoken"].Value;
            WatchToken = tokens[0].Attributes["watchtoken"].Value;
            RollbackToken = tokens[0].Attributes["rollbacktoken"].Value;
        }

        // if UseInToken = false then won't be given starttimestamp, so use currentTimestamp instead
        TokenTimestamp = reader.GetAttribute("starttimestamp");
        if (string.IsNullOrEmpty(TokenTimestamp))
            TokenTimestamp = currentTimestamp;

        long revisionId;
        RevisionID = long.TryParse(reader.GetAttribute("lastrevisionId"), out revisionId) ? revisionId : -1;

        Title = reader.GetAttribute("title");
        DisplayTitle = reader.GetAttribute("displaytitle");
        var ns = reader.GetAttribute("ns");
        NamespaceID = ns != null ? int.Parse(ns) : 0;

        if (reader.ReadToDescendant("protection") && !reader.IsEmptyElement)
        {
            foreach (XmlNode protectionNode in doc.GetElementsByTagName("pr"))
            {
                switch (protectionNode.Attributes["type"].Value)
                {
                    case "edit":
                        EditProtection = protectionNode.Attributes["level"].Value;
                        break;
                    case "move":
                        MoveProtection = protectionNode.Attributes["level"].Value;
                        break;
                    case "create":
                        CreateProtection = protectionNode.Attributes["level"].Value;
                        break;
                }
            }
        }

        reader.ReadToFollowing("revisions");

        reader.ReadToDescendant("rev");
        Timestamp = reader.GetAttribute("timestamp");

        // API returns \n line endings, we have standardized on \r\n (including under Mono)
        Text = reader.ReadString().Replace("\n", "\r\n");
    }

    /// <summary>
    /// Display title of the Page in HTML format, used e.g. if page has some italics (using {{italic title}} etc.)
    /// </summary>
    public string DisplayTitle
    { get; private set; }

    /// <summary>
    /// Title of the Page
    /// </summary>
    public string Title
    { get; private set; }

    /// <summary>
    /// Original title (before redirects/normalization) of the Page
    /// </summary>
    public string OriginalTitle
    { get; private set; }

    /// <summary>
    /// Why OriginalTitle differs from Title
    /// </summary>
    public PageTitleStatus TitleChangedStatus
    { get; private set; }

    /// <summary>
    /// Text of the Page
    /// </summary>
    public string Text
    { get; private set; }

    /// <summary>
    /// Whether the page exists or not
    /// </summary>
    public bool Exists
    { get; private set; }

    /// <summary>
    /// Revision ID, -1 if N/A
    /// </summary>
    public long RevisionID
    { get; private set; }

    /// <summary>
    /// Namespace number
    /// </summary>
    public int NamespaceID
    { get; private set; }

    /// <summary>
    /// Timestamp of the latest revision of the page
    /// </summary>
    public string Timestamp
    { get; private set; }

    /// <summary>
    /// Edit token (https://www.mediawiki.org/wiki/Manual:Edit_token)
    /// </summary>
    public string EditToken
    { get; internal set; }

    /// <summary>
    /// Delete Token
    /// </summary>
    public string DeleteToken
    { get; internal set; }

    /// <summary>
    /// Protect Token
    /// </summary>
    public string ProtectToken
    { get; internal set; }

    /// <summary>
    /// Move Token
    /// </summary>
    public string MoveToken
    { get; internal set; }

    /// <summary>
    /// Watch Token
    /// </summary>
    public string WatchToken
    { get; internal set; }

    /// <summary>
    /// Rollback Token
    /// </summary>
    public string RollbackToken
    { get; internal set; }

    /// <summary>
    /// Time when the token was obtained. Used for deletion detection.
    /// </summary>
    public string TokenTimestamp
    { get; private set; }

    /// <summary>
    /// String of any edit protection applied to the page
    /// </summary>
    public string EditProtection
    { get; private set; }

    /// <summary>
    /// String of any move protection applied to the page
    /// </summary>
    public string MoveProtection
    { get; private set; }

    /// <summary>
    /// String of any create protection applied to the page
    /// </summary>
    public string CreateProtection
    { get; private set; }

    /// <summary>
    /// Whether the current user is watching this page
    /// </summary>
    public bool IsWatched
    { get; set; }

    /// <summary>
    /// Parses XML for callers that still provide a raw API response string.
    /// Callers that already have a validated XmlDocument should use the
    /// XmlDocument constructor to avoid parsing the same response again.
    /// </summary>
    private static XmlDocument CreateXmlDocument(string xml)
    {
        XmlDocument document = new XmlDocument();
        document.LoadXml(xml);
        return document;
    }

    /// <summary>
    /// Was the specified PageInfo redirected to get to the final target
    /// </summary>
    /// <param name="page">PageInfo object</param>
    /// <returns>Whether the article was redirected</returns>
    public static bool WasRedirected(PageInfo page)
    {
        PageTitleStatus pts = page.TitleChangedStatus;

        if (pts == PageTitleStatus.NoChange)
            return false;

        return ((pts & PageTitleStatus.Redirected) == PageTitleStatus.Redirected ||
                (pts & PageTitleStatus.RedirectLoop) == PageTitleStatus.RedirectLoop ||
                (pts & PageTitleStatus.MultipleRedirects) == PageTitleStatus.MultipleRedirects);
    }
}

/// <summary>
/// 
/// </summary>
[Flags]
public enum PageTitleStatus
{
    NoChange = 0,
    RedirectLoop = 1,
    MultipleRedirects = 2,
    Redirected = 4,
    Normalised = 8,
}
