using Avalonia.Controls;
using Avalonia.Interactivity;
using Twain.CustomModules;
using Twain.UI.Controls;
using Twain.UI.Controls.Lists;
using Twain.UI.DBScanner;
using Twain.UI.ExternalPrograms;
using Twain.UI.Preferences;
using Twain.UI.Profiles;
using Twain.UI.Shell;

namespace Twain.UI.Views.Shell;

/// <summary>
/// Hosts the top-level Twain user interface.
/// </summary>
public partial class ShellWindow : Window
{
    /// <summary>
    /// Initializes the shell window.
    /// </summary>
    public ShellWindow()
    {
        InitializeComponent();
    }

    private async void LoginProfilesMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel)
            return;

        ProfilesWindow window =
            new(viewModel.Session);

        await window.ShowDialog<bool>(this);
    }

    private void ExitMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            Close();
        }

    private async void MakeModuleMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            CustomModule window = new();

            await window.ShowDialog(this);
        }

    private async void ExternalProcessingMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            ExternalProgramWindow window = new();

            await window.ShowDialog<bool>(this);
        }

    private async void RegexTesterMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            RegexTesterWindow window = new();

            await window.ShowDialog(this);
        }

    private async void DatabaseScannerMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            DatabaseScannerWindow window = new();

            await window.ShowDialog(this);
        }

    private async void ListComparerMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            ListComparerWindow window = new();

            await window.ShowDialog(this);
        }

    private async void ListSplitterMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            ListSplitterWindow window = new();

            await window.ShowDialog(this);
        }

    /// <summary>
    /// Opens the application preferences window.
    /// </summary>
    private async void PreferencesMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        PreferencesWindow window = new();

        await window.ShowDialog<bool?>(this);
    }
}