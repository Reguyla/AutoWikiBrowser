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
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
*/

using System.Reflection;
using System.Windows.Forms;
using WikiFunctions;
using WikiFunctions.Plugin;

namespace AutoWikiBrowser.Plugins;

/// <summary>
/// Discovers, loads, initializes, and tracks AWB plugins.
/// </summary>
internal static class Plugin
{
    /// <summary>
    /// Registry of loaded AWB plugins, keyed by plugin name.
    /// </summary>
    internal static readonly Dictionary<string, IAWBPlugin> AWBPlugins = new();

    /// <summary>
    /// Registry of loaded AWB base plugins, keyed by plugin name.
    /// </summary>
    internal static readonly Dictionary<string, IAWBBasePlugin> AWBBasePlugins = new();

    /// <summary>
    /// Registry of loaded ListMaker plugins, keyed by plugin name.
    /// </summary>
    internal static readonly Dictionary<string, IListMakerPlugin> ListMakerPlugins = new();

    /// <summary>
    /// Gets plugins that could not be loaded because they appear to be
    /// incompatible with the current AWB version.
    /// </summary>
    public static readonly Dictionary<string, string> FailedPlugins = new();

    /// <summary>
    /// Gets assembly files that Windows prevented AWB from loading.
    /// </summary>
    public static readonly List<string> FailedAssemblies = new();

