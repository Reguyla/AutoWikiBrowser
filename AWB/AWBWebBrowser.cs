using System.Reflection;
using System.Windows.Forms;
using Twain.Core;

namespace AutoWikiBrowser;

// TODO (WebView2): Remove AWBWebBrowser after all remaining legacy
// System.Windows.Forms.WebBrowser usages have been migrated to WebView2.
// This class depends on the Internet Explorer/MSHTML engine and should not
// remain part of the final browser implementation.
//
// TODO (architecture): Replace direct access to Variables.MainForm with
// commands, events, or an injected interface. The browser control should
// request Save, Start, Skip, and Find operations without inspecting form
// controls or invoking MainForm workflow methods directly.
//
/// <summary>
/// Extends the legacy Windows Forms <see cref="WebBrowser"/> control with
/// Keyboard shortcuts and browser-selection support.
/// </summary>
/// <remarks>
/// This control depends on the legacy Internet Explorer/MSHTML browser engine.
/// It also communicates directly with the active <see cref="MainForm"/> through
/// <see cref="Variables.MainForm"/>. Both dependencies should be removed when
/// the remaining browser functionality is migrated to WebView2.
/// </remarks>
internal sealed class AWBWebBrowser : WebBrowser
{
    private const int WmKeyUp = 0x0101;
    private const string CommandSource = nameof(AWBWebBrowser);

    // TODO (WebView2): Reimplement AWB browser shortcuts through WebView2's
    // AcceleratorKeyPressed event. Preserve the existing Ctrl+C, Ctrl+J,
    // Ctrl+S, and Ctrl+I behavior, including whether each shortcut is consumed
    // when its associated action is unavailable.

    /// <summary>
    /// Processes keyboard messages before they are dispatched to the embedded browser.
    /// </summary>
    /// <param name="msg">The Windows message to process.</param>
    /// <returns>
    /// <see langword="true"/> when an AWB keyboard shortcut handled the
    /// message; otherwise, the result returned by the base control.
    /// </returns>
    /// <remarks>
    /// The following Ctrl-key shortcuts are handled:
    /// <list type="bullet">
    /// <item><description>Ctrl+C copies selected browser text.</description></item>
    /// <item>
    /// <description>
    /// Ctrl+J finds the selected browser text in the article editor.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Ctrl+S saves the current article, or starts processing when saving is unavailable.
    /// </description>
    /// </item>
    /// <item><description>Ctrl+I skips the current article.</description></item>
    /// </list>
    /// </remarks>
    public override bool PreProcessMessage(ref Message msg)
    {
        // TODO (behavior): Decide whether AWB shortcuts should require Ctrl as the
        // only modifier or should also work with combinations such as Ctrl+Shift.
        // Preserve ModifierKeys == Keys.Control until the intended behavior has
        // been verified.
        if (msg.Msg != WmKeyUp ||
            ModifierKeys != Keys.Control)
        {
            return base.PreProcessMessage(ref msg);
        }

        Keys keyCode = (Keys)msg.WParam.ToInt32();

        switch (keyCode)
        {
            // TODO (WebView2): Verify whether the WebView2 browser's built-in Ctrl+C
            // behavior is sufficient before adding custom copy handling. Only intercept
            // Ctrl+C if AWB needs behavior beyond WebView2's normal accelerator handling.
            case Keys.C:
                CopySelectedText();
                return true;

            // TODO (WebView2): Replace MSHTML-based selected-text access with an
            // asynchronous WebView2 selection provider, likely using ExecuteScriptAsync
            // to evaluate window.getSelection()?.toString() after the document has loaded.
            // Keep browser-specific JavaScript outside the form and control classes.
            case Keys.J:
                FindSelectedTextInEditor();
                return true;

            case Keys.S:
                SaveOrStart();
                return true;

            case Keys.I:
                SkipCurrentPage();
                return true;

            default:
                return base.PreProcessMessage(ref msg);
        }
    }

    /// <summary>
    /// Copies the selected browser text to the clipboard.
    /// </summary>
    private void CopySelectedText()
    {
        Document?.ExecCommand(
            "Copy",
            false,
            null);
    }

