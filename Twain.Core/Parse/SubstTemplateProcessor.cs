namespace Twain.Core.Parse;

/// <summary>
/// Applies configured template substitutions to article text.
/// </summary>
public sealed class TemplateSubstitutionProcessor
{
    private readonly Dictionary<Regex, string> _regexes = new();

    private readonly HideText _removeUnformatted =
        new(true, false, true);

    private string[] _templateList = [];

    /// <summary>
    /// Gets or sets the list of templates to substitute.
    /// </summary>
    public string[] TemplateList
    {
        get => _templateList;

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _templateList = value;
            RefreshRegexes();
        }
    }

    /// <summary>
    /// Gets or sets whether template substitution should expand templates
    /// recursively.
    /// </summary>
    public bool ExpandRecursively { get; set; }

    /// <summary>
    /// Gets or sets whether unformatted portions of article text should be
    /// ignored during substitution.
    /// </summary>
    public bool IgnoreUnformatted { get; set; }

    /// <summary>
    /// Gets or sets whether comments should be included when recursively
    /// expanding templates.
    /// </summary>
    public bool IncludeComments { get; set; }

    /// <summary>
    /// Gets the number of generated template-substitution expressions.
    /// </summary>
    public int NoOfRegexes =>
        _regexes.Count;

    /// <summary>
    /// Gets whether any template substitutions are configured.
    /// </summary>
    public bool HasSubstitutions =>
        NoOfRegexes != 0;

    /// <summary>
    /// Clears all configured template substitutions.
    /// </summary>
    public void Clear()
    {
        _templateList = [];
        _regexes.Clear();
    }

    /// <summary>
    /// Substitutes configured templates in article text.
    /// </summary>
    /// <param name="articleText">
    /// The wiki text of the article.
    /// </param>
    /// <param name="articleTitle">
    /// The title of the article.
    /// </param>
    /// <returns>
    /// The article text after configured template substitutions have been
    /// applied.
    /// </returns>
    public string SubstituteTemplates(
        string articleText,
        string articleTitle)
    {
        if (!HasSubstitutions)
            return articleText;

        if (IgnoreUnformatted)
        {
            articleText =
                _removeUnformatted.HideUnformatted(
                    articleText);
        }

        if (!ExpandRecursively)
        {
            foreach (KeyValuePair<Regex, string> pair in _regexes)
            {
                articleText =
                    pair.Key.Replace(
                        articleText,
                        pair.Value);
            }
        }
        else
        {
            articleText =
                Tools.ExpandTemplate(
                    articleText,
                    articleTitle,
                    _regexes,
                    IncludeComments);
        }

        if (IgnoreUnformatted)
        {
            articleText =
                _removeUnformatted.AddBackUnformatted(
                    articleText);
        }

        return articleText;
    }

    /// <summary>
    /// Rebuilds the regular expressions used to match configured templates.
    /// </summary>
    private void RefreshRegexes()
    {
        _regexes.Clear();

        string templateNamespace =
            Variables.NamespacesCaseInsensitive[
                Namespace.Template];

        if (templateNamespace[0] == '(')
        {
            templateNamespace =
                "(?:" +
                templateNamespace.Insert(
                    templateNamespace.IndexOf(')'),
                    "|[Mm]sg") +
                @")?\s*";
        }
        else
        {
            templateNamespace =
                @"(?:" +
                templateNamespace +
                @"|[Mm]sg:|)\s*";
        }

        foreach (string template in _templateList)
        {
            if (string.IsNullOrEmpty(template.Trim()))
                continue;

            _regexes.Add(
                new Regex(
                    @"\{\{\s*" +
                    templateNamespace +
                    Tools.FirstLetterCaseInsensitive(
                        Regex.Escape(template)) +
                    @"\s*(\|[^\}]*|)}}",
                    RegexOptions.Singleline),
                @"{{subst:" +
                template +
                "$1}}");
        }
    }
}