using Avalonia.Controls;
using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Twain.UI.ViewModels.Workspaces;

namespace Twain.UI.Preview;

/// <summary>
/// Displays rendered MediaWiki article previews.
/// </summary>
public partial class PreviewView : UserControl
{
    private WorkspaceViewModel? _subscribedViewModel;
    private bool _webViewReady;

    public PreviewView()
    {
        InitializeComponent();

        PreviewWebView.AdapterCreated +=
            PreviewWebView_AdapterCreated;
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

        _subscribedViewModel = DataContext as WorkspaceViewModel;

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
    }
}