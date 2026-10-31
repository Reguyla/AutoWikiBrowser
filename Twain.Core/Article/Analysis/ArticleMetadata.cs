namespace Twain.Core.ArticleAnalysis;

/// <summary>
/// Represents a recognized metadata declaration in the original article text.
/// </summary>
public sealed class ArticleMetadata
{
    public ArticleMetadata(
        ArticleMetadataType type,
        string text,
        int startIndex,
        int length)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        Type = type;
        Text = text;
        StartIndex = startIndex;
        Length = length;
    }

    /// <summary>
    /// Gets the type of metadata declaration.
    /// </summary>
    public ArticleMetadataType Type { get; }

    /// <summary>
    /// Gets the original metadata declaration.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets its starting position in the original article.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// Gets its length.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets its exclusive ending position.
    /// </summary>
    public int EndIndex => StartIndex + Length;
}