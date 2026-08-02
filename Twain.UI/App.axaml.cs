using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Twain.UI.ViewModels;
using Twain.UI.Views;

namespace Twain.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ShellWindow
            {
                DataContext = new ShellViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}