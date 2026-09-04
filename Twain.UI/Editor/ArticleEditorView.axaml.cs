using Avalonia.Controls;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Twain.UI.Editor;

/// <summary>
/// Displays the editable text of the active article document.
/// </summary>
/// <remarks>
/// Hosts the Monaco editor while retaining the existing pane, view-model,
/// and shared-document boundaries.
/// </remarks>
public partial class ArticleEditorView : UserControl
{
    /// <summary>
    /// Initializes the article editor view.
    /// </summary>
    public ArticleEditorView()
    {
        InitializeComponent();

        EditorWebView.AdapterCreated +=
            EditorWebView_AdapterCreated;

        EditorWebView.WebMessageReceived +=
            EditorWebView_WebMessageReceived;
    }

    /// <summary>
    /// Loads Monaco after the native web view adapter has been initialized.
    /// </summary>
    private void EditorWebView_AdapterCreated(
        object? sender,
        WebViewAdapterEventArgs e)
    {
        EditorWebView.AdapterCreated -=
            EditorWebView_AdapterCreated;

        LoadMonacoEditor();
    }

    /// <summary>
    /// Loads the bundled Monaco editor shell into the native web view.
    /// </summary>
    private void LoadMonacoEditor()
    {
        string editorPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Monaco",
                "MonacoEditor.html");

        Uri editorUri =
            new Uri(
                editorPath,
                UriKind.Absolute);

        EditorWebView.Navigate(editorUri);
    }

    /// <summary>
    /// Handles messages sent from the Monaco editor.
    /// </summary>
    private async void EditorWebView_WebMessageReceived(
        object? sender,
        WebMessageReceivedEventArgs e)
    {
        using JsonDocument message =
            JsonDocument.Parse(e.Body);

        string? messageType =
            message.RootElement
                .GetProperty("type")
                .GetString();

        if (messageType == "ready")
        {
            await LoadDocumentTextAsync();

            return;
        }

        if (messageType == "textChanged" &&
            DataContext is ArticleEditorViewModel viewModel)
        {
            string text =
                message.RootElement
                    .GetProperty("text")
                    .GetString()
                ?? string.Empty;

            viewModel.Document.CurrentText = text;
        }
    }

    /// <summary>
    /// Loads the current document text into Monaco.
    /// </summary>
    private async Task LoadDocumentTextAsync()
    {
        if (DataContext is not ArticleEditorViewModel viewModel)
        {
            return;
        }

        string jsonText =
            JsonSerializer.Serialize(
                viewModel.Document.CurrentText);

        await EditorWebView.InvokeScript(
            $"window.twainEditor.setText({jsonText});");
    }
}