/*
Copyright (C) 2009-2018 Sam Reed
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

using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Forms;
using Twain.Core.Background;

namespace Twain.Core;

public static class Updater
{
    private static readonly string AWBDirectory;

    /// <summary>
    /// Runs Update() at creation time
    /// </summary>
    static Updater()
    {
        AWBDirectory = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
        Result = AWBEnabledStatus.None;
        NewerVersions = new();
    }

    /// <summary>
    /// Available Enabled statuses for AWB
    /// </summary>
    [Flags]
    public enum AWBEnabledStatus
    {
        None = 0,
        Error = 1,
        Disabled = 2,
        Enabled = 4,
        UpdaterUpdate = 8,
        OptionalUpdate = 16,
    }

    /// <summary>
    /// Last AWBEnabledStatus Result from CheckPage Check
    /// </summary>
    public static AWBEnabledStatus Result { get; private set; }

    /// <summary>
    /// Text (JSON) of the Current AWB Global CheckPage (en.wp)
    /// </summary>
    public static string GlobalVersionPage { get; private set; }

    /// <summary>
    /// Gets the list of versions of AWB newer than the current version
    /// </summary>
    /// <value>The newer versions.</value>
    public static List<string> NewerVersions { get; private set; }

    private const string CHECKPAGE_URL =
        "https://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage/VersionJSON&action=raw";

    private const string CHECKPAGE_URL_TEST =
        "https://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage/VersionJSONTest&action=raw";

    private sealed class VersionPage
    {
        [JsonProperty("enabledversions")]
        public List<EnabledVersion> EnabledVersions { get; set; } = new();

        [JsonProperty("updaterversion")]
        public string UpdaterVersion { get; set; } = string.Empty;
    }

    private sealed class EnabledVersion
    {
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("releasedate")]
        public string ReleaseDate { get; set; } = string.Empty;

        [JsonProperty("dotnetversion")]
        public string DotNetVersion { get; set; } = string.Empty;

        [JsonProperty("dev")]
        public bool Dev { get; set; }

        [JsonProperty("released")]
        public bool Released { get; set; }
    }

    /// <summary>
    /// Do the actual checking for enabledness etc
    /// </summary>
    private static void UpdateFunc()
    {
        Result = AWBEnabledStatus.Error;

        try
        {
            string text = Tools.GetHTML(CHECKPAGE_URL);

            if (string.IsNullOrWhiteSpace(text))
                return;

            VersionPage versionPage;

            using (var stringReader = new StringReader(text))
            using (var jsonReader = new JsonTextReader(stringReader)
            {
                MaxDepth = 32,
                DateParseHandling = DateParseHandling.None
            })
            {
                var serializer = JsonSerializer.CreateDefault();

                versionPage = serializer.Deserialize<VersionPage>(jsonReader);
            }

            if (versionPage == null ||
                versionPage.EnabledVersions == null ||
                !Version.TryParse(versionPage.UpdaterVersion, out Version updaterVersion))
            {
                return;
            }

            // Only expose the downloaded JSON after it has been
            // successfully parsed and validated.
            GlobalVersionPage = text;

            Result = AWBEnabledStatus.Disabled;

            string awbPath =
                Path.Combine(AWBDirectory, "AutoWikiBrowser.exe");

            string updaterPath =
                Path.Combine(AWBDirectory, "AWBUpdater.exe");

            string awbFileVersion =
                FileVersionInfo.GetVersionInfo(awbPath).FileVersion;

            string updaterFileVersion =
                FileVersionInfo.GetVersionInfo(updaterPath).FileVersion;

            if (!Version.TryParse(awbFileVersion, out Version currentAwbVersion) ||
                !Version.TryParse(updaterFileVersion, out Version currentUpdaterVersion))
            {
                Result = AWBEnabledStatus.Error;
                return;
            }

            var validEnabledVersions = versionPage.EnabledVersions
                .Where(item =>
                    item != null &&
                    Version.TryParse(item.Version, out _))
                .ToList();

            if (validEnabledVersions.Any(item =>
                    string.Equals(
                        item.Version,
                        awbFileVersion,
                        StringComparison.OrdinalIgnoreCase)))
            {
                Result = AWBEnabledStatus.Enabled;
            }

            if (currentUpdaterVersion < updaterVersion)
            {
                Result |= AWBEnabledStatus.UpdaterUpdate;
            }

            if ((Result & AWBEnabledStatus.Disabled) ==
                AWBEnabledStatus.Disabled)
            {
                return;
            }

            var newerVersions = validEnabledVersions
                .Where(item =>
                    !item.Dev &&
                    Version.TryParse(item.Version, out Version candidateVersion) &&
                    candidateVersion > currentAwbVersion)
                .Select(item => item.Version)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (newerVersions.Count > 0)
            {
                NewerVersions.AddRange(newerVersions);
                Result |= AWBEnabledStatus.OptionalUpdate;
            }
        }
        catch (Exception ex)
            when (ex is JsonException
                or IOException
                or UnauthorizedAccessException
                or WebException
                or HttpRequestException)
        {
            Result = AWBEnabledStatus.Error;
        }
    }

    private static BackgroundRequest _request;

    /// <summary>
    /// Checks to see if AWBUpdater.exe.new exists, if it does, replace it.
    /// </summary>
    public static void UpdateUpdaterFile()
    {
        if (File.Exists(AWBDirectory + "AWBUpdater.exe.new"))
        {
            File.Copy(AWBDirectory + "AWBUpdater.exe.new", AWBDirectory + "AWBUpdater.exe", true);
            File.Delete(AWBDirectory + "AWBUpdater.exe.new");
        }
    }

    /// <summary>
    /// Background request to check enabled state of AWB
    /// </summary>
    public static void CheckForUpdates()
    {
        if (_request != null)
        {
            return;
        }

        _request = new BackgroundRequest();
        _request.Execute(UpdateFunc);
    }

    /// <summary>
    /// Waits for background enabled check to complete
    /// </summary>
    public static void WaitForCompletion()
    {
        if (_request == null)
        {
            return;
        }

        _request.Wait();
        _request = null;
    }

    /// <summary>
    /// Runs the Updater program
    /// </summary>
    public static void RunUpdater()
    {
        Process.Start(AWBDirectory + "AWBUpdater.exe");
    }
}