using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Twain.Core.Controls;

namespace Twain.UI.Controls;

/// <summary>
/// Collects the options required for move, delete, and protection actions.
/// </summary>
public partial class ArticleActionDialogWindow : Avalonia.Controls.Window
{
    private readonly ArticleAction _currentAction;

    private readonly ObservableCollection<ProtectionLevel> _protectionLevels = [];

    /// <summary>
    /// Initializes a preview instance of the dialog.
    /// </summary>
    public ArticleActionDialogWindow()
        : this(ArticleAction.Move)
    {
    }

    /// <summary>
    /// Initializes the dialog for the supplied article action.
    /// </summary>
    public ArticleActionDialogWindow(
        ArticleAction action)
    {
        InitializeComponent();

        _currentAction = action;

        ConfigureAction();
        PopulateSummaries();
        PopulateProtectionLevels();
    }

    /// <summary>
    /// Gets or sets the new article title.
    /// </summary>
    public string NewTitle
    {
        get => NewTitleTextBox.Text ?? string.Empty;
        set => NewTitleTextBox.Text = value;
    }

    /// <summary>
    /// Gets or sets the action summary.
    /// </summary>
    public string Summary
    {
        get => SummaryComboBox.Text ?? string.Empty;
        set => SummaryComboBox.Text = value;
    }

    /// <summary>
    /// Gets or sets the edit protection level.
    /// </summary>
    public string EditProtectionLevel
    {
        get => GetSelectedProtectionLevel(
            EditProtectionListBox);

        set => SelectProtectionLevel(
            EditProtectionListBox,
            value);
    }

    /// <summary>
    /// Gets or sets the move protection level.
    /// </summary>
    public string MoveProtectionLevel
    {
        get => GetSelectedProtectionLevel(
            MoveProtectionListBox);

        set => SelectProtectionLevel(
            MoveProtectionListBox,
            value);
    }

    /// <summary>
    /// Gets the protection expiry value.
    /// </summary>
    public string ProtectExpiry =>
        ExpiryTextBox.Text ?? string.Empty;

    /// <summary>
    /// Gets whether cascading protection is selected.
    /// </summary>
    public bool CascadingProtection =>
        CascadingProtectionCheckBox.IsChecked == true;

    /// <summary>
    /// Gets whether automatic protection of all related pages is selected.
    /// </summary>
    public bool AutoProtectAll =>
        AutoProtectAllCheckBox.IsChecked == true;

    /// <summary>
    /// Gets whether a redirect should be suppressed after a move.
    /// </summary>
    public bool NoRedirect =>
        NoRedirectCheckBox.IsChecked == true;

    /// <summary>
    /// Gets whether the page should be watched.
    /// </summary>
    public bool Watch =>
        WatchCheckBox.IsChecked == true;

    /// <summary>
    /// Gets whether the associated talk page should also be moved.
    /// </summary>
    public bool DealWithAssocTalkPage =>
        DealWithAssociatedTalkPageCheckBox.IsChecked == true;

    private void ConfigureAction()
    {
        string actionText =
            _currentAction switch
            {
                ArticleAction.Move => "Move",
                ArticleAction.Delete => "Delete",
                ArticleAction.Protect => "Protect",
                _ => "OK"
            };

        Title = actionText;
        OkButton.Content = actionText;

        NewTitlePanel.IsVisible =
            _currentAction == ArticleAction.Move;

        ExpiryPanel.IsVisible =
            _currentAction == ArticleAction.Protect;

        ProtectionPanel.IsVisible =
            _currentAction == ArticleAction.Protect;

        MoveOptionsPanel.IsVisible =
            _currentAction == ArticleAction.Move;
    }

    private void PopulateSummaries()
    {
        SummaryComboBox.ItemsSource =
            ArticleActionHelper.GetDefaultSummaries(
                _currentAction);
    }

    private void PopulateProtectionLevels()
    {
        foreach (ProtectionLevel level in
            ArticleActionHelper.GetProtectionLevels())
        {
            _protectionLevels.Add(level);
        }

        EditProtectionListBox.ItemsSource =
            _protectionLevels;

        MoveProtectionListBox.ItemsSource =
            _protectionLevels;

        EditProtectionListBox.SelectedIndex = 0;
        MoveProtectionListBox.SelectedIndex = 0;
    }

    private void UnlockMovePermissionsCheckBox_Click(
        object? sender,
        RoutedEventArgs e)
    {
        bool unlocked =
            UnlockMovePermissionsCheckBox.IsChecked == true;

        MoveProtectionListBox.IsEnabled =
            unlocked;

        if (!unlocked)
        {
            MoveProtectionListBox.SelectedIndex =
                EditProtectionListBox.SelectedIndex;
        }
    }

    private void EditProtectionSelectionChanged(
    object? sender,
    SelectionChangedEventArgs e)
    {
        if (UnlockMovePermissionsCheckBox.IsChecked == true)
            return;

        MoveProtectionListBox.SelectedIndex =
            EditProtectionListBox.SelectedIndex;
    }

    private void MoveProtectionSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        UpdateCascadingProtectionState();
    }

    private void UpdateCascadingProtectionState()
    {
        CascadingProtectionCheckBox.IsEnabled =
            ArticleActionHelper.IsCascadingProtectionAvailable(
                EditProtectionListBox.SelectedIndex,
                MoveProtectionListBox.SelectedIndex);

        if (!CascadingProtectionCheckBox.IsEnabled)
            CascadingProtectionCheckBox.IsChecked = false;
    }

    private async void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        string errorMessage =
            ArticleActionHelper.Validate(
                _currentAction,
                Summary,
                NewTitle,
                ProtectExpiry,
                EditProtectionLevel,
                MoveProtectionLevel);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            string errorTitle =
                ArticleActionHelper.GetValidationErrorTitle(
                    _currentAction);

            ArticleActionValidationWindow validationWindow =
                new(
                    errorTitle,
                    errorMessage);

            await validationWindow.ShowDialog(this);

            return;
        }

        Close(true);
    }

    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    private static string GetSelectedProtectionLevel(
        ListBox listBox)
    {
        return listBox.SelectedItem is ProtectionLevel level
            ? level.Group
            : string.Empty;
    }

    private void SelectProtectionLevel(
        ListBox listBox,
        string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            listBox.SelectedIndex = 0;
            return;
        }

        ProtectionLevel? matchingLevel =
            FindProtectionLevel(value);

        if (matchingLevel is null)
        {
            matchingLevel =
                new ProtectionLevel(
                    value,
                    value);

            _protectionLevels.Add(
                matchingLevel);
        }

        listBox.SelectedItem =
            matchingLevel;
    }

    private ProtectionLevel? FindProtectionLevel(
    string group)
    {
        foreach (ProtectionLevel level in
            _protectionLevels)
        {
            if (level.Group == group)
                return level;
        }

        return null;
    }
}