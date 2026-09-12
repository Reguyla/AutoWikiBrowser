/*

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

namespace Twain.Core.Controls;

public partial class EditProtectControl : UserControl
{
    public event EventHandler TextBoxIndexChanged;

    public EditProtectControl()
    {
        InitializeComponent();

        foreach (ProtectionLevel level in
            ProtectionLevelHelper.GetLevels(
                Variables.LangCode))
        {
            lbEdit.Items.Add(level);
            lbMove.Items.Add(level);
        }
    }

    private void chkUnlock_CheckedChanged(object sender, EventArgs e)
    {
        lbMove.Enabled = chkUnlock.Checked;
    }

    private void lbEdit_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!chkUnlock.Checked)
            lbMove.SelectedIndex = lbEdit.SelectedIndex;
    }

    private void BothListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (TextBoxIndexChanged != null)
            TextBoxIndexChanged(this, e);
    }

    [Browsable(false)]
    public bool CascadingEnabled
    {
        get
        {
            return ProtectionLevelHelper.IsCascadingEnabled(
                lbEdit.SelectedIndex,
                lbMove.SelectedIndex);
        }
    }

    /// <summary>
    /// Gets or sets the edit protection level selected by the control.
    /// </summary>
    /// <remarks>
    /// This property is intended for runtime use only. It proxies the selected
    /// value of the edit-protection list and must not be serialized separately
    /// by the Windows Forms designer.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EditProtectionLevel
    {
        get { return GetProtectionLevel(lbEdit); }
        set
        {
            if (DesignMode)
                return;

            if (string.IsNullOrEmpty(value))
            {
                lbEdit.SelectedIndex = 0;
                return;
            }

            EnsureProtectionLevelExists(value);
            lbEdit.SelectedItem = value;
        }
    }

    /// <summary>
    /// Gets or sets the move protection level selected by the control.
    /// </summary>
    /// <remarks>
    /// This property is intended for runtime use only. It proxies the selected
    /// value of the move-protection list and must not be serialized separately
    /// by the Windows Forms designer.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string MoveProtectionLevel
    {
        get { return GetProtectionLevel(lbMove); }
        set
        {
            if (DesignMode)
                return;

            if (string.IsNullOrEmpty(value))
            {
                lbMove.SelectedIndex = 0;
                return;
            }

            EnsureProtectionLevelExists(value);
            lbMove.SelectedItem = value;
        }
    }

    /// <summary>
    /// Sets whether the protection controls are visible.
    /// </summary>
    /// <remarks>
    /// This write-only property updates the visibility of several child controls
    /// at runtime. It is not a design-time property and must not be serialized by
    /// the Windows Forms designer.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Visibility
    {
        set
        {
            lbEdit.Visible =
                lbMove.Visible =
                lblEdit.Visible =
                lblMove.Visible =
                chkUnlock.Visible = value;
        }
    }

    public void Reset()
    {
        lbEdit.SelectedIndex = 0;
        lbMove.SelectedIndex = 0;
        chkUnlock.Checked = false;
    }

    private static string GetProtectionLevel(ListBox lb)
    {
        var prot = lb.SelectedItem as ProtectionLevel;
        return (prot != null) ? prot.Group : "";
    }

    private void EnsureProtectionLevelExists(string group)
    {
        EnsureProtectionLevelExists(group, lbEdit);
        EnsureProtectionLevelExists(group, lbMove);
    }

    private static void EnsureProtectionLevelExists(string group, ListBox lb)
    {
        ProtectionLevel p = new ProtectionLevel(group, group);
        if (!lb.Items.Contains(p)) lb.Items.Add(p);
    }
}