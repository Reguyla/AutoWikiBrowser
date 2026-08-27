using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Windows.Forms;
using Twain.Core;

namespace AutoWikiBrowser.Services.Diff;

/// <summary>
/// Manages the WebView2 control used to display generated article diffs.
/// </summary>
internal sealed class DiffWebViewService
{
    private readonly WebView2 _webView;

    private Task _initializationTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffWebViewService"/>
    /// class and creates the WebView2 diff control in the supplied parent.
    /// </summary>
    /// <param name="parent">
    /// The control that should contain the diff WebView2 instance.
    /// </param>
    public DiffWebViewService(
        Control parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        parent.Controls.Add(_webView);
    }

    /// <summary>
    /// Occurs when the generated diff document sends a message to the host.
    /// </summary>
    public event EventHandler<CoreWebView2WebMessageReceivedEventArgs>
        WebMessageReceived;

    /// <summary>
    /// Gets or sets whether the diff WebView2 control is visible.
    /// </summary>
    public bool Visible
    {
        get => _webView.Visible;
        set => _webView.Visible = value;
    }

    /// <summary>
    /// Initializes the WebView2 diff renderer, ensuring that only one
    /// initialization attempt runs at a time.
    /// </summary>
    public async Task InitializeAsync()
    {
        Task initializationTask =
            _initializationTask ??=
                InitializeCoreAsync();

        try
        {
            await initializationTask;
        }
        catch
        {
            if (ReferenceEquals(
                    _initializationTask,
                    initializationTask))
            {
                _initializationTask = null;
            }

            throw;
        }
    }

    /// <summary>
    /// Renders generated diff HTML and waits for the WebView2 document to
    /// finish loading.
    /// </summary>
    /// <param name="html">
    /// The complete diff HTML document.
    /// </param>
    public async Task RenderAsync(
        string html)
    {
        if (_webView.IsDisposed ||
            _webView.CoreWebView2 == null)
        {
            Tools.WriteDebug(
                nameof(RenderAsync),
                "WebView2 was unavailable when the diff was rendered.");

            return;
        }

        var navigationCompletion =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        void NavigationCompleted(
            object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                navigationCompletion.TrySetResult(true);
            }
            else
            {
                navigationCompletion.TrySetException(
                    new InvalidOperationException(
                        $"WebView2 diff navigation failed: {e.WebErrorStatus}."));
            }
        }

        _webView.NavigationCompleted +=
            NavigationCompleted;

        try
        {
            _webView.NavigateToString(html);

            await navigationCompletion.Task;
        }
        finally
        {
            _webView.NavigationCompleted -=
                NavigationCompleted;
        }
    }

    /// <summary>
    /// Performs the WebView2 diff renderer initialization.
    /// </summary>
    private async Task InitializeCoreAsync()
    {
        if (_webView.IsDisposed)
        {
            throw new ObjectDisposedException(
                nameof(_webView));
        }

        await _webView.EnsureCoreWebView2Async();

        if (_webView.IsDisposed)
        {
            return;
        }

        CoreWebView2 core =
            _webView.CoreWebView2
            ?? throw new InvalidOperationException(
                "WebView2 initialization completed without creating CoreWebView2.");

        Configure(core);

        core.WebMessageReceived -=
            Core_WebMessageReceived;

        core.WebMessageReceived +=
            Core_WebMessageReceived;

        Tools.WriteDebug(
            nameof(InitializeAsync),
            "WebView2 initialized.");
    }

    /// <summary>
    /// Configures the WebView2 instance used to display generated article
    /// diffs.
    /// </summary>
    /// <param name="core">
    /// The initialized WebView2 core to configure.
    /// </param>
    private static void Configure(
        CoreWebView2 core)
    {
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;

        // Script and web messaging are required by the generated diff
        // document for diff navigation and undo commands.
        core.Settings.IsWebMessageEnabled = true;
        core.Settings.IsScriptEnabled = true;
    }

    /// <summary>
    /// Forwards messages received from the diff document to the application.
    /// </summary>
    private void Core_WebMessageReceived(
        object sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        WebMessageReceived?.Invoke(
            sender,
            e);
    }
}