using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;

namespace AWBUpdater;

internal sealed partial class Updater : Form
{
    /// <summary>
    /// Tracks the current state and outcome of the update process.
    /// </summary>
    private UpdateStatus _updateStatus = UpdateStatus.None;

    /// <summary>
    /// Directory containing the currently installed AutoWikiBrowser executable.
    /// </summary>
    private readonly string _awbDirectory = "";

    /// <summary>
    /// Temporary working directory used while downloading and extracting updates.
    /// </summary>
    private readonly string _tempDirectory = string.Empty;

    /// <summary>
    /// Name of the downloaded update archive currently being processed.
    /// </summary>
    private string _zipName = string.Empty;

    /// <summary>
    /// Optional proxy used for updater HTTP requests.
    /// </summary>
    private IWebProxy _proxy;

    /// <summary>
    /// URL used to retrieve the published AutoWikiBrowser version information.
    /// </summary>
    /// <remarks>
    /// The endpoint returns the contents of the Wikipedia-hosted
    /// VersionJSON check page.
    ///
    /// TODO: Replace the Wikipedia-hosted updater manifest with a
    /// project-controlled release/update metadata endpoint.
    /// TODO: Move update source configuration out of the updater UI class.
    /// </remarks>
    private const string VERSION_URL =
        "https://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage/VersionJSON&action=raw";

    /// <summary>
    /// Base URL used to download AutoWikiBrowser update packages from SourceForge.
    /// </summary>
    /// <remarks>
    /// TODO: Remove the SourceForge dependency when the legacy updater is replaced.
    /// TODO: Replace direct URL construction with release metadata that provides
    /// the exact artifact URL, version, and integrity information.
    /// TODO: Require HTTPS for all update downloads.
    /// </remarks>
    private const string SOURCEFORGE_URL =
        "http://downloads.sourceforge.net/project/autowikibrowser/autowikibrowser";

    /// <summary>
    /// Identifies the current updater state and the final result of an update attempt.
    /// </summary>
    /// <remarks>
    /// Multiple values may be combined because this enumeration is marked with
    /// <see cref="FlagsAttribute"/>.
    ///
    /// TODO: Review whether the replacement updater requires flag combinations.
    /// A dedicated update-result or state model may represent the workflow more
    /// clearly than a flags enumeration.
    /// </remarks>
    [Flags]
    public enum UpdateStatus
    {
        /// <summary>
        /// No update state has been recorded.
        /// </summary>
        None = 0,

        /// <summary>
        /// An error occurred during the update process.
        /// </summary>
        Error = 1,

        /// <summary>
        /// A mandatory update is available.
        /// </summary>
        RequiredUpdate = 2,

        /// <summary>
        /// An optional update is available.
        /// </summary>
        OptionalUpdate = 4,

        /// <summary>
        /// The user declined an optional update.
        /// </summary>
        OptionalUpdateDeclined = 8,

        /// <summary>
        /// The updater itself requires or performed an update.
        /// </summary>
        UpdaterUpdate = 16,

        /// <summary>
        /// The update completed successfully.
        /// </summary>
        UpdateSuccessful = 32,
    }

    /// <summary>
    /// Initializes the updater form and establishes the installation and
    /// temporary working directories used during the update process.
    /// </summary>
    /// <remarks>
    /// The updater currently derives the application installation directory
    /// from the executable path and creates a fixed updater subdirectory beneath
    /// the operating system temporary directory.
    ///
    /// TODO: Remove filesystem and update-service initialization from the form
    /// when replacing the legacy updater.
    /// TODO: Use platform-neutral temporary-directory APIs in the replacement
    /// updater rather than falling back to a Windows-specific path.
    /// TODO: Define explicit cleanup and recovery behavior for temporary update
    /// files and interrupted updates.
    /// </remarks>
    public Updater()
    {
        InitializeComponent();

        Text += " - " + Application.ProductVersion;

        _awbDirectory = Path.GetDirectoryName(Application.ExecutablePath);
        _tempDirectory = Environment.GetEnvironmentVariable("TEMP") ?? "C:\\Windows\\Temp";
        _tempDirectory = Path.Combine(_tempDirectory, "$AWB$Updater$Temp$");
    }

