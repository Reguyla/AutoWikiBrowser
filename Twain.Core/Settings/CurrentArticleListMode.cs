namespace Twain.Core.Settings;

/// <summary>
/// Specifies whether an auxiliary tool should use the current article list.
/// </summary>
public enum CurrentArticleListMode
{
    /// <summary>
    /// Prompts the user before using the current article list.
    /// </summary>
    Ask = 0,

    /// <summary>
    /// Always uses the current article list.
    /// </summary>
    Always = 1,

    /// <summary>
    /// Never uses the current article list.
    /// </summary>
    Never = 2
}