
using System.Collections.Generic;
using System.Linq;

namespace Twain.Core.GeneralFixes;

/// <summary>
/// Stores the user's enabled general fixes.
/// </summary>
public sealed class GeneralFixPreferences
{
    /// <summary>
    /// Enabled fix identifiers.
    /// Null means the user has not configured
    /// individual general fixes yet.
    /// An empty list means all fixes are disabled.
    /// </summary>
    public List<GeneralFixId>? EnabledFixes { get; set; }

    /// <summary>
    /// Returns the effective enabled fixes,
    /// using catalog defaults when unconfigured.
    /// </summary>
    public HashSet<GeneralFixId> GetEnabledFixes()
    {
        if (EnabledFixes is null)
        {
            return GeneralFixCatalog.Fixes
                .Where(fix => fix.EnabledByDefault)
                .Select(fix => fix.Id)
                .ToHashSet();
        }

        return EnabledFixes.ToHashSet();
    }

    /// <summary>
    /// Determines whether an individual fix is enabled.
    /// </summary>
    public bool IsEnabled(GeneralFixId id)
    {
        if (EnabledFixes is null)
        {
            return GeneralFixCatalog.IsEnabledByDefault(id);
        }

        return EnabledFixes.Contains(id);
    }

    /// <summary>
    /// Replaces the enabled fixes with the
    /// supplied selection.
    /// </summary>
    public void SetEnabledFixes(
        IEnumerable<GeneralFixId> enabledFixes)
    {
        ArgumentNullException.ThrowIfNull(enabledFixes);

        EnabledFixes = enabledFixes
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Restores the catalog defaults.
    /// </summary>
    public void RestoreDefaults()
    {
        EnabledFixes = null;
    }
}
