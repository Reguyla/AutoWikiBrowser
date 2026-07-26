/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

using System.CodeDom.Compiler;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using WikiFunctions;
using WikiFunctions.CustomModules;
using WikiFunctions.Plugin;

namespace AutoWikiBrowser;

internal sealed partial class CustomModule : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomModule"/> form and
    /// loads the available custom module compilers.
    /// </summary>
    public CustomModule()
    {
        InitializeComponent();

        cmboLang.Items.Clear();
        cmboLang.Items.AddRange(CustomModuleCompiler.GetList());
        cmboLang.SelectedIndex = 0;
        txtCode.Text = _codeExample;
    }

    /// <summary>
    /// Gets or sets the custom module source code entered by the user.
    /// Consecutive Windows-style line breaks are reduced when the code is
    /// assigned.
    /// </summary>
    public string Code
    {
        get => txtCode.Text;
        set => txtCode.Text = value.Replace("\r\n\r\n", "\r\n");
    }

    /// <summary>
    /// Gets or sets the programming language used for the custom module.
    /// </summary>
    /// <remarks>
    /// Older settings that do not contain a recognized language name default
    /// to the first compiler, historically the C# compiler.
    /// </remarks>
    public string Language
    {
        get => cmboLang.SelectedItem!.ToString()!;

        set
        {
            foreach (CustomModuleCompiler compiler in cmboLang.Items)
            {
                if (!compiler.CanHandleLanguage(value))
                {
                    continue;
                }

                cmboLang.SelectedItem = compiler;
                return;
            }

            // Older configurations that stored an index rather than a
            // language name could only have selected C#.
            cmboLang.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Gets the compiler responsible for compiling the currently selected
    /// custom module language.
    /// </summary>
    public CustomModuleCompiler Compiler =>
        (CustomModuleCompiler)cmboLang.SelectedItem!;

    /// <summary>
    /// Gets or sets a value indicating whether the custom module is enabled.
    /// Enabling the module automatically attempts to compile and load it.
    /// </summary>
    public bool ModuleEnabled
    {
        get => chkModuleEnabled.Checked;

        set
        {
            chkModuleEnabled.Checked = value;

            if (value)
            {
                MakeModule();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the custom module is enabled and has been
    /// successfully compiled and loaded.
    /// </summary>
    public bool ModuleUsable =>
        ModuleEnabled && Module is not null;

    private const string BuiltPrefix = "Custom Module Built At: ";

    private IModule? _m;

    /// <summary>
    /// Gets the currently loaded custom module instance, or
    /// <see langword="null"/> if no module has been successfully compiled.
    /// </summary>
    public IModule? Module
    {
        get => _m;

        private set
        {
            _m = value;

            if (value is null)
            {
                lblStatus.Text = "No module loaded";
                lblStatus.BackColor = Color.Orange;
                lblBuilt.Text = BuiltPrefix + "n/a";
                return;
            }

            lblStatus.Text = "Module compiled and loaded";
            lblStatus.BackColor = Color.LightGreen;
            lblBuilt.Text = BuiltPrefix + DateTime.Now;
        }
    }

    private string _codeStart = "";
    private string _codeEnd = "";
    private string _codeExample = "";

    /// <summary>
    /// Provides the user interface for creating, compiling, and managing
    /// AutoWikiBrowser custom modules.
    /// </summary>
    public void MakeModule()
    {
        try
        {
            CompilerParameters parameters =
                new()
                {
                    GenerateExecutable = false,
                    IncludeDebugInformation = false
                };

            AddLoadedAssemblyReferences(parameters);

            CompilerResults results =
                Compiler.Compile(
                    txtCode.Text,
                    parameters);

            if (!ShowCompilationMessages(results))
            {
                Module = null;
                return;
            }

            Assembly compiledAssembly =
                results.CompiledAssembly
                ?? throw new InvalidOperationException(
                    "The compiler did not return a compiled assembly.");

            Type moduleType =
                compiledAssembly
                    .GetTypes()
                    .FirstOrDefault(
                        type =>
                            !type.IsAbstract &&
                            typeof(IModule).IsAssignableFrom(type))
                ?? throw new InvalidOperationException(
                    "The compiled assembly does not contain an IModule implementation.");

            Module =
                Activator.CreateInstance(
                    moduleType,
                    Program.AWB) as IModule
                ?? throw new InvalidOperationException(
                    $"Unable to instantiate custom module type '{moduleType.FullName}'.");
        }
        catch (Exception ex)
        {
            Module = null;
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Adds references for assemblies currently loaded by the application
    /// so custom modules can use AWB and framework types during compilation.
    /// </summary>
    /// <param name="parameters">
    /// The compiler parameters that receive the assembly references.
    /// </param>
    private static void AddLoadedAssemblyReferences(
        CompilerParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        HashSet<string> referencePaths =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (Assembly assembly in
                 AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            if (assembly.FullName?.Contains(
                    "Microsoft.GeneratedCode",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            string location;

            try
            {
                location = assembly.Location;
            }
            catch (NotSupportedException)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(location) ||
                !File.Exists(location))
            {
                continue;
            }

            if (referencePaths.Add(location))
            {
                parameters.ReferencedAssemblies.Add(location);
            }
        }
    }

    /// <summary>
    /// Displays compiler warnings and errors returned while building
    /// a custom module.
    /// </summary>
    /// <param name="results">
    /// The results returned by the selected custom module compiler.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when compilation may continue; otherwise,
    /// <see langword="false"/> when one or more errors prevent the module
    /// from being loaded.
    /// </returns>
    private bool ShowCompilationMessages(
        CompilerResults results)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Errors.Count == 0)
        {
            return true;
        }

        bool hasErrors = false;
        StringBuilder builder = new();

        foreach (CompilerError error in results.Errors)
        {
            hasErrors |= !error.IsWarning;

            if (error.Line > 0)
            {
                builder.AppendFormat(
                    "Line {0}, col {1}: ",
                    error.Line,
                    error.Column);
            }

            if (!string.IsNullOrEmpty(error.ErrorNumber))
            {
                builder.AppendFormat(
                    "[{0}] ",
                    error.ErrorNumber);
            }

            builder.AppendLine(error.ErrorText);
        }

        using CustomModuleErrors errorDialog =
            new()
            {
                ErrorText = builder.ToString(),
                Text = hasErrors
                    ? "Compilation errors"
                    : "Compilation warnings"
            };

        errorDialog.ShowDialog(this);

        return !hasErrors;
    }

    /// <summary>
    /// Clears the currently loaded custom module and updates the module status.
    /// </summary>
    public void SetModuleNotBuilt()
    {
        Module = null;
    }

    /// <summary>
    /// Closes the custom module window.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void btnClose_Click(object sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Hides the custom module window instead of disposing it when the user
    /// attempts to close it.
    /// </summary>
    /// <param name="sender">The form that raised the event.</param>
    /// <param name="e">The form-closing event data.</param>
    private void CustomModule_FormClosing(object sender, FormClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// Compiles and loads the current custom module source code.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void btnMake_Click(object sender, EventArgs e)
    {
        MakeModule();
    }

    /// <summary>
    /// Updates the custom module code template when the selected language
    /// changes.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void cmboLang_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        CustomModuleCompiler compiler = Compiler;

        _codeStart = compiler.CodeStart;
        _codeExample = compiler.CodeExample;
        _codeEnd = compiler.CodeEnd;

        lblStart.Text = _codeStart;
        txtCode.Text = _codeExample;
        lblEnd.Text = _codeEnd;
    }

    // TODO: Review and update this guide to reflect the Roslyn-based compiler
    // and any changes to the supported custom module workflow.
    /// <summary>
    /// Displays guidance for creating and using custom modules.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void guideToolStripMenuItem_Click(object sender, EventArgs e)
    {
        MessageBox.Show(this, @"A module allows you to process the article text using your own .NET code.

Use the ""Make module"" button to compile and load the code.

The method ""ProcessArticle"" is called when AWB is applying all its own processes. Do not change the signature of this method.

The int value ""Namespace"" gives you the key of the namespace, e.g. mainspace is 0 etc., the string ""Summary"" must be set to the message to append to the summary (or can be an empty string), the bool ""Skip"" must be set whether to skip the article or not.

For more detailed information, click Help -> Manual on the Custom Module window.",
            "Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Enables or disables the Make Module button when the custom module
    /// enabled state changes.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void chkModuleEnabled_CheckedChanged(object sender, EventArgs e)
    {
        btnMake.Enabled = chkModuleEnabled.Checked;
    }

    /// <summary>
    /// Switches the code editor and template display between proportional and
    /// fixed-width fonts.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void chkFixedwidth_CheckedChanged(object sender, EventArgs e)
    {
        // TODO: Cache the fixed-width and proportional Font instances instead
        // of creating new Font objects each time the option changes. Reusing
        // the fonts avoids unnecessary GDI object allocations and simplifies
        // resource lifetime management.

        txtCode.Font =
            lblStart.Font =
                lblEnd.Font =
                    chkFixedwidth.Checked
                        ? new Font("Courier New", 9)
                        : new Font("Microsoft Sans Serif", 8);
    }

    #region txtCode Context Menu

    /// <summary>
    /// Undoes the last edit in the custom module editor.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void menuitemMakeFromTextBoxUndo_Click(object sender, EventArgs e)
    {
        txtCode.Undo();
    }

    /// <summary>
    /// Cuts the selected text from the custom module editor.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void menuitemMakeFromTextBoxCut_Click(object sender, EventArgs e)
    {
        txtCode.Cut();
    }

    /// <summary>
    /// Copies the selected text from the custom module editor.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void menuitemMakeFromTextBoxCopy_Click(object sender, EventArgs e)
    {
        txtCode.Copy();
    }

    /// <summary>
    /// Pastes the clipboard contents into the custom module editor.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void menuitemMakeFromTextBoxPaste_Click(
        object sender,
        EventArgs e)
    {
        txtCode.Paste();
    }

    /// <summary>
    /// Selects all text in the custom module editor.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void selectAllToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        txtCode.SelectAll();
    }

    #endregion

    private Point _oldPosition;
    private Size _oldSize;

    /// <summary>
    /// Expands the code editor to fill the form or restores its previous position
    /// and size.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void showOnlyCodeBoxToolStripMenuItem_CheckedChanged(
        object sender,
        EventArgs e)
    {
        bool showOnlyCode =
            showOnlyCodeBoxToolStripMenuItem.Checked;

        lblStart.Visible = !showOnlyCode;
        lblEnd.Visible = !showOnlyCode;

        if (showOnlyCode)
        {
            // Remember the current editor bounds before filling the form.
            _oldPosition = txtCode.Location;
            _oldSize = txtCode.Size;
            txtCode.Dock = DockStyle.Fill;
            return;
        }

        txtCode.Dock = DockStyle.None;
        txtCode.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right |
            AnchorStyles.Bottom;

        // Restore the previous bounds because the editor does not resize itself
        // correctly after docking is removed.
        txtCode.Location = _oldPosition;
        txtCode.Size = _oldSize;
    }

    // TODO: Consider clearing the placeholder only when the control first
    // receives focus so clicking an existing value does not erase user input.
    /// <summary>
    /// Clears the current line-number entry when the go-to-line box is selected.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void toolStripTextBox1_Click(
        object sender,
        EventArgs e)
    {
        toolStripTextBox1.Text = "";
    }

    /// <summary>
    /// Restricts the go-to-line entry to decimal digits and navigates to the
    /// entered line when Enter is pressed.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The key-press event data.</param>
    private void toolStripTextBox1_KeyPress(
        object sender,
        KeyPressEventArgs e)
    {
        if (e.KeyChar == '\r')
        {
            e.Handled = true;

            if (int.TryParse(
                    toolStripTextBox1.Text,
                    out int lineNumber))
            {
                txtCode.GoToLine(lineNumber);
                mnuTextBox.Hide();
            }

            return;
        }

        if (!char.IsDigit(e.KeyChar) &&
            e.KeyChar != '\b')
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Opens the AutoWikiBrowser custom modules manual in the user's default
    /// web browser.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void manualToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        Tools.OpenENArticleInBrowser(
            "Wikipedia:AutoWikiBrowser/Custom_Modules",
            false);
    }
}