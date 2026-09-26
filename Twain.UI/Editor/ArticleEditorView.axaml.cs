using Avalonia.Controls;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Twain.Core.Editing;

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
    private ArticleDocumentViewModel? _subscribedDocument;
    private readonly ArticleSearchHelper.ArticleSearchState _searchState = new();

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

    /// <summary>
    /// Selects the specified range of article text in the Monaco editor.
    /// </summary>
    /// <param name="start">
    /// The zero-based character offset at which the selection begins.
    /// </param>
    /// <param name="length">
    /// The number of characters to select.
    /// </param>
    public async Task SelectTextAsync(
        int start,
        int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(start));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length));
        }

        await EditorWebView.InvokeScript(
            $"window.twainEditor.selectText({start}, {length});");
    }

    /// <summary>
    /// Gets the current zero-based caret offset in the Monaco editor.
    /// </summary>
    /// <returns>
    /// The current character offset within the article text.
    /// </returns>
    public async Task<int> GetCaretOffsetAsync()
    {
        string result =
            await EditorWebView.InvokeScript(
                "window.twainEditor.getCaretOffset();");

        return int.TryParse(
            result,
            out int offset)
                ? offset
                : 0;
    }

    /// <summary>
    /// Gets the zero-based starting offset of the current Monaco selection.
    /// </summary>
    /// <returns>
    /// The character offset at which the current selection begins.
    /// </returns>
    public async Task<int> GetSelectionStartAsync()
    {
        string result =
            await EditorWebView.InvokeScript(
                "window.twainEditor.getSelectionStart();");

        return int.TryParse(
            result,
            out int offset)
                ? offset
                : 0;
    }

    /// <summary>
    /// Updates the document subscription when the editor data context changes.
    /// </summary>
    protected override void OnDataContextChanged(
        EventArgs e)
    {
        if (_subscribedDocument is not null)
        {
            _subscribedDocument.PropertyChanged -=
                Document_PropertyChanged;
        }

        _subscribedDocument = null;

        if (DataContext is ArticleEditorViewModel viewModel)
        {
            _subscribedDocument = viewModel.Document;

            _subscribedDocument.PropertyChanged +=
                Document_PropertyChanged;
        }

        base.OnDataContextChanged(e);
    }

    /// <summary>
    /// Synchronizes document text changes to the Monaco editor.
    /// </summary>
    private async void Document_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
            nameof(ArticleDocumentViewModel.CurrentText))
        {
            return;
        }

        await LoadDocumentTextAsync();
    }
}