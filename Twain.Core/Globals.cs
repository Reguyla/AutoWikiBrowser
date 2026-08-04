/*

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

using System.Reflection;

namespace WikiFunctions;

/// <summary>
/// Holds some deepest-level things to be initialized prior to most other static classes,
/// including Variables.
/// </summary>
public static class Globals
{
    private static readonly bool mSHTMLAvailable;

    private static readonly bool Windows =
        Environment.OSVersion.VersionString.Contains("Windows");

    private static readonly bool Linux =
        File.Exists("/usr/bin/uname");

    private static readonly bool Mono =
        Type.GetType("Mono.Runtime") != null;

    static Globals()
    {
        mSHTMLAvailable = IsMshtmlAvailable();
    }

    private static bool IsMshtmlAvailable()
    {
        try
        {
            Assembly.Load(new AssemblyName("Microsoft.mshtml"));
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (FileLoadException)
        {
            return false;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Whether we are running under Windows.
    /// </summary>
    public static bool RunningOnWindows
    {
        get { return Windows; }
    }

    public static bool UsingLinux
    {
        get { return Linux; }
    }

    /// <summary>
    /// Returns whether we are using the Mono Runtime.
    /// </summary>
    public static bool UsingMono
    {
        get { return Mono; }
    }

    /// <summary>
    /// Returns the WikiFunctions assembly version.
    /// </summary>
    public static Version WikiFunctionsVersion
    {
        get
        {
            return Assembly.GetAssembly(typeof(Variables))
                .GetName()
                .Version;
        }
    }

    /// <summary>
    /// Gets whether the legacy Microsoft.mshtml interop assembly is available
    /// for browser-specific functionality.
    /// </summary>
    /// <remarks>
    /// A value of <c>true</c> means the assembly could be loaded, but individual
    /// browser interop operations may still fail at runtime and should remain
    /// defensively guarded.
    /// </remarks>
    public static bool MSHTMLAvailable => mSHTMLAvailable;

    #region Unit tests support

    /// <summary>
    /// Set this to true in unit tests to disable checkpage loading and other slow operations.
    /// This disables some functions.
    /// </summary>
    public static bool UnitTestMode;

    /// <summary>
    /// Unit-test integer value.
    /// </summary>
    public static int UnitTestIntValue;

    /// <summary>
    /// Unit-test Boolean value.
    /// </summary>
    public static bool UnitTestBoolValue;

    #endregion
}