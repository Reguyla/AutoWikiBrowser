/*
Copyright (C) 2009 Max Semenik

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
*/

using System.Xml;

namespace Twain.Core.API;

/// <summary>
/// Contains information about the page currently being edited.
/// </summary>
public sealed class PageInfo
{
    /// <summary>
    /// Initializes an empty page-information object.
    /// </summary>
    internal PageInfo()
    {
    }

    /// <summary>
    /// Initializes page information from a raw MediaWiki API XML response.
    /// </summary>
    /// <param name="xml">The raw API XML response.</param>
    internal PageInfo(string xml)
        : this(CreateXmlDocument(xml))
    {
    }

    /// <summary>
    /// Initializes page information from a MediaWiki API XML document.
    /// </summary>
    /// <param name="document">The API XML document to process.</param>
    /// <exception cref="BrokenXmlException">
    /// The response does not contain the expected page information.
    /// </exception>
    internal PageInfo(XmlDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using XmlReader reader = new XmlNodeReader(document);

        bool redirectLoop =
            ReadRedirectInformation(
                document,
                out bool hasRedirects,
                out string redirectFrom);

        if (redirectLoop)
            return;

        string currentTimestamp =
            ReadCurrentTimestamp(reader);

        if (!reader.ReadToFollowing("page"))
        {
            // A redirect without a page element may point to an interwiki
            // target or another location that cannot be loaded as a page.
            if (hasRedirects)
                return;

            throw new BrokenXmlException(
                null,
                "The API response did not contain a <page> element.");
        }

        ReadNormalizationInformation(
            document,
            redirectFrom);

        ReadPageState(reader);
        ReadTokens(document, reader);
        ReadRevisionMetadata(reader, currentTimestamp);
        ReadProtectionInformation(document, reader);
        ReadRevisionContent(reader);
    }

    /// <summary>
    /// Gets the page display title in HTML format.
    /// </summary>
    public string DisplayTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the final page title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the original title before normalization or redirection.
    /// </summary>
    public string OriginalTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the reason the original title differs from the final title.
    /// </summary>
    public PageTitleStatus TitleChangedStatus { get; private set; }

    /// <summary>
    /// Gets the page text.
    /// </summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the page exists.
    /// </summary>
    public bool Exists { get; private set; }

    /// <summary>
    /// Gets the revision identifier, or <c>-1</c> when unavailable.
    /// </summary>
    public long RevisionID { get; private set; } = -1;

    /// <summary>
    /// Gets the page namespace identifier.
    /// </summary>
    public int NamespaceID { get; private set; }

