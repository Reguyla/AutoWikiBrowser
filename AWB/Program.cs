/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

using AutoWikiBrowser.Logging;
using Twain.Core;
using Twain.Core.Plugin;

namespace AutoWikiBrowser;

/// <summary>
/// Provides the application entry point and shared process-level state for
/// Twain.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Initializes application-wide services and starts the main application form.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments passed to the application.
    /// </param>
    [STAThread]
    private static void Main(string[] args)
    {
        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        try
        {
            Thread.CurrentThread.Name = "Main thread";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += ApplicationThreadException;

            if (Globals.UsingMono)
            {
                MessageBox.Show(
                    "This application is not currently supported by Mono. You may use it for " +
                    "testing purposes, but functionality is not guaranteed.",
                    "Not supported",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            AwbDirs.MigrateDefaultSettings();

            MainForm awb = new();
            AWB = awb;

            awb.ParseCommandLine(args);

            Article.SetAddListener(
                MyTrace.AddListener,
                MyTrace,
                "Twain");

            Application.Run(awb);
        }
        catch (Exception ex)
        {
            if (ex is SecurityException)
            {
                // Some execution locations, such as restricted network shares,
                // may not grant the permissions required to start AWB.
                MessageBox.Show(
                    "This application is unable to start from the current location due to a " +
                    "lack of permissions.\r\nPlease try a local drive or a " +
                    "similarly trusted location.",
                    "Permissions Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                ErrorHandler.HandleException(ex);
            }
        }
    }

    /// <summary>
    /// Handles exceptions that escape the Windows Forms message loop.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// Event data containing the unhandled thread exception.
    /// </param>
    private static void ApplicationThreadException(
        object sender,
        ThreadExceptionEventArgs e)
    {
        ErrorHandler.HandleException(e.Exception);
    }

    /// <summary>
    /// Gets the version of the currently executing Twain assembly.
    /// </summary>
    internal static Version Version =>
        Assembly.GetExecutingAssembly()
            .GetName()
            .Version;

    /// <summary>
    /// Gets the application version as a string.
    /// </summary>
    internal static string VersionString =>
        Version.ToString();

    /// <summary>
    /// The application display name.
    /// </summary>
    internal const string Name = "Twain";

    /// <summary>
    /// Gets or sets the active Twain application instance.
    /// </summary>
    internal static IAutoWikiBrowser AWB;

    /// <summary>
    /// Provides the shared application trace listener.
    /// </summary>
    internal static readonly MyTrace MyTrace = new();
}