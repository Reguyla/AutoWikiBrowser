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

    /// <summary>
    /// Appends find and replace results to an edit summary using the
    /// language-specific wording for the current wiki.
    /// </summary>
    /// <param name="editSummary">
    /// The existing edit summary.
    /// </param>
    /// <param name="replacedSummary">
    /// The summary of replacement operations.
    /// </param>
    /// <param name="removedSummary">
    /// The summary of removal operations.
    /// </param>
    /// <param name="languageCode">
    /// The current wiki language code.
    /// </param>
    /// <returns>
    /// The updated edit summary.
    /// </returns>
    public static string AppendEditSummary(
        string editSummary,
        string replacedSummary,
        string removedSummary,
        string languageCode)
    {
        if (!string.IsNullOrEmpty(replacedSummary))
        {
            if (languageCode.Equals("ar"))
                editSummary = "استبدل: " + replacedSummary.Trim();
            else if (languageCode.Equals("arz"))
                editSummary = "غير: " + replacedSummary.Trim();
            else if (languageCode.Equals("be"))
                editSummary = "перанесена: " + replacedSummary.Trim();
            else if (languageCode.Equals("el"))
                editSummary = "αντικατέστησε: " + replacedSummary.Trim();
            else if (languageCode.Equals("eo"))
                editSummary = "anstataŭigis: " + replacedSummary.Trim();
            else if (languageCode.Equals("fa"))
                editSummary = "جایگزین شد: " + replacedSummary.Trim();
            else if (languageCode.Equals("fr"))
                editSummary = "remplacement: " + replacedSummary.Trim();
            else if (languageCode.Equals("hy"))
                editSummary = "փոխարինվեց: " + replacedSummary.Trim();
            else if (languageCode.Equals("sq"))
                editSummary = "zëvendësova: " + replacedSummary.Trim();
            else if (languageCode.Equals("tr"))
                editSummary = "değiştirildi: " + replacedSummary.Trim();
            else
                editSummary += "replaced: " + replacedSummary.Trim();
        }

        if (!string.IsNullOrEmpty(removedSummary))
        {
            if (!string.IsNullOrEmpty(editSummary))
            {
                if (languageCode.Equals("ar") ||
                    languageCode.Equals("arz") ||
                    languageCode.Equals("fa"))
                {
                    editSummary += "، ";
                }
                else
                {
                    editSummary += ", ";
                }
            }

            if (languageCode.Equals("ar"))
                editSummary += "أزال: " + removedSummary.Trim();
            else if (languageCode.Equals("arz"))
                editSummary += "شال: " + removedSummary.Trim();
            else if (languageCode.Equals("be"))
                editSummary += "выдалена: " + removedSummary.Trim();
            else if (languageCode.Equals("el"))
                editSummary += "αφαίρεσε: " + removedSummary.Trim();
            else if (languageCode.Equals("eo"))
                editSummary += "forigis: " + removedSummary.Trim();
            else if (languageCode.Equals("fa"))
                editSummary += "حذف شده: " + removedSummary.Trim();
            else if (languageCode.Equals("fr"))
                editSummary += "retrait: " + removedSummary.Trim();
            else if (languageCode.Equals("hy"))
                editSummary += "ջնջվեց: " + removedSummary.Trim();
            else if (languageCode.Equals("sq"))
                editSummary += "hoqa: " + removedSummary.Trim();
            else if (languageCode.Equals("tr"))
                editSummary += "çıkartıldı:" + removedSummary.Trim();
            else
                editSummary += "removed: " + removedSummary.Trim();
        }

        return editSummary;
    }
}