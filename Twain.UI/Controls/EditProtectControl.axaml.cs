using Avalonia.Controls;
using Twain.Core;
using Twain.Core.Controls;

namespace Twain.UI.Controls;

public partial class EditProtectControl : UserControl
{
    public event EventHandler? SelectionChanged;

    public EditProtectControl()
    {
        InitializeComponent();

        foreach (ProtectionLevel level in
            ProtectionLevelHelper.GetLevels(Variables.LangCode))
        {
            EditProtectionListBox.Items.Add(level);
            MoveProtectionListBox.Items.Add(level);
        }

        Reset();
    }

    public bool CascadingEnabled =>
        ProtectionLevelHelper.IsCascadingEnabled(
            EditProtectionListBox.SelectedIndex,
            MoveProtectionListBox.SelectedIndex);

    public string EditProtectionLevel
    {
        get => GetProtectionLevel(EditProtectionListBox);

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                EditProtectionListBox.SelectedIndex = 0;
                return;
            }

            EnsureProtectionLevelExists(value);
            SelectProtectionLevel(EditProtectionListBox, value);
        }
    }

    public string MoveProtectionLevel
    {
        get => GetProtectionLevel(MoveProtectionListBox);

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                MoveProtectionListBox.SelectedIndex = 0;
                return;
            }

            EnsureProtectionLevelExists(value);
            SelectProtectionLevel(MoveProtectionListBox, value);
        }
    }

    public void Reset()
    {
        EditProtectionListBox.SelectedIndex =
            EditProtectionListBox.ItemCount > 0 ? 0 : -1;

        MoveProtectionListBox.SelectedIndex =
            MoveProtectionListBox.ItemCount > 0 ? 0 : -1;

        UnlockMoveCheckBox.IsChecked = false;
        MoveProtectionListBox.IsEnabled = false;
    }

    private void UnlockMoveCheckBox_IsCheckedChanged(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        MoveProtectionListBox.IsEnabled =
            UnlockMoveCheckBox.IsChecked == true;
    }

    private void EditProtectionListBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (UnlockMoveCheckBox.IsChecked != true)
        {
            MoveProtectionListBox.SelectedIndex =
                EditProtectionListBox.SelectedIndex;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MoveProtectionListBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string GetProtectionLevel(ListBox listBox)
    {
        return listBox.SelectedItem is ProtectionLevel level
            ? level.Group
            : string.Empty;
    }

    private void EnsureProtectionLevelExists(string group)
    {
        EnsureProtectionLevelExists(group, EditProtectionListBox);
        EnsureProtectionLevelExists(group, MoveProtectionListBox);
    }

    private static void EnsureProtectionLevelExists(
        string group,
        ListBox listBox)
    {
        ProtectionLevel level = new(group, group);

        if (!listBox.Items.Contains(level))
            listBox.Items.Add(level);
    }

    private static void SelectProtectionLevel(
        ListBox listBox,
        string group)
    {
        foreach (object? item in listBox.Items)
        {
            if (item is ProtectionLevel level &&
                level.Group == group)
            {
                listBox.SelectedItem = item;
                return;
            }
        }
    }
}