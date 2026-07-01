\##08-Migration-Assessment



\##1. Executive Summary



\##2. Upgrade Assistant Results

\##2.1. AWB Core migration analysis without plugins



\##2.2. AWB Full migration analysis with plugins

| Issue        | Severity  | Incidents | Notes                                          |

| ------------ | --------- | --------: | ---------------------------------------------- |

| Api.0001     | Mandatory |    56,751 | Missing APIs requiring replacement or redesign |

| Api.0002     | Potential |     1,039 | APIs available via NuGet packages              |

| Api.0003     | Optional  |        30 | Obsolete APIs to modernize                     |

| NuGet.0002   | Potential |         3 | Upgrade recommended                            |

| NuGet.0003   | Mandatory |         5 | Package functionality now in framework         |

| Project.0001 | Mandatory |        13 | Convert to SDK-style projects                  |

| Project.0002 | Mandatory |        13 | Update target framework                        |



\##3. Migration Categories

&#x20;  ###3.1 Project Conversion

&#x20;  ###3.2 Target Framework

&#x20;  ###3.3 NuGet Packages

&#x20;  ###3.4 Missing APIs

&#x20;  ###3.5 Obsolete APIs



\##4. Migration Priority Matrix



\##5. Migration Backlog



\##6. Testing Strategy



\##7. Risks



\##8. Decisions



\##9. Legacy Networking Inventory



A solution-wide search identified legacy networking APIs concentrated primarily in

`WikiFunctions`.



| API / Pattern | AWB | AWBUpdater | WikiFunctions | Notes |

|---|---:|---:|---:|---|

| WebRequest | 0 | 2 | 18 | Counts may overlap with HttpWebRequest results. |

| HttpWebRequest | 0 | 1 | 13 | Primary legacy request type. |

| HttpWebResponse | 2 | 1 | 8 | Response handling is mostly centralized in WikiFunctions. |

| WebClient | 0 | 2 | 0 | Appears limited to updater download behavior. |

| ServicePoint | 0 | 1 | 7 | Legacy request configuration. |

| ServicePointManager | 0 | 1 | 5 | Process-wide networking configuration. |

| GetResponse() | 0 | 1 | 3 | High-priority execution paths for conversion. |

| GetRequestStream() | 0 | 0 | 2 | Likely POST/upload request paths. |

| Abort() | 6 | 0 | 17 | Requires type-level review; not every hit is necessarily HTTP cancellation. |

| DownloadFile() | 0 | 1 | 0 | Dedicated updater download path. |



\### Initial Assessment



Legacy networking is concentrated enough to support a staged migration:

first modernize shared WikiFunctions request flows, then convert dependent

AutoWikiBrowser callers, and handle AWBUpdater downloads as a separate work item.



\##10. Networking Concentration Assessment



Detailed review found that, excluding `Abort()` usages requiring separate

type-level classification, legacy networking APIs are concentrated in the

following files:



\- AWB.Main.cs

\- AWBUpdater.Updater.cs

\- WikiFunctions.API.ApiEdit.cs

\- WikiFunctions.Parse.Parsers.cs

\- WikiFunctions.Sessions.cs

\- WikiFunctions.Tools.cs

\- WikiFunctions.Variables.cs



This indicates that legacy networking can be modernized through a small number

of coordinated request workflows rather than a broad file-by-file solution-wide

rewrite.



\### The work should be divided into:



\#### Main AWB wiki/API networking, centered on WikiFunctions and its AWB callers.

\#### AWBUpdater download/update networking, handled as an independent workflow.

\#### Separate review of `Abort()` usages before assigning them to HTTP

&#x20;  cancellation migration work.



1. Variables.cs - Define shared HTTP configuration: proxy, cookies, user agent, decompression, timeout, certificate policy.

2\. Tools.cs - Replace general request/response helpers with reusable HttpClient-based helpers.

3\. Sessions.cs - Convert login, authenticated requests, cookie/session behavior, and true HTTP cancellation.

4\. API.ApiEdit.cs - Convert edit/save POST behavior after the session/authenticated client works.

5\. Parsers.cs - Convert any remaining fetch/parse workflows.

6\. AWB.Main.cs - Convert UI callers and ensure cancellation, error display, and progress behavior remain correct.

7\. AWBUpdater.Updater.cs - Convert separately after normal wiki operations are stable.



\## 11. Related Documents

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

09 - Project Terminology

