/*
AWB Plugin Manager
Copyright
(C) 2007 Martin Richards
(C) 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/
(C) 2008-2018 Sam Reed

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

using AutoWikiBrowser.Plugins;
using System.ComponentModel;
using System.Windows.Forms;
using WikiFunctions.Plugin;

namespace AutoWikiBrowser;

internal sealed partial class PluginManager : Form
{
    private readonly IAutoWikiBrowser _awb;

    private static string _lastPluginLoadedLocation;

    public PluginManager(IAutoWikiBrowser awb)
    {
        InitializeComponent();
        InitializePluginGroups();

        _awb = awb;
    }

    /// <summary>
    /// Creates the groups used to organize plugins by type and load status.
    /// </summary>
    private void InitializePluginGroups()
    {
        lvPlugin.Groups.AddRange(
            new[]
            {
            new ListViewGroup(
                "Loaded AWB Plugins",
                HorizontalAlignment.Left)
            {
                Name = "groupAWBLoaded"
            },
            new ListViewGroup(
                "Previously Loaded AWB Plugins",
                HorizontalAlignment.Left)
            {
                Name = "groupAWBPrevious"
            },
            new ListViewGroup(
                "Loaded ListMaker Plugins",
                HorizontalAlignment.Left)
            {
                Name = "groupLMLoaded"
            },
            new ListViewGroup(
                "Previously Loaded ListMaker Plugins",
                HorizontalAlignment.Left)
            {
                Name = "groupLMPrevious"
            },
            new ListViewGroup(
                "Loaded Base Plugins",
                HorizontalAlignment.Left)
            {
                Name = "groupBaseLoaded"
            },
            new ListViewGroup(
                "Previously Loaded Base Plugins",
                HorizontalAlignment.Left)
            {
                Name = "groupBasePrevious"
            },
            new ListViewGroup(
                "Obsolete Plugins",
                HorizontalAlignment.Left)
            {
                Name = "groupObsolete"
            },
            new ListViewGroup(
                "Assemblies that failed to load",
                HorizontalAlignment.Left)
            {
                Name = "groupFailed"
            }
            });
    }

    /// <summary>
    /// Prompts the user to select one or more plugin assemblies and loads the
    /// selected plugins into AutoWikiBrowser.
    /// </summary>
    /// <param name="awb">
    /// The AutoWikiBrowser instance supplied to the loaded plugins.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="awb"/> is <see langword="null"/>.
    /// </exception>
    public static void LoadNewPlugin(IAutoWikiBrowser awb)
    {
        ArgumentNullException.ThrowIfNull(awb);

        if (string.IsNullOrEmpty(_lastPluginLoadedLocation))
        {
            LoadLastPluginLoadedLocation();
        }

        using OpenFileDialog pluginOpen = new()
        {
            InitialDirectory =
                string.IsNullOrEmpty(_lastPluginLoadedLocation)
                    ? Application.StartupPath
                    : _lastPluginLoadedLocation,
            DefaultExt = "dll",
            Filter = "DLL files (*.dll)|*.dll",
            CheckFileExists = true,
            Multiselect = true
        };

        if (pluginOpen.ShowDialog() != DialogResult.OK ||
            pluginOpen.FileNames.Length == 0)
        {
            return;
        }

        string? newPath =
            Path.GetDirectoryName(pluginOpen.FileNames[0]);

        if (!string.IsNullOrEmpty(newPath) &&
            !string.Equals(
                _lastPluginLoadedLocation,
                newPath,
                StringComparison.OrdinalIgnoreCase))
        {
            _lastPluginLoadedLocation = newPath;
            SaveLastPluginLoadedLocation();
        }

        Plugin.LoadPlugins(
            awb,
            pluginOpen.FileNames,
            true);
    }

    /// <summary>
    /// Loads the most recently used plugin directory from the current user's
    /// AutoWikiBrowser registry settings.
    /// </summary>
    /// <remarks>
    /// Loading this optional preference is a best-effort operation. Registry
    /// access failures leave the current location unchanged.
    /// </remarks>
    private static void LoadLastPluginLoadedLocation()
    {
        try
        {
            using Microsoft.Win32.RegistryKey? registryKey =
                Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\AutoWikiBrowser");

            _lastPluginLoadedLocation =
                registryKey?
                    .GetValue(
                        "RecentPluginLoadedLocation",
                        string.Empty)
                    ?.ToString() ??
                string.Empty;
        }
        catch
        {
            // Loading this optional preference must not prevent plugin use.
        }
    }

    /// <summary>
    /// Saves the most recently used plugin location in the current user's
    /// AutoWikiBrowser registry settings.
    /// </summary>
    /// <remarks>
    /// Saving this preference is a best-effort operation. Registry access failures
    /// do not prevent the plugin manager from continuing.
    /// </remarks>
    private static void SaveLastPluginLoadedLocation()
    {
        try
        {
            using Microsoft.Win32.RegistryKey? registryKey =
                Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\AutoWikiBrowser");

            registryKey?.SetValue(
                "RecentPluginLoadedLocation",
                _lastPluginLoadedLocation);
        }
        catch (UnauthorizedAccessException)
        {
            // The current user does not have permission to update the setting.
        }
        catch (System.Security.SecurityException)
        {
            // Registry access is restricted by the current security policy.
        }
        catch (IOException)
        {
            // The registry setting could not be written.
        }
    }

    /// <summary>
    /// Loads the list of currently loaded plugins when the plugin manager opens.
    /// </summary>
    /// <param name="sender">
    /// The form that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the form-load operation.
    /// </param>
    private void PluginManager_Load(
        object sender,
        EventArgs e)
    {
        LoadLoadedPluginList();
    }

    /// <summary>
    /// Prompts the user to load new plugins and refreshes the displayed plugin
    /// list afterward.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void loadNewPluginsToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        LoadNewPlugin(_awb);

        lvPlugin.Items.Clear();
        LoadLoadedPluginList();
    }

    private void LoadLoadedPluginList()
    {
        foreach (string pluginName in Plugin.GetAWBPluginList())
        {
            lvPlugin.Items.Add(new ListViewItem(pluginName) { Group = lvPlugin.Groups["groupAWBLoaded"] });
        }

        foreach (string pluginName in Plugin.GetBasePluginList())
        {
            lvPlugin.Items.Add(new ListViewItem(pluginName) { Group = lvPlugin.Groups["groupBaseLoaded"] });
        }

        foreach (string pluginName in Plugin.GetListMakerPluginList())
        {
            lvPlugin.Items.Add(new ListViewItem(pluginName) { Group = lvPlugin.Groups["groupLMLoaded"] });
        }

        foreach (string pluginName in Plugin.FailedPlugins.Keys)
        {
            lvPlugin.Items.Add(new ListViewItem(pluginName) { Group = lvPlugin.Groups["groupObsolete"] });
        }

        foreach (string assemblyName in Plugin.FailedAssemblies)
        {
            lvPlugin.Items.Add(new ListViewItem(assemblyName) { Group = lvPlugin.Groups["groupFailed"] });
        }

        UpdatePluginCount();
    }

    /// <summary>
    /// Updates the plugin context-menu commands before the menu is displayed.
    /// </summary>
    /// <param name="sender">
    /// The context menu that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-opening operation.
    /// </param>
    private void contextMenuStrip1_Opening(
        object sender,
        CancelEventArgs e)
    {
        loadPluginToolStripMenuItem.Enabled =
            lvPlugin.SelectedItems.Count > 0;
    }

    /// <summary>
    /// Loads the plugins currently selected in the plugin list.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// Event data for the menu-item click.
    /// </param>
    private void loadPluginToolStripMenuItem_Click(
        object sender,
        EventArgs e)
    {
        int selectedPluginCount =
            lvPlugin.SelectedItems.Count;

        if (selectedPluginCount == 0)
        {
            return;
        }

        string[] plugins =
            new string[selectedPluginCount];

        for (int i = 0; i < selectedPluginCount; i++)
        {
            plugins[i] =
                lvPlugin.SelectedItems[i].Text;
        }

        Plugin.LoadPlugins(
            _awb,
            plugins,
            true);
    }

    /// <summary>
    /// Updates the displayed number of plugins in the plugin list.
    /// </summary>
    private void UpdatePluginCount()
    {
        lblPluginCount.Text =
            lvPlugin.Items.Count.ToString();
    }
}