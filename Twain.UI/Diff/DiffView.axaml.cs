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

        DiffWebView.WebMessageReceived +=
            DiffWebView_WebMessageReceived;
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

    /// <summary>
    /// Handles navigation and undo requests sent from the rendered diff.
    /// </summary>
    private async void DiffWebView_WebMessageReceived(
        object? sender,
        WebMessageReceivedEventArgs e)
    {
        using JsonDocument message =
            JsonDocument.Parse(e.Body);

        JsonElement root =
            message.RootElement;

        if (!root.TryGetProperty(
                "action",
                out JsonElement actionElement))
        {
            return;
        }

        string? action =
            actionElement.GetString();

        if (DataContext is not DiffViewModel viewModel)
        {
            return;
        }

        switch (action)
        {
            case "GoTo":
                if (TryGetLine(
                        root,
                        "rightLine",
                        out int rightLine))
                {
                    await viewModel.GoToLineAsync(
                        rightLine);
                }

                break;

            case "UndoChange":
                if (TryGetLine(
                        root,
                        "leftLine",
                        out int leftLine) &&
                    TryGetLine(
                        root,
                        "rightLine",
                        out int changedRightLine))
                {
                    viewModel.UndoChange(
                        leftLine,
                        changedRightLine);
                }

                break;

            case "UndoAddition":
                if (TryGetLine(
                        root,
                        "rightLine",
                        out int addedRightLine))
                {
                    viewModel.UndoAddition(
                        addedRightLine);
                }

                break;

            case "UndoDeletion":
                if (TryGetLine(
                        root,
                        "leftLine",
                        out int deletedLeftLine) &&
                    TryGetLine(
                        root,
                        "rightLine",
                        out int deletedRightLine))
                {
                    viewModel.UndoDeletion(
                        deletedLeftLine,
                        deletedRightLine);
                }

                break;
        }
    }

    /// <summary>
    /// Reads a line number from a diff web message.
    /// </summary>
    private static bool TryGetLine(
        JsonElement message,
        string propertyName,
        out int line)
    {
        line = 0;

        return
            message.TryGetProperty(
                propertyName,
                out JsonElement lineElement) &&
            lineElement.ValueKind ==
                JsonValueKind.Number &&
            lineElement.TryGetInt32(
                out line);
    }
}