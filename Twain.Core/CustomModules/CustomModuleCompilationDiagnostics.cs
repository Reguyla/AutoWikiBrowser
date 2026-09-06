using System.CodeDom.Compiler;
using System.Text;

namespace Twain.Core.CustomModules;

/// <summary>
/// Provides shared interpretation and formatting of custom-module compiler
/// diagnostics.
/// </summary>
public static class CustomModuleCompilationDiagnostics
{
    /// <summary>
    /// Determines whether the supplied compilation results contain errors.
    /// </summary>
    public static bool HasErrors(
        CompilerResults results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Errors.HasErrors;
    }

    /// <summary>
    /// Formats compiler errors and warnings for display.
    /// </summary>
    public static string Format(
        CompilerResults results)
    {
        ArgumentNullException.ThrowIfNull(results);

        StringBuilder builder = new();

        foreach (CompilerError error in results.Errors)
        {
            if (error.Line > 0)
            {
                builder.AppendFormat(
                    "Line {0}, col {1}: ",
                    error.Line,
                    error.Column);
            }

            if (!string.IsNullOrEmpty(
                    error.ErrorNumber))
            {
                builder.AppendFormat(
                    "[{0}] ",
                    error.ErrorNumber);
            }

            builder.Append(
                error.ErrorText);

            builder.AppendLine();
        }

        return builder.ToString();
    }
}