/*
Copyright (C) 2008 Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using Twain.Core;
using Twain.Core.ExternalPrograms;
using ExternalProgramPrefs = Twain.Core.AWBSettings.ExternalProgramPrefs;

namespace AutoWikiBrowser;

/// <summary>
/// Configures and executes an external program as an AutoWikiBrowser
/// custom processing module.
/// </summary>
/// <remarks>
/// This form implements <see cref="Twain.Core.Plugin.IModule"/> and allows
/// AWB to pass article text to an external executable for processing.
/// Depending on the selected configuration, article text is provided either
/// through command-line parameters or a temporary input/output file.
/// </remarks>
public partial class ExternalProgram : Form, Twain.Core.Plugin.IModule
{
    // TODO (UI Modernization):
    // Review whether AWBToolTip should be added to the form's component
    // container or disposed explicitly if it acquires disposable resources.
    private readonly Twain.Core.Controls.AWBToolTip _toolTip;

    /// <summary>
    /// Initializes the external program configuration dialog.
    /// </summary>
    public ExternalProgram()
    {
        InitializeComponent();

        _toolTip =
            new Twain.Core.Controls.AWBToolTip();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the external program module is enabled.
    /// </summary>
    /// <remarks>
    /// This property exposes the state of the internal enabled checkbox for
    /// runtime use. It is not a design-time property and must not be serialized
    /// by the Windows Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ModuleEnabled
    {
        get => chkEnabled.Checked;
        set => chkEnabled.Checked = value;
    }

    /// <summary>
    /// Gets or sets the settings for the external program module.
    /// </summary>
    /// <remarks>
    /// This property builds or applies the runtime external-program settings from
    /// the form's child controls. It is managed by AWB's settings system and must
    /// not be serialized independently by the Windows Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ExternalProgramPrefs Settings
    {
        get
        {
            return new ExternalProgramPrefs
            {
                Enabled = chkEnabled.Checked,
                Skip = chkSkip.Checked,
                Program = txtProgram.Text,
                Parameters = txtParameters.Text,
                PassAsFile = radFile.Checked,
                OutputFile = txtFile.Text
            };
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            chkEnabled.Checked = value.Enabled;
            chkSkip.Checked = value.Skip;
            txtProgram.Text = value.Program;
            txtParameters.Text = value.Parameters;

            radFile.Checked = value.PassAsFile;
            radParameter.Checked = !value.PassAsFile;
            txtFile.Text = value.OutputFile;
        }
    }

    /// <summary>
    /// Updates the enabled state of the external program configuration controls.
    /// </summary>
    /// <remarks>
    /// When the module is disabled, all execution options are disabled to prevent
    /// editing settings that are not currently in use.
    /// </remarks>
    private void UpdateEnabledState()
    {
        groupBox1.Enabled =
            chkSkip.Enabled =
            chkEnabled.Checked;
    }

    /// <summary>
    /// Updates the dialog when the module enabled state changes.
    /// </summary>
    /// <param name="sender">
    /// The checkbox that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the checked-state change.
    /// </param>
    private void chkEnabled_CheckedChanged(
        object sender,
        EventArgs e)
    {
        UpdateEnabledState();
        UpdateInputModeState();
    }

    /// <summary>
    /// Processes article text using the configured external program.
    /// </summary>
    /// <param name="articleText">
    /// The current article text.
    /// </param>
    /// <param name="articleTitle">
    /// The title of the article being processed.
    /// </param>
    /// <param name="namespace">
    /// The namespace identifier of the article. This implementation does not
    /// currently use the value.
    /// </param>
    /// <param name="summary">
    /// Receives the edit summary produced by the module. This implementation
    /// returns an empty summary.
    /// </param>
    /// <param name="skip">
    /// Receives <see langword="true"/> when skip-unchanged is enabled and the
    /// external program returns text identical to the original article.
    /// </param>
    /// <returns>
    /// The transformed article text, or the original text when external processing
    /// fails or produces no output file.
    /// </returns>
    public string ProcessArticle(
        string articleText,
        string articleTitle,
        int @namespace,
        out string summary,
        out bool skip)
    {
        summary = string.Empty;
        skip = false;

        ExternalProgramOptions options =
            CreateExecutionOptions();

        try
        {
            ExternalProgramResult result =
                ExternalProgramRunner.ProcessArticle(
                    articleText,
                    articleTitle,
                    options);

            skip = result.Skip;

            return result.ArticleText;
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                "Ext Proc",
                ex.ToString());

            // Most failures here result from invalid user configuration or
            // environment-specific conditions, so the general ErrorHandler is not
            // invoked.
            MessageBox.Show(
                this,
                ex.Message,
                "External processing error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return articleText;
        }
    }

    /// <summary>
    /// Creates an execution-settings snapshot from the current dialog controls.
    /// </summary>
    /// <returns>
    /// The settings required to execute the configured external program.
    /// </returns>
    private ExternalProgramOptions CreateExecutionOptions() =>
        new()
        {
            ProgramPath = txtProgram.Text,
            Parameters = txtParameters.Text,
            PassAsFile = radFile.Checked,
            OutputFile = txtFile.Text,
            SkipUnchanged = chkSkip.Checked
        };

    /// <summary>
    /// Initializes the enabled state and tooltip text for the external program
    /// configuration controls.
    /// </summary>
    /// <param name="sender">
    /// The form that raised the load event.
    /// </param>
    /// <param name="e">
    /// Event data for the form load operation.
    /// </param>
    private void ExternalProgram_Load(
        object sender,
        EventArgs e)
    {
        UpdateEnabledState();

        const string parameterTooltip =
            "Use \"%%articletext%%\" to pass the current article text, or " +
            "\"%%file%%\" to pass the configured input/output file path.";

        _toolTip.SetToolTip(
            txtParameters,
            parameterTooltip);

        _toolTip.SetToolTip(
            radParameter,
            parameterTooltip);

        const string fileTooltip =
            "This is the file AWB writes when file mode is selected and reads " +
            "again after the external program finishes.";

        _toolTip.SetToolTip(
            txtFile,
            fileTooltip);

        _toolTip.SetToolTip(
            label4,
            fileTooltip);
    }

    /// <summary>
    /// Validates the external program configuration before closing the dialog.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the click event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnOk_Click(
        object sender,
        EventArgs e)
    {
        if (HasValidSettings())
        {
            Close();
            return;
        }

        MessageBox.Show(
            this,
            "Please make sure all relevant fields are completed.",
            "Incomplete external program settings",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Determines whether the current external program settings are complete.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the current configuration contains all
    /// required values; otherwise, <see langword="false"/>.
    /// </returns>
    private bool HasValidSettings()
    {
        if (!chkEnabled.Checked)
            return true;

        if (string.IsNullOrWhiteSpace(txtProgram.Text))
            return false;

        if (radFile.Checked)
        {
            return !string.IsNullOrWhiteSpace(
                txtFile.Text);
        }

        return !string.IsNullOrWhiteSpace(
            txtParameters.Text);
    }

    /// <summary>
    /// Hides the dialog instead of disposing it when the user closes the window.
    /// </summary>
    /// <param name="sender">
    /// The form that raised the closing event.
    /// </param>
    /// <param name="e">
    /// Event data that allows the close operation to be canceled.
    /// </param>
    /// <remarks>
    /// The dialog remains in memory so its configuration can be reused without
    /// recreating the form.
    /// </remarks>
    private void ExternalProgram_FormClosing(
        object sender,
        FormClosingEventArgs e)
    {
        // TODO (UI Modernization):
        // Review this lifecycle when the module configuration is converted to a
        // dockable panel or other reusable UI component.
        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// Allows the user to browse for the external program executable.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the click event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnSelectProgram_Click(
        object sender,
        EventArgs e)
    {
        InitializeDialogDirectory(openProgram);

        if (openProgram.ShowDialog(this) == DialogResult.OK)
        {
            txtProgram.Text =
                openProgram.FileName;
        }
    }

    /// <summary>
    /// Initializes the file dialog to the application directory when no initial
    /// directory has been configured.
    /// </summary>
    /// <param name="dialog">
    /// The file dialog to initialize.
    /// </param>
    private static void InitializeDialogDirectory(
        FileDialog dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        if (string.IsNullOrEmpty(dialog.InitialDirectory))
        {
            dialog.InitialDirectory =
                Application.StartupPath;
        }
    }

    /// <summary>
    /// Allows the user to select the input/output file used by the external
    /// program.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the click event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnSelectIO_Click(
        object sender,
        EventArgs e)
    {
        InitializeDialogDirectory(openIO);

        if (openIO.ShowDialog(this) == DialogResult.OK)
        {
            txtFile.Text =
                openIO.FileName;
        }
    }

    /// <summary>
    /// Updates the controls associated with file-based article processing.
    /// </summary>
    private void UpdateInputModeState()
    {
        btnSelectIO.Enabled =
            txtFile.Enabled =
            radFile.Checked;
    }

    /// <summary>
    /// Updates the dialog when the selected external-program input mode changes.
    /// </summary>
    /// <param name="sender">
    /// The radio button that raised the checked-state change event.
    /// </param>
    /// <param name="e">
    /// Event data for the checked-state change.
    /// </param>
    private void RadioButtonCheckedChanged(
        object sender,
        EventArgs e)
    {
        UpdateInputModeState();
    }
}