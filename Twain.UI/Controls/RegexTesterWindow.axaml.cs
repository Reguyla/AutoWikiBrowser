using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Twain.Core;
using Twain.Core.Controls;

namespace Twain.UI.Controls;

/// <summary>
/// Provides an Avalonia interface for testing regular expressions
/// against article text.
/// </summary>
public partial class RegexTesterWindow : Avalonia.Controls.Window
{
    private int _operationVersion;
    private bool _busy;
    private bool _allowClose;
    private bool? _applyChangesResult;

    /// <summary>
    /// Initializes a new regex tester window.
    /// </summary>
    public RegexTesterWindow()
        : this(false)
    {
    }

    /// <summary>
    /// Initializes a new regex tester window.
    /// </summary>
    /// <param name="askToApply">
    /// Whether closing the tester should ask the user if the edited regex
    /// settings should be applied to the caller.
    /// </param>
    public RegexTesterWindow(bool askToApply)
    {
        InitializeComponent();

        AskToApply = askToApply;

        KeyDown += RegexTesterWindow_KeyDown;

        Closing += RegexTesterWindow_Closing;
    }

    /// <summary>
    /// Handles the optional apply-changes prompt when the regex tester closes.
    /// </summary>
    private async void RegexTesterWindow_Closing(
        object? sender,
        WindowClosingEventArgs e)
    {
        if (_allowClose)
            return;

        if (_busy)
            AbortProcessing();

        if (!AskToApply)
            return;

        e.Cancel = true;

        RegexApplyChangesWindow prompt =
            new();

        bool? result =
            await prompt.ShowDialog<bool?>(this);

        if (result is null)
            return;

        _applyChangesResult =
            result;

        _allowClose = true;

        Close();
    }