    /// <summary>
    /// Starts the update process after the updater form has loaded.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the load event.
    /// </param>
    /// <param name="e">
    /// Event data associated with the load event.
    /// </param>
    /// <remarks>
    /// The actual update workflow is deferred to a timer rather than being
    /// started directly from the form load event.
    ///
    /// TODO: Determine why update processing is timer-driven and whether that
    /// behavior is still required.
    /// TODO: Replace timer-driven orchestration with an asynchronous update
    /// workflow that is independent of the UI.
    /// </remarks>
    private void Updater_Load(object sender, EventArgs e)
    {
        tmrTimer.Enabled = true;
        UpdateUI("Initialising...", true);
    }

    /// <summary>
    /// Coordinates the complete AutoWikiBrowser update workflow.
    /// </summary>
    /// <remarks>
    /// The update process checks the currently installed and available versions,
    /// determines whether an update is required, creates a temporary working
    /// directory, downloads and extracts the update package, closes AutoWikiBrowser
    /// when necessary, copies the updated files into the installation directory,
    /// removes temporary files, and prepares the updater for exit.
    ///
    /// An <see cref="AbortException"/> represents a controlled cancellation of
    /// the update process. Other exceptions are forwarded to the updater's
    /// centralized error handler.
    ///
    /// TODO: Separate update orchestration from the WinForms UI so the update
    /// workflow can execute independently of the updater window.
    /// TODO: Replace synchronous update operations and Application.DoEvents-based
    /// UI responsiveness with an asynchronous, cancellable workflow.
    /// TODO: Define explicit update stages and results instead of coordinating
    /// workflow through UpdateStatus flags.
    /// TODO: Determine which portions of this workflow must be retained by the
    /// replacement updater and which are specific to the legacy AWB installation
    /// model.
    /// </remarks>
    private void UpdateAwb()
    {
        try
        {
            _proxy = HttpClient.DefaultProxy;

            if (_proxy.IsBypassed(new Uri("https://en.wikipedia.org")))
            {
                _proxy = null;
            }

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
    /// Updates the updater activity log with the current operation.
    /// </summary>
    /// <param name="currentStatus">
    /// Text describing the operation currently being performed.
    /// </param>
    /// <param name="newLine">
    /// <see langword="true"/> to append a new log entry; otherwise, replaces
    /// the most recent log entry.
    /// </param>
    /// <remarks>
    /// This method calls <see cref="Application.DoEvents"/> to keep the WinForms
    /// interface responsive while update operations execute synchronously.
    ///
    /// TODO: Remove Application.DoEvents when the update workflow is converted
    /// to asynchronous operations.
    /// TODO: Replace direct control manipulation with progress/status reporting
    /// from an updater service.
    /// </remarks>
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

    /// <summary>
    /// Appends text to the updater's most recent log entry.
    /// </summary>
    /// <param name="line">
    /// Text to append to the current log entry.
    /// </param>
    /// <remarks>
    /// TODO: Replace direct ListBox manipulation with updater progress reporting
    /// when the workflow is separated from the UI.
    /// </remarks>
    private void AppendLine(string line)
    {
        lstLog.Items[lstLog.Items.Count - 1] += line;
    }

    /// <summary>
    /// Completes the updater workflow when no installation work is required.
    /// </summary>
    /// <remarks>
    /// When no update is available, or when the user declines an optional update,
    /// the updater restarts AutoWikiBrowser and transitions the window to its
    /// ready-to-exit state.
    ///
    /// TODO: Determine whether the replacement updater should launch the main
    /// application automatically when no update is performed.
    /// TODO: Replace status-specific branching with an explicit update-check result
    /// when the legacy UpdateStatus flags are removed.
    /// </remarks>
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
    /// Places the updater window into the state used after update processing has
    /// completed or been cancelled.
    /// </summary>
    /// <remarks>
    /// This enables the close button, clears the current status text, and hides
    /// the update progress indicator.
    ///
    /// TODO: Remove this UI-specific completion state from the update workflow when
    /// the updater logic and presentation are separated.
    /// </remarks>
    private void ReadyToExit()
    {
        btnCancel.Text = "Close";
        lblStatus.Text = string.Empty;
        progressUpdate.Visible = false;
        btnCancel.Enabled = true;
    }

    #endregion

    /// <summary>
    /// Creates a clean temporary working directory for the update process.
    /// </summary>
    /// <remarks>
    /// Any existing updater working directory is deleted before a new one is
    /// created to prevent files from previous update attempts from interfering
    /// with the current installation.
    ///
    /// If the temporary directory cannot be created, the update process is
    /// aborted.
    ///
    /// TODO: Replace the fixed updater working directory with a uniquely named
    /// temporary directory to avoid conflicts between concurrent or interrupted
    /// update attempts.
    /// TODO: Determine whether deletion failures should be reported separately
    /// from directory creation failures.
    /// TODO: Move temporary directory management into the replacement updater's
    /// installation service.
    /// </remarks>
    private void CreateTempDir()
    {
        if (Directory.Exists(_tempDirectory))
        {
            // Clear its contents to ensure no files remain from a previous update.
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
    /// Downloads the published AutoWikiBrowser version manifest.
    /// </summary>
    /// <returns>
    /// The raw JSON returned by the configured update manifest endpoint.
    /// </returns>
    /// <remarks>
    /// The request uses the updater's configured proxy settings and supplies a
    /// custom User-Agent containing the updater version, operating system, and
    /// .NET runtime version.
    ///
    /// Any HTTP errors are propagated to the caller.
    ///
    /// TODO: Replace the Wikipedia-hosted version manifest with a project-owned
    /// update service.
    /// TODO: Convert this synchronous implementation to an asynchronous download.
    /// TODO: Consider reusing a shared HttpClient instead of creating one for each
    /// request.
    /// TODO: Add request timeout, retry, and cancellation support.
    /// TODO: Determine whether the replacement updater should validate the
    /// authenticity of the downloaded update metadata.
    /// </remarks>
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
    /// Checks the installed AutoWikiBrowser version against the published update
    /// manifest and determines whether an application or updater update is needed.
    /// </summary>
    /// <remarks>
    /// The installed AutoWikiBrowser version is read from AutoWikiBrowser.exe and
    /// compared with the versions listed in the published version manifest.
    ///
    /// If the installed version is no longer enabled, a required update is selected.
    /// If the installed version is still enabled but newer non-development versions
    /// are available, the user may choose whether to install an optional update.
    /// If no AutoWikiBrowser update is selected, the updater's own version is checked
    /// and may trigger an updater-only update.
    ///
    /// The selected version is converted into the legacy SourceForge package naming
    /// convention and stored in <c>_zipName</c> for later download.
    ///
    /// TODO: Separate version discovery and update-policy decisions from the WinForms
    /// UI and updater orchestration.
    /// TODO: Replace FileVersionInfo-based application version discovery with an
    /// explicit application/version contract suitable for the replacement updater.
    /// TODO: Replace mutable UpdateStatus flags with a strongly typed update-check
    /// result containing the selected version, update type, and package metadata.
    /// TODO: Move required-versus-optional update policy into project-controlled
    /// release metadata rather than deriving it from the enabled-version list.
    /// TODO: Replace string-based version handling with parsed Version values or
    /// another explicit release-version type throughout the updater.
    /// TODO: Remove SourceForge-specific package naming from version selection once
    /// update manifests provide direct artifact metadata.
    /// TODO: Replace broad catch blocks with error handling that distinguishes
    /// manifest errors, version parsing failures, missing executables, and other
    /// update-check failures.
    /// </remarks>
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

            string versionToUpdateAWBTo = string.Empty;

            if (updaterData.enabledversions.All(v => v.version != awbVersionInfo.FileVersion))
            {
                // The installed version is no longer enabled and must be updated.
                _updateStatus = UpdateStatus.RequiredUpdate;

                versionToUpdateAWBTo = updaterData.enabledversions
                    .Where(x => !x.dev)
                    .OrderByDescending(x => x.version)
                    .First()
                    .version;
            }
            else
            {
                var newerVersions = updaterData.enabledversions
                    .Where(
                        x =>
                            !x.dev &&
                            new Version(x.version) > new Version(awbVersionInfo.FileVersion))
                    .OrderByDescending(x => x.version)
                    .ToList();

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
                                 "Optional update",
                                 MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        _updateStatus = UpdateStatus.OptionalUpdate;
                        versionToUpdateAWBTo = newerVersions.First().version;
                    }
                }
            }

            if ((_updateStatus & (UpdateStatus.RequiredUpdate | UpdateStatus.OptionalUpdate)) != 0)
            {
                _zipName =
                    "AutoWikiBrowser" +
                    VersionToFileVersion(versionToUpdateAWBTo) +
                    ".zip";
            }
            else if (new Version(updaterData.updaterversion) >
                     new Version(
                         Assembly.GetExecutingAssembly()
                             .GetName()
                             .Version
                             .ToString()))
            {
                _zipName =
                    "AWBUpdater" +
                    VersionToFileVersion(updaterData.updaterversion) +
                    ".zip";

                _updateStatus = UpdateStatus.UpdaterUpdate;
            }
        }
        catch
        {
            _updateStatus = UpdateStatus.Error;

            UpdateUI(
                "   Unable to find AutoWikiBrowser.exe to query its version",
                true);

            throw new AbortException();
        }

        progressUpdate.Value = 35;
    }

