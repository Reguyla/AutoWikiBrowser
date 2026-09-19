using Twain.Core.API;

namespace Twain.Core.Profiles;

/// <summary>
/// Provides profile-based authentication for a wiki session.
/// </summary>
public static class ProfileLoginService
{
    /// <summary>
    /// Describes the outcome of a profile login attempt.
    /// </summary>
    public enum LoginStatus
    {
        Success,
        SessionBusy,
        UriChanged,
        LoginFailed
    }

    /// <summary>
    /// Represents the result of a profile login attempt.
    /// </summary>
    /// <param name="Status">
    /// The outcome of the login attempt.
    /// </param>
    /// <param name="Message">
    /// The message associated with the outcome.
    /// </param>
    /// <param name="Title">
    /// The suggested title for an error message.
    /// </param>
    public readonly record struct LoginResult(
        LoginStatus Status,
        string Message = "",
        string Title = "");

    /// <summary>
    /// Attempts to log in to the specified session.
    /// </summary>
    /// <param name="session">
    /// The wiki session to authenticate.
    /// </param>
    /// <param name="username">
    /// The username to use for authentication.
    /// </param>
    /// <param name="password">
    /// The password to use for authentication.
    /// </param>
    /// <param name="loginDomain">
    /// The optional login domain.
    /// </param>
    /// <returns>The result of the login attempt.</returns>
    public static LoginResult Login(
        Session session,
        string username,
        string password,
        string loginDomain)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.IsBusy)
        {
            return new LoginResult(
                LoginStatus.SessionBusy,
                "Cannot log in because the session is busy.\r\n\r\n" +
                "Please wait for the current page-saving operation to complete.",
                "Session busy");
        }

        try
        {
            session.Editor.SynchronousEditor.Login(
                username,
                password,
                loginDomain);

            return new LoginResult(LoginStatus.Success);
        }
        catch (UriChangedException ex)
        {
            return new LoginResult(
                LoginStatus.UriChanged,
                ex.Message,
                ex.Header);
        }
        catch (LoginException ex)
        {
            return new LoginResult(
                LoginStatus.LoginFailed,
                ex.Message,
                "Login failed");
        }
    }
}