using Newtonsoft.Json.Linq;
using System.Collections;
using System.Windows.Forms;

namespace WikiFunctions;

/// <summary>
/// Provides extension methods for commonly used collection operations.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds the specified key and value to the dictionary when the supplied
    /// condition is <see langword="true"/>.
    /// </summary>
    /// <param name="dict">
    /// The dictionary to update.
    /// </param>
    /// <param name="input">
    /// The condition that determines whether the value is added.
    /// </param>
    /// <param name="key">
    /// The key to add.
    /// </param>
    /// <param name="value">
    /// The value associated with the key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="input"/> is <see langword="true"/> and
    /// <paramref name="key"/> already exists in the dictionary.
    /// </exception>
    public static void AddIfTrue(
        this Dictionary<string, string> dict,
        bool input,
        string key,
        string value)
    {
        if (input)
        {
            dict.Add(key, value);
        }
    }

    /// <summary>
    /// Adds the supplied collection to the list when the collection is not
    /// <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of items contained in the list.
    /// </typeparam>
    /// <param name="list">
    /// The list to update.
    /// </param>
    /// <param name="collection">
    /// The collection to add, or <see langword="null"/> when there are no
    /// items to add.
    /// </param>
    public static void AddRangeIfNotNull<T>(
        this List<T> list,
        IEnumerable<T>? collection)
    {
        if (collection is not null)
        {
            list.AddRange(collection);
        }
    }

    /// <summary>
    /// Determines whether the value equals any of the supplied possible
    /// values using the default equality comparer for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of value being compared.
    /// </typeparam>
    /// <param name="this">
    /// The value to locate.
    /// </param>
    /// <param name="possibles">
    /// The possible matching values.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the value matches one of the supplied
    /// possibilities; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsIn<T>(
        this T @this,
        params T[] possibles)
    {
        return Array.IndexOf(possibles, @this) >= 0;
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