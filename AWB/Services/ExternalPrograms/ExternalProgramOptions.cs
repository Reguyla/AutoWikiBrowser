using System;
using System.Collections.Generic;
using System.Text;

namespace AutoWikiBrowser.Services.ExternalPrograms;

/// <summary>
/// Contains the settings required to process article text with an external
/// program.
/// </summary>
internal sealed class ExternalProgramOptions
{
    /// <summary>
    /// Gets or initializes the external program path.
    /// </summary>
    internal required string ProgramPath { get; init; }

    /// <summary>
    /// Gets or initializes the command-line parameter template.
    /// </summary>
    internal required string Parameters { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether article text is passed
    /// through an input/output file.
    /// </summary>
    internal bool PassAsFile { get; init; }

    /// <summary>
    /// Gets or initializes the configured input/output file path.
    /// </summary>
    internal required string OutputFile { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether unchanged articles
    /// should be skipped.
    /// </summary>
    internal bool SkipUnchanged { get; init; }
}