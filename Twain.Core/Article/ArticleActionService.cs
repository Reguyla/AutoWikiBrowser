namespace Twain.Core;

/// <summary>
/// Provides article actions that can be performed independently of the
/// application user interface.
/// </summary>
public static class ArticleActionService
{
    /// <summary>
    /// Attempts to move the specified article and creates a message describing
    /// the result.
    /// </summary>
    /// <param name="article">
    /// The article to move.
    /// </param>
    /// <param name="session">
    /// The session used to perform the move.
    /// </param>
    /// <param name="newTitle">
    /// The destination title returned by the move operation.
    /// </param>
    /// <param name="message">
    /// The message describing the move result.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the move succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool TryMove(
        Article article,
        Session session,
        out string newTitle,
        out string message)
    {
        bool succeeded =
            article.Move(
                session,
                out newTitle);

        if (succeeded)
        {
            message =
                "Moved " +
                article.Name +
                " to " +
                newTitle;
        }
        else
        {
            message =
                "Move of " +
                article.Name +
                " failed!";
        }

        return succeeded;
    }

    /// <summary>
    /// Attempts to delete the specified article and creates a message describing
    /// the result.
    /// </summary>
    /// <param name="article">
    /// The article to delete.
    /// </param>
    /// <param name="session">
    /// The session used to perform the deletion.
    /// </param>
    /// <param name="message">
    /// The message describing the deletion result.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when deletion succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool TryDelete(
        Article article,
        Session session,
        out string message)
    {
        bool succeeded =
            article.Delete(session);

        if (succeeded)
        {
            message =
                "Deleted " +
                article.Name;
        }
        else
        {
            message =
                "Deletion of " +
                article.Name +
                " failed!";
        }

        return succeeded;
    }

    /// <summary>
    /// Attempts to protect the specified article and creates a message describing
    /// the result.
    /// </summary>
    /// <param name="article">
    /// The article to protect.
    /// </param>
    /// <param name="session">
    /// The session used to perform the protection.
    /// </param>
    /// <param name="message">
    /// The message describing the protection result.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when protection succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool TryProtect(
        Article article,
        Session session,
        out string message)
    {
        bool succeeded =
            article.Protect(session);

        if (succeeded)
        {
            message =
                "Protected " +
                article.Name;
        }
        else
        {
            message =
                "Protection of " +
                article.Name +
                " failed!";
        }

        return succeeded;
    }
}