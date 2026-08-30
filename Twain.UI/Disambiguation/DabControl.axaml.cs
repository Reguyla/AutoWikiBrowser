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

        ChoiceComboBox.Items.Add("No change");
        ChoiceComboBox.Items.Add("Unlink");
        ChoiceComboBox.Items.Add("Disambiguation needed");

        foreach (string variant in _variants)
        {
            ChoiceComboBox.Items.Add(variant);
        }

        ChoiceComboBox.SelectedIndex = 0;
    }

    private void ChoiceComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_preparation is null ||
            _variants is null ||
            ChoiceComboBox.SelectedIndex < 0)
        {
            return;
        }

        CorrectionTextBox.Text =
            DisambiguationProcessor.CreateReplacement(
                ChoiceComboBox.SelectedIndex,
                _preparation.OriginalLink,
                _preparation.VisibleLink,
                _preparation.RealLink,
                _preparation.LinkTrail,
                _preparation.StartOfSentence,
                _variants);
    }
}