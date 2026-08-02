using CommunityToolkit.Mvvm.ComponentModel;

namespace Twain.UI.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}
