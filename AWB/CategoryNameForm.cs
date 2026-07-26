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
using WikiFunctions;

namespace AutoWikiBrowser;

/// <summary>
/// Prompts the user to enter the name of a category.
/// </summary>
/// <remarks>
/// The form automatically prefixes the entered name with the localized
/// category namespace (for example, "Category:" on English Wikipedia)
/// before returning the completed page title.
/// </remarks>
public partial class CategoryNameForm : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryNameForm"/> class.
    /// </summary>
    public CategoryNameForm()
    {
        InitializeComponent();
    }

    // TODO (modernization): Consider exposing the category name through the
    // dialog result instead of returning an empty string for blank input.
    // This would separate "user entered nothing" from "user cancelled" and
    // avoid using an empty string as a sentinel value. Preserve the current
    // behavior until all callers have been reviewed.
    /// <summary>
    /// Gets the fully qualified category page name entered by the user.
    /// </summary>
    /// <value>
    /// The localized category namespace followed by the entered category
    /// name, or an empty string if no category name was supplied.
    /// </value>
    public string CategoryName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(txtCategory.Text))
                return lblCategory.Text + txtCategory.Text;

            return string.Empty;
        }
    }

    /// <summary>
    /// Initializes the dialog after it has been loaded.
    /// </summary>
    /// <param name="sender">
    /// The object raising the event.
    /// </param>
    /// <param name="e">
    /// Event data associated with the load event.
    /// </param>
    private void frmCategoryName_Load(object sender, EventArgs e)
    {
        // Display the localized category namespace for the current wiki.
        lblCategory.Text = Variables.Namespaces[Namespace.Category];

        btnOk.DialogResult = DialogResult.OK;

        // Select any existing category name so it can be replaced quickly.
        if (!string.IsNullOrEmpty(txtCategory.Text))
            txtCategory.SelectAll();
    }

    /// <summary>
    /// Closes the dialog after the user accepts the current category name.
    /// </summary>
    /// <param name="sender">
    /// The object raising the event.
    /// </param>
    /// <param name="e">
    /// Event data associated with the click event.
    /// </param>
    private void btnOk_Click(object sender, EventArgs e)
    {
        Close();
    }
}