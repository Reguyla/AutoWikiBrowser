/*
Copyright (C) 2008 Max Semenik, Sam Reed

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

using System.Xml;
using WikiFunctions.API;

namespace WikiFunctions.Lists.Providers;

/// <summary>
/// Provides the shared implementation for MediaWiki API list providers
/// that process XML responses.
/// </summary>
/// <remarks>
/// Simultaneous use of more than one API generator is not fully supported.
/// </remarks>
public abstract class ApiListProviderBase : IListProvider
{
    /// <summary>
    /// Gets the XML element names that represent pages, such as
    /// <c>p</c>, <c>cm</c>, or <c>bl</c>.
    /// </summary>
    protected abstract ICollection<string> PageElements { get; }

    /// <summary>
    /// Gets the API action names supported by this provider.
    /// </summary>
    protected abstract ICollection<string> Actions { get; }

    /// <summary>
    /// Gets or sets the approximate maximum number of pages returned.
    /// The final API response may cause this value to be exceeded slightly.
    /// </summary>
    public int Limit { get; set; } = 25000;

    /// <summary>
    /// Identifies the XML attribute containing the value used as the
    /// article title.
    /// </summary>
    protected string WantedAttribute = "title";

    /// <summary>
    /// Retrieves pages from the MediaWiki API, following continuation
    /// responses until the configured limit is reached.
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
        EnsureApiAccessAllowed();

        List<Article> articles = new();

        ApiEdit editor =
            Variables.MainForm.TheSession.Editor.SynchronousEditor;

        string continuationPostfix = string.Empty;

        while (articles.Count + haveSoFar < Limit)
        {
            string response = QueryApiWithRetry(
                editor,
                url + "&rawcontinue=1" + continuationPostfix);

            continuationPostfix =
                ParseApiResponse(response, articles);

            if (string.IsNullOrEmpty(continuationPostfix))
                break;
        }

        return articles;
    }

    /// <summary>
    /// Verifies that API access is permitted in the current execution context.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the application is running in unit test mode to prevent
    /// accidental access to live MediaWiki services.
    /// </exception>
    private static void EnsureApiAccessAllowed()
    {
        if (Globals.UnitTestMode)
        {
            throw new InvalidOperationException(
                "Wikipedia should not be accessed during unit tests.");
        }
    }

    /// <summary>
    /// Executes an API query, retrying transient HTTP failures when appropriate.
    /// </summary>
    /// <param name="editor">
    /// The <see cref="ApiEdit"/> instance used to execute the query.
    /// </param>
    /// <param name="query">
    /// The API query string to execute.
    /// </param>
    /// <returns>
    /// The raw XML response returned by the MediaWiki API.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when a non-retryable HTTP failure occurs.
    /// </exception>
    private static string QueryApiWithRetry(
        ApiEdit editor,
        string query)
    {
        while (true)
        {
            try
            {
                return editor.QueryApi(query);
            }
            catch (HttpRequestException ex)
            {
                if (!Tools.HandleHttpException(ex))
                    throw;
            }
        }
    }

    /// <summary>
    /// Represents the outcome of attempting to parse a page element from an
    /// API response.
    /// </summary>
    private enum ArticleElementResult
    {
        /// <summary>
        /// The current XML element is not a page that should be converted into
        /// an <see cref="Article"/>.
        /// </summary>
        Ignored,

        /// <summary>
        /// The current XML element was successfully converted into an
        /// <see cref="Article"/>.
        /// </summary>
        Added,

        /// <summary>
        /// The current XML element was malformed and parsing of the current
        /// API response should stop.
        /// </summary>
        Malformed
    }

    /// <summary>
    /// Parses an API response, extracting page results and continuation data.
    /// </summary>
    /// <param name="response">
    /// The raw XML response returned by the MediaWiki API.
    /// </param>
    /// <param name="articles">
    /// The collection that receives any parsed articles.
    /// </param>
    /// <returns>
    /// The continuation query string to append to the next request, or an empty
    /// string when no additional pages are available.
    /// </returns>
    private string ParseApiResponse(
        string response,
        ICollection<Article> articles)
    {
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        using StringReader textReader =
            new(response);

        using XmlReader xml =
            XmlReader.Create(textReader, settings);

        xml.MoveToContent();

        string continuationPostfix = string.Empty;

        while (xml.Read())
        {
            if (xml.Name == "query-continue")
            {
                continuationPostfix =
                    ReadContinuationPostfix(xml);

                continue;
            }

            if (!PageElements.Contains(xml.Name) ||
                !xml.IsStartElement())
            {
                continue;
            }

            ArticleElementResult result =
                ReadArticleElement(xml, out Article article);

            if (result == ArticleElementResult.Malformed)
                break;

            if (result == ArticleElementResult.Added)
                articles.Add(article);
        }

        return continuationPostfix;
    }

    /// <summary>
    /// Attempts to create an <see cref="Article"/> from the current XML element.
    /// </summary>
    /// <param name="xml">
    /// The XML reader positioned on a page element.
    /// </param>
    /// <param name="article">
    /// When this method returns, contains the parsed article when one was created;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// A value indicating whether the element was ignored, successfully converted
    /// into an article, or determined to be malformed.
    /// </returns>
    private ArticleElementResult ReadArticleElement(
        XmlReader xml,
        out Article article)
    {
        article = null;

        if (!EvaluateXmlElement(xml))
            return ArticleElementResult.Ignored;

        bool namespaceIsValid =
            int.TryParse(
                xml.GetAttribute("ns"),
                out int namespaceId);

        string name =
            xml.GetAttribute(WantedAttribute);

        if (string.IsNullOrEmpty(name))
        {
            Tools.WriteDebug(
                nameof(ApiMakeList),
                $"An API page element did not contain the " +
                $"required '{WantedAttribute}' attribute.");

            return ArticleElementResult.Malformed;
        }

        article =
            namespaceIsValid && namespaceId >= 0
                ? new Article(name, namespaceId)
                : new Article(name);

        return ArticleElementResult.Added;
    }

    /// <summary>
    /// Reads the legacy <c>&lt;query-continue&gt;</c> element and builds the
    /// continuation query string for the next API request.
    /// </summary>
    /// <param name="xml">
    /// The XML reader positioned on the
    /// <c>&lt;query-continue&gt;</c> element.
    /// </param>
    /// <returns>
    /// The continuation query string, or an empty string when no continuation
    /// parameters are present.
    /// </returns>
    private static string ReadContinuationPostfix(
        XmlReader xml)
    {
        StringBuilder postfix = new();

        using XmlReader continuationReader =
            xml.ReadSubtree();

        continuationReader.Read();

        while (continuationReader.Read())
        {
            if (!continuationReader.IsStartElement())
                continue;

            string elementName =
                continuationReader.Name;

            if (!continuationReader.MoveToFirstAttribute())
            {
                throw new FormatException(
                    $"Malformed element '{elementName}' " +
                    "in <query-continue>.");
            }

            postfix
                .Append('&')
                .Append(continuationReader.Name)
                .Append('=')
                .Append(
                    WebUtility.UrlEncode(
                        continuationReader.Value));
        }

        return postfix.ToString();
    }

    /// <summary>
    /// Determines whether the current XML element may be added to the
    /// article list.
    /// </summary>
    /// <param name="xml">
    /// The XML reader positioned on the element to evaluate.
    /// </param>
    /// <returns>
    /// <see langword="true"/> by default. Derived providers may override this
    /// method to filter specific XML elements.
    /// </returns>
    protected virtual bool EvaluateXmlElement(XmlReader xml)
    {
        return true;
    }

    /// <summary>
    /// Gets whether URL information should be removed from returned values.
    /// </summary>
    public virtual bool StripUrl => false;

    /// <summary>
    /// Creates an article list using the supplied search criteria.
    /// </summary>
    /// <param name="searchCriteria">
    /// The provider-specific search criteria.
    /// </param>
    /// <returns>The matching articles.</returns>
    public abstract List<Article> MakeList(
        params string[] searchCriteria);

    /// <summary>
    /// Gets the text displayed for this provider in the list-source UI.
    /// </summary>
    public abstract string DisplayText { get; }

    /// <summary>
    /// Gets the text displayed beside the provider input field.
    /// </summary>
    public abstract string UserInputTextBoxText { get; }

    /// <summary>
    /// Gets whether the provider input field is enabled.
    /// </summary>
    public abstract bool UserInputTextBoxEnabled { get; }

    /// <summary>
    /// Performs any provider-specific action required when it is selected.
    /// </summary>
    public abstract void Selected();

    /// <summary>
    /// Gets whether list generation should run on a separate thread.
    /// </summary>
    public virtual bool RunOnSeparateThread => true;
}