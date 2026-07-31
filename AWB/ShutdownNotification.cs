using System.ComponentModel;
using System.Windows.Forms;

namespace AutoWikiBrowser;

public partial class ShutdownNotification : Form
{
    int Counter = 120;  // 2 minutes
    string SType;

    public ShutdownNotification()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the shutdown action displayed by the notification dialog.
    /// </summary>
    /// <remarks>
    /// This write-only property updates the runtime shutdown state and refreshes
    /// the dialog text. It is not intended for Windows Forms designer serialization.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ShutdownType
    {
        set
        {
            SType = value;
            txtPrompt.Text = string.Format(txtPrompt.Text, value);
            SetShutdownLabel(Counter);
        }
    }

    private void SetShutdownLabel(int time)
    {
        lblTimer.Text = "Time until " + SType + ": " + time;
    }

    private void CountdownTimer_Tick(object sender, EventArgs e)
    {
        Counter--;
        if (Counter != 0)
        {
            SetShutdownLabel(Counter);
            Application.DoEvents();
        }
        else
            Close();
    }
}