    /// <summary>
    /// Gets the timestamp of the latest page revision.
    /// </summary>
    public string Timestamp { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the edit token.
    /// </summary>
    public string EditToken { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delete token.
    /// </summary>
    public string DeleteToken { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the protect token.
    /// </summary>
    public string ProtectToken { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the move token.
    /// </summary>
    public string MoveToken { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the watch token.
    /// </summary>
    public string WatchToken { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rollback token.
    /// </summary>
    public string RollbackToken { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the time at which the token was obtained.
    /// </summary>
    public string TokenTimestamp { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the edit-protection level applied to the page.
    /// </summary>
    public string EditProtection { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the move-protection level applied to the page.
    /// </summary>
    public string MoveProtection { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the create-protection level applied to the page.
    /// </summary>
    public string CreateProtection { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the current user is watching the page.
    /// </summary>
    public bool IsWatched { get; set; }

    /// <summary>
    /// Determines whether the page was redirected before reaching its
    /// final target.
    /// </summary>
    /// <param name="page">The page information to examine.</param>
    /// <returns>
    /// <c>true</c> if the page was redirected, redirected multiple times,
    /// or encountered a redirect loop; otherwise, <c>false</c>.
    /// </returns>
    public static bool WasRedirected(PageInfo page)
    {
        ArgumentNullException.ThrowIfNull(page);

        PageTitleStatus status = page.TitleChangedStatus;

        return (status & PageTitleStatus.Redirected) != 0 ||
               (status & PageTitleStatus.RedirectLoop) != 0 ||
               (status & PageTitleStatus.MultipleRedirects) != 0;
    }

    /// <summary>
    /// Reads redirect information and applies redirect-loop state when
    /// necessary.
    /// </summary>
    /// <param name="document">The API XML document.</param>
    /// <param name="hasRedirects">
    /// Receives whether the response contained redirect entries.
    /// </param>
    /// <param name="redirectFrom">
    /// Receives the original title from the first redirect.
    /// </param>
    /// <returns>
    /// <c>true</c> when a redirect loop was detected; otherwise,
    /// <c>false</c>.
    /// </returns>
    private bool ReadRedirectInformation(
        XmlDocument document,
        out bool hasRedirects,
        out string redirectFrom)
    {
        XmlNodeList redirects =
            document.GetElementsByTagName("r");

        hasRedirects = redirects.Count > 0;
        redirectFrom = string.Empty;

        if (!hasRedirects)
        {
            TitleChangedStatus = PageTitleStatus.NoChange;
            return false;
        }

        XmlAttributeCollection firstAttributes =
            redirects[0].Attributes;

        XmlAttributeCollection lastAttributes =
            redirects[redirects.Count - 1].Attributes;

        string firstFrom =
            firstAttributes?["from"]?.Value
            ?? string.Empty;

        string lastFrom =
            lastAttributes?["from"]?.Value
            ?? string.Empty;

        string lastTo =
            lastAttributes?["to"]?.Value
            ?? string.Empty;

        bool isRedirectLoop =
            string.Equals(
                firstFrom,
                lastTo,
                StringComparison.Ordinal) ||
            string.Equals(
                lastFrom,
                lastTo,
                StringComparison.Ordinal);

        if (isRedirectLoop)
        {
            TitleChangedStatus =
                PageTitleStatus.RedirectLoop;

            OriginalTitle = firstFrom;
            Title = firstFrom;
            Exists = true;
            Text = string.Empty;

            return true;
        }

        redirectFrom = firstFrom;

        TitleChangedStatus =
            redirects.Count == 1
                ? PageTitleStatus.Redirected
                : PageTitleStatus.MultipleRedirects;

        return false;
    }

    /// <summary>
    /// Reads the API response timestamp used when no token start timestamp
    /// is available.
    /// </summary>
    private static string ReadCurrentTimestamp(
        XmlReader reader)
    {
        if (!reader.ReadToFollowing("api"))
            return string.Empty;

        return reader.GetAttribute("curtimestamp")
            ?? string.Empty;
    }

    /// <summary>
    /// Reads title-normalization information and determines the original
    /// title supplied to the API.
    /// </summary>
    private void ReadNormalizationInformation(
        XmlDocument document,
        string redirectFrom)
    {
        XmlNodeList normalizedNodes =
            document.GetElementsByTagName("n");

        string normalizedFrom =
            normalizedNodes.Count > 0
                ? normalizedNodes[0]
                    .Attributes?["from"]
                    ?.Value
                  ?? string.Empty
                : string.Empty;

        if (!string.IsNullOrEmpty(normalizedFrom))
        {
            TitleChangedStatus |=
                PageTitleStatus.Normalised;

            OriginalTitle = normalizedFrom;
        }
        else if (!string.IsNullOrEmpty(redirectFrom))
        {
            OriginalTitle = redirectFrom;
        }
    }

    /// <summary>
    /// Reads basic page identity, existence, watch, and namespace
    /// information.
    /// </summary>
    private void ReadPageState(
        XmlReader reader)
    {
        Exists =
            reader.GetAttribute("missing") is null;

        IsWatched =
            reader.GetAttribute("watched") is not null;

        Title =
            reader.GetAttribute("title")
            ?? string.Empty;

        DisplayTitle =
            reader.GetAttribute("displaytitle")
            ?? string.Empty;

        NamespaceID =
            int.TryParse(
                reader.GetAttribute("ns"),
                out int namespaceId)
                ? namespaceId
                : 0;
    }

    /// <summary>
    /// Reads modern or legacy MediaWiki action tokens.
    /// </summary>
    private void ReadTokens(
        XmlDocument document,
        XmlReader reader)
    {
        XmlNodeList tokenNodes =
            document.GetElementsByTagName("tokens");

        if (tokenNodes.Count == 0)
        {
            ReadLegacyTokens(reader);
            return;
        }

        XmlAttributeCollection attributes =
            tokenNodes[0].Attributes;

        if (attributes is null)
            return;

        string csrfToken =
            attributes["csrftoken"]?.Value
            ?? string.Empty;

        EditToken = csrfToken;
        ProtectToken = csrfToken;
        DeleteToken = csrfToken;
        MoveToken = csrfToken;

        WatchToken =
            attributes["watchtoken"]?.Value
            ?? string.Empty;

        RollbackToken =
            attributes["rollbacktoken"]?.Value
            ?? string.Empty;
    }

    /// <summary>
    /// Reads action tokens returned by MediaWiki versions earlier than
    /// version 1.24.
    /// </summary>
    private void ReadLegacyTokens(
        XmlReader reader)
    {
        EditToken =
            reader.GetAttribute("edittoken")
            ?? string.Empty;

        ProtectToken =
            reader.GetAttribute("protecttoken")
            ?? string.Empty;

        DeleteToken =
            reader.GetAttribute("deletetoken")
            ?? string.Empty;

        MoveToken =
            reader.GetAttribute("movetoken")
            ?? string.Empty;

        WatchToken =
            reader.GetAttribute("watchtoken")
            ?? string.Empty;
    }

    /// <summary>
    /// Reads the token timestamp and latest revision identifier.
    /// </summary>
    private void ReadRevisionMetadata(
        XmlReader reader,
        string currentTimestamp)
    {
        TokenTimestamp =
            reader.GetAttribute("starttimestamp")
            ?? string.Empty;

        if (string.IsNullOrEmpty(TokenTimestamp))
        {
            TokenTimestamp = currentTimestamp;
        }

        RevisionID =
            long.TryParse(
                reader.GetAttribute("lastrevid"),
                out long revisionId)
                ? revisionId
                : -1;
    }

    /// <summary>
    /// Reads edit, move, and create protection levels from the response.
    /// </summary>
    private void ReadProtectionInformation(
        XmlDocument document,
        XmlReader reader)
    {
        if (!reader.ReadToDescendant("protection") ||
            reader.IsEmptyElement)
        {
            return;
        }

        foreach (XmlNode protectionNode in
                 document.GetElementsByTagName("pr"))
        {
            string protectionType =
                protectionNode.Attributes?["type"]?.Value
                ?? string.Empty;

            string protectionLevel =
                protectionNode.Attributes?["level"]?.Value
                ?? string.Empty;

            switch (protectionType)
            {
                case "edit":
                    EditProtection = protectionLevel;
                    break;

                case "move":
                    MoveProtection = protectionLevel;
                    break;

                case "create":
                    CreateProtection = protectionLevel;
                    break;
            }
        }
    }

    /// <summary>
    /// Reads the latest revision timestamp and page text.
    /// </summary>
    private void ReadRevisionContent(
        XmlReader reader)
    {
        if (!reader.ReadToFollowing("revisions") ||
            !reader.ReadToDescendant("rev"))
        {
            Timestamp = string.Empty;
            Text = string.Empty;
            return;
        }

        Timestamp =
            reader.GetAttribute("timestamp")
            ?? string.Empty;

        // MediaWiki returns LF line endings. AWB standardizes page text
        // on Windows-style CRLF line endings.
        Text = reader
            .ReadString()
            .Replace(
                "\n",
                "\r\n",
                StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses XML for callers that provide a raw API response string.
    /// </summary>
    private static XmlDocument CreateXmlDocument(
        string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        XmlDocument document = new();
        document.LoadXml(xml);

        return document;
    }
}

/// <summary>
/// Describes how the requested page title changed while being processed
/// by the MediaWiki API.
/// </summary>
[Flags]
public enum PageTitleStatus
{
    /// <summary>
    /// The title was not changed.
    /// </summary>
    NoChange = 0,

    /// <summary>
    /// The redirect chain contained a loop.
    /// </summary>
    RedirectLoop = 1,

    /// <summary>
    /// The request followed more than one redirect.
    /// </summary>
    MultipleRedirects = 2,

    /// <summary>
    /// The request followed one redirect.
    /// </summary>
    Redirected = 4,

    /// <summary>
    /// MediaWiki normalized the requested title.
    /// </summary>
    Normalised = 8
}