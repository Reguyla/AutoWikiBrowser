using Newtonsoft.Json.Linq;
using System.Collections;
using System.Windows.Forms;

namespace WikiFunctions;

public static class Extensions
{
    public static void AddIfTrue(this Dictionary<string, string> dict, bool input, string key, string value)
    {
        if (input)
        {
            dict.Add(key, value);
        }
    }

    public static void AddRangeIfNotNull<T>(this List<T> list, IEnumerable<T> collection)
    {
        if (collection != null)
        {
            list.AddRange(collection);
        }
    }

    public static bool IsIn<T>(this T @this, params T[] possibles)
    {
        return ((IList)possibles).Contains(@this);
    }

    /// <summary>
    /// Moves the caret to a specific line of a textbox
    /// </summary>
    /// <param name="t"></param>
    /// <param name="lineNumber"></param>
    public static void GoToLine(this TextBoxBase t, int lineNumber)
    {
        int i = 1;
        int intStart = 0;
        int intEnd = 0;

        foreach (Match m in Regex.Matches(t.Text, "^.*?$", RegexOptions.Multiline))
        {
            if (i == lineNumber)
            {
                intStart = m.Index;
                intEnd = intStart + m.Length;
                break;
            }

            i++;
        }

        t.Select(intStart, intEnd - intStart);
        t.ScrollToCaret();
        t.Focus();
    }

    /// <summary>
    /// Resets any custom formatting of text (if copied from syntax highlighted text in edit box etc.),
    /// restoring cursor position
    /// </summary>
    public static void ResetFormatting(this RichTextBox rtb)
    {
        string a = rtb.Text;
        int i = rtb.SelectionStart;
        rtb.ResetText();
        rtb.Text = a;
        rtb.Select(i, 0);
    }

    /// <summary>
    /// Returns a sorted dictionary whose key order follows the specified key
    /// sequence.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of keys in the dictionary.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values in the dictionary.
    /// </typeparam>
    /// <param name="dictionary">
    /// The dictionary to copy and sort.
    /// </param>
    /// <param name="keys">
    /// The sequence that defines the preferred key order.
    /// </param>
    /// <returns>
    /// A new sorted dictionary containing the original key-value pairs in the
    /// order defined by <paramref name="keys"/>.
    /// </returns>
    public static IDictionary<TKey, TValue> SortBy<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        IEnumerable<TKey> keys)
    {
        KeyComparer<TKey> sorter = new(keys);

        return new SortedDictionary<TKey, TValue>(
            dictionary,
            sorter);
    }

    /// <summary>
    /// Determines whether the specified string is <see langword="null"/> or empty.
    /// </summary>
    /// <param name="str">
    /// The string to test.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="str"/> is
    /// <see langword="null"/> or empty; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsNullOrEmpty(this string? str) =>
        string.IsNullOrEmpty(str);

    /// <summary>
    /// Returns the distinct string representations of the token's immediate
    /// child values.
    /// </summary>
    /// <param name="token">
    /// The JSON token whose child values are inspected.
    /// </param>
    /// <returns>
    /// A list containing the distinct string representations of the token's
    /// immediate children.
    /// </returns>
    public static List<string> DistinctList(this JToken token) =>
        token
            .Select(item => item.ToString())
            .Distinct()
            .ToList();
}