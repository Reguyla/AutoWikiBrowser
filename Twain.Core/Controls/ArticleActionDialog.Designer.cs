namespace Twain.Core.Controls
{
    partial class ArticleActionDialog
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
            txtNewTitle = new System.Windows.Forms.TextBox();
            lblNewTitle = new System.Windows.Forms.Label();
            lblSummary = new System.Windows.Forms.Label();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            cmboSummary = new System.Windows.Forms.ComboBox();
            lblExpiry = new System.Windows.Forms.Label();
            txtExpiry = new System.Windows.Forms.TextBox();
            chkAutoProtect = new System.Windows.Forms.CheckBox();
            chkCascadingProtection = new System.Windows.Forms.CheckBox();
            chkNoRedirect = new System.Windows.Forms.CheckBox();
            toolTip = new AWBToolTip(components);
            chkWatch = new System.Windows.Forms.CheckBox();
            MoveDelete = new EditProtectControl();
            chkDealWithAssoc = new System.Windows.Forms.CheckBox();
            SuspendLayout();
            // 
            // txtNewTitle
            // 
            txtNewTitle.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtNewTitle.Location = new System.Drawing.Point(62, 12);
            txtNewTitle.Name = "txtNewTitle";
            txtNewTitle.Size = new System.Drawing.Size(349, 23);
            txtNewTitle.TabIndex = 1;
            // 
            // lblNewTitle
            // 
            lblNewTitle.AutoSize = true;
            lblNewTitle.Location = new System.Drawing.Point(8, 15);
            lblNewTitle.Name = "lblNewTitle";
            lblNewTitle.Size = new System.Drawing.Size(57, 15);
            lblNewTitle.TabIndex = 0;
            lblNewTitle.Text = "&New title:";
            // 
            // lblSummary
            // 
            lblSummary.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblSummary.AutoSize = true;
            lblSummary.Location = new System.Drawing.Point(9, 41);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new System.Drawing.Size(58, 15);
            lblSummary.TabIndex = 4;
            lblSummary.Text = "Summary";
            // 
            // btnOk
            // 
            btnOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOk.Location = new System.Drawing.Point(137, 182);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(75, 23);
            btnOk.TabIndex = 11;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(218, 182);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 23);
            btnCancel.TabIndex = 12;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // cmboSummary
            // 
            cmboSummary.FormattingEnabled = true;
            cmboSummary.Location = new System.Drawing.Point(62, 38);
            cmboSummary.Name = "cmboSummary";
            cmboSummary.Size = new System.Drawing.Size(349, 23);
            cmboSummary.TabIndex = 5;
            // 
            // lblExpiry
            // 
            lblExpiry.AutoSize = true;
            lblExpiry.Location = new System.Drawing.Point(8, 41);
            lblExpiry.Name = "lblExpiry";
            lblExpiry.Size = new System.Drawing.Size(41, 15);
            lblExpiry.TabIndex = 2;
            lblExpiry.Text = "&Expiry:";
            // 
            // txtExpiry
            // 
            txtExpiry.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtExpiry.Location = new System.Drawing.Point(62, 38);
            txtExpiry.Name = "txtExpiry";
            txtExpiry.Size = new System.Drawing.Size(349, 23);
            txtExpiry.TabIndex = 3;
            // 
            // chkAutoProtect
            // 
            chkAutoProtect.AutoSize = true;
            chkAutoProtect.Location = new System.Drawing.Point(148, 162);
            chkAutoProtect.Name = "chkAutoProtect";
            chkAutoProtect.Size = new System.Drawing.Size(110, 19);
            chkAutoProtect.TabIndex = 8;
            chkAutoProtect.Text = "Auto &Protect All";
            chkAutoProtect.UseVisualStyleBackColor = true;
            chkAutoProtect.Visible = false;
            // 
            // chkCascadingProtection
            // 
            chkCascadingProtection.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            chkCascadingProtection.AutoSize = true;
            chkCascadingProtection.Enabled = false;
            chkCascadingProtection.Location = new System.Drawing.Point(23, 160);
            chkCascadingProtection.Name = "chkCascadingProtection";
            chkCascadingProtection.Size = new System.Drawing.Size(139, 19);
            chkCascadingProtection.TabIndex = 7;
            chkCascadingProtection.Text = "&Cascading protection";
            chkCascadingProtection.UseVisualStyleBackColor = true;
            // 
            // chkNoRedirect
            // 
            chkNoRedirect.AutoSize = true;
            chkNoRedirect.Location = new System.Drawing.Point(253, 162);
            chkNoRedirect.Name = "chkNoRedirect";
            chkNoRedirect.Size = new System.Drawing.Size(88, 19);
            chkNoRedirect.TabIndex = 9;
            chkNoRedirect.Text = "&No Redirect";
            chkNoRedirect.UseVisualStyleBackColor = true;
            chkNoRedirect.Visible = false;
            // 
            // chkWatch
            // 
            chkWatch.AutoSize = true;
            chkWatch.Location = new System.Drawing.Point(342, 162);
            chkWatch.Name = "chkWatch";
            chkWatch.Size = new System.Drawing.Size(60, 19);
            chkWatch.TabIndex = 10;
            chkWatch.Text = "&Watch";
            chkWatch.UseVisualStyleBackColor = true;
            chkWatch.Visible = false;
            // 
            // MoveDelete
            // 
            MoveDelete.Location = new System.Drawing.Point(62, 64);
            MoveDelete.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MoveDelete.Name = "MoveDelete";
            MoveDelete.Size = new System.Drawing.Size(313, 101);
            MoveDelete.TabIndex = 6;
            MoveDelete.TextBoxIndexChanged += MoveDelete_TextBoxIndexChanged;
            // 
            // chkDealWithAssoc
            // 
            chkDealWithAssoc.AutoSize = true;
            chkDealWithAssoc.Location = new System.Drawing.Point(93, 162);
            chkDealWithAssoc.Name = "chkDealWithAssoc";
            chkDealWithAssoc.Size = new System.Drawing.Size(165, 19);
            chkDealWithAssoc.TabIndex = 13;
            chkDealWithAssoc.Text = "Move associated talk page";
            chkDealWithAssoc.UseVisualStyleBackColor = true;
            chkDealWithAssoc.Visible = false;
            // 
            // ArticleActionDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(413, 204);
            Controls.Add(chkDealWithAssoc);
            Controls.Add(chkWatch);
            Controls.Add(chkNoRedirect);
            Controls.Add(chkAutoProtect);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(chkCascadingProtection);
            Controls.Add(txtExpiry);
            Controls.Add(lblExpiry);
            Controls.Add(cmboSummary);
            Controls.Add(lblSummary);
            Controls.Add(txtNewTitle);
            Controls.Add(lblNewTitle);
            Controls.Add(MoveDelete);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            MaximumSize = new System.Drawing.Size(429, 243);
            MinimumSize = new System.Drawing.Size(429, 100);
            Name = "ArticleActionDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Enter details";
            FormClosing += ArticleActionDialog_FormClosing;
            Load += ArticleActionDialog_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtNewTitle;
        private System.Windows.Forms.Label lblNewTitle;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox cmboSummary;
        private System.Windows.Forms.Label lblExpiry;
        private System.Windows.Forms.TextBox txtExpiry;
        private System.Windows.Forms.CheckBox chkAutoProtect;
        private System.Windows.Forms.CheckBox chkCascadingProtection;
        private EditProtectControl MoveDelete;
        private System.Windows.Forms.CheckBox chkNoRedirect;
        private Twain.Core.Controls.AWBToolTip toolTip;
        private System.Windows.Forms.CheckBox chkWatch;
        private System.Windows.Forms.CheckBox chkDealWithAssoc;
    }
}
