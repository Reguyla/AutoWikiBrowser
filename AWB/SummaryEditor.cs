/*
Autowikibrowser

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

using System.Windows.Forms;

namespace AutoWikiBrowser;

/// <summary>
/// Provides an editor for managing the collection of predefined edit summaries.
/// </summary>
internal sealed partial class SummaryEditor : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryEditor"/> form.
    /// </summary>
    public SummaryEditor()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sorts the non-empty summary entries using the current culture's
    /// default string comparison rules.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void btnSort_Click(object sender, EventArgs e)
    {
        List<string> summaries = Summaries.Lines
            .Where(summary => !string.IsNullOrEmpty(summary))
            .ToList();

        summaries.Sort(StringComparer.CurrentCulture);

        Summaries.Lines = summaries.ToArray();
    }
}