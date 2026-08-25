namespace Twain.Core.Navigation;

/// <summary>
/// Provides validation for URLs that may be opened by an external browser.
/// </summary>
public static class ExternalUrlValidator
{
    /// <summary>
    /// Validates a URL before it is opened outside the application.
    /// </summary>
    /// <param name="url">
    /// The candidate URL.
    /// </param>
    /// <param name="allowedUrl">
    /// Contains the normalized URL when validation succeeds.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the URL is an absolute HTTP or HTTPS address;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGetAllowedExternalUrl(
        string url,
        out string allowedUrl)
    {
        allowedUrl = null;

        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out Uri uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        allowedUrl = uri.AbsoluteUri;
        return true;
    }
}