    /// <summary>
    /// Gets or sets the article text displayed in the regex tester.
    /// </summary>
    public string ArticleText
    {
        get => InputTextBox.Text ?? string.Empty;
        set => InputTextBox.Text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the regular expression used to find matching text.
    /// </summary>
    public string Find
    {
        get => FindTextBox.Text ?? string.Empty;
        set => FindTextBox.Text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the replacement expression used by the regex tester.
    /// </summary>
    public string Replace
    {
        get => ReplaceTextBox.Text ?? string.Empty;
        set => ReplaceTextBox.Text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the title of the article currently being tested.
    /// </summary>
    /// <remarks>
    /// When supplied, AWB replacement keywords are expanded before a
    /// replacement operation is executed.
    /// </remarks>
    public string? ArticleTitle { get; set; }

    /// <summary>
    /// Gets or sets the selected regular-expression options.
    /// </summary>
    public RegexOptions RegexOptions
    {
        get
        {
            RegexOptions options =
                RegexOptions.None;

            if (Multiline)
                options |= RegexOptions.Multiline;

            if (Singleline)
                options |= RegexOptions.Singleline;

            if (IgnoreCase)
                options |= RegexOptions.IgnoreCase;

            if (ExplicitCapture)
                options |= RegexOptions.ExplicitCapture;

            return options;
        }

        set
        {
            Multiline =
                (value & RegexOptions.Multiline) != 0;

            Singleline =
                (value & RegexOptions.Singleline) != 0;

            IgnoreCase =
                (value & RegexOptions.IgnoreCase) != 0;

            ExplicitCapture =
                (value & RegexOptions.ExplicitCapture) != 0;
        }
    }

    /// <summary>
    /// Gets or sets whether multiline regular-expression behavior is enabled.
    /// </summary>
    public bool Multiline
    {
        get => MultilineCheckBox.IsChecked == true;
        set => MultilineCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether single-line regular-expression behavior is enabled.
    /// </summary>
    public bool Singleline
    {
        get => SinglelineCheckBox.IsChecked == true;
        set => SinglelineCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether matching ignores character case.
    /// </summary>
    public bool IgnoreCase
    {
        get => IgnoreCaseCheckBox.IsChecked == true;
        set => IgnoreCaseCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Gets or sets whether explicit-capture behavior is enabled.
    /// </summary>
    public bool ExplicitCapture
    {
        get => ExplicitCaptureCheckBox.IsChecked == true;
        set => ExplicitCaptureCheckBox.IsChecked = value;
    }

    /// <summary>
    /// Updates the availability of the Find and Replace commands.
    /// </summary>
    private void ConditionsChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        UpdateCommandState();
    }

    /// <summary>
    /// Updates command availability based on the current regex and input text.
    /// </summary>
    private void UpdateCommandState()
    {
        bool enabled =
            !string.IsNullOrEmpty(Find) &&
            !string.IsNullOrEmpty(ArticleText);

        FindButton.IsEnabled =
            enabled && !ProgressBar.IsIndeterminate;

        ReplaceButton.IsEnabled =
            enabled && !ProgressBar.IsIndeterminate;
    }

    /// <summary>
    /// Executes the current regular expression as a find operation.
    /// </summary>
    private async void FindButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        int operationVersion =
            ++_operationVersion;

        ClearResults();
        SetBusy(true);

        try
        {
            string input = ArticleText;
            string pattern = Find;
            RegexOptions options = RegexOptions;

            RegexTestResult result =
                await Task.Run(
                    () => RegexTesterProcessor.Find(
                        input,
                        pattern,
                        options));

            if (operationVersion != _operationVersion)
                return;

            DisplayFindResult(result);
        }
        catch (Exception ex)
        {
            if (operationVersion == _operationVersion)
                ShowProcessingError(ex);
        }
        finally
        {
            if (operationVersion == _operationVersion)
                SetBusy(false);
        }
    }

    /// <summary>
    /// Executes the current regular expression as a replacement operation.
    /// </summary>
    private async void ReplaceButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        int operationVersion =
            ++_operationVersion;

        ClearResults();
        SetBusy(true);

        try
        {
            string input = ArticleText;
            string pattern = Find;

            string replacement =
                Replace;

            if (!string.IsNullOrEmpty(ArticleTitle))
            {
                replacement =
                    Tools.ApplyKeyWords(
                        ArticleTitle,
                        replacement);
            }

            replacement =
                NormalizeLineEndings(
                    replacement);

            RegexOptions options = RegexOptions;

            RegexTestResult result =
                await Task.Run(
                    () => RegexTesterProcessor.Replace(
                        input,
                        pattern,
                        replacement,
                        options));

            if (operationVersion != _operationVersion)
                return;

            DisplayReplaceResult(result);
        }
        catch (Exception ex)
        {
            if (operationVersion == _operationVersion)
                ShowProcessingError(ex);
        }
        finally
        {
            if (operationVersion == _operationVersion)
                SetBusy(false);
        }
    }

    /// <summary>
    /// Displays the matches and capture groups returned by a find operation.
    /// </summary>
    private void DisplayFindResult(
        RegexTestResult result)
    {
        CapturesTreeView.IsVisible = true;
        ResultTextBox.IsVisible = false;

        List<TreeViewItem> matchItems = [];

        int displayedMatches = 0;

        foreach (Match match in result.Matches)
        {
            TreeViewItem matchItem =
                new()
                {
                    Header =
                        "{" +
                        FormatCaptureText(match.Value) +
                        "}",
                    IsExpanded = true
                };

            foreach (Group group in match.Groups)
            {
                if (group.Captures.Count > 1)
                {
                    TreeViewItem groupItem =
                        new()
                        {
                            Header = "...",
                            IsExpanded = true
                        };

                    List<TreeViewItem> captureItems = [];

                    foreach (Capture capture in group.Captures)
                    {
                        captureItems.Add(
                            new TreeViewItem
                            {
                                Header =
                                    "{" +
                                    FormatCaptureText(capture.Value) +
                                    "}"
                            });
                    }

                    groupItem.ItemsSource =
                        captureItems;

                    AddChild(
                        matchItem,
                        groupItem);
                }
                else if (group.Captures.Count == 1)
                {
                    AddChild(
                        matchItem,
                        new TreeViewItem
                        {
                            Header =
                                "{" +
                                FormatCaptureText(
                                    group.Captures[0].Value) +
                                "}"
                        });
                }
            }

            matchItems.Add(matchItem);

            displayedMatches++;

            if (displayedMatches >= 500)
                break;
        }

        CapturesTreeView.ItemsSource =
            matchItems;

        StatusText.Text =
            CreateFindStatus(
                result.Matches.Count,
                result.ExecutionTimeMilliseconds);
    }

    /// <summary>
    /// Displays the output returned by a replacement operation.
    /// </summary>
    private void DisplayReplaceResult(
        RegexTestResult result)
    {
        CapturesTreeView.IsVisible = false;
        ResultTextBox.IsVisible = true;

        ResultTextBox.Text =
            FormatReplacementResult(
                result.ReplacementResult ??
                string.Empty);

        StatusText.Text =
            CreateReplacementStatus(
                result.Matches.Count,
                result.ExecutionTimeMilliseconds);
    }

    /// <summary>
    /// Adds a child node to a tree-view item.
    /// </summary>
    private static void AddChild(
        TreeViewItem parent,
        TreeViewItem child)
    {
        List<TreeViewItem> children = [];

        if (parent.ItemsSource is IEnumerable existingItems)
        {
            foreach (object? item in existingItems)
            {
                if (item is TreeViewItem treeViewItem)
                    children.Add(treeViewItem);
            }
        }

        children.Add(child);

        parent.ItemsSource =
            children;
    }

    /// <summary>
    /// Clears the previous regex test results.
    /// </summary>
    private void ClearResults()
    {
        CapturesTreeView.ItemsSource = null;
        ResultTextBox.Text = string.Empty;
        StatusText.Text = string.Empty;
    }

    /// <summary>
    /// Updates the busy-state presentation.
    /// </summary>
    private void SetBusy(bool busy)
    {
        _busy = busy;

        ProgressBar.IsIndeterminate =
            busy;

        if (busy)
        {
            FindButton.IsEnabled = false;
            ReplaceButton.IsEnabled = false;

            StatusText.Text =
                "Processing (ESC to cancel)";
        }
        else
        {
            UpdateCommandState();
        }
    }

    /// <summary>
    /// Handles Escape for cancelling processing or closing the tester.
    /// </summary>
    private void RegexTesterWindow_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        e.Handled = true;

        if (_busy)
        {
            AbortProcessing();
            return;
        }

        Close();
    }

    /// <summary>
    /// Cancels the currently active regex test and prevents its result from
    /// being displayed when background processing completes.
    /// </summary>
    private void AbortProcessing()
    {
        if (!_busy)
            return;

        _operationVersion++;

        SetBusy(false);

        StatusText.Text =
            "Processing aborted";
    }

    /// <summary>
    /// Converts line breaks to a visible escape sequence for capture display.
    /// </summary>
    private static string FormatCaptureText(
        string value)
    {
        return value
            .Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal)
            .Replace(
                "\n",
                "\\n",
                StringComparison.Ordinal);
    }

    /// <summary>
    /// Normalizes replacement text before it is passed to the regex processor.
    /// </summary>
    private static string NormalizeLineEndings(
        string value)
    {
        return value.Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Formats replacement output for display in the multiline result editor.
    /// </summary>
    private static string FormatReplacementResult(
        string value)
    {
        string normalized =
            value.Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal);

        normalized =
            normalized.Replace(
                "\\n",
                "\n",
                StringComparison.Ordinal);

        return normalized.Replace(
            "\n",
            Environment.NewLine,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates the status message for a find operation.
    /// </summary>
    private static string CreateFindStatus(
        int matchCount,
        long elapsedMilliseconds)
    {
        string message =
            matchCount switch
            {
                0 => "No matches",
                1 => "1 match found",
                > 500 =>
                    $"{matchCount} matches found (showing first 500)",
                _ =>
                    $"{matchCount} matches found"
            };

        return
            $"{message} in {elapsedMilliseconds} ms";
    }

    /// <summary>
    /// Creates the status message for a replacement operation.
    /// </summary>
    private static string CreateReplacementStatus(
        int replacementCount,
        long elapsedMilliseconds)
    {
        string message =
            replacementCount == 1
                ? "1 replacement performed"
                : $"{replacementCount} replacements performed";

        return
            $"{message} in {elapsedMilliseconds} ms";
    }

    /// <summary>
    /// Displays an error produced while compiling or executing a regular expression.
    /// </summary>
    private void ShowProcessingError(
        Exception exception)
    {
        StatusText.Text =
            $"Error: {exception.Message}";
    }

    /// <summary>
    /// Opens the .NET regular-expression quick-reference documentation.
    /// </summary>
    private void HelpButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Tools.OpenURLInBrowser(
            "https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference");
    }

    /// <summary>
    /// Gets whether the caller should be asked to apply changes when the
    /// regex tester is closed.
    /// </summary>
    public bool AskToApply { get; }

    /// <summary>
    /// Gets the result of the apply-changes prompt.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> means apply the edited settings;
    /// <see langword="false"/> means discard them; and
    /// <see langword="null"/> means no decision has been made.
    /// </remarks>
    public bool? ApplyChangesResult =>
        _applyChangesResult;
}