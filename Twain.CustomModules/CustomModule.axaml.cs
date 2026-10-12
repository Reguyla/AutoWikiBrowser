using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Media;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Twain.Core.AWBSettings;
using Twain.Core.CustomModules;
using Twain.Core.Plugin;

namespace Twain.CustomModules;

/// <summary>
/// Provides the user interface for creating, compiling, and enabling
/// custom modules.
/// </summary>
public partial class CustomModule : Avalonia.Controls.Window
{
    private readonly ITwainHost? _host;

    private readonly CustomModuleState _state;

    private bool _isInitializing;

    /// <summary>
    /// Initializes the custom module window.
    /// </summary>
    public CustomModule()
        : this(new CustomModuleState(), null)
    {
    }

    private bool _monacoReady;

    /// <summary>
    /// Initializes the custom module window using the supplied application host.
    /// </summary>
    /// <param name="host">
    /// The application interface supplied to custom modules when they are created.
    /// </param>
    public CustomModule(ITwainHost? host)
        : this(new CustomModuleState(), host)
    {
    }

    private readonly UserPrefs? _preferences;

    /// <summary>
    /// Initializes the custom module window using the supplied state and
    /// application host.
    /// </summary>
    /// <param name="state">
    /// The custom-module state shared with the application.
    /// </param>
    /// <param name="host">
    /// The application interface supplied to custom modules when they are created.
    /// </param>
    public CustomModule(
        CustomModuleState state,
        ITwainHost? host,
        UserPrefs? preferences = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        _state = state;
        _host = host;
        _preferences = preferences;

        _isInitializing = true;

        InitializeComponent();

        CodeEditorWebView.AdapterCreated +=
           CodeEditorWebView_AdapterCreated;

        CodeEditorWebView.WebMessageReceived +=
            CodeEditorWebView_WebMessageReceived;

        LoadLanguages();
        InitializeControlsFromState();

        _isInitializing = false;
    }

    /// <summary>
    /// Applies the shared editor appearance preferences to Monaco.
    /// </summary>
    private async Task ApplyEditorAppearanceAsync()
    {
        if (!_monacoReady || _preferences is null)
        {
            return;
        }

        var general = _preferences.General;

        var appearance = new
        {
            fontFamily = general.TextBoxFont,
            fontSize = general.TextBoxSize,
            wordWrap = general.WordWrap,
            lineNumbers = general.EditorLineNumbers
        };

        string json = JsonSerializer.Serialize(appearance);

        await CodeEditorWebView.InvokeScript(
            $"window.twainEditor.setAppearance({json});");
    }

    /// <summary>
    /// Loads the Monaco editor into the CustomModules WebView.
    /// </summary>
    private void LoadMonacoEditor()
    {
        string editorPath = Path.Combine(
            AppContext.BaseDirectory,
            "Monaco",
            "MonacoEditor.html");

        Uri editorUri = new Uri(
            editorPath,
            UriKind.Absolute);

        CodeEditorWebView.Navigate(editorUri);
    }

    /// <summary>
    /// Loads Monaco when the native WebView adapter is ready.
    /// </summary>
    private void CodeEditorWebView_AdapterCreated(
        object? sender,
        WebViewAdapterEventArgs e)
    {
        CodeEditorWebView.AdapterCreated -=
            CodeEditorWebView_AdapterCreated;

        LoadMonacoEditor();
    }

