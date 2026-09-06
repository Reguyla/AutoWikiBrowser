using Twain.Core.AWBSettings;
using Twain.Core.ExternalPrograms;

namespace Twain.UI.ExternalPrograms;

/// <summary>
/// Provides the Avalonia configuration interface for external program
/// processing.
/// </summary>
public partial class ExternalProgramWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes the external program configuration window.
    /// </summary>
    public ExternalProgramWindow()
        : this(new ExternalProgramPrefs())
    {
    }

    /// <summary>
    /// Initializes the external program configuration window from the
    /// supplied settings.
    /// </summary>
    /// <param name="settings">
    /// The external program settings to display.
    /// </param>
    public ExternalProgramWindow(
        ExternalProgramPrefs settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        InitializeComponent();

        ApplySettings(settings);
        UpdateEnabledState();
        UpdateInputModeState();
    }

    /// <summary>
    /// Gets the external program settings currently represented by the
    /// window.
    /// </summary>
    public ExternalProgramPrefs Settings =>
        new()
        {
            Enabled =
                EnabledCheckBox.IsChecked == true,

            Skip =
                SkipCheckBox.IsChecked == true,

            Program =
                ProgramTextBox.Text ?? string.Empty,

            Parameters =
                ParametersTextBox.Text ?? string.Empty,

            PassAsFile =
                FileModeRadioButton.IsChecked == true,

            OutputFile =
                OutputFileTextBox.Text ?? string.Empty
        };

    /// <summary>
    /// Applies external program settings to the window controls.
    /// </summary>
    /// <param name="settings">
    /// The settings to display.
    /// </param>
    private void ApplySettings(
        ExternalProgramPrefs settings)
    {
        EnabledCheckBox.IsChecked =
            settings.Enabled;

        SkipCheckBox.IsChecked =
            settings.Skip;

        ProgramTextBox.Text =
            settings.Program;

        ParametersTextBox.Text =
            settings.Parameters;

        FileModeRadioButton.IsChecked =
            settings.PassAsFile;

        ParameterModeRadioButton.IsChecked =
            !settings.PassAsFile;

        OutputFileTextBox.Text =
            settings.OutputFile;
    }

    /// <summary>
    /// Updates the enabled state of the external program configuration
    /// controls.
    /// </summary>
    private void UpdateEnabledState()
    {
        bool enabled =
            EnabledCheckBox.IsChecked == true;

        ConfigurationGroup.IsEnabled =
            enabled;

        SkipCheckBox.IsEnabled =
            enabled;
    }

    /// <summary>
    /// Updates the controls associated with file-based processing.
    /// </summary>
    private void UpdateInputModeState()
    {
        bool fileMode =
            FileModeRadioButton.IsChecked == true;

        OutputFileTextBox.IsEnabled =
            fileMode;

        BrowseOutputFileButton.IsEnabled =
            fileMode;
    }

    /// <summary>
    /// Updates the window when external program processing is enabled or
    /// disabled.
    /// </summary>
    private void EnabledCheckBox_Changed(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateEnabledState();
        UpdateInputModeState();
    }

    /// <summary>
    /// Updates the window when the external program input mode changes.
    /// </summary>
    private void InputMode_Changed(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateInputModeState();
    }

    /// <summary>
    /// Validates and accepts the current external program settings.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!ExternalProgramConfiguration.IsValid(Settings))
        {
            return;
        }

        Close(true);
    }

    /// <summary>
    /// Cancels configuration without accepting the current values.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    private void BrowseProgramButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Added in the next step.
    }

    private void BrowseOutputFileButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Added in the next step.
    }
}