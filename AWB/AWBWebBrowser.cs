using System.Windows.Forms;
using WikiFunctions;

namespace AutoWikiBrowser
{
    class AWBWebBrowser : WebBrowser
    {
        private const int WmKeyUp = 0x0101;

        public override bool PreProcessMessage(ref Message msg)
        {
            bool isKeyUp = msg.Msg == WmKeyUp;
            bool isControlPressed = ModifierKeys == Keys.Control;
            int keyCode = msg.WParam.ToInt32();

            if (isKeyUp && isControlPressed && keyCode == (int)Keys.C)
            {
                CopySelectedText();
                return true;
            }

            if (isKeyUp &&
                isControlPressed &&
                keyCode == (int)Keys.J &&
                TextSelected())
            {
                Variables.MainForm.EditBox.Find(
                    SelectedText(),
                    false,
                    false,
                    Variables.MainForm.TheSession.Page.Title);

                return true;
            }

            if (isKeyUp && isControlPressed && keyCode == (int)Keys.S)
            {
                if (Variables.MainForm.SaveButton.Enabled)
                {
                    Variables.MainForm.Save("AWBWebBrowser");
                }
                else if (Variables.MainForm.StartButton.Enabled)
                {
                    Variables.MainForm.Start("AWBWebBrowser");
                }

                return true;
            }

            if (isKeyUp && isControlPressed && keyCode == (int)Keys.I)
            {
                if (Variables.MainForm.SkipButton.Enabled)
                {
                    Variables.MainForm.SkipPage("AWBWebBrowser", "user");
                }

                return true;
            }

            return base.PreProcessMessage(ref msg);
        }

        /// <summary>
        /// Copies the selected text, if any, to the clipboard.
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

        private bool TextSelectedChecked()
        {
            return !string.IsNullOrEmpty(GetSelectedText());
        }

        private string SelectedText()
        {
            return GetSelectedText() ?? string.Empty;
        }

        private string GetSelectedText()
        {
            return BrowserSelectionProvider.GetSelectedText(Document);
        }

        public override void Refresh()
        {
            // Legacy WebBrowser refresh calls may fail under Mono,
            // so skip the operation when running under Mono.
            if (!Globals.UsingMono)
            {
                base.Refresh();
            }
        }

        public new void Navigate(string urlString)
        {
            // Legacy WebBrowser navigation calls may fail under Mono,
            // so skip the operation when running under Mono.
            if (!Globals.UsingMono)
            {
                base.Navigate(
                    urlString,
                    null,
                    null,
                    string.Format(
                        "User-Agent: AWBWebBrowser {0}/{1} {2}\r\n",
                        System.Reflection.Assembly
                            .GetExecutingAssembly()
                            .GetName(),
                        Tools.VersionString,
                        Tools.DefaultUserAgentString));
            }
        }
    }
}