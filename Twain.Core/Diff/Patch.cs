/*
 * Patch support for the diff algorithm.
 */

using System.Collections;

namespace Twain.Core;

/// <summary>
/// Represents a set of changes that can be applied to an original sequence.
/// </summary>
/// <remarks>
/// A patch consists of ordered <see cref="Hunk"/> instances describing
/// unchanged and replacement regions. The implementation uses the legacy
/// non-generic collection interfaces required by the original diff algorithm.
/// </remarks>
public class Patch : IEnumerable
{
    private readonly Hunk[] hunks;

    /// <summary>
    /// Initializes a new instance of the <see cref="Patch"/> class.
    /// </summary>
    /// <param name="hunks">
    /// The ordered collection of patch hunks.
    /// </param>
    internal Patch(Hunk[] hunks)
    {
        this.hunks = hunks;
    }

    /// <summary>
    /// Represents a contiguous region within a patch.
    /// </summary>
    /// <remarks>
    /// A hunk either preserves a range from the original sequence or replaces
    /// that range with data from the modified sequence.
    /// </remarks>
    public class Hunk
    {
        private readonly object[] rightData;
        private readonly int leftstart;
        private readonly int leftcount;
        private readonly int rightstart;
        private readonly int rightcount;
        private readonly bool same;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hunk"/> class.
        /// </summary>
        /// <param name="rightData">
        /// The shared array containing replacement data for changed hunks.
        /// </param>
        /// <param name="st">
        /// The starting index of the corresponding range in the original
        /// sequence.
        /// </param>
        /// <param name="c">
        /// The number of elements represented by the original range.
        /// </param>
        /// <param name="rs">
        /// The starting index of the replacement data in
        /// <paramref name="rightData"/>.
        /// </param>
        /// <param name="rc">
        /// The number of replacement elements.
        /// </param>
        /// <param name="s">
        /// <see langword="true"/> if the hunk preserves unchanged content;
        /// otherwise, <see langword="false"/>.
        /// </param>
        internal Hunk(
            object[] rightData,
            int st,
            int c,
            int rs,
            int rc,
            bool s)
        {
            this.rightData = rightData;
            leftstart = st;
            leftcount = c;
            rightstart = rs;
            rightcount = rc;
            same = s;
        }

        /// <summary>
        /// Gets a value indicating whether this hunk preserves unchanged
        /// content from the original sequence.
        /// </summary>
        public bool Same => same;

        /// <summary>
        /// Gets the zero-based starting index of this hunk in the original
        /// sequence.
        /// </summary>
        public int Start => leftstart;

        /// <summary>
        /// Gets the number of elements represented by this hunk in the
        /// original sequence.
        /// </summary>
        public int Count => leftcount;

        /// <summary>
        /// Gets the inclusive ending index of this hunk in the original
        /// sequence.
        /// </summary>
        /// <remarks>
        /// The value is calculated as <c>Start + Count - 1</c>.
        /// </remarks>
        public int End => leftstart + leftcount - 1;

        /// <summary>
        /// Gets the replacement data associated with this hunk.
        /// </summary>
        /// <value>
        /// A range containing the replacement data, or <see langword="null"/>
        /// when this hunk represents unchanged content.
        /// </value>
        public IList Right =>
            same
                ? null
                : new Range(rightData, rightstart, rightcount);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the patch hunks.
    /// </summary>
    /// <returns>
    /// An enumerator for the patch's <see cref="Hunk"/> instances.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => hunks.GetEnumerator();

    /// <summary>
    /// Applies this patch to the specified original sequence.
    /// </summary>
    /// <param name="original">
    /// The original sequence to which the patch will be applied.
    /// </param>
    /// <returns>
    /// A new sequence containing the unchanged original elements and the
    /// replacement data described by this patch.
    /// </returns>
    public IList Apply(IList original)
    {
        ArrayList right = new ArrayList();

        foreach (Hunk hunk in this)
        {
            if (hunk.Same)
            {
                right.AddRange(
                    new Range(
                        original,
                        hunk.Start,
                        hunk.Count));
            }
            else
            {
                right.AddRange(hunk.Right);
            }
        }

        return right;
    }
}