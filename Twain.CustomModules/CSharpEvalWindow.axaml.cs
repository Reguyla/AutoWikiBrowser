using Avalonia.Interactivity;
using System.CodeDom.Compiler;
using Twain.Core;
using Twain.Core.CustomModules;

namespace Twain.CustomModules;

/// <summary>
/// Provides a user interface for compiling and executing individual
/// C# expressions.
/// </summary>
public partial class CSharpEvalWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes the C# evaluator window.
    /// </summary>
    public CSharpEvalWindow()
    {
        InitializeComponent();

        Opened += (_, _) =>
        {
            ExpressionTextBox.Focus();
        };
    }

    /// <summary>
    /// Compiles and executes the entered C# expression.
    /// </summary>
    private async void EvaluateButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ResultTextBox.Clear();

        try
        {
            CompilerResults results =
                CSharpExpressionEvaluator.Compile(
                    ExpressionTextBox.Text ?? string.Empty,
                    typeof(CSharpEvalWindow).Assembly,
                    typeof(Tools).Assembly);

            if (!await DisplayCompilerDiagnosticsAsync(
                    results))
            {
                return;
            }

            object? result =
                CSharpExpressionEvaluator.Execute(
                    results.CompiledAssembly);

            ResultTextBox.Text =
                result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            await ShowEvaluationErrorAsync(ex);
        }
    }

    /// <summary>
    /// Displays compiler errors or warnings produced while compiling the
    /// evaluator expression.
    /// </summary>
    private async Task<bool> DisplayCompilerDiagnosticsAsync(
        CompilerResults results)
    {
        if (results.Errors.Count == 0)
        {
            return true;
        }

        bool hasErrors =
            CustomModuleCompilationDiagnostics.HasErrors(
                results);

        CustomModuleErrors errorDialog =
            new(
                CustomModuleCompilationDiagnostics.Format(
                    results))
            {
                Title =
                    hasErrors
                        ? "Compilation errors"
                        : "Compilation warnings"
            };

        await errorDialog.ShowDialog(this);

        return !hasErrors;
    }

    /// <summary>
    /// Displays an exception raised while compiling or executing evaluator
    /// code.
    /// </summary>
    private async Task ShowEvaluationErrorAsync(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception displayedException =
            exception.InnerException ??
            exception;

        CustomModuleErrors errorDialog =
            new(
                displayedException.ToString())
            {
                Title = "C# evaluation error"
            };

        await errorDialog.ShowDialog(this);
    }

    /// <summary>
    /// Closes the evaluator window.
    /// </summary>
    private void CloseButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}