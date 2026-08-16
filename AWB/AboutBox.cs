/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

using Twain.Core;

namespace AutoWikiBrowser;

/// <summary>
/// Displays version, environment, licensing, and support
/// information.
/// </summary>
internal sealed partial class AboutBox : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AboutBox"/> form.
    /// </summary>
    /// <param name="ieVersion">
    /// The detected Internet Explorer version displayed in the environment
    /// information. This parameter is retained until the browser integration
    /// is fully migrated to WebView2.
    /// </param>
    public AboutBox(string ieVersion)
    {
        InitializeComponent();

        lblAWBVersion.Text = $"Version {Program.VersionString}";
        lblRevision.Text = $"SVN {Variables.Revision}";

        txtWarning.Text =
            Twain.Core.Controls.AboutBox.GetDetailedMessage(
                typeof(AboutBox).Assembly);

        // TODO (.NET10 / WebView2):
        // Replace the legacy Internet Explorer version reporting with WebView2 runtime
        // information once the browser migration is complete. Rename the constructor
        // parameter from 'ieVersion' to 'browserVersion' (or 'webViewVersion') and
        // update the About dialog to display the installed Microsoft Edge WebView2
        // Runtime version instead of the legacy Internet Explorer version. This will
        // require obtaining the runtime version from the WebView2 environment (or a
        // centralized browser service) before constructing the AboutBox.
        txtVersions.Text = $"""
            Internet Explorer version: {ieVersion}
            .NET version: {Environment.Version}
            Windows version: {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}
            """;
    }

    /// <summary>
    /// Closes the About dialog.
    /// </summary>
    private void OkButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Opens the Twain project page on the English Wikipedia.
    /// </summary>
    private void LinkAWBPage_LinkClicked(
        object sender,
        LinkLabelLinkClickedEventArgs e)
    {
        linkAWBPage.LinkVisited = true;
        Tools.OpenENArticleInBrowser(
            "Wikipedia:AutoWikiBrowser",
            false);
    }

}