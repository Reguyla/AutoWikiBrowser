using Twain.Core.Lists.Providers;

namespace Twain.Core.Lists;

/// <summary>
/// Describes a request to generate an article list using a list provider.
/// </summary>
public sealed class ListGenerationRequest
{
    /// <summary>
    /// Initializes a list-generation request.
    /// </summary>
    /// <param name="provider">
    /// The provider used to generate the article list.
    /// </param>
    /// <param name="sourceValues">
    /// The prepared source values supplied to the provider.
    /// </param>
    public ListGenerationRequest(
        IListProvider provider,
        string[] sourceValues)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(sourceValues);

        Provider = provider;
        SourceValues = sourceValues;
    }

    /// <summary>
    /// Gets the provider used to generate the article list.
    /// </summary>
    public IListProvider Provider { get; }

    /// <summary>
    /// Gets the prepared source values supplied to the provider.
    /// </summary>
    public string[] SourceValues { get; }
}