using System.Collections;

namespace Twain.Core.Lists;

/// <summary>
/// Maintains the articles contained in a generated or manually managed page list.
/// </summary>
public sealed class ArticleList : IEnumerable<Article>
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

    /// <summary>
    /// Gets the article at the specified position.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the article to retrieve.
    /// </param>
    public Article this[int index] => _articles[index];

    /// <summary>
    /// Determines whether the specified article is contained in the list.
    /// </summary>
    /// <param name="article">
    /// The article to locate.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the article is contained in the list;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        return _articles.Contains(article);
    }

    /// <summary>
    /// Returns the zero-based index of the specified article.
    /// </summary>
    /// <param name="article">
    /// The article to locate.
    /// </param>
    /// <returns>
    /// The zero-based index of the article when found; otherwise, -1.
    /// </returns>
    public int IndexOf(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        return _articles.IndexOf(article);
    }

    /// <summary>
    /// Copies the articles to the specified array, starting at the supplied index.
    /// </summary>
    /// <param name="array">
    /// The destination array.
    /// </param>
    /// <param name="arrayIndex">
    /// The zero-based index in the destination array at which copying begins.
    /// </param>
    public void CopyTo(Article[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        _articles.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the articles.
    /// </summary>
    public IEnumerator<Article> GetEnumerator()
    {
        return _articles.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the articles.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Removes articles that are not in the main namespace.
    /// </summary>
    /// <returns>
    /// The articles removed from the list.
    /// </returns>
    public List<Article> RemoveNonMainArticles()
    {
        List<Article> removed =
            _articles.FindAll(
                article =>
                    article.NameSpaceKey != Namespace.Article);

        if (removed.Count > 0)
        {
            _articles.RemoveAll(
                article =>
                    article.NameSpaceKey != Namespace.Article);
        }

        return removed;
    }

    /// <summary>
    /// Removes duplicate articles while preserving the first occurrence
    /// of each article.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when one or more duplicate articles were removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool RemoveDuplicates()
    {
        List<Article> distinct =
            _articles.Distinct().ToList();

        if (distinct.Count == _articles.Count)
            return false;

        _articles.Clear();
        _articles.AddRange(distinct);

        return true;
    }
}