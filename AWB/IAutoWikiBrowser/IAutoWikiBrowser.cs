/*
Copyright (C) 2007 Stephen Kennedy <steve@sdk-software.com>

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

using Twain.Core.Logging;
using Twain.Core.Plugin;

namespace AutoWikiBrowser;

partial class MainForm
{
    // Objects and settings exposed through the IAutoWikiBrowser interface.

    /// <summary>
    /// Gets the application's trace manager used for diagnostic logging.
    /// </summary>
    TraceManager IAutoWikiBrowser.TraceManager =>
        Program.MyTrace;

    /// <summary>
    /// Gets or sets a value indicating whether pages with no detected changes
    /// should be skipped.
    /// </summary>
    bool IAutoWikiBrowser.SkipNoChanges
    {
        get => chkSkipNoChanges.Checked;
        set => chkSkipNoChanges.Checked = value;
    }

    /// <summary>
    /// Gets the configured find-and-replace processor.
    /// </summary>
    Twain.Core.Parse.FindandReplace IAutoWikiBrowser.FindandReplace =>
        FindAndReplace;

    /// <summary>
    /// Gets the configured template substitution processor.
    /// </summary>
    Twain.Core.SubstTemplates IAutoWikiBrowser.SubstTemplates =>
        SubstTemplates;

    /// <summary>
    /// Gets the current custom module source code when a usable module is
    /// available; otherwise, <see langword="null"/>.
    /// </summary>
    string? IAutoWikiBrowser.CustomModule =>
        CModule.ModuleUsable
            ? CModule.Code
            : null;
}