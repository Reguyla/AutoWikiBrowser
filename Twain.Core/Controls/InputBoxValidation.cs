namespace Twain.Core.Controls;

/// <summary>
/// Stores the result of an input-box operation.
/// </summary>
public class InputBoxResult
{
    /// <summary>
    /// Indicates whether the input was accepted.
    /// </summary>
    public bool OK;

    /// <summary>
    /// Contains the text entered by the user.
    /// </summary>
    public string Text;
}

/// <summary>
/// Provides data for input-box validation.
/// </summary>
public class InputBoxValidatingArgs : EventArgs
{
    /// <summary>
    /// Gets the text being validated.
    /// </summary>
    public string Text;

    /// <summary>
    /// Gets the validation message to display when validation fails.
    /// </summary>
    public string Message;

    /// <summary>
    /// Indicates whether the current input should be rejected.
    /// </summary>
    public bool Cancel;
}

/// <summary>
/// Represents a method that validates text entered into an input box.
/// </summary>
/// <param name="sender">
/// The object requesting validation.
/// </param>
/// <param name="e">
/// The validation data.
/// </param>
public delegate void InputBoxValidatingHandler(
    object sender,
    InputBoxValidatingArgs e);