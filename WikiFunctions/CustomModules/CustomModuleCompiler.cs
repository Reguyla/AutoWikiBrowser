using System.Reflection;

namespace WikiFunctions.CustomModules;

/// <summary>
/// Defines the shared behavior for custom-module language compilers.
/// </summary>
public abstract class CustomModuleCompiler
{
    private static readonly object ResolvablePathsLock = new();

    private static readonly Dictionary<string, string> ResolvablePaths =
        new(StringComparer.OrdinalIgnoreCase);

    static CustomModuleCompiler()
    {
        AppDomain.CurrentDomain.AssemblyResolve +=
            ResolveAssembly;
    }

    /// <summary>
    /// Gets the human-readable language name.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the code prepended to the module source.
    /// </summary>
    public abstract string CodeStart { get; }

    /// <summary>
    /// Gets the code appended to the module source.
    /// </summary>
    public abstract string CodeEnd { get; }

    /// <summary>
    /// Gets the default content displayed in the code input box.
    /// </summary>
    public abstract string CodeExample { get; }

    /// <summary>
    /// Gets or sets the CodeDOM provider used by legacy compiler
    /// implementations.
    /// </summary>
    /// <remarks>
    /// C# compilation overrides <see cref="Compile"/> and uses Roslyn.
    /// This property remains temporarily for other language compilers.
    /// </remarks>
    protected CodeDomProvider? Compiler { get; set; }

    /// <summary>
    /// Determines whether this compiler can compile the specified language.
    /// </summary>
    /// <param name="language">The language name to check.</param>
    /// <returns>
    /// <see langword="true"/> when the language is supported; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public virtual bool CanHandleLanguage(
        string language)
    {
        return string.Equals(
            Name,
            language,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Compiles the supplied custom-module source code.
    /// </summary>
    /// <param name="sourceCode">
    /// The user-provided source code, excluding the compiler-specific wrapper.
    /// </param>
    /// <param name="parameters">
    /// The compilation settings and assembly references.
    /// </param>
    /// <returns>The results of the compilation.</returns>
    public abstract CompilerResults Compile(
        string sourceCode,
        CompilerParameters parameters);

    /// <summary>
    /// Returns the compiler's human-readable language name.
    /// </summary>
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Returns the currently available custom-module compilers.
    /// </summary>
    public static CustomModuleCompiler[] GetList()
    {
        // Preserve the historic order for compatibility and user experience.
        List<CustomModuleCompiler> modules =
        [
            new CSharpCustomModule()
        ];

        AddToList(
            modules,
            typeof(VbModuleCompiler));

        return modules.ToArray();
    }

    /// <summary>
    /// Wraps user-provided source with the compiler-specific beginning
    /// and ending code.
    /// </summary>
    protected string BuildWrappedSource(
        string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        return string.Concat(
            CodeStart,
            sourceCode,
            Environment.NewLine,
            CodeEnd);
    }

    #region Helpers

    private static void AddToList(
        ICollection<CustomModuleCompiler> modules,
        Type compilerType)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(compilerType);

        try
        {
            modules.Add(
                Instantiate<CustomModuleCompiler>(
                    compilerType));
        }
        catch (
            Exception ex) when (
                ex is MissingMethodException or
                MemberAccessException or
                TargetInvocationException or
                TypeLoadException)
        {
            // The optional language compiler is unavailable.
        }
    }

    protected static T Instantiate<T>(
        Type type)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!typeof(T).IsAssignableFrom(type))
        {
            throw new ArgumentException(
                $"Type '{type.FullName}' is not assignable to " +
                $"'{typeof(T).FullName}'.",
                nameof(type));
        }

        ConstructorInfo constructor =
            type.GetConstructor(Type.EmptyTypes)
            ?? throw new MissingMethodException(
                type.FullName,
                ".ctor()");

        return constructor.Invoke(null) as T
            ?? throw new InvalidOperationException(
                $"Type '{type.FullName}' could not be instantiated.");
    }

    protected static object Instantiate(
        Assembly assembly,
        string typeName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        Type type =
            assembly.GetType(
                typeName,
                throwOnError: true)
            ?? throw new TypeLoadException(
                $"Type '{typeName}' could not be loaded.");

        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException(
                $"Type '{typeName}' could not be instantiated.");
    }

    protected static Assembly LoadAssembly(
        string path,
        string dependentAssembliesPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dependentAssembliesPrefix);

        string fullPath =
            Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "The requested assembly could not be found.",
                fullPath);
        }

        string directory =
            Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException(
                $"The directory for '{fullPath}' could not be determined.");

        lock (ResolvablePathsLock)
        {
            ResolvablePaths[dependentAssembliesPrefix] =
                directory;
        }

        return Assembly.LoadFile(fullPath);
    }

    private static Assembly? ResolveAssembly(
        object? sender,
        ResolveEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        AssemblyName requestedAssembly =
            new(args.Name);

        string? requestedName =
            requestedAssembly.Name;

        if (string.IsNullOrWhiteSpace(requestedName))
        {
            return null;
        }

        KeyValuePair<string, string>[] paths;

        lock (ResolvablePathsLock)
        {
            paths =
                ResolvablePaths.ToArray();
        }

        foreach (
            KeyValuePair<string, string> entry in paths)
        {
            if (!requestedName.StartsWith(
                    entry.Key,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string assemblyPath =
                Path.Combine(
                    entry.Value,
                    requestedName + ".dll");

            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFile(
                    assemblyPath);
            }
        }

        return null;
    }

    #endregion
}