    /// <summary>
    /// Downloads the update package selected during version evaluation.
    /// </summary>
    /// <remarks>
    /// If no package filename was selected, no download is performed.
    ///
    /// TODO: Replace the shared <c>_zipName</c> state with an update-package model
    /// returned by the update-check service.
    /// TODO: Move download orchestration out of the WinForms updater class.
    /// </remarks>
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
    /// <param name="file">
    /// The update package filename.
    /// </param>
    /// <param name="target">
    /// The local path where the downloaded package will be saved.
    /// </param>
    /// <remarks>
    /// The download URL is constructed using the legacy SourceForge project layout.
    /// A timestamp and redirect URL are appended as query parameters before the
    /// request is made.
    ///
    /// The updater's configured proxy is applied to the HTTP request. HTTP request
    /// failures and local file-write failures are reported to the user and converted
    /// into <see cref="AbortException"/> instances.
    ///
    /// TODO: Remove SourceForge-specific URL construction and obtain the complete
    /// artifact URL directly from the replacement update manifest.
    /// TODO: Require HTTPS for update artifact downloads.
    /// TODO: Convert the synchronous download to an asynchronous, cancellable
    /// operation.
    /// TODO: Reuse an updater HTTP client instead of constructing one for each
    /// request.
    /// TODO: Add explicit request timeout and retry behavior.
    /// TODO: Validate package size and cryptographic integrity before installation.
    /// TODO: Consider downloading to a temporary filename and atomically promoting
    /// it only after validation succeeds.
    /// TODO: Report download progress through an updater progress abstraction rather
    /// than manipulating WinForms controls directly.
    /// </remarks>
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

