namespace Twain.Core.Exceptions;

/// <summary>
/// Provides helpers for inspecting and classifying exception chains.
/// </summary>
public static class ExceptionClassifier
{
    /// <summary>
    /// Determines whether an exception represents a retryable network failure.
    /// </summary>
    /// <param name="exception">
    /// The exception to classify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the exception chain contains a retryable
    /// network failure; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsRetryableNetworkException(
        Exception exception)
    {
        for (Exception? current = exception;
             current != null;
             current = current.InnerException)
        {
            if (current is WebException or HttpRequestException)
            {
                return true;
            }

            if (current is IOException &&
                current.Message.Contains(
                    "0x2746",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Searches an exception and its inner-exception chain for the first
    /// exception of the specified type.
    /// </summary>
    /// <typeparam name="TException">
    /// The exception type to locate.
    /// </typeparam>
    /// <param name="exception">
    /// The exception at the beginning of the chain to inspect.
    /// </param>
    /// <returns>
    /// The first matching exception in the chain, or
    /// <see langword="null"/> when no matching exception is found.
    /// </returns>
    public static TException? FindException<TException>(
        Exception exception)
        where TException : Exception
    {
        for (Exception? current = exception;
             current != null;
             current = current.InnerException)
        {
            if (current is TException matchingException)
            {
                return matchingException;
            }
        }

        return null;
    }
}