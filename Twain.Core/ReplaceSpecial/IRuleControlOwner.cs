namespace Twain.Core.ReplaceSpecial;

/// <summary>
/// Receives notifications from a Replace Special rule editor.
/// </summary>
public interface IRuleControlOwner
{
    /// <summary>
    /// Updates the display name of the currently selected rule.
    /// </summary>
    /// <param name="name">
    /// The new rule name.
    /// </param>
    void NameChanged(string name);
}