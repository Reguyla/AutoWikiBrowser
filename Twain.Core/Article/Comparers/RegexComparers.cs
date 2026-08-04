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
/// Determines whether an article matches a precompiled regular expression.
/// </summary>
public sealed class RegexArticleComparer : IArticleComparer
{
    private readonly Regex _comparator;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="RegexArticleComparer"/> class.
    /// </summary>
    /// <param name="comparator">
    /// The regular expression used to evaluate article text.
    /// </param>
    public RegexArticleComparer(Regex comparator)
    {
        ArgumentNullException.ThrowIfNull(comparator);
        _comparator = comparator;
    }

    /// <summary>
    /// Determines whether the normalized article text matches the configured
    /// regular expression.
    /// </summary>
    /// <param name="article">The article to examine.</param>
    /// <returns>
    /// <c>true</c> if the regular expression matches the article text;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        string text =
            Tools.ConvertFromLocalLineEndings(article.ArticleText);

        return _comparator.IsMatch(text);
    }
}

/// <summary>
/// Determines whether an article matches a regular expression whose pattern
/// is generated dynamically by applying AWB keywords for that article.
/// </summary>
public sealed class DynamicRegexArticleComparer : IArticleComparer
{
    private static readonly TimeSpan MatchTimeout =
        TimeSpan.FromSeconds(2);

    private readonly string _comparator;
    private readonly RegexOptions _options;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="DynamicRegexArticleComparer"/> class.
    /// </summary>
    /// <param name="comparator">
    /// The regular-expression template to evaluate after applying AWB
    /// keywords.
    /// </param>
    /// <param name="options">
    /// The regular-expression options used for matching. The
    /// <see cref="RegexOptions.Compiled"/> flag is removed because the final
    /// pattern is generated separately for each article.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The keyword-expanded comparison pattern is not a valid regular
    /// expression.
    /// </exception>
    public DynamicRegexArticleComparer(
        string comparator,
        RegexOptions options)
    {
        ArgumentNullException.ThrowIfNull(comparator);

        _comparator = comparator;
        _options = options & ~RegexOptions.Compiled;

        // Validate the template at construction time so invalid expressions
        // fail before article processing begins.
        _ = new Regex(
            Tools.ApplyKeyWords("a", comparator),
            _options,
            MatchTimeout);
    }

    /// <summary>
    /// Determines whether the normalized article text matches the
    /// keyword-expanded regular expression.
    /// </summary>
    /// <param name="article">The article to examine.</param>
    /// <returns>
    /// <c>true</c> if the expanded regular expression matches the article
    /// text; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="RegexMatchTimeoutException">
    /// Matching exceeds the configured regular-expression timeout.
    /// </exception>
    public bool Matches(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        string text =
            Tools.ConvertFromLocalLineEndings(article.ArticleText);

        string pattern =
            Tools.ApplyKeyWords(article.Name, _comparator);

        return Regex.IsMatch(
            text,
            pattern,
            _options,
            MatchTimeout);
    }
}