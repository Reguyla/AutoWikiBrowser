using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.AWBSettings;
using Twain.Core.Controls.Lists;

namespace Twain.UI.Controls.Lists;

/// <summary>
/// Splits an article list into groups that can be saved as separate files.
/// </summary>
public partial class ListSplitterWindow : Avalonia.Controls.Window
{

    private readonly UserPrefs? _prefs;


    /// <summary>
    /// Initializes a new empty list splitter window.
    /// </summary>
    public ListSplitterWindow()
    {
        InitializeComponent();

        ListMaker.MakeListEnabled = true;
        UpdateControlState();
    }

    /// <summary>
    /// Initializes a new list splitter window with the supplied articles.
    /// </summary>
    /// <param name="articles">
    /// The articles to place in the list.
    /// </param>
    public ListSplitterWindow(
        IEnumerable<Article> articles)
        : this()
    {
        ArgumentNullException.ThrowIfNull(articles);

        ListMaker.AddArticles(articles);
    }

    /// <summary>
    /// Updates splitter controls when the article list changes.
    /// </summary>
    private void ListMaker_NoOfArticlesChanged(
        object? sender,
        EventArgs e)
    {
        UpdateControlState();
    }

    /// <summary>
    /// Enables or disables controls that require an article list.
    /// </summary>
    private void UpdateControlState()
    {
        bool hasArticles =
            ListMaker.Count > 0;

        SaveTextButton.IsEnabled =
            hasArticles;

        SaveSettingsButton.IsEnabled =
            hasArticles &&
            _prefs != null;
    }

    /// <summary>
    /// Prompts for a base text file path and saves the current article list
    /// as numbered text files.
    /// </summary>
    private async void SaveTextButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save article list",
                    SuggestedFileName =
                        ListSplitterProcessor.CreateFileName(
                            ListMaker.SourceText),
                    DefaultExtension = "txt",
                    FileTypeChoices =
                    [
                        new FilePickerFileType("Text files")
                    {
                        Patterns = ["*.txt"]
                    }
                    ]
                });

        if (file == null)
            return;

        string? path =
            file.TryGetLocalPath();

        if (string.IsNullOrEmpty(path))
            return;

        int articlesPerFile =
            (int)(PagesPerFile.Value ?? 100);

        ListMaker.AlphaSortList();

        List<Article> articles =
            ListMaker.GetArticleList();

        ListSplitterProcessor.SaveTextFiles(
            articles,
            path,
            articlesPerFile);

        ListMaker.Clear();
    }

    /// <summary>
    /// Initializes a new list splitter window using the supplied preferences.
    /// </summary>
    /// <param name="prefs">
    /// The preferences used as the basis for generated settings files.
    /// </param>
    public ListSplitterWindow(
        UserPrefs prefs)
        : this()
    {
        ArgumentNullException.ThrowIfNull(prefs);

        _prefs = prefs;
        UpdateControlState();
    }

    /// <summary>
    /// Initializes a new list splitter window using the supplied preferences
    /// and article list.
    /// </summary>
    /// <param name="prefs">
    /// The preferences used as the basis for generated settings files.
    /// </param>
    /// <param name="articles">
    /// The articles to place in the list.
    /// </param>
    public ListSplitterWindow(
        UserPrefs prefs,
        IEnumerable<Article> articles)
        : this(prefs)
    {
        ArgumentNullException.ThrowIfNull(articles);

        ListMaker.AddArticles(articles);
    }

    /// <summary>
    /// Prompts for a base settings file path and saves the current article
    /// list as numbered settings files.
    /// </summary>
    private async void SaveSettingsButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_prefs == null)
            return;

        IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save settings files",
                    SuggestedFileName =
                        ListSplitterProcessor.CreateFileName(
                            ListMaker.SourceText),
                    DefaultExtension = "xml",
                    FileTypeChoices =
                    [
                        new FilePickerFileType("XML files")
                    {
                        Patterns = ["*.xml"]
                    }
                    ]
                });

        if (file == null)
            return;

        string? path =
            file.TryGetLocalPath();

        if (string.IsNullOrEmpty(path))
            return;

        int articlesPerFile =
            (int)(PagesPerFile.Value ?? 100);

        List<Article> articles =
            ListMaker.GetArticleList();

        try
        {
            ListSplitterProcessor.SaveTextFiles(
                articles,
                path,
                articlesPerFile);

            await ShowMessageAsync(
                "Lists saved to text files",
                "List splitter");

            ListMaker.Clear();
        }
        catch (IOException ex)
        {
            await ShowMessageAsync(
                ex.Message,
                "Save error");
        }
    }

    /// <summary>
    /// Displays a simple modal message to the user.
    /// </summary>
    private async Task ShowMessageAsync(
        string message,
        string title)
    {
        Window dialog = new()
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        Button okButton = new()
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            MinWidth = 80
        };

        okButton.Click += (_, _) => dialog.Close();

        dialog.Content =
            new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 16,
                Children =
                {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                okButton
                }
            };

        await dialog.ShowDialog(this);
    }
}