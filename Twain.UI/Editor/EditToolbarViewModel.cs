
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Twain.UI.Editor;

public enum EditToolbarDisplayMode
{
    Icons,
    Text,
    IconsAndText
}

public sealed class EditToolbarViewModel : INotifyPropertyChanged
{
    private bool _isVisible = false;
    private EditToolbarDisplayMode _displayMode =
        EditToolbarDisplayMode.Text;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool ShowIcons =>
        DisplayMode != EditToolbarDisplayMode.Text;

    public bool ShowText =>
        DisplayMode != EditToolbarDisplayMode.Icons;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
                return;

            _isVisible = value;
            OnPropertyChanged();
        }
    }


    public EditToolbarDisplayMode DisplayMode
    {
        get => _displayMode;
        set
        {
            if (_displayMode == value)
                return;

            _displayMode = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowIcons));
            OnPropertyChanged(nameof(ShowText));
        }
    }


    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
