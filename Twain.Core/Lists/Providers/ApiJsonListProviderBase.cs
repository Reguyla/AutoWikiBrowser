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

    /// <summary>
    /// Gets a value indicating whether URL-specific syntax should be stripped
    /// from the user input before list generation.
    /// </summary>
    /// <value>
    /// <see langword="true"/> when URL-specific syntax should be removed;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public virtual bool StripUrl => false;

    /// <summary>
    /// Builds a list of articles from the supplied search criteria.
    /// </summary>
    /// <param name="searchCriteria">
    /// The provider-specific criteria used to generate the article list.
    /// </param>
    /// <returns>
    /// The articles returned by the provider.
    /// </returns>
    public abstract List<Article> MakeList(
        params string[] searchCriteria);

    /// <summary>
    /// Gets the text used to identify this provider in the list-source UI.
    /// </summary>
    public abstract string DisplayText { get; }

    /// <summary>
    /// Gets the label displayed beside the provider's user-input text box.
    /// </summary>
    public abstract string UserInputTextBoxText { get; }

    /// <summary>
    /// Gets a value indicating whether the provider's user-input text box is
    /// enabled.
    /// </summary>
    public abstract bool UserInputTextBoxEnabled { get; }

    /// <summary>
    /// Performs any provider-specific initialization required when this provider
    /// is selected.
    /// </summary>
    public abstract void Selected();

    /// <summary>
    /// Gets a value indicating whether list generation should run on a separate
    /// worker thread.
    /// </summary>
    /// <value>
    /// <see langword="true"/> by default.
    /// </value>
    public virtual bool RunOnSeparateThread => true;
}

/// <summary>
/// Retrieves pages that have one or more specified MediaWiki page properties.
/// </summary>
/// <remarks>
/// Each supplied property name is queried separately through the MediaWiki
/// <c>pageswithprop</c> API list module, and the returned pages are combined
/// into a single result list.
/// </remarks>
public sealed class PagesWithPropJsonListProvider
    : ApiJsonListProviderBase
{
    /// <summary>
    /// Retrieves pages associated with the specified MediaWiki page-property
    /// names.
    /// </summary>
    /// <param name="searchCriteria">
    /// One or more page-property names to query.
    /// </param>
    /// <returns>
    /// The combined list of pages returned for the supplied property names.
    /// </returns>
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

    /// <summary>
    /// Retrieves pages associated with the specified MediaWiki page-property
    /// names.
    /// </summary>
    /// <param name="namespace">
    /// Namespace value retained for compatibility with list-provider call
    /// patterns. This provider does not use the supplied namespace.
    /// </param>
    /// <param name="searchCriteria">
    /// One or more page-property names to query.
    /// </param>
    /// <returns>
    /// The combined list of pages returned for the supplied property names.
    /// </returns>
    public List<Article> MakeList(
        int @namespace,
        params string[] searchCriteria) =>
        MakeList(searchCriteria);

    /// <summary>
    /// Gets the display name shown for this list provider.
    /// </summary>
    public override string DisplayText =>
        "(JSON)Pages with a page property";

    /// <summary>
    /// Gets the label shown for the property-name input field.
    /// </summary>
    public override string UserInputTextBoxText =>
        "Property name:";

    /// <summary>
    /// Gets a value indicating that the property-name input field is enabled.
    /// </summary>
    public override bool UserInputTextBoxEnabled => true;

    /// <summary>
    /// Handles selection of this provider.
    /// </summary>
    /// <remarks>
    /// No provider-specific selection initialization is currently required.
    /// </remarks>
    public override void Selected()
    {
    }
}