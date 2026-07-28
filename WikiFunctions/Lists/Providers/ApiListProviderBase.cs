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
        if (Globals.UnitTestMode)
        {
            throw new InvalidOperationException(
                "Wikipedia should not be accessed during unit tests.");
        }

        List<Article> list = new();
        string postfix = string.Empty;

        ApiEdit editor =
            Variables.MainForm.TheSession.Editor.SynchronousEditor;

        while (list.Count + haveSoFar < Limit)
        {
            string text;

            try
            {
                // TODO:
                // Replace legacy rawcontinue/query-continue handling with
                // MediaWiki's modern continuation format while preserving
                // provider paging and limit behavior.
                text = editor.QueryApi(
                    url + "&rawcontinue=1" + postfix);
            }
            catch (HttpRequestException ex)
            {
                if (Tools.HandleHttpException(ex))
                    continue;

                throw;
            }

            using XmlTextReader xml =
                new(new StringReader(text))
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

            xml.MoveToContent();
            postfix = string.Empty;

            while (xml.Read())
            {
                if (xml.Name == "query-continue")
                {
                    using XmlReader continuationReader =
                        xml.ReadSubtree();

                    continuationReader.Read();

                    while (continuationReader.Read())
                    {
                        if (!continuationReader.IsStartElement())
                            continue;

                        if (!continuationReader.MoveToFirstAttribute())
                        {
                            throw new FormatException(
                                $"Malformed element " +
                                $"'{continuationReader.Name}' " +
                                "in <query-continue>.");
                        }

                        postfix +=
                            $"&{continuationReader.Name}=" +
                            WebUtility.UrlEncode(
                                continuationReader.Value);
                    }
                }
                else if (PageElements.Contains(xml.Name) &&
                         xml.IsStartElement())
                {
                    if (!EvaluateXmlElement(xml))
                        continue;

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

                        break;
                    }

                    list.Add(
                        namespaceIsValid && namespaceId >= 0
                            ? new Article(name, namespaceId)
                            : new Article(name));
                }
            }

            if (string.IsNullOrEmpty(postfix))
                break;
        }

        return list;
    }

    /// <summary>
    /// Determines whether the current XML element may be added to the
    /// article list.
    /// </summary>
    /// <param name="xml">
    /// The XML reader positioned on the element to evaluate.
    /// </param>
    /// <returns>
    /// <c>true</c> if the element may be added; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool EvaluateXmlElement(
        XmlTextReader xml) =>
        true;

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