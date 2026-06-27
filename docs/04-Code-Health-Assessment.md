\# 04. Code Health Assessment



\## Executive Summary



This document provides a baseline assessment of the current health of the AutoWikiBrowser codebase prior to migration to .NET 8. Its purpose is to identify technical debt, legacy implementation patterns, compiler warnings, code quality concerns, and maintainability issues that may affect the modernization effort.



The assessment will inventory existing TODO, FIXME, and HACK comments, evaluate static analysis findings, identify deprecated APIs, review project complexity, and document areas requiring refactoring or additional testing.



The objective is not to resolve these issues immediately, but to establish a measurable baseline that supports informed migration decisions and allows progress to be tracked throughout the modernization effort.





\## 2. Assessment Objectives

The purpose of this assessment is to establish a baseline understanding of the overall health of the AutoWikiBrowser codebase prior to migration to .NET 8.



The assessment focuses on identifying:



\* Existing technical debt

\* Compiler warnings and build issues

\* Legacy implementation patterns

\* Deprecated APIs and technologies

\* Code complexity and maintainability concerns

\* Testing coverage

\* Opportunities for modernization discovered during the migration process



The assessment is intended to document the current state of the codebase rather than prescribe immediate solutions. Findings will be used to prioritize improvements throughout the migration.





\## 3. Compiler Warnings

Compiler warnings will be reviewed to identify existing issues that may affect the migration to .NET 8.



The assessment will document:



\* Current warning counts by project

\* Warning categories and severity

\* Warnings introduced by obsolete APIs

\* Nullable reference type considerations

\* Opportunities to reduce or eliminate compiler warnings during migration



Compiler warnings identified during this assessment will be prioritized according to their potential impact on build stability, application behavior, and long-term maintainability.





\## 4. TODO / FIXME / HACK Inventory

The existing codebase contains developer annotations accumulated over many years of development. These comments provide valuable insight into known issues, technical debt, unfinished work, and historical design decisions.



This assessment will inventory annotations including:



\* TODO

\* FIXME

\* HACK

\* XXX

\* NOTE (where appropriate)



| Type  	| Count | Priority | Migration Impact | Status  |

| ------------- | :----:|: ------: |: --------------: |---------|

| TODO		|   47  | TBD      | TBD              | Pending |

| FIXME		|   4	| TBD      | TBD              | Pending |

| HACK		|   10  | TBD      | TBD              | Pending |

| Workaround	|   6 	| TBD      | TBD              | Pending |

| SourceForge-GIT | 14	| TBD	   | TBD	      | Pending	|

| Out of sync	| 	|	   |		      |		|



Each item will be classified by project, file, priority, and relevance to the .NET 8 migration.



The objective is to distinguish between items that should be addressed during migration and those that can be deferred for future modernization efforts.



\### Updates out of sync from Sourceforge and GITHUB

|Commit # | Change description| Change date|

|---------|:------------------------------------------:|:-------------------:|

|\[r13011] | AWBWebBrowser: Set User-Agent for requests using embedded web browser | 2026-01-10|

|\[r13012] | T403895 revision: Don't reset default delay; allow for a long wait    | 2026-02-05|

|\[r13013] | T415566: Remove obsolete SecurityAction.Demand decoration from MainForm class | 2026-02-05|

|\[r13014] | T416505 Fix possible RetryAfter errors in AWB and plugins (actually in List provider), plus two minor bugs  | 2026-02-11|

|\[r13015] | T421588: Handle rate limiting in WikiFunctions - step 1, implement GetHTML() retries| 2026-03-29|

|\[r13016] | T421588: Restore countdown timer for in-app retries| 2026-03-31|

|\[r13017] | T421588: New Tools.GetHTML versions to pass authentication information| 2026-04-04|

|\[r13018] | T421991: Don't request notifications if you don't have the right| 2026-04-11|

|\[r13019] | T399860 redux: better messages on 2FA failures| 2026-05-11|

|\[r13020] | Release version 6.5.0.0| 2026-05-12|

|\[r13021] | Tag release 6.5.0.0| 2026-05-12|

|\[r13022] | Bump 6.5.0.1| 2026-05-12|

|\[r13023] | Belatedly update About box copyright year| 2026-05-18|

