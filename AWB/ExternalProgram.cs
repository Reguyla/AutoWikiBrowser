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

using System.ComponentModel;
using System.Windows.Forms;
using WikiFunctions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AutoWikiBrowser;

// TODO (Module Modernization):
// Separate the external process execution logic from the Windows Forms UI.
// This form should eventually become a configuration surface, with execution
// handled by a dedicated service that can be reused by future UI frameworks.
/// <summary>
/// Configures and executes an external program as an AutoWikiBrowser
/// custom processing module.
/// </summary>
/// <remarks>
/// This form implements <see cref="WikiFunctions.Plugin.IModule"/> and allows
/// AWB to pass article text to an external executable for processing.
/// Depending on the selected configuration, article text is provided either
/// through command-line parameters or a temporary input/output file.
/// </remarks>
public partial class ExternalProgram : Form, WikiFunctions.Plugin.IModule
{
    // TODO (UI Modernization):
    // Review whether AWBToolTip should be added to the form's component
    // container or disposed explicitly if it acquires disposable resources.
    private readonly WikiFunctions.Controls.AWBToolTip _toolTip;
    /// <summary>
    /// Initializes the external program configuration dialog.
    /// </summary>
    public ExternalProgram()
    {
        InitializeComponent();

        _toolTip =
            new WikiFunctions.Controls.AWBToolTip();
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
    public WikiFunctions.AWBSettings.ExternalProgramPrefs Settings
    {
        get
        {
            return new WikiFunctions.AWBSettings.ExternalProgramPrefs
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
}

    // Look at User:Pseudomonas/AWBPerlWrapperPlugin
    // TODO (External Program Modernization):
    // Move process execution, argument construction, input/output file handling,
    // and article-result processing into a dedicated service. Keep this form
    // responsible only for collecting settings and displaying validation or
    // execution results.
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
    /// currently returns an empty summary.
    /// </param>
    /// <param name="skip">
    /// Receives <see langword="true"/> when skip-unchanged is enabled and the
    /// external program returns text identical to the original article.
    /// </param>
    /// <returns>
    /// The transformed article text, or the original text if processing fails or
    /// no output file is produced.
    /// </returns>
    public string ProcessArticle(string articleText, string articleTitle, int @namespace, out string summary, out bool skip)
    {
        string origText = articleText;
        skip = false;
        summary = "";

        string ioFile = txtFile.Text;

        try
        {
            // under Wine WaitForExit() does not work and need to use absolute file paths. So under Linux use StandardOutput.ReadToEnd instead
            if (Globals.UsingLinux)
            {
                using (System.Diagnostics.Process p = new System.Diagnostics.Process())
                {
                    p.StartInfo.FileName = txtProgram.Text;
                    p.StartInfo.Arguments = Tools.ApplyKeyWords(articleTitle, txtParameters.Text.Replace("%%file%%", txtFile.Text));
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;

                    if (radFile.Checked)
                        Tools.WriteTextFileAbsolutePath(articleText, ioFile, false);
                    else
                        p.StartInfo.Arguments = p.StartInfo.Arguments.Replace("%%articletext%%", articleText);

                    p.Start();

                    // TODO (External Program Reliability):
                    // Add configurable timeout and cancellation support so an unresponsive
                    // external process cannot block AWB indefinitely. Ensure the process is
                    // terminated safely when the operation is canceled or times out.
                    //
                    // TODO (External Program Compatibility):
                    // Define whether redirected standard output is diagnostic output or the
                    // transformed article text. The Linux path currently reads and logs standard
                    // output but only returns text read from the configured output file.
                    string output = p.StandardOutput.ReadToEnd();

                    p.Close();

                    // pretend to do something with output just to keep compiler happy
                    Tools.WriteDebug("Ext Proc", output);
                }
            }
            else
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    WorkingDirectory = Path.GetDirectoryName(txtProgram.Text),
                    FileName = Path.GetFileName(txtProgram.Text),
                    Arguments = Tools.ApplyKeyWords(articleTitle, txtParameters.Text.Replace("%%file%%", txtFile.Text))
                };

                if (radFile.Checked)
                {
                    if (txtFile.Text.Contains("\\"))
                        Tools.WriteTextFileAbsolutePath(articleText, ioFile, false);
                    else
                        Tools.WriteTextFile(articleText, ioFile, false);
                }
                else
                    // TODO (External Program Modernization):
                    // Replace direct article-text substitution into the command-line string with
                    // standard input, a temporary file, or structured ProcessStartInfo.ArgumentList
                    // handling. Large or quoted article text may exceed command-line limits or be
                    // parsed incorrectly by the target program.
                    psi.Arguments = psi.Arguments.Replace("%%articletext%%", articleText);

                System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);

                p.WaitForExit();
            }

            // TODO (External Program Safety):
            // Track whether AWB created the input/output file during the current operation
            // and delete only files that AWB owns. Avoid deleting a pre-existing
            // user-selected file unintentionally.
            if (File.Exists(ioFile))
            {
                articleText = File.ReadAllText(ioFile);

                skip = (chkSkip.Checked && (articleText == origText));

                File.Delete(ioFile);
            }
            return articleText;
        }
        catch (Exception ex)
        {
            Tools.WriteDebug("Ext Proc", ex.StackTrace);
            // Most, if not all exceptions here are related to user wrong user input
            // or environment specifics, so ErrorHandler is not needed.
            MessageBox.Show(ActiveForm, ex.Message, "External processing error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            return origText;
        }
    }

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

    private void btnOk_Click(object sender, EventArgs e)
    {
        if (!chkEnabled.Checked || !string.IsNullOrEmpty(txtProgram.Text) && !string.IsNullOrEmpty(txtFile.Text) || (radParameter.Checked && !string.IsNullOrEmpty(txtParameters.Text)))
            Close();
        else
            MessageBox.Show("Please make sure all relevant fields are completed");
    }

    private void ExternalProgram_FormClosing(object sender, FormClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void btnSelectProgram_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(openProgram.InitialDirectory))
            openProgram.InitialDirectory = Application.StartupPath;

        if (openProgram.ShowDialog() == DialogResult.OK)
        {
            txtProgram.Text = openProgram.FileName;
        }
    }

    private void btnSelectIO_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(openIO.InitialDirectory))
            openIO.InitialDirectory = Application.StartupPath;

        if (openIO.ShowDialog() == DialogResult.OK)
        {
            txtFile.Text = openIO.FileName;
        }
    }

    private void RadioButtonCheckedChanged(object sender, EventArgs e)
    {
        btnSelectIO.Enabled = txtFile.Enabled = radFile.Checked;
    }
}