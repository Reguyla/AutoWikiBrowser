using System.Windows.Forms;

namespace Twain.Core.Controls;

public class ComboBoxInvoke : ComboBox
{
    public override int SelectedIndex
    {
        get
        {
            if (!InvokeRequired)
            {
                return base.SelectedIndex;
            }

            return (int)Invoke(new Func<int>(() => SelectedIndex));
        }
        set { base.SelectedIndex = value; }
    }
}