|\[r13024] | T428372: AutoWikiBrowser memory leak when preprocessing a large list| 2026-06-15|



\### TODO

|Code |File 	|Line |Column

\------------------------------------------------|:-------------------:|:-----------:------|

|// TODO: Were is CLR version of MaxPath defined? Can't find it in Environment. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\WindowsNameTransform.cs | 227 | 7 |

|// TODO:Move any code that doesn't need to be directly behind the form to WF or other code files (Preferably WF) | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 54 | 8 |

|// TODO:Move regexes declared in method bodies (if not dynamic based on article title, etc), into class body 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 55 | 8 |

|// TODO:Move any Regexes to WikiRegexes as required | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 56 | 8 |

|// TODO: must be a less crude way 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 618 | 28 |

|// TODO:Reinstate as needed | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 1473 | 16 |

|//\* TODO: does not work fully in that: focus always scrolls to current line unnecessarily | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 3073 | 16 |

|// TODO: Cleanup/refactor UI update functions | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 3391 | 12 |

|// TODO: Doesn't always stop | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 4133 | 11 |

|// TODO: Should we be setting a default? 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 4429 | 28 |

|// TODO: Try to use TheSession.Site.ArticleUrl for prettier URL | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 4443 | 28 |

|// TODO:ApiEdit PageExists/similar function (wrapper for this, we don't need/care about page text) | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs | 5753 | 20 |

|// TODO:Use Utils 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\PluginManager.cs | 71 | 11 |

|// TODO:Reinstate/Use? | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Preferences.cs 	| 339 | 11 |

|// TODO: Add other stuff we'd like to track | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Stats.cs | 112 | 12 |

|// TODO: Here or in PHP: tl.wikipedia.org CUS: Translate to site name/lang code any Wikimedia site set up as custom 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Stats.cs | 215 | 16 |

|// TODO: suggest a bug report for other exceptions 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ErrorHandler.cs 	| 104 | 16 |

|// TODO: Phab urls 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ErrorHandler.cs 	| 176 | 20 |

|// TODO: Check to see if combining RE's makes it faster/smaller. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Core\\NameFilter.cs | 249 | 7 |

|// TODO: This should be a long? 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\Compression\\Inflater.cs | 858 | 7 |

|// TODO Path.GetDirectory can fail here on invalid characters. 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\FastZip.cs 	| 472 | 10 |

|// TODO: FastZip - Setting of other file attributes on extraction is a little trickier. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\FastZip.cs 	| 606 | 11 |

|// TODO: Fire delegate/throw exception were compression method not supported, or name is invalid? | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\FastZip.cs 	| 641 | 7 |

|// TODO: A better estimation of the true limit based on compression overhead should be used 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipEntry.cs 	| 640 | 9 |

|// TODO: This is slightly safer but less efficient. Think about wether it should change. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipEntry.cs 	| 858 | 4 |

|// TODO: Sort out wether tagged data is useful and what a good implementation might look like. 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipExtraData.cs | 42 | 5 |

|// TODO: This will be slow as the next ice age for huge archives! | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 723 | 7 |

|// TODO: the 'Corrina Johns' test where local headers are missing from 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 978 | 8 |

|// TODO: make test more correct... can't compare lengths as was done originally as this can fail for MBCS strings | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 1198 | 24 |

|// TODO: Local offset will require adjusting for multi-disk zip files. 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 1890 | 7 |

|// TODO: Need to clear any entry flags that dont make sense or throw an exception here. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 1893 | 7 |

|// TODO: This is slow if the changes don't effect the data!! 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 2465 | 7 |

|// TODO: Add base for SFX friendly handling | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 2495 | 7 |

|// TODO: Stop re-reading name and data length in CopyEntryDirect. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 2501 | 7 |

|// TODO: Find out why this calculation comes up 4 bytes short on some entries in ODT (Office Document Text) archives. 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 2511 | 9 |

|// TODO: This wont work for SFX files! | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 2535 | 8 |

|// TODO: archiveStorage wasnt originally intended for this use. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 2584 | 9 |

|// TODO: Direct modifying of an entry will take some legwork. 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 2727 | 12 |

|// TODO: Difficulty with Zip64 and SFX offset handling needs resolution - maths? | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipFile.cs 	| 3179 | 7 |

