using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using Twain.Core.Lists.Providers;

namespace Twain.UI.Lists.Providers;

/// <summary>
/// Provides the Avalonia dialog used to configure a special-page list request.
/// </summary>
public partial class SpecialPageListProviderWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpecialPageListProviderWindow"/> class.
    /// </summary>
    public SpecialPageListProviderWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the currently selected special-page provider option.
    /// </summary>
    public SpecialPageProviderOption? SelectedProvider =>
        SourceComboBox.SelectedItem as SpecialPageProviderOption;

    public SpecialPageListProviderWindow(
    SpecialPageListProvider.DialogRequest request)
    : this()
    {
        ArgumentNullException.ThrowIfNull(request);

        SetProviders(
            request.Providers);

        SetNamespaces(
            request.Namespaces);
    }

    /// <summary>
    /// Creates the dialog selection represented by the current control values.
    /// </summary>
    /// <returns>
    /// The selected provider, namespace, and page criteria, or
    /// <see langword="null"/> when no provider is selected.
    /// </returns>
    public SpecialPageListProvider.DialogSelection? CreateSelection()
    {
        if (SelectedProvider is not { } provider)
        {
            return null;
        }

        return new SpecialPageListProvider.DialogSelection(
            provider.Index,
            NamespaceText,
            PagesText);
    }

    /// <summary>
    /// Gets the page criteria entered by the user.
    /// </summary>
    public string PagesText =>
        PagesTextBox.Text ?? string.Empty;

    /// <summary>
    /// Gets the namespace selected by the user.
    /// </summary>
    public string NamespaceText =>
        NamespaceComboBox.SelectedItem as string ??
        string.Empty;

    /// <summary>
    /// Populates the source selector with the available special-page providers.
    /// </summary>
    /// <param name="providers">
    /// The provider options available for selection.
    /// </param>
    public void SetProviders(
        IReadOnlyList<SpecialPageProviderOption> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        SourceComboBox.ItemsSource =
            providers;

        if (SourceComboBox.ItemCount > 0)
        {
            SourceComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Populates the namespace selector with the available namespaces.
    /// </summary>
    /// <param name="namespaces">
    /// The namespace names available for selection.
    /// </param>
    public void SetNamespaces(
        IReadOnlyList<string> namespaces)
    {
        ArgumentNullException.ThrowIfNull(namespaces);

        NamespaceComboBox.ItemsSource =
            namespaces;

        if (NamespaceComboBox.ItemCount > 0)
        {
            NamespaceComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Updates the input controls to reflect the capabilities of the selected
    /// special-page provider.
    /// </summary>
    /// <param name="sender">
    /// The source combo box.
    /// </param>
    /// <param name="e">
    /// The selection-changed event data.
    /// </param>
    private void SourceComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (SelectedProvider is not { } provider)
        {
            return;
        }

        PagesTextBox.IsEnabled =
            provider.PagesEnabled;

        NamespaceComboBox.IsEnabled =
            provider.NamespacesEnabled;
    }

    /// <summary>
    /// Accepts the current selections and closes the dialog.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// Cancels the operation and closes the dialog.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}