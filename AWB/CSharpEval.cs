using System.CodeDom.Compiler;
using Twain.Core;
using Twain.Core.CustomModules;

namespace AutoWikiBrowser;

/// <summary>
/// Provides a form for compiling and executing C# evaluator code.
/// </summary>
public partial class CSharpEval : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpEval"/> class.
    /// </summary>
    public CSharpEval()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        textBox2.Clear();

        try
        {
            CompilerResults results =
                CSharpExpressionEvaluator.Compile(
                    textBox1.Text,
                    typeof(CSharpEval).Assembly,
                    typeof(Tools).Assembly);

            if (!DisplayCompilerDiagnostics(results))
            {
                return;
            }

            object? result =
                CSharpExpressionEvaluator.Execute(
                    results.CompiledAssembly);

            textBox2.Text =
                result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            ShowEvaluationError(ex);
        }
    }

    /// <summary>
    /// Displays compiler errors and warnings produced while compiling evaluator
    /// code.
    /// </summary>
    /// <param name="results">
    /// The compilation results containing any reported diagnostics.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when compilation produced no errors; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Warnings are displayed to the user but do not prevent the compiled
    /// evaluator from running.
    /// </remarks>
    private bool DisplayCompilerDiagnostics(
        CompilerResults results)
    {
        if (results.Errors.Count == 0)
        {
            return true;
        }

        bool hasErrors = false;
        StringBuilder builder = new();

        foreach (CompilerError error in results.Errors)
        {
            hasErrors |= !error.IsWarning;

            if (error.Line > 0)
            {
                builder.AppendFormat(
                    "Line {0}, col {1}: ",
                    error.Line,
                    error.Column);
            }

            if (!string.IsNullOrEmpty(error.ErrorNumber))
            {
                builder.AppendFormat(
                    "[{0}] ",
                    error.ErrorNumber);
            }

            builder.Append(error.ErrorText);
            builder.AppendLine();
        }

        using CustomModuleErrors errorDialog = new();

        errorDialog.ErrorText = builder.ToString();

        errorDialog.Text =
            "Compilation " +
            (hasErrors
                ? "errors"
                : "warnings");

        errorDialog.ShowDialog(this);

        return !hasErrors;
    }

    /// <summary>
    /// Displays an exception raised while compiling or executing evaluator code.
    /// </summary>
    /// <param name="exception">
    /// The exception to display.
    /// </param>
    private void ShowEvaluationError(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception displayedException =
            exception.InnerException ??
            exception;

        MessageBox.Show(
            this,
            displayedException.ToString(),
            "C# evaluation error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}