|// TODO: ZipHelperStream.WriteLocalHeader is not yet used and needs checking for ZipFile and ZipOuptutStream usage | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipHelperStream.cs 	| 216 | 6 |

|// TODO: This loop could be optimised for speed. 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipHelperStream.cs 	| 328 | 7 |

|// TODO: ZipFile Multi disk handling not done | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipHelperStream.cs 	| 392 | 7 |

|// TODO: Its not yet clear how to handle unicode comments here. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipOutputStream.cs 	| 144 | 7 |

|// TODO: Refactor header writing. Its done in several places. 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWBUpdater\\ICSharpCode.SharpZipLib\\Zip\\ZipOutputStream.cs 	| 345 | 7 |

|// TODO: move it to parts testing specific functions, when they're covered | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\UnitTests\\FixSyntaxTests.cs | 1220 | 16 |

|// TODO: cover everything | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\UnitTests\\FormattingTests.cs | 771 | 12 |

|// TODO: uncomment when Namespace.Determine() will support non-normalised names | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\UnitTests\\MiscellaneousTests.cs 	| 745 | 16 |

|// TODO: decide if such improvements really belong here | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\UnitTests\\ParserTests.cs 	| 250 | 16 |

|// normalising Media: is not yet supported, see TODO in BasicImprovements() 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\UnitTests\\ParserTests.cs 	| 380 | 61 |

|// TODO: refactor XML parsing | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 34 | 7 |

|// TODO: generalise edit token retrieval 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 35 | 7 |

|string s = kvp.Key;|// TODO: This is probably redundant now 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 239 | 40 |

|// TODO: Implement better validation. JOE 20110722 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 364 | 16 |

|// TODO: Probably should do this somewhere else/earlier... At first request to the API/wiki? 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 441 | 24 |

|// TODO: Not Json friendly 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 1344 | 16 |

|queryParameters);|// TODO: Should we be checking for maxlag? 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 1363 | 38 |

|// TODO: can't figure out the best time for this check | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 1593 | 15 |

|// TODO: adopt for retrieval of information for protection, deletion, etc. | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\PageInfo.cs | 33 | 12 |

|// TODO: 2009-01-28 review which of the genfixes below should be labelled 'significant' | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Article\\Article.cs 	| 1393 | 14 |

|// TODO: handle things like "bad regex" here| | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Article\\Comparers\\ArticleComparerFactory.cs 	| 65 | 24 |

|// checks links to make them bypass redirects and (TODO) disambigs | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Background\\Background.cs | 278 | |

|text = editor.Open(article, false); |// TODO:Resolve redirects betterer | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Background\\Background.cs | 323 | 67 |

|// TODO bold support, sub/sup support, mulitple italics support, span to hide support 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Controls\\Lists\\ListMaker.cs 	| 1514 | 24 |

|// TODO: This feels weird 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Controls\\PageContainsControl.cs | 18 | 16 |

|// TODO:Load proper protection levels | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\DBScanner\\DatabaseScanner.cs 	| 35 | 7 |

|// TODO:Update TextContains etc to use Inheritors of IArticleComparer 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\DBScanner\\Scanners.cs | 50 | 7 |

|// TODO: suggest a bug report for other exceptions 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\ErrorHandler.cs | 106 | 16 |

|// TODO: Phab urls 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\ErrorHandler.cs | 178 | 20 |

|// TODO: error handling 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Lists\\Providers\\ApiListProviderBase.cs | 70 | 16 |

|// TODO: normalise usage of FirstToUpperAndRemoveHashOnArray() and alikes | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Lists\\Providers\\ListProviders.cs | 29 | 7 |

|// TODO resolve exception by prevention rather than simply catching 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Logging\\AWBLogListener.cs 	| 100 | 16 |

|// TODO: User IArticleComparer derivatives where possible | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\FindandReplace.cs | 28 | 7 |

|// TODO: only works when there is another section following the references section | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\MetaDataSorter.cs | 1171 | 12 |

|// TODO:Move Regexes to WikiRegexes as required 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\Parsers.cs | 28 | 7 |

|// TODO:Move regexes declared in method bodies (if not dynamic based on article title, etc), into class body 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\Parsers.cs | 29 | 7 |

|// TODO: wikitravel support? | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\Parsers.cs | 1697 | 16 |