    private static readonly HashSet<string> NotPlugins =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "DotNetWikiBot",
            "Diff",
            "WikiFunctions",
            "Newtonsoft.Json",
            "Microsoft.mshtml"
        };

    static Plugin()
    {
        ErrorHandler.AppendToErrorHandler +=
            ErrorHandlerAppendToErrorHandler;
    }

    /// <summary>
    /// Gets the names of all currently loaded AWB plugins.
    /// </summary>
    /// <returns>The loaded AWB plugin names.</returns>
    internal static List<string> GetAWBPluginList() =>
        AWBPlugins.Keys.ToList();

    /// <summary>
    /// Gets the names of all currently loaded AWB base plugins.
    /// </summary>
    /// <returns>The loaded AWB base plugin names.</returns>
    internal static List<string> GetBasePluginList() =>
        AWBBasePlugins.Keys.ToList();

    /// <summary>
    /// Gets the names of all currently loaded ListMaker plugins.
    /// </summary>
    /// <returns>The loaded ListMaker plugin names.</returns>
    internal static List<string> GetListMakerPluginList() =>
        ListMakerPlugins.Keys.ToList();

    /// <summary>
    /// Loads plugins during AWB startup and updates the splash-screen
    /// progress indicator.
    /// </summary>
    /// <param name="awb">The active AWB application instance.</param>
    /// <param name="splash">The startup splash screen.</param>
    internal static void LoadPluginsStartup(
        IAutoWikiBrowser awb,
        Splash splash)
    {
        ArgumentNullException.ThrowIfNull(awb);
        ArgumentNullException.ThrowIfNull(splash);

        splash.SetProgress(25);

        string path = Application.StartupPath;

        string[] pluginFiles =
            Directory.GetFiles(
                path,
                "*.dll");

        LoadPlugins(
            awb,
            pluginFiles,
            false);

        splash.SetProgress(50);
    }

    /// <summary>
    /// Controls whether external plugins are discovered and loaded.
    /// Plugins remain disabled during the core .NET 8 migration.
    /// </summary>
    private static readonly bool ExternalPluginLoadingEnabled = false;

    /// <summary>
    /// Loads the specified plugin assemblies.
    /// </summary>
    /// <param name="awb">The active AWB application instance.</param>
    /// <param name="plugins">The plugin assembly file paths.</param>
    /// <param name="afterStartup">
    /// Whether the plugins are being loaded after application startup.
    /// </param>
    internal static void LoadPlugins(
        IAutoWikiBrowser awb,
        string[] plugins,
        bool afterStartup)
    {
        ArgumentNullException.ThrowIfNull(awb);
        ArgumentNullException.ThrowIfNull(plugins);

        try
        {
            IEnumerable<string> candidatePlugins =
                plugins.Where(IsPotentialPluginAssembly);

            foreach (string pluginFile in candidatePlugins)
            {
                LoadPluginAssembly(
                    pluginFile,
                    awb,
                    afterStartup);
            }
        }
        catch (Exception ex)
        {
            HandlePluginLoadingFailure(ex);
        }
    }

    /// <summary>
    /// Loads and processes a single candidate plugin assembly.
    /// </summary>
    /// <param name="pluginFile">
    /// The path to the plugin assembly.
    /// </param>
    /// <param name="awb">
    /// The active AWB application instance.
    /// </param>
    /// <param name="afterStartup">
    /// Whether the plugin is being loaded after application startup.
    /// </param>
    private static void LoadPluginAssembly(
        string pluginFile,
        IAutoWikiBrowser awb,
        bool afterStartup)
    {
        if (!TryLoadPluginAssembly(
                pluginFile,
                out Assembly assembly))
        {
            return;
        }

        TryLoadPluginTypes(
            assembly,
            pluginFile,
            awb,
            afterStartup);
    }

    /// <summary>
    /// Attempts to load a candidate plugin assembly.
    /// </summary>
    /// <param name="pluginFile">
    /// The path to the plugin assembly.
    /// </param>
    /// <param name="assembly">
    /// The loaded assembly when the operation succeeds.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the assembly was loaded successfully;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryLoadPluginAssembly(
        string pluginFile,
        out Assembly assembly)
    {
        try
        {
            // TODO:
            // Replace direct Assembly.LoadFile loading with a controlled
            // AssemblyLoadContext that shares AWB contract assemblies and
            // resolves plugin-local dependencies predictably.
            assembly = Assembly.LoadFile(pluginFile);

            return true;
        }
        catch (NotSupportedException ex)
        {
            // Windows may block assemblies downloaded from another computer
            // until the file is explicitly unblocked.
            AddFailedAssembly(pluginFile);

            Tools.WriteDebug(
                pluginFile,
                ex.ToString());
        }
        catch (Exception ex)
        {
            Tools.WriteDebug(
                pluginFile,
                ex.ToString());
        }

        assembly = null;

        return false;
    }

    /// <summary>
    /// Attempts to discover and initialize supported plugin types from a
    /// loaded assembly while isolating plugin-specific loading failures.
    /// </summary>
    /// <param name="assembly">
    /// The loaded plugin assembly.
    /// </param>
    /// <param name="pluginFile">
    /// The path to the plugin assembly.
    /// </param>
    /// <param name="awb">
    /// The active AWB application instance.
    /// </param>
    /// <param name="afterStartup">
    /// Whether the plugins are being loaded after application startup.
    /// </param>
    private static void TryLoadPluginTypes(
        Assembly assembly,
        string pluginFile,
        IAutoWikiBrowser awb,
        bool afterStartup)
    {
        try
        {
            LoadPluginTypes(
                assembly,
                pluginFile,
                awb,
                afterStartup);
        }
        catch (ReflectionTypeLoadException ex)
        {
            PluginObsolete(
                pluginFile,
                GetAssemblyVersion(assembly));

            LogLoaderExceptions(
                pluginFile,
                ex);
        }
        catch (MissingMemberException ex)
        {
            PluginObsolete(
                pluginFile,
                GetAssemblyVersion(assembly));

            Tools.WriteDebug(
                pluginFile,
                ex.ToString());
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    /// <summary>
    /// Discovers supported plugin implementations in an assembly and delegates
    /// creation and registration to the appropriate plugin loader.
    /// </summary>
    /// <param name="assembly">
    /// The plugin assembly whose types will be inspected.
    /// </param>
    /// <param name="pluginFile">
    /// The path to the plugin assembly, used for diagnostics and duplicate-plugin
    /// reporting.
    /// </param>
    /// <param name="awb">
    /// The active AWB application instance passed to plugins during
    /// initialization.
    /// </param>
    /// <param name="afterStartup">
    /// Whether the plugins are being loaded after application startup.
    /// </param>
    /// <remarks>
    /// External plugin types are not instantiated while plugin loading is disabled
    /// for the .NET 8 migration. AWB plugins are checked before AWB base plugins
    /// because <see cref="IAWBPlugin"/> inherits from
    /// <see cref="IAWBBasePlugin"/>.
    /// </remarks>
    private static void LoadPluginTypes(
        Assembly assembly,
        string pluginFile,
        IAutoWikiBrowser awb,
        bool afterStartup)
    {
        if (!ExternalPluginLoadingEnabled)
        {
            Tools.WriteDebug(
                nameof(Plugin),
                $"Skipping external plugin '{pluginFile}' during the .NET 8 migration.");

            return;
        }

        foreach (Type type in assembly.GetTypes())
        {
            if (!IsCreatablePluginType(type))
                continue;

            // IAWBPlugin must be checked before IAWBBasePlugin because
            // IAWBPlugin inherits from IAWBBasePlugin.
            if (typeof(IAWBPlugin).IsAssignableFrom(type))
            {
                LoadAWBPlugin(
                    type,
                    pluginFile,
                    awb,
                    afterStartup);
            }
            else if (typeof(IAWBBasePlugin).IsAssignableFrom(type))
            {
                LoadAWBBasePlugin(
                    type,
                    pluginFile,
                    awb,
                    afterStartup);
            }
            else if (typeof(IListMakerPlugin).IsAssignableFrom(type))
            {
                LoadListMakerPlugin(
                    type,
                    pluginFile,
                    afterStartup);
            }
        }
    }

    /// <summary>
    /// Reports an unexpected failure in the overall plugin-loading operation.
    /// </summary>
    /// <param name="exception">
    /// The exception raised while loading plugins.
    /// </param>
    private static void HandlePluginLoadingFailure(
        Exception exception)
    {
#if DEBUG
        ErrorHandler.HandleException(exception);
#else
    Tools.WriteDebug(
        nameof(LoadPlugins),
        exception.ToString());

    MessageBox.Show(
        exception.Message,
        "Problem loading plugins",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);
#endif
    }

    // TODO (Plugin Modernization):
    // Review the plugin version reporting helpers. GetPluginVersionString()
    // currently requires overloads for different plugin interfaces even though
    // the implementation is identical. Investigate replacing the interface-
    // specific overloads with a single helper based on a common abstraction
    // (for example, Type or Assembly) once the plugin architecture redesign
    // begins.
    /// <summary>
    /// Gets the version string of an AWB base plugin.
    /// </summary>
    /// <param name="plugin">The plugin whose version is requested.</param>
    /// <returns>The plugin assembly version.</returns>
    internal static string GetPluginVersionString(
        IAWBBasePlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        return GetAssemblyVersion(
            plugin.GetType().Assembly);
    }

    /// <summary>
    /// Gets the version string of a ListMaker plugin.
    /// </summary>
    /// <param name="plugin">
    /// The plugin whose version is requested.
    /// </param>
    /// <returns>The plugin assembly version.</returns>
    internal static string GetPluginVersionString(
        IListMakerPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        return GetAssemblyVersion(
            plugin.GetType().Assembly);
    }

    /// <summary>
    /// Adds loaded plugin information to AWB error reports.
    /// </summary>
    /// <returns>
    /// Markdown-formatted plugin information, or an empty string when no
    /// plugins are loaded.
    /// </returns>
    private static string ErrorHandlerAppendToErrorHandler()
    {
        if (AWBPlugins.Count == 0 &&
            AWBBasePlugins.Count == 0 &&
            ListMakerPlugins.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();

        builder.AppendLine("```");

        AppendPluginNames(
            builder,
            "AWBPlugins",
            AWBPlugins.Keys);

        AppendPluginNames(
            builder,
            "AWBBasePlugins",
            AWBBasePlugins.Keys);

        AppendPluginNames(
            builder,
            "ListMakerPlugins",
            ListMakerPlugins.Keys);

        builder.AppendLine("```");

        return builder.ToString();
    }

    /// <summary>
    /// Creates and registers an AWB plugin.
    /// </summary>
    private static void LoadAWBPlugin(
        Type pluginType,
        string pluginFile,
        IAutoWikiBrowser awb,
        bool afterStartup)
    {
        if (Activator.CreateInstance(pluginType)
            is not IAWBPlugin awbPlugin)
        {
            return;
        }

        if (AWBPlugins.ContainsKey(awbPlugin.Name))
        {
            ShowDuplicatePluginMessage(
                awbPlugin.Name,
                pluginFile,
                "AWB Plugin");

            return;
        }

        InitialisePlugin(
            awbPlugin,
            awb);

        AWBPlugins.Add(
            awbPlugin.Name,
            awbPlugin);

        if (afterStartup)
        {
            UsageStats.AddedPlugin(awbPlugin);
        }
    }

    /// <summary>
    /// Creates and registers an AWB base plugin.
    /// </summary>
    private static void LoadAWBBasePlugin(
        Type pluginType,
        string pluginFile,
        IAutoWikiBrowser awb,
        bool afterStartup)
    {
        if (Activator.CreateInstance(pluginType)
            is not IAWBBasePlugin awbBasePlugin)
        {
            return;
        }

        if (AWBBasePlugins.ContainsKey(
                awbBasePlugin.Name))
        {
            ShowDuplicatePluginMessage(
                awbBasePlugin.Name,
                pluginFile,
                "AWB Base Plugin");

            return;
        }

        InitialisePlugin(
            awbBasePlugin,
            awb);

        AWBBasePlugins.Add(
            awbBasePlugin.Name,
            awbBasePlugin);

        if (afterStartup)
        {
            UsageStats.AddedPlugin(
                awbBasePlugin);
        }
    }

    /// <summary>
    /// Creates and registers a ListMaker plugin.
    /// </summary>
    private static void LoadListMakerPlugin(
        Type pluginType,
        string pluginFile,
        bool afterStartup)
    {
        if (Activator.CreateInstance(pluginType)
            is not IListMakerPlugin listMakerPlugin)
        {
            return;
        }

        if (ListMakerPlugins.ContainsKey(
                listMakerPlugin.Name))
        {
            ShowDuplicatePluginMessage(
                listMakerPlugin.Name,
                pluginFile,
                "AWB ListMaker Plugin");

            return;
        }

        WikiFunctions.Controls.Lists.ListMaker.AddProvider(
            listMakerPlugin);

        ListMakerPlugins.Add(
            listMakerPlugin.Name,
            listMakerPlugin);

        if (afterStartup)
        {
            UsageStats.AddedPlugin(
                listMakerPlugin);
        }
    }

    /// <summary>
    /// Passes the current AWB application instance to a plugin.
    /// </summary>
    private static void InitialisePlugin(
        IAWBBasePlugin plugin,
        IAutoWikiBrowser awb)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(awb);

        plugin.Initialise(awb);
    }

    /// <summary>
    /// Determines whether a discovered DLL may contain an AWB plugin.
    /// </summary>
    private static bool IsPotentialPluginAssembly(
        string pluginFile)
    {
        if (string.IsNullOrWhiteSpace(pluginFile))
            return false;

        string assemblyName =
            Path.GetFileNameWithoutExtension(pluginFile);

        return !string.IsNullOrEmpty(assemblyName) &&
               !NotPlugins.Contains(assemblyName);
    }

    /// <summary>
    /// Determines whether a reflected type can be instantiated as a plugin.
    /// </summary>
    private static bool IsCreatablePluginType(
        Type type) =>
        type.IsClass &&
        !type.IsAbstract &&
        !type.ContainsGenericParameters;

    /// <summary>
    /// Records an obsolete or incompatible plugin.
    /// </summary>
    private static void PluginObsolete(
        string name,
        string version)
    {
        FailedPlugins.TryAdd(
            name,
            version);
    }

    /// <summary>
    /// Adds an assembly path to the failed-assembly list without creating
    /// duplicate entries.
    /// </summary>
    private static void AddFailedAssembly(
        string pluginFile)
    {
        if (!FailedAssemblies.Contains(
                pluginFile,
                StringComparer.OrdinalIgnoreCase))
        {
            FailedAssemblies.Add(pluginFile);
        }
    }

    /// <summary>
    /// Logs the individual assembly-loader failures contained in a
    /// <see cref="ReflectionTypeLoadException"/>.
    /// </summary>
    private static void LogLoaderExceptions(
        string pluginFile,
        ReflectionTypeLoadException exception)
    {
        foreach (Exception loaderException in
                 exception.LoaderExceptions)
        {
            if (loaderException is null)
                continue;

            Tools.WriteDebug(
                pluginFile,
                loaderException.ToString());
        }
    }

    /// <summary>
    /// Displays a warning when two plugins use the same name.
    /// </summary>
    private static void ShowDuplicatePluginMessage(
        string pluginName,
        string pluginFile,
        string pluginType)
    {
        MessageBox.Show(
            $"A plugin with the name \"{pluginName}\" has already " +
            "been added.\r\n" +
            "Please remove old duplicates from your AutoWikiBrowser " +
            "directory and restart AWB.\r\n" +
            $"The duplicate was loaded from \"{pluginFile}\".",
            $"Duplicate {pluginType}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Appends a named plugin collection to the diagnostic report.
    /// </summary>
    private static void AppendPluginNames(
        StringBuilder builder,
        string heading,
        IEnumerable<string> pluginNames)
    {
        builder.AppendLine(heading);

        foreach (string pluginName in pluginNames)
        {
            builder.AppendLine(
                $"- {pluginName}");
        }

        builder.AppendLine();
    }

    /// <summary>
    /// Gets the version string of an assembly.
    /// </summary>
    private static string GetAssemblyVersion(
        Assembly assembly) =>
        assembly
            .GetName()
            .Version?
            .ToString()
        ?? string.Empty;
}