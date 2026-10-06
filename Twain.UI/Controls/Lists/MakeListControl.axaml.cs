using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.Lists;
using Twain.UI.Lists;
using Twain.UI.ViewModels.Lists;

namespace Twain.UI.Controls.Lists;

public partial class MakeListControl : UserControl
{
    public MakeListControl()
    {
        InitializeComponent();
    }

    private void RemoveButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel)
            return;

        Article[] selectedArticles =
            ArticleListBox.SelectedItems?
                .OfType<Article>()
                .ToArray()
            ?? [];

        if (selectedArticles.Length == 0)
            return;

        viewModel.RemoveArticlesCommand.Execute(selectedArticles);
    }

    private void ArticleListBox_DoubleTapped(
        object? sender,
        TappedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel ||
            viewModel.SelectedArticle is null)
        {
            return;
        }

        viewModel.OpenSelectedArticleCommand.Execute(null);
    }

    private async void ArticleListBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel)
        {
            return;
        }

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.A:
                    ArticleListBox.SelectAll();
                    e.Handled = true;
                    return;

                case Key.C:
                    await CopySelectedArticlesAsync();
                    e.Handled = true;
                    return;

                case Key.X:
                    await CutSelectedArticlesAsync(viewModel);
                    e.Handled = true;
                    return;

                case Key.V:
                    await PasteArticlesAsync(viewModel);
                    e.Handled = true;
                    return;
            }
        }

        if (e.KeyModifiers == KeyModifiers.None &&
           e.Key == Key.Delete)
        {
            Article[] selectedArticles =
                GetSelectedArticles();

            if (selectedArticles.Length == 0)
            {
                return;
            }

            viewModel.RemoveArticlesCommand.Execute(
                selectedArticles);

            e.Handled = true;
            return;
        }

        if (viewModel.SelectedArticle is null ||
            e.KeyModifiers !=
                (KeyModifiers.Control | KeyModifiers.Shift))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.P:
                viewModel.OpenSelectedArticleCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.H:
                viewModel.OpenSelectedArticleHistoryCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.T:
                viewModel.OpenSelectedArticleTalkCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private Article[] GetSelectedArticles()
    {
        return ArticleListBox.SelectedItems?
            .OfType<Article>()
            .ToArray()
            ?? [];
    }

    private async Task<bool> CopySelectedArticlesAsync()
    {
        Article[] selectedArticles =
            GetSelectedArticles();

        if (selectedArticles.Length == 0)
        {
            return false;
        }

        TopLevel? topLevel =
            TopLevel.GetTopLevel(this);

        if (topLevel?.Clipboard is null)
        {
            return false;
        }

        string text =
            string.Join(
                Environment.NewLine,
                selectedArticles.Select(
                    article => article.Name));

        await topLevel.Clipboard.SetTextAsync(text);

        return true;
    }

    private async Task PasteArticlesAsync(
        MakeListViewModel viewModel)
    {
        TopLevel? topLevel =
            TopLevel.GetTopLevel(this);

        if (topLevel?.Clipboard is null)
        {
            return;
        }

        string? text =
            await topLevel.Clipboard.TryGetTextAsync();

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string[] titles =
            text.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        viewModel.AddArticleTitles(
            titles);
    }

    private async Task CutSelectedArticlesAsync(
        MakeListViewModel viewModel)
    {
        Article[] selectedArticles =
            GetSelectedArticles();

        if (selectedArticles.Length == 0)
        {
            return;
        }

        if (!await CopySelectedArticlesAsync())
        {
            return;
        }

        viewModel.RemoveArticlesCommand.Execute(
            selectedArticles);
    }

    private async void CutMenuItem_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel)
        {
            return;
        }

        await CutSelectedArticlesAsync(
            viewModel);
    }

    private async void CopyMenuItem_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CopySelectedArticlesAsync();
    }

    private async void PasteMenuItem_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel)
        {
            return;
        }

        await PasteArticlesAsync(
            viewModel);
    }

    private void SelectAllMenuItem_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ArticleListBox.SelectAll();
    }

    private void RemoveMenuItem_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel)
        {
            return;
        }

        Article[] selectedArticles =
            GetSelectedArticles();

        if (selectedArticles.Length == 0)
        {
            return;
        }

        viewModel.RemoveArticlesCommand.Execute(
            selectedArticles);
    }

    private void MoveToTopMenuItem_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel)
        {
            return;
        }

        Article[] selectedArticles =
            GetSelectedArticles();

        if (selectedArticles.Length == 0)
        {
            return;
        }

        viewModel.MoveArticlesToTopCommand.Execute(
            selectedArticles);
    }

    private void MoveToBottomMenuItem_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel)
        {
            return;
        }

        Article[] selectedArticles =
            GetSelectedArticles();

        if (selectedArticles.Length == 0)
        {
            return;
        }

        viewModel.MoveArticlesToBottomCommand.Execute(
            selectedArticles);
    }

    private async void SaveListMenuItem_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel ||
            viewModel.Articles.Count == 0)
        {
            return;
        }

        TopLevel? topLevel =
            TopLevel.GetTopLevel(this);

        if (topLevel is null)
        {
            return;
        }

        var file =
            await topLevel.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Save Article List",
                    SuggestedFileName = "ArticleList.txt",
                    DefaultExtension = "txt",
                    FileTypeChoices =
                    [
                        new Avalonia.Platform.Storage.FilePickerFileType(
                        "Text files")
                    {
                        Patterns = ["*.txt"]
                    }
                    ]
                });

        if (file is null)
        {
            return;
        }

        await using Stream stream =
            await file.OpenWriteAsync();

        await using StreamWriter writer =
            new(stream);

        foreach (Article article in viewModel.Articles)
        {
            await writer.WriteLineAsync(article.Name);
        }
    }

    private async void FilterMenuItem_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MakeListViewModel viewModel ||
            viewModel.Articles.Count < 2)
        {
            return;
        }

        ArticleListFilterConfiguration configuration = new();

        ListFilterWindow window =
            new(configuration);

        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        bool applied =
            await window.ShowDialog<bool>(owner);

        if (!applied)
            return;

        List<Article> comparisonArticles =
            configuration.ComparisonArticleTitles
                .Select(title => new Article(title))
                .ToList();

        List<Article> filteredArticles =
            ArticleListFilterProcessor.Apply(
                viewModel.Articles,
                comparisonArticles,
                configuration.NamespaceIds,
                configuration.ContainsText,
                configuration.DoesNotContainText,
                configuration.FilterTitlesThatContain,
                configuration.FilterTitlesThatDoNotContain,
                configuration.UseRegex,
                configuration.RemoveDuplicates,
                configuration.ComparisonArticleTitles.Count > 0,
                configuration.IntersectComparisonList,
                configuration.SortAscending);

        viewModel.ReplaceArticles(filteredArticles);
    }
}