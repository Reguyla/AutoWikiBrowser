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

namespace Twain.Core.Background
{
    public partial class PleaseWait : Form
    {
        /// <summary>
        /// Occurs when the user requests cancellation of the background operation.
        /// </summary>
        public event EventHandler CancelRequested;

        delegate void SetTextCallback(string text);

        delegate void SetProgressCallback(int completed, int total);

        public PleaseWait()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void SetStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                SetTextCallback d = SetStatus;
                Invoke(d, new object[] { status });
            }
            else
                lblStatus.Text = status;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Status
        {
            get => lblStatus.Text;
            set => lblStatus.Text = value;
        }

        public void SetProgress(int completed, int total)
        {
            if (Progress.InvokeRequired)
            {
                SetProgressCallback d = SetProgress;
                Invoke(d, new object[] { completed, total });
            }
            else
            {
                Progress.Maximum = total;
                Progress.Value = completed;

                groupBox.Text = string.Format("{0}/{1} complete", completed, total);
            }
        }
    }
}