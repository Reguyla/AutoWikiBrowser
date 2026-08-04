/*
(C) 2007 Sam Reed, Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

using System.Globalization;
using System.Windows.Forms;
using Twain.Core.Controls;
using Twain.Core.Controls.Lists;

namespace Twain.Core.Logging;

/// <summary>
/// Displays successful and failed article actions and allows logged articles
/// to be added back to the list maker.
/// </summary>
public partial class ArticleActionLogControl : UserControl
{
    private ListMaker? _listMaker;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ArticleActionLogControl"/> class.
    /// </summary>
    public ArticleActionLogControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Associates the control with a list maker and initializes the log-list
    /// column widths.
    /// </summary>
    /// <param name="rlistMaker">
    /// The list maker that receives articles added from the action logs.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="rlistMaker"/> is <see langword="null"/>.
    /// </exception>
    public void Initialise(ListMaker rlistMaker)
    {
        ArgumentNullException.ThrowIfNull(rlistMaker);

        _listMaker = rlistMaker;

        ResizeListView(lvFailed);
        ResizeListView(lvSuccessful);
    }

    /// <summary>
    /// Adds an article action result to the successful or failed action log.
    /// </summary>
    /// <param name="page">
    /// The name of the article on which the action was performed.
    /// </param>
    /// <param name="succeeded">
    /// Whether the article action completed successfully.
    /// </param>
    /// <param name="action">
    /// The article action that was performed.
    /// </param>
    /// <param name="message">
    /// Additional information describing the result.
    /// </param>
    public void LogArticleAction(
        string page,
        bool succeeded,
        ArticleAction action,
        string message)
    {
        ListViewItem item = new(page);

        item.SubItems.Add(
            action.ToString());

        item.SubItems.Add(
            DateTime.Now.ToString(
                CultureInfo.InvariantCulture));

        item.SubItems.Add(message);

        NoFlickerExtendedListView targetList =
            succeeded
                ? lvSuccessful
                : lvFailed;

        targetList.Items.Add(item);
        ResizeListView(targetList);
    }

    /// <summary>
    /// Returns the ListView object from which the menu item was clicked
    /// </summary>
    ///
    private static ListView MenuItemOwner(object sender)
    {
        /* we seem to sometimes be receiving a ToolStripMenuItem, and sometimes a ContextMenuStrip...
         * I've no idea why, but in the meantime this version of the function handles both. */

        if (sender is ContextMenuStrip)
            return ((ListView)((ContextMenuStrip)sender).SourceControl);

        if (sender is ToolStripMenuItem)
            return (ListView)(((ContextMenuStrip)((ToolStripMenuItem)sender).Owner).SourceControl);
        throw new ArgumentException("Object of unknown type passed to LogControl.MenuItemOwner()", "sender");
    }

    private LogFileType GetFilePrefs()
    {
        if (saveListDialog.ShowDialog() != DialogResult.OK)
            return 0;
        return (LogFileType)saveListDialog.FilterIndex;
    }

    #region Event Handlers

    /// <summary>
    /// Adds the selected list-view entries to the article list.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void addSelectedToArticleListToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        AddToListMaker(
            MenuItemOwner(sender).SelectedItems);
    }

    /// <summary>
    /// Converts the supplied list-view items into articles and adds them to the
    /// list maker.
    /// </summary>
    /// <param name="sic">
    /// The list-view items whose text is used as article names.
    /// </param>
    private void AddToListMaker(
        System.Collections.IEnumerable sic)
    {
        ArgumentNullException.ThrowIfNull(sic);

        List<Article> list = new();

        foreach (ListViewItem item in sic)
        {
            list.Add(
                new Article(item.Text));
        }

        _listMaker.Add(list);
    }

    /// <summary>
    /// Opens the focused log entry in the default web browser when the log list is
    /// double-clicked.
    /// </summary>
    /// <param name="sender">
    /// The list view that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the double-click operation.
    /// </param>
    private void LogLists_DoubleClick(
        object sender,
        EventArgs e)
    {
        if (sender is not ListView listView ||
            listView.FocusedItem is not AWBLogListener logEntry)
        {
            return;
        }

        logEntry.OpenInBrowser();
    }

    /// <summary>
    /// Resizes the list-view columns to fit their current contents.
    /// </summary>
    /// <param name="lstView">
    /// The list view whose columns should be resized.
    /// </param>
    private static void ResizeListView(
        NoFlickerExtendedListView lstView)
    {
        ArgumentNullException.ThrowIfNull(lstView);

        lstView.ResizeColumns(true);
    }

    /// <summary>
    /// Saves the contents of a log list to the selected file.
    /// </summary>
    /// <param name="listview">
    /// The list view whose entries should be written.
    /// </param>
    private void SaveListView(
        ListView listview)
    {
        ArgumentNullException.ThrowIfNull(listview);

        LogFileType logFileType =
            GetFilePrefs();

        if (logFileType == 0)
        {
            return;
        }

        StringBuilder articleList =
            new(listview.Items.Count * 32);

        foreach (ListViewItem item in listview.Items)
        {
            articleList.AppendLine(item.Text);
        }

        Tools.WriteTextFileAbsolutePath(
            articleList.ToString(),
            saveListDialog.FileName,
            false);
    }

    /// <summary>
    /// Adds all failed or ignored article entries to the list maker.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnAddToList_Click(
        object sender,
        EventArgs e)
    {
        AddToListMaker(lvFailed.Items);
    }

    /// <summary>
    /// Saves the successful article entries to a file selected by the user.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnSaveSaved_Click(
        object sender,
        EventArgs e)
    {
        SaveListView(lvSuccessful);
    }

    /// <summary>
    /// Saves the failed or ignored article entries to a file selected by the
    /// user.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnSaveIgnored_Click(
        object sender,
        EventArgs e)
    {
        SaveListView(lvFailed);
    }

    /// <summary>
    /// Removes all successful article entries from the log.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnClearSaved_Click(
        object sender,
        EventArgs e)
    {
        lvSuccessful.Items.Clear();
    }

    /// <summary>
    /// Removes all failed or ignored article entries from the log.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnClearIgnored_Click(
        object sender,
        EventArgs e)
    {
        lvFailed.Items.Clear();
    }

    /// <summary>
    /// Copies the selected entries to the clipboard and removes them from their
    /// list.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void cutToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        Tools.Copy(
            MenuItemOwner(sender));

        RemoveSelected(sender);
    }

    /// <summary>
    /// Copies the selected entries to the clipboard.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void copyToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        Tools.Copy(
            MenuItemOwner(sender));
    }

    /// <summary>
    /// Removes the selected entries from the list associated with the supplied
    /// context-menu item.
    /// </summary>
    /// <param name="sender">
    /// The context-menu item whose owning list contains the selected entries.
    /// </param>
    private static void RemoveSelected(object sender)
    {
        ListView listView =
            MenuItemOwner(sender);

        ListViewItem[] selectedItems =
            new ListViewItem[listView.SelectedItems.Count];

        listView.SelectedItems.CopyTo(
            selectedItems,
            0);

        foreach (ListViewItem item in selectedItems)
        {
            item.Remove();
        }
    }

    /// <summary>
    /// Selects every entry in the list associated with the supplied context-menu
    /// item.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void selectAllToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        ListView listView =
            MenuItemOwner(sender);

        listView.BeginUpdate();

        try
        {
            foreach (ListViewItem item in listView.Items)
            {
                item.Selected = true;
            }
        }
        finally
        {
            listView.EndUpdate();
        }
    }

    /// <summary>
    /// Clears the selection in the list associated with the supplied context-menu
    /// item.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void selectNoneToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        ListView listView =
            MenuItemOwner(sender);

        listView.BeginUpdate();

        try
        {
            foreach (ListViewItem item in listView.Items)
            {
                item.Selected = false;
            }
        }
        finally
        {
            listView.EndUpdate();
        }
    }

    /// <summary>
    /// Opens each selected article in the default web browser.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void openInBrowserToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        ListView listView =
            MenuItemOwner(sender);

        foreach (ListViewItem item in listView.SelectedItems)
        {
            Tools.OpenArticleInBrowser(
                item.Text);
        }
    }

    /// <summary>
    /// Opens the revision history for each selected article in the default web
    /// browser.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void openHistoryInBrowserToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        ListView listView =
            MenuItemOwner(sender);

        foreach (ListViewItem item in listView.SelectedItems)
        {
            Tools.OpenArticleHistoryInBrowser(
                item.Text);
        }
    }

    /// <summary>
    /// Removes the selected entries from the list associated with the supplied
    /// context-menu item.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void removeToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        RemoveSelected(sender);
    }

    /// <summary>
    /// Removes all entries from the list associated with the supplied
    /// context-menu item.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void clearToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        MenuItemOwner(sender).Items.Clear();
    }

    /// <summary>
    /// Updates the enabled state of context-menu commands before the menu is
    /// displayed.
    /// </summary>
    /// <param name="sender">
    /// The context menu that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-opening operation.
    /// </param>
    private void mnuListView_Opening(
        object sender,
        EventArgs e)
    {
        ListView listView =
            MenuItemOwner(sender);

        bool hasSelection =
            listView.SelectedItems.Count > 0;

        bool hasItems =
            listView.Items.Count > 0;

        addSelectedToArticleListToolStripMenuItem.Enabled =
            hasSelection;
        cutToolStripMenuItem.Enabled =
            hasSelection;
        copyToolStripMenuItem.Enabled =
            hasSelection;
        removeToolStripMenuItem.Enabled =
            hasSelection;
        openInBrowserToolStripMenuItem.Enabled =
            hasSelection;
        openHistoryInBrowserToolStripMenuItem.Enabled =
            hasSelection;
        clearToolStripMenuItem.Enabled =
            hasSelection;

        selectAllToolStripMenuItem.Enabled =
            hasItems;
        selectNoneToolStripMenuItem.Enabled =
            hasItems;
    }

    /// <summary>
    /// Opens the log page for each selected article in the default web browser.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void openLogInBrowserToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        ListView listView =
            MenuItemOwner(sender);

        foreach (ListViewItem item in listView.SelectedItems)
        {
            Tools.OpenArticleLogInBrowser(
                item.Text);
        }
    }

    /// <summary>
    /// Adds all successful article entries to the list maker.
    /// </summary>
    /// <param name="sender">
    /// The button that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the button click.
    /// </param>
    private void btnAddSucessfulToList_Click(
        object sender,
        EventArgs e)
    {
        AddToListMaker(
            lvSuccessful.Items);
    }
    #endregion
}