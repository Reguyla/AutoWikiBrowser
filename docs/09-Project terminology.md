\# 9. Project Terminology



\## 9.1. Purpose



This document defines project-specific terminology, abbreviations, and naming conventions used throughout the AutoWikiBrowser modernization project. It serves as both a glossary and a shared vocabulary for contributors.



\---



\## 9.2.  Application Terms



\### AutoWikiBrowser Terms



| Term | Definition |

|------|------------|

| Action | A processing operation performed by AutoWikiBrowser on a page during an editing session. |

| Article List | The collection of pages queued for processing by AutoWikiBrowser. |

| AutoFix | An automated correction performed by AutoWikiBrowser. |

| AWB | Abbreviation for AutoWikiBrowser. |

| CheckPage | A wiki page containing configuration or operational settings read by AWB. |

| Custom Module | A user-developed C# extension that integrates with AWB's processing pipeline. |

| Edit Box | The primary editing window where article text is displayed and modified before saving. |

| Existing Page Cache | An in-memory cache used to avoid repeatedly querying whether linked pages exist. |

| Find and Replace Rule | A configurable rule used to automatically replace one text pattern with another. |

| Ignore Rule | A rule that prevents specified text or links from being modified during processing. |

| Module Builder | The AWB utility used to create and compile custom modules. |

| Parse Rule | A rule used to analyze or transform article text before saving. |

| Plugin | A compiled extension that adds functionality to AutoWikiBrowser. |

| Plugin Host | The portion of AWB responsible for discovering, loading, and executing plugins. |

| Processing Pipeline | The ordered sequence of operations applied to an article before it is saved. |

| Regex Tester | A utility included with AWB for testing and validating regular expressions. |

| Replace Rule | A rule that substitutes matched text with a predefined replacement. |

| Rule Page | A wiki page containing user-maintained rules, regular expressions, or configuration data consumed by AWB. |

| Rule Set | A logical collection of related processing rules loaded by a module or plugin. |

| Session Cache | Temporary in-memory data retained only for the duration of the current AWB session. |

| Skip | A decision to leave the current page unchanged and continue to the next page. |

| Skip Reason | The recorded explanation for why a page was skipped during processing. |

| Typo Rule | A regular expression rule used to detect and correct common typographical errors. |

| Variables | The central configuration class that stores shared application settings and state information. |

| WikiFunctions | The shared core library providing much of AWB's common functionality. |

| WikiRegexes | A shared collection of regular expressions used throughout AutoWikiBrowser. |---



\### MediaWiki Terms



| Term | Definition |

|------|------------|

| Article | A standard content page within a wiki. |

| Category | A page used to organize related articles into groups. |

| Diff | A comparison showing the differences between two revisions of a page. |

| Edit Summary | A brief description entered by the editor explaining the purpose of an edit. |

| Fandom | A commercial wiki platform based on MediaWiki. |

| Interlanguage Link | A link connecting equivalent pages in different language editions of a wiki. |

| Interwiki | A link between Wikimedia projects or other configured MediaWiki sites. |

| Magic Word | A special MediaWiki keyword that controls page behavior or outputs system information. |

| MediaWiki | The open-source wiki software that powers Wikipedia and many other wiki sites. |

| Module | A Lua script stored in the Module namespace and executed through the Scribunto extension. |

| Namespace | A logical grouping of related pages within MediaWiki, such as Article, Template, or Category. |

| Parser Function | A built-in MediaWiki parser function used to perform logic or formatting within wikitext. |

| Redirect | A page that automatically forwards readers to another page. |

| Red Link | A wikilink pointing to a page that does not currently exist. |

| Revision | A saved version of a wiki page. |

| Scribunto | The MediaWiki extension that provides Lua scripting support. |

| Section | A subdivision of a wiki page created using heading markup. |

| Stub | An article that is considered incomplete and requires expansion. |

| Talk Page | A discussion page associated with an article or other wiki page. |

| Template | A reusable page that can be transcluded into other pages. |

| Transclusion | The process of including the contents of one page within another page. |

| User Page | A personal page associated with a registered user account. |

| Watchlist | A personalized list of pages monitored for changes. |

| Wikimedia | The organization that operates Wikipedia and related projects. |

