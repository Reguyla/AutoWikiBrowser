using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Loader;

namespace WikiFunctions.CustomModules;

public static class RoslynCompiler
{
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
                    LanguageVersion.Latest),
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

        results.CompiledAssembly =
            AssemblyLoadContext.Default.LoadFromStream(
                assemblyStream);

        return results;
    }

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

            FileLinePositionSpan lineSpan =
                diagnostic.Location.GetLineSpan();

            CompilerError error = new(
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
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