    /// <summary>
    /// Handles messages received from the Monaco code editor.
    /// </summary>
    private void CodeEditorWebView_WebMessageReceived(
        object? sender,
        WebMessageReceivedEventArgs e)
    {
        try
        {
            using JsonDocument document =
                JsonDocument.Parse(e.Body);

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("type", out JsonElement type))
            {
                return;
            }

            switch (type.GetString())
            {
                case "ready":
                    _monacoReady = true;
                    UpdateMonacoCode();
                    _ = ApplyEditorAppearanceAsync();
                    break;

                case "textChanged":
                    if (root.TryGetProperty(
                        "text",
                        out JsonElement text))
                    {
                        _state.Code =
                            text.GetString() ?? string.Empty;
                    }
                    break;

                case "saveTextToFile":
                    _ = SaveCodeToFileAsync();
                    break;
            }
        }
        catch (JsonException)
        {
            // Ignore messages that are not valid JSON.
        }
    }

    /// <summary>
    /// Saves the current custom-module source code to a local file.
    /// </summary>
    private async Task SaveCodeToFileAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Custom Module Source",
                SuggestedFileName =
                    Compiler?.ToString()?.Contains(
                        "Visual Basic",
                        StringComparison.OrdinalIgnoreCase) == true
                        ? "CustomModule.vb"
                        : "CustomModule.cs",
                FileTypeChoices =
                [
                    new Avalonia.Platform.Storage.FilePickerFileType(
                    "Source files")
                {
                    Patterns = ["*.cs", "*.vb"]
                }
                ]
            });

        if (file is null)
        {
            return;
        }

        await using Stream stream =
            await file.OpenWriteAsync();

        await using StreamWriter writer =
            new(stream);

        await writer.WriteAsync(_state.Code);
    }

    /// <summary>
    /// Updates Monaco with the current custom module source code.
    /// </summary>
    private void UpdateMonacoCode()
    {
        if (!_monacoReady)
        {
            return;
        }

        string codeJson =
            JsonSerializer.Serialize(_state.Code);

        CodeEditorWebView.InvokeScript(
            $"window.twainEditor.setText({codeJson});");
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
    /// Initializes the custom-module controls from the shared application state.
    /// </summary>
    private void InitializeControlsFromState()
    {
        bool hasConfiguredState =
            !string.IsNullOrWhiteSpace(_state.Language) ||
            !string.IsNullOrEmpty(_state.Code) ||
            _state.ModuleEnabled;

        if (!string.IsNullOrWhiteSpace(_state.Language))
        {
            Language = _state.Language;
        }
        else if (Compiler is not null)
        {
            _state.Language = Compiler.ToString();
        }

        if (!hasConfiguredState && Compiler is not null)
        {
            _state.Code = Compiler.CodeExample;
        }

        ModuleEnabledCheckBox.IsChecked =
            _state.ModuleEnabled;

        MakeModuleButton.IsEnabled =
            _state.ModuleEnabled;

        UpdateCompilationStatus();
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
    /// Shows or hides the generated module wrapper while keeping
    /// the module controls and editable code visible.
    /// </summary>
    private void ShowOnlyCodeBoxMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        bool hideWrapper =
            ShowOnlyCodeBoxMenuItem.IsChecked;

        ModuleStartTextBox.IsVisible = !hideWrapper;
        ModuleEndTextBox.IsVisible = !hideWrapper;
    }

    /// <summary>
    /// Changes the Monaco editor font based on the selected option.
    /// </summary>
    private async void FixedWidthFontCheckBox_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        if (!_monacoReady)
        {
            return;
        }

        bool fixedWidth =
            FixedWidthFontCheckBox.IsChecked == true;

        await CodeEditorWebView.InvokeScript(
            $"window.twainEditor.setFixedWidthFont({fixedWidth.ToString().ToLowerInvariant()});");
    }

    /// <summary>
    /// Gets or sets the custom module source code entered by the user.
    /// </summary>
    public string Code
    {
        get => _state.Code;

        set
        {
            string normalizedCode =
                (value ?? string.Empty)
                    .Replace("\r\n\r\n", "\r\n");

            _state.Code = normalizedCode;
            UpdateMonacoCode();
        }
    }

    /// <summary>
    /// Gets or sets the programming language used for the custom module.
    /// </summary>
    public string Language
    {
        get => _state.Language;

        set
        {
            foreach (object? item in LanguageComboBox.Items)
            {
                if (item is not CustomModuleCompiler compiler ||
                    !compiler.CanHandleLanguage(value))
                {
                    continue;
                }

                _state.Language = compiler.ToString();
                LanguageComboBox.SelectedItem = compiler;
                return;
            }

            if (LanguageComboBox.ItemCount > 0)
            {
                LanguageComboBox.SelectedIndex = 0;

                if (Compiler is not null)
                {
                    _state.Language = Compiler.ToString();
                }
            }
        }
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

        if (_isInitializing)
        {
            ModuleStartTextBox.Text = compiler.CodeStart;
            ModuleEndTextBox.Text = compiler.CodeEnd;
            return;
        }

        _state.Language = compiler.ToString();

        ModuleStartTextBox.Text = compiler.CodeStart;
        _state.Code = compiler.CodeExample;
        ModuleEndTextBox.Text = compiler.CodeEnd;

        UpdateMonacoCode();
    }

    /// <summary>
    /// Compiles and loads the current custom module source code.
    /// </summary>
    public void MakeModule()
    {
        try
        {
            CustomModuleCompiler? compiler = Compiler;

            if (compiler is null)
            {
                Module = null;
                return;
            }

            CompilerParameters parameters =
                new()
                {
                    GenerateExecutable = false,
                    IncludeDebugInformation = false
                };

            CustomModuleCompilationReferences.AddLoadedAssemblyReferences(
                parameters);

            CompilerResults results =
                compiler.Compile(
                    _state.Code,
                    parameters);

            if (!ShowCompilationMessages(results))
            {
                _state.SetCompilationFailed();
                UpdateCompilationStatus();
                return;
            }

            Assembly compiledAssembly =
                results.CompiledAssembly
                ?? throw new InvalidOperationException(
                    "The compiler did not return a compiled assembly.");

            Type moduleType =
                CustomModuleCompiler.FindModuleType(
                    compiledAssembly);

            IModule module =
                _host is null
                    ? CustomModuleCompiler.CreateModule(moduleType)
                    : CustomModuleCompiler.CreateModule(moduleType, _host);

            _state.SetModule(
                module,
                results.Errors.HasWarnings);

            UpdateCompilationStatus();
        }
        catch (Exception ex)
        {
            _state.SetCompilationFailed();
            UpdateCompilationStatus();

            ShowRuntimeError(ex);
        }
    }

    /// <summary>
    /// Updates the compilation status displayed in the custom module editor.
    /// </summary>
    private void UpdateCompilationStatus()
    {
        StatusTextBlock.Text =
            _state.CompilationStatus switch
            {
                CustomModuleCompilationStatus.Success =>
                    "Module compiled and loaded",

                CustomModuleCompilationStatus.Warning =>
                    "Module loaded with warnings",

                CustomModuleCompilationStatus.Failed =>
                    "Module compilation failed",

                _ => "No module loaded"
            };

        CompilationStatusBorder.Background =
            _state.CompilationStatus switch
            {
                CustomModuleCompilationStatus.Success =>
                    Brushes.ForestGreen,

                CustomModuleCompilationStatus.Warning =>
                    Brushes.Goldenrod,

                CustomModuleCompilationStatus.Failed =>
                    Brushes.Firebrick,

                _ => Brushes.DimGray
            };

        BuiltAtTextBlock.Text =
            _state.LastCompiledAt is { } compiledAt
                ? BuiltPrefix + compiledAt.ToLocalTime().ToString("g")
                : BuiltPrefix + "n/a";
    }

    /// <summary>
    /// Displays compiler errors and warnings produced while building a custom module.
    /// </summary>
    /// <param name="results">
    /// The results returned by the custom module compiler.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when compilation completed without errors;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool ShowCompilationMessages(
        CompilerResults results)
    {
        if (results.Errors.Count == 0)
            return true;

        StringBuilder messages = new();

        foreach (CompilerError error in results.Errors)
        {
            messages.AppendLine(error.ToString());
        }

        CustomModuleErrors errorDialog =
            new(messages.ToString());

        errorDialog.Title =
            results.Errors.HasErrors
                ? "Custom Module Compilation Errors"
                : "Custom Module Compilation Warnings";

        _ = errorDialog.ShowDialog(this);

        return !results.Errors.HasErrors;
    }

    /// <summary>
    /// Displays an unexpected error encountered while compiling or loading
    /// a custom module.
    /// </summary>
    private async void ShowRuntimeError(Exception exception)
    {
        CustomModuleErrors errorDialog =
            new(exception.ToString());

        errorDialog.Title =
            "Custom Module Error";

        await errorDialog.ShowDialog(this);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the custom module is enabled.
    /// Enabling the module automatically attempts to compile and load it.
    /// </summary>
    public bool ModuleEnabled
    {
        get => _state.ModuleEnabled;

        set
        {
            _state.ModuleEnabled = value;
            ModuleEnabledCheckBox.IsChecked = value;

            if (value)
            {
                MakeModule();
            }
        }
    }

    private void ModuleEnabledCheckBox_Changed(
        object? sender,
        RoutedEventArgs e)
    {
        bool enabled =
            ModuleEnabledCheckBox.IsChecked == true;

        _state.ModuleEnabled = enabled;
        MakeModuleButton.IsEnabled = enabled;
    }

    /// <summary>
    /// Gets a value indicating whether the custom module is enabled and has been
    /// successfully compiled and loaded.
    /// </summary>
    public bool ModuleUsable =>
        _state.ModuleUsable;

    private const string BuiltPrefix =
        "Custom Module Built At: ";

    /// <summary>
    /// Gets or sets the currently loaded custom module.
    /// </summary>
    public IModule? Module
    {
        get => _state.Module;

        private set
        {
            if (value is null)
            {
                _state.SetModuleNotBuilt();
            }
            else
            {
                _state.SetModule(value);
            }

            UpdateCompilationStatus();
        }
    }

    /// <summary>
    /// Clears the currently loaded custom module and updates the module status.
    /// </summary>
    public void SetModuleNotBuilt()
    {
        Module = null;
    }

    /// <summary>
    /// Applies the configured custom-module settings to the window and shared state.
    /// </summary>
    public void ApplySettings(
        string language,
        string code,
        bool enabled)
    {
        Language = language;
        Code = code;
        ModuleEnabled = enabled;

        if (!enabled)
        {
            SetModuleNotBuilt();
        }
    }

    /// <summary>
    /// Disables the custom module and clears any compiled module instance.
    /// </summary>
    public void DisableModule()
    {
        ModuleEnabled = false;
        SetModuleNotBuilt();
    }

    private void MakeModuleButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        MakeModule();
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

}