            throw new AbortException(
                "The update package could not be downloaded.",
                ex);
        }
        catch (IOException ex)
        {
            UpdateUI(
                $"Unable to save `{file}`: {ex.Message}",
                true);

            throw new AbortException(
                "The downloaded update package could not be saved.",
                ex);
        }
    }

    /// <summary>
    /// Locates the downloaded update archive, extracts its contents into the
    /// temporary working directory, and removes the archive afterward.
    /// </summary>
    /// <remarks>
    /// If the expected update archive is not present, the updater records the
    /// failure in the UI but does not abort the update process here.
    ///
    /// TODO: Treat a missing update package as an explicit installation failure
    /// rather than continuing the workflow.
    /// TODO: Move archive extraction into the replacement updater's package
    /// installation service.
    /// TODO: Validate package integrity before extraction.
    /// TODO: Validate extracted file paths to prevent files from being written
    /// outside the intended temporary directory.
    /// </remarks>
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
    /// Extracts the specified update archive into the updater's temporary working
    /// directory.
    /// </summary>
    /// <param name="file">
    /// Absolute path of the update archive to extract.
    /// </param>
    /// <remarks>
    /// Corrupt ZIP archives are deleted and cause the current update attempt to be
    /// aborted. Other extraction failures are forwarded to the centralized error
    /// handler and terminate the application.
    ///
    /// TODO: Replace SharpZipLib with the archive implementation selected for the
    /// replacement updater.
    /// TODO: Standardize extraction failures so all failures return an explicit
    /// updater result rather than sometimes aborting and sometimes terminating the
    /// application.
    /// TODO: Remove the SourceForge-specific error message when the legacy download
    /// infrastructure is replaced.
    /// TODO: Ensure the replacement extraction implementation protects against
    /// path traversal and other malformed archive entries.
    /// </remarks>
    private void Extract(string file)
    {
        try
        {
            new FastZip().ExtractZip(file, _tempDirectory, null);
        }
        catch (ZipException)
        {
            UpdateUI(
                Path.GetFileName(file) + " seems to be corrupt. Deleting the zip.",
                true);

            UpdateUI(
                "Please confirm that sourceforge is up.",
                true);

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
    /// Waits until all running AutoWikiBrowser processes have been closed before
    /// allowing application files to be replaced.
    /// </summary>
    /// <remarks>
    /// The updater enumerates all running processes and repeatedly prompts the user
    /// to close AutoWikiBrowser if a matching process is found.
    ///
    /// TODO: Replace process-name polling with an explicit updater/application
    /// shutdown handshake.
    /// TODO: Determine whether the replacement updater should request graceful
    /// application shutdown rather than relying on the user to close the process.
    /// TODO: Add cancellation support so the user can abandon the update while
    /// waiting for the application to close.
    /// TODO: Avoid repeatedly enumerating every running process when checking
    /// whether the application is still running.
    /// </remarks>
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
                    MessageBox.Show(
                        "Please save your settings (if you wish) and close " +
                        p.ProcessName +
                        " completely before pressing OK.");

                    break;
                }
            }
        } while (awbOpen);

        progressUpdate.Value = 75;
    }

    /// <summary>
    /// Deletes the specified file if it currently exists.
    /// </summary>
    /// <param name="path">
    /// Absolute path of the file to delete.
    /// </param>
    private static void DeleteAbsoluteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Deletes the specified file from the AutoWikiBrowser installation directory.
    /// </summary>
    /// <param name="name">
    /// Name of the file to delete from the installation directory.
    /// </param>
    /// <remarks>
    /// Access-denied failures abort the update immediately. Other deletion failures
    /// allow the user to retry or cancel the update.
    ///
    /// TODO: Move installation-directory mutation out of the WinForms UI class.
    /// TODO: Replace interactive retry loops with an installation result that can
    /// be presented by any UI.
    /// TODO: Define rollback or recovery behavior when a file cannot be deleted
    /// after installation changes have already begun.
    /// TODO: Review whether elevation should be detected before installation starts
    /// rather than after an individual file operation fails.
    /// </remarks>
    private void DeleteIfExists(string name)
    {
        string path = Path.Combine(_awbDirectory, name);

        while (true)
        {
            try
            {
                DeleteAbsoluteIfExists(path);
            }
            catch (UnauthorizedAccessException)
            {
                // The exception that is thrown when the operating system denies
                // access because of an I/O error or a specific type of security error.
                MessageBox.Show(
                    this,
                    "Access denied for deleting files. Program Files and such are not the best place to run AWB from.\r\n" +
                    "Please run the updater with Administrator rights.");

                Fail();
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(
                        this,
                        "Problem deleting file:\r\n   " +
                        ex.Message +
                        "\r\n\r\n" +
                        "Please close all applications that may use it and press 'Retry' to try again " +
                        "or 'Cancel' to cancel the upgrade.",
                        "Error",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Error) == DialogResult.Retry)
                {
                    continue;
                }

                Fail();
            }

            break;
        }
    }

    /// <summary>
    /// Aborts the current update after an installation failure.
    /// </summary>
    /// <remarks>
    /// The updater records the failure, warns that AutoWikiBrowser may no longer be
    /// functional, removes the temporary working directory, returns the UI to its
    /// completion state, and terminates the current workflow using
    /// <see cref="AbortException"/>.
    ///
    /// TODO: Replace destructive in-place installation with a transactional or
    /// recoverable installation strategy so a failed update does not leave the
    /// application unusable.
    /// TODO: Replace exception-driven control flow with an explicit installation
    /// result in the replacement updater.
    /// </remarks>
    private void Fail()
    {
        AppendLine("... FAILED");

        UpdateUI(
            "Update aborted. AutoWikiBrowser may be unfunctional",
            true);

        KillTempDir();
        ReadyToExit();

        throw new AbortException();
    }

    /// <summary>
    /// Copies extracted update files from the temporary working directory into the
    /// AutoWikiBrowser installation directory.
    /// </summary>
    /// <remarks>
    /// Updater binaries are staged separately as AWBUpdater.exe.new so the running
    /// updater executable is not overwritten directly.
    ///
    /// For application updates, obsolete files and plugin components are deleted
    /// before all extracted non-updater files are copied into the installation
    /// directory. Plugin files are then copied back over matching files in the
    /// application root.
    ///
    /// TODO: Replace in-place file replacement with an atomic or transactional
    /// installation strategy.
    /// TODO: Replace hard-coded obsolete-file deletions with manifest-driven file
    /// cleanup or a clean installation model.
    /// TODO: Remove AutoWikiBrowser- and AWBUpdater-specific filenames from the
    /// replacement updater.
    /// TODO: Determine why plugin files are copied back over identically named
    /// files in the application root and whether this behavior must be preserved.
    /// TODO: Replace string-based relative-path manipulation with
    /// Path.GetRelativePath.
    /// TODO: Replace Windows-specific path assumptions with platform-neutral path
    /// handling.
    /// TODO: Ensure destination directories are created explicitly before copying
    /// files into nested paths.
    /// TODO: Define rollback behavior if copying fails after files have already
    /// been deleted or replaced.
    /// TODO: Determine how the replacement updater should update itself without
    /// modifying the currently running updater executable.
    /// </remarks>
    private void CopyFiles()
    {
        string updater = Path.Combine(_tempDirectory, "AWBUpdater.exe");

        if ((_updateStatus & UpdateStatus.UpdaterUpdate) == UpdateStatus.UpdaterUpdate ||
            File.Exists(updater))
        {
            CopyFile(
                updater,
                Path.Combine(_awbDirectory, "AWBUpdater.exe.new"));
        }

        if ((_updateStatus & (UpdateStatus.OptionalUpdate | UpdateStatus.RequiredUpdate)) != 0)
        {
            // Explicit deletions of obsolete files from previous releases.
            DeleteIfExists("Wikidiff2.dll");
            DeleteIfExists("Diff.dll");
            DeleteIfExists("Twain.Core2.dll");
            DeleteIfExists("WPAssessmentsCatCreator.dll");

            if (Directory.Exists(
                    Path.Combine(
                        _awbDirectory,
                        "Plugins\\WPAssessmentsCatCreator")))
            {
                Directory.Delete(
                    Path.Combine(
                        _awbDirectory,
                        "Plugins\\WPAssessmentsCatCreator"),
                    true);
            }

            foreach (string file in Directory.GetFiles(
                         _tempDirectory,
                         "*.*",
                         SearchOption.AllDirectories))
            {
                if (file.Contains("AWBUpdater"))
                {
                    continue;
                }

                CopyFile(
                    file,
                    Path.Combine(
                        _awbDirectory,
                        file.Replace(_tempDirectory + "\\", "")));
            }

            string[] pluginFiles = Directory.GetFiles(
                Path.Combine(_awbDirectory, "Plugins"),
                "*.*",
                SearchOption.AllDirectories);

            foreach (string file in Directory.GetFiles(
                         _awbDirectory,
                         "*.*",
                         SearchOption.TopDirectoryOnly))
            {
                foreach (string pluginFile in pluginFiles)
                {
                    if (file.Substring(
                            file.LastIndexOf(
                                "\\",
                                StringComparison.CurrentCulture)) ==
                        pluginFile.Substring(
                            pluginFile.LastIndexOf(
                                "\\",
                                StringComparison.CurrentCulture)))
                    {
                        File.Copy(pluginFile, file, true);
                        break;
                    }
                }
            }
        }

        progressUpdate.Value = 95;
    }

    /// <summary>
    /// Copies a file to the specified destination, replacing any existing file.
    /// </summary>
    /// <param name="source">
    /// Path of the source file to copy.
    /// </param>
    /// <param name="destination">
    /// Destination path where the file will be written.
    /// </param>
    /// <remarks>
    /// The destination directory is created before the copy is attempted.
    ///
    /// If access is denied, the update is aborted. For other copy failures, the
    /// user may retry the operation or cancel the update.
    ///
    /// TODO: Move installation file operations out of the WinForms updater class.
    /// TODO: Replace interactive retry loops with structured installation errors
    /// that can be handled by the updater UI.
    /// TODO: Detect required permissions before installation begins rather than
    /// after individual file operations fail.
    /// TODO: Replace direct in-place overwrites with a transactional or recoverable
    /// installation strategy.
    /// TODO: Add cancellation support to file installation operations.
    /// </remarks>
    private void CopyFile(string source, string destination)
    {
        CreatePath(destination);
        UpdateUI("     " + destination, true);

        // Loop until the file is successfully copied or the user cancels.
        while (true)
        {
            try
            {
                File.Copy(source, destination, true);
            }
            catch (UnauthorizedAccessException)
            {
                // The operating system denied access to the destination file.
                MessageBox.Show(
                    this,
                    "Access denied for copying files. Program Files and such are not the best place to run AWB from.\r\n" +
                    "Please run the updater with Administrator rights.");

                Fail();
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(
                        this,
                        "Problem replacing file:\r\n   " +
                        ex.Message +
                        "\r\n\r\n" +
                        "Please close all applications that may use it and press 'Retry' to try again " +
                        "or 'Cancel' to cancel the upgrade.",
                        "Error",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Error) == DialogResult.Retry)
                {
                    continue;
                }

                Fail();
            }

            break;
        }
    }

    /// <summary>
    /// Ensures that the directory containing the specified file path exists.
    /// </summary>
    /// <param name="path">
    /// Destination file path whose parent directory should be created.
    /// </param>
    /// <remarks>
    /// Directory creation failures are reported through the updater UI but do not
    /// directly abort the update. A subsequent file copy may therefore encounter
    /// the same underlying failure.
    ///
    /// TODO: Move destination-directory preparation into the replacement updater's
    /// installation service.
    /// TODO: Treat directory creation failures as explicit installation failures
    /// rather than allowing the workflow to continue.
    /// TODO: Replace broad exception handling with errors specific to filesystem
    /// and permission failures.
    /// </remarks>
    private void CreatePath(string path)
    {
        path = Path.GetDirectoryName(path);

        if (path != null && !Directory.Exists(path))
        {
            UpdateUI(
                "   Creating directory " + path + "...",
                true);

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                AppendLine("... FAILED");

                UpdateUI(
                    "     (" + ex.Message + ")",
                    true);
            }
        }
    }

    /// <summary>
    /// Offers to start AutoWikiBrowser when the application is installed and is
    /// not already running.
    /// </summary>
    /// <remarks>
    /// The updater first checks running processes for AutoWikiBrowser. If no
    /// matching process is found and the executable exists, the user is prompted
    /// to start the application.
    ///
    /// TODO: Replace process-name detection with an explicit application/updater
    /// lifecycle contract.
    /// TODO: Determine whether the replacement updater should restart Twain
    /// automatically after a successful update or make restart behavior configurable.
    /// TODO: Replace legacy AutoWikiBrowser executable-name assumptions with
    /// application metadata supplied to the updater.
    /// TODO: Use ProcessStartInfo with explicit process-launch settings in the
    /// replacement updater.
    /// TODO: Build executable paths using Path.Combine rather than string
    /// concatenation.
    /// </remarks>
    private void StartAwb()
    {
        bool awbOpen = false;

        foreach (Process p in Process.GetProcesses())
        {
            awbOpen = p.ProcessName == "AutoWikiBrowser";

            if (awbOpen)
            {
                break;
            }
        }

        if (!awbOpen &&
            File.Exists(_awbDirectory + "AutoWikiBrowser.exe") &&
            MessageBox.Show(
                "Would you like to start AWB?",
                "Start AWB?",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            Process.Start(_awbDirectory + "AutoWikiBrowser.exe");
        }

        progressUpdate.Value = 99;
    }

    /// <summary>
    /// Removes the updater's temporary working directory after update processing
    /// has completed.
    /// </summary>
    /// <remarks>
    /// The entire temporary directory and all remaining contents are deleted.
    ///
    /// TODO: Move temporary-directory cleanup into the replacement updater's
    /// package lifecycle management.
    /// TODO: Ensure cleanup occurs through a guaranteed cleanup path even when
    /// download, extraction, installation, or restart operations fail.
    /// TODO: Decide whether failed update artifacts should optionally be retained
    /// for diagnostics rather than always being deleted.
    /// TODO: Handle cleanup failures without masking the outcome of an otherwise
    /// successful update.
    /// </remarks>
    private void KillTempDir()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }

        progressUpdate.Value = 100;
    }

    /// <summary>
    /// Starts the update workflow after the updater form has finished loading.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the timer event.
    /// </param>
    /// <param name="e">
    /// Event data associated with the timer event.
    /// </param>
    /// <remarks>
    /// The timer disables itself before invoking the update workflow so the update
    /// process runs only once.
    ///
    /// TODO: Remove timer-based workflow startup when the updater is replaced with
    /// an asynchronous application lifecycle.
    /// </remarks>
    private void tmrTimer_Tick(object sender, EventArgs e)
    {
        tmrTimer.Enabled = false;
        UpdateAwb();
    }

    /// <summary>
    /// Converts a dotted application version into the legacy compact version
    /// format used by update package filenames.
    /// </summary>
    /// <param name="version">
    /// Version string to convert.
    /// </param>
    /// <returns>
    /// The version string with all period characters removed.
    /// </returns>
    /// <remarks>
    /// For example, <c>6.3.1.0</c> becomes <c>6310</c>.
    ///
    /// TODO: Remove filename-derived version formatting when the replacement
    /// update manifest provides explicit artifact names and download URLs.
    /// </remarks>
    private static string VersionToFileVersion(string version)
    {
        return version.Replace(".", "");
    }

    /// <summary>
    /// Handles the updater window's Cancel or Close button.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the click event.
    /// </param>
    /// <param name="e">
    /// Event data associated with the click event.
    /// </param>
    /// <remarks>
    /// If an application update was selected, the updater offers to restart
    /// AutoWikiBrowser before closing.
    ///
    /// TODO: Separate application restart behavior from the updater window's close
    /// action.
    /// TODO: Ensure cancellation during an active update has an explicit and safe
    /// cancellation path in the replacement updater.
    /// </remarks>
    private void btnCancel_Click(object sender, EventArgs e)
    {
        if ((_updateStatus &
             (UpdateStatus.OptionalUpdate | UpdateStatus.RequiredUpdate)) != 0)
        {
            StartAwb();
        }

        Close();
    }

    /// <summary>
    /// Represents a controlled abort of the current update operation.
    /// </summary>
    /// <remarks>
    /// This exception is used internally by the legacy updater to stop further
    /// processing without treating the condition as an unexpected application
    /// failure. The top-level update workflow catches this exception and returns
    /// the updater UI to a state where it can be closed.
    ///
    /// TODO: Replace exception-driven update cancellation and failure control flow
    /// with explicit update results and cancellation tokens in the replacement
    /// updater.
    /// TODO: Distinguish user cancellation, recoverable installation failures, and
    /// unrecoverable updater errors rather than representing them with the same
    /// exception type.
    /// </remarks>
    public class AbortException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbortException"/> class.
        /// </summary>
        public AbortException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbortException"/> class
        /// with the specified error message.
        /// </summary>
        /// <param name="message">
        /// Message describing why the update operation was aborted.
        /// </param>
        public AbortException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbortException"/> class
        /// with the specified error message and underlying exception.
        /// </summary>
        /// <param name="message">
        /// Message describing why the update operation was aborted.
        /// </param>
        /// <param name="innerException">
        /// Exception that caused the update operation to be aborted.
        /// </param>
        public AbortException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}