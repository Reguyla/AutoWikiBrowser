using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Twain.Core.Plugin;

namespace Twain.UI.SkipOptions;

/// <summary>
/// Provides options for skipping articles when selected automatic processing
/// operations did not make a change.
/// </summary>
/// <remarks>
/// The window is hidden rather than closed so that its selected options remain
/// available to the active Twain workflow.
/// </remarks>
public partial class SkipOptionsWindow :
    Avalonia.Controls.Window,
    ISkipOptions
{
    private const int BoldTitleOptionId = 1;
    private const int BulletedExternalLinkOptionId = 2;
    private const int BadLinksOptionId = 3;
    private const int UnicodeOptionId = 4;
    private const int AutoTagOptionId = 5;
    private const int HeaderErrorOptionId = 6;
    private const int DefaultSortOptionId = 7;
    private const int UserTalkTemplatesOptionId = 8;
    private const int CitationTemplateDatesOptionId = 9;
    private const int HumanCategoriesOptionId = 10;

    private readonly Dictionary<int, CheckBox> _optionCheckBoxes;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipOptionsWindow"/> window.
    /// </summary>
    public SkipOptionsWindow()
    {
        InitializeComponent();

        _optionCheckBoxes =
            new Dictionary<int, CheckBox>
            {
                [BoldTitleOptionId] = BoldTitleOption,
                [BulletedExternalLinkOptionId] = BulletedExternalLinkOption,
                [BadLinksOptionId] = BadLinksOption,
                [UnicodeOptionId] = UnicodeOption,
                [AutoTagOptionId] = AutoTagOption,
                [HeaderErrorOptionId] = HeaderErrorOption,
                [DefaultSortOptionId] = DefaultSortOption,
                [UserTalkTemplatesOptionId] = UserTalkTemplatesOption,
                [CitationTemplateDatesOptionId] = CitationTemplateDatesOption,
                [HumanCategoriesOptionId] = HumanCategoriesOption
            };
    }

    /// <inheritdoc />
    public bool SkipNoBoldTitle =>
        IsOptionChecked(BoldTitleOptionId);

    /// <inheritdoc />
    public bool SkipNoBulletedLink =>
        IsOptionChecked(BulletedExternalLinkOptionId);

    /// <inheritdoc />
    public bool SkipNoBadLink =>
        IsOptionChecked(BadLinksOptionId);

    /// <inheritdoc />
    public bool SkipNoUnicode =>
        IsOptionChecked(UnicodeOptionId);

    /// <inheritdoc />
    public bool SkipNoTag =>
        IsOptionChecked(AutoTagOptionId);

    /// <inheritdoc />
    public bool SkipNoHeaderError =>
        IsOptionChecked(HeaderErrorOptionId);

    /// <inheritdoc />
    public bool SkipNoDefaultSortAdded =>
        IsOptionChecked(DefaultSortOptionId);

    /// <inheritdoc />
    public bool SkipNoUserTalkTemplatesSubstd =>
        IsOptionChecked(UserTalkTemplatesOptionId);

    /// <inheritdoc />
    public bool SkipNoCiteTemplateDatesFixed =>
        IsOptionChecked(CitationTemplateDatesOptionId);

    /// <inheritdoc />
    public bool SkipNoPeopleCategoriesFixed =>
        IsOptionChecked(HumanCategoriesOptionId);

    /// <summary>
    /// Gets or sets the identifiers of the currently selected skip options.
    /// </summary>
    public List<int> SelectedItems
    {
        get
        {
            List<int> selectedItems = new();

            foreach (
                KeyValuePair<int, CheckBox> option
                in _optionCheckBoxes)
            {
                if (option.Value.IsChecked == true)
                {
                    selectedItems.Add(
                        option.Key);
                }
            }

            return selectedItems;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            CheckAll.IsChecked = false;
            CheckNone.IsChecked = false;

            foreach (
                KeyValuePair<int, CheckBox> option
                in _optionCheckBoxes)
            {
                option.Value.IsChecked =
                    value.Contains(
                        option.Key);
            }
        }
    }

    /// <summary>
    /// Selects every available skip option when Check All is selected.
    /// </summary>
    private void CheckAll_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (CheckAll.IsChecked != true)
        {
            return;
        }

        CheckNone.IsChecked = false;
        SetCheckboxes(true);
    }

    /// <summary>
    /// Clears every available skip option when Check None is selected.
    /// </summary>
    private void CheckNone_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (CheckNone.IsChecked != true)
        {
            return;
        }

        CheckAll.IsChecked = false;
        SetCheckboxes(false);
    }

    /// <summary>
    /// Hides the skip-options window while preserving its current state.
    /// </summary>
    private void CloseButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Hide();
    }

    /// <summary>
    /// Prevents the reusable skip-options window from being destroyed when
    /// the user closes it.
    /// </summary>
    private void SkipOptionsWindow_Closing(
        object? sender,
        WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// Determines whether the skip option with the specified identifier is
    /// currently selected.
    /// </summary>
    /// <param name="optionId">
    /// The stable identifier of the option.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the option exists and is checked;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool IsOptionChecked(
        int optionId)
    {
        return
            _optionCheckBoxes.TryGetValue(
                optionId,
                out CheckBox? checkBox) &&
            checkBox.IsChecked == true;
    }

    /// <summary>
    /// Sets the checked state of every available skip option.
    /// </summary>
    /// <param name="isChecked">
    /// The checked state to apply.
    /// </param>
    private void SetCheckboxes(
        bool isChecked)
    {
        foreach (
            CheckBox checkBox
            in _optionCheckBoxes.Values)
        {
            checkBox.IsChecked =
                isChecked;
        }
    }
}