using System.ComponentModel;
using System.Windows.Forms;

namespace WikiFunctions.Controls;

public partial class PageContainsControl : UserControl
{
    private IArticleComparer _comparer;

    public PageContainsControl()
    {
        InitializeComponent();
        txtContains.TextChanged += txtContains_TextChanged;
    }

    private void chkSkipIfContains_CheckedChanged(object sender, EventArgs e)
    {
        // TODO: This feels weird
        CheckEnabled = chkContains.Checked;
    }

    private void txtContains_TextChanged(object sender, EventArgs e)
    {
        // disable TextChanged temporarily under Mono otherwise get infinite loop
        if (Globals.UsingMono)
            txtContains.TextChanged -= txtContains_TextChanged;

        txtContains.ResetFormatting();

        if (Globals.UsingMono)
            txtContains.TextChanged += txtContains_TextChanged;
    }

    private void InvalidateComparer(object sender, EventArgs e)
    {
        _comparer = null;
    }

    /// <summary>
    /// Gets or sets whether the page-contains check is enabled.
    /// </summary>
    /// <remarks>
    /// This property updates the enabled and checked states of the internal
    /// controls at runtime. It is not intended to be serialized independently
    /// by the Windows Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CheckEnabled
    {
        get { return chkContains.Checked; }
        set
        {
            txtContains.Enabled = value;
            chkIsRegex.Enabled = value;
            chkCaseSensitive.Enabled = value;
            chkAfterProcessing.Enabled = value;
            chkContains.Checked = value;
        }
    }

    /// <summary>
    /// Gets or sets the text used by the page-contains check.
    /// </summary>
    /// <remarks>
    /// This property is a runtime wrapper around the internal text box and should
    /// not be serialized separately by the Windows Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CheckText
    {
        get { return txtContains.Text; }
        set { txtContains.Text = value; }
    }

    /// <summary>
    /// Gets or sets whether the page-contains text is interpreted as a regular
    /// expression.
    /// </summary>
    /// <remarks>
    /// This property exposes the state of the internal regular-expression
    /// checkbox for runtime use and should not be serialized independently by
    /// the Windows Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsRegex
    {
        get { return chkIsRegex.Checked; }
        set { chkIsRegex.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether the page-contains check is case-sensitive.
    /// </summary>
    /// <remarks>
    /// This property exposes the state of the internal case-sensitivity checkbox
    /// for runtime use and should not be serialized independently by the Windows
    /// Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsCaseSensitive
    {
        get { return chkCaseSensitive.Checked; }
        set { chkCaseSensitive.Checked = value; }
    }

    /// <summary>
    /// Gets or sets whether the page-contains check runs after article processing.
    /// </summary>
    /// <remarks>
    /// This property exposes the state of the internal processing-order checkbox
    /// for runtime use and should not be serialized independently by the Windows
    /// Forms designer.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool After
    {
        get { return chkAfterProcessing.Checked; }
        set { chkAfterProcessing.Checked = value; }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="article"></param>
    /// <returns></returns>
    public virtual bool Matches(Article article)
    {
        if (_comparer == null)
        {
            _comparer = ArticleComparerFactory.Create(txtContains.Text,
                chkCaseSensitive.Checked,
                chkIsRegex.Checked,
                false, // singleline
                false // multiline
                );
        }
        return _comparer.Matches(article);
    }

    /// <summary>
    ///
    /// </summary>
    public virtual string SkipReason
    {
        get { return "Page contains: " + txtContains.Text; }
    }
}