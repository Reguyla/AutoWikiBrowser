using Avalonia.Controls;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace Twain.UI.Diff;

public partial class DiffView : UserControl
{
    private DiffViewModel? _subscribedViewModel;
    private bool _webViewReady;

    public DiffView()
    {
        InitializeComponent();

        DiffWebView.AdapterCreated +=
            DiffWebView_AdapterCreated;
    }

    private void DiffWebView_AdapterCreated(
        object? sender,
        WebViewAdapterEventArgs e)
    {
        DiffWebView.AdapterCreated -=
            DiffWebView_AdapterCreated;

        _webViewReady = true;

        _ = LoadDiffHtmlAsync();
    }

    protected override void OnDataContextChanged(
        EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged -=
                ViewModel_PropertyChanged;
        }

        _subscribedViewModel = null;

        if (DataContext is DiffViewModel viewModel)
        {
            _subscribedViewModel = viewModel;
            _subscribedViewModel.PropertyChanged +=
                ViewModel_PropertyChanged;
        }

        base.OnDataContextChanged(e);

        if (_webViewReady)
        {
            _ = LoadDiffHtmlAsync();
        }
    }

    private async void ViewModel_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
            nameof(DiffViewModel.DiffHtml))
        {
            return;
        }

        await LoadDiffHtmlAsync();
    }

    private async Task LoadDiffHtmlAsync()
    {
        if (!_webViewReady ||
            DataContext is not DiffViewModel viewModel)
        {
            return;
        }

        string html =
            JsonSerializer.Serialize(
                viewModel.DiffHtml);

        await DiffWebView.InvokeScript(
            $"document.open();" +
            $"document.write({html});" +
            $"document.close();");
    }
}