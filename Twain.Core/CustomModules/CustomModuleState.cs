using Twain.Core.Plugin;

namespace Twain.Core.CustomModules;

/// <summary>
/// Stores the configured custom-module state independently of the user
/// interface used to edit it.
/// </summary>
public sealed class CustomModuleState
{
    /// <summary>
    /// Gets or sets the custom module source code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the programming language used by the custom module.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the custom module is enabled.
    /// </summary>
    public bool ModuleEnabled { get; set; }

    /// <summary>
    /// Gets the currently compiled custom module.
    /// </summary>
    public IModule? Module { get; private set; }

    /// <summary>
    /// Gets the most recent compilation and loading status.
    /// </summary>
    public CustomModuleCompilationStatus CompilationStatus
    {
        get;
        private set;
    } = CustomModuleCompilationStatus.NotCompiled;

    /// <summary>
    /// Gets the time the current module was successfully compiled and loaded.
    /// </summary>
    public DateTimeOffset? LastCompiledAt
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets a value indicating whether the custom module is enabled and has
    /// a compiled module available for processing.
    /// </summary>
    public bool ModuleUsable =>
        ModuleEnabled && Module is not null;

    /// <summary>
    /// Stores the successfully compiled custom module and its compilation status.
    /// </summary>
    public void SetModule(
        IModule module,
        bool hasWarnings = false)
    {
        ArgumentNullException.ThrowIfNull(module);

        Module = module;

        CompilationStatus = hasWarnings
            ? CustomModuleCompilationStatus.Warning
            : CustomModuleCompilationStatus.Success;

        LastCompiledAt = DateTimeOffset.Now;
    }

    /// <summary>
    /// Clears the currently compiled module and resets its status.
    /// </summary>
    public void SetModuleNotBuilt()
    {
        Module = null;
        CompilationStatus = CustomModuleCompilationStatus.NotCompiled;
        LastCompiledAt = null;
    }

    /// <summary>
    /// Records a compilation or module-loading failure.
    /// </summary>
    public void SetCompilationFailed()
    {
        Module = null;
        CompilationStatus = CustomModuleCompilationStatus.Failed;
        LastCompiledAt = null;
    }
}