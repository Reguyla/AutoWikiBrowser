/*
 * Utility methods for writing unified diffs.
 */

using System.Collections;

namespace Twain.Core;

/// <summary>
/// Provides methods for writing differences between two sequences in unified
/// diff format.
/// </summary>
public sealed class UnifiedDiff
{
    private UnifiedDiff()
    {
    }

    /// <summary>
    /// Compares two arrays of lines and writes the resulting unified diff.
    /// </summary>
    /// <param name="leftLines">
    /// The original sequence of lines.
    /// </param>
    /// <param name="leftName">
    /// The name written in the original-file header.
    /// </param>
    /// <param name="rightLines">
    /// The modified sequence of lines.
    /// </param>
    /// <param name="rightName">
    /// The name written in the modified-file header.
    /// </param>
    /// <param name="writer">
    /// The writer that receives the unified diff.
    /// </param>
    /// <param name="context">
    /// The maximum number of unchanged lines retained around each changed
    /// region.
    /// </param>
    /// <param name="caseSensitive">
    /// <see langword="true"/> to compare lines using case-sensitive matching;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="compareWhitespace">
    /// <see langword="true"/> to include whitespace differences in the
    /// comparison; otherwise, <see langword="false"/>.
    /// </param>
    public static void WriteUnifiedDiff(
        string[] leftLines,
        string leftName,
        string[] rightLines,
        string rightName,
        TextWriter writer,
        int context,
        bool caseSensitive,
        bool compareWhitespace)
    {
        Diff diff = new Diff(
            leftLines,
            rightLines,
            caseSensitive,
            compareWhitespace);

        WriteUnifiedDiff(
            diff,
            writer,
            leftName,
            rightName,
            context);
    }

    /// <summary>
    /// Compares two text files and writes the resulting unified diff.
    /// </summary>
    /// <param name="leftFile">
    /// The path of the original file.
    /// </param>
    /// <param name="rightFile">
    /// The path of the modified file.
    /// </param>
    /// <param name="writer">
    /// The writer that receives the unified diff.
    /// </param>
    /// <param name="context">
    /// The maximum number of unchanged lines retained around each changed
    /// region.
    /// </param>
    /// <param name="caseSensitive">
    /// <see langword="true"/> to compare lines using case-sensitive matching;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="compareWhitespace">
    /// <see langword="true"/> to include whitespace differences in the
    /// comparison; otherwise, <see langword="false"/>.
    /// </param>
    public static void WriteUnifiedDiff(
        string leftFile,
        string rightFile,
        TextWriter writer,
        int context,
        bool caseSensitive,
        bool compareWhitespace)
    {
        WriteUnifiedDiff(
            LoadFileLines(leftFile),
            leftFile,
            LoadFileLines(rightFile),
            rightFile,
            writer,
            context,
            caseSensitive,
            compareWhitespace);
    }

    /// <summary>
    /// Reads all lines from the specified text file.
    /// </summary>
    /// <param name="file">
    /// The path of the file to read.
    /// </param>
    /// <returns>
    /// An array containing the file's lines without their line terminators.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="file"/> is <see langword="null"/>.
    /// </exception>
    internal static string[] LoadFileLines(string file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        ArrayList lines = new ArrayList();

        using StreamReader reader = new StreamReader(file);

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return (string[])lines.ToArray(typeof(string));
    }

    /// <summary>
    /// Writes the specified diff using default file names and two lines of
    /// surrounding context.
    /// </summary>
    /// <param name="diff">
    /// The diff to write.
    /// </param>
    /// <param name="writer">
    /// The writer that receives the unified diff.
    /// </param>
    public static void WriteUnifiedDiff(
        Diff diff,
        TextWriter writer)
    {
        WriteUnifiedDiff(
            diff,
            writer,
            "Left",
            "Right",
            2);
    }

