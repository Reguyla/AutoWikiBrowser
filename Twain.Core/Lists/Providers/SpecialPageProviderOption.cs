namespace Twain.Core.Lists.Providers;

/// <summary>
/// Describes a special-page provider for presentation by a user interface.
/// </summary>
public sealed record SpecialPageProviderOption(
    int Index,
    string DisplayText,
    bool PagesEnabled,
    bool NamespacesEnabled)
{
    /// <summary>
    /// Returns the display text used by UI controls.
    /// </summary>
    public override string ToString()
    {
        return DisplayText;
    }
}