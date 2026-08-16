using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Twain.CustomModules;

/// <summary>
/// Displays compilation or execution errors produced by a custom module.
/// </summary>
public partial class CustomModuleErrors : Window
{
    /// <summary>
    /// Initializes an empty custom module error window.
    /// </summary>
    public CustomModuleErrors()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the custom module error window with the supplied error text.
    /// </summary>
    /// <param name="errorText">
    /// The error information to display.
    /// </param>
    public CustomModuleErrors(string errorText)
        : this()
    {
        ErrorsTextBox.Text = errorText;
    }

    /// <summary>
    /// Closes the error window.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}