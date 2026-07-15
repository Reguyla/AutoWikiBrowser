/*
(C) 2008 Stephen Kennedy, Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
*/

namespace WikiFunctions.Lists.Providers;

/// <summary>
/// Defines a provider that integrates with the ListMaker source selector
/// and produces a list of wiki articles.
/// </summary>
public interface IListProvider
{
    /// <summary>
    /// Processes the supplied search criteria and creates a list of articles.
    /// </summary>
    /// <param name="searchCriteria">
    /// The user-entered values or pages used by the provider.
    /// </param>
    /// <returns>The articles produced by the provider.</returns>
    List<Article> MakeList(params string[] searchCriteria);

    /// <summary>
    /// Gets the text displayed for this provider in the source selection
    /// combo box.
    /// </summary>
    string DisplayText { get; }

    /// <summary>
    /// Gets the text displayed beside the source input field.
    /// </summary>
    string UserInputTextBoxText { get; }

    /// <summary>
    /// Gets whether the source input field should be enabled.
    /// </summary>
    bool UserInputTextBoxEnabled { get; }

    /// <summary>
    /// Performs any provider-specific action required when the provider
    /// is selected.
    /// </summary>
    void Selected();

    /// <summary>
    /// Gets whether list generation should run on a separate thread.
    /// </summary>
    bool RunOnSeparateThread { get; }

    /// <summary>
    /// Gets whether the current wiki URL should be removed from input values.
    /// </summary>
    bool StripUrl { get; }
}

    /// <summary>
    /// Extends <see cref="IListProvider"/> with options required by
    /// MediaWiki special-page providers.
    /// </summary>
    interface ISpecialPageProvider : IListProvider
{
    /// <summary>
    /// Processes the supplied search criteria for a specific namespace
    /// and creates a list of articles.
    /// </summary>
    /// <param name="namespace">
    /// The namespace identifier used to filter or enumerate results.
    /// </param>
    /// <param name="searchCriteria">
    /// The user-entered values or pages used by the provider.
    /// </param>
    /// <returns>The articles produced by the provider.</returns>
    List<Article> MakeList(
        int @namespace,
        params string[] searchCriteria);

    /// <summary>
    /// Gets whether the provider requires text to be entered in the source
    /// input field.
    /// </summary>
    bool PagesNeeded { get; }

    /// <summary>
    /// Gets whether the namespace selector should be enabled.
    /// </summary>
    bool NamespacesEnabled { get; }
}