|// TODO: This doesn't work against authenticated wikis, need to load via Editor.HttpGet() for auth'd request 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\RegExTypoFix.cs | 82 | 24 |

|// TODO:Needs re-write | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\Tagger.cs | 73 | 11 |

|// TODO, better to not apply to text within imagemaps | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\WikiLinks.cs 	| 166 | 16 |

|// TODO: We should offer to try changing the protocol to the response Uri scheme and attempt to load again | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Profiles\\AWBProfilesForm.cs 	| 261 | 20 |

|// TODO: Use IArticleComparer derivatives where possible | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\ReplaceSpecial\\ReplaceSpecial.cs | 29 | 7 |

|// TODO: We should offer to try changing the protocol to the response Uri scheme and attempt to load again | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Session.cs 	| 215 | 20 |

|// TODO: T294397 removed writeapi userright. T202192 to add some version checking (because of MW LTS at least) 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Session.cs 	| 320 | 20 |

|// TODO: assess the impact on servers later | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Session.cs 	| 326 | 20 |

|// TODO: Proper semver version checking 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Session.cs 	| 478 | 16 |

|// TODO: Stop this depending on MessageBox.Show() add an event/delegate and handle in Main.cs 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Session.cs 	| 489 | 16 |

|// TODO:Better error handling | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Session.cs 	| 583 | 20 |

|// articleText = MoveTalkTemplate(articleText, TodoTemplate); 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\TalkPageFixes.cs | 100 | 57 |

|// private static readonly Regex TodoTemplate = Tools.NestedTemplateRegex(new\[] { "To do", "Todo", "To-do" }); 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\TalkPageFixes.cs | 259 | 39 |

|// TODO: should be replaced with SiteInfo.OpenPageInBrowser() wherever possible | C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Tools.cs | 1570 | 12 |

|// TODO: There's gotta be a better way to reconstruct the template... 	| C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Tools.cs | 3271 | 16 |







\### HACK

|Code		|File		|Line	|Column|

|---------------------------------------------------|:------------------------------------------------:|:-------:|----|

| lIPUay9e1ACPS3KOAQj37n5B3VaZ7XF6c4tl7NhZjOzLwCS1G9l5F6C6hHacKbKA559mcMEOoho1+Sq/	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Properties\\Resources.resx	| 1741	| 66 |

| // leading (back)slash is hack for incorrectly formatted breaks per	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\UnitTests\\FormattingTests.cs	| 56	| 39 |

| /// This is a hack required for some multilingual Wikimedia projects,		|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs	| 201	| 23 |

| mAWBLogListener.Skipped = false;  // a bit of a hack, if plugin says not to skip I'm resetting the LogListener.Skipped value to False	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Article\\Article.cs		| 575	| 65 |

| // A hack for the annoying bug with this option being mysteriously enabled to switch		|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Controls\\ArticleTextBox.cs		| 96	| 18 |

| string text = editor.QueryApi(newUrl + "\&rawcontinue=1" + postfix); // HACK: Hacky hack hack	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Lists\\Providers\\ApiListProviderBase.cs	| 81	| 88 |

| // HACK	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\SiteInfo.cs	| 394	| 20 |

| // HACK we are allowing matching on tilde character around parameter name to represent cleaned HTML comment, so may falsely match	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Tools.cs	| 2910	| 1  |

| // HACK:	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Variables.cs		| 674	| 16 |

| //HACK:HACK:HACK:HACK:HACK:	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Variables.cs	| 728	| 15 |





\###WORKAROUND

| Code							| File			| Line	| Column |

|---------------------------------------------------|:--------------------------:|:------:|--------|

|// Workaround for https://phabricator.wikimedia.org/T41492	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs | 1688	| 17	 |

|// workaround for Wine issue: use of {HOME} then +{END} leads to 100% CPU and locked application	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Controls\\Lists\\ListComparer.cs	| 235	| 17	 |

|// workaround for Wine issue: use of {HOME} then +{END} leads to 100% CPU and locked application	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Controls\\Lists\\ListMaker.cs	| 1277	| 13	 |

|// workaround for https://phabricator.wikimedia.org/T4700 -- {{subst:}} doesn't work within ref tags	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\FixSyntax.cs		| 461	| 13	 |

