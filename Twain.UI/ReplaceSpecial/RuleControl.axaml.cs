using Avalonia.Controls;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Twain.Core.ReplaceSpecial;
using Twain.UI.Controls;

namespace Twain.UI.ReplaceSpecial;

/// <summary>
/// Provides the Avalonia editor used to configure a
/// <see cref="Rule"/>.
/// </summary>
public partial class RuleControl : Avalonia.Controls.UserControl
{
    private readonly IRuleControlOwner? _owner;
    private bool _loadingRule;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="RuleControl"/> class.
    /// </summary>
    public RuleControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="RuleControl"/> class for the specified rule.
    /// </summary>
    /// <param name="rule">
    /// The rule edited by this control.
    /// </param>
    public RuleControl(
        Rule rule)
        : this(
            rule,
            null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="RuleControl"/> class for the specified rule and owner.
    /// </summary>
    /// <param name="rule">
    /// The rule edited by this control.
    /// </param>
    /// <param name="owner">
    /// The owner that receives notifications when the rule name changes.
    /// </param>
    public RuleControl(
        Rule rule,
        IRuleControlOwner? owner)
        : this()
    {
        ArgumentNullException.ThrowIfNull(rule);

        _owner = owner;

        RestoreFromRule(rule);
    }

    /// <summary>
    /// Sets the name displayed in the rule name text box.
    /// </summary>
    /// <param name="name">
    /// The rule name to display.
    /// </param>
    public void SetName(
        string name)
    {
        NameTextBox.Text =
            name;
    }

    /// <summary>
    /// Selects all text in the rule name text box.
    /// </summary>
    public void SelectName()
    {
        NameTextBox.Focus();
        NameTextBox.SelectAll();
    }

    /// <summary>
    /// Saves the values currently displayed by the control to the specified
    /// rule.
    /// </summary>
    /// <param name="rule">
    /// The rule to update.
    /// </param>
    public void SaveToRule(
        Rule rule)
    {
        if (rule is null)
        {
            return;
        }

        rule.Name =
            NameTextBox.Text?.Trim() ??
            string.Empty;

        rule.replace_ =
            (ReplaceTextBox.Text ?? string.Empty)
                .Replace(
                    "\r\n",
                    "\n");

        rule.with_ =
            (WithTextBox.Text ?? string.Empty)
                .Replace(
                    "\r\n",
                    "\n");

        rule.ruletype_ =
            (Rule.T)RuleTypeComboBox.SelectedIndex;

        rule.enabled_ =
            RuleEnabledCheckBox.IsChecked == true;

        rule.regex_ =
            ReplaceIsRegexCheckBox.IsChecked == true;

        rule.regexOptions_ =
            RegexOptions.None;

        if (ReplaceIsCaseSensitiveCheckBox.IsChecked != true)
        {
            rule.regexOptions_ |=
                RegexOptions.IgnoreCase;
        }

        if (ReplaceIsSinglelineCheckBox.IsChecked == true)
        {
            rule.regexOptions_ |=
                RegexOptions.Singleline;
        }

        if (ReplaceIsMultilineCheckBox.IsChecked == true)
        {
            rule.regexOptions_ |=
                RegexOptions.Multiline;
        }

        rule.numoftimes_ =
            (int)(NumberOfTimesUpDown.Value ?? 0);

        rule.ifContains_ =
            IfContainsTextBox.Text ??
            string.Empty;

        rule.ifNotContains_ =
            IfNotContainsTextBox.Text ??
            string.Empty;

        rule.ifIsRegex_ =
            IfIsRegexCheckBox.IsChecked == true;

        rule.ifRegexOptions_ =
            RegexOptions.None;

        if (IfIsCaseSensitiveCheckBox.IsChecked != true)
        {
            rule.ifRegexOptions_ |=
                RegexOptions.IgnoreCase;
        }

        if (IfIsSinglelineCheckBox.IsChecked == true)
        {
            rule.ifRegexOptions_ |=
                RegexOptions.Singleline;
        }

        if (IfIsMultilineCheckBox.IsChecked == true)
        {
            rule.ifRegexOptions_ |=
                RegexOptions.Multiline;
        }
    }

    /// <summary>
    /// Restores the specified rule values to the editor.
    /// </summary>
    /// <param name="rule">
    /// The rule whose values should be displayed.
    /// </param>
    public void RestoreFromRule(
        Rule rule)
    {
        if (rule is null)
        {
            return;
        }

        _loadingRule = true;

        try
        {
            NameTextBox.Text =
                rule.Name;

            ReplaceTextBox.Text =
                rule.replace_.Replace(
                    "\n",
                    "\r\n");

            WithTextBox.Text =
                rule.with_.Replace(
                    "\n",
                    "\r\n");

            RuleTypeComboBox.SelectedIndex =
                (int)rule.ruletype_;

            RuleEnabledCheckBox.IsChecked =
                rule.enabled_;

            ReplaceIsRegexCheckBox.IsChecked =
                rule.regex_;

            ReplaceIsCaseSensitiveCheckBox.IsChecked =
                (rule.regexOptions_ &
                 RegexOptions.IgnoreCase) == 0;

            ReplaceIsMultilineCheckBox.IsChecked =
                (rule.regexOptions_ &
                 RegexOptions.Multiline) != 0;

            ReplaceIsSinglelineCheckBox.IsChecked =
                (rule.regexOptions_ &
                 RegexOptions.Singleline) != 0;

            NumberOfTimesUpDown.Value =
                rule.numoftimes_;

            IfContainsTextBox.Text =
                rule.ifContains_;

            IfNotContainsTextBox.Text =
                rule.ifNotContains_;

            IfIsRegexCheckBox.IsChecked =
                rule.ifIsRegex_;

            IfIsCaseSensitiveCheckBox.IsChecked =
                (rule.ifRegexOptions_ &
                 RegexOptions.IgnoreCase) == 0;

            IfIsMultilineCheckBox.IsChecked =
                (rule.ifRegexOptions_ &
                 RegexOptions.Multiline) != 0;

            IfIsSinglelineCheckBox.IsChecked =
                (rule.ifRegexOptions_ &
                 RegexOptions.Singleline) != 0;

            UpdateRegexOptionControls();
        }
        finally
        {
            _loadingRule = false;
        }
    }

    /// <summary>
    /// Notifies the control owner when the rule name changes.
    /// </summary>
    private void NameTextBox_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (_loadingRule)
        {
            return;
        }

        _owner?.NameChanged(
            NameTextBox.Text?.Trim() ??
            string.Empty);
    }

