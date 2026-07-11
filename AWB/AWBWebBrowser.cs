using System.Windows.Forms;
using WikiFunctions;

namespace AutoWikiBrowser
{
    class AWBWebBrowser : WebBrowser
    {
        public override bool PreProcessMessage(ref Message msg) 
        {
            // look for and intercept a Ctrl+C key up event to copy selected text to keyboard
            if (msg.Msg == 0x101 && msg.WParam.ToInt32() == (int)Keys.C && ModifierKeys == Keys.Control)
            {
                CopySelectedText();
                return true;
            }

            // Ctrl+J to find selected text in edit text box
            if (msg.Msg == 0x101 && msg.WParam.ToInt32() == (int)Keys.J && ModifierKeys == Keys.Control && TextSelected())
            {
                Variables.MainForm.EditBox.Find(SelectedText(), false, false, Variables.MainForm.TheSession.Page.Title);
                return true;
            }

            // Ctrl+S passed through
            if (msg.Msg == 0x101 && msg.WParam.ToInt32() == (int)Keys.S && ModifierKeys == Keys.Control)
            {
                if(Variables.MainForm.SaveButton.Enabled)
                    Variables.MainForm.Save("AWBWebBrowser");
                else if(Variables.MainForm.StartButton.Enabled)
                    Variables.MainForm.Start("AWBWebBrowser");
                return true;
            }

            // Ctrl+I passed through
            if (msg.Msg == 0x101 && msg.WParam.ToInt32() == (int)Keys.I && ModifierKeys == Keys.Control)
            {
                if(Variables.MainForm.SkipButton.Enabled)
                    Variables.MainForm.SkipPage("AWBWebBrowser", "user");
                return true;
            }

            return base.PreProcessMessage(ref msg);
        }

        /// <summary>
        /// Copies the selected text (if any) to the clipboard
        /// </summary>
        private void CopySelectedText()
        {
            if (Document != null)
            {
                Document.ExecCommand("Copy", false, null);
            }
        }

        /// <summary>
        /// Returns whether text is currently selected in the embedded browser.
        /// </summary>
        /// <remarks>
        /// Browser-specific selection access is delegated to
        /// <see cref="BrowserSelectionProvider"/>. Some systems may report that
        /// the legacy browser interop assembly is available even when selection
        /// access still fails, so this method keeps a defensive try/catch.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if text is currently selected; otherwise, <c>false</c>.
        /// </returns>
        public bool TextSelected()
        {
            if (!Globals.MSHTMLAvailable)
            {
                return false;
            }

            try
            {
                return TextSelectedChecked();
            }
            catch
            {
                // Some systems report that the legacy browser interop assembly
                // is available even though selection access still fails at runtime.
                // See:
                // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_23#Single_click_to_focus_the_edit_box_to_a_line_-_no_longer_works_with_SVN9282
                return false;
            }
        }

        /// <summary>
        /// Returns whether there is currently any text selected
        /// in the embedded browser.
        /// </summary>
        /// <remarks>
        /// Browser-specific selection retrieval is delegated to
        /// <see cref="BrowserSelectionProvider"/>.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if text is currently selected; otherwise, <c>false</c>.
        /// </returns>
        private bool TextSelectedChecked()
        {
            return !string.IsNullOrEmpty(TextRange());
        }

        private string SelectedText()
        {
            var range = TextRange();

            return string.IsNullOrEmpty(range) ? "" : range;
        }
                private string TextRange()
        {
            return BrowserSelectionProvider.GetSelectedText(Document);
        }

        public override void Refresh()
        {
            // webbrowser Refresh calls fail under Mono so silently ignore for the moment
            if(!Globals.UsingMono)
                base.Refresh();
        }

        public new void Navigate(string urlString)
        {
            // webbrowser Navigate calls fail under Mono so silently ignore for the moment
            if (!Globals.UsingMono)
                base.Navigate(urlString, null, null,
                    string.Format("User-Agent: AWBWebBrowser {0}/{1} {2}\r\n",
                        System.Reflection.Assembly.GetExecutingAssembly().GetName(), Tools.VersionString,
                        Tools.DefaultUserAgentString)
                );
        }
    }
}