    /// <summary>
    /// Writes the specified diff in unified diff format.
    /// </summary>
    /// <param name="diff">
    /// The diff to write.
    /// </param>
    /// <param name="writer">
    /// The writer that receives the unified diff.
    /// </param>
    /// <param name="fromfile">
    /// The name written in the original-file header.
    /// </param>
    /// <param name="tofile">
    /// The name written in the modified-file header.
    /// </param>
    /// <param name="context">
    /// The maximum number of unchanged lines retained around each changed
    /// region.
    /// </param>
    public static void WriteUnifiedDiff(
        Diff diff,
        TextWriter writer,
        string fromfile,
        string tofile,
        int context)
    {
        writer.Write("--- ");
        writer.WriteLine(fromfile);
        writer.Write("+++ ");
        writer.WriteLine(tofile);

        // TODO (Modernization):
        // Replace the non-generic ArrayList with List<Diff.Hunk> after the
        // surrounding diff infrastructure has been migrated to generic
        // collections.
        ArrayList hunkSet = new ArrayList();

        foreach (Diff.Hunk hunk in diff)
        {
            Diff.Hunk lastHunk = null;

            if (hunkSet.Count > 0)
            {
                lastHunk =
                    (Diff.Hunk)hunkSet[hunkSet.Count - 1];
            }

            if (hunk.Same)
            {
                // At the beginning of a section, retain only the trailing
                // context lines from this unchanged hunk.
                if (lastHunk == null)
                {
                    if (hunk.Left.Count > context)
                    {
                        hunkSet.Add(
                            hunk.Crop(
                                hunk.Left.Count - context,
                                0));
                    }
                    else
                    {
                        hunkSet.Add(hunk);
                    }
                }
                else
                {
                    // Unchanged content is small enough to keep this section
                    // of the unified diff contiguous.
                    if (hunk.Left.Count <= context * 2)
                    {
                        hunkSet.Add(hunk);
                    }
                    else
                    {
                        // Retain the leading context, write the completed
                        // section, and then retain trailing context for the
                        // next section.
                        hunkSet.Add(
                            hunk.Crop(
                                0,
                                hunk.Left.Count - context));

                        WriteUnifiedDiffSection(
                            writer,
                            hunkSet);

                        hunkSet.Clear();

                        if (hunk.Left.Count > context)
                        {
                            hunkSet.Add(
                                hunk.Crop(
                                    hunk.Left.Count - context,
                                    0));
                        }
                        else
                        {
                            hunkSet.Add(hunk);
                        }
                    }
                }
            }
            else
            {
                hunkSet.Add(hunk);
            }
        }

        if (hunkSet.Count > 0 &&
            !(hunkSet.Count == 1 &&
              ((Diff.Hunk)hunkSet[0]).Same))
        {
            WriteUnifiedDiffSection(
                writer,
                hunkSet);
        }
    }

    /// <summary>
    /// Writes one contiguous unified-diff section.
    /// </summary>
    /// <param name="writer">
    /// The writer that receives the section.
    /// </param>
    /// <param name="hunks">
    /// The ordered hunks contained in the section.
    /// </param>
    private static void WriteUnifiedDiffSection(
        TextWriter writer,
        ArrayList hunks)
    {
        Diff.Hunk first =
            (Diff.Hunk)hunks[0];

        Diff.Hunk last =
            (Diff.Hunk)hunks[hunks.Count - 1];

        writer.Write("@@ -");
        writer.Write(first.Left.Start + 1);
        writer.Write(",");
        writer.Write(
            last.Left.End -
            first.Left.Start +
            1);

        writer.Write(" +");
        writer.Write(first.Right.Start + 1);
        writer.Write(",");
        writer.Write(
            last.Right.End -
            first.Right.Start +
            1);

        writer.WriteLine(" @@");

        foreach (Diff.Hunk hunk in hunks)
        {
            if (hunk.Same)
            {
                WriteBlock(
                    writer,
                    ' ',
                    hunk.Left);

                continue;
            }

            WriteBlock(
                writer,
                '-',
                hunk.Left);

            WriteBlock(
                writer,
                '+',
                hunk.Right);
        }
    }

    /// <summary>
    /// Writes a range using the appropriate character or object formatting.
    /// </summary>
    /// <param name="writer">
    /// The writer that receives the output.
    /// </param>
    /// <param name="prefix">
    /// The unified-diff prefix written before each output line.
    /// </param>
    /// <param name="items">
    /// The range to write.
    /// </param>
    private static void WriteBlock(
        TextWriter writer,
        char prefix,
        Range items)
    {
        if (items.Count > 0 &&
            items[0] is char)
        {
            WriteCharBlock(
                writer,
                prefix,
                items);
        }
        else
        {
            WriteStringBlock(
                writer,
                prefix,
                items);
        }
    }

    /// <summary>
    /// Writes a range of values as individual unified-diff lines.
    /// </summary>
    /// <param name="writer">
    /// The writer that receives the output.
    /// </param>
    /// <param name="prefix">
    /// The unified-diff prefix written before each line.
    /// </param>
    /// <param name="items">
    /// The range of values to write.
    /// </param>
    private static void WriteStringBlock(
        TextWriter writer,
        char prefix,
        Range items)
    {
        foreach (object item in items)
        {
            writer.Write(prefix);
            writer.WriteLine(item.ToString());
        }
    }

    /// <summary>
    /// Writes a range of characters as prefixed unified-diff lines.
    /// </summary>
    /// <param name="writer">
    /// The writer that receives the output.
    /// </param>
    /// <param name="prefix">
    /// The unified-diff prefix written before each output line.
    /// </param>
    /// <param name="items">
    /// The character range to write.
    /// </param>
    /// <remarks>
    /// Explicit newline characters are written using the marker
    /// <c>[newline]</c>. Long character sequences are wrapped after 60
    /// characters.
    /// </remarks>
    private static void WriteCharBlock(
        TextWriter writer,
        char prefix,
        Range items)
    {
        bool newline = true;
        int counter = 0;

        foreach (char character in items)
        {
            if (character == '\n' &&
                !newline)
            {
                writer.WriteLine();
                newline = true;
            }

            if (newline)
            {
                writer.Write(prefix);
                newline = false;
                counter = 0;
            }

            if (character == '\n')
            {
                writer.WriteLine("[newline]");
                newline = true;
            }
            else
            {
                writer.Write(character);
                counter++;

                if (counter == 60)
                {
                    writer.WriteLine();
                    newline = true;
                }
            }
        }

        if (!newline)
        {
            writer.WriteLine();
        }
    }
}