using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
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

    /// <summary>
    /// Displays the profile login workflow for the active session.
    /// </summary>
    public async Task ShowLoginAsync()
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        ProfilesWindow window =
            new(viewModel.Session);

        bool loggedIn =
            await window.ShowDialog<bool>(this);

        if (loggedIn)
        {
            viewModel.LoginCompleted();
        }
    }

    private async void LoginProfilesMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await ShowLoginAsync();
    }

    private void ExitMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            Close();
        }

    /// <summary>
    /// Opens the custom module window.
    /// </summary>
    public async void OpenCustomModules()
    {
        CustomModule window = new();

        await window.ShowDialog(this);
    }

    private void MakeModuleMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenCustomModules();
    }

    private async void ExternalProcessingMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
        {
            ExternalProgramWindow window = new();

            await window.ShowDialog<bool>(this);
        }

    /// <summary>
    /// Opens the regular expression tester window.
    /// </summary>
    public async void OpenRegexTester()
    {
        RegexTesterWindow window = new();

        await window.ShowDialog(this);
    }

    private void RegexTesterMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenRegexTester();
    }

    /// <summary>
    /// Opens the database scanner window.
    /// </summary>
    public async void OpenDatabaseScanner()
    {
        DatabaseScannerWindow window = new();

        await window.ShowDialog(this);
    }

    private void DatabaseScannerMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenDatabaseScanner();
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
    public async void OpenPreferences()
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        PreferencesWindow window =
            new(
                viewModel.Preferences.LanguageCode,
                viewModel.Preferences.Project,
                viewModel.Preferences.CustomProject,
                viewModel.Preferences.Protocol);

        window.LoadPreferences(
            viewModel.Preferences);

        bool? result =
            await window.ShowDialog<bool?>(this);

        if (result == true)
        {
            window.ApplyPreferences(
                viewModel.Preferences);

            viewModel.ApplySitePreferences();
        }
    }

    /// <summary>
    /// Opens the application preferences window from the main menu.
    /// </summary>
    private void PreferencesMenuItem_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenPreferences();
    }
}