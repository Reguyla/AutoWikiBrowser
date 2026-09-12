/*
Copyright (C) 2007 Martin Richards

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
using System.Drawing;
using System.Windows.Forms;

namespace Twain.Core.Controls;

public enum ArticleAction
{
    Move,
    Delete,
    Protect
}

public partial class ArticleActionDialog : Form
{
    private readonly ArticleAction CurrentAction;

    public ArticleActionDialog(ArticleAction moveDeleteProtect)
    {
        InitializeComponent();

        CurrentAction = moveDeleteProtect;

        if (moveDeleteProtect == ArticleAction.Protect)
        {
            lblSummary.Location = new Point(8, 15);
            cmboSummary.Location = new Point(62, 12);
            lblNewTitle.Visible = false;
            txtNewTitle.Visible = false;

            toolTip.SetToolTip(
                chkCascadingProtection,
                "Automatically protect any pages transcluded in this page");
        }
        else
        {
            MoveDelete.Visibility = false;
            lblExpiry.Visible = false;
            txtExpiry.Visible = false;
            chkCascadingProtection.Visible = false;

            if (moveDeleteProtect == ArticleAction.Move)
            {
                Size = new Size(Width, 240);
                chkNoRedirect.Visible = true;
                chkWatch.Visible = true;
                chkDealWithAssoc.Visible = true;
            }
            else
            {
                Size = new Size(Width, 100);
                lblSummary.Location = new Point(8, 15);
                cmboSummary.Location = new Point(62, 12);

                lblNewTitle.Visible = false;
                txtNewTitle.Visible = false;
            }
        }

        cmboSummary.Items.AddRange(
            ArticleActionHelper
                .GetDefaultSummaries(moveDeleteProtect)
                .ToArray());
    }

    private void MoveDelete_TextBoxIndexChanged(object sender, EventArgs e)
    {
        chkCascadingProtection.Enabled =
            MoveDelete.CascadingEnabled;
    }

    public bool AutoProtectAll
    {
        get { return chkAutoProtect.Checked; }
    }

    /// <summary>
    /// Gets or sets the new article title entered by the user.
    /// </summary>
    /// <remarks>
    /// This property is a runtime wrapper around <see cref="txtNewTitle"/> and
    /// should not be serialized by the Windows Forms designer. The underlying
    /// text box is already responsible for persisting its own design-time state.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string NewTitle
    {
        get { return txtNewTitle.Text; }
        set { txtNewTitle.Text = value; }
    }

    /// <summary>
    /// Gets or sets the edit summary entered or selected by the user.
    /// </summary>
    /// <remarks>
    /// This property exposes the current value of the summary combo box for
    /// runtime use. Designer serialization is disabled because the underlying
    /// control already manages its own design-time state.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Summary
    {
        get { return cmboSummary.Text; }
        set { cmboSummary.Text = value; }
    }

    /// <summary>
    /// Gets or sets the edit protection level for the current protection action.
    /// </summary>
    /// <remarks>
    /// This property forwards to the <c>MoveDelete</c> control and exists only as
    /// a convenience wrapper for runtime code. It is not intended to participate
    /// in Windows Forms designer serialization.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EditProtectionLevel
    {
        get
        {
            return CurrentAction == ArticleAction.Protect
                ? MoveDelete.EditProtectionLevel
                : string.Empty;
        }
        set { MoveDelete.EditProtectionLevel = value; }
    }

    /// <summary>
    /// Gets or sets the move protection level for the current protection action.
    /// </summary>
    /// <remarks>
    /// This property forwards to the <c>MoveDelete</c> control and exists only as
    /// a convenience wrapper for runtime code. It is not intended to participate
    /// in Windows Forms designer serialization.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string MoveProtectionLevel
    {
        get
        {
            return CurrentAction == ArticleAction.Protect
                ? MoveDelete.MoveProtectionLevel
                : string.Empty;
        }
        set { MoveDelete.MoveProtectionLevel = value; }
    }

    public string ProtectExpiry
    {
        get { return txtExpiry.Text; }
    }

    public bool CascadingProtection
    {
        get { return chkCascadingProtection.Checked; }
    }

    public bool NoRedirect
    {
        get { return chkNoRedirect.Checked; }
    }

    public bool Watch
    {
        get { return chkWatch.Checked; }
    }

    public bool DealWithAssocTalkPage
    {
        get { return chkDealWithAssoc.Checked; }
    }

    private void ArticleActionDialog_Load(object sender, EventArgs e)
    {
        switch (CurrentAction)
        {
            case ArticleAction.Delete:
                Text = btnOk.Text = "Delete";
                break;

            case ArticleAction.Move:
                Text = btnOk.Text = "Move";
                break;

            case ArticleAction.Protect:
                Text = btnOk.Text = "Protect";
                break;
        }
    }

    private void ArticleActionDialog_FormClosing(
        object sender,
        FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
            return;

        string errorMessage =
            ArticleActionHelper.Validate(
                CurrentAction,
                Summary,
                NewTitle,
                ProtectExpiry,
                EditProtectionLevel,
                MoveProtectionLevel);

        if (string.IsNullOrEmpty(errorMessage))
            return;

        string errorTitle =
            ArticleActionHelper.GetValidationErrorTitle(
                CurrentAction);

        MessageBox.Show(
            errorMessage,
            errorTitle);

        e.Cancel = true;
    }
}