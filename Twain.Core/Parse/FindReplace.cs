namespace Twain.Core.Parse;

/// <summary>
/// Provides framework-independent find and replace processing and
/// configuration support.
/// </summary>
public static class FindReplace
{

    private static readonly Regex NewlineRegex = new Regex(@"(?<!\\)\\n", RegexOptions.Compiled),
    TabulationRegex = new Regex(@"(?<!\\)\\t", RegexOptions.Compiled);


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

    /// <summary>
    /// Returns the separator used between find and replace summary entries
    /// for the specified wiki language.
    /// </summary>
    /// <param name="languageCode">
    /// The current wiki language code.
    /// </param>
    /// <returns>
    /// The language-appropriate summary separator.
    /// </returns>
    public static string GetSummarySeparator(
        string languageCode)
    {
        if (languageCode.Equals("ar") ||
            languageCode.Equals("arz") ||
            languageCode.Equals("fa"))
        {
            return "، ";
        }

        return ", ";
    }

    /// <summary>
    /// Contains the result of executing a single find and replace rule.
    /// </summary>
    public sealed record ReplacementResult(
        string Text,
        int ReplacementCount,
        int RemovalCount,
        string FirstReplacementOriginal,
        string FirstReplacementResult,
        string FirstRemoval)
    {
        /// <summary>
        /// Gets whether the replacement changed the article text.
        /// </summary>
        public bool ChangeMade =>
            ReplacementCount + RemovalCount > 0;
    }

    /// <summary>
    /// Contains the updated find and replace summary values.
    /// </summary>
    public sealed record SummaryResult(
        string ReplacedSummary,
        string RemovedSummary);

    /// <summary>
    /// Updates the accumulated find and replace summaries from the result
    /// of executing a replacement rule.
    /// </summary>
    /// <param name="result">
    /// The result of the replacement operation.
    /// </param>
    /// <param name="replacedSummary">
    /// The existing replacement summary.
    /// </param>
    /// <param name="removedSummary">
    /// The existing removal summary.
    /// </param>
    /// <param name="summarySeparator">
    /// The separator to place between summary entries.
    /// </param>
    /// <param name="arrow">
    /// The separator displayed between the original and replacement text.
    /// </param>
    /// <returns>
    /// The updated replacement and removal summaries.
    /// </returns>
    public static SummaryResult UpdateSummaries(
        ReplacementResult result,
        string replacedSummary,
        string removedSummary,
        string summarySeparator,
        string arrow)
    {
        if (result.ReplacementCount > 0)
        {
            if (!string.IsNullOrEmpty(replacedSummary))
            {
                replacedSummary +=
                    summarySeparator;
            }

            replacedSummary +=
                result.FirstReplacementOriginal +
                arrow +
                result.FirstReplacementResult;

            if (result.ReplacementCount > 1)
            {
                replacedSummary +=
                    " (" +
                    result.ReplacementCount +
                    ")";
            }
        }

        if (result.RemovalCount > 0)
        {
            if (!string.IsNullOrEmpty(removedSummary))
            {
                removedSummary +=
                    summarySeparator;
            }

            removedSummary +=
                result.FirstRemoval;

            if (result.RemovalCount > 1)
            {
                removedSummary +=
                    " (" +
                    result.RemovalCount +
                    ")";
            }
        }

        return new SummaryResult(
            replacedSummary,
            removedSummary);
    }

    /// <summary>
    /// Executes a prepared regular expression replacement and records the
    /// changes needed to generate an edit summary.
    /// </summary>
    /// <param name="articleText">
    /// The article text to process.
    /// </param>
    /// <param name="findThis">
    /// The prepared regular expression pattern.
    /// </param>
    /// <param name="replaceWith">
    /// The prepared replacement expression.
    /// </param>
    /// <param name="options">
    /// The regular expression options to use.
    /// </param>
    /// <returns>
    /// The processed text and information about replacements and removals.
    /// </returns>
    public static ReplacementResult ExecuteReplacement(
        string articleText,
        string findThis,
        string replaceWith,
        RegexOptions options)
    {
        // T350636 1-minute timeout to guard against regex backtracking
        Regex findRegex =
            new(
                findThis,
                options,
                TimeSpan.FromSeconds(60));

        int replacementCount = 0;
        int removalCount = 0;
        string firstReplacementOriginal = string.Empty;
        string firstReplacementResult = string.Empty;
        string firstRemoval = string.Empty;

        string result =
            findRegex.Replace(
                articleText,
                match =>
                {
                    string replacementResult =
                        match.Result(
                            replaceWith);

                    if (match.Value.Equals(replacementResult))
                    {
                        return replacementResult;
                    }

                    if (!string.IsNullOrEmpty(replacementResult))
                    {
                        if (replacementCount == 0)
                        {
                            firstReplacementOriginal =
                                match.Value;

                            firstReplacementResult =
                                replacementResult;
                        }

                        replacementCount++;
                    }
                    else
                    {
                        if (removalCount == 0)
                        {
                            firstRemoval =
                                match.Value;
                        }

                        removalCount++;
                    }

                    return replacementResult;
                });

        return new ReplacementResult(
            result,
            replacementCount,
            removalCount,
            firstReplacementOriginal,
            firstReplacementResult,
            firstRemoval);
    }

