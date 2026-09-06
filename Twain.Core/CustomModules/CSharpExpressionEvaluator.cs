using System.CodeDom.Compiler;
using System.Reflection;

namespace Twain.Core.CustomModules;

/// <summary>
/// Compiles and executes individual C# expressions.
/// </summary>
public static class CSharpExpressionEvaluator
{
    private const string EvaluatorTypeName =
        "CSharpEvaluator.CSharpEval";

    /// <summary>
    /// Compiles a C# expression into an in-memory evaluator assembly.
    /// </summary>
    /// <param name="expression">
    /// The C# expression to compile.
    /// </param>
    /// <param name="requiredAssemblies">
    /// Assemblies that must be explicitly available to the expression.
    /// </param>
    /// <returns>
    /// The compiler results, including diagnostics and the compiled assembly
    /// when compilation succeeds.
    /// </returns>
    public static CompilerResults Compile(
        string expression,
        params Assembly[] requiredAssemblies)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(requiredAssemblies);

        CompilerParameters parameters =
            CreateCompilerParameters();

        CustomModuleCompilationReferences.AddLoadedAssemblyReferences(
            parameters,
            requiredAssemblies);

        return RoslynCompiler.Compile(
            BuildEvaluatorSource(expression),
            parameters);
    }

    /// <summary>
    /// Executes a previously compiled C# evaluator assembly.
    /// </summary>
    /// <param name="assembly">
    /// The compiled evaluator assembly.
    /// </param>
    /// <returns>
    /// The value returned by the evaluated expression.
    /// </returns>
    public static object? Execute(
        Assembly? assembly)
    {
        if (assembly is null)
        {
            throw new InvalidOperationException(
                "The compiler did not return a compiled assembly.");
        }

        Type evaluatorType =
            assembly.GetType(
                EvaluatorTypeName,
                throwOnError: true)
            ?? throw new TypeLoadException(
                $"Type '{EvaluatorTypeName}' could not be loaded.");

        object evaluator =
            Activator.CreateInstance(evaluatorType)
            ?? throw new InvalidOperationException(
                $"Type '{EvaluatorTypeName}' could not be instantiated.");

        MethodInfo evaluateMethod =
            evaluatorType.GetMethod(
                "EvalCode",
                BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMethodException(
                EvaluatorTypeName,
                "EvalCode");

        try
        {
            return evaluateMethod.Invoke(
                evaluator,
                null);
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                "The evaluated expression threw an exception.",
                ex.InnerException);
        }
    }

    /// <summary>
    /// Creates the compiler settings used for expression evaluation.
    /// </summary>
    private static CompilerParameters
        CreateCompilerParameters()
    {
        return new CompilerParameters
        {
            GenerateExecutable = false,
            GenerateInMemory = true,
            IncludeDebugInformation = false,
            TreatWarningsAsErrors = false,
            WarningLevel = 4
        };
    }

    /// <summary>
    /// Wraps an expression in the source required for compilation.
    /// </summary>
    private static string BuildEvaluatorSource(
        string expression)
    {
        return
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Text.RegularExpressions;
            using Twain.Core;

            namespace CSharpEvaluator
            {
                internal sealed class CSharpEval
                {
                    public object EvalCode()
                    {
                        return 
            """
            + expression
            +
            """
            ;
                    }
                }
            }
            """;
    }
}