namespace Twain.Core.CustomModules;

/// <summary>
/// Provides Visual Basic custom-module source templates and compilation.
/// </summary>
public sealed class VbModuleCompiler : CustomModuleCompiler
{
    /// <inheritdoc />
    public override string Name =>
        "VB.NET 17.13";

    /// <inheritdoc />
    public override string CodeStart =>
        @"Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic
Imports Twain.Core

Namespace AutoWikiBrowser.CustomModules
    Public Class CustomModule
        Implements Twain.Core.Plugin.IModule

        Private ReadOnly host As Twain.Core.Plugin.ITwainHost

        Public Sub New(host As Twain.Core.Plugin.ITwainHost)
            Me.host = host
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
        ) As String Implements Twain.Core.Plugin.IModule.ProcessArticle

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