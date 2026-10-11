namespace Twain.Core.CustomModules;

/// <summary>
/// Describes the compilation and loading status of a custom module.
/// </summary>
public enum CustomModuleCompilationStatus
{
    /// <summary>
    /// No compilation has been completed.
    /// </summary>
    NotCompiled,

    /// <summary>
    /// The module compiled and loaded without warnings.
    /// </summary>
    Success,

    /// <summary>
    /// The module compiled and loaded with warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// Compilation or module loading failed.
    /// </summary>
    Failed
}