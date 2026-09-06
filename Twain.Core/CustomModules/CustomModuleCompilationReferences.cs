using System.CodeDom.Compiler;
using System.Reflection;

namespace Twain.Core.CustomModules;

/// <summary>
/// Provides assembly-reference setup for dynamically compiled custom code.
/// </summary>
public static class CustomModuleCompilationReferences
{
    /// <summary>
    /// Adds references for assemblies currently loaded by the application.
    /// </summary>
    /// <param name="parameters">
    /// The compiler parameters that receive the assembly references.
    /// </param>
    /// <param name="requiredAssemblies">
    /// Additional assemblies that should be referenced explicitly.
    /// </param>
    public static void AddLoadedAssemblyReferences(
        CompilerParameters parameters,
        params Assembly[] requiredAssemblies)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(requiredAssemblies);

        HashSet<string> referencePaths =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (Assembly assembly in requiredAssemblies)
        {
            AddAssemblyReference(
                referencePaths,
                assembly);
        }

        foreach (Assembly assembly in
                 AppDomain.CurrentDomain.GetAssemblies())
        {
            AddAssemblyReference(
                referencePaths,
                assembly);
        }

        foreach (string referencePath in referencePaths)
        {
            parameters.ReferencedAssemblies.Add(
                referencePath);
        }
    }

    /// <summary>
    /// Adds a loadable assembly location to the reference collection.
    /// </summary>
    private static void AddAssemblyReference(
        ISet<string> referencePaths,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(referencePaths);
        ArgumentNullException.ThrowIfNull(assembly);

        if (assembly.IsDynamic)
        {
            return;
        }

        if (assembly.FullName?.Contains(
                "Microsoft.GeneratedCode",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        string location;

        try
        {
            location = assembly.Location;
        }
        catch (NotSupportedException)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return;
        }

        string fileName =
            Path.GetFileName(location);

        if (string.Equals(
                fileName,
                "mscorlib.dll",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(location))
        {
            referencePaths.Add(location);
        }
    }
}