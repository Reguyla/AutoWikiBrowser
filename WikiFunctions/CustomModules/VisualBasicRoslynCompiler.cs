using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Loader;

namespace WikiFunctions.CustomModules;

/// <summary>
/// Compiles Visual Basic custom-module source code using Roslyn.
/// </summary>
internal static class VisualBasicRoslynCompiler
{
    /// <summary>
    /// Compiles Visual Basic source code into an in-memory assembly.
    /// </summary>
    /// <param name="sourceCode">
    /// The complete Visual Basic source code to compile.
    /// </param>
    /// <param name="parameters">
    /// Compilation settings and referenced assemblies.
    /// </param>
    /// <returns>
    /// Compiler results containing diagnostics and, when successful,
    /// the compiled assembly.
    /// </returns>
    internal static CompilerResults Compile(
        string sourceCode,
        CompilerParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);
        ArgumentNullException.ThrowIfNull(parameters);

        CompilerResults results =
            new(parameters.TempFiles);

        VisualBasicParseOptions parseOptions =
            VisualBasicParseOptions.Default;

        SyntaxTree syntaxTree =
            VisualBasicSyntaxTree.ParseText(
                SourceText.From(
                    sourceCode,
                    Encoding.UTF8),
                parseOptions,
                path: "CustomModule.vb");

        IReadOnlyList<MetadataReference> references =
            CreateMetadataReferences(
                parameters.ReferencedAssemblies);

        OutputKind outputKind =
            parameters.GenerateExecutable
                ? OutputKind.ConsoleApplication
                : OutputKind.DynamicallyLinkedLibrary;

        VisualBasicCompilationOptions compilationOptions =
            new(
                outputKind: outputKind,
                optionStrict: OptionStrict.Off,
                optionInfer: true,
                optionExplicit: true,
                optionCompareText: false,
                optimizationLevel:
                    parameters.IncludeDebugInformation
                        ? OptimizationLevel.Debug
                        : OptimizationLevel.Release,
                generalDiagnosticOption:
                    parameters.TreatWarningsAsErrors
                        ? ReportDiagnostic.Error
                        : ReportDiagnostic.Default);

        VisualBasicCompilation compilation =
            VisualBasicCompilation.Create(
                assemblyName:
                    $"AWB.VisualBasicCustomModule.{Guid.NewGuid():N}",
                syntaxTrees:
                [
                    syntaxTree
                ],
                references:
                    references,
                options:
                    compilationOptions);

        using MemoryStream assemblyStream =
            new();

        EmitResult emitResult =
            compilation.Emit(
                assemblyStream);

        AddDiagnostics(
            results,
            emitResult.Diagnostics);

        if (!emitResult.Success)
        {
            results.NativeCompilerReturnValue = 1;
            return results;
        }

        assemblyStream.Position = 0;

        results.CompiledAssembly =
            AssemblyLoadContext.Default.LoadFromStream(
                assemblyStream);

        results.NativeCompilerReturnValue = 0;

        return results;
    }

    /// <summary>
    /// Creates Roslyn metadata references from framework assemblies,
    /// loaded assemblies, and caller-provided references.
    /// </summary>
    private static IReadOnlyList<MetadataReference>
        CreateMetadataReferences(
            StringCollection referencedAssemblies)
    {
        HashSet<string> referencePaths =
            new(StringComparer.OrdinalIgnoreCase);

        AddTrustedPlatformAssemblies(
            referencePaths);

        AddLoadedAssemblyReferences(
            referencePaths);

        AddExplicitReferences(
            referencePaths,
            referencedAssemblies);

        return referencePaths
            .Select(
                path =>
                    MetadataReference.CreateFromFile(
                        path))
            .Cast<MetadataReference>()
            .ToList();
    }

    /// <summary>
    /// Adds the runtime's trusted platform assemblies.
    /// </summary>
    private static void AddTrustedPlatformAssemblies(
        ISet<string> referencePaths)
    {
        string? trustedPlatformAssemblies =
            AppContext.GetData(
                "TRUSTED_PLATFORM_ASSEMBLIES")
            as string;

        if (string.IsNullOrWhiteSpace(
                trustedPlatformAssemblies))
        {
            return;
        }

        foreach (string path in
                 trustedPlatformAssemblies.Split(
                     Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (File.Exists(path))
            {
                referencePaths.Add(path);
            }
        }
    }

    /// <summary>
    /// Adds assemblies already loaded by the current AWB process.
    /// </summary>
    private static void AddLoadedAssemblyReferences(
        ISet<string> referencePaths)
    {
        foreach (Assembly assembly in
                 AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            string? location =
                GetAssemblyLocation(
                    assembly);

            if (!string.IsNullOrWhiteSpace(location) &&
                File.Exists(location))
            {
                referencePaths.Add(location);
            }
        }
    }

    /// <summary>
    /// Adds references supplied through CompilerParameters.
    /// </summary>
    private static void AddExplicitReferences(
        ISet<string> referencePaths,
        StringCollection referencedAssemblies)
    {
        foreach (string reference in
                 referencedAssemblies)
        {
            string? resolvedPath =
                ResolveReferencePath(
                    reference);

            if (!string.IsNullOrWhiteSpace(
                    resolvedPath))
            {
                referencePaths.Add(
                    resolvedPath);
            }
        }
    }

    /// <summary>
    /// Resolves a reference that may be either a full path or an
    /// assembly filename.
    /// </summary>
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

        if (File.Exists(reference))
        {
            return Path.GetFullPath(reference);
        }

        string requestedFileName =
            Path.GetFileName(reference);

        foreach (Assembly assembly in
                 AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            string? location =
                GetAssemblyLocation(
                    assembly);

            if (string.IsNullOrWhiteSpace(location))
            {
                continue;
            }

            if (string.Equals(
                    Path.GetFileName(location),
                    requestedFileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return location;
            }
        }

        return null;
    }

    /// <summary>
    /// Safely retrieves the physical path of an assembly.
    /// </summary>
    private static string? GetAssemblyLocation(
        Assembly assembly)
    {
        try
        {
            return string.IsNullOrWhiteSpace(
                    assembly.Location)
                ? null
                : assembly.Location;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Converts Roslyn diagnostics into CodeDOM-compatible
    /// compiler errors.
    /// </summary>
    private static void AddDiagnostics(
        CompilerResults results,
        IEnumerable<Diagnostic> diagnostics)
    {
        foreach (Diagnostic diagnostic in
                 diagnostics)
        {
            if (diagnostic.Severity is not
                DiagnosticSeverity.Error and not
                DiagnosticSeverity.Warning)
            {
                continue;
            }

            FileLinePositionSpan lineSpan =
                diagnostic.Location.IsInSource
                    ? diagnostic.Location.GetLineSpan()
                    : default;

            CompilerError compilerError =
                new()
                {
                    ErrorNumber =
                        diagnostic.Id,

                    ErrorText =
                        diagnostic.GetMessage(),

                    IsWarning =
                        diagnostic.Severity ==
                        DiagnosticSeverity.Warning,

                    FileName =
                        diagnostic.Location.IsInSource
                            ? lineSpan.Path
                            : string.Empty,

                    Line =
                        diagnostic.Location.IsInSource
                            ? lineSpan.StartLinePosition.Line + 1
                            : 0,

                    Column =
                        diagnostic.Location.IsInSource
                            ? lineSpan.StartLinePosition.Character + 1
                            : 0
                };

            results.Errors.Add(
                compilerError);
        }
    }

}