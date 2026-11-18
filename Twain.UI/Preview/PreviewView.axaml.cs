
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Twain.UI.Themes;
using Twain.UI.ViewModels.Workspaces;

namespace Twain.UI.Preview;

/// <summary>
/// Displays rendered MediaWiki article previews.
/// </summary>
public partial class PreviewView : UserControl
{
    private WorkspaceViewModel? _subscribedViewModel;
    private bool _webViewReady;

    private const string DarkPreviewCss = """
        html, body {
            background-color: #1B222B !important;
            color: #E7E9EC !important;
        }

        body {
            margin: 10px;
        }

        a, a:link {
            color: #8DBCF0 !important;
        }

        a:visited {
            color: #B5A0E6 !important;
        }

        h1, h2, h3, h4, h5, h6 {
            color: #E7E9EC !important;
            border-color: #46505C !important;
        }

        table, th, td {
            border-color: #46505C !important;
        }

        .infobox,
        .wikitable {
            background-color: #252E38 !important;
            color: #E7E9EC !important;
            border-color: #46505C !important;
        }

        .infobox th,
        .wikitable th {
            background-color: #303A46 !important;
            color: #E7E9EC !important;
        }

        .infobox td,
        .wikitable td {
            background-color: #252E38 !important;
            color: #E7E9EC !important;
        }

        blockquote,
        pre,
        code {
            color: #E7E9EC !important;
            border-color: #46505C !important;
        }

        pre {
            background-color: #252E38 !important;
        }
        """;

    private const string LightPreviewCss = """
        html, body {
            background-color: #FFFFFF !important;
            color: #000000 !important;
        }

        body {
            margin: 10px;
        }
        """;

    public PreviewView()
    {
        InitializeComponent();

        PreviewWebView.AdapterCreated +=
            PreviewWebView_AdapterCreated;

        AttachedToVisualTree +=
            PreviewView_AttachedToVisualTree;

        DetachedFromVisualTree +=
            PreviewView_DetachedFromVisualTree;
    }

    private void PreviewView_AttachedToVisualTree(
        object? sender,
        VisualTreeAttachmentEventArgs e)
    {
        ThemeManager.ThemeChanged -=
            ThemeManager_ThemeChanged;

        ThemeManager.ThemeChanged +=
            ThemeManager_ThemeChanged;

        _ = LoadPreviewHtmlAsync();
    }

    private void PreviewView_DetachedFromVisualTree(
        object? sender,
        VisualTreeAttachmentEventArgs e)
    {
        ThemeManager.ThemeChanged -=
            ThemeManager_ThemeChanged;
    }

    private void ThemeManager_ThemeChanged(
        object? sender,
        EventArgs e)
    {
        _ = ApplyPreviewThemeAsync();
    }

    private void PreviewWebView_AdapterCreated(
        object? sender,
        WebViewAdapterEventArgs e)
    {
        PreviewWebView.AdapterCreated -=
            PreviewWebView_AdapterCreated;

        _webViewReady = true;

        _ = LoadPreviewHtmlAsync();
    }

    protected override void OnDataContextChanged(
        EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged -=
                ViewModel_PropertyChanged;
        }

        _subscribedViewModel =
            DataContext as WorkspaceViewModel;

        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged +=
                ViewModel_PropertyChanged;
        }

        base.OnDataContextChanged(e);

        if (_webViewReady)
        {
            _ = LoadPreviewHtmlAsync();
        }
    }

    private async void ViewModel_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
            nameof(WorkspaceViewModel.PreviewHtml))
        {
            return;
        }

        await LoadPreviewHtmlAsync();
    }

    /// <summary>
    /// Loads the original MediaWiki preview HTML and applies
    /// the currently selected Twain theme.
    /// </summary>
    private async Task LoadPreviewHtmlAsync()
    {
        if (!_webViewReady ||
            DataContext is not WorkspaceViewModel viewModel)
        {
            return;
        }

        string html = JsonSerializer.Serialize(
            viewModel.PreviewHtml);

        await PreviewWebView.InvokeScript(
            "document.open();" +
            $"document.write({html});" +
            "document.close();");

        await ApplyPreviewThemeAsync();
    }

    /// <summary>
    /// Applies the current Twain theme without requesting
    /// another preview from the MediaWiki API.
    /// </summary>
    private async Task ApplyPreviewThemeAsync()
    {
        if (!_webViewReady)
        {
            return;
        }

        bool darkTheme =
            ThemeManager.CurrentTheme == TwainTheme.ModernDark;

        string css = darkTheme
            ? DarkPreviewCss
            : LightPreviewCss;

        string serializedCss =
            JsonSerializer.Serialize(css);

        string script = $$"""
            (function () {
                const styleId = 'twain-preview-theme';

                let style =
                    document.getElementById(styleId);

                if (!style) {
                    style = document.createElement('style');
                    style.id = styleId;
                    document.head.appendChild(style);
                }

                style.textContent = {{serializedCss}};
            })();
            """;

        await PreviewWebView.InvokeScript(script);
    }
}
