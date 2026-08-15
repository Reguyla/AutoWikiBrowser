/*
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

// From the Kingbotk plugin. Converted from VB to C#.

using Twain.Core.Logging;

namespace AutoWikiBrowser.Logging;

/// <summary>
/// Coordinates application logging and forwards AWB-specific logging events
/// to the configured trace listeners.
/// </summary>
internal sealed class MyTrace : TraceManager, IAWBTraceListener
{
    /// <summary>
    /// Initializes configured log listeners and records the start of logging.
    /// </summary>
    internal void Initialise()
    {
        try
        {
            foreach (KeyValuePair<string, IMyTraceListener> listener in Listeners)
            {
                if (listener.Key == "AWB")
                {
                    continue;
                }

                listener.Value.WriteBulletedLine(
                    AWBLogListener.LoggingStartButtonClicked,
                    true,
                    false,
                    true);
            }
        }
        catch (Exception ex)
        {
            ConfigError(ex);
        }
    }

    /// <summary>
    /// Handles a logging configuration error and stops the current AWB
    /// operation.
    /// </summary>
    /// <param name="ex">
    /// The exception raised while configuring logging.
    /// </param>
    internal void ConfigError(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        MessageBox.Show(
            ex.Message,
            "Logging error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

        Program.AWB.Stop("AutoWikiBrowser");
    }

    /// <summary>
    /// Gets a value indicating whether any non-default log listener is open.
    /// </summary>
    internal bool HaveOpenFile =>
        Listeners.Any(listener => listener.Key != "AWB");

    /// <summary>
    /// Adds or replaces a trace listener with the specified key.
    /// </summary>
    /// <param name="key">
    /// The unique key used to identify the listener.
    /// </param>
    /// <param name="listener">
    /// The listener to add.
    /// </param>
    public override void AddListener(
        string key,
        IMyTraceListener listener)
    {
        if (ContainsKey(key))
        {
            base.RemoveListener(key);
        }

        base.AddListener(key, listener);
    }

    /// <summary>
    /// Removes the trace listener associated with the specified key.
    /// </summary>
    /// <param name="key">
    /// The key of the listener to remove.
    /// </param>
    public override void RemoveListener(string key)
    {
        if (!Listeners.TryGetValue(key, out IMyTraceListener? listener))
        {
            return;
        }

        base.RemoveListener(key);
    }

    /// <summary>
    /// Closes all configured trace listeners and clears the listener
    /// collection.
    /// </summary>
    public override void Close()
    {
        foreach (KeyValuePair<string, IMyTraceListener> listener in Listeners)
        {
            listener.Value.WriteCommentAndNewLine("closing all logs");
            listener.Value.Close();
        }

        Listeners.Clear();
    }

    #region Generic overrides

    // TODO: Separate logging configuration errors from application shutdown.
    // Report the failure through a UI-independent result or exception and let
    // the application layer decide whether the current operation should stop.
    //
    // TODO: Confirm whether every registered IMyTraceListener is required to
    // implement IAWBTraceListener. If not, replace direct casts with guarded
    // interface checks.
    /// <summary>
    /// Records that AutoWikiBrowser skipped the current page.
    /// </summary>
    /// <param name="reason">
    /// The reason the page was skipped.
    /// </param>
    public void AWBSkipped(string reason)
    {
        foreach (KeyValuePair<string, IMyTraceListener> listener in Listeners)
        {
            ((IAWBTraceListener)listener.Value).AWBSkipped(reason);
        }
    }

    /// <summary>
    /// Records that a plugin skipped the current page.
    /// </summary>
    public void PluginSkipped()
    {
        foreach (KeyValuePair<string, IMyTraceListener> listener in Listeners)
        {
            ((IAWBTraceListener)listener.Value).PluginSkipped();
        }
    }

    /// <summary>
    /// Records that the user skipped the current page.
    /// </summary>
    public void UserSkipped()
    {
        foreach (KeyValuePair<string, IMyTraceListener> listener in Listeners)
        {
            ((IAWBTraceListener)listener.Value).UserSkipped();
        }
    }

    #endregion

    /// <summary>
    /// Gets the application name written to logging output.
    /// </summary>
    protected override string ApplicationName =>
        "Twain logging manager";
}