| Wikidata | Wikimedia's structured knowledge database used by many MediaWiki projects. |

| Wikimedia Commons | The shared media repository used across Wikimedia projects. |

| Wikitext | The markup language used to create and format MediaWiki pages. |

| WikiProject | A collaborative group focused on improving articles within a particular subject area. |

| Wiki Table | A table created using MediaWiki wikitext syntax rather than HTML. |

| Wikilink | An internal link to another page within the same wiki. |

\---



\### Plugin Terminology



| Term             | Definition |

|------------------|------------|

| BingSearch       | Optional plugin that assists with searching for external information using the Bing search engine. |

| CFD              | Optional plugin related to deletion discussion workflows. Exact functionality to be verified during plugin evaluation. |

| CheckPage Plugin | A plugin that retrieves or processes data from one or more AWB CheckPages. |

| Delinker         | Plugin that removes unnecessary wikilinks from articles. |

| Fronds           | Optional plugin providing additional editing functionality. Exact purpose to be verified during plugin evaluation. |

| IFD              | Optional plugin related to image deletion workflows. Exact functionality to be verified during plugin evaluation. |

| KingbotK Plugin  | Optional plugin that provides automated assistance for selected maintenance tasks originally developed for KingbotK workflows. |

| NoLimitsPlugin   | Optional plugin that extends or relaxes certain AutoWikiBrowser operating limits. Exact behavior to be verified during plugin evaluation. |

| Plugin           | Optional extension loaded by AutoWikiBrowser to provide additional functionality. |

| TheTemplator     | Plugin that assists with inserting and managing templates within articles. |

| TypoScan         | Plugin that detects common spelling mistakes and typographical issues. |

\---



\### Development Terms



| Term | Definition |

|------|------------|

| Build Stabilization | The process of restoring successful compilation after migration changes. |

| Code Health | An overall assessment of the maintainability, quality, and technical condition of the source code. |

| Code Complexity | A measure of how difficult code is to understand, maintain, or modify. |

| Compiler Warning | A compiler-generated message indicating a potential issue that does not prevent compilation. |

| Continuous Integration (CI) | An automated process that builds and validates code changes whenever they are committed. |

| Dependency Audit | The process of identifying and evaluating all internal and external project dependencies. |

| Discovery Phase | The initial planning and assessment stage completed before modifying production code. |

| Feature Parity | Existing functionality preserved after migration. |

| Framework Dependency | A dependency provided by the .NET Framework or .NET runtime rather than a third-party library. |

| Incremental Migration | A migration strategy that performs small, verifiable changes rather than a complete rewrite. |

| Legacy Code | Existing source code written for earlier technologies that requires modernization or continued maintenance. |

| Migration | The process of moving the application from .NET Framework to .NET 8 while preserving functionality. |

| Modernization | Improvements made after feature parity has been achieved to improve architecture, usability, or maintainability. |

| NuGet Package | A reusable software package distributed through the NuGet package manager. |

| Plugin | An optional extension that adds functionality without modifying the core application. |

| Project Baseline | The recorded state of the project at the beginning of the modernization effort. |

| Regression Testing | Testing performed to verify that existing functionality continues to work after changes are made. |

| Risk Mitigation | Actions taken to reduce the likelihood or impact of identified project risks. |

| Rollback | Reverting one or more changes to restore a previously known working state. |

| SDK-style Project | The modern MSBuild project format introduced with .NET Core and continued in .NET 5+. |

| Source Control | A system used to track changes to source code and project documentation over time. |

| Static Analysis | Automated inspection of source code to identify potential defects, code smells, or maintainability issues without executing the program. |

| Technical Debt | Design or implementation compromises that require future improvement to maintain long-term code quality. |

| Validation | The process of confirming that a migration step has achieved its intended outcome without introducing regressions. |



\## 9.3. Related Documents

Prerequisites

\-------------

00 – Foundation, Discovery \& Planning

01 - Migration Strategy

02 - Development Environment



Supporting

\----------

03 - Solution inventory

04 - Dependency audit

05 – Code Health Assessment





Operational

\-----------

06 – Change Control Log

07 – Lessons Learned

08 - Migration-Assessment



