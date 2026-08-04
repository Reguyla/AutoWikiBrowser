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
/// Contains identity, group, rights, and status information for a
/// MediaWiki user.
/// </summary>
public sealed class UserInfo
{
    private const string SysopGroup = "sysop";
    private const string BotGroup = "bot";

    private const string BotRight = "bot";
    private const string ApiHighLimitsRight = "apihighlimits";
    private const string EditInterfaceRight = "editinterface";
    private const string DeleteRight = "delete";
    private const string ProtectRight = "protect";
    private const string ReadNotificationsRight =
        "echo-read-notifications";

    private readonly List<string> _groups = new();
    private readonly List<string> _rights = new();

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the MediaWiki database user identifier.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets whether the user is logged in.
    /// </summary>
    public bool IsLoggedIn => Id != 0;

    /// <summary>
    /// Gets whether the current user belongs to the administrator group.
    /// </summary>
    public bool IsSysop => IsInGroup(SysopGroup);

    /// <summary>
    /// Gets whether the current user belongs to the bot group or has the
    /// bot right.
    /// </summary>
    public bool IsBot =>
        IsInGroup(BotGroup) ||
        HasRight(BotRight);

    /// <summary>
    /// Gets whether the user has the MediaWiki
    /// <c>apihighlimits</c> right.
    /// </summary>
    public bool HasApiHighLimit =>
        HasRight(ApiHighLimitsRight);

    /// <summary>
    /// Gets whether the current user is blocked from editing.
    /// </summary>
    public bool IsBlocked { get; private set; }

    /// <summary>
    /// Gets or sets whether the user has an unread user-talk message.
    /// </summary>
    public bool HasMessages { get; internal set; }

    /// <summary>
    /// Gets or sets the number of unread notifications for the user.
    /// </summary>
    public int Notifications { get; internal set; }

    /// <summary>
    /// Determines whether the user belongs to the specified group.
    /// An empty group represents no group restriction.
    /// </summary>
    /// <param name="group">The MediaWiki group name to check.</param>
    /// <returns>
    /// <c>true</c> if no group is required or the user belongs to the
    /// specified group; otherwise, <c>false</c>.
    /// </returns>
    public bool IsInGroup(string group) =>
        string.IsNullOrEmpty(group) ||
        _groups.Contains(group);

    /// <summary>
    /// Determines whether the user has the specified MediaWiki right.
    /// An empty right represents no rights restriction.
    /// </summary>
    /// <param name="right">The MediaWiki right to check.</param>
    /// <returns>
    /// <c>true</c> if no right is required or the user has the specified
    /// right; otherwise, <c>false</c>.
    /// </returns>
    public bool HasRight(string right) =>
        string.IsNullOrEmpty(right) ||
        _rights.Contains(right);

    /// <summary>
    /// Determines whether the user may edit the specified page based on
    /// its protection settings and namespace.
    /// </summary>
    /// <param name="page">The page whose edit permissions are evaluated.</param>
    /// <returns>
    /// <c>true</c> if the user may edit the page; otherwise, <c>false</c>.
    /// </returns>
    public bool CanEditPage(PageInfo page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return (IsInGroup(page.EditProtection) ||
                HasRight(page.EditProtection)) &&
               (page.NamespaceID != Namespace.MediaWiki ||
                HasRight(EditInterfaceRight));
    }

    /// <summary>
    /// Determines whether the user has the general right required to
    /// delete pages.
    /// </summary>
    /// <param name="page">
    /// The page being evaluated. The current implementation does not apply
    /// page-specific deletion restrictions.
    /// </param>
    /// <returns>
    /// <c>true</c> if the user has the <c>delete</c> right; otherwise,
    /// <c>false</c>.
    /// </returns>
    public bool CanDeletePage(PageInfo page) =>
        HasRight(DeleteRight);

    /// <summary>
    /// Determines whether the user may create the specified page based on
    /// its creation-protection settings.
    /// </summary>
    /// <param name="page">
    /// The page whose creation permissions are evaluated.
    /// </param>
    /// <returns>
    /// <c>true</c> if the user may create the page; otherwise,
    /// <c>false</c>.
    /// </returns>
    public bool CanCreatePage(PageInfo page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return IsInGroup(page.CreateProtection) ||
               HasRight(page.CreateProtection);
    }

