namespace Twain.UI.Profiles;

/// <summary>
/// Represents a profile displayed in the profiles window.
/// </summary>
public sealed class ProfileListItem
{
    public int ID { get; init; }

    public string Username { get; init; } = string.Empty;

    public string PasswordSaved { get; init; } = string.Empty;

    public string DefaultSettings { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;
}