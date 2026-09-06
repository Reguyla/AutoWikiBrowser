namespace Twain.Core.ExternalPrograms;

/// <summary>
/// Contains the settings required to process article text with an external
/// program.
/// </summary>
public sealed class ExternalProgramOptions
{
    /// <summary>
    /// Gets or initializes the external program path.
    /// </summary>
    public required string ProgramPath { get; init; }

    /// <summary>
    /// Gets or initializes the command-line parameter template.
    /// </summary>
    public required string Parameters { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether article text is passed
    /// through an input/output file.
    /// </summary>
    public bool PassAsFile { get; init; }

    /// <summary>
    /// Gets or initializes the configured input/output file path.
    /// </summary>
    public required string OutputFile { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether unchanged articles
    /// should be skipped.
    /// </summary>
    public bool SkipUnchanged { get; init; }
}