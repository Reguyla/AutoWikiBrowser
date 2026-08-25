using Twain.Core.Parse;
using Twain.Core.Plugin;

namespace Twain.Core.Processing;

/// <summary>
/// Contains the configured processing dependencies used while an article
/// passes through the main processing pipeline.
/// </summary>
public sealed class MainProcessDependencies
{
    /// <summary>
    /// Gets or initializes the skip options used during article processing.
    /// </summary>
    public required ISkipOptions Skip { get; init; }

    /// <summary>
    /// Gets or initializes the text-hiding helper used during article
    /// processing.
    /// </summary>
    public required HideText RemoveText { get; init; }

    /// <summary>
    /// Gets or initializes the collection of article titles excluded from
    /// standard processing.
    /// </summary>
    public required ICollection<string> NoParse { get; init; }

    /// <summary>
    /// Gets or initializes the configured find-and-replace processor.
    /// </summary>
    public required FindandReplace FindAndReplace { get; init; }

    /// <summary>
    /// Gets or initializes the configured template-substitution processor.
    /// </summary>
    public required SubstTemplates SubstTemplates { get; init; }

    /// <summary>
    /// Gets or initializes the configured advanced replacement processor.
    /// </summary>
    public required ReplaceSpecial.ReplaceSpecial ReplaceSpecial { get; init; }

    /// <summary>
    /// Gets or initializes the configured user-talk template expression.
    /// </summary>
    public Regex UserTalkTemplatesRegex { get; init; }
}