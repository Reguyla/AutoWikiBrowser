namespace Twain.Core.Lists;

/// <summary>
/// Maintains the articles contained in a generated or manually managed page list.
/// </summary>
public sealed class ArticleList
{
    private readonly List<Article> _articles = new();

    /// <summary>
    /// Gets the number of articles in the list.
    /// </summary>
    public int Count => _articles.Count;

    /// <summary>
    /// Adds an article to the list.
    /// </summary>
    /// <param name="article">
    /// The article to add.
    /// </param>
    public void Add(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        _articles.Add(article);
    }

    /// <summary>
    /// Adds the supplied articles to the list.
    /// </summary>
    /// <param name="articles">
    /// The articles to add.
    /// </param>
    public void AddRange(IEnumerable<Article> articles)
    {
        ArgumentNullException.ThrowIfNull(articles);

        _articles.AddRange(articles);
    }

    /// <summary>
    /// Removes all articles from the list.
    /// </summary>
    public void Clear()
    {
        _articles.Clear();
    }

    /// <summary>
    /// Returns a copy of the current article list.
    /// </summary>
    public List<Article> ToList()
    {
        return new List<Article>(_articles);
    }

    /// <summary>
    /// Inserts an article at the specified position.
    /// </summary>
    /// <param name="index">
    /// The zero-based index at which the article should be inserted.
    /// </param>
    /// <param name="article">
    /// The article to insert.
    /// </param>
    public void Insert(int index, Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        _articles.Insert(index, article);
    }

    /// <summary>
    /// Removes the specified article from the list.
    /// </summary>
    /// <param name="article">
    /// The article to remove.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the article was removed; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Remove(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        return _articles.Remove(article);
    }

    /// <summary>
    /// Removes the article at the specified position.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the article to remove.
    /// </param>
    public void RemoveAt(int index)
    {
        _articles.RemoveAt(index);
    }

    /// <summary>
    /// Replaces the article at the specified position.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the article to replace.
    /// </param>
    /// <param name="article">
    /// The replacement article.
    /// </param>
    public void Set(int index, Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        _articles[index] = article;
    }

    /// <summary>
    /// Removes all articles that match the supplied predicate.
    /// </summary>
    /// <param name="match">
    /// The predicate used to determine which articles should be removed.
    /// </param>
    /// <returns>
    /// The number of articles removed.
    /// </returns>
    public int RemoveAll(Predicate<Article> match)
    {
        ArgumentNullException.ThrowIfNull(match);

        return _articles.RemoveAll(match);
    }

    /// <summary>
    /// Replaces the current article collection with the supplied articles.
    /// </summary>
    /// <param name="articles">
    /// The articles that should replace the current collection.
    /// </param>
    public void ReplaceWith(IEnumerable<Article> articles)
    {
        ArgumentNullException.ThrowIfNull(articles);

        _articles.Clear();
        _articles.AddRange(articles);
    }
}