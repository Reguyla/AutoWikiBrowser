/*
Copyright (C) 2009

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

*/

using System.Drawing;
using System.Windows.Forms;
using Twain.Core.Editing;

namespace Twain.Core.Controls;

/// <summary>
/// Provides the legacy WinForms article-editing control used to display and
/// modify the current article text.
/// </summary>
/// <remarks>
/// <para>
/// The control extends <see cref="RichTextBox"/> with AWB-specific handling for
/// line endings, programmatic text updates, article searching, selection
/// management, and syntax highlighting.
/// </para>
/// <para>
/// Programmatic changes to <see cref="Text"/> and <see cref="SelectedText"/>
/// suppress <see cref="TextChanged"/> notifications so that internal editor
/// updates are not treated as user edits.
/// </para>
/// </remarks>
// TODO(Twain): Separate editor-independent behavior from this WinForms control
// so that the same article-editing operations can be used by Monaco.
// TODO(Twain): Move the remaining WinForms-specific editor implementation out
// of Twain.Core once an editor abstraction has been established.
public class ArticleTextBox : RichTextBox
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleTextBox"/> class
    /// with the legacy AWB RichTextBox configuration.
    /// </summary>
    public ArticleTextBox()
    {
        LanguageOption = RichTextBoxLanguageOptions.DualFont;
        EnableAutoDragDrop = true;
        InitializeComponent();
    }

    /// <summary>
    /// Maintains the state of the current incremental article search.
    /// </summary>
    private readonly ArticleSearchHelper.ArticleSearchState SearchState = new();

    /// <summary>
    /// Gets or sets the complete article text.
    /// </summary>
    /// <remarks>
    /// When running under the Windows RichTextBox implementation, line endings
    /// are normalized from line-feed characters to carriage-return/line-feed
    /// pairs when text is read. Programmatic assignments suppress
    /// <see cref="TextChanged"/> notifications.
    /// </remarks>
    public override string Text
    {
        set
        {
            _suppressTextChanged = true;

            try
            {
                base.Text = value;
            }
            finally
            {
                _suppressTextChanged = false;
            }
        }
    }

    /// <summary>
    /// Gets or sets the text contained in the current editor selection.
    /// </summary>
    /// <remarks>
    /// Selected text is normalized to carriage-return/line-feed line endings
    /// when read. Programmatic replacement of the selection suppresses
    /// <see cref="TextChanged"/> notifications.
    /// </remarks>
    public override string SelectedText
    {
        set
        {
            _suppressTextChanged = true;

            try
            {
                base.SelectedText = value;
            }
            finally
            {
                _suppressTextChanged = false;
            }
        }
    }

    /// <summary>
    /// Gets the underlying unformatted text stored by the base
    /// <see cref="RichTextBox"/> without applying AWB-specific line-ending
    /// normalization.
    /// </summary>
    public string RawText
    {
        get { return base.Text; }
    }


    /// <summary>
    /// Raises the <see cref="TextChanged"/> event when the article text is
    /// changed outside a programmatically locked update.
    /// </summary>
    /// <param name="e">
    /// Event data associated with the text change.
    /// </param>
    /// <remarks>
    /// Programmatic updates performed through <see cref="Text"/> or
    /// <see cref="SelectedText"/> temporarily lock the control so that those
    /// changes do not trigger edit-processing behavior intended for user edits.
    /// </remarks>
    protected override void OnTextChanged(EventArgs e)
    {
        if (!_suppressTextChanged)
        {
            base.OnTextChanged(e);
        }
    }

    // TODO(Twain): Verify whether the RichTextBox AutoWordSelection workaround is
    // still required on supported .NET/Windows versions before carrying it into
    // the legacy editor adapter.
    /// <summary>
    /// Applies RichTextBox-specific initialization after the native control
    /// handle has been created.
    /// </summary>
    /// <param name="e">
    /// Event data associated with creation of the control handle.
    /// </param>
    /// <remarks>
    /// <see cref="RichTextBox.AutoWordSelection"/> is toggled after handle
    /// creation to work around the legacy RichTextBox initialization behavior
    /// that can otherwise leave automatic word selection enabled unexpectedly.
    /// </remarks>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Work around the RichTextBox AutoWordSelection initialization bug
        // by toggling the property after the native control handle is created.
        // Bug fix for AutoWordSelection:
        // http://msdn.microsoft.com/en-us/library/system.windows.forms.richtextbox.autowordselection.aspx
        if (!AutoWordSelection)
        {
            AutoWordSelection = true;
            AutoWordSelection = false;
        }
    }

    /// <summary>
    /// Indicates whether the RichTextBox automatic keyboard-layout behavior has
    /// already been disabled for this control instance.
    /// </summary>
    private bool AutoKeyboardDisabled;

    // TODO(Twain): Verify whether disabling RichTextBox AutoKeyboard is still
    // required on supported .NET/Windows versions before carrying this workaround
    // into the legacy editor adapter.
    /// <summary>
    /// Disables the RichTextBox automatic keyboard-layout behavior when the editor
    /// first receives focus.
    /// </summary>
    /// <param name="e">
    /// Event data associated with the control receiving input focus.
    /// </param>
    /// <remarks>
    /// The RichTextBox can enable <see cref="RichTextBoxLanguageOptions.AutoKeyboard"/>
    /// and change the user's keyboard layout automatically. This workaround removes
    /// that option the first time the control is entered.
    /// </remarks>
    protected override void OnEnter(EventArgs e)
    {
        // Prevent the RichTextBox from automatically changing the user's
        // keyboard layout when the editor receives focus.
        if (!AutoKeyboardDisabled)
        {
            LanguageOption &= ~RichTextBoxLanguageOptions.AutoKeyboard;
            AutoKeyboardDisabled = true;
        }

        base.OnEnter(e);
    }

    /// <summary>
    /// Clears the state associated with the current incremental find operation.
    /// </summary>
    public void ResetFind()
    {
        SearchState.Reset();
    }

    /// <summary>
    /// Indicates whether the control is being updated programmatically and
    /// should temporarily suppress <see cref="TextChanged"/> notifications.
    /// </summary>
    private bool _suppressTextChanged;

    /// <summary>
    /// Finds and selects the next occurrence of the specified search expression
    /// in the current article text.
    /// </summary>
    /// <param name="strRegex">
    /// The text or regular expression to search for.
    /// </param>
    /// <param name="isRegex">
    /// <see langword="true"/> when <paramref name="strRegex"/> should be interpreted
    /// as a regular expression; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="caseSensitive">
    /// <see langword="true"/> to perform a case-sensitive search; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="articleName">
    /// The current article name used when expanding AWB search keywords.
    /// </param>
    /// <remarks>
    /// The first call begins searching at the current selection position.
    /// Subsequent calls continue from the previous match. When no further match
    /// exists, the selection is reset to the start of the editor and the
    /// incremental search state is cleared.
    ///
    /// After processing the search, the editor receives focus and the resulting
    /// selection is scrolled into view.
    /// </remarks>

    public void Find(
        string strRegex,
        bool isRegex,
        bool caseSensitive,
        string articleName)
    {
        string articleText = Tools.ConvertFromLocalLineEndings(RawText);

        Match match = ArticleSearchHelper.FindNext(
            articleText,
            strRegex,
            isRegex,
            caseSensitive,
            articleName,
            SelectionStart,
            SearchState);

        if (match == null)
        {
            SelectionStart = 0;
            SelectionLength = 0;
        }
        else
        {
            SelectionStart = match.Index;
            SelectionLength = match.Length;
        }

        Focus();
        ScrollToCaret();
    }

    /// <summary>
    /// Finds all occurrences of the specified search expression in the current
    /// article text.
    /// </summary>
    /// <param name="strRegex">
    /// The text or regular expression to search for.
    /// </param>
    /// <param name="isRegex">
    /// <see langword="true"/> when <paramref name="strRegex"/> should be interpreted
    /// as a regular expression; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="caseSensitive">
    /// <see langword="true"/> to perform a case-sensitive search; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="articleName">
    /// The current article name used when expanding AWB search keywords.
    /// </param>
    /// <returns>
    /// A dictionary containing the zero-based starting index and length of each
    /// non-empty match. An empty dictionary is returned when the search expression
    /// is empty or no matches are found.
    /// </returns>
    /// <remarks>
    /// Article text is converted from local line endings before matching so that
    /// search offsets correspond to the normalized text used by the search logic.
    /// </remarks>

    public Dictionary<int, int> FindAll(
        string strRegex,
        bool isRegex,
        bool caseSensitive,
        string articleName)
    {
        string articleText = Tools.ConvertFromLocalLineEndings(RawText);

        return ArticleSearchHelper.FindAll(
            articleText,
            strRegex,
            isRegex,
            caseSensitive,
            articleName);
    }

    /// <summary>
    /// Selects a range of text within the article editor and optionally scrolls
    /// the selection into view.
    /// </summary>
    /// <param name="inputIndex">
    /// The zero-based index of the first character to select.
    /// </param>
    /// <param name="inputLength">
    /// The number of characters to include in the selection.
    /// </param>
    /// <param name="scrollToCaret">
    /// <see langword="true"/> to scroll the resulting caret position into view;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// The selection is changed only when the supplied range is valid and contains
    /// at least one character. Scrolling is performed independently of whether the
    /// selection was changed.
    /// </remarks>
    private void SetEditBoxSelection(
        int inputIndex,
        int inputLength,
        bool scrollToCaret)
    {
        if (inputIndex >= 0 &&
            inputLength > 0 &&
            (inputIndex + inputLength) <= TextLength)
        {
            SelectionStart = inputIndex;
            SelectionLength = inputLength;
        }

        if (scrollToCaret)
        {
            ScrollToCaret();
        }
    }

    /// <summary>
    /// Selects a range of text within the edit box without changing the
    /// current scroll position.
    /// </summary>
    /// <param name="inputIndex">
    /// The zero-based index of the first character to select.
    /// </param>
    /// <param name="inputLength">
    /// The number of characters to include in the selection.
    /// </param>
    private void SetEditBoxSelection(int inputIndex, int inputLength)
    {
        SetEditBoxSelection(inputIndex, inputLength, false);
    }

    /// <summary>
    /// Initializes the control's designer-managed components and applies
    /// the default RichTextBox settings used by AWB.
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        // URLs are detected by AWB itself. Disable the RichTextBox's built-in
        // URL detection to avoid automatic formatting and event handling.
        DetectUrls = false;

        ResumeLayout(false);
    }

    /// <summary>
    /// Converts an article-text range to the corresponding RichTextBox selection
    /// range and selects it in the editor.
    /// </summary>
    /// <param name="inputIndex">
    /// The zero-based index within the article text.
    /// </param>
    /// <param name="inputLength">
    /// The number of article-text characters to select.
    /// </param>
    /// <param name="scrollToCaret">
    /// <see langword="true"/> to scroll the resulting caret position into view;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// Article text uses normalized line-feed line endings, while the public
    /// <see cref="Text"/> representation uses local line endings. The RichTextBox
    /// selection indexes therefore require adjustment for expanded newline
    /// characters.
    /// </remarks>
    public void SetArticleTextSelection(
        int inputIndex,
        int inputLength,
        bool scrollToCaret)
    {
        string text = Text;

        int newlinesToIndex =
            WikiRegexes.Newline.Matches(
                text.Substring(0, inputIndex)).Count;

        int newlinesInSelection =
            WikiRegexes.Newline.Matches(
                text.Substring(inputIndex, inputLength)).Count;

        SetEditBoxSelection(
            inputIndex - newlinesToIndex,
            inputLength - newlinesInSelection,
            scrollToCaret);
    }

    /// <summary>
    /// Applies legacy wiki-syntax highlighting to the current article text.
    /// </summary>
    /// <remarks>
    /// The method identifies supported wiki markup using <see cref="WikiRegexes"/>
    /// and applies RichTextBox-specific foreground colors, background colors, and
    /// font styles to the corresponding text ranges.
    ///
    /// Highlighting is applied directly to the editor selection and formatting
    /// state. This behavior is specific to the legacy WinForms editor and should
    /// not be used as the presentation model for the Monaco implementation.
    /// </remarks>
    // TODO(Twain): Separate wiki-syntax recognition from RichTextBox presentation.
    // The editor-independent layer should describe highlighted text ranges and
    // semantic highlight types, while each editor implementation determines how
    // those highlights are rendered.
    public void HighlightSyntax()
    {
        // reset background color to avoid issues on re-parse
        SetEditBoxSelection(0, RawText.Length);
        SelectionBackColor = Color.White;

        Font currentFont = SelectionFont;
        Font boldFont = new Font(currentFont.FontFamily, currentFont.Size, FontStyle.Bold);
        Font italicFont = new Font(currentFont.FontFamily, currentFont.Size, FontStyle.Italic);
        Font boldItalicFont = new Font(currentFont.FontFamily, currentFont.Size, FontStyle.Bold | FontStyle.Italic);

        // headings text in bold
        foreach (Match m in WikiRegexes.Headings.Matches(RawText))
        {
            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionFont = boldFont;
        }

        // templates grey background
        foreach (Match m in WikiRegexes.NestedTemplates.Matches(RawText))
        {
            SetEditBoxSelection(m.Index, m.Length);
            SelectionBackColor = Color.LightGray;
        }

        // * items grey background
        foreach (Match m in WikiRegexes.StarRows.Matches(RawText))
        {
            SetEditBoxSelection(m.Index, m.Length);
            SelectionBackColor = Color.LightGray;

            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionFont = boldFont;
        }

        // template names dark blue font
        foreach (Match m in WikiRegexes.TemplateName.Matches(RawText))
        {
            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionColor = Color.DarkBlue;
        }

        // refs grey background
        foreach (Match m in WikiRegexes.Refs.Matches(RawText))
        {
            SetEditBoxSelection(m.Index, m.Length);
            SelectionBackColor = Color.LightGray;
        }

        // external links grey background, blue bold
        foreach (Match m in WikiRegexes.ExternalLinks.Matches(RawText))
        {
            SetEditBoxSelection(m.Index, m.Length);
            SelectionColor = Color.Blue;
            SelectionFont = boldFont;
        }

        // Image/file links green background
        foreach (Match m in WikiRegexes.FileNamespaceLink.Matches(RawText))
        {
            SetEditBoxSelection(m.Index, m.Length);
            SelectionBackColor = Color.LightGreen;
        }

        // italics
        foreach (Match m in WikiRegexes.Italics.Matches(RawText))
        {
            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionFont = italicFont;
        }

        // bold
        foreach (Match m in WikiRegexes.Bold.Matches(RawText))
        {
            // reset anything incorrectly done by italics earlier
            SetEditBoxSelection(m.Index, m.Length);
            SelectionFont = currentFont;

            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionFont = boldFont;
        }

        // bold italics
        foreach (Match m in WikiRegexes.BoldItalics.Matches(RawText))
        {
            // reset anything incorrectly done by italics/bold earlier
            SetEditBoxSelection(m.Index, m.Length);
            SelectionFont = currentFont;

            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionFont = boldItalicFont;
        }

        // piped wikilink text in blue, piped part in bold
        foreach (Match m in WikiRegexes.PipedWikiLink.Matches(RawText))
        {
            SetEditBoxSelection(m.Groups[2].Index, m.Groups[2].Length);
            SelectionColor = Color.Blue;
            SelectionFont = boldFont;

            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionColor = Color.Blue;
        }

        // unpiped wikilinks in blue and bold
        foreach (Match m in WikiRegexes.UnPipedWikiLink.Matches(RawText))
        {
            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionColor = Color.Blue;
            SelectionFont = boldFont;
        }

        // pipe trick: in blue bold too
        foreach (Match m in WikiRegexes.WikiLinksOnlyPlusWord.Matches(RawText))
        {
            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionColor = Color.Blue;
            SelectionFont = boldFont;
        }

        // cats grey background
        foreach (Match m in WikiRegexes.Category.Matches(RawText))
        {
            SetEditBoxSelection(m.Index, m.Length);
            SelectionBackColor = Color.LightGray;
            SelectionFont = currentFont;
            SelectionColor = Color.Black;

            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionColor = Color.Blue;
        }

        // interwikis dark grey background
        foreach (Match m in WikiRegexes.PossibleInterwikis.Matches(RawText))
        {
            SetEditBoxSelection(m.Index, m.Length);
            SelectionBackColor = Color.Gray;
            SelectionFont = currentFont;

            SetEditBoxSelection(m.Groups[2].Index, m.Groups[2].Length);
            SelectionColor = Color.Blue;

            SetEditBoxSelection(m.Groups[1].Index, m.Groups[1].Length);
            SelectionColor = Color.Black;
        }

        // comments dark orange background
        foreach (Match m in WikiRegexes.Comments.Matches(RawText))
        {
            SetEditBoxSelection(m.Index, m.Length);
            SelectionBackColor = Color.PaleGoldenrod;
        }
    }

    /// <summary>
    /// Applies a background color to a range expressed in article-text
    /// coordinates.
    /// </summary>
    /// <param name="inputIndex">
    /// The zero-based index within the article text.
    /// </param>
    /// <param name="inputLength">
    /// The number of article-text characters to highlight.
    /// </param>
    /// <param name="backColor">
    /// The background color to apply to the selected range.
    /// </param>
    public void HighlightArticleTextRange(
        int inputIndex,
        int inputLength,
        Color backColor,
        bool scrollToCaret = false)
    {
        SetArticleTextSelection(
            inputIndex,
            inputLength,
            scrollToCaret);

        SelectionBackColor = backColor;
    }

    /// <summary>
    /// Clears background highlighting from the entire article editor.
    /// </summary>
    public void ClearBackgroundHighlighting()
    {
        SelectAll();
        SelectionBackColor = Color.White;
    }

    /// <summary>
    /// Gets the current zero-based editor caret position.
    /// </summary>
    public int CaretPosition => SelectionStart;

    /// <summary>
    /// Moves the editor caret to the specified position and optionally scrolls it
    /// into view.
    /// </summary>
    /// <param name="position">
    /// The zero-based editor position at which to place the caret.
    /// </param>
    /// <param name="scrollToCaret">
    /// <see langword="true"/> to scroll the caret into view; otherwise,
    /// <see langword="false"/>.
    /// </param>
    public void SetCaretPosition(
        int position,
        bool scrollToCaret)
    {
        Select(position, 0);

        if (scrollToCaret)
        {
            ScrollToCaret();
        }
    }

    /// <summary>
    /// Gets the current caret position expressed in normalized article-text
    /// coordinates.
    /// </summary>
    public int ArticleTextCaretPosition
    {
        get
        {
            int caretPosition = SelectionStart;

            string textBeforeCaret =
                Text[..caretPosition];

            int newlineOffset =
                WikiRegexes.Newline.Matches(
                    textBeforeCaret).Count;

            return caretPosition + newlineOffset;
        }
    }
}