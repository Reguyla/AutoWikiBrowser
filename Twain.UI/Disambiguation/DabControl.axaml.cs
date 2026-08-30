using Avalonia.Controls;
using System.Collections.Generic;
using Twain.Core.Disambiguation;

namespace Twain.UI.Disambiguation;

public partial class DabControl : UserControl
{
    private DisambiguationProcessor.DisambiguationItemPreparation? _preparation;
    private IReadOnlyList<string>? _variants;

    public DabControl()
    {
        InitializeComponent();
    }

    public DabControl(
        DisambiguationProcessor.DisambiguationItemPreparation preparation,
        IReadOnlyList<string> variants)
        : this()
    {
        _preparation = preparation;
        _variants = variants;

        PopulateControl();
    }

    private void PopulateControl()
    {
        if (_preparation is null || _variants is null)
            return;

        ParagraphTextBlock.Text = _preparation.ParagraphText;
        CorrectionTextBox.Text = _preparation.OriginalLink;

        ChoiceComboBox.Items.Clear();

        foreach (string variant in _variants)
        {
            ChoiceComboBox.Items.Add(variant);
        }

        ChoiceComboBox.SelectedIndex = 0;
    }
}