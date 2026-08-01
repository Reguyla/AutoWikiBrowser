using System.Windows.Forms;
using WikiFunctions;

namespace AutoWikiBrowser;

/// <summary>
/// Presents a dialog for entering HTTP authentication credentials.
/// </summary>
internal sealed partial class Login : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Login"/> class.
    /// </summary>
    public Login()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Closes the dialog when the Enter key is pressed.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data containing the released key.
    /// </param>
    private void FormOnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            CloseForm();
        }
    }

    /// <summary>
    /// Saves the entered credentials and closes the dialog when the
    /// <c>Login</c> button is clicked.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnLogin_Click(object sender, EventArgs e)
    {
        CloseForm();
    }

    /// <summary>
    /// Stores the entered HTTP authentication credentials and closes the
    /// dialog.
    /// </summary>
    private void CloseForm()
    {
        Variables.HttpAuthUsername = txtUsername.Text;
        Variables.HttpAuthPassword = txtPassword.Text;

        Close();
    }
}