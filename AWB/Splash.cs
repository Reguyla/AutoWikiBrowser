/*
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

using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace AutoWikiBrowser;

internal sealed partial class Splash : Form
{
    public Splash()
    {
        InitializeComponent();

        lblVersion.Text = "Version " + Program.VersionString;
        SetProgress(0);
    }

    private void ClickHandler(object sender, EventArgs e)
    {
        Close();
    }

    public void SetProgress(int percent)
    {
        MethodBase method = new StackFrame(1).GetMethod();

        string methodText = method?.DeclaringType == null
            ? string.Empty
            : method.DeclaringType.Name + "::" + method.Name + "()";

        SetProgressCore(percent, methodText);
    }

    private void SetProgressCore(int percent, string methodText)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(
                new Action<int, string>(SetProgressCore),
                percent,
                methodText);

            return;
        }

        if (!string.IsNullOrEmpty(methodText))
        {
            MethodLabel.Text = methodText;
        }

        progressBar.Value = Math.Clamp(
            percent,
            progressBar.Minimum,
            progressBar.Maximum);
    }
}