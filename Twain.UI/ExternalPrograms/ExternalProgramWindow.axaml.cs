using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
    private async void OkButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ExternalProgramPrefs settings =
            Settings;

        if (!ExternalProgramConfiguration.IsValid(settings))
        {
            await ShowValidationWarningAsync(settings);
            return;
        }

        Close(true);
    }

    /// <summary>
    /// Displays a warning describing the missing external program setting.
    /// </summary>
    /// <param name="settings">
    /// The external program settings that failed validation.
    /// </param>
    private async Task ShowValidationWarningAsync(
        ExternalProgramPrefs settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string message;

        if (string.IsNullOrWhiteSpace(settings.Program))
        {
            message =
                "Please select the external program to run.";
        }
        else if (settings.PassAsFile &&
                 string.IsNullOrWhiteSpace(settings.OutputFile))
        {
            message =
                "Please specify the input/output file.";
        }
        else
        {
            message =
                "Please specify the command-line parameters.";
        }

        Avalonia.Controls.Window warningWindow =
            new()
            {
                Title = "External Program",
                Width = 420,
                Height = 170,
                CanResize = false,
                WindowStartupLocation =
                    Avalonia.Controls.WindowStartupLocation.CenterOwner,

                Content =
                    new Avalonia.Controls.StackPanel
                    {
                        Margin = new Avalonia.Thickness(16),
                        Spacing = 16,

                        Children =
                        {
                        new Avalonia.Controls.TextBlock
                        {
                            Text = message,
                            TextWrapping =
                                Avalonia.Media.TextWrapping.Wrap
                        },

                        new Avalonia.Controls.Button
                        {
                            Content = "OK",
                            HorizontalAlignment =
                                Avalonia.Layout.HorizontalAlignment.Right,
                            MinWidth = 80
                        }
                        }
                    }
            };

        Avalonia.Controls.Button okButton =
            (Avalonia.Controls.Button)
            ((Avalonia.Controls.StackPanel)warningWindow.Content!)
            .Children[1];

        okButton.Click += (_, _) =>
            warningWindow.Close();

        await warningWindow.ShowDialog(this);
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

    /// <summary>
    /// Opens a file picker for selecting the external program executable.
    /// </summary>
    private async void BrowseProgramButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files =
            await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Select external program",
                    AllowMultiple = false
                });

        if (files.Count == 0)
            return;

        IStorageFile file =
            files[0];

        string? localPath =
            file.TryGetLocalPath();

        if (!string.IsNullOrWhiteSpace(localPath))
        {
            ProgramTextBox.Text =
                localPath;
        }
    }

    /// <summary>
    /// Opens a file picker for selecting the input/output file used by the
    /// external program.
    /// </summary>
    private async void BrowseOutputFileButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FilePickerSaveOptions options =
            new()
            {
                Title = "Select input/output file"
            };

        string currentPath =
            OutputFileTextBox.Text ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            options.SuggestedFileName =
                Path.GetFileName(currentPath);
        }

        IStorageFile? file =
            await StorageProvider.SaveFilePickerAsync(options);

        if (file == null)
            return;

        string? localPath =
            file.TryGetLocalPath();

        if (!string.IsNullOrWhiteSpace(localPath))
        {
            OutputFileTextBox.Text =
                localPath;
        }
    }
}