using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Twain.UI;

/// <summary>
/// Resolves a view-model instance to its conventionally named Avalonia view.
/// </summary>
/// <remarks>
/// View models and views must share the same namespace and follow the naming
/// convention <c>FeatureViewModel</c> and <c>FeatureView</c>. For example,
/// <c>Twain.UI.Editor.ArticleEditorViewModel</c> resolves to
/// <c>Twain.UI.Editor.ArticleEditorView</c>.
/// </remarks>
[RequiresUnreferencedCode(
    "The default ViewLocator implementation uses reflection to create views, " +
    "and the required view types may be removed by trimming.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public sealed class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Creates the view associated with the specified view model.
    /// </summary>
    /// <param name="param">
    /// The view-model instance for which a view should be created.
    /// </param>
    /// <returns>
    /// The resolved view, <see langword="null"/> when no view model was
    /// supplied, or a diagnostic text block when the expected view type
    /// cannot be found.
    /// </returns>
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        string viewTypeName = param
            .GetType()
            .FullName!
            .Replace(
                "ViewModel",
                "View",
                StringComparison.Ordinal);

        Type? viewType = Type.GetType(viewTypeName);

        if (viewType is not null)
        {
            return (Control)Activator.CreateInstance(viewType)!;
        }

        return new TextBlock
        {
            Text = $"View not found: {viewTypeName}"
        };
    }

    /// <summary>
    /// Determines whether this template can resolve a view for the supplied
    /// data object.
    /// </summary>
    /// <param name="data">
    /// The object being evaluated by Avalonia's data-template system.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="data"/> is a Twain view
    /// model; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}