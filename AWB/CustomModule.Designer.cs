namespace AutoWikiBrowser
{
    partial class CustomModule
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            mnuTextBox = new System.Windows.Forms.ContextMenuStrip(components);
            menuitemMakeFromTextBoxUndo = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator27 = new System.Windows.Forms.ToolStripSeparator();
            menuitemMakeFromTextBoxCut = new System.Windows.Forms.ToolStripMenuItem();
            menuitemMakeFromTextBoxCopy = new System.Windows.Forms.ToolStripMenuItem();
            menuitemMakeFromTextBoxPaste = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            goToLineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            btnClose = new System.Windows.Forms.Button();
            btnMake = new System.Windows.Forms.Button();
            cmboLang = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            lblStatus = new System.Windows.Forms.Label();
            chkModuleEnabled = new System.Windows.Forms.CheckBox();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showOnlyCodeBoxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            guideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            manualToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            chkFixedwidth = new System.Windows.Forms.CheckBox();
            lblBuilt = new System.Windows.Forms.Label();
            panel1 = new System.Windows.Forms.Panel();
            lblStart = new System.Windows.Forms.Label();
            txtCode = new System.Windows.Forms.TextBox();
            lblEnd = new System.Windows.Forms.Label();
            mnuTextBox.SuspendLayout();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // mnuTextBox
            // 
            mnuTextBox.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuitemMakeFromTextBoxUndo, toolStripSeparator27, menuitemMakeFromTextBoxCut, menuitemMakeFromTextBoxCopy, menuitemMakeFromTextBoxPaste, toolStripSeparator1, selectAllToolStripMenuItem, toolStripSeparator2, goToLineToolStripMenuItem });
            mnuTextBox.Name = "mnuMakeFromTextBox";
            mnuTextBox.Size = new System.Drawing.Size(165, 154);
            // 
            // menuitemMakeFromTextBoxUndo
            // 
            menuitemMakeFromTextBoxUndo.Name = "menuitemMakeFromTextBoxUndo";
            menuitemMakeFromTextBoxUndo.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z;
            menuitemMakeFromTextBoxUndo.Size = new System.Drawing.Size(164, 22);
            menuitemMakeFromTextBoxUndo.Text = "&Undo";
            menuitemMakeFromTextBoxUndo.Click += menuitemMakeFromTextBoxUndo_Click;
            // 
            // toolStripSeparator27
            // 
            toolStripSeparator27.Name = "toolStripSeparator27";
            toolStripSeparator27.Size = new System.Drawing.Size(161, 6);
            // 
            // menuitemMakeFromTextBoxCut
            // 
            menuitemMakeFromTextBoxCut.Name = "menuitemMakeFromTextBoxCut";
            menuitemMakeFromTextBoxCut.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X;
            menuitemMakeFromTextBoxCut.Size = new System.Drawing.Size(164, 22);
            menuitemMakeFromTextBoxCut.Text = "Cu&t";
            menuitemMakeFromTextBoxCut.Click += menuitemMakeFromTextBoxCut_Click;
            // 
            // menuitemMakeFromTextBoxCopy
            // 
            menuitemMakeFromTextBoxCopy.Name = "menuitemMakeFromTextBoxCopy";
            menuitemMakeFromTextBoxCopy.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C;
            menuitemMakeFromTextBoxCopy.Size = new System.Drawing.Size(164, 22);
            menuitemMakeFromTextBoxCopy.Text = "&Copy";
            menuitemMakeFromTextBoxCopy.Click += menuitemMakeFromTextBoxCopy_Click;
            // 
            // menuitemMakeFromTextBoxPaste
            // 
            menuitemMakeFromTextBoxPaste.Name = "menuitemMakeFromTextBoxPaste";
            menuitemMakeFromTextBoxPaste.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V;
            menuitemMakeFromTextBoxPaste.Size = new System.Drawing.Size(164, 22);
            menuitemMakeFromTextBoxPaste.Text = "&Paste";
            menuitemMakeFromTextBoxPaste.Click += menuitemMakeFromTextBoxPaste_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(161, 6);
            // 
            // selectAllToolStripMenuItem
            // 
            selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            selectAllToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A;
            selectAllToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            selectAllToolStripMenuItem.Text = "&Select All";
            selectAllToolStripMenuItem.Click += selectAllToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(161, 6);
            // 
            // goToLineToolStripMenuItem
            // 
            goToLineToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripTextBox1 });
            goToLineToolStripMenuItem.Name = "goToLineToolStripMenuItem";
            goToLineToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            goToLineToolStripMenuItem.Text = "&Go to Line";
            // 
            // toolStripTextBox1
            // 
            toolStripTextBox1.MaxLength = 6;
            toolStripTextBox1.Name = "toolStripTextBox1";
            toolStripTextBox1.Size = new System.Drawing.Size(100, 23);
            toolStripTextBox1.Text = "Enter line number";
            toolStripTextBox1.KeyPress += toolStripTextBox1_KeyPress;
            toolStripTextBox1.Click += toolStripTextBox1_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnClose.Location = new System.Drawing.Point(673, 35);
            btnClose.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(88, 27);
            btnClose.TabIndex = 8;
            btnClose.Text = "&Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnMake
            // 
            btnMake.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnMake.Enabled = false;
            btnMake.Location = new System.Drawing.Point(565, 35);
            btnMake.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnMake.Name = "btnMake";
            btnMake.Size = new System.Drawing.Size(102, 27);
            btnMake.TabIndex = 7;
            btnMake.Text = "&Make module";
            btnMake.UseVisualStyleBackColor = true;
            btnMake.Click += btnMake_Click;
            // 
            // cmboLang
            // 
            cmboLang.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            cmboLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmboLang.FormattingEnabled = true;
            cmboLang.Location = new System.Drawing.Point(398, 37);
            cmboLang.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            cmboLang.Name = "cmboLang";
            cmboLang.Size = new System.Drawing.Size(159, 23);
            cmboLang.TabIndex = 5;
            cmboLang.SelectedIndexChanged += cmboLang_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(327, 40);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(59, 15);
            label1.TabIndex = 4;
            label1.Text = "&Language";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.BackColor = System.Drawing.Color.FromArgb(255, 128, 0);
            lblStatus.Location = new System.Drawing.Point(97, 40);
            lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(106, 15);
            lblStatus.TabIndex = 2;
            lblStatus.Text = "No module loaded";
            // 
            // chkModuleEnabled
            // 
            chkModuleEnabled.AutoSize = true;
            chkModuleEnabled.Location = new System.Drawing.Point(14, 39);
            chkModuleEnabled.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            chkModuleEnabled.Name = "chkModuleEnabled";
            chkModuleEnabled.Size = new System.Drawing.Size(68, 19);
            chkModuleEnabled.TabIndex = 1;
            chkModuleEnabled.Text = "&Enabled";
            chkModuleEnabled.UseVisualStyleBackColor = true;
            chkModuleEnabled.CheckedChanged += chkModuleEnabled_CheckedChanged;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { viewToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            menuStrip1.Size = new System.Drawing.Size(775, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { showOnlyCodeBoxToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            // 
            // showOnlyCodeBoxToolStripMenuItem
            // 
            showOnlyCodeBoxToolStripMenuItem.CheckOnClick = true;
            showOnlyCodeBoxToolStripMenuItem.Name = "showOnlyCodeBoxToolStripMenuItem";
            showOnlyCodeBoxToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F11;
            showOnlyCodeBoxToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            showOnlyCodeBoxToolStripMenuItem.Text = "Show only code box";
            showOnlyCodeBoxToolStripMenuItem.CheckedChanged += showOnlyCodeBoxToolStripMenuItem_CheckedChanged;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { guideToolStripMenuItem, manualToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // guideToolStripMenuItem
            // 
            guideToolStripMenuItem.Name = "guideToolStripMenuItem";
            guideToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            guideToolStripMenuItem.Text = "Quick &guide";
            guideToolStripMenuItem.Click += guideToolStripMenuItem_Click;
            // 
            // manualToolStripMenuItem
            // 
            manualToolStripMenuItem.Name = "manualToolStripMenuItem";
            manualToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            manualToolStripMenuItem.Text = "Manual";
            manualToolStripMenuItem.Click += manualToolStripMenuItem_Click;
            // 
            // chkFixedwidth
            // 
            chkFixedwidth.AutoSize = true;
            chkFixedwidth.Checked = true;
            chkFixedwidth.CheckState = System.Windows.Forms.CheckState.Checked;
            chkFixedwidth.Location = new System.Drawing.Point(14, 65);
            chkFixedwidth.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            chkFixedwidth.Name = "chkFixedwidth";
            chkFixedwidth.Size = new System.Drawing.Size(111, 19);
            chkFixedwidth.TabIndex = 3;
            chkFixedwidth.Text = "&Fixed width font";
            chkFixedwidth.UseVisualStyleBackColor = true;
            chkFixedwidth.CheckedChanged += chkFixedwidth_CheckedChanged;
            // 
            // lblBuilt
            // 
            lblBuilt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblBuilt.AutoSize = true;
            lblBuilt.Location = new System.Drawing.Point(327, 66);
            lblBuilt.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblBuilt.Name = "lblBuilt";
            lblBuilt.Size = new System.Drawing.Size(159, 15);
            lblBuilt.TabIndex = 6;
            lblBuilt.Text = "Custom Module Built At: n/a";
            // 
            // panel1
            // 
            panel1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            panel1.Controls.Add(lblStart);
            panel1.Controls.Add(txtCode);
            panel1.Controls.Add(lblEnd);
            panel1.Location = new System.Drawing.Point(14, 91);
            panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(747, 522);
            panel1.TabIndex = 12;
            // 
            // lblStart
            // 
            lblStart.Dock = System.Windows.Forms.DockStyle.Top;
            lblStart.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblStart.Location = new System.Drawing.Point(0, 0);
            lblStart.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblStart.Name = "lblStart";
            lblStart.Size = new System.Drawing.Size(747, 292);
            lblStart.TabIndex = 9;
            lblStart.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // txtCode
            // 
            txtCode.AcceptsTab = true;
            txtCode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtCode.ContextMenuStrip = mnuTextBox;
            txtCode.Font = new System.Drawing.Font("Courier New", 9F);
            txtCode.Location = new System.Drawing.Point(0, 295);
            txtCode.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtCode.MaxLength = 0;
            txtCode.Multiline = true;
            txtCode.Name = "txtCode";
            txtCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            txtCode.Size = new System.Drawing.Size(742, 184);
            txtCode.TabIndex = 10;
            txtCode.TabStop = false;
            txtCode.WordWrap = false;
            // 
            // lblEnd
            // 
            lblEnd.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lblEnd.Font = new System.Drawing.Font("Courier New", 9F);
            lblEnd.Location = new System.Drawing.Point(0, 483);
            lblEnd.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblEnd.Name = "lblEnd";
            lblEnd.Size = new System.Drawing.Size(743, 45);
            lblEnd.TabIndex = 11;
            // 
            // CustomModule
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(775, 627);
            Controls.Add(panel1);
            Controls.Add(lblBuilt);
            Controls.Add(chkFixedwidth);
            Controls.Add(chkModuleEnabled);
            Controls.Add(lblStatus);
            Controls.Add(label1);
            Controls.Add(cmboLang);
            Controls.Add(btnMake);
            Controls.Add(btnClose);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MinimumSize = new System.Drawing.Size(791, 664);
            Name = "CustomModule";
            ShowIcon = false;
            SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            Text = "Module";
            FormClosing += CustomModule_FormClosing;
            mnuTextBox.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnMake;
        private System.Windows.Forms.ComboBox cmboLang;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.CheckBox chkModuleEnabled;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem guideToolStripMenuItem;
        private System.Windows.Forms.CheckBox chkFixedwidth;
        private System.Windows.Forms.Label lblBuilt;
        private System.Windows.Forms.ContextMenuStrip mnuTextBox;
        private System.Windows.Forms.ToolStripMenuItem menuitemMakeFromTextBoxUndo;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator27;
        private System.Windows.Forms.ToolStripMenuItem menuitemMakeFromTextBoxCut;
        private System.Windows.Forms.ToolStripMenuItem menuitemMakeFromTextBoxCopy;
        private System.Windows.Forms.ToolStripMenuItem menuitemMakeFromTextBoxPaste;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblStart;
        private System.Windows.Forms.TextBox txtCode;
        private System.Windows.Forms.Label lblEnd;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showOnlyCodeBoxToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem goToLineToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
        private System.Windows.Forms.ToolStripMenuItem manualToolStripMenuItem;
    }
}
