namespace WikiFunctions.CustomModules;

/// <summary>
/// Provides C# custom-module templates and compiles custom modules
/// using the Roslyn C# compiler.
/// </summary>
public sealed class CSharpCustomModule : CustomModuleCompiler
{
    /// <inheritdoc />
    public override string Name =>
        "C# 14";

    /// <inheritdoc />
    public override bool CanHandleLanguage(string language)
    {
        return string.Equals(
                   language,
                   Name,
                   StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                   language,
                   "C# 12.0",
                   StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                   language,
                   "C# 4.0",
                   StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                   language,
                   "C#",
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override string CodeStart =>
        @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WikiFunctions;

namespace AutoWikiBrowser.CustomModules
{
    public class CustomModule : WikiFunctions.Plugin.IModule
    {
        private readonly WikiFunctions.Plugin.IAutoWikiBrowser awb;

        public CustomModule(WikiFunctions.Plugin.IAutoWikiBrowser awb)
        {
            this.awb = awb;
        }
";

    /// <inheritdoc />
    public override string CodeEnd =>
        @"    }
}";

    /// <inheritdoc />
    public override string CodeExample =>
        @"        public string ProcessArticle(
            string ArticleText,
            string ArticleTitle,
            int wikiNamespace,
            out string Summary,
            out bool Skip)
        {
            Skip = false;
            Summary = ""test"";

            ArticleText = ""test \r\n\r\n"" + ArticleText;

            return ArticleText;
        }";

    /// <inheritdoc />
    public override CompilerResults Compile(
        string sourceCode,
        CompilerParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);
        ArgumentNullException.ThrowIfNull(parameters);

        return RoslynCompiler.Compile(
            BuildWrappedSource(sourceCode),
            parameters);
    }
}