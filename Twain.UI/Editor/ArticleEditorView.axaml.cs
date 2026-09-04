using Avalonia.Controls;
using System.IO;

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
}