    /// <summary>
    /// Converts escaped newline and tab sequences in replacement text
    /// to their corresponding characters.
    /// </summary>
    /// <param name="replace">
    /// The replacement text to prepare.
    /// </param>
    /// <returns>
    /// The prepared replacement text.
    /// </returns>
    public static string PrepareReplacePart(
        string replace)
    {
        replace =
            NewlineRegex.Replace(
                replace,
                "\n");

        return TabulationRegex.Replace(
            replace,
            "\t");
    }

    /// <summary>
    /// Contains the prepared find and replacement expressions for a rule.
    /// </summary>
    public sealed record PreparedReplacement(
        string Find,
        string Replace);

    /// <summary>
    /// Prepares the find and replacement expressions for a replacement rule,
    /// including article-specific keyword expansion.
    /// </summary>
    /// <param name="replacement">
    /// The replacement rule to prepare.
    /// </param>
    /// <param name="articleTitle">
    /// The title of the article being processed.
    /// </param>
    /// <returns>
    /// The prepared find and replacement expressions.
    /// </returns>
    public static PreparedReplacement PrepareReplacement(
        Replacement replacement,
        string articleTitle)
    {
        ArgumentNullException.ThrowIfNull(replacement);

        string find =
            Tools.ApplyKeyWords(
                articleTitle,
                replacement.Find,
                true);

        string replace =
            Tools.ApplyKeyWords(
                articleTitle,
                PrepareReplacePart(
                    replacement.Replace));

        return new PreparedReplacement(
            find,
            replace);
    }

    /// <summary>
    /// Creates the regular expression options for a replacement entry.
    /// </summary>
    public static RegexOptions CreateRegexOptions(
        bool caseSensitive,
        bool multiline,
        bool singleline)
    {
        RegexOptions options =
            RegexOptions.None;

        if (!caseSensitive)
        {
            options |=
                RegexOptions.IgnoreCase;
        }

        if (multiline)
        {
            options |=
                RegexOptions.Multiline;
        }

        if (singleline)
        {
            options |=
                RegexOptions.Singleline;
        }

        return options;
    }

    /// <summary>
    /// Prepares encoded find text for use by a replacement entry.
    /// </summary>
    public static string PrepareFindText(
        string find,
        bool isRegex)
    {
        string preparedFind =
            find;

        // In F&R newline matching is on \n, so if not a regex ensure this isn't escaped.
        if (!isRegex)
        {
            bool newlines =
                preparedFind.Contains("\\n");

            preparedFind =
                Regex.Escape(preparedFind);

            if (newlines)
            {
                preparedFind =
                    preparedFind.Replace(
                        @"\\n",
                        "\n");
            }
        }

        return preparedFind;
    }

    /// <summary>
    /// Creates a replacement entry from editable replacement values.
    /// </summary>
    public static Replacement CreateReplacement(
        string find,
        string replace,
        bool enabled,
        bool minor,
        bool isRegex,
        bool beforeOrAfter,
        bool caseSensitive,
        bool multiline,
        bool singleline,
        string comment)
    {
        return new Replacement
        {
            Enabled = enabled,
            Minor = minor,
            IsRegex = isRegex,
            BeforeOrAfter = beforeOrAfter,
            Find = PrepareFindText(
                find,
                isRegex),
            Replace = replace,
            RegularExpressionOptions = CreateRegexOptions(
                caseSensitive,
                multiline,
                singleline),
            Comment = comment
        };
    }

    /// <summary>
    /// Encodes replacement editor text by converting escaped CRLF sequences
    /// to actual CRLF characters.
    /// </summary>
    public static string Encode(
        string text)
    {
        return text.Replace(
            "\\r\\n",
            "\r\n");
    }

    /// <summary>
    /// Decodes replacement text for display by converting LF characters
    /// to escaped CRLF sequences.
    /// </summary>
    public static string Decode(
        string text)
    {
        return text.Replace(
            "\n",
            "\\r\\n");
    }

    /// <summary>
    /// Prepares replacement find text for display in an editor.
    /// </summary>
    public static string PrepareFindTextForEditor(
        Replacement replacement,
        bool decodeRequired)
    {
        if (decodeRequired)
        {
            string find =
                Decode(
                    replacement.Find);

            return replacement.IsRegex
                ? find
                : Regex.Unescape(find);
        }

        return replacement.IsRegex
            ? replacement.Find
            : Regex.Unescape(
                replacement.Find);
    }

