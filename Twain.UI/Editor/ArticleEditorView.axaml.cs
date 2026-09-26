using Avalonia.Controls;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Twain.Core;
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
    private string _lastSearchText = string.Empty;
    private bool _lastSearchIsRegex;
    private bool _lastSearchCaseSensitive;
    private string _lastSearchArticleName = string.Empty;
    private bool _updatingDocumentFromMonaco;

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

            _updatingDocumentFromMonaco = true;

            try
            {
                viewModel.Document.CurrentText = text;
            }
            finally
            {
                _updatingDocumentFromMonaco = false;
            }
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

        ResetFind();

        if (_updatingDocumentFromMonaco)
        {
            return;
        }

        await LoadDocumentTextAsync();
    }

    /// <summary>
    /// Clears the state associated with the current incremental find operation.
    /// </summary>
    public void ResetFind()
    {
        _searchState.Reset();
    }

    /// <summary>
    /// Finds and selects the next occurrence of the specified search expression
    /// in the current article text.
    /// </summary>
    /// <param name="searchText">
    /// The text or regular expression to search for.
    /// </param>
    /// <param name="isRegex">
    /// <see langword="true"/> when <paramref name="searchText"/> should be
    /// interpreted as a regular expression; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="caseSensitive">
    /// <see langword="true"/> to perform a case-sensitive search; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="articleName">
    /// The current article name used when expanding AWB search keywords.
    /// </param>
    public async Task FindNextAsync(
        string searchText,
        bool isRegex,
        bool caseSensitive,
        string articleName)
    {
        if (DataContext is not ArticleEditorViewModel viewModel)
        {
            return;
        }

        string articleText =
            Tools.ConvertFromLocalLineEndings(
                viewModel.Document.CurrentText);

        int selectionStart =
            await GetSelectionStartAsync();

        Match? match =
            ArticleSearchHelper.FindNext(
                articleText,
                searchText,
                isRegex,
                caseSensitive,
                articleName,
                selectionStart,
                _searchState);

        if (match is null)
        {
            await SelectTextAsync(0, 0);
            return;
        }

        await SelectTextAsync(
            match.Index,
            match.Length);
    }
}