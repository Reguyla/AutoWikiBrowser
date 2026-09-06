namespace Twain.Core.CustomModules;

/// <summary>
/// Represents the result of compiling and evaluating a C# expression.
/// </summary>
public sealed class CSharpExpressionEvaluationResult
{
    /// <summary>
    /// Initializes a new evaluation result.
    /// </summary>
    public CSharpExpressionEvaluationResult(
        CompilerResults compilationResults,
        object? value)
    {
        ArgumentNullException.ThrowIfNull(
            compilationResults);

        CompilationResults =
            compilationResults;

        Value =
            value;
    }

    /// <summary>
    /// Gets the compiler results produced while building the evaluator.
    /// </summary>
    public CompilerResults CompilationResults { get; }

    /// <summary>
    /// Gets the value returned by the evaluated expression.
    /// </summary>
    public object? Value { get; }
}