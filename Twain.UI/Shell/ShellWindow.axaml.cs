using Avalonia.Controls;
using Avalonia.Interactivity;
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
}