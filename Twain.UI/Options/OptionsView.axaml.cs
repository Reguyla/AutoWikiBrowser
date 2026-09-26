using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Twain.Core;
using Twain.Core.Disambiguation;
using Twain.UI.FindReplace;
using Twain.UI.ReplaceSpecial;
using Twain.UI.Templates;

namespace Twain.UI.Options;

/// <summary>
/// Displays configurable options for the active editing workspace.
/// </summary>
/// <remarks>
/// The initial implementation presents temporary option controls. Future
/// implementations will bind these controls to persistent settings and
/// processing services supplied by Twain.Core.
/// </remarks>
public partial class OptionsView : UserControl
{
    private ReplaceSpecialWindow? _replaceSpecialWindow;

    /// <summary>
    /// Initializes the options view.
    /// </summary>
    public OptionsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Displays the additional skip options.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the click.
    /// </param>
    private void AutoChangesSkipOptionsButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OptionsTabControl.SelectedIndex = 3;
    }

    /// <summary>
    /// Opens the find and replace configuration window.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the click.
    /// </param>
    private async void FindReplaceNormalSettingsButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not OptionsViewModel viewModel)
        {
            return;
        }

        FindReplaceWindow window =
            new(viewModel.FindReplace);

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            await window.ShowDialog(owner);
            return;
        }

        window.Show();
    }

    /// <summary>
    /// Shows or hides the Replace Special configuration window.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the click.
    /// </param>
    private void FindReplaceAdvancedSettingsButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_replaceSpecialWindow is null)
        {
            _replaceSpecialWindow = new ReplaceSpecialWindow();

            _replaceSpecialWindow.Closed +=
                ReplaceSpecialWindow_Closed;

            _replaceSpecialWindow.Show();
            return;
        }

        if (_replaceSpecialWindow.IsVisible)
        {
            _replaceSpecialWindow.Hide();
            return;
        }

        _replaceSpecialWindow.Show();
    }

    /// <summary>
    /// Clears the cached Replace Special window after it is closed.
    /// </summary>
    private void ReplaceSpecialWindow_Closed(
        object? sender,
        EventArgs e)
    {
        _replaceSpecialWindow = null;
    }


    /// <summary>
    /// Opens the template substitution configuration dialog.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data associated with the click.
    /// </param>
    private async void TemplateSubstitutionButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not OptionsViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        SubstTemplatesWindow window =
            new(
                viewModel.TemplateSubstitutionTemplates,
                viewModel.TemplateSubstitutionExpandRecursively,
                viewModel.TemplateSubstitutionIgnoreUnformatted,
                viewModel.TemplateSubstitutionIncludeComments);

        bool accepted =
            await window.ShowDialog<bool>(owner);

        if (!accepted)
        {
            return;
        }

        viewModel.TemplateSubstitutionTemplates =
            [.. window.TemplateList];

        viewModel.TemplateSubstitutionExpandRecursively =
            window.ExpandRecursively;

        viewModel.TemplateSubstitutionIgnoreUnformatted =
            window.IgnoreUnformatted;

        viewModel.TemplateSubstitutionIncludeComments =
            window.IncludeComments;
    }

    /// <summary>
    /// Loads the available variants for the configured disambiguation page.
    /// </summary>
    private void LoadDisambiguationLinksButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not OptionsViewModel viewModel)
        {
            return;
        }

        try
        {
            viewModel.DisambiguationVariants =
                DisambiguationLinkHelper.LoadVariantsText(
                    viewModel.DisambiguationLink);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Loads disambiguation variants when Enter is pressed in the
    /// disambiguation page field.
    /// </summary>
    private void DisambiguationLinkTextBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;

        LoadDisambiguationLinksButton_Click(
            LoadDisambiguationLinksButton,
            new RoutedEventArgs());
    }

    /// <summary>
    /// Initializes an empty disambiguation link from the current Make List source.
    /// </summary>
    private void DisambiguationLinkTextBox_GotFocus(
        object? sender,
        FocusChangedEventArgs e)
    {
        if (DataContext is OptionsViewModel viewModel)
        {
            viewModel.PopulateDisambiguationLinkFromSource();
        }
    }

    private void PageExistenceRadioButton_IsCheckedChanged(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not OptionsViewModel viewModel
            || sender is not RadioButton radioButton
            || radioButton.IsChecked != true
            || radioButton.Tag is not string tag
            || !int.TryParse(tag, out int value))
        {
            return;
        }

        viewModel.PageExistenceSkip = value;
    }

}