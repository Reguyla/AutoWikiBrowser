using System.Text.Json;
using Twain.Core.API;

namespace Twain.Core.Lists.Providers;

/// <summary>
/// Provides article lists from MediaWiki API responses formatted as JSON.
/// </summary>
public abstract class ApiJsonListProviderBase : IListProvider
{
    /// <summary>
    /// Gets or sets the maximum number of pages to return.
    /// </summary>
    public int Limit { get; set; } = 1000;

    /// <summary>
    /// Gets the JSON property containing the page title.
    /// </summary>
    protected virtual string WantedAttribute => "title";

    /// <summary>
    /// Retrieves a list of pages from a MediaWiki API JSON response.
    /// </summary>
    /// <param name="url">The API request query string.</param>
    /// <param name="haveSoFar">
    /// The number of pages already retrieved by the current list operation.
    /// </param>
    /// <returns>The pages returned by the API.</returns>
    public List<Article> ApiMakeList(
        string url,
        int haveSoFar)
    {
        if (Globals.UnitTestMode)
        {
            throw new InvalidOperationException(
                "Wikipedia should not be accessed during unit tests.");
        }

        ApiEdit editor =
            Variables.MainForm.TheSession.Editor.SynchronousEditor;

        string responseJson = editor.QueryApiJson(url);

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return [];
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    responseJson,
                    new JsonDocumentOptions
                    {
                        MaxDepth = 64
                    });

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty(
                    "query",
                    out JsonElement query) ||
                !query.TryGetProperty(
                    "pageswithprop",
                    out JsonElement pages) ||
                pages.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            List<Article> articles = new();

            foreach (JsonElement page in pages.EnumerateArray())
            {
                if (page.ValueKind != JsonValueKind.Object ||
                    !page.TryGetProperty(
                        WantedAttribute,
                        out JsonElement value) ||
                    value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                string? articleName =
                    value.GetString();

                if (!string.IsNullOrWhiteSpace(articleName))
                {
                    articles.Add(
                        new Article(articleName));
                }
            }

            return articles;
        }
        catch (JsonException ex)
        {
            Tools.WriteDebug(
                nameof(ApiMakeList),
                ex.ToString());

            return [];
        }
    }

    public virtual bool StripUrl => false;

    public abstract List<Article> MakeList(
        params string[] searchCriteria);

    public abstract string DisplayText { get; }

    public abstract string UserInputTextBoxText { get; }

    public abstract bool UserInputTextBoxEnabled { get; }

    public abstract void Selected();

    public virtual bool RunOnSeparateThread => true;
}

/// <summary>
/// Retrieves pages that have one or more specified MediaWiki page properties.
/// </summary>
public sealed class PagesWithPropJsonListProvider
    : ApiJsonListProviderBase
{
    public override List<Article> MakeList(
        params string[] searchCriteria)
    {
        List<Article> list = new();

        foreach (string prop in searchCriteria)
        {
            string url =
                $"list=pageswithprop&pwppropname={WebUtility.UrlEncode(prop)}&pwplimit=max";

            list.AddRange(
                ApiMakeList(url, list.Count));
        }

        return list;
    }

    public List<Article> MakeList(
        int @namespace,
        params string[] searchCriteria) =>
        MakeList(searchCriteria);

    public override string DisplayText =>
        "(JSON)Pages with a page property";

    public override string UserInputTextBoxText =>
        "Property name:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected()
    {
    }
}