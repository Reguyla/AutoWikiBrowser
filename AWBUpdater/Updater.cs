/*
AWBUpdater
Copyright (C) 2009 Sam Reed, Max Semenik

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

using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;

namespace AWBUpdater;

internal sealed partial class Updater : Form
{
    private UpdateStatus _updateStatus = UpdateStatus.None;
    private readonly string _awbDirectory = "", _tempDirectory = "";
    private string _zipName = "";

    private IWebProxy _proxy;

    private const string VERSION_URL =
        "https://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage/VersionJSON&action=raw";

    private const string SOURCEFORGE_URL =
        "http://downloads.sourceforge.net/project/autowikibrowser/autowikibrowser";

    [Flags]
    public enum UpdateStatus
    {
        None = 0,
        Error = 1,
        RequiredUpdate = 2,
        OptionalUpdate = 4,
        OptionalUpdateDeclined = 8,
        UpdaterUpdate = 16,
        UpdateSuccessful = 32,
    }

    public Updater()
    {
        InitializeComponent();

        Text += " - " + Application.ProductVersion;

        _awbDirectory = Path.GetDirectoryName(Application.ExecutablePath);
        _tempDirectory = Environment.GetEnvironmentVariable("TEMP") ?? "C:\\Windows\\Temp";
        _tempDirectory = Path.Combine(_tempDirectory, "$AWB$Updater$Temp$");
    }

    private void Updater_Load(object sender, EventArgs e)
    {
        tmrTimer.Enabled = true;
        UpdateUI("Initialising...", true);
    }

    /// <summary>
    /// Main program function
    /// </summary>
    ///
    /// TODO:
    /// Evaluate externally supplied regular expressions with a timeout to
    /// prevent pathological patterns from blocking the UI.
    private void UpdateAwb()
    {
        try
        {
            _proxy = WebRequest.GetSystemWebProxy();

            if (_proxy.IsBypassed(new Uri("https://en.wikipedia.org")))
                _proxy = null;

            UpdateUI("Getting current AWB and Updater versions", true);
            AWBVersion();

            if ((_updateStatus & (UpdateStatus.OptionalUpdate | UpdateStatus.RequiredUpdate |
                                  UpdateStatus.UpdaterUpdate)) == 0)
            {
                ExitEarly();
                return;
            }

            UpdateUI("Creating a temporary directory", true);
            CreateTempDir();

            UpdateUI("Downloading", true);
            GetZipFromInternet();

            UpdateUI("Unzipping to the temp directory", true);
            UnzipFile();

            if ((_updateStatus & (UpdateStatus.RequiredUpdate | UpdateStatus.OptionalUpdate)) != 0)
            {
                UpdateUI("Making sure AWB is closed", true);
                CloseAwb();
            }

            UpdateUI("Copying files from temp directory to the AWB directory...", true);
            CopyFiles();
            UpdateUI("Update successful", true);

            UpdateUI("Cleaning up from update", true);
            KillTempDir();

            UpdateUI("Update finished. You may close this window (AWB Updater) now.", true);
            _updateStatus = UpdateStatus.UpdateSuccessful;

            ReadyToExit();
        }
        catch (AbortException)
        {
            ReadyToExit();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
        }
    }

    #region UI functions

    /// <summary>
    /// Multiple use function to update the GUI items
    /// </summary>
    /// <param name="currentStatus">What the updater is currently doing</param>
    /// <param name="newLine">If true, adds new line to log instead of reusing last existing one</param>
    private void UpdateUI(string currentStatus, bool newLine)
    {
        if (newLine)
        {
            lstLog.Items.Add(currentStatus);
        }
        else
        {
            lstLog.Items[lstLog.Items.Count - 1] = currentStatus;
        }

        lstLog.SelectedIndex = lstLog.Items.Count - 1;
        Application.DoEvents();
    }

    private void AppendLine(string line)
    {
        lstLog.Items[lstLog.Items.Count - 1] += line;
    }

    /// <summary>
    /// Close the updater early due to lack of updates
    /// </summary>
    private void ExitEarly()
    {
        switch (_updateStatus)
        {
            case UpdateStatus.None:
                UpdateUI("No update available", true);
                break;

            case UpdateStatus.OptionalUpdateDeclined:
                UpdateUI("Optional update declined", true);
                break;
        }

        StartAwb();
        ReadyToExit();
    }

    /// <summary>
    /// Sets UI to "ready to exit" state
    /// </summary>
    private void ReadyToExit()
    {
        btnCancel.Text = "Close";
        lblStatus.Text = "";
        progressUpdate.Visible = false;
        btnCancel.Enabled = true;
    }

    #endregion

    /// <summary>
    /// Creates the temporary folder if it doesn't already exist. If it does exist, delete all the contents
    /// </summary>
    private void CreateTempDir()
    {
        if (Directory.Exists(_tempDirectory))
        {
            // clear its content just to be sure that no parasitic files are left
            Directory.Delete(_tempDirectory, true);
        }

        try
        {
            Directory.CreateDirectory(_tempDirectory);
        }
        catch (Exception)
        {
            // UnauthorizedAccessException and IOException
            UpdateUI("Unable to create temporary directory: " + _tempDirectory, true);
            throw new AbortException();
        }

        progressUpdate.Value = 10;
    }

    /// <summary>
    /// Downloads the current AWB version information.
    /// </summary>
    /// <returns>The version information JSON.</returns>
    private string DownloadVersionJson()
    {
        using HttpClientHandler handler = new HttpClientHandler
        {
            Proxy = _proxy,
            UseProxy = _proxy != null
        };

        using HttpClient client = new HttpClient(handler);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            string.Format(
                "AWBUpdater/{0} ({1}; .NET {2})",
                Assembly.GetExecutingAssembly().GetName().Version,
                Environment.OSVersion.VersionString,
                Environment.Version));

        using HttpResponseMessage response = client
            .GetAsync(VERSION_URL)
            .GetAwaiter()
            .GetResult();

        response.EnsureSuccessStatusCode();

        return response.Content
            .ReadAsStringAsync()
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Checks and compares the current AWB version with the version listed on the enwiki VersionJSON page
    /// </summary>
    private void AWBVersion()
    {
        string json;

        UpdateUI("   Retrieving current version...", true);

        try
        {
            json = DownloadVersionJson();
        }
        catch
        {
            AppendLine("FAILED");
            throw new AbortException();
        }

        try
        {
            FileVersionInfo awbVersionInfo =
                FileVersionInfo.GetVersionInfo(
                    Path.Combine(_awbDirectory, "AutoWikiBrowser.exe"));

            // Existing version comparison code continues here.

            RootObject updaterData =
                System.Text.Json.JsonSerializer.Deserialize<RootObject>(
                    json,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (updaterData == null)
            {
                throw new InvalidDataException(
                    "The version information response did not contain valid version data.");
            }

            string versionToUpdateAWBTo = "";

            if (updaterData.enabledversions.All(v => v.version != awbVersionInfo.FileVersion))
            {
                // The version of AWB in the directory definitely isn't enabled
                _updateStatus = UpdateStatus.RequiredUpdate;

                versionToUpdateAWBTo = updaterData.enabledversions.Where(x => !x.dev)
                    .OrderByDescending(x => x.version).First().version;
            }
            else
            {
                var newerVersions = updaterData.enabledversions
                    .Where(x => !x.dev && new Version(x.version) > new Version(awbVersionInfo.FileVersion))
                    .OrderByDescending(x => x.version).ToList();

                if (newerVersions.Any())
                {
                    _updateStatus = UpdateStatus.OptionalUpdateDeclined;

                    if (newerVersions.Count > 1)
                    {
                        using (VersionChooser chooser = new VersionChooser(newerVersions))
                        {
                            if (chooser.ShowDialog() == DialogResult.OK &&
                                !string.IsNullOrEmpty(chooser.SelectedVersion))
                            {
                                _updateStatus = UpdateStatus.OptionalUpdate;
                                versionToUpdateAWBTo = chooser.SelectedVersion;
                            }
                        }
                    }
                    else if (newerVersions.Count == 1 &&
                             MessageBox.Show(
                                 string.Format(
                                     "There is an optional update to AutoWikiBrowser. Would you like to upgrade to {0}?",
                                     newerVersions.First().version),
                                 "Optional update", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        _updateStatus = UpdateStatus.OptionalUpdate;
                        versionToUpdateAWBTo = newerVersions.First().version;
                    }
                }
            }

            if ((_updateStatus & (UpdateStatus.RequiredUpdate | UpdateStatus.OptionalUpdate)) != 0)
            {
                _zipName = "AutoWikiBrowser" + VersionToFileVersion(versionToUpdateAWBTo) + ".zip";
            }
            else if (new Version(updaterData.updaterversion) >
                     new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString()))
            {
                _zipName = "AWBUpdater" + VersionToFileVersion(updaterData.updaterversion) + ".zip";
                _updateStatus = UpdateStatus.UpdaterUpdate;
            }
        }
        catch
        {
            _updateStatus = UpdateStatus.Error;
            UpdateUI("   Unable to find AutoWikiBrowser.exe to query its version", true);

            throw new AbortException();
        }

        progressUpdate.Value = 35;
    }

    /// <summary>
    /// Downloads the selected update package from the internet.
    /// </summary>
    private void GetZipFromInternet()
    {
        if (!string.IsNullOrEmpty(_zipName))
        {
            DownloadZip(
                _zipName,
                Path.Combine(_tempDirectory, _zipName));
        }

        progressUpdate.Value = 50;
    }

    /// <summary>
    /// Downloads the specified update package to the target file.
    /// </summary>
    /// <param name="file">The update package filename.</param>
    /// <param name="target">The local path where the package will be saved.</param>
    private void DownloadZip(string file, string target)
    {
        string fileWithoutExtension =
            Path.GetFileNameWithoutExtension(file);

        string fileUrl =
            $"{SOURCEFORGE_URL}/{fileWithoutExtension}/{file}";

        try
        {
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string url = string.Format(
                "{0}?r={1}&ts={2}",
                fileUrl,
                WebUtility.UrlEncode(
                    $"{SOURCEFORGE_URL}/{fileWithoutExtension}/"),
                unixTime);

            using HttpClientHandler handler = new HttpClientHandler
            {
                Proxy = _proxy,
                UseProxy = _proxy != null
            };

            using HttpClient client = new HttpClient(handler);

            using HttpResponseMessage response = client
                .GetAsync(url)
                .GetAwaiter()
                .GetResult();

            response.EnsureSuccessStatusCode();

            using Stream sourceStream = response.Content
                .ReadAsStreamAsync()
                .GetAwaiter()
                .GetResult();

            using FileStream targetStream = new FileStream(
                target,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            sourceStream.CopyTo(targetStream);
        }
        catch (HttpRequestException ex)
        {
            UpdateUI(
                $"Download of `{fileUrl}` failed: {ex.Message}",
                true);

            throw new AbortException("The update package could not be downloaded.", ex);
        }
        catch (IOException ex)
        {
            UpdateUI(
                $"Unable to save `{file}`: {ex.Message}",
                true);

            throw new AbortException("The downloaded update package could not be saved.", ex);
        }
    }

    /// <summary>
    /// Checks the zip files exist and calls the functions to unzip them
    /// </summary>
    private void UnzipFile()
    {
        string zip = Path.Combine(_tempDirectory, _zipName);
        if (!string.IsNullOrEmpty(zip) && File.Exists(zip))
        {
            Extract(zip);
            DeleteAbsoluteIfExists(zip);
        }
        else
        {
            UpdateUI("File not unzipped...", true);
        }

        progressUpdate.Value = 70;
    }

    /// <summary>
    /// Code used to unzip the zip files to the temporary directory
    /// </summary>
    /// <param name="file"></param>
    private void Extract(string file)
    {
        try
        {
            new FastZip().ExtractZip(file, _tempDirectory, null);
        }
        catch (ZipException)
        {
            UpdateUI(Path.GetFileName(file) + " seems to be corrupt. Deleting the zip.", true);
            UpdateUI("Please confirm that sourceforge is up.", true);
            DeleteAbsoluteIfExists(file);
            throw new AbortException();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleException(ex);
            Application.Exit();
        }
    }

    /// <summary>
    /// Looks if AWB is open. If it is, tell the user
    /// </summary>
    private void CloseAwb()
    {
        bool awbOpen = false;

        do
        {
            foreach (Process p in Process.GetProcesses())
            {
                awbOpen = p.ProcessName == "AutoWikiBrowser";
                if (awbOpen)
                {
                    MessageBox.Show("Please save your settings (if you wish) and close " + p.ProcessName +
                                    " completely before pressing OK.");
                    break;
                }
            }
        } while (awbOpen);

        progressUpdate.Value = 75;
    }

    private static void DeleteAbsoluteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private void DeleteIfExists(string name)
    {
        string path = Path.Combine(_awbDirectory, name);
        while (true)
        {
            try
            {
                DeleteAbsoluteIfExists(path);
            }
            catch
                (UnauthorizedAccessException)
            {
                // The exception that is thrown when the operating system denies access because of an I/O error or a specific type of security error.
                MessageBox.Show(this,
                    "Access denied for deleting files. Program Files and such are not the best place to run AWB from.\r\n" +
                    "Please run the updater with Administrator rights.");
                Fail();
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(
                        this,
                        "Problem deleting file:\r\n   " + ex.Message + "\r\n\r\n" +
                        "Please close all applications that may use it and press 'Retry' to try again " +
                        "or 'Cancel' to cancel the upgrade.",
                        "Error",
                        MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                {
                    continue;
                }

                Fail();
            }

            break;
        }
    }

    private void Fail()
    {
        AppendLine("... FAILED");
        UpdateUI("Update aborted. AutoWikiBrowser may be unfunctional", true);
        KillTempDir();
        ReadyToExit();
        throw new AbortException();
    }

    /// <summary>
    /// Copies files from the temporary to the working directory
    /// </summary>
    private void CopyFiles()
    {
        string updater = Path.Combine(_tempDirectory, "AWBUpdater.exe");
        if ((_updateStatus & UpdateStatus.UpdaterUpdate) == UpdateStatus.UpdaterUpdate || File.Exists(updater))
        {
            CopyFile(updater, Path.Combine(_awbDirectory, "AWBUpdater.exe.new"));
        }

        if ((_updateStatus & (UpdateStatus.OptionalUpdate | UpdateStatus.RequiredUpdate)) != 0)
        {
            // Explicit Deletions (Remove these if they exist!!)
            DeleteIfExists("Wikidiff2.dll");

            DeleteIfExists("Diff.dll");

            DeleteIfExists("WikiFunctions2.dll");

            DeleteIfExists("WPAssessmentsCatCreator.dll");

            if (Directory.Exists(Path.Combine(_awbDirectory, "Plugins\\WPAssessmentsCatCreator")))
            {
                Directory.Delete(Path.Combine(_awbDirectory, "Plugins\\WPAssessmentsCatCreator"), true);
            }

            foreach (string file in Directory.GetFiles(_tempDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (file.Contains("AWBUpdater"))
                {
                    continue;
                }

                CopyFile(file,
                    Path.Combine(_awbDirectory, file.Replace(_tempDirectory + "\\", "")));
            }

            string[] pluginFiles = Directory.GetFiles(Path.Combine(_awbDirectory, "Plugins"), "*.*",
                SearchOption.AllDirectories);

            foreach (string file in Directory.GetFiles(_awbDirectory, "*.*", SearchOption.TopDirectoryOnly))
            {
                foreach (string pluginFile in pluginFiles)
                {
                    if (file.Substring(file.LastIndexOf("\\", StringComparison.CurrentCulture)) ==
                        pluginFile.Substring(pluginFile.LastIndexOf("\\", StringComparison.CurrentCulture)))
                    {
                        File.Copy(pluginFile, file, true);
                        break;
                    }
                }
            }
        }

        progressUpdate.Value = 95;
    }

    private void CopyFile(string source, string destination)
    {
        CreatePath(destination);
        UpdateUI("     " + destination, true);

        // loop until the file is successfully copied, or user is tired of retrying
        while (true)
        {
            try
            {
                File.Copy(source, destination, true);
            }
            catch (UnauthorizedAccessException)
            {
                //The exception that is thrown when the operating system denies access because of an I/O error or a specific type of security error.
                MessageBox.Show(this,
                    "Access denied for copying files. Program Files and such are not the best place to run AWB from.\r\n" +
                    "Please run the updater with Administrator rights.");
                Fail();
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(
                        this,
                        "Problem replacing file:\r\n   " + ex.Message + "\r\n\r\n" +
                        "Please close all applications that may use it and press 'Retry' to try again " +
                        "or 'Cancel' to cancel the upgrade.",
                        "Error",
                        MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                {
                    continue;
                }

                Fail();
            }

            break;
        }
    }

    /// <summary>
    /// Creates all subdirectories in the path, if needed
    /// </summary>
    /// <param name="path">Path to process, assumed to start from </param>
    private void CreatePath(string path)
    {
        path = Path.GetDirectoryName(path); // strip filename
        if (path != null && !Directory.Exists(path))
        {
            UpdateUI("   Creating directory " + path + "...", true);
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                AppendLine("... FAILED");
                UpdateUI("     (" + ex.Message + ")", true);
            }
        }
    }

    /// <summary>
    /// Starts AWB if exists and is not already running
    /// </summary>
    private void StartAwb()
    {
        bool awbOpen = false;
        foreach (Process p in Process.GetProcesses())
        {
            awbOpen = (p.ProcessName == "AutoWikiBrowser");
            if (awbOpen)
            {
                break;
            }
        }

        if (!awbOpen && File.Exists(_awbDirectory + "AutoWikiBrowser.exe")
                     && MessageBox.Show("Would you like to start AWB?", "Start AWB?", MessageBoxButtons.YesNo) ==
                     DialogResult.Yes)
        {
            Process.Start(_awbDirectory + "AutoWikiBrowser.exe");
        }

        progressUpdate.Value = 99;
    }

    /// <summary>
    /// Deletes the temporary directory
    /// </summary>
    private void KillTempDir()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }

        progressUpdate.Value = 100;
    }

    private void tmrTimer_Tick(object sender, EventArgs e)
    {
        tmrTimer.Enabled = false;
        UpdateAwb();
    }

    private static string VersionToFileVersion(string version)
    {
        return version.Replace(".", "");
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        if ((_updateStatus &
             (UpdateStatus.OptionalUpdate | UpdateStatus.RequiredUpdate)) != 0)
        {
            StartAwb();
        }

        Close();
    }
}

/// <summary>
/// This exception stops processing and prepares the updater for exit
/// </summary>
public class AbortException : Exception
{
    public AbortException()
    {
    }

    public AbortException(string message)
        : base(message)
    {
    }

    public AbortException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}