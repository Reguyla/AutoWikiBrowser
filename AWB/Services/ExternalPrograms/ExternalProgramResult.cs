namespace AutoWikiBrowser.Services.ExternalPrograms;

/// <summary>
/// Contains the result of processing article text with an external program.
/// </summary>
internal sealed class ExternalProgramResult
{
    /// <summary>
    /// Initializes an external program processing result.
    /// </summary>
    /// <param name="articleText">
    /// The article text produced by the external program.
    /// </param>
    /// <param name="skip">
    /// Whether the article should be skipped.
    /// </param>
    internal ExternalProgramResult(
        string articleText,
        bool skip)
    {
        ArgumentNullException.ThrowIfNull(articleText);

        ArticleText = articleText;
        Skip = skip;
    }

    /// <summary>
    /// Gets the article text produced by the external program.
    /// </summary>
    internal string ArticleText { get; }

    /// <summary>
    /// Gets a value indicating whether the article should be skipped.
    /// </summary>
    internal bool Skip { get; }
}