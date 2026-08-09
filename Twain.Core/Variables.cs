/*
Copyright (C) 2007 Martin Richards, 2008 Stephen Kennedy

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

using System.Threading;
using Twain.Core.Background;
using Twain.Core.Lists.Providers;
using Twain.Core.Plugin;

namespace Twain.Core;

// TODO: Consider replacing ProjectEnum with a modern project descriptor or
// strongly typed project model. The current enum assumes a fixed set of wiki
// families and does not naturally represent arbitrary third-party MediaWiki
// installations.

/// <summary>
/// Identifies the wiki project family used by the current configuration.
/// </summary>
/// <remarks>
/// Legacy member names are retained for compatibility with existing settings
/// and callers.
/// </remarks>
public enum ProjectEnum
{
    wikipedia,
    wiktionary,
    wikidata,
    wikisource,
    wikiquote,
    wikiversity,
    wikivoyage,
    wikibooks,
    wikinews,
    species,
    commons,
    meta,
    mediawiki,
    incubator,
    wikia,
    fandom,
    custom
}

// TODO: Gradually replace mutable global state in Variables with focused,
// injectable configuration and session services. Preserve compatibility while
// migrating existing consumers incrementally.

/// <summary>
/// Stores shared wiki, namespace, URL, editing, and application configuration
/// used throughout the application.
/// </summary>
/// <remarks>
/// This class represents legacy global application state. Consumers should
/// avoid adding new dependencies on it where more focused configuration or
/// session abstractions are available.
/// </remarks>
public static partial class Variables
{
    /// <summary>
    /// Initializes canonical namespace metadata and the default application/wiki
    /// configuration.
    /// </summary>
    static Variables()
    {
        CanonicalNamespaces[-2] = "Media:";
        CanonicalNamespaces[-1] = "Special:";
        CanonicalNamespaces[1] = "Talk:";
        CanonicalNamespaces[2] = "User:";
        CanonicalNamespaces[3] = "User talk:";
        CanonicalNamespaces[4] = "Project:";
        CanonicalNamespaces[5] = "Project talk:";
        CanonicalNamespaces[6] = "File:";
        CanonicalNamespaces[7] = "File talk:";
        CanonicalNamespaces[8] = "MediaWiki:";
        CanonicalNamespaces[9] = "MediaWiki talk:";
        CanonicalNamespaces[10] = "Template:";
        CanonicalNamespaces[11] = "Template talk:";
        CanonicalNamespaces[12] = "Help:";
        CanonicalNamespaces[13] = "Help talk:";
        CanonicalNamespaces[14] = "Category:";
        CanonicalNamespaces[15] = "Category talk:";
        CanonicalNamespaces[118] = "Draft:";
        CanonicalNamespaces[119] = "Draft talk:";
        CanonicalNamespaces[126] = "MOS:";
        CanonicalNamespaces[127] = "MOS talk:";
        CanonicalNamespaces[710] = "TimedText:";
        CanonicalNamespaces[711] = "TimedText talk:";
        CanonicalNamespaces[828] = "Module:";
        CanonicalNamespaces[829] = "Module talk:";
        CanonicalNamespaces[1728] = "Event:";
        CanonicalNamespaces[1729] = "Event talk:";

        CanonicalNamespaceAliases =
            PrepareAliases(CanonicalNamespaces);

        CanonicalNamespaceAliases[6].Add("Image:");
        CanonicalNamespaceAliases[7].Add("Image talk:");

        if (!Globals.UnitTestMode)
        {
            // TODO: Remove the dependency on loading English Wikipedia defaults
            // during global initialization. Default namespace and language state
            // should eventually be initialized independently of a specific wiki.
            SetProject(
                "en",
                ProjectEnum.wikipedia);
        }
        else
        {
            SetToEnglish();
            RegenerateRegexes();
        }

        CapitalizeFirstLetter = true;
        IndexPHP = "index.php";
        ApiPHP = "api.php";
        TypoSummaryTag = "typos fixed: ";
        AWBDefaultSummaryTag();
        mSummaryTag = "using ";
        Protocol = "http://";
        NotificationsEnabled = true;
        UnicodeCategoryCollation = false;
    }

    // TODO: Replace legacy revision placeholders with build/version metadata or
    // remove them if no remaining consumers require source-control revision data.

    /// <summary>
    /// Gets the source revision identifier for the current build.
    /// </summary>
    /// <remarks>
    /// The current implementation returns a legacy placeholder value.
    /// </remarks>
    public static string Revision => "?";

    /// <summary>
    /// Gets the numeric source revision for the current build.
    /// </summary>
    /// <remarks>
    /// The current implementation returns a legacy placeholder value.
    /// </remarks>
    public static int RevisionNumber => 0;

    // TODO: Move typo-rule source configuration into the dedicated typo/language
    // quality workflow so individual wikis and languages can select an
    // appropriate rule provider independently.

    /// <summary>
    /// Gets or sets the source location used to load regular-expression
    /// typo-fix rules.
    /// </summary>
    /// <remarks>
    /// The value may be a wiki page such as
    /// <c>Project:AutoWikiBrowser/Typos</c> or a complete URL supplied by wiki
    /// configuration.
    /// </remarks>
    public static string RetfPath;

    // TODO: Remove the global MainForm dependency from Twain.Core by routing
    // application interactions through focused services, events, or interfaces.

    /// <summary>
    /// Gets or sets the application's primary AutoWikiBrowser interface.
    /// </summary>
    /// <remarks>
    /// This legacy global UI reference couples core functionality to the
    /// application shell.
    /// </remarks>
    public static IAutoWikiBrowser MainForm { get; set; }

    /// <summary>
    /// Gets the shared performance profiler used by the application.
    /// </summary>
    /// <remarks>
    /// The field remains mutable for compatibility until existing assignments
    /// have been reviewed.
    /// </remarks>
    public static Profiler Profiler = new();

    #region project and language settings

    /// <summary>
    /// Gets the canonical English names of known MediaWiki namespaces, indexed
    /// by namespace identifier.
    /// </summary>
    public static readonly Dictionary<int, string> CanonicalNamespaces =
        new(20);

    /// <summary>
    /// Gets the canonical namespace aliases derived from
    /// <see cref="CanonicalNamespaces"/>.
    /// </summary>
    public static readonly Dictionary<int, List<string>>
        CanonicalNamespaceAliases;

    /// <summary>
    /// Stores the namespace names for the currently selected wiki, indexed by
    /// namespace identifier.
    /// </summary>
    public static Dictionary<int, string> Namespaces =
        new(40);

    /// <summary>
    /// Stores namespace aliases for the currently selected wiki, indexed by
    /// namespace identifier.
    /// </summary>
    /// <remarks>
    /// Initialization is performed as part of the existing project-loading
    /// workflow and is intentionally not changed here.
    /// </remarks>
    public static Dictionary<int, List<string>> NamespaceAliases;

    /// <summary>
    /// Gets namespace patterns whose first character can be matched without
    /// regard to case, such as <c>[Ww]ikipedia:</c>.
    /// </summary>
    public static readonly Dictionary<int, string>
        NamespacesCaseInsensitive = new(24);

    /// <summary>
    /// Stores MediaWiki magic-word aliases for the currently selected wiki.
    /// </summary>
    public static Dictionary<string, List<string>> MagicWords = new();

    // TODO: Replace the legacy URL fragment fields and concatenation logic with
    // a dedicated wiki endpoint model that constructs and validates API and
    // index URLs.

    /// <summary>
    /// Gets the base URL for the current wiki installation, for example
    /// <c>https://en.wikipedia.org/w/</c>.
    /// </summary>
    public static string URLLong =>
        URL + URLEnd;

    /// <summary>
    /// Gets the URL of the current wiki's <c>index.php</c> endpoint.
    /// </summary>
    public static string URLIndex =>
        URLLong + IndexPHP;

    /// <summary>
    /// Gets the URL of the current wiki's <c>api.php</c> endpoint.
    /// </summary>
    public static string URLApi =>
        URLLong + ApiPHP;

    // TODO: Move HTTP authentication credentials out of global mutable state
    // and into a scoped connection/authentication configuration. Avoid
    // retaining plaintext credentials longer than necessary.

    /// <summary>
    /// Gets or sets the username used for HTTP-level authentication to the
    /// current wiki.
    /// </summary>
    public static string HttpAuthUsername { get; set; }

    /// <summary>
    /// Gets or sets the password used for HTTP-level authentication to the
    /// current wiki.
    /// </summary>
    public static string HttpAuthPassword { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current wiki uses a
    /// right-to-left writing system.
    /// </summary>
    public static bool RTL { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether MediaWiki Echo notifications are
    /// available on the current wiki.
    /// </summary>
    public static bool NotificationsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current wiki capitalizes the
    /// first letter of page titles.
    /// </summary>
    /// <remarks>
    /// This corresponds to MediaWiki's <c>$wgCapitalLinks</c> configuration.
    /// Most Wikimedia projects enable this behavior, while some projects such as
    /// Wiktionary may not.
    /// </remarks>
    public static bool CapitalizeFirstLetter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current wiki uses a Unicode
    /// Collation Algorithm (<c>uca-</c>) category collation.
    /// </summary>
    /// <remarks>
    /// This value is derived from the wiki's <c>$wgCategoryCollation</c>
    /// configuration.
    /// </remarks>
    public static bool UnicodeCategoryCollation { get; set; }

    /// <summary>
    /// Stores the localized month names used by the current wiki.
    /// </summary>
    public static string[] MonthNames;

    /// <summary>
    /// Gets the default English-language month names.
    /// </summary>
    public static readonly string[] ENLangMonthNames =
    {
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December"
};

    private static string URLEnd = "/w/";

    // TODO: Replace the legacy URL and script-path fragments with a dedicated wiki
    // endpoint configuration that validates and constructs base, index, and API
    // URLs consistently.

    /// <summary>
    /// Stores the base URL of the current wiki, for example
    /// <c>https://en.wikipedia.org</c>.
    /// </summary>
    public static string URL = "https://en.wikipedia.org";

    /// <summary>
    /// Gets the host name of the current wiki.
    /// </summary>
    public static string Host =>
        new Uri(URL).Host;

    /// <summary>
    /// Gets the MediaWiki script path for the current wiki, for example
    /// <c>/w</c>.
    /// </summary>
    public static string ScriptPath =>
        URLEnd.Substring(
            0,
            URLEnd.LastIndexOf('/'));

    /// <summary>
    /// Gets the project family associated with the current wiki.
    /// </summary>
    public static ProjectEnum Project { get; private set; }

    /// <summary>
    /// Gets or sets the language code associated with the current wiki, for
    /// example <c>en</c>.
    /// </summary>
    public static string LangCode { get; internal set; }

    // TODO: Replace enum-order-based project classification with explicit project
    // metadata. These checks currently depend on the numeric ordering of
    // ProjectEnum members remaining unchanged.

    /// <summary>
    /// Gets a value indicating whether the current project is a Wikimedia project
    /// represented by the Wikimedia portion of <see cref="ProjectEnum"/>.
    /// </summary>
    public static bool IsWikimediaProject =>
        Project <= ProjectEnum.commons;

    /// <summary>
    /// Gets a value indicating whether the current project is the English
    /// Wikipedia.
    /// </summary>
    public static bool IsWikipediaEN =>
        Project == ProjectEnum.wikipedia &&
        LangCode.Equals("en");

    /// <summary>
    /// Gets a value indicating whether the current project is Wikimedia Commons.
    /// </summary>
    public static bool IsCommons =>
        Project == ProjectEnum.commons;

    /// <summary>
    /// Gets a value indicating whether the current project is a monolingual
    /// Wikimedia project represented by the corresponding
    /// <see cref="ProjectEnum"/> range.
    /// </summary>
    public static bool IsWikimediaMonolingualProject =>
        Project >= ProjectEnum.species &&
        Project < ProjectEnum.wikia;

    /// <summary>
    /// Gets a value indicating whether the current wiki is configured as a custom
    /// project.
    /// </summary>
    public static bool IsCustomProject =>
        Project == ProjectEnum.custom;

    /// <summary>
    /// Gets a value indicating whether the current wiki is a legacy Wikia or
    /// Fandom-hosted wiki.
    /// </summary>
    public static bool IsWikia =>
        Project == ProjectEnum.wikia ||
        Project == ProjectEnum.fandom;

    /// <summary>
    /// Gets the custom project script path, or an empty string when the current
    /// project uses a standard configuration.
    /// </summary>
    public static string CustomProject { get; private set; }

    /// <summary>
    /// Gets or sets the authentication domain used during login when the target
    /// wiki requires one.
    /// </summary>
    public static string LoginDomain { get; set; }

    /// <summary>
    /// Gets the protocol used to access the current wiki.
    /// </summary>
    /// <remarks>
    /// Legacy values include strings such as <c>http://</c> and
    /// <c>https://</c>.
    /// </remarks>
    public static string Protocol { get; private set; }

    /// <summary>
    /// Gets the configured MediaWiki index script name.
    /// </summary>
    /// <remarks>
    /// Legacy configurations may append <c>5</c> when required by the target
    /// wiki.
    /// </remarks>
    public static string IndexPHP { get; private set; }

    /// <summary>
    /// Gets the configured MediaWiki API script name.
    /// </summary>
    /// <remarks>
    /// Legacy configurations may append <c>5</c> when required by the target
    /// wiki.
    /// </remarks>
    public static string ApiPHP { get; private set; }

    /// <summary>
    /// Gets the text used to introduce typo-fix information in edit summaries.
    /// </summary>
    /// <remarks>
    /// The value should not begin with spaces or commas and must end with a
    /// trailing space.
    /// </remarks>
    public static string TypoSummaryTag { get; private set; }

    // TODO: Replace legacy edit-summary tag construction with a focused summary
    // formatter that does not depend on global state or historical AWB naming.

    /// <summary>
    /// Stores the localized equivalent of <c>using</c> used when constructing the
    /// legacy application edit-summary tag.
    /// </summary>
    /// <remarks>
    /// The value does not include leading or trailing spaces.
    /// </remarks>
    private static string mSummaryTag;

    /// <summary>
    /// Gets the localized word used for an untitled section heading.
    /// </summary>
    public static string UntitledHeading { get; private set; }

    /// <summary>
    /// Gets the application attribution text appended to an edit summary.
    /// </summary>
    /// <remarks>
    /// No summary attribution is returned when edit tagging is enabled because the
    /// edit tag identifies the editing application separately.
    /// </remarks>
    public static string SummaryTag
    {
        get
        {
            if (TagEdits)
            {
                // If we're applying an edit tag, don't append "using AWB" to the
                // edit summary.
                return string.Empty;
            }

            string text =
                " " +
                mSummaryTag +
                " " +
                WPAWB;

            text +=
                " (" +
                RevisionNumber +
                ")";

            return text;
        }
    }

    /// <summary>
    /// Gets the wiki-text link or name used to identify AutoWikiBrowser in edit
    /// summaries.
    /// </summary>
    public static string WPAWB { get; private set; }

    /// <summary>
    /// Creates an empty alias collection for each namespace in the supplied
    /// namespace dictionary.
    /// </summary>
    /// <param name="namespaces">
    /// The namespace dictionary whose identifiers should be represented in the
    /// returned alias collection.
    /// </param>
    /// <returns>
    /// A dictionary containing an empty alias list for each namespace identifier.
    /// </returns>
    /// <remarks>
    /// Pre-populating the dictionary prevents callers from encountering
    /// <see cref="KeyNotFoundException"/> when adding aliases for known
    /// namespaces.
    /// </remarks>
    internal static Dictionary<int, List<string>> PrepareAliases(
        Dictionary<int, string> namespaces)
    {
        Dictionary<int, List<string>> aliases =
            new(namespaces.Count);

        foreach (int namespaceId in namespaces.Keys)
        {
            aliases[namespaceId] = new();
        }

        return aliases;
    }
    /// <summary>
    /// Restores the default AutoWikiBrowser attribution text used in edit
    /// summaries.
    /// </summary>
    private static void AWBDefaultSummaryTag()
    {
        mSummaryTag = "using ";
        WPAWB = "[[Project:AWB|AWB]]";
    }

    /// <summary>
    /// Gets or sets a value indicating whether edits should be identified through
    /// a MediaWiki edit tag instead of edit-summary attribution.
    /// </summary>
    public static bool TagEdits;

    #region Delayed load stuff

    // TODO: Replace the legacy BackgroundRequest collection and polling-based
    // synchronization with task-based asynchronous loading and cancellation once
    // project-loading behavior is separated from the WinForms application flow.

    /// <summary>
    /// Gets the list of page titles known to contain underscores.
    /// </summary>
    /// <remarks>
    /// For English Wikipedia this information is historically loaded from
    /// <c>Category:Articles with underscores in the title</c>.
    /// </remarks>
    public static readonly List<string> UnderscoredTitles = new();

    /// <summary>
    /// Tracks outstanding background requests associated with delayed project
    /// configuration loading.
    /// </summary>
    private static readonly List<BackgroundRequest> DelayedRequests = new();

    /// <summary>
    /// Adds page titles to <see cref="UnderscoredTitles"/> when running in unit-test
    /// mode.
    /// </summary>
    /// <param name="titles">
    /// The page titles to add.
    /// </param>
    /// <remarks>
    /// This method has no effect outside unit-test mode.
    /// </remarks>
    public static void AddUnderscoredTitles(List<string> titles)
    {
        if (Globals.UnitTestMode)
            UnderscoredTitles.AddRange(titles);
    }

    /// <summary>
    /// Aborts and removes all outstanding delayed background requests.
    /// </summary>
    private static void CancelBackgroundRequests()
    {
        lock (DelayedRequests)
        {
            foreach (BackgroundRequest request in DelayedRequests)
            {
                request.Abort();
            }

            DelayedRequests.Clear();
        }
    }

    /// <summary>
    /// Blocks until all delayed background requests have completed or been removed.
    /// </summary>
    /// <remarks>
    /// The current implementation polls the shared request collection at
    /// 100-millisecond intervals.
    /// </remarks>
    public static void WaitForDelayedRequests()
    {
        do
        {
            lock (DelayedRequests)
            {
                if (DelayedRequests.Count == 0)
                    return;
            }

            Thread.Sleep(100);
        }
        while (true);
    }

    /// <summary>
    /// Begins loading pages whose titles contain underscores from the supplied
    /// category names.
    /// </summary>
    /// <param name="categories">
    /// The category names used to populate <see cref="UnderscoredTitles"/>.
    /// </param>
    internal static void LoadUnderscores(params string[] categories)
    {
        if (categories.Length == 0)
        {
            return;
        }

        BackgroundRequest request =
            new(UnderscoresLoaded)
            {
                HasUI = false
            };

        lock (DelayedRequests)
        {
            DelayedRequests.Add(request);
        }

        request.GetList(
            new CategoryListProvider(),
            categories);
    }

    /// <summary>
    /// Applies the result of a completed underscore-title background request.
    /// </summary>
    /// <param name="request">
    /// The completed request containing the loaded article list.
    /// </param>
    private static void UnderscoresLoaded(BackgroundRequest request)
    {
        lock (DelayedRequests)
        {
            DelayedRequests.Remove(request);
            UnderscoredTitles.Clear();

            foreach (Article article in (List<Article>)request.Result)
            {
                UnderscoredTitles.Add(article.Name);
            }
        }
    }

    #endregion

    #region Proxy support

    /// <summary>
    /// Refreshes the HTTP proxy used by application networking operations.
    /// </summary>
    public static void RefreshProxy()
    {
        Networking.AwbHttpClient.RefreshProxy();
    }

    #endregion

    /// <summary>
    /// Formats an AutoWikiBrowser version line for diagnostic or logging output.
    /// </summary>
    /// <param name="version">
    /// The application version to include.
    /// </param>
    /// <returns>
    /// A formatted wiki-text line containing the application link and version.
    /// </returns>
    // TODO: Move AWB-branded diagnostic/log formatting out of Variables during the
    // broader Twain naming and diagnostics cleanup.
    public static string AWBVersionString(string version)
    {
        return
            "*" +
            WPAWB +
            " version " +
            version +
            Environment.NewLine;
    }

    /// <summary>
    /// Stores the regular-expression fragment used to identify stub templates.
    /// </summary>
    public static string Stub;

    /// <summary>
    /// Stores the regular-expression fragment used to identify section-stub
    /// templates.
    /// </summary>
    public static string SectStub;

    /// <summary>
    /// Stores the compiled regular expression used to identify section-stub
    /// templates.
    /// </summary>
    public static Regex SectStubRegex;

    /// <summary>
    /// Configures the current project using its language code and project family.
    /// </summary>
    /// <param name="langCode">
    /// The language code, such as <c>en</c>.
    /// </param>
    /// <param name="projectName">
    /// The project family to configure.
    /// </param>
    public static void SetProject(
        string langCode,
        ProjectEnum projectName)
    {
        SetProject(
            langCode,
            projectName,
            "",
            "https://");
    }

    /// <summary>
    /// Sets the language-specific values used by the current project.
    /// </summary>
    /// <param name="langCode">
    /// The language code to use.
    /// </param>
    /// <remarks>
    /// This method is intended for unit tests and should not be used by normal
    /// application code.
    /// </remarks>
    public static void SetProjectLangCode(string langCode)
    {
        SetLanguageSpecificValues(
            langCode,
            ProjectEnum.wikipedia);

        LangCode = langCode;

        RTL = langCode.Equals("ar");
    }

    /// <summary>
    /// Sets a simplified project and language configuration for unit testing.
    /// </summary>
    /// <param name="langCode">
    /// The language code to use.
    /// </param>
    /// <param name="projectName">
    /// The project family to configure.
    /// </param>
    /// <remarks>
    /// This method is intended for unit tests and should not be used by normal
    /// application code.
    /// </remarks>
    public static void SetProjectSimple(
        string langCode,
        ProjectEnum projectName)
    {
        Project = projectName;
        SetProjectLangCode(langCode);
    }

    /// <summary>
    /// Gets a value indicating whether project loading should be retried after
    /// authentication completes.
    /// </summary>
    public static bool TryLoadingAgainAfterLogin { get; private set; }

    /// <summary>
    /// Stores project settings that must be reapplied after authentication.
    /// </summary>
    public static ProjectHoldArea ReloadProjectSettings;

    // TODO: Replace ProjectHoldArea with a properly named immutable project
    // configuration/state object. The current mutable public-field shape is legacy
    // state retained for compatibility with the login retry workflow.

    /// <summary>
    /// Stores project settings temporarily while project loading is deferred for
    /// authentication.
    /// </summary>
    public class ProjectHoldArea
    {
        /// <summary>
        /// The project family being configured.
        /// </summary>
        public ProjectEnum projectName;

        /// <summary>
        /// The language code being configured.
        /// </summary>
        public string langCode;

        /// <summary>
        /// The custom project identifier or script path.
        /// </summary>
        public string customProject;

        /// <summary>
        /// The protocol used to access the project.
        /// </summary>
        public string protocol;
    }

    /// <summary>
    /// Configures the current wiki project and refreshes project-specific state.
    /// </summary>
    /// <param name="langCode">
    /// The language code to use.
    /// </param>
    /// <param name="projectName">
    /// The project family to configure.
    /// </param>
    /// <param name="customProject">
    /// The custom project host, identifier, or script path used by custom,
    /// Wikia, or Fandom projects.
    /// </param>
    /// <param name="protocol">
    /// The protocol used to access the project.
    /// </param>
    /// <remarks>
    /// This method updates shared project state, cancels outstanding background
    /// requests, refreshes project metadata, and may defer initialization until
    /// authentication succeeds.
    /// </remarks>
    public static void SetProject(
        string langCode,
        ProjectEnum projectName,
        string customProject,
        string protocol)
    {
        TryLoadingAgainAfterLogin = false;

        Namespaces.Clear();
        CancelBackgroundRequests();
        UnderscoredTitles.Clear();
        WikiRegexes.TemplateRedirects.Clear();

        bool typoReloadNeeded =
            LangCode != langCode ||
            Project != projectName ||
            customProject != CustomProject;

        Project = projectName;
        LangCode = langCode;
        CustomProject = customProject;
        Protocol = protocol;

        RefreshProxy();

        URLEnd = "/w/";

        Stub = "[^{}|]*?[Ss]tub";

        // TODO: Confirm whether MonthNames should reference the shared English array
        // or receive a clone. Preserve the existing reference assignment until its
        // mutation behavior has been reviewed.
        MonthNames = ENLangMonthNames;

        SectStub = @"\{\{[Ss]ect";
        SectStubRegex =
            new Regex(
                SectStub,
                RegexOptions.Compiled);

        TypoSummaryTag = "typos fixed: ";
        AWBDefaultSummaryTag();
        mSummaryTag = "using";
        NotificationsEnabled = true;

        if (IsCustomProject)
        {
            LangCode = "en";

            var uri =
                new Uri(
                    Protocol +
                    customProject);

            URLEnd = uri.AbsolutePath;

            URL =
                protocol +
                uri.Host;

            if (!uri.IsDefaultPort)
            {
                URL +=
                    ":" +
                    uri.Port;
            }

            CustomProject = customProject;
        }
        else
        {
            URL =
                "https://" +
                LangCode +
                "." +
                Project +
                ".org";
        }

        // TODO: Replace project-specific URL and language overrides with explicit
        // project metadata/configuration rather than a growing switch statement.
        switch (projectName)
        {
            case ProjectEnum.wikipedia:
            case ProjectEnum.wikinews:
            case ProjectEnum.wikisource:
            case ProjectEnum.wikibooks:
            case ProjectEnum.wikiquote:
            case ProjectEnum.wiktionary:
            case ProjectEnum.wikiversity:
                SetLanguageSpecificValues(
                    langCode,
                    projectName);
                break;

            case ProjectEnum.commons:
                URL = "https://commons.wikimedia.org";
                LangCode = "en";
                break;

            case ProjectEnum.meta:
                URL = "https://meta.wikimedia.org";
                LangCode = "en";
                break;

            case ProjectEnum.mediawiki:
                URL = "https://www.mediawiki.org";
                LangCode = "en";
                break;

            case ProjectEnum.incubator:
                URL = "https://incubator.wikimedia.org";
                LangCode = "en";
                break;

            case ProjectEnum.species:
                URL = "https://species.wikimedia.org";
                LangCode = "en";
                break;

            case ProjectEnum.wikidata:
                URL = "https://www.wikidata.org";
                LangCode = "en";
                break;

            case ProjectEnum.wikia:
                URL =
                    "https://" +
                    customProject +
                    ".wikia.com";

                URLEnd = "/";
                break;

            case ProjectEnum.fandom:
                URL =
                    "https://" +
                    customProject +
                    ".fandom.com";

                URLEnd = "/";
                break;

            case ProjectEnum.custom:
                break;
        }

        // Refresh once more in case project settings were reset due to an error
        // while loading.
        RefreshProxy();

        // Project initialization currently depends on the active WinForms session.
        // If a wiki requires authentication before project data can be read, retain
        // the requested settings and retry after login instead of applying partial
        // state.
        //
        // TODO: Separate project-state configuration from MainForm/session
        // coordination so project switching can be validated and tested without UI
        // coupling.
        if (MainForm != null &&
            MainForm.TheSession != null)
        {
            try
            {
                if (!MainForm.TheSession.UpdateProject(false))
                {
                    LangCode = "en";
                    Project = ProjectEnum.wikipedia;
                    SetToEnglish();
                }
            }
            catch (ReadApiDeniedException)
            {
                TryLoadingAgainAfterLogin = true;

                ReloadProjectSettings =
                    new ProjectHoldArea
                    {
                        projectName = projectName,
                        customProject = customProject,
                        langCode = langCode,
                        protocol = Protocol
                    };

                return;
            }
        }

        RegenerateRegexes();

        if (projectName == ProjectEnum.wiktionary)
            CapitalizeFirstLetter = false;

        RetfPath =
            Namespaces[Namespace.Project] +
            "AutoWikiBrowser/Typos";

        if (typoReloadNeeded &&
            MainForm != null)
        {
            MainForm.LoadTypos(true);
        }

        foreach (string namespaceName in Namespaces.Values)
        {
            System.Diagnostics.Trace.Assert(
                namespaceName.EndsWith(":"),
                "Internal error: namespace does not end with ':'.",
                "Please contact a developer.");
        }

        System.Diagnostics.Trace.Assert(
            !Namespaces.ContainsKey(0),
            "Internal error: key exists for namespace 0.",
            "Please contact a developer.");
    }
    /// <summary>
    /// Rebuilds namespace-aware regular expressions for the current project and
    /// refreshes other language-specific regex patterns.
    /// </summary>
    private static void RegenerateRegexes()
    {
        NamespacesCaseInsensitive.Clear();

        foreach (int namespaceId in Namespaces.Keys)
        {
            NamespacesCaseInsensitive.Add(
                namespaceId,
                "(?i:" +
                WikiRegexes.GenerateNamespaceRegex(namespaceId) +
                @")\s*:");
        }

        WikiRegexes.MakeLangSpecificRegexes();
    }

    /// <summary>
    /// Restores the default English Wikipedia namespace and language-specific
    /// configuration.
    /// </summary>
    /// <remarks>
    /// This method resets namespace names, summary attribution, localized month
    /// names, stub-detection expressions, and capitalization behavior to their
    /// English defaults.
    /// </remarks>
    private static void SetToEnglish()
    {
        foreach (int namespaceId in CanonicalNamespaces.Keys)
        {
            Namespaces[namespaceId] =
                CanonicalNamespaces[namespaceId];
        }

        Namespaces[4] = "Wikipedia:";
        Namespaces[5] = "Wikipedia talk:";
        Namespaces[100] = "Portal:";
        Namespaces[101] = "Portal talk:";
        Namespaces[118] = "Draft:";
        Namespaces[119] = "Draft talk:";
        Namespaces[126] = "MOS:";
        Namespaces[127] = "MOS talk:";
        Namespaces[710] = "TimedText:";
        Namespaces[711] = "TimedText talk:";
        Namespaces[828] = "Module:";
        Namespaces[829] = "Module talk:";

        mSummaryTag = "using";
        WPAWB = "[[Project:AWB|AWB]]";

        NamespaceAliases = CanonicalNamespaceAliases;

        // TODO: Confirm whether these shared arrays/dictionaries should remain
        // referenced directly or whether project state should receive independent
        // copies. Preserve the existing reference behavior for compatibility.
        MonthNames = ENLangMonthNames;

        SectStub = @"\{\{[Ss]ect";
        SectStubRegex =
            new Regex(
                SectStub,
                RegexOptions.Compiled);

        Stub = "[^{}|]*?[Ss]tub";

        LangCode = "en";

        RTL = false;
        CapitalizeFirstLetter = true;
    }

    // TODO: Replace hard-coded language-specific summary text, stub patterns, and
    // project links with data-driven localization/configuration. This switch mixes
    // localization, wiki behavior, and regex configuration in a single method and
    // is difficult to extend or validate independently.

    /// <summary>
    /// Applies language-specific summary text, stub-detection expressions,
    /// project links, and related localized settings.
    /// </summary>
    /// <param name="langCode">
    /// The language code of the current wiki.
    /// </param>
    /// <param name="projectName">
    /// The project family of the current wiki.
    /// </param>
    private static void SetLanguageSpecificValues(
        string langCode,
        ProjectEnum projectName)
    {
        UntitledHeading = "Untitled";

        switch (langCode)
        {
            case "en":
                if (projectName == ProjectEnum.wikipedia)
                {
                    SetToEnglish();
                    WPAWB = "[[WP:AWB|AWB]]";
                }

                TypoSummaryTag =
                    @"[[WP:AWB/T|typo(s) fixed]]: ";
                break;

            case "ar":
                mSummaryTag = string.Empty;
                WPAWB = "باستخدام [[Project:أوب|أوب]]";
                Stub =
                    @"[^{}|]*?(?:[Ss]tub|بذرة|بذور)[^{}]*?";
                TypoSummaryTag =
                    "الأخطاء المصححة: ";
                break;

            case "arz":
                mSummaryTag = string.Empty;
                WPAWB = "عن طريق [[Project:AWB|اوب]]";
                Stub =
                    @"[^{}|]*?(?:[Ss]tub|تقاوى|بذرة)[^{}]*?";
                TypoSummaryTag =
                    "الأخطاء المصححة: ";
                break;

            case "be":
                mSummaryTag = "з дапамогай";
                break;

            case "bg":
                mSummaryTag = "редактирано с";
                WPAWB = "AWB";
                break;

            case "bn":
                mSummaryTag = string.Empty;
                WPAWB =
                    "[[Project:অউব্রা|অউব্রা]] ব্যবহার করে";
                TypoSummaryTag =
                    "বানান সংশোধন: ";
                break;

            case "ca":
                mSummaryTag = string.Empty;
                WPAWB =
                    "[[Viquipèdia:AutoWikiBrowser|AWB]]";
                break;

            case "cs":
                mSummaryTag = "za použití";
                WPAWB =
                    "[[Wikipedie:AutoWikiBrowser|AWB]]";
                Stub =
                    @"[^{}|]*?([Pp]ahýl)";
                break;

            case "cy":
                Stub =
                    @"[^{}|]*?([Ss]tub|[Εe]ginyn[^{}]*?)";
                break;

            case "da":
                mSummaryTag = "ved brug af";
                WPAWB = "[[en:WP:AWB|AWB]]";
                break;

            case "de":
                mSummaryTag = "mit";
                TypoSummaryTag = "Schreibweise: ";
                break;

            case "el":
                mSummaryTag = "με τη χρήση";
                Stub =
                    @"[^{}|]*?([Ss]tub|[Εε]πέκταση)";
                SectStub =
                    @"\{\{θέματος";
                SectStubRegex =
                    new Regex(
                        SectStub,
                        RegexOptions.Compiled);
                break;

            case "eo":
                mSummaryTag = "per";
                WPAWB =
                    "[[Vikipedio:AutoWikiBrowser|AWB]]";
                TypoSummaryTag =
                    "Skribmaniero: ";
                break;

            case "es":
                mSummaryTag = "(Usando";
                WPAWB =
                    "[[:w:WP:AWB|AWB]])";
                break;

            case "fa":
                mSummaryTag = string.Empty;
                WPAWB =
                    "با استفاده از [[Project:AutoWikiBrowser|AWB]]";
                break;

            case "fr":
                mSummaryTag = "avec";
                WPAWB =
                    "[[Wikipédia:AutoWikiBrowser|AWB]]";
                break;

            case "he":
                mSummaryTag = "באמצעות";
                WPAWB =
                    "[[ויקיפדיה:AutoWikiBrowser|AWB]]";
                break;

            case "hi":
                mSummaryTag = string.Empty;
                WPAWB =
                    "[[विकिपीडिया:ऑटोविकिब्राउज़र|AWB]] के साथ";
                break;

            case "hu":
                mSummaryTag = string.Empty;
                WPAWB =
                    "[[Wikipédia:AutoWikiBrowser|AWB]]";
                break;

            case "hy":
                mSummaryTag = "oգտվելով";
                WPAWB =
                    "[[Վիքիպեդիա:ԱվտոՎիքիԲրաուզեր|ԱՎԲ]]";
                Stub =
                    @"[^{}|]*?([Ss]tub|Անավարտ|Զարգացնել[^{}]*?)";
                break;

            case "it":
                mSummaryTag = string.Empty;
                Stub = @"(DUMMYTEMPLATE)";
                break;

            case "ku":
                mSummaryTag = string.Empty;
                WPAWB =
                    "[[Wîkîpediya:AutoWikiBrowser|AWB]]";
                break;

            case "ms":
                mSummaryTag = "menggunakan";
                break;

            case "ne":
                mSummaryTag = string.Empty;
                WPAWB =
                    "स्वतःविकी ब्राउजर प्रयोग गर्दै";
                break;

            case "nl":
                mSummaryTag = "met";
                break;

            case "pa":
                mSummaryTag = "ਦੀ ਵਰਤੋਂ ਨਾਲ";
                break;

            case "pl":
                mSummaryTag = "przy użyciu";
                SectStub =
                    @"\{\{[Ss]ek";
                SectStubRegex =
                    new Regex(
                        SectStub,
                        RegexOptions.Compiled);
                break;

            case "pt":
                mSummaryTag = "utilizando";
                break;

            case "ru":
                mSummaryTag = "с помощью";
                Stub =
                    "[^{}]*?(?:[Ss]tub|[Зз]аготовка)";
                break;

            case "sco":
                Stub =
                    "(Stub/[^{}|]+|[^{}|]*?[Ss]tub)";
                break;

            case "sk":
                mSummaryTag = string.Empty;
                break;

            case "sl":
                mSummaryTag = string.Empty;
                Stub =
                    "(?:[^{}]*?[Ss]tub|[Šš]krbina[^{}]*?)";
                break;

            case "sq":
                mSummaryTag = "duke përdorur";
                TypoSummaryTag =
                    @"[[WP:AWB/T|përmirësime tipografike]]: ";
                Stub =
                    "(?:[^{}]*?[Cc]ung[^{}]*?)";
                break;

            case "sr":
                mSummaryTag = "користећи";
                WPAWB = "[[Project:AWB|AWB]]";
                UntitledHeading =
                    "Први поднаслов";
                break;

            case "sv":
                mSummaryTag = "med";
                TypoSummaryTag =
                    "rättar stavfel: ";
                Stub =
                    @"(?:[^{}]*?[Ss]tub|[^{}]+?stub(?:[ \-][^{}]+)?)(?<![Ss]tubbmall|[Ss]ubstub|[Ss]tubavsnitt|[Uu]ncategorized stub)";
                break;

            case "tr":
                mSummaryTag = string.Empty;
                WPAWB =
                    "[[Vikipedi:AWB|AWB]] ile ";
                TypoSummaryTag =
                    "yazış şekli: ";
                break;

            case "uk":
                Stub =
                    "[^{}|]*?(?:[Ss]tub|[Дд]оробити)";
                SectStub =
                    @"\{\{([Рp]озділ\-доробити|[Ss]ection[ \-]stub)";
                SectStubRegex =
                    new Regex(
                        SectStub,
                        RegexOptions.Compiled);
                mSummaryTag =
                    "за допомогою";
                break;

            case "ur":
                TypoSummaryTag =
                    "درستی املا";
                break;

            case "zh":
                Stub =
                    ".*?(?:小作品|[Ss]tub)";
                mSummaryTag = "由";
                WPAWB =
                    "[[维基百科:自动维基浏览器|自动维基浏览器]]协助";
                UntitledHeading =
                    "無標題";
                break;

            case "zh-classical":
                mSummaryTag = "藉";
                WPAWB =
                    "[[維基大典:自動維基瀏覽器|自動維基瀏覽器]]之助";
                break;

            case "zh-yue":
                mSummaryTag = "用";
                WPAWB =
                    "[[Wikipedia:AutoWikiBrowser|AWB]]幫手";
                break;

                // Add additional language-specific overrides here only when required.
        }
    }

    #endregion

    #region URL Builders

    // TODO: Move URL construction to the dedicated wiki endpoint/configuration
    // abstraction so callers do not need to build query strings from global state.

    /// <summary>
    /// Gets the non-prettified URL for the specified wiki page using the current
    /// project configuration.
    /// </summary>
    /// <param name="title">
    /// The page title to include in the URL.
    /// </param>
    /// <returns>
    /// The full <c>index.php?title=...</c> URL for the page.
    /// </returns>
    public static string NonPrettifiedURL(string title) =>
        URLIndex +
        "?title=" +
        Tools.WikiEncode(title);

    /// <summary>
    /// Gets the history URL for the specified wiki page.
    /// </summary>
    /// <param name="title">
    /// The page title whose history should be displayed.
    /// </param>
    /// <returns>
    /// The full URL to the page history.
    /// </returns>
    public static string GetArticleHistoryURL(string title) =>
        NonPrettifiedURL(title) +
        "&action=history";

    /// <summary>
    /// Gets the edit URL for the specified wiki page.
    /// </summary>
    /// <param name="title">
    /// The page title to edit.
    /// </param>
    /// <returns>
    /// The full URL to the page edit form.
    /// </returns>
    public static string GetEditURL(string title) =>
        NonPrettifiedURL(title) +
        "&action=edit";

    /// <summary>
    /// Gets the user-talk URL for the specified username.
    /// </summary>
    /// <param name="username">
    /// The username whose talk page should be opened.
    /// </param>
    /// <returns>
    /// The full URL to the user's talk page, including the existing purge action.
    /// </returns>
    public static string GetUserTalkURL(string username) =>
        URLIndex +
        "?title=User_talk:" +
        Tools.WikiEncode(username) +
        "&action=purge";

    /// <summary>
    /// Gets the raw-text URL for the specified wiki page.
    /// </summary>
    /// <param name="title">
    /// The page title whose raw text should be retrieved.
    /// </param>
    /// <returns>
    /// The full URL to the page using <c>action=raw</c>.
    /// </returns>
    public static string GetPlainTextURL(string title) =>
        NonPrettifiedURL(title) +
        "&action=raw";

    #endregion
}

/// <summary>
/// Identifies the current operational or registration state of a wiki session.
/// </summary>
public enum WikiStatusResult
{
    /// <summary>
    /// An unexpected error occurred while determining session status.
    /// </summary>
    Error,

    /// <summary>
    /// The current user is not logged in.
    /// </summary>
    NotLoggedIn,

    /// <summary>
    /// The current user is not registered or otherwise permitted by the
    /// configured access rules.
    /// </summary>
    NotRegistered,

    /// <summary>
    /// The running application version is not permitted by the configured
    /// version policy.
    /// </summary>
    OldVersion,

    /// <summary>
    /// The current user does not have the required rights.
    /// </summary>
    NoRights,

    /// <summary>
    /// The current user is registered and may proceed.
    /// </summary>
    Registered,

    /// <summary>
    /// Session status has been marked for reevaluation.
    /// </summary>
    PendingUpdate
}