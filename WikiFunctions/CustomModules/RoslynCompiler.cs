using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Collections.Specialized;

namespace WikiFunctions.CustomModules;

internal static class RoslynCompiler
{
    internal static CompilerResults Compile(
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
                    LanguageVersion.CSharp10),
                path: "CustomModule.cs",
                encoding: Encoding.UTF8);

        List<MetadataReference> references =
            CreateMetadataReferences(
                parameters.ReferencedAssemblies);

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
                    warningLevel: parameters.WarningLevel,
                    generalDiagnosticOption:
                        parameters.TreatWarningsAsErrors
                            ? ReportDiagnostic.Error
                            : ReportDiagnostic.Default));

        using MemoryStream assemblyStream = new();

        EmitResult emitResult =
            compilation.Emit(assemblyStream);

        foreach (Diagnostic diagnostic in emitResult.Diagnostics)
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

        if (!emitResult.Success)
        {
            return results;
        }

        assemblyStream.Position = 0;

        results.CompiledAssembly =
            Assembly.Load(
                assemblyStream.ToArray());

        return results;
    }

    private static List<MetadataReference>
        CreateMetadataReferences(
            StringCollection referencedAssemblies)
    {
        HashSet<string> paths =
            new(StringComparer.OrdinalIgnoreCase);

        string trustedPlatformAssemblies =
            AppContext.GetData(
                "TRUSTED_PLATFORM_ASSEMBLIES")
            as string;

        if (!string.IsNullOrEmpty(
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
            string resolvedPath =
                ResolveReferencePath(reference);

            if (!string.IsNullOrEmpty(resolvedPath))
            {
                paths.Add(resolvedPath);
            }
        }

        return paths
            .Where(File.Exists)
            .Select(path =>
                (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }

    private static string ResolveReferencePath(
        string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return string.Empty;
        }

        if (Path.IsPathFullyQualified(reference) &&
            File.Exists(reference))
        {
            return reference;
        }

        string fileName =
            Path.GetFileName(reference);

        Assembly loadedAssembly =
            AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(
                    assembly =>
                        !assembly.IsDynamic &&
                        string.Equals(
                            Path.GetFileName(
                                assembly.Location),
                            fileName,
                            StringComparison.OrdinalIgnoreCase));

        if (loadedAssembly != null)
        {
            return loadedAssembly.Location;
        }

        string applicationPath =
            Path.Combine(
                AppContext.BaseDirectory,
                fileName);

        return File.Exists(applicationPath)
            ? applicationPath
            : string.Empty;
    }
}