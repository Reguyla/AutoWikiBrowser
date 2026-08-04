namespace AutoWikiBrowser
{
    partial class PluginManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PluginManager));
            lvPlugin = new Twain.Core.Controls.NoFlickerExtendedListView();
            colName = new System.Windows.Forms.ColumnHeader();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            loadPluginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            pluginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            loadNewPluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            label1 = new System.Windows.Forms.Label();
            lblPluginCount = new System.Windows.Forms.Label();
            contextMenuStrip1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // lvPlugin
            // 
            resources.ApplyResources(lvPlugin, "lvPlugin");
            lvPlugin.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { colName });
            lvPlugin.ComparerFactory = lvPlugin;
            lvPlugin.ContextMenuStrip = contextMenuStrip1;
            lvPlugin.Name = "lvPlugin";
            lvPlugin.UseCompatibleStateImageBehavior = false;
            lvPlugin.View = System.Windows.Forms.View.Details;
            // 
            // colName
            // 
            resources.ApplyResources(colName, "colName");
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { loadPluginToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            resources.ApplyResources(contextMenuStrip1, "contextMenuStrip1");
            contextMenuStrip1.Opening += contextMenuStrip1_Opening;
            // 
            // loadPluginToolStripMenuItem
            // 
            loadPluginToolStripMenuItem.Name = "loadPluginToolStripMenuItem";
            resources.ApplyResources(loadPluginToolStripMenuItem, "loadPluginToolStripMenuItem");
            loadPluginToolStripMenuItem.Click += loadPluginToolStripMenuItem_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { pluginToolStripMenuItem });
            resources.ApplyResources(menuStrip1, "menuStrip1");
            menuStrip1.Name = "menuStrip1";
            // 
            // pluginToolStripMenuItem
            // 
            pluginToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { loadNewPluginsToolStripMenuItem });
            pluginToolStripMenuItem.Name = "pluginToolStripMenuItem";
            resources.ApplyResources(pluginToolStripMenuItem, "pluginToolStripMenuItem");
            // 
            // loadNewPluginsToolStripMenuItem
            // 
            loadNewPluginsToolStripMenuItem.Name = "loadNewPluginsToolStripMenuItem";
            resources.ApplyResources(loadNewPluginsToolStripMenuItem, "loadNewPluginsToolStripMenuItem");
            loadNewPluginsToolStripMenuItem.Click += loadNewPluginsToolStripMenuItem_Click;
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // lblPluginCount
            // 
            resources.ApplyResources(lblPluginCount, "lblPluginCount");
            lblPluginCount.Name = "lblPluginCount";
            // 
            // PluginManager
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(menuStrip1);
            Controls.Add(lvPlugin);
            Controls.Add(label1);
            Controls.Add(lblPluginCount);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MainMenuStrip = menuStrip1;
            Name = "PluginManager";
            ShowIcon = false;
            Load += PluginManager_Load;
            contextMenuStrip1.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private Twain.Core.Controls.NoFlickerExtendedListView lvPlugin;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem pluginToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadNewPluginsToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem loadPluginToolStripMenuItem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblPluginCount;
    }
}