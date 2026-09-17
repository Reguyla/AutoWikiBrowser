using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Twain.UI.FindReplace;

/// <summary>
/// Provides the state used by the Avalonia find and replace interface.
/// </summary>
public partial class FindReplaceViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindReplaceViewModel"/> class.
    /// </summary>
    public FindReplaceViewModel()
    {
    }

    /// <summary>
    /// Gets the editable find and replace rules.
    /// </summary>
    public ObservableCollection<FindReplaceRowViewModel> Rows { get; } = [];

    /// <summary>
    /// Adds a new empty find and replace rule.
    /// </summary>
    [RelayCommand]
    private void AddRule()
    {
        Rows.Add(
            new FindReplaceRowViewModel());
    }
}