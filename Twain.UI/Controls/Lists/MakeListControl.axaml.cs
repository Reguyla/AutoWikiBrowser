using Avalonia.Controls;
using System.Linq;
using Twain.Core;
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
}