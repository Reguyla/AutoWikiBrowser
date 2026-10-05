using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Twain.Core;

namespace Twain.UI.Converters;

/// <summary>
/// Selects the text used to display an article in an article list.
/// </summary>
public sealed class ArticleDisplayTitleConverter : IMultiValueConverter
{
    public object Convert(
        System.Collections.Generic.IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count < 2 ||
            values[0] is not Article article ||
            values[1] is not bool formatDisplayTitle ||
            !formatDisplayTitle ||
            string.IsNullOrWhiteSpace(article.DisplayTitle))
        {
            return values.Count > 0 &&
                   values[0] is Article fallbackArticle
                ? fallbackArticle.Name
                : string.Empty;
        }

        return article.DisplayTitle;
    }
}