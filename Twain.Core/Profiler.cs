/*
WikiFunctions
Copyright (C) 2008 Max Semenik, Stephen Kennedy

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
using System.Threading;

namespace Twain.Core;

/// <summary>
/// Provides lightweight performance profiling and writes elapsed timing
/// information to a profiling log in Debug builds.
/// </summary>
/// <remarks>
/// Profiling calls are compiled out of Release builds. Debug profiling uses
/// a named semaphore to serialize access to the profiling log across profiler
/// instances.
/// </remarks>
public class Profiler
{
#if DEBUG
    private Stopwatch _watch = new();

    private TextWriter _log;

    private readonly string _fileName = string.Empty;

    private readonly bool _append = true;

    private static readonly Semaphore _profilerSemaphore =
        new(1, 1, "AWBProfilerSemaphore");

    /// <summary>
    /// Initializes a new profiler that writes timing information to the
    /// specified log file.
    /// </summary>
    /// <param name="filename">
    /// The name of the file used to store profiling information.
    /// </param>
    /// <param name="append">
    /// <see langword="true"/> to append profiling information to the existing
    /// file; otherwise, <see langword="false"/> to overwrite it.
    /// </param>
    public Profiler(string filename, bool append)
    {
        // Verify that the profiling log path is writable. A new writer is
        // opened for each log entry so the file is not locked for the
        // lifetime of the application.
        using StreamWriter writer =
            new(filename, append, Encoding.Unicode);

        _fileName = filename;
        _append = append;
    }

    /// <summary>
    /// Initializes a profiler without configuring a profiling log file.
    /// </summary>
    public Profiler()
    {
    }

    /// <summary>
    /// Starts measuring elapsed time for a profiling operation.
    /// </summary>
    /// <param name="message">
    /// A description associated with the profiling operation.
    /// </param>
    public void Start(string message)
    {
        AddLog("--------------------------------------");

        _watch = Stopwatch.StartNew();

        AddLog(
            "Started profiling: " +
            message +
            " at " +
            DateTime.Now);
    }

    /// <summary>
    /// Records the elapsed time since the previous profiling mark and starts
    /// measuring the next interval.
    /// </summary>
    /// <param name="message">
    /// A description of the measured interval.
    /// </param>
    public void Profile(string message)
    {
        AddLog(
            "\t" +
            message +
            "\t" +
            _watch.ElapsedMilliseconds);

        _watch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Writes a line to the profiling log.
    /// </summary>
    /// <param name="message">
    /// The text to write to the profiling log.
    /// </param>
    public void AddLog(string message)
    {
        if (_log == null)
        {
            return;
        }

        _profilerSemaphore.WaitOne();

        try
        {
            using (_log = new StreamWriter(
                _fileName,
                _append,
                Encoding.Unicode))
            {
                _log.WriteLine(message);
            }
        }
        finally
        {
            _profilerSemaphore.Release();
        }
    }

    // TODO(Twain): Verify whether Flush() is still required. Profiling writes
    // currently open and dispose a StreamWriter for every log entry, so no
    // persistent buffered writer appears to remain for Flush() to flush.
    /// <summary>
    /// Flushes profiling log output to disk.
    /// </summary>
    public void Flush()
    {
        _profilerSemaphore.WaitOne();

        try
        {
            using (_log = new StreamWriter(
                _fileName,
                _append,
                Encoding.Unicode))
            {
                _log.Flush();
            }
        }
        finally
        {
            _profilerSemaphore.Release();
        }
    }
#else
    /*
     * Unfortunately it seems that code within [Conditional] blocks still
     * gets analysed by the compiler; having the class-level variables inside
     * #if blocks and all methods inside Conditional attribute blocks did not
     * work. The conditional compilation keeps the Release implementation
     * free of the Debug-only profiler infrastructure, while the attributes
     * ensure calls are compiled out in Release builds.
     */

    /// <summary>
    /// Records a profiling interval in Debug builds.
    /// </summary>
    /// <param name="message">
    /// A description of the measured interval.
    /// </param>
    [Conditional("DEBUG")]
    public void Profile(string message)
    {
    }

    /// <summary>
    /// Flushes profiler output in Debug builds.
    /// </summary>
    [Conditional("DEBUG")]
    public void Flush()
    {
    }
#endif
}