|/// workaround for https://phabricator.wikimedia.org/T4700 -- {{subst:}} doesn't work within ref tags	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\FixSyntax.cs		| 637	| 10	 |

|// Workaround constraint: we might incorrectly report some valid tags with < or > in them as unclosed	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\UnbalancedBrackets.cs	| 507	| 13	 |







\### FIXME

|Code |File 	|Line	|Column

|------|:-------:|:---------:|-------|

|// FIXME: this position is imprefect, since above there is code that can explode, but this way 	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\AWB\\Main.cs 	|1031	| 16 |

|// FIXME: 	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\UnitTests\\RegexAssert.cs |167	| 12 |

|// FIXME: Awful code is awful |C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\API\\ApiEdit.cs 	|1695	| 15 |

|// FIXME: Usages of IgnoreMore with number (or M) replacement done in the FindAndReplace can cause corruption	|C:\\Users\\\[LocalFileName]\\source\\repos\\AutoWikiBrowser\\WikiFunctions\\Parse\\FindandReplace.cs	|218	| 20 |









\## 5. Static Analysis (Roslynator / CodeMaid)



Static analysis tools will be used to establish an objective baseline of the current code quality prior to migration.



The assessment will utilize the following tools:



\### Visual Studio Code Analysis



\*\*Tool\*\*: Visual Studio 2022 Community



\*\*Analysis Date\*\*: 2026-06-27



\*\*Scope\*\*: Entire Solution





\####Initial analysis

| Tool | Result |

|------|--------|

| Visual Studio 2022 Community Code Analysis | Completed |

| Build Status | Builds successfully |

| Blocking Errors | None confirmed |

| Analysis Findings | Missing generated `SvnInfo.cs`, unresolved `m\\\_Revision`, unused variable |



\#### Findings

| ID | Severity | Description | Recommendation | Status |

|----|:--------:|-------------|----------------|--------|

| CA-001 | Medium | `WikiFunctions\\SvnInfo.cs` could not be found during analysis. | Investigate legacy SVN-generated build metadata before migration. | Open |

| CA-002 | Medium | `m\_Revision` does not exist in the current context during analysis. | Determine if this is generated code or a stale reference. | Open |

| CA-003 | Low | `usingDefaultJSONText` is assigned but never used. | Clean up during refactoring phase. | Deferred |



\### Roslynator



Roslynator will be used to identify:



\* Code quality improvements

\* Code style inconsistencies

\* Potential refactoring opportunities

\* Redundant or unnecessary code

\* Performance recommendations

\* Modern C# language improvements where applicable



\### CodeMaid



CodeMaid will be used to assist with:



\* Source code organization

\* Formatting consistency

\* Removal of unused code

\* File and member organization

\* General maintainability improvements



Findings from static analysis will be reviewed throughout the migration process. Recommendations will be evaluated on a case-by-case basis to ensure they align with the project's guiding principles of incremental migration, feature preservation, and evidence-based decision making.



The objective is to improve overall code quality while avoiding unnecessary changes that could increase migration risk.



\### Sample table
| Metric                 | Baseline | Current |       Goal |

| ---------------------- | -------: | ------: | ---------: |

| Roslynator Suggestions |      TBD |     TBD |          ↓ |

| Compiler Warnings      |      TBD |     TBD | 0 Critical |

| TODO Items             |      TBD |     TBD |    Tracked |

| FIXME Items            |      TBD |     TBD |    Tracked |

| HACK Items             |      TBD |     TBD |   Minimize |

| Build Errors           |        0 |       0 |          0 |





\## 6. Deprecated APIs

Legacy APIs and framework features will be reviewed to identify components that may require modification or replacement during migration to .NET 8.



The assessment will include:



\* Obsolete framework APIs

\* Legacy Windows technologies

\* COM / Interop usage

\* Deprecated third-party libraries

\* APIs with recommended modern alternatives



Each deprecated API will be evaluated to determine whether it should be retained, upgraded, replaced, or removed as part of the migration strategy.





\## 7. Code Complexity

This section evaluates the overall complexity of the AutoWikiBrowser codebase and identifies areas that may present challenges during migration and future maintenance.



The assessment will consider:



\* Large or highly coupled classes

\* Long or complex methods

\* High cyclomatic complexity

\* Excessive nesting

\* Duplicate or redundant code

\* Tight project coupling

