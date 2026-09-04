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

        LoadMonacoEditor();
    }

    /// <summary>
    /// Loads the bundled Monaco editor shell into the native web view.
    /// </summary>
    private void LoadMonacoEditor()
    {
        string monacoDirectory =
            Path.Combine(
                AppContext.BaseDirectory,
                "Monaco");

        string editorPath =
            Path.Combine(
                monacoDirectory,
                "MonacoEditor.html");

        string html =
            File.ReadAllText(editorPath);

        Uri baseUri =
            new Uri(
                monacoDirectory +
                Path.DirectorySeparatorChar);

        EditorWebView.NavigateToString(
            html,
            baseUri);
    }
}