    /// <summary>
    /// Prepares replacement text for display in an editor.
    /// </summary>
    public static string PrepareReplaceTextForEditor(
        Replacement replacement,
        bool decodeRequired)
    {
        return decodeRequired
            ? Decode(
                replacement.Replace)
            : replacement.Replace;
    }

    /// <summary>
    /// Creates editable row values from a replacement entry.
    /// </summary>
    public static EditorRow CreateEditorRow(
        Replacement replacement,
        bool decodeRequired)
    {
        ArgumentNullException.ThrowIfNull(
            replacement);

        return new EditorRow(
            PrepareFindTextForEditor(
                replacement,
                decodeRequired),
            PrepareReplaceTextForEditor(
                replacement,
                decodeRequired),
            IsCaseSensitive(
                replacement),
            replacement.IsRegex,
            IsMultiline(
                replacement),
            IsSingleline(
                replacement),
            replacement.Minor,
            replacement.BeforeOrAfter,
            replacement.Enabled,
            replacement.Comment);
    }

    /// <summary>
    /// Creates a replacement entry from editable row values.
    /// </summary>
    /// <param name="row">
    /// The editable replacement values to convert.
    /// </param>
    /// <returns>
    /// The configured replacement entry.
    /// </returns>
    public static Replacement CreateReplacement(
        EditorRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return CreateReplacement(
            Encode(
                row.Find),
            Encode(
                row.Replace),
            row.Enabled,
            row.Minor,
            row.IsRegex,
            row.BeforeOrAfter,
            row.CaseSensitive,
            row.Multiline,
            row.Singleline,
            row.Comment);
    }

    /// <summary>
    /// Returns the replacement-summary arrow for the specified writing direction.
    /// </summary>
    /// <param name="rightToLeft">
    /// Whether the wiki uses right-to-left text.
    /// </param>
    /// <returns>
    /// A left-pointing arrow for right-to-left text; otherwise, a
    /// right-pointing arrow.
    /// </returns>
    public static string GetSummaryArrow(
        bool rightToLeft)
    {
        return rightToLeft
            ? " ← "
            : " → ";
    }

    /// <summary>
    /// Contains article text prepared for find and replace processing together
    /// with the state required to restore hidden content afterward.
    /// </summary>
    public sealed record PreparedArticleText(
        string Text,
        HideText HiddenText);

    /// <summary>
    /// Prepares article text for find and replace processing by hiding content
    /// that should be excluded from replacements.
    /// </summary>
    /// <param name="articleText">
    /// The article text to prepare.
    /// </param>
    /// <param name="ignoreLinks">
    /// Whether external links and images should be hidden.
    /// </param>
    /// <param name="ignoreMore">
    /// Whether headings, internal link targets, templates, refs, and other
    /// protected content should be hidden.
    /// </param>
    /// <returns>
    /// The prepared article text and the state required to restore hidden content.
    /// </returns>
    public static PreparedArticleText PrepareArticleText(
        string articleText,
        bool ignoreLinks,
        bool ignoreMore)
    {
        HideText hiddenText =
            new(
                true,
                false,
                true);

        if (ignoreMore)
        {
            articleText =
                hiddenText.HideMore(
                    articleText);
        }
        else if (ignoreLinks)
        {
            articleText =
                hiddenText.Hide(
                    articleText);
        }

        return new PreparedArticleText(
            articleText,
            hiddenText);
    }

    /// <summary>
    /// Restores content hidden while preparing article text for find and replace
    /// processing.
    /// </summary>
    /// <param name="articleText">
    /// The processed article text.
    /// </param>
    /// <param name="prepared">
    /// The preparation state returned by <see cref="PrepareArticleText"/>.
    /// </param>
    /// <param name="ignoreLinks">
    /// Whether external links and images were hidden.
    /// </param>
    /// <param name="ignoreMore">
    /// Whether the extended set of protected content was hidden.
    /// </param>
    /// <returns>
    /// The article text with hidden content restored.
    /// </returns>
    public static string RestoreArticleText(
        string articleText,
        PreparedArticleText prepared,
        bool ignoreLinks,
        bool ignoreMore)
    {
        ArgumentNullException.ThrowIfNull(prepared);

        if (ignoreMore)
        {
            // FIXME: Usages of IgnoreMore with number (or M) replacement done in
            // FindAndReplace can cause corruption of HideText placeholders.
            return prepared.HiddenText.AddBackMore(
                articleText);
        }

        if (ignoreLinks)
        {
            return prepared.HiddenText.AddBack(
                articleText);
        }

        return articleText;
    }
}