\* Maintainability indicators identified through static analysis



The objective is to identify components where reducing complexity may improve reliability, readability, testability, and long-term maintainability without unnecessarily increasing migration scope.





\## 8. Technical Debt

Technical debt accumulated throughout the lifetime of the project will be documented and evaluated to determine its impact on the .NET 8 migration and future development.



Technical debt may include:



\* Legacy implementation patterns

\* Obsolete framework features

\* Outdated dependencies

\* Code duplication

\* Architectural limitations

\* Historical workarounds

\* Incomplete or deferred improvements



Each identified item will be evaluated to determine whether it should be:



\* Addressed during the migration

\* Deferred until post-migration modernization

\* Retained due to compatibility or historical considerations



The objective is to make deliberate, evidence-based decisions regarding technical debt rather than attempting to eliminate it indiscriminately during the migration.



\### Partial review of WikiFunctions.Tools

| Area | Finding | Severity | Migration Phase | Status | Recommendation |

|---|---|:---:|:---:|:---:|---|

| Null Handling | Several public methods assume non-null input. | Medium | Cleanup | Open | Add null guards during cleanup/refactoring. |

| Runtime Information | User agent uses `.NET CLR` and `Environment.Version`. | Medium | Migration | Open | Replace with `RuntimeInformation.FrameworkDescription` during the .NET 8 migration. |

| Regex Handling | `RedirectTarget` relies on empty unmatched group behavior. | Low | Cleanup | Open | Add an explicit `m.Success` check for clarity. |

| String Processing | `RemoveInvalidChars` repeatedly reallocates strings. | Low | Cleanup | Open | Consider using `StringBuilder` or filtering characters in a single pass. |

| Enum Logic | `IsWikimediaProject` uses exclusion logic. | Low | Cleanup | Open | Consider an explicit allow-list of supported Wikimedia projects. |

| Networking | `GetHTML` uses legacy `HttpWebRequest` and `HttpWebResponse`. | High | Migration | Open | Migrate to `HttpClient`. |

| Resource Management | Network streams and responses are manually closed. | Medium | Cleanup | Open | Replace manual `Close()` calls with `using` statements. |

| Exception Handling | `FlashWindow` silently swallows all exceptions. | Medium | Cleanup | Open | Catch specific exceptions or document why failures are intentionally ignored. |

| Platform Dependency | `FlashWindow` uses Win32 `user32.dll` P/Invoke. | Medium | Migration | Open | Document as Windows-specific and verify .NET 8 compatibility. |

| Large Inline Regex | `MakeHumanCatKey` contains a very large embedded regex. | Medium | Refactor | Open | Extract to a named `static readonly Regex` with comments and tests. |

| Testability | `GetHTML` performs live network access directly. | Medium | Refactor | Open | Abstract networking to improve testability. |

| Documentation | Several XML documentation comments are empty or minimal. | Low | Cleanup | Open | Improve comments during cleanup phase. |



\###Partial review of Wikiregexes
### WikiRegexes Assessment Note



`WikiRegexes` should be treated as a high-risk compatibility component. It contains many static regular expressions that are rebuilt from global project/language state. Because these regexes affect parsing, categorization, image handling, redirects, dates, templates, disambiguation detection, and language-specific behavior, changes should be minimized during the initial .NET 8 migration.



Recommended approach:

\- Preserve behavior during initial migration.

\- Add regression tests around key regex behavior.

\- Avoid broad cleanup or formatting changes that obscure functional changes.

\- Refactor only after the application builds and runs successfully on .NET 8.



| Area | Finding | Severity | Migration Phase | Status | Recommendation |

|---|---|:---:|:---:|:---:|---|

| Regex Architecture | `WikiRegexes` centralizes a very large number of mutable static regex fields. | High | Refactor | Open | Consider separating namespace regexes, date regexes, template regexes, and language-specific regexes into smaller focused classes. |

| Initialization Order | `MakeLangSpecificRegexes()` rebuilds multiple static regexes based on global `Variables` state. | High | Migration | Open | Document initialization order and verify it is called after language/project metadata is loaded. |

| Global State Coupling | Regex construction depends heavily on `Variables.NamespacesCaseInsensitive`, `Variables.MagicWords`, `Variables.LangCode`, `Variables.URL`, and `Variables.Stub`. | High | Refactor | Open | Reduce direct dependency on global state or isolate regex construction behind a context object. |