    /// <summary>
    /// Determines whether the user may move the specified page based on
    /// its namespace and move-protection settings.
    /// </summary>
    /// <param name="page">The page whose move permissions are evaluated.</param>
    /// <returns>
    /// <c>true</c> if the user may move the page; otherwise, <c>false</c>.
    /// </returns>
    public bool CanMovePage(PageInfo page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return page.NamespaceID != Namespace.MediaWiki &&
               (IsInGroup(page.MoveProtection) ||
                HasRight(page.MoveProtection));
    }

    /// <summary>
    /// Determines whether the user has the general right required to
    /// protect pages.
    /// </summary>
    /// <param name="page">
    /// The page being evaluated. The current implementation does not apply
    /// page-specific protection restrictions.
    /// </param>
    /// <returns>
    /// <c>true</c> if the user has the <c>protect</c> right; otherwise,
    /// <c>false</c>.
    /// </returns>
    public bool CanProtectPage(PageInfo page) =>
        HasRight(ProtectRight);

    /// <summary>
    /// Determines whether the user may read Echo notifications.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the user has the
    /// <c>echo-read-notifications</c> right; otherwise, <c>false</c>.
    /// </returns>
    public bool HasReadNotificationsRight() =>
        HasRight(ReadNotificationsRight);

    /// <summary>
    /// Initializes user information from a MediaWiki
    /// <c>meta=userinfo</c> XML response.
    /// </summary>
    /// <param name="xml">
    /// The API XML response to process. The response must already have been
    /// checked for API errors by <c>ApiEdit.CheckForErrors()</c>.
    /// </param>
    /// <exception cref="BrokenXmlException">
    /// The response does not contain a valid <c>userinfo</c> element.
    /// </exception>
    internal UserInfo(XmlDocument xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        XmlNodeList users =
            xml.GetElementsByTagName("userinfo");

        if (users.Count == 0)
        {
            throw new BrokenXmlException(
                null,
                "XML with a <userinfo> element was expected.");
        }

        XmlNode user = users[0];

        string name = user.Attributes?["name"]?.Value;
        string idText = user.Attributes?["id"]?.Value;

        if (string.IsNullOrEmpty(name) ||
            !int.TryParse(idText, out int id))
        {
            throw new BrokenXmlException(
                null,
                "The <userinfo> element did not contain a valid name and ID.");
        }

        Name = name;
        Id = id;

        XmlElement groups = user["groups"];

        if (groups != null)
        {
            foreach (XmlNode group in
                     groups.GetElementsByTagName("g"))
            {
                _groups.Add(group.InnerText);
            }
        }

        XmlElement rights = user["rights"];

        if (rights != null)
        {
            foreach (XmlNode right in
                     rights.GetElementsByTagName("r"))
            {
                _rights.Add(right.InnerText);
            }
        }

        Update(xml);
    }

    /// <summary>
    /// Updates changeable information about the current user from a
    /// MediaWiki user-information response.
    /// </summary>
    /// <param name="xml">The MediaWiki API XML response to process.</param>
    internal void Update(XmlDocument xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        XmlNodeList users =
            xml.GetElementsByTagName("userinfo");

        if (users.Count > 0)
        {
            XmlAttributeCollection attributes =
                users[0].Attributes;

            HasMessages =
                attributes?["messages"] != null;

            IsBlocked =
                attributes?["blockedby"] != null;
        }

        XmlNodeList notifications =
            xml.GetElementsByTagName("notifications");

        string rawCount =
            notifications.Count > 0
                ? notifications[0]
                    .Attributes?["rawcount"]
                    ?.Value
                : null;

        Notifications =
            int.TryParse(rawCount, out int count)
                ? count
                : 0;
    }

    /// <summary>
    /// Initializes user information for an unregistered or anonymous user.
    /// </summary>
    internal UserInfo()
    {
    }
}