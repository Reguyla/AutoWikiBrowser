using Twain.Core;

namespace AutoWikiBrowser.Services.ExternalPrograms;

/// <summary>
/// Executes external programs used to process AWB article text.
/// </summary>
internal static class ExternalProgramRunner
{
    /// <summary>
    /// Processes article text using the supplied external program settings.
    /// </summary>
    /// <param name="articleText">
    /// The original article text.
    /// </param>
    /// <param name="articleTitle">
    /// The title of the article being processed.
    /// </param>
    /// <param name="options">
    /// The external program execution settings.
    /// </param>
    /// <returns>
    /// The processed article text and resulting skip state.
    /// </returns>
    internal static ExternalProgramResult ProcessArticle(
        string articleText,
        string articleTitle,
        ExternalProgramOptions options)
    {
        ArgumentNullException.ThrowIfNull(articleText);
        ArgumentNullException.ThrowIfNull(articleTitle);
        ArgumentNullException.ThrowIfNull(options);

        string originalText = articleText;

        if (Globals.UsingLinux)
        {
            ExecuteLinuxProcess(
                articleText,
                articleTitle,
                options);
        }
        else
        {
            ExecuteWindowsProcess(
                articleText,
                articleTitle,
                options);
        }

        return ReadResult(
            originalText,
            options);
    }

    /// <summary>
    /// Executes the configured external program using the Linux and Wine
    /// compatibility workflow.
    /// </summary>
    private static void ExecuteLinuxProcess(
        string articleText,
        string articleTitle,
        ExternalProgramOptions options)
    {
        using Process process = new();

        process.StartInfo.FileName =
            options.ProgramPath;

        process.StartInfo.Arguments =
            BuildArguments(
                articleTitle,
                options);

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;

        if (options.PassAsFile)
        {
            Tools.WriteTextFileAbsolutePath(
                articleText,
                options.OutputFile,
                false);
        }
        else
        {
            process.StartInfo.Arguments =
                AddArticleText(
                    process.StartInfo.Arguments,
                    articleText);
        }

        process.Start();

        // TODO (External Program Reliability):
        // Add timeout and cancellation support so an unresponsive process
        // cannot block AWB indefinitely.
        //
        // TODO (External Program Compatibility):
        // Define whether redirected standard output is diagnostic output or
        // transformed article text. The current Linux workflow logs standard
        // output but returns only text read from the output file.
        string output =
            process.StandardOutput.ReadToEnd();

        Tools.WriteDebug(
            "Ext Proc",
            output);
    }

    /// <summary>
    /// Executes the configured external program using the Windows workflow.
    /// </summary>
    private static void ExecuteWindowsProcess(
        string articleText,
        string articleTitle,
        ExternalProgramOptions options)
    {
        ProcessStartInfo startInfo = new()
        {
            WorkingDirectory =
                Path.GetDirectoryName(options.ProgramPath),

            FileName =
                Path.GetFileName(options.ProgramPath),

            Arguments =
                BuildArguments(
                    articleTitle,
                    options)
        };

        if (options.PassAsFile)
        {
            WriteInputFile(
                articleText,
                options.OutputFile);
        }
        else
        {
            // TODO (External Program Modernization):
            // Replace direct article-text substitution with standard input, a
            // temporary file, or structured argument handling. Large or quoted
            // article text can exceed command-line limits or be parsed
            // incorrectly.
            startInfo.Arguments =
                AddArticleText(
                    startInfo.Arguments,
                    articleText);
        }

        using Process process =
            Process.Start(startInfo) ??
            throw new InvalidOperationException(
                "The configured external program could not be started.");

        // TODO (External Program Reliability):
        // Add timeout and cancellation support so an unresponsive process
        // cannot block AWB indefinitely.
        process.WaitForExit();
    }

    /// <summary>
    /// Builds the command-line arguments for an external program invocation.
    /// </summary>
    private static string BuildArguments(
        string articleTitle,
        ExternalProgramOptions options)
    {
        string parameters =
            options.Parameters.Replace(
                "%%file%%",
                options.OutputFile);

        return Tools.ApplyKeyWords(
            articleTitle,
            parameters);
    }

    /// <summary>
    /// Replaces the article-text placeholder in the command-line arguments.
    /// </summary>
    private static string AddArticleText(
        string arguments,
        string articleText) =>
        arguments.Replace(
            "%%articletext%%",
            articleText);

    /// <summary>
    /// Writes article text to the configured input/output file.
    /// </summary>
    private static void WriteInputFile(
        string articleText,
        string outputFile)
    {
        if (outputFile.Contains(
                '\\',
                StringComparison.Ordinal))
        {
            Tools.WriteTextFileAbsolutePath(
                articleText,
                outputFile,
                false);

            return;
        }

        Tools.WriteTextFile(
            articleText,
            outputFile,
            false);
    }

    /// <summary>
    /// Reads and removes the external program output file and builds the
    /// processing result.
    /// </summary>
    private static ExternalProgramResult ReadResult(
        string originalText,
        ExternalProgramOptions options)
    {
        if (!File.Exists(options.OutputFile))
        {
            return new ExternalProgramResult(
                originalText,
                false);
        }

        string processedText =
            File.ReadAllText(options.OutputFile);

        bool skip =
            options.SkipUnchanged &&
            string.Equals(
                processedText,
                originalText,
                StringComparison.Ordinal);

        // TODO (External Program Safety):
        // Track whether AWB created this file during the current operation and
        // delete only files owned by AWB. Do not remove a pre-existing
        // user-selected file unintentionally.
        File.Delete(options.OutputFile);

        return new ExternalProgramResult(
            processedText,
            skip);
    }
}