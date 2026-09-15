namespace Twain.Core.Parse;

/// <summary>
/// Provides framework-independent find and replace processing and
/// configuration support.
/// </summary>
public static class FindReplace
{
    /// <summary>
    /// Represents the editable values for a find and replace entry independently
    /// of the user interface used to edit them.
    /// </summary>
    public sealed record EditorRow(
        string Find,
        string Replace,
        bool CaseSensitive,
        bool IsRegex,
        bool Multiline,
        bool Singleline,
        bool Minor,
        bool BeforeOrAfter,
        bool Enabled,
        string Comment);

    /// <summary>
    /// Determines whether a replacement uses case-sensitive matching.
    /// </summary>
    public static bool IsCaseSensitive(
    Replacement replacement)
    {
        return (replacement.RegularExpressionOptions & RegexOptions.IgnoreCase) !=
            RegexOptions.IgnoreCase;
    }

    /// <summary>
    /// Determines whether a replacement uses multiline regular expression behavior.
    /// </summary>
    public static bool IsMultiline(
        Replacement replacement)
    {
        return (replacement.RegularExpressionOptions & RegexOptions.Multiline) ==
            RegexOptions.Multiline;
    }

    /// <summary>
    /// Determines whether a replacement uses single-line regular expression behavior.
    /// </summary>
    public static bool IsSingleline(
        Replacement replacement)
    {
        return (replacement.RegularExpressionOptions & RegexOptions.Singleline) ==
            RegexOptions.Singleline;
    }
}