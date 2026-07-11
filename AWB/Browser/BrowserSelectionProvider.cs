using System.Windows.Forms;
using mshtml;

namespace AutoWikiBrowser
{
    /// <summary>
    /// Provides access to the current text selection from the embedded browser.
    /// The current implementation uses the legacy Microsoft.mshtml DOM.
    /// </summary>
    internal static class BrowserSelectionReader
    {
        /// <summary>
        /// Returns the currently selected browser text, or null when no text
        /// selection is available.
        /// </summary>
        internal static string GetSelectedText(HtmlDocument document)
        {
            if (document == null)
            {
                return null;
            }

            if (!(document.DomDocument is IHTMLDocument2 htmlDocument))
            {
                return null;
            }

            IHTMLSelectionObject selection = htmlDocument.selection;

            if (selection == null)
            {
                return null;
            }

            if (!(selection.createRange() is IHTMLTxtRange textRange))
            {
                return null;
            }

            return textRange.text;
        }
    }
}