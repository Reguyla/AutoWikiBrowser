
namespace WikiFunctions.CustomModules;

/// <summary>
/// Provides Visual Basic custom-module source templates and compilation.
/// </summary>
public sealed class VbModuleCompiler : CustomModuleCompiler
{
    /// <inheritdoc />
    public override string Name =>
        "VB.NET 2.0";

    /// <inheritdoc />
    public override string CodeStart =>
        @"Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic
Imports WikiFunctions

Namespace AutoWikiBrowser.CustomModules
    Public Class CustomModule
        Implements WikiFunctions.Plugin.IModule

        Private awb As WikiFunctions.Plugin.IAutoWikiBrowser

        Public Sub New(ByRef _awb As WikiFunctions.Plugin.IAutoWikiBrowser)
            awb = _awb
        End Sub
";

    /// <inheritdoc />
    public override string CodeEnd =>
        @"    End Class
End Namespace";

    /// <inheritdoc />
    public override string CodeExample =>
        @"        Public Function ProcessArticle(
            ByVal ArticleText As String,
            ByVal ArticleTitle As String,
            ByVal wikiNamespace As Integer,
            ByRef Summary As String,
            ByRef Skip As Boolean
        ) As String Implements WikiFunctions.Plugin.IModule.ProcessArticle

            Skip = False
            Summary = ""test""

            ArticleText =
                ""test "" & VbCrLf & VbCrLf & ArticleText

            Return ArticleText
        End Function";

    /// <summary>
    /// Compiles the Visual Basic custom module using Roslyn.
    /// </summary>
    /// <param name="sourceCode">
    /// The user-provided module source code.
    /// </param>
    /// <param name="parameters">
    /// Compilation settings and referenced assemblies.
    /// </param>
    /// <returns>The compilation results.</returns>
    public override CompilerResults Compile(
        string sourceCode,
        CompilerParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);
        ArgumentNullException.ThrowIfNull(parameters);

        string wrappedSource =
            BuildWrappedSource(sourceCode);

        return VisualBasicRoslynCompiler.Compile(
            wrappedSource,
            parameters);
    }
}