    /// <summary>
    /// Selects the complete rule name when the name text box is
    /// double-clicked.
    /// </summary>
    private void NameTextBox_DoubleTapped(
        object? sender,
        Avalonia.Input.TappedEventArgs e)
    {
        SelectName();
    }

    private void UpdateRegexOptionControls()
    {
        bool replaceRegexEnabled =
            ReplaceIsRegexCheckBox.IsChecked == true;

        ReplaceIsMultilineCheckBox.IsEnabled =
            replaceRegexEnabled;

        ReplaceIsSinglelineCheckBox.IsEnabled =
            replaceRegexEnabled;

        TestFindButton.IsEnabled =
            replaceRegexEnabled;

        bool ifRegexEnabled =
            IfIsRegexCheckBox.IsChecked == true;

        IfIsMultilineCheckBox.IsEnabled =
            ifRegexEnabled;

        IfIsSinglelineCheckBox.IsEnabled =
            ifRegexEnabled;

        TestIfButton.IsEnabled =
            ifRegexEnabled;
    }

    private void ReplaceIsRegexCheckBox_IsCheckedChanged(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateRegexOptionControls();
        }

    private void IfIsRegexCheckBox_IsCheckedChanged(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateRegexOptionControls();
    }

    private async void TestFindButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RegexTesterWindow tester =
            new(true)
            {
                Find =
                    ReplaceTextBox.Text ??
                    string.Empty,

                Replace =
                    WithTextBox.Text ??
                    string.Empty,

                Multiline =
                    ReplaceIsMultilineCheckBox.IsChecked == true,

                Singleline =
                    ReplaceIsSinglelineCheckBox.IsChecked == true,

                IgnoreCase =
                    ReplaceIsCaseSensitiveCheckBox.IsChecked != true
            };

        Window? owner =
            TopLevel.GetTopLevel(this) as Window;

        if (owner is not null)
        {
            await tester.ShowDialog(owner);
        }
        else
        {
            tester.Show();
            return;
        }

        if (tester.ApplyChangesResult != true)
        {
            return;
        }

        ReplaceTextBox.Text =
            tester.Find;

        WithTextBox.Text =
            tester.Replace;

        ReplaceIsMultilineCheckBox.IsChecked =
            tester.Multiline;

        ReplaceIsSinglelineCheckBox.IsChecked =
            tester.Singleline;

        ReplaceIsCaseSensitiveCheckBox.IsChecked =
            !tester.IgnoreCase;
    }

    private void TestIfButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        TestIfButton.ContextMenu?.Open(
            TestIfButton);
    }

    private async void TestIfContainsMenuItem_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RegexTesterWindow tester =
            new(true)
            {
                Find =
                    IfContainsTextBox.Text ??
                    string.Empty,

                Multiline =
                    IfIsMultilineCheckBox.IsChecked == true,

                Singleline =
                    IfIsSinglelineCheckBox.IsChecked == true,

                IgnoreCase =
                    IfIsCaseSensitiveCheckBox.IsChecked != true
            };

        Window? owner =
            TopLevel.GetTopLevel(this) as Window;

        if (owner is not null)
        {
            await tester.ShowDialog(owner);
        }
        else
        {
            tester.Show();
            return;
        }

        if (tester.ApplyChangesResult != true)
        {
            return;
        }

        IfContainsTextBox.Text =
            tester.Find;

        IfIsMultilineCheckBox.IsChecked =
            tester.Multiline;

        IfIsSinglelineCheckBox.IsChecked =
            tester.Singleline;

        IfIsCaseSensitiveCheckBox.IsChecked =
            !tester.IgnoreCase;
    }

    private async void TestIfNotContainsMenuItem_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RegexTesterWindow tester =
            new(true)
            {
                Find =
                    IfNotContainsTextBox.Text ??
                    string.Empty,

                Multiline =
                    IfIsMultilineCheckBox.IsChecked == true,

                Singleline =
                    IfIsSinglelineCheckBox.IsChecked == true,

                IgnoreCase =
                    IfIsCaseSensitiveCheckBox.IsChecked != true
            };

        Window? owner =
            TopLevel.GetTopLevel(this) as Window;

        if (owner is not null)
        {
            await tester.ShowDialog(owner);
        }
        else
        {
            tester.Show();
            return;
        }

        if (tester.ApplyChangesResult != true)
        {
            return;
        }

        IfNotContainsTextBox.Text =
            tester.Find;

        IfIsMultilineCheckBox.IsChecked =
            tester.Multiline;

        IfIsSinglelineCheckBox.IsChecked =
            tester.Singleline;

        IfIsCaseSensitiveCheckBox.IsChecked =
            !tester.IgnoreCase;
    }

}