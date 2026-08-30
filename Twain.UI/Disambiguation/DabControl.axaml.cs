using Avalonia.Controls;
using System.Collections.Generic;
using Twain.Core.Disambiguation;

namespace Twain.UI.Disambiguation;

/// <summary>
/// Displays and manages one disambiguation occurrence in the Avalonia UI.
/// </summary>
public partial class DabControl : UserControl
{
    private DisambiguationProcessor.DisambiguationItemPreparation? _preparation;
    private IReadOnlyList<string>? _variants;

    /// <summary>
    /// Raised when the selected disambiguation choice or correction text changes.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DabControl"/> class.
    /// </summary>
    public DabControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DabControl"/> class
    /// using prepared disambiguation data.
    /// </summary>
    /// <param name="preparation">
    /// The prepared data for the disambiguation occurrence.
    /// </param>
    /// <param name="variants">
    /// The available disambiguation target variants.
    /// </param>
    public DabControl(
        DisambiguationProcessor.DisambiguationItemPreparation preparation,
        IReadOnlyList<string> variants)
        : this()
    {
        _preparation = preparation;
        _variants = variants;

        PopulateControl();
    }

    /// <summary>
    /// Gets the replacement text currently entered for this occurrence.
    /// </summary>
    public string Result =>
        CorrectionTextBox.Text ?? string.Empty;

    /// <summary>
    /// Gets whether the current selection represents no change.
    /// </summary>
    /// <remarks>
    /// This currently preserves the behavior of the legacy WinForms
    /// disambiguation control.
    /// </remarks>
    public bool NoChange =>
        ChoiceComboBox.SelectedIndex == 0 &&
        Result == _preparation?.Surroundings;

    /// <summary>
    /// Gets whether the current correction contains text that can be saved.
    /// </summary>
    public bool CanSave =>
        !string.IsNullOrWhiteSpace(Result);

    /// <summary>
    /// Restores this occurrence to the default no-change selection.
    /// </summary>
    public void Reset()
    {
        if (ChoiceComboBox.SelectedIndex != 0)
        {
            ChoiceComboBox.SelectedIndex = 0;
        }
        else
        {
            UpdateReplacement();
        }
    }

    /// <summary>
    /// Discards manual correction edits and regenerates the replacement
    /// text from the currently selected disambiguation choice.
    /// </summary>
    public void Undo()
    {
        UpdateReplacement();
    }

    /// <summary>
    /// Populates the control from the prepared disambiguation data.
    /// </summary>
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

        bool hasPipe = Result.Contains('|');

        UnpipeButton.IsEnabled = hasPipe;
        FlipButton.IsEnabled = hasPipe;
    }

    /// <summary>
    /// Handles changes to the selected disambiguation choice.
    /// </summary>
    private void ChoiceComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        UpdateReplacement();
    }

    /// <summary>
    /// Regenerates the replacement text for the currently selected
    /// disambiguation choice.
    /// </summary>
    private void UpdateReplacement()
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

        Changed?.Invoke(this, EventArgs.Empty);

        bool hasPipe = Result.Contains('|');

        UnpipeButton.IsEnabled = hasPipe;
        FlipButton.IsEnabled = hasPipe;
    }

    /// <summary>
    /// Handles manual changes to the correction text.
    /// </summary>
    private void CorrectionTextBox_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes the displayed-text portion from the current piped wikilink.
    /// </summary>
    private void UnpipeButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        string currentLink = Result;

        string newLink =
            DisambiguationProcessor.UnpipeLink(currentLink);

        CorrectionTextBox.Text =
            Result.Replace(currentLink, newLink);
    }

    /// <summary>
    /// Swaps the target and displayed-text portions of the current piped
    /// wikilink.
    /// </summary>
    private void FlipButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        string currentLink = Result;

        string newLink =
            DisambiguationProcessor.FlipLink(currentLink);

        CorrectionTextBox.Text =
            Result.Replace(currentLink, newLink);
    }
}