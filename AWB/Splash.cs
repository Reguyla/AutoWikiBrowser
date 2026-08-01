/*
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

using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace AutoWikiBrowser;

/// <summary>
/// Displays application version and startup progress while
/// AutoWikiBrowser is being initialized.
/// </summary>
internal sealed partial class Splash : Form
{
    /// <summary>
    /// Initializes the splash screen and resets its progress display.
    /// </summary>
    internal Splash()
    {
        InitializeComponent();

        lblVersion.Text = $"Version {Program.VersionString}";
        SetProgress(0);
    }

    /// <summary>
    /// Closes the splash screen when the user clicks a configured
    /// splash-screen control.
    /// </summary>
    private void ClickHandler(object? sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Updates the startup progress display.
    /// </summary>
    /// <param name="percent">
    /// The requested completion percentage. Values outside the progress bar's
    /// configured range are clamped to that range.
    /// </param>
    /// <remarks>
    /// This method can be called from a thread other than the splash screen's
    /// UI thread.
    /// </remarks>
    internal void SetProgress(int percent)
    {
        // TODO: Replace runtime stack inspection with an explicit startup-stage
        // description supplied by the caller. Stack-frame inspection adds
        // overhead and can be affected by compiler or JIT method inlining.
        MethodBase? method = new StackFrame(1, false).GetMethod();

        string methodText = method?.DeclaringType is null
            ? string.Empty
            : $"{method.DeclaringType.Name}::{method.Name}()";

        SetProgressCore(percent, methodText);
    }

    /// <summary>
    /// Applies a startup progress update on the splash screen's UI thread.
    /// </summary>
    /// <param name="percent">The requested progress percentage.</param>
    /// <param name="methodText">
    /// The startup method to display, or an empty string to retain the
    /// currently displayed method.
    /// </param>
    private void SetProgressCore(int percent, string methodText)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            if (!IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke(
                    new Action<int, string>(SetProgressCore),
                    percent,
                    methodText);
            }
            catch (InvalidOperationException) when (IsDisposed || Disposing)
            {
                // The form was disposed after the state checks but before
                // the update could be queued.
            }

            return;
        }

        if (!string.IsNullOrEmpty(methodText))
        {
            MethodLabel.Text = methodText;
        }

        progressBar.Value = Math.Clamp(
            percent,
            progressBar.Minimum,
            progressBar.Maximum);
    }
}