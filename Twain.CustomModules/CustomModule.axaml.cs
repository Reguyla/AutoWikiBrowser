using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Diagnostics;
using Twain.Core.CustomModules;

namespace Twain.CustomModules;

/// <summary>
/// Provides the user interface for creating, compiling, and enabling
/// custom modules.
/// </summary>
public partial class CustomModule : Avalonia.Controls.Window
{
    private static readonly Avalonia.Media.FontFamily FixedWidthFont =
        new("Cascadia Mono, Consolas, Courier New");

    private static readonly Avalonia.Media.FontFamily DefaultFont =
        Avalonia.Media.FontFamily.Default;

    /// <summary>
    /// Initializes the custom module window.
    /// </summary>
    public CustomModule()
    {
        InitializeComponent();

        LoadLanguages();
    }

    /// <summary>
    /// Loads the available custom module compilers into the language selector.
    /// </summary>
    private void LoadLanguages()
    {
        LanguageComboBox.Items.Clear();

        foreach (CustomModuleCompiler compiler in CustomModuleCompiler.GetList())
        {
            LanguageComboBox.Items.Add(compiler);
        }

        if (LanguageComboBox.ItemCount > 0)
        {
            LanguageComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Gets the compiler for the currently selected custom module language.
    /// </summary>
    private CustomModuleCompiler? Compiler =>
        LanguageComboBox.SelectedItem as CustomModuleCompiler;

    /// <summary>
    /// Closes the custom module window.
    /// </summary>
    private void CloseButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Shows or hides the controls surrounding the code editor.
    /// </summary>
    private void ShowOnlyCodeBoxMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        bool showOnlyCode =
            ShowOnlyCodeBoxMenuItem.IsChecked;

        ModuleControlsPanel.IsVisible = !showOnlyCode;
        ModuleStartTextBox.IsVisible = !showOnlyCode;
        ModuleEndTextBox.IsVisible = !showOnlyCode;
    }

    /// <summary>
    /// Changes the code editor between fixed-width and default fonts.
    /// </summary>
    private void FixedWidthFontCheckBox_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        CodeTextBox.FontFamily =
            FixedWidthFontCheckBox.IsChecked == true
                ? FixedWidthFont
                : DefaultFont;
    }

    /// <summary>
    /// Gets or sets the custom module source code entered by the user.
    /// </summary>
    public string Code
    {
        get => CodeTextBox.Text ?? string.Empty;

        set =>
            CodeTextBox.Text =
                (value ?? string.Empty)
                    .Replace("\r\n\r\n", "\r\n");
    }

    /// <summary>
    /// Gets or sets the programming language used for the custom module.
    /// </summary>
    public string Language
    {
        get => Compiler?.ToString() ?? string.Empty;

        set
        {
            foreach (object? item in LanguageComboBox.Items)
            {
                if (item is not CustomModuleCompiler compiler ||
                    !compiler.CanHandleLanguage(value))
                {
                    continue;
                }

                LanguageComboBox.SelectedItem = compiler;
                return;
            }

            if (LanguageComboBox.ItemCount > 0)
            {
                LanguageComboBox.SelectedIndex = 0;
            }
        }
    }

    /// <summary>
    /// Compiles and loads the current custom module.
    /// </summary>
    public void MakeModule()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the custom module is enabled.
    /// Enabling the module automatically attempts to compile and load it.
    /// </summary>
    public bool ModuleEnabled
    {
        get => ModuleEnabledCheckBox.IsChecked == true;

        set
        {
            ModuleEnabledCheckBox.IsChecked = value;

            if (value)
            {
                MakeModule();
            }
        }
    }

    /// <summary>
    /// Selects all text in the custom module editor.
    /// </summary>
    private void SelectAllMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CodeTextBox.SelectAll();
    }

    /// <summary>
    /// Moves the caret to the requested line.
    /// </summary>
    private void GoToLineTextBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (!int.TryParse(
                GoToLineTextBox.Text,
                out int requestedLine))
        {
            return;
        }

        GoToLine(requestedLine);

        e.Handled = true;
    }

    /// <summary>
    /// Moves the code editor caret to the specified one-based line number.
    /// </summary>
    private void GoToLine(int lineNumber)
    {
        if (lineNumber < 1)
            return;

        string text =
            CodeTextBox.Text ?? string.Empty;

        int currentLine = 1;
        int position = 0;

        while (currentLine < lineNumber &&
               position < text.Length)
        {
            int nextLine =
                text.IndexOf('\n', position);

            if (nextLine < 0)
                return;

            position = nextLine + 1;
            currentLine++;
        }

        if (currentLine != lineNumber)
            return;

        CodeTextBox.CaretIndex = position;
        CodeTextBox.Focus();
    }

    /*
     * The handlers below are intentionally left as integration points.
     *
     * Their existing business logic should be moved from the WinForms
     * CustomModule class rather than duplicated in the Avalonia view.
     */

    private void ModuleEnabledCheckBox_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        MakeModuleButton.IsEnabled =
            ModuleEnabledCheckBox.IsChecked == true;
    }

    /// <summary>
    /// Updates the custom module template when the selected language changes.
    /// </summary>
    private void LanguageComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        CustomModuleCompiler? compiler = Compiler;

        if (compiler is null)
        {
            return;
        }

        ModuleStartTextBox.Text = compiler.CodeStart;
        CodeTextBox.Text = compiler.CodeExample;
        ModuleEndTextBox.Text = compiler.CodeEnd;
    }

    private void MakeModuleButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
    }

    private async void GuideMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        const string guideText =
            "A custom module allows you to process article text using your own C# or Visual Basic code.\n\n" +
            "Select \"Make module\" to compile and load your code into Twain.\n\n" +
            "Twain calls the \"ProcessArticle\" method while processing each article. " +
            "Do not change the signature of this method, as Twain uses it to communicate with your module.\n\n" +
            "\"articleText\" contains the current article text, \"articleTitle\" contains the article title, " +
            "and \"namespaceID\" identifies the article namespace.\n\n" +
            "Set \"summary\" to the edit-summary text produced by your module. " +
            "Set \"skip\" to true when Twain should skip the current article; otherwise, leave it false.\n\n" +
            "For more detailed information, select Help > Manual.";

        Window guideWindow =
            new()
            {
                Title = "Guide",
                Width = 500,
                Height = 360,
                MinWidth = 450,
                MinHeight = 320,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background =
                    this.FindResource("TwainApplicationBackgroundBrush")
                    as Avalonia.Media.IBrush
            };

        TextBlock messageText =
            new()
            {
                Text = guideText,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground =
                    this.FindResource("TwainTextPrimaryBrush")
                    as Avalonia.Media.IBrush,
                FontSize = 12
            };

        Button okButton =
            new()
            {
                Content = "OK",
                Width = 90,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };

        Grid grid =
            new()
            {
                Margin = new Avalonia.Thickness(16),
                RowDefinitions =
                {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
                },
                RowSpacing = 16
            };

        grid.Children.Add(messageText);

        Grid.SetRow(okButton, 1);
        grid.Children.Add(okButton);

        guideWindow.Content = grid;

        okButton.Click += (_, _) =>
            guideWindow.Close();

        await guideWindow.ShowDialog(this);
    }

    private void ManualMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        const string manualUrl =
            "https://en.wikipedia.org/wiki/Wikipedia:AutoWikiBrowser/Custom_Modules";

        Process.Start(
            new ProcessStartInfo
            {
                FileName = manualUrl,
                UseShellExecute = true
            });
    }

    private void UndoMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
    }

    private void CutMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
    }

    private void CopyMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
    }

    private void PasteMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
    }
}