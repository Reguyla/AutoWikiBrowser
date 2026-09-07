using Avalonia.Controls;
using Avalonia.Interactivity;
using Twain.Core.Controls;

namespace Twain.UI.Controls;

/// <summary>
/// Displays a prompt and allows the user to enter and validate a text value.
/// </summary>
public partial class InputBoxWindow : Avalonia.Controls.Window
{
    private readonly InputBoxValidatingHandler? _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputBoxWindow"/> class
    /// for the XAML previewer.
    /// </summary>
    public InputBoxWindow()
        : this(
            "Prompt",
            "Input",
            string.Empty,
            null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputBoxWindow"/> class.
    /// </summary>
    /// <param name="prompt">
    /// The prompt displayed above the input field.
    /// </param>
    /// <param name="title">
    /// The title displayed in the window title bar.
    /// </param>
    /// <param name="defaultResponse">
    /// The initial text displayed in the input field.
    /// </param>
    /// <param name="validator">
    /// An optional delegate used to validate the entered text.
    /// </param>
    public InputBoxWindow(
        string prompt,
        string title,
        string defaultResponse,
        InputBoxValidatingHandler? validator)
    {
        InitializeComponent();

        PromptTextBlock.Text =
            prompt ?? string.Empty;

        Title =
            title ?? string.Empty;

        InputTextBox.Text =
            defaultResponse ?? string.Empty;

        _validator =
            validator;
    }

    /// <summary>
    /// Gets the text currently entered in the dialog.
    /// </summary>
    public string Text =>
        InputTextBox.Text ?? string.Empty;

    /// <summary>
    /// Gets whether the user accepted the entered value.
    /// </summary>
    public bool Accepted { get; private set; }

    /// <summary>
    /// Clears the displayed validation message when the input text changes.
    /// </summary>
    private void InputTextBox_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        ClearValidationMessage();
    }

    /// <summary>
    /// Validates and accepts the current input.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (!ValidateInput())
            return;

        Accepted = true;
        Close(true);
    }

    /// <summary>
    /// Closes the dialog without accepting the current input.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Accepted = false;
        Close(false);
    }

    /// <summary>
    /// Validates the current input using the configured validator.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the input is valid or no validator is
    /// configured; otherwise, <see langword="false"/>.
    /// </returns>
    private bool ValidateInput()
    {
        if (_validator is null)
            return true;

        InputBoxValidatingArgs args =
            new()
            {
                Text = Text
            };

        _validator(
            this,
            args);

        if (!args.Cancel)
            return true;

        ValidationTextBlock.Text =
            args.Message ?? string.Empty;

        ValidationTextBlock.IsVisible =
            true;

        return false;
    }

    /// <summary>
    /// Clears the current validation message.
    /// </summary>
    private void ClearValidationMessage()
    {
        ValidationTextBlock.Text =
            string.Empty;

        ValidationTextBlock.IsVisible =
            false;
    }
}