namespace Twain.Core.Lists;

/// <summary>
/// Represents a wiki namespace that can be selected when filtering an article list.
/// </summary>
public sealed class NamespaceFilterItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamespaceFilterItem"/> class.
    /// </summary>
    /// <param name="id">
    /// The MediaWiki namespace identifier.
    /// </param>
    /// <param name="displayName">
    /// The namespace name displayed to the user.
    /// </param>
    /// <param name="isTalk">
    /// Whether the namespace is a talk namespace.
    /// </param>
    public NamespaceFilterItem(
        int id,
        string displayName,
        bool isTalk)
    {
        ArgumentNullException.ThrowIfNull(displayName);

        Id = id;
        DisplayName = displayName;
        IsTalk = isTalk;
    }

    /// <summary>
    /// Gets the MediaWiki namespace identifier.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the namespace name displayed to the user.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets whether this namespace is a talk namespace.
    /// </summary>
    public bool IsTalk { get; }

    /// <summary>
    /// Returns the namespace display name.
    /// </summary>
    /// <returns>
    /// The namespace name displayed to the user.
    /// </returns>
    public override string ToString()
    {
        return DisplayName;
    }
}