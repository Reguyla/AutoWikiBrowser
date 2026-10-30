using System;
using System.Collections.Generic;

namespace Twain.Core;

/// <summary>
/// Collects edit-summary contributions generated during article processing.
/// </summary>
/// <remarks>
/// Contributions are stored independently of processing order.
/// Formatting, ordering, and length management are handled separately.
/// </remarks>
public sealed class EditSummaryCollector
{
    private readonly List<EditSummaryContribution> _contributions = new();

    /// <summary>
    /// Gets the collected contributions.
    /// </summary>
    public IReadOnlyList<EditSummaryContribution> Contributions =>
        _contributions.AsReadOnly();

    /// <summary>
    /// Gets whether any contributions have been collected.
    /// </summary>
    public bool HasContributions => _contributions.Count > 0;

    /// <summary>
    /// Adds an edit-summary contribution.
    /// </summary>
    /// <param name="contribution">
    /// The contribution to add.
    /// </param>
    /// <returns>
    /// True if the contribution was added; otherwise false.
    /// </returns>
    public bool Add(EditSummaryContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        if (string.IsNullOrWhiteSpace(contribution.Description))
        {
            return false;
        }

        // Prevent duplicate descriptions within the same category.
        foreach (EditSummaryContribution existing in _contributions)
        {
            if (existing.Category == contribution.Category &&
                string.Equals(
                    existing.Description,
                    contribution.Description,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        _contributions.Add(contribution);

        return true;
    }

    /// <summary>
    /// Removes all collected contributions.
    /// </summary>
    public void Clear()
    {
        _contributions.Clear();
    }
}