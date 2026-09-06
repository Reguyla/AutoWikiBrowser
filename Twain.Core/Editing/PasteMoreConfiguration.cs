namespace Twain.Core.Editing;

/// <summary>
/// Stores the text entries available through the Paste More feature.
/// </summary>
public sealed class PasteMoreConfiguration
{
    private readonly List<string> _items;

    /// <summary>
    /// Initializes an empty Paste More configuration.
    /// </summary>
    public PasteMoreConfiguration()
    {
        _items = [];
    }

    /// <summary>
    /// Initializes a Paste More configuration with the supplied text entries.
    /// </summary>
    /// <param name="items">
    /// The initial Paste More text entries.
    /// </param>
    public PasteMoreConfiguration(
        IEnumerable<string?> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items =
            items
                .Select(item => item ?? string.Empty)
                .ToList();
    }

    /// <summary>
    /// Gets the configured Paste More text entries.
    /// </summary>
    public IReadOnlyList<string> Items =>
        _items;

    /// <summary>
    /// Gets the number of configured Paste More entries.
    /// </summary>
    public int Count =>
        _items.Count;

    /// <summary>
    /// Gets or sets the Paste More text at the specified position.
    /// </summary>
    /// <param name="index">
    /// The zero-based item index.
    /// </param>
    public string this[int index]
    {
        get => _items[index];

        set =>
            _items[index] =
                value ?? string.Empty;
    }

    /// <summary>
    /// Replaces the current Paste More entries with the supplied values.
    /// </summary>
    /// <param name="items">
    /// The new Paste More text entries.
    /// </param>
    public void Replace(
        IEnumerable<string?> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items.Clear();

        _items.AddRange(
            items.Select(
                item => item ?? string.Empty));
    }
}