    /// <summary>
    /// Finds the selected browser text in the current article editor.
    /// </summary>
    private void FindSelectedTextInEditor()
    {
        if (!TextSelected())
        {
            return;
        }

        Variables.MainForm.EditBox.Find(
            SelectedText(),
            false,
            false,
            Variables.MainForm.TheSession.Page.Title);
    }

    /// <summary>
    /// Saves the current article when saving is available; otherwise, starts
    /// article processing when the Start command is available.
    /// </summary>
    private static void SaveOrStart()
    {
        if (Variables.MainForm.SaveButton.Enabled)
        {
            Variables.MainForm.Save(CommandSource);
        }
        else if (Variables.MainForm.StartButton.Enabled)
        {
            Variables.MainForm.Start(CommandSource);
        }
    }

    /// <summary>
    /// Skips the current article when the Skip command is available.
    /// </summary>
    private static void SkipCurrentPage()
    {
        if (Variables.MainForm.SkipButton.Enabled)
        {
            Variables.MainForm.SkipPage(
                CommandSource,
                "user");
        }
    }

    /// <summary>
    /// Determines whether text is currently selected in the embedded browser.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the browser contains selected text;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Browser-specific selection access is delegated to
    /// <see cref="BrowserSelectionProvider"/>. Some systems report that the
    /// legacy browser interop assembly is available even though selection
    /// access still fails at runtime, so this method retains defensive exception handling.
    /// </remarks>
    public bool TextSelected()
    {
        if (!Globals.MSHTMLAvailable)
        {
            return false;
        }

        try
        {
            return !string.IsNullOrEmpty(GetSelectedText());
        }
        catch
        {
            // Some systems report that the legacy browser interop assembly
            // is available even though selection access fails at runtime.
            //
            // Historical report:
            // Wikipedia talk:AutoWikiBrowser/Bugs/Archive 23
            // "Single click to focus the edit box to a line..."
            return false;
        }
    }

    /// <summary>
    /// Gets the selected browser text, returning an empty string when no text
    /// is selected.
    /// </summary>
    private string SelectedText()
    {
        return GetSelectedText() ?? string.Empty;
    }

    /// <summary>
    /// Retrieves the selected text through the browser-specific selection provider.
    /// </summary>
    private string? GetSelectedText()
    {
        return BrowserSelectionProvider.GetSelectedText(Document);
    }

    // TODO (.NET10): Remove the Mono-specific Refresh and Navigate guards after
    // confirming that the modern AWB build no longer supports or targets the
    // legacy Mono runtime.
    /// <summary>
    /// Refreshes the embedded browser when supported by the current runtime.
    /// </summary>
    /// <remarks>
    /// Legacy Windows Forms browser refresh calls may fail under Mono, so the
    /// operation is intentionally ignored there.
    /// </remarks>
    public override void Refresh()
    {
        if (!Globals.UsingMono)
        {
            base.Refresh();
        }
    }

    // TODO (WebView2): Replace the hidden WebBrowser.Navigate method and custom
    // request-header string with a WebView2 navigation service. Determine whether
    // AWB still needs to override the user agent or attach headers to individual
    // navigation requests, and centralize that policy instead of implementing it
    // in the UI control.
    /// <summary>
    /// Navigates to the specified address using AWB's custom user-agent header.
    /// </summary>
    /// <param name="urlString">The address to open.</param>
    /// <remarks>
    /// <see cref="WebBrowser.Navigate(string)"/> is not virtual, so this method
    /// hides rather than overrides the inherited method. Legacy browser
    /// navigation is intentionally skipped under Mono.
    /// </remarks>
    public new void Navigate(string urlString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urlString);

        // TODO (validation): Review all Navigate callers and confirm whether null,
        // empty, or whitespace addresses are possible. If they always represent a
        // caller error, add ArgumentException.ThrowIfNullOrWhiteSpace(urlString)
        // before invoking the browser navigation API.
        if (Globals.UsingMono)
        {
            return;
        }

        AssemblyName assemblyName =
            typeof(AWBWebBrowser).Assembly.GetName();

        string headers =
            $"User-Agent: AWBWebBrowser {assemblyName}/{Tools.VersionString} " +
            $"{Tools.DefaultUserAgentString}\r\n";

        base.Navigate(
            urlString,
            null,
            null,
            headers);
    }
}