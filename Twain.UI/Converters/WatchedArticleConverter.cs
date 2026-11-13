
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Twain.Core;
using Twain.UI.ViewModels.Lists;

namespace Twain.UI.Converters;

/// <summary>
/// Displays a star beside watched articles in Make List.
/// </summary>
public sealed class WatchedArticleConverter : IMultiValueConverter
{
    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count < 3 ||
            values[0] is not Article article ||
            values[1] is not MakeListViewModel viewModel ||
            values[2] is not int)
        {
            return false;
        }

        return viewModel.HighlightWatchedArticles &&
               viewModel.IsArticleWatched(article.Name);
    }
}
