namespace AutoWikiBrowser.Services.Settings;

/// <summary>
/// Identifies the outcome of an attempt to save application settings.
/// </summary>
internal enum SettingsSaveFailure
{
    /// <summary>
    /// The settings were saved successfully.
    /// </summary>
    None,

    /// <summary>
    /// The settings could not be saved because the destination was not
    /// writable.
    /// </summary>
    UnauthorizedAccess,

    /// <summary>
    /// The settings could not be saved because an input/output operation
    /// failed.
    /// </summary>
    IoError,

    /// <summary>
    /// The settings could not be saved because of an unexpected error.
    /// </summary>
    Unexpected
}

/// <summary>
/// Contains the result of an application settings save operation.
/// </summary>
internal sealed class SettingsSaveResult
{
    /// <summary>
    /// Initializes a new settings save result.
    /// </summary>
    /// <param name="failure">
    /// The category of failure, or <see cref="SettingsSaveFailure.None"/> when
    /// the save completed successfully.
    /// </param>
    /// <param name="exception">
    /// The exception raised during the save operation, if any.
    /// </param>
    internal SettingsSaveResult(
        SettingsSaveFailure failure,
        Exception? exception = null)
    {
        Failure = failure;
        Exception = exception;
    }

    /// <summary>
    /// Gets the category of the save failure.
    /// </summary>
    internal SettingsSaveFailure Failure { get; }

    /// <summary>
    /// Gets the exception raised during the save operation, if any.
    /// </summary>
    internal Exception? Exception { get; }

    /// <summary>
    /// Gets a value indicating whether the settings were saved successfully.
    /// </summary>
    internal bool Succeeded =>
        Failure == SettingsSaveFailure.None;
}