| Thread Safety | Static regex fields are reassigned at runtime when language-specific regexes are rebuilt. | Medium | Migration | Open | Confirm whether AWB ever changes language/project context during processing; avoid rebuilding shared static state concurrently. |

| Regex Complexity | Several expressions use nested balancing groups and large alternations. | Medium | Assessment | Open | Preserve behavior initially, but add targeted tests before modifying. |

| Maintainability | Language-specific template logic is handled through a large `switch(Variables.LangCode)`. | Medium | Refactor | Open | Consider moving language-specific definitions to data/config tables later. |

| Duplicate Data | Some template names appear duplicated, such as repeated Russian disambiguation values. | Low | Cleanup | Open | Review and deduplicate language template lists after migration baseline. |

| Performance | Several regexes are created without `RegexOptions.Compiled`. | Low | Assessment | Open | Benchmark before changing; compiled regex is not always automatically better. |

| Naming/Style | `ImagesString` is a local variable using PascalCase. | Low | Cleanup | Open | Rename locals to camelCase during style cleanup if project convention allows. |

| Null/Key Safety | Code indexes namespace dictionaries directly by expected keys. | Medium | Migration | Open | Verify all required namespace keys exist for every supported wiki/project. |

| Legacy Compatibility | Uses `#if !MONO`-era conditional compatibility patterns elsewhere in the related utility layer. | Low | Migration | Open | Review whether Mono-specific branches are still needed after .NET 8 migration. |


### Review of WikiFunctions.Variables
Importance: Very High
Migration Risk: High
Refactor Urgency: High, but defer until after baseline migration
Recommended Action: Preserve behavior first; document initialization flow; test before refactoring

### WikiFunctions Variables

`WikiFunctions.Variables` is one of the most important and highest-risk components reviewed so far. It acts as the central runtime configuration hub for project selection, language settings, namespaces, URLs, proxy behavior, edit-summary text, background loading, and regex regeneration.

The file contains significant legacy design patterns, including broad mutable static state, static initialization side effects, direct UI/session coupling, SVN-based build metadata, legacy `HttpWebRequest` setup, and language-specific behavior embedded in large switch statements.

This component should be treated as high importance and high regression risk. During the initial .NET 8 migration, behavior should be preserved as much as possible. Refactoring should be deferred until project initialization, URL generation, namespace setup, and language-specific behavior have regression test coverage.

| Area | Finding | Severity | Migration Phase | Status | Recommendation |
|---|---|:---:|:---:|:---:|---|
| Global State | `Variables` acts as a central mutable static state container for project, language, URL, namespace, proxy, summary, and runtime behavior. | High | Refactor | Open | Preserve initially, but later consider replacing with a project/session context object. |
| Initialization Order | Static constructor performs substantial setup and conditionally calls `SetProject()` or test setup logic. | High | Migration | Open | Document startup initialization order before migration. |
| Generated Build Metadata | `Revision` and `RevisionNumber` depend on `m_Revision`, which appears tied to legacy `SvnInfo.cs` generation. | High | Migration | Open | Replace SVN-based revision metadata with Git/version metadata or a generated build-info file. |
| Legacy Networking | `PrepareWebRequest()` creates `HttpWebRequest` instances and uses `ServicePointManager`. | High | Migration | Open | Plan eventual migration to `HttpClient` and modern handler configuration. |
| Security/Protocol Handling | TLS protocols are explicitly modified through `ServicePointManager.SecurityProtocol`. | Medium | Migration | Open | Review whether explicit TLS settings are still needed under .NET 8. |
| Threading | Background request management uses shared static lists, manual locks, and `Thread.Sleep`. | Medium | Refactor | Open | Consider task-based async patterns after initial migration. |
| UI Coupling | `Variables.MainForm` gives the core library direct access to the AWB UI/session. | High | Refactor | Open | Decouple core project settings from the main form/session where possible. |
| Language Configuration | Large switch statement stores language-specific behavior directly in code. | Medium | Refactor | Open | Consider moving language/project configuration to data-driven structures later. |
| Project Configuration | `SetProject()` performs many unrelated tasks: URL setup, proxy refresh, session update, regex regeneration, typo reload, namespace validation. | High | Refactor | Open | Split into smaller responsibilities after behavior is covered by tests. |
| Fandom/Wikia Handling | Fandom and Wikia URL construction is hardcoded. | Medium | Migration | Open | Verify current Fandom URL behavior and API compatibility during migration. |
| Test Coupling | Unit test behavior is controlled through global `Globals.UnitTestMode`. | Medium | Refactor | Open | Replace with explicit testable configuration where practical. |
| Documentation | Several XML comments are empty or minimal. | Low | Cleanup | Open | Improve comments during cleanup phase. |


