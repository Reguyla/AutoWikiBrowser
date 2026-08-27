using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Twain.Diagnostics;

/// <summary>
/// Profiles regular-expression typo rules and produces timing reports.
/// </summary>
public static class TypoRuleProfiler
{
    /// <summary>
    /// Profiles the supplied typo rules against the specified article text and
    /// returns a formatted timing report.
    /// </summary>
    /// <param name="typos">
    /// The regular-expression typo rules to profile.
    /// </param>
    /// <param name="text">
    /// The article text used for profiling.
    /// </param>
    /// <param name="articleName">
    /// The article name included in the report.
    /// </param>
    /// <returns>
    /// A formatted profiling report.
    /// </returns>
    public static StringBuilder Profile(
        List<KeyValuePair<Regex, string>> typos,
        string text,
        string articleName)
    {
        int iterations =
            1000000 / text.Length;

        if (iterations > 500)
        {
            iterations = 500;
        }

        List<KeyValuePair<int, string>> times =
            new();

        foreach (KeyValuePair<Regex, string> typo in typos)
        {
            Stopwatch watch = new();
            watch.Start();

            for (int i = 0; i < iterations; i++)
            {
                typo.Key.IsMatch(text);
            }

            times.Add(
                new KeyValuePair<int, string>(
                    (int)watch.ElapsedMilliseconds,
                    typo.Key + " > " + typo.Value));
        }

        times.Sort(CompareRegexPairs);

        StringBuilder builder = new();

        builder.AppendLine(
            "Profiling " +
            iterations +
            @" iterations of """ +
            articleName +
            @"""");

        foreach (KeyValuePair<int, string> result in times)
        {
            builder.AppendLine(
                result.ToString());
        }

        return builder;
    }

    /// <summary>
    /// Compares profiling results by elapsed time in descending order.
    /// </summary>
    private static int CompareRegexPairs(
        KeyValuePair<int, string> x,
        KeyValuePair<int, string> y)
    {
        return y.Key.CompareTo(x.Key);
    }
}