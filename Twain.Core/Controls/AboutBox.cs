/*

Copyright (C) 2007 Martin Richards
(C) 2009 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

using System.Reflection;
using System.Windows.Forms;

namespace Twain.Core.Controls;

public partial class AboutBox : Form
{
    public AboutBox()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The AboutBox form is being initialized. Override this if you are inheriting and recycling the form.
    /// </summary>
    protected virtual void Initialise()
    {
        lblVersion.Text = "Version " + Tools.VersionString;
        textBoxDescription.Text = GPLNotice;
    }

    protected virtual void okButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    protected virtual void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        linkLabel1.LinkVisited = true;
        Tools.OpenENArticleInBrowser("WP:AWB", false);
    }

    #region Shared

    public static string GPLNotice =>
        AboutInformation.GPLNotice;

    public static string AssemblyDescription(Assembly ass)
    {
        return AboutInformation.AssemblyDescription(ass);
    }

    public static string AssemblyCopyright(Assembly ass)
    {
        return AboutInformation.AssemblyCopyright(ass);
    }

    public static string GetDetailedMessage(Assembly ass)
    {
        return AboutInformation.GetDetailedMessage(ass);
    }

    #endregion

    private void AboutBox_Load(object sender, EventArgs e)
    {
        Initialise();
    }
}