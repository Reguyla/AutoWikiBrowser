namespace Twain.UI.Templates;

/// <summary>
/// Allows the user to edit template-substitution settings.
/// </summary>
public partial class SubstTemplatesWindow : Avalonia.Controls.Window
{
    private readonly string[] _originalTemplateList;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="SubstTemplatesWindow"/> class for the XAML previewer.
    /// </summary>
    public SubstTemplatesWindow()
        : this(
            [],
            true,
            false,
            false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="SubstTemplatesWindow"/> class.
    /// </summary>
    /// <param name="templateList">
    /// The currently configured template list.
    /// </param>
    /// <param name="expandRecursively">
    /// Whether templates should be expanded recursively.
    /// </param>
    /// <param name="ignoreUnformatted">
    /// Whether unformatted portions of article text should be ignored.
    /// </param>
    /// <param name="includeComments">
    /// Whether comments should be included during recursive expansion.
    /// </param>
    public SubstTemplatesWindow(
        string[] templateList,
        bool expandRecursively,
        bool ignoreUnformatted,
        bool includeComments)
    {
        ArgumentNullException.ThrowIfNull(templateList);

        InitializeComponent();

        _originalTemplateList =
            [.. templateList];

        TemplateList =
            [.. templateList];

        ExpandRecursively =
            expandRecursively;

        IgnoreUnformatted =
            ignoreUnformatted;

        IncludeComments =
            includeComments;

        ApplySettingsToControls();
    }

    /// <summary>
    /// Gets the template list committed by the dialog.
    /// </summary>
    public string[] TemplateList { get; private set; }

    /// <summary>
    /// Gets whether template substitution should expand templates
    /// recursively.
    /// </summary>
    public bool ExpandRecursively { get; private set; }

    /// <summary>
    /// Gets whether unformatted portions of article text should be ignored.
    /// </summary>
    public bool IgnoreUnformatted { get; private set; }

    /// <summary>
    /// Gets whether comments should be included during recursive template
    /// expansion.
    /// </summary>
    public bool IncludeComments { get; private set; }

    /// <summary>
    /// Populates the controls from the current committed settings.
    /// </summary>
    private void ApplySettingsToControls()
    {
        TemplatesTextBox.Text =
            string.Join(
                Environment.NewLine,
                TemplateList);

        ExpandRecursivelyCheckBox.IsChecked =
            ExpandRecursively;

        IgnoreUnformattedCheckBox.IsChecked =
            IgnoreUnformatted;

        IncludeCommentsCheckBox.IsChecked =
            IncludeComments;

        UpdateIncludeCommentsState();
    }

    /// <summary>
    /// Clears the editable template list without changing the committed
    /// settings.
    /// </summary>
    private void ClearButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        TemplatesTextBox.Text =
            string.Empty;
    }

    /// <summary>
    /// Restores the template editor to the values supplied when the dialog
    /// was opened.
    /// </summary>
    private void ResetButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        TemplatesTextBox.Text =
            string.Join(
                Environment.NewLine,
                _originalTemplateList);
    }

    /// <summary>
    /// Commits the current settings and closes the dialog.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        TemplateList =
            GetTemplateLines();

        ExpandRecursively =
            ExpandRecursivelyCheckBox.IsChecked == true;

        IgnoreUnformatted =
            IgnoreUnformattedCheckBox.IsChecked == true;

        IncludeComments =
            IncludeCommentsCheckBox.IsChecked == true;

        Close(true);
    }

    /// <summary>
    /// Closes the dialog without committing the current edits.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    /// <summary>
    /// Updates the availability of the include-comments option when
    /// recursive template expansion changes.
    /// </summary>
    private void ExpandRecursivelyCheckBox_Changed(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateIncludeCommentsState();
    }

    /// <summary>
    /// Enables the include-comments option only when recursive template
    /// expansion is enabled.
    /// </summary>
    private void UpdateIncludeCommentsState()
    {
        IncludeCommentsCheckBox.IsEnabled =
            ExpandRecursivelyCheckBox.IsChecked == true;
    }

    /// <summary>
    /// Splits the current editor contents into individual template names.
    /// </summary>
    private string[] GetTemplateLines()
    {
        string text =
            TemplatesTextBox.Text ?? string.Empty;

        return text.Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal)
            .Replace(
                '\r',
                '\n')
            .Split('\n');
    }
}