| Area | Finding | Severity | Migration Phase | Status | Recommendation |
|------|----------|:-------:|:---------------:|:------:|----------------|
| Build System | Legacy SVN-based revision metadata generation (`SvnInfo.cs`). | Medium | Migration | Open | Replace or modernize build metadata generation after migration baseline is established. |


\## 9. Testing Coverage

Testing coverage will be evaluated to determine the current level of automated validation available to support the migration.



The assessment will include:



\* Existing unit test projects

\* Areas currently covered by automated tests

\* Critical workflows requiring manual validation

\* Opportunities to improve automated testing during and after migration



The objective is to identify gaps in test coverage that may increase migration risk and to prioritize areas where additional validation would provide the greatest benefit.





\## 10. Initial Findings

This section summarizes significant observations identified during the code health assessment.



Initial findings will be updated as additional analysis is completed and may include:



\* High-priority technical debt

\* Legacy implementation patterns

\* Deprecated APIs

\* Static analysis results

\* Areas requiring refactoring

\* Opportunities for performance or maintainability improvements



Findings documented in this section will serve as input for migration planning and future modernization efforts.



\### WikiFunctions.Tools



`WikiFunctions.Tools` appears to be a mature legacy utility class that contains core helper logic for title handling, redirects, namespace processing, human category sort keys, HTML retrieval, JSON parsing, window notification behavior, and string comparison.



The code is generally stable and test-aware, with several methods already covered by unit tests. However, it also contains common legacy patterns that should be reviewed during the .NET 8 migration, including limited null handling, direct network access through `HttpWebRequest`, manual resource cleanup, static/global utility design, Windows-specific P/Invoke usage, and some large embedded regular expressions.



Overall, `Tools` should be treated as a medium-risk support component. It does not appear to be an immediate migration blocker, but it contains several modernization candidates that should be addressed after the baseline migration is stable.



\###WikiRegexes

`WikiRegexes` serves as the central repository for MediaWiki parsing expressions and language-specific template detection. The implementation is functionally mature and supports numerous localized wiki configurations. However, it relies heavily on mutable static state, global configuration objects, and complex regular expressions. Due to its central role in article parsing, it should be considered a high-regression-risk component. During the initial .NET 8 migration, behavior should be preserved, with refactoring deferred until comprehensive regression testing is available.

###Build baseline
| Property | Result |
|----------|--------|
| Build Date | 2026-06-27 |
| Configuration | Debug |
| Platform | AnyCPU |
| Projects Built | 4 |
| Build Result | Success |
| Projects Succeeded | 4 |
| Projects Failed | 0 |
| Projects Skipped | 0 |
| Overall Assessment | Solution builds successfully under .NET Framework 4.8.1. |


### Build health
####################################
# 2. Build Health
####################################

### Baseline Build

| Property | Result |
|-----------|--------|
| Build Status | Success |
| Projects Built | 4 |
| Projects Failed | 0 |
| Target Framework | .NET Framework 4.8.1 |

### Build Observations

- Solution rebuild completed successfully.
- `SvnInfo.cs` is regenerated or updated during the build process.
- Revision metadata appears to be provided through the legacy `m_Revision` mechanism.
- Static analysis references this generated source file.
- Build generation mechanism should be evaluated during the .NET 8 migration.

\## 11. Recommendations

Recommendations will be developed from the findings documented throughout this assessment.



Recommendations may include:



\* Refactoring opportunities

\* Technical debt reduction

\* Modernization priorities

\* Testing improvements

\* Performance optimizations

\* Code quality improvements

\* Migration sequencing recommendations



Recommendations should be based on verified findings and aligned with the project's guiding principles of incremental migration, feature preservation, and evidence-based decision making.

