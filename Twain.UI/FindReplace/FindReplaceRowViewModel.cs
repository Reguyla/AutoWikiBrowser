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

    [ObservableProperty]
    private bool _caseSensitive;

    [ObservableProperty]
    private bool _isRegex;

    [ObservableProperty]
    private bool _multiline;

    [ObservableProperty]
    private bool _singleline;

    [ObservableProperty]
    private bool _minor;

    [ObservableProperty]
    private bool _beforeOrAfter;

    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private string _comment = string.Empty;

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
            Replace = row.Replace,
            CaseSensitive = row.CaseSensitive,
            IsRegex = row.IsRegex,
            Multiline = row.Multiline,
            Singleline = row.Singleline,
            Minor = row.Minor,
            BeforeOrAfter = row.BeforeOrAfter,
            Enabled = row.Enabled,
            Comment = row.Comment
        };
    }

    /// <summary>
    /// Creates a Core editor row from the current editable state.
    /// </summary>
    /// <returns>
    /// A Core editor row containing the current find and replace rule.
    /// </returns>
    public CoreFindReplace.EditorRow ToEditorRow()
    {
        return new CoreFindReplace.EditorRow(
            Find,
            Replace,
            CaseSensitive,
            IsRegex,
            Multiline,
            Singleline,
            Minor,
            BeforeOrAfter,
            Enabled,
            Comment);
    }
}