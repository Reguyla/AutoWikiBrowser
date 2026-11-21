namespace Twain.Core.Lists.Providers;

/// <summary>
/// Represents Special Pages in the Make List source selector.
/// </summary>
/// <remarks>
/// The Avalonia Make List workflow handles dialog presentation
/// asynchronously and delegates article generation to
/// <see cref="SpecialPageListService"/>.
///
/// This adapter exists only to register Special Pages as an
/// IListProvider without depending on the legacy WinForms dialog.
/// </remarks>
public sealed class SpecialPageListAdapter : IListProvider
{
    /// <inheritdoc />
    public string DisplayText => "Special page";

    /// <inheritdoc />
    public string UserInputTextBoxText => string.Empty;

    /// <inheritdoc />
    public bool UserInputTextBoxEnabled => false;

    /// <inheritdoc />
    public bool RunOnSeparateThread => true;

    /// <inheritdoc />
    public bool StripUrl => false;

    /// <inheritdoc />
    public void Selected()
    {
        // No action required.
    }

    /// <inheritdoc />
    public List<Article> MakeList(params string[] searchCriteria)
    {
        throw new NotSupportedException(
            "Special Pages requires the asynchronous dialog workflow " +
            "provided by the Twain Make List interface.");
    }
}