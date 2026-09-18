using Avalonia.Controls;
using System.Collections.Generic;
using Twain.Core;
using Twain.Core.Controls;

namespace Twain.UI.Controls;

public partial class NamespacesControl : UserControl
{
    public NamespacesControl()
    {
        InitializeComponent();
        Populate();
    }

    public void Populate()
    {
        SubjectPanel.Children.Clear();
        TalkPanel.Children.Clear();

        AddNamespace(
            SubjectPanel,
            new NSItem(
                new KeyValuePair<int, string>(
                    0,
                    "Main/Article")));

        foreach (KeyValuePair<int, string> item in Variables.Namespaces)
        {
            if (item.Key < 0)
                continue;

            NSItem namespaceItem = new(item);

            if (Namespace.IsTalk(item.Key))
                AddNamespace(TalkPanel, namespaceItem);
            else
                AddNamespace(SubjectPanel, namespaceItem);
        }
    }

    public void Reset()
    {
        SetSelectedNamespaces([0]);
    }

    public List<int> GetSelectedNamespaces()
    {
        List<int> selected = [];

        selected.AddRange(
            GetSelectedNamespaces(SubjectPanel));

        selected.AddRange(
            GetSelectedNamespaces(TalkPanel));

        selected.Sort();

        return selected;
    }

    public void SetSelectedNamespaces(
        ICollection<int> namespaces)
    {
        ArgumentNullException.ThrowIfNull(namespaces);

        SetSelectedNamespaces(
            SubjectPanel,
            namespaces);

        SetSelectedNamespaces(
            TalkPanel,
            namespaces);
    }

    private void SubjectCheckBox_IsCheckedChanged(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetAllChecked(
            SubjectPanel,
            SubjectCheckBox.IsChecked == true);
    }

    private void TalkCheckBox_IsCheckedChanged(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetAllChecked(
            TalkPanel,
            TalkCheckBox.IsChecked == true);
    }

    private static void AddNamespace(
        StackPanel panel,
        NSItem item)
    {
        CheckBox checkBox = new()
        {
            Content = item.Value,
            Tag = item,
            FontSize = 13,
            Padding = new Avalonia.Thickness(2)
        };

        panel.Children.Add(checkBox);
    }

    private static IEnumerable<int> GetSelectedNamespaces(
        StackPanel panel)
    {
        foreach (Control control in panel.Children)
        {
            if (control is CheckBox
                {
                    IsChecked: true,
                    Tag: NSItem item
                })
            {
                yield return item.Key;
            }
        }
    }

    private static void SetSelectedNamespaces(
        StackPanel panel,
        ICollection<int> namespaces)
    {
        foreach (Control control in panel.Children)
        {
            if (control is CheckBox
                {
                    Tag: NSItem item
                } checkBox)
            {
                checkBox.IsChecked =
                    namespaces.Contains(item.Key);
            }
        }
    }

    private static void SetAllChecked(
        StackPanel panel,
        bool isChecked)
    {
        foreach (Control control in panel.Children)
        {
            if (control is CheckBox checkBox)
                checkBox.IsChecked = isChecked;
        }
    }
}