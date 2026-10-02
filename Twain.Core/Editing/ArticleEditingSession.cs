namespace Twain.Core.Editing;

/// <summary>
/// Represents the original and current text of an article editing session.
/// </summary>
public sealed class ArticleEditingSession
{
    /// <summary>
    /// Initializes an article editing session.
    /// </summary>
    /// <param name="originalText">
    /// The article text loaded at the start of the session.
    /// </param>
    public ArticleEditingSession(string originalText)
    {
        OriginalText = originalText;
        CurrentText = originalText;
    }

    /// <summary>
    /// Gets the article text originally loaded into the session.
    /// </summary>
    public string OriginalText { get; private set; }

    /// <summary>
    /// Starts a new editing session using the supplied article text.
    /// </summary>
    /// <param name="articleText">
    /// The article text loaded from the wiki.
    /// </param>
    public void LoadArticle(
        string articleText)
    {
        ArgumentNullException.ThrowIfNull(articleText);

        OriginalText = articleText;
        CurrentText = articleText;
    }

    /// <summary>
    /// Gets or sets the article text currently being edited.
    /// </summary>
    public string CurrentText { get; set; }

}