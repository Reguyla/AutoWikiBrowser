using Twain.Core.API;
using Twain.Core.Lists.Providers;

namespace Twain.Core.Lists;

/// <summary>
/// Executes article-list providers independently of the ListMaker user interface.
/// </summary>
public static class ListGenerationProcessor
{
    /// <summary>
    /// Generates articles using the specified list provider and source values.
    /// </summary>
    /// <param name="provider">
    /// The provider used to generate the article list.
    /// </param>
    /// <param name="sourceValues">
    /// The source values supplied to the provider.
    /// </param>
    /// <returns>
    /// The result of the list-generation operation.
    /// </returns>
    public static ListGenerationResult Generate(
        IListProvider provider,
        string[] sourceValues)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(sourceValues);

        try
        {
            List<Article> articles =
                provider.MakeList(
                    provider.UserInputTextBoxEnabled
                        ? sourceValues
                        : Array.Empty<string>());

            return new ListGenerationResult(articles);
        }
        catch (FeatureDisabledException exception)
        {
            return new ListGenerationResult(
                ListGenerationFailure.FeatureDisabled,
                exception.ApiErrorMessage);
        }
        catch (LoggedOffException)
        {
            return new ListGenerationResult(
                ListGenerationFailure.LoggedOff,
                string.Empty);
        }
        catch (ApiErrorException exception)
            when (exception.ErrorCode == "eiinvalidtitle")
        {
            return new ListGenerationResult(
                ListGenerationFailure.InvalidTitle,
                exception.ApiErrorMessage,
                exception.GetErrorVariable());
        }
        catch (ApiErrorException exception)
        {
            return new ListGenerationResult(
                ListGenerationFailure.ApiError,
                exception.ApiErrorMessage);
        }
        catch (ArgumentException exception)
        {
            return new ListGenerationResult(
                ListGenerationFailure.InvalidParameter,
                exception.Message);
        }
        catch (InterwikiException exception)
        {
            return new ListGenerationResult(
                ListGenerationFailure.InterwikiTitle,
                exception.Message);
        }
        catch (InvalidTitleException exception)
        {
            return new ListGenerationResult(
                ListGenerationFailure.InvalidArticleTitle,
                exception.Message);
        }
    }

    /// <summary>
    /// Prepares source values for the specified list provider.
    /// </summary>
    /// <param name="provider">
    /// The provider that will receive the source values.
    /// </param>
    /// <param name="sourceValues">
    /// The source values to prepare.
    /// </param>
    /// <param name="normalizeTitle">
    /// The title-normalization function to apply when the provider requires URL stripping.
    /// </param>
    /// <returns>
    /// The prepared source values.
    /// </returns>
    public static string[] PrepareSourceValues(
        IListProvider provider,
        string[] sourceValues)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(sourceValues);

        string[] preparedValues = (string[])sourceValues.Clone();

        if (!provider.StripUrl)
            return preparedValues;

        for (int i = 0; i < preparedValues.Length; i++)
        {
            preparedValues[i] =
                NormalizeTitle(preparedValues[i]);
        }

        return preparedValues;
    }

    /// <summary>
    /// Converts list-provider source text into the source values used to generate a list.
    /// </summary>
    /// <param name="sourceText">
    /// The source text entered by the user.
    /// </param>
    /// <returns>
    /// The source values to pass to the list provider.
    /// </returns>
    public static string[] ParseSourceValues(string sourceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        // Pipe character is a separator for standard searches, such as foo|bar.
        // In an insource regular-expression search, however, the pipe may be part
        // of the expression and must not be treated as a separator.
        if (sourceText.Contains("|") &&
            !sourceText.Contains("insource:/"))
        {
            return sourceText.Split(
                new[] { '|' },
                StringSplitOptions.RemoveEmptyEntries);
        }

        return new[] { sourceText };
    }

    private const string DiffEditUrl =
    @"/w(?:(?:iki)?/index\.php5?\?|/\?)title=(.*?)(?:&(?:action|diff|oldid|pe|offset|curid|redirect|type)=.*|$)";

    /// <summary>
    /// Extracts a wiki page title from a wiki page URL, including diff and
    /// revision-history URLs.
    /// </summary>
    /// <param name="source">
    /// The wiki page URL or article title to normalize.
    /// </param>
    /// <returns>
    /// The normalized wiki page title.
    /// </returns>
    public static string NormalizeTitle(string source)
    {
        string originalSource = source;
        string escaped = Regex.Escape(Variables.URL);

        Regex historyDiff =
            new(
                Regex.Replace(
                    escaped,
                    @"https?://",
                    @"(?:https?://|//)?")
                + DiffEditUrl);

        source = historyDiff.Replace(
            source,
            match => match.Groups[1].Value.Replace('+', '_'));

        // Assumption: wikis use /wiki/ as the default article path.
        string url = Variables.URL + "/wiki/";

        if (Variables.URL.Contains("https:"))
            source = source.Replace("http://", "https://");

        source = source.Replace(url, "").Trim();

        // Clean section links.
        if (source.IndexOf("#", StringComparison.Ordinal) > 0)
        {
            source =
                source.Substring(
                    0,
                    source.IndexOf("#", StringComparison.Ordinal));
        }

        if (!originalSource.Equals(source))
            source = Tools.WikiDecode(source);

        // Remove left-to-right marks from the title.
        source = source.Replace("‎", "").Trim();

        return source;
    }
}