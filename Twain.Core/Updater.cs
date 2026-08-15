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

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    /// <summary>
    /// URL of the legacy global AutoWikiBrowser version and policy metadata.
    /// </summary>
    /// <remarks>
    /// The returned JSON identifies enabled AWB versions and supplies additional
    /// global configuration used by the legacy client-validation workflow.
    /// This endpoint is retained until the corresponding behavior is replaced
    /// by the Twain policy and compatibility services.
    /// </remarks>
    private const string CHECKPAGE_URL =
        "https://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage/VersionJSON&action=raw";

    /// <summary>
    /// Represents the version-related portion of the global AWB configuration.
    /// </summary>
    private sealed class VersionPage
    {
        /// <summary>
        /// Gets or sets the AWB versions recognized by the global configuration.
        /// </summary>
        [JsonPropertyName("enabledversions")]
        public List<EnabledVersion> EnabledVersions { get; set; } = new();
    }

    /// <summary>
    /// Represents metadata for an AWB version listed in the global
    /// configuration.
    /// </summary>
    private sealed class EnabledVersion
    {
        /// <summary>
        /// Gets or sets the application version identifier.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the configured release date.
        /// </summary>
        [JsonPropertyName("releasedate")]
        public string ReleaseDate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the configured .NET version associated with this
        /// application version.
        /// </summary>
        [JsonPropertyName("dotnetversion")]
        public string DotNetVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is a development
        /// version.
        /// </summary>
        [JsonPropertyName("dev")]
        public bool Dev { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this version is marked as
        /// released.
        /// </summary>
        [JsonPropertyName("released")]
        public bool Released { get; set; }
    }

    /// <summary>
    /// Checks the global AWB version configuration and determines whether the
    /// current application version is enabled and whether a newer released
    /// version is available.
    /// </summary>
    /// <remarks>
    /// The downloaded configuration is exposed through
    /// <see cref="GlobalVersionPage"/> only after the version metadata has been
    /// successfully parsed and validated.
    /// </remarks>
    private static void UpdateFunc()
    {
        Result = AWBEnabledStatus.Error;

        try
        {
            if (!TryLoadVersionPage(
                    out string versionPageText,
                    out VersionPage versionPage))
            {
                return;
            }

            if (!TryGetCurrentAwbVersion(
                    out string currentVersionText,
                    out Version currentVersion))
            {
                return;
            }

            GlobalVersionPage = versionPageText;

            List<EnabledVersion> validEnabledVersions =
                GetValidEnabledVersions(
                    versionPage.EnabledVersions);

            Result =
                IsCurrentVersionEnabled(
                    validEnabledVersions,
                    currentVersionText)
                    ? AWBEnabledStatus.Enabled
                    : AWBEnabledStatus.Disabled;

            if (Result == AWBEnabledStatus.Disabled)
            {
                return;
            }

            List<string> newerVersions =
                GetNewerVersions(
                    validEnabledVersions,
                    currentVersion);

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

    /// <summary>
    /// Downloads and parses the global AWB version configuration.
    /// </summary>
    /// <param name="versionPageText">
    /// Contains the downloaded JSON text when successful.
    /// </param>
    /// <param name="versionPage">
    /// Contains the parsed version metadata when successful.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when valid version metadata was retrieved and parsed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryLoadVersionPage(
        out string versionPageText,
        out VersionPage versionPage)
    {
        versionPageText =
            Tools.GetHTML(CHECKPAGE_URL);

        versionPage = null;

        if (string.IsNullOrWhiteSpace(versionPageText))
        {
            return false;
        }

        versionPage =
            JsonSerializer.Deserialize<VersionPage>(
                versionPageText);

        return versionPage != null &&
               versionPage.EnabledVersions != null;
    }


    /// <summary>
    /// Reads and parses the file version of the current AutoWikiBrowser executable.
    /// </summary>
    /// <param name="versionText">
    /// Contains the executable file-version string when available.
    /// </param>
    /// <param name="version">
    /// Contains the parsed application version when successful.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the current application version could be read
    /// and parsed; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryGetCurrentAwbVersion(
        out string versionText,
        out Version version)
    {
        string awbPath =
            Path.Combine(
                AWBDirectory,
                "AutoWikiBrowser.exe");

        versionText =
            FileVersionInfo.GetVersionInfo(awbPath)
                .FileVersion;

        return Version.TryParse(
            versionText,
            out version);
    }

    /// <summary>
    /// Returns the enabled-version entries that contain valid version identifiers.
    /// </summary>
    /// <param name="enabledVersions">
    /// The configured AWB version entries.
    /// </param>
    /// <returns>
    /// The entries whose version values can be parsed successfully.
    /// </returns>
    private static List<EnabledVersion> GetValidEnabledVersions(
        IEnumerable<EnabledVersion> enabledVersions)
    {
        return enabledVersions
            .Where(item =>
                item != null &&
                Version.TryParse(
                    item.Version,
                    out _))
            .ToList();
    }

    /// <summary>
    /// Determines whether the current AWB version is present in the enabled-version
    /// configuration.
    /// </summary>
    /// <param name="enabledVersions">
    /// The validated enabled-version entries.
    /// </param>
    /// <param name="currentVersion">
    /// The current application file-version string.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the current version is enabled; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    private static bool IsCurrentVersionEnabled(
        IEnumerable<EnabledVersion> enabledVersions,
        string currentVersion)
    {
        return enabledVersions.Any(item =>
            string.Equals(
                item.Version,
                currentVersion,
                StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns non-development AWB versions newer than the current application
    /// version.
    /// </summary>
    /// <param name="enabledVersions">
    /// The validated enabled-version entries.
    /// </param>
    /// <param name="currentVersion">
    /// The current application version.
    /// </param>
    /// <returns>
    /// Distinct newer version identifiers.
    /// </returns>
    private static List<string> GetNewerVersions(
        IEnumerable<EnabledVersion> enabledVersions,
        Version currentVersion)
    {
        return enabledVersions
            .Where(item =>
                !item.Dev &&
                Version.TryParse(
                    item.Version,
                    out Version candidateVersion) &&
                candidateVersion > currentVersion)
            .Select(item => item.Version)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static BackgroundRequest _request;

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

}