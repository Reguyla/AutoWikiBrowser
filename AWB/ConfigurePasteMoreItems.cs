/*
Copyright (C) 2009

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

using System.Windows.Forms;

namespace AutoWikiBrowser;

/// <summary>
/// Displays the configuration dialog for the Paste More Items feature.
/// </summary>
/// <remarks>
/// The dialog allows the user to define up to ten custom text entries that
/// can be inserted through the Paste More Items functionality.
/// </remarks>
public partial class ConfigurePasteMoreItems : Form
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ConfigurePasteMoreItems"/> class.
    /// </summary>
    public ConfigurePasteMoreItems()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ConfigurePasteMoreItems"/> class using the supplied item
    /// values.
    /// </summary>
    /// <param name="string1">Initial value for item 1.</param>
    /// <param name="string2">Initial value for item 2.</param>
    /// <param name="string3">Initial value for item 3.</param>
    /// <param name="string4">Initial value for item 4.</param>
    /// <param name="string5">Initial value for item 5.</param>
    /// <param name="string6">Initial value for item 6.</param>
    /// <param name="string7">Initial value for item 7.</param>
    /// <param name="string8">Initial value for item 8.</param>
    /// <param name="string9">Initial value for item 9.</param>
    /// <param name="string10">Initial value for item 10.</param>
    public ConfigurePasteMoreItems(
        string string1,
        string string2,
        string string3,
        string string4,
        string string5,
        string string6,
        string string7,
        string string8,
        string string9,
        string string10)
        : this()
    {
        String1 = string1;
        String2 = string2;
        String3 = string3;
        String4 = string4;
        String5 = string5;
        String6 = string6;
        String7 = string7;
        String8 = string8;
        String9 = string9;
        String10 = string10;
    }

    public string String1
    {
        get => textBox1.Text;
        private set => textBox1.Text = value;
    }

    public string String2
    {
        get => textBox2.Text;
        private set => textBox2.Text = value;
    }

    public string String3
    {
        get => textBox3.Text;
        private set => textBox3.Text = value;
    }

    public string String4
    {
        get => textBox4.Text;
        private set => textBox4.Text = value;
    }

    public string String5
    {
        get => textBox5.Text;
        private set => textBox5.Text = value;
    }

    public string String6
    {
        get => textBox6.Text;
        private set => textBox6.Text = value;
    }

    public string String7
    {
        get => textBox7.Text;
        private set => textBox7.Text = value;
    }

    public string String8
    {
        get => textBox8.Text;
        private set => textBox8.Text = value;
    }

    public string String9
    {
        get => textBox9.Text;
        private set => textBox9.Text = value;
    }

    public string String10
    {
        get => textBox10.Text;
        private set => textBox10.Text = value;
    }
}