/*
Copyright (C) 2009

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

namespace WikiFunctions;

/// <summary>
/// Determines whether an article contains an exact, case-sensitive
/// text value.
/// </summary>
public sealed class CaseSensitiveArticleComparer : IArticleComparer
{
    private readonly string _comparator;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CaseSensitiveArticleComparer"/> class.
    /// </summary>
    /// <param name="comparator">
    /// The text to locate in the article content.
    /// </param>
    public CaseSensitiveArticleComparer(string comparator)
    {
        ArgumentNullException.ThrowIfNull(comparator);
        _comparator = comparator;
    }

    /// <summary>
    /// Determines whether the article text contains the configured
    /// comparison value using case-sensitive matching.
    /// </summary>
    /// <param name="article">The article to examine.</param>
    /// <returns>
    /// <c>true</c> if the article contains the comparison value;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        string text =
            Tools.ConvertFromLocalLineEndings(article.ArticleText);

        return text.Contains(_comparator, StringComparison.Ordinal);
    }
}

/// <summary>
/// Determines whether an article contains a text value without
/// regard to letter casing.
/// </summary>
public sealed class CaseInsensitiveArticleComparer : IArticleComparer
{
    private readonly string _comparator;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CaseInsensitiveArticleComparer"/> class.
    /// </summary>
    /// <param name="comparator">
    /// The text to locate in the article content.
    /// </param>
    public CaseInsensitiveArticleComparer(string comparator)
    {
        ArgumentNullException.ThrowIfNull(comparator);
        _comparator = comparator;
    }

    /// <summary>
    /// Determines whether the article text contains the configured
    /// comparison value using case-insensitive matching.
    /// </summary>
    /// <param name="article">The article to examine.</param>
    /// <returns>
    /// <c>true</c> if the article contains the comparison value;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        string text =
            Tools.ConvertFromLocalLineEndings(article.ArticleText);

        return text.Contains(
            _comparator,
            StringComparison.CurrentCultureIgnoreCase);
    }
}

/// <summary>
/// Determines whether an article contains a case-sensitive comparison
/// value after AWB keywords have been expanded for that article.
/// </summary>
public sealed class CaseSensitiveArticleComparerWithKeywords
    : IArticleComparer
{
    private readonly string _comparator;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CaseSensitiveArticleComparerWithKeywords"/> class.
    /// </summary>
    /// <param name="comparator">
    /// The text template to evaluate after applying AWB keywords.
    /// </param>
    public CaseSensitiveArticleComparerWithKeywords(string comparator)
    {
        ArgumentNullException.ThrowIfNull(comparator);
        _comparator = comparator;
    }

    /// <summary>
    /// Determines whether the article text contains the keyword-expanded
    /// comparison value using case-sensitive matching.
    /// </summary>
    /// <param name="article">The article to examine.</param>
    /// <returns>
    /// <c>true</c> if the article contains the expanded comparison value;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        string text =
            Tools.ConvertFromLocalLineEndings(article.ArticleText);

        string comparator =
            Tools.ApplyKeyWords(article.Name, _comparator);

        return text.Contains(
            comparator,
            StringComparison.Ordinal);
    }
}

/// <summary>
/// Determines whether an article contains a case-insensitive comparison
/// value after AWB keywords have been expanded for that article.
/// </summary>
public sealed class CaseInsensitiveArticleComparerWithKeywords
    : IArticleComparer
{
    private readonly string _comparator;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CaseInsensitiveArticleComparerWithKeywords"/> class.
    /// </summary>
    /// <param name="comparator">
    /// The text template to evaluate after applying AWB keywords.
    /// </param>
    public CaseInsensitiveArticleComparerWithKeywords(string comparator)
    {
        ArgumentNullException.ThrowIfNull(comparator);
        _comparator = comparator;
    }

    /// <summary>
    /// Determines whether the article text contains the keyword-expanded
    /// comparison value using case-insensitive matching.
    /// </summary>
    /// <param name="article">The article to examine.</param>
    /// <returns>
    /// <c>true</c> if the article contains the expanded comparison value;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        string text =
            Tools.ConvertFromLocalLineEndings(article.ArticleText);

        string comparator =
            Tools.ApplyKeyWords(article.Name, _comparator);

        return text.Contains(
            comparator,
            StringComparison.CurrentCultureIgnoreCase);
    }
}