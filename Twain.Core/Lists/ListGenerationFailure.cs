namespace Twain.Core.Lists;

/// <summary>
/// Identifies a known failure that occurred while generating an article list.
/// </summary>
public enum ListGenerationFailure
{
    /// <summary>
    /// No known failure occurred.
    /// </summary>
    None,

    /// <summary>
    /// The selected list provider is unavailable because its required
    /// wiki feature is disabled.
    /// </summary>
    FeatureDisabled,

    /// <summary>
    /// The current wiki session is no longer logged in.
    /// </summary>
    LoggedOff,

    /// <summary>
    /// The list provider rejected the supplied title.
    /// </summary>
    InvalidTitle,

    /// <summary>
    /// The list provider returned an API error that does not require
    /// user-facing handling.
    /// </summary>
    ApiError,

    /// <summary>
    /// An invalid parameter was supplied to the list provider.
    /// </summary>
    InvalidParameter,

    /// <summary>
    /// An interwiki title was supplied to the list provider.
    /// </summary>
    InterwikiTitle,

    /// <summary>
    /// An invalid article title was supplied to the list provider.
    /// </summary>
    InvalidArticleTitle
}