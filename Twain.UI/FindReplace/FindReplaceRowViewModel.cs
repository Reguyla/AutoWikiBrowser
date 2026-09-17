using CommunityToolkit.Mvvm.ComponentModel;
using CoreFindReplace = Twain.Core.Parse.FindReplace;

namespace Twain.UI.FindReplace;

/// <summary>
/// Provides editable state for a single find and replace rule.
/// </summary>
public partial class FindReplaceRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _find = string.Empty;

    [ObservableProperty]
    private string _replace = string.Empty;

    /// <summary>
    /// Creates an editable row from a Core editor row.
    /// </summary>
    public static FindReplaceRowViewModel FromEditorRow(
        CoreFindReplace.EditorRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new FindReplaceRowViewModel
        {
            Find = row.Find,
            Replace = row.Replace
        };
    }
}