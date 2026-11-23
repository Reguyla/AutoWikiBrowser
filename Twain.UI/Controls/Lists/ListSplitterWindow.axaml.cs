
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.Controls.Lists;
using Twain.Core.TwainSettings;
using Twain.UI.ViewModels.Lists;

namespace Twain.UI.Controls.Lists;

/// <summary>
/// Splits an article list into groups that can be saved
/// as separate text or Twain settings files.
/// </summary>
public partial class ListSplitterWindow : Window
{
    private readonly UserPrefs _prefs;
    private readonly MakeListViewModel _listViewModel;

    /// <summary>
    /// Initializes an empty list splitter using the active
    /// wiki session and application preferences.
    /// </summary>
    public ListSplitterWindow(
        Session session,
        UserPrefs prefs)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(prefs);

        InitializeComponent();

        _prefs = prefs;
        _listViewModel = new MakeListViewModel(session);

        ListMaker.DataContext = _listViewModel;

        _listViewModel.Articles.CollectionChanged +=
            (_, _) => UpdateControlState();

        UpdateControlState();
    }

    /// <summary>
    /// Initializes the splitter with an existing article list.
    /// </summary>
    public ListSplitterWindow(
        Session session,
        UserPrefs prefs,
        IEnumerable<Article> articles)
        : this(session, prefs)
    {
        ArgumentNullException.ThrowIfNull(articles);

        _listViewModel.ReplaceArticles(articles);
    }

    /// <summary>
    /// Enables or disables save controls based on the
    /// current article list.
    /// </summary>
    private void UpdateControlState()
    {
        bool hasArticles =
            _listViewModel.Articles.Count > 0;

        SaveTextButton.IsEnabled = hasArticles;
        SaveSettingsButton.IsEnabled = hasArticles;
    }

    /// <summary>
    /// Releases the temporary list view model.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _listViewModel.Dispose();

        base.OnClosed(e);
    }

    /// <summary>
    /// Returns an alphabetically sorted snapshot of the
    /// current article list.
    /// </summary>
    private List<Article> GetSortedArticles()
    {
        return _listViewModel.Articles
            .OrderBy(
                article => article.Name,
                StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Gets the selected number of articles per output file.
    /// </summary>
    private int GetArticlesPerFile()
    {
        return (int)(PagesPerFile.Value ?? 100);
    }

    /// <summary>
    /// Saves the article list as numbered text files.
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
                        GetSuggestedFileName("txt"),
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

        string? path = file.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            ListSplitterProcessor.SaveTextFiles(
                GetSortedArticles(),
                path,
                GetArticlesPerFile());

            await ShowMessageAsync(
                "Lists saved to text files.",
                "List splitter");

            _listViewModel.Articles.Clear();
        }
        catch (IOException ex)
        {
            await ShowMessageAsync(
                ex.Message,
                "Save error");
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Saves the article list as numbered Twain XML
    /// settings files.
    /// </summary>
    private async void SaveSettingsButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save settings files",
                    SuggestedFileName =
                        GetSuggestedFileName("xml"),
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

        string? path = file.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            ListSplitterProcessor.SaveSettingsFiles(
                _prefs,
                GetSortedArticles(),
                path,
                GetArticlesPerFile());

            await ShowMessageAsync(
                "Lists saved to Twain settings files.",
                "List splitter");

            _listViewModel.Articles.Clear();
        }
        catch (IOException ex)
        {
            await ShowMessageAsync(
                ex.Message,
                "Save error");
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Creates a suggested output filename based on
    /// the selected list source.
    /// </summary>
    private string GetSuggestedFileName(
        string extension)
    {
        string name =
            ListSplitterProcessor.CreateFileName(
                _listViewModel.SourceText);

        if (string.IsNullOrWhiteSpace(name))
            name = "Article list";

        return $"{name}.{extension}";
    }

    /// <summary>
    /// Displays a modal notification.
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
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner
        };

        Button okButton = new()
        {
            Content = "OK",
            MinWidth = 80,
            HorizontalAlignment =
                Avalonia.Layout.HorizontalAlignment.Right
        };

        okButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping =
                        Avalonia.Media.TextWrapping.Wrap
                },
                okButton
            }
        };

        await dialog.ShowDialog(this);
    }
}
