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
    /// Gets a value indicating whether the custom module is enabled and has
    /// a compiled module available for processing.
    /// </summary>
    public bool ModuleUsable =>
        ModuleEnabled && Module is not null;

    /// <summary>
    /// Stores the successfully compiled custom module.
    /// </summary>
    public void SetModule(
        IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        Module = module;
    }

    /// <summary>
    /// Clears the currently compiled custom module.
    /// </summary>
    public void SetModuleNotBuilt()
    {
        Module = null;
    }
}