using Twain.Core.AWBSettings;

namespace Twain.Core.ExternalPrograms;

/// <summary>
/// Provides shared operations for external program configuration.
/// </summary>
public static class ExternalProgramConfiguration
{
    /// <summary>
    /// Determines whether the supplied external program settings contain
    /// all values required for execution.
    /// </summary>
    /// <param name="settings">
    /// The external program settings to validate.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the configuration is complete; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool IsValid(
        ExternalProgramPrefs settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Enabled)
            return true;

        if (string.IsNullOrWhiteSpace(settings.Program))
            return false;

        if (settings.PassAsFile)
        {
            return !string.IsNullOrWhiteSpace(
                settings.OutputFile);
        }

        return !string.IsNullOrWhiteSpace(
            settings.Parameters);
    }

    /// <summary>
    /// Creates an external program execution snapshot from persisted
    /// external program settings.
    /// </summary>
    /// <param name="settings">
    /// The external program settings to convert.
    /// </param>
    /// <returns>
    /// The execution options represented by the supplied settings.
    /// </returns>
    public static ExternalProgramOptions CreateOptions(
        ExternalProgramPrefs settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new ExternalProgramOptions
        {
            ProgramPath = settings.Program,
            Parameters = settings.Parameters,
            PassAsFile = settings.PassAsFile,
            OutputFile = settings.OutputFile,
            SkipUnchanged = settings.Skip
        };
    }
}