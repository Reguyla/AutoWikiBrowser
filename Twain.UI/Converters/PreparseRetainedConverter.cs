using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Collections.Generic;
using System.Globalization;
using Twain.Core;
using Twain.UI.ViewModels.Lists;

namespace Twain.UI.Converters;

public sealed class PreparseRetainedConverter : IMultiValueConverter
{
    private static readonly IBrush RetainedBrush =
        new SolidColorBrush(Color.Parse("#D8F0D8"));

    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count >= 3 &&
            values[0] is Article article &&
            values[1] is MakeListViewModel viewModel &&
            viewModel.IsPreparseRetained(article))
        {
            return RetainedBrush;
        }

        return Brushes.Transparent;
    }
}