using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Loader;

namespace WikiFunctions.CustomModules;

/// <summary>
/// Compiles C# source code for an AutoWikiBrowser custom module by using the
/// Roslyn compiler platform.
/// </summary>
/// <remarks>
/// <para>
/// Custom modules are always compiled as in-memory dynamic-link libraries and
/// loaded into the application's default assembly load context.
/// </para>
/// <para>
/// This compiler supports only the subset of <see cref="CompilerParameters"/>
/// required by the custom-module system. Options such as
/// <see cref="CompilerParameters.GenerateExecutable"/>,
/// <see cref="CompilerParameters.GenerateInMemory"/>, and
/// <see cref="CompilerParameters.OutputAssembly"/> do not control the output.
/// </para>
/// </remarks>
public static class RoslynCompiler
{
    /// <summary>
    /// Compiles the supplied C# source code into an in-memory assembly.
    /// </summary>
    /// <param name="sourceCode">
    /// The C# source code for the custom module.
    /// </param>
    /// <param name="parameters">
    /// The compiler parameters containing assembly references, warning
    /// configuration, and debug-build preferences.
    /// </param>
    /// <returns>
    /// A <see cref="CompilerResults"/> instance containing compiler diagnostics
    /// and, when compilation succeeds, the compiled assembly.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="sourceCode"/> is empty or consists only of white-space
    /// characters.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="parameters"/> is <see langword="null"/>.
    /// </exception>
    public static CompilerResults Compile(
        string sourceCode,
        CompilerParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCode);
        ArgumentNullException.ThrowIfNull(parameters);

        CompilerResults results =
            new(parameters.TempFiles);

        SyntaxTree syntaxTree =
            CSharpSyntaxTree.ParseText(
                sourceCode,
                new CSharpParseOptions(
                    LanguageVersion.CSharp14),
                path: "CustomModule.cs",
                encoding: Encoding.UTF8);

        List<MetadataReference> references =
            CreateMetadataReferences(
                parameters.ReferencedAssemblies);

        int warningLevel =
            parameters.WarningLevel is >= 0 and <= 4
                ? parameters.WarningLevel
                : 4;

        CSharpCompilation compilation =
            CSharpCompilation.Create(
                $"AWBCustomModule_{Guid.NewGuid():N}",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel:
                        parameters.IncludeDebugInformation
                            ? OptimizationLevel.Debug
                            : OptimizationLevel.Release,
                    warningLevel: warningLevel,
                    generalDiagnosticOption:
                        parameters.TreatWarningsAsErrors
                            ? ReportDiagnostic.Error
                            : ReportDiagnostic.Default));

        using MemoryStream assemblyStream = new();

        // TODO: Emit a portable PDB when IncludeDebugInformation is enabled.
        // The current implementation selects Debug optimization but does not
        // generate a separate debugging-symbol stream.
        EmitResult emitResult =
            compilation.Emit(assemblyStream);

        AddDiagnostics(
            results,
            emitResult.Diagnostics);

        if (!emitResult.Success)
        {
            return results;
        }

        assemblyStream.Position = 0;

        // TODO: Consider loading custom modules into a collectible
        // AssemblyLoadContext so superseded compilations can be unloaded.
        results.CompiledAssembly =
            AssemblyLoadContext.Default.LoadFromStream(
                assemblyStream);

        return results;
    }

    /// <summary>
    /// Adds Roslyn compilation warnings and errors to the supplied compiler results.
    /// </summary>
    /// <param name="results">
    /// The compiler results that receive the converted diagnostics.
    /// </param>
    /// <param name="diagnostics">
    /// The Roslyn diagnostics produced while compiling the custom module.
    /// </param>
    private static void AddDiagnostics(
        CompilerResults results,
        IEnumerable<Diagnostic> diagnostics)
    {
        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Severity is not
                DiagnosticSeverity.Warning and not
                DiagnosticSeverity.Error)
            {
                continue;
            }

            string fileName = string.Empty;
            int line = 0;
            int column = 0;

            if (diagnostic.Location.IsInSource)
            {
                FileLinePositionSpan lineSpan =
                    diagnostic.Location.GetMappedLineSpan();

                fileName = lineSpan.Path;
                line = lineSpan.StartLinePosition.Line + 1;
                column = lineSpan.StartLinePosition.Character + 1;
            }

            CompilerError error = new(
                fileName,
                line,
                column,
                diagnostic.Id,
                diagnostic.GetMessage())
            {
                IsWarning =
                    diagnostic.Severity ==
                    DiagnosticSeverity.Warning
            };

            results.Errors.Add(error);
        }
    }

    private static List<MetadataReference>
        CreateMetadataReferences(
            StringCollection referencedAssemblies)
    {
        HashSet<string> paths =
            new(StringComparer.OrdinalIgnoreCase);

        string? trustedPlatformAssemblies =
            AppContext.GetData(
                "TRUSTED_PLATFORM_ASSEMBLIES")
            as string;

        if (!string.IsNullOrWhiteSpace(
                trustedPlatformAssemblies))
        {
            foreach (string path in
                     trustedPlatformAssemblies.Split(
                         Path.PathSeparator,
                         StringSplitOptions.RemoveEmptyEntries))
            {
                paths.Add(path);
            }
        }

        foreach (string reference in referencedAssemblies)
        {
            string? resolvedPath =
                ResolveReferencePath(reference);

            if (resolvedPath is not null)
            {
                paths.Add(resolvedPath);
            }
        }

        return paths
            .Where(File.Exists)
            .Select(
                path =>
                    (MetadataReference)
                    MetadataReference.CreateFromFile(path))
            .ToList();
    }

    private static string? ResolveReferencePath(
        string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        if (Path.IsPathFullyQualified(reference) &&
            File.Exists(reference))
        {
            return Path.GetFullPath(reference);
        }

        string fileName =
            Path.GetFileName(reference);

        string applicationPath =
            Path.Combine(
                AppContext.BaseDirectory,
                fileName);

        if (File.Exists(applicationPath))
        {
            return applicationPath;
        }

        foreach (Assembly assembly in
                 AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            string location;

            try
            {
                location = assembly.Location;
            }
            catch (NotSupportedException)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                continue;
            }

            if (string.Equals(
                    Path.GetFileName(location),
                    fileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return location;
            }
        }

        return null;
    }
}