# 6. - Change Control Log
Date: 2026-06-26

## 6.1. Project Foundation

- Created GitHub fork.
- Established development branch.
- Configured Visual Studio 2022.
- Installed Git for Windows.
- Installed Roslynator
- Installed Codemaid
- Installed .NET Framework 4.8.1 Developer Pack.
- Added initial project documentation.
- Established Git workflow.
- Created first project commit.

## 6.2. Decisions

- Bundled plugins are outside the initial modernization scope.
- Preserve the plugin architecture.
- Focus on modernizing the core application first.

## 6.3. Purpose
This document records all significant decisions, changes, and milestones during the modernization of AutoWikiBrowser to .NET 8 (TWAIN).

Routine Git commits should be used for source-level history. This document captures architectural decisions, planning changes, and major implementation milestones that affect the project as a whole.

## 6.4. Status
| Property        | Value      |
| --------------- | ---------- |
| Document Status | Active     |
| Project Phase   | Planning   |
| Started         | 2026-06-27 |
| Last Updated    | 2026-06-27 |

## 6.5. Decision categories
| Code  | Meaning         |
| ----- | --------------- |
| DOC   | Documentation   |
| ARCH  | Architecture    |
| MIG   | Migration       |
| DEP   | Dependency      |
| UI    | User Interface  |
| PERF  | Performance     |
| TEST  | Testing         |
| BUILD | Build System    |
| BUG   | Defect Fix      |
| BREAK | Breaking Change |
| SEC   | Security        |


## 6.6. Change log 
| Date       | ID     | Category | Description                             | Decision | Git Commit |
| ---------- | ------ | -------- | --------------------------------------- | -------- | ---------- |
| 2026-06-27 | CC-001 | DOC      | Created project documentation framework | Approved | Commit 9   |
| 2026-06-27 | CC-002 | CODE	 | Added `Tools.IsValidWebUrl()` helper. Provides safer URL validation than the legacy regex and allows callers to migrate gradually. | Approved | Commit |

## 6.7. New methods added to Wikifunctions.Tools
### 6.7.1. New method added to Wikifunctions.Tools for now
        /// <summary>
        /// <param name="url">The URL to validate.</param>
        /// </summary>
        /// <c>true</c> if the URL is a valid absolute HTTP, HTTPS, or FTP URL; otherwise, <c>false</c>.
        /// <returns></returns>
        public static bool IsValidWebUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            Uri uri;

            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps
                || uri.Scheme == Uri.UriSchemeFtp;
        }

### 6.7.2. New method that handles temporary HTTP/API failures that may succeed if retried.
        /// <summary>
        /// Handles temporary HTTP/API failures that may succeed if retried.
        /// </summary>
        /// <param name="webex">The web exception thrown by the request.</param>
        /// <returns>
        /// true if the exception was handled and the caller should retry;
        /// false if the exception should be rethrown.
        /// </returns>
        public static bool HandleHttpException(System.Net.WebException webex)
        {
            if (webex == null)
                return false;

            System.Net.HttpWebResponse response = webex.Response as System.Net.HttpWebResponse;

            if (response == null)
                return false;

            System.Net.HttpStatusCode statusCode = response.StatusCode;

            if (statusCode == System.Net.HttpStatusCode.RequestTimeout ||
                statusCode == System.Net.HttpStatusCode.BadGateway ||
                statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                statusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                (int)statusCode == 429)
            {
                System.Threading.Thread.Sleep(5000);
                return true;
            }

            return false;
        }

### 6.7.3. New helper that gets the appropriate CookieContainer for the specified URL and session.
        /// <summary>
        /// Helper that gets the appropriate CookieContainer for the specified URL and session.
        /// </summary>
        public static CookieContainer GetCookieContainer(string url, IAutoWikiBrowser awb)
        {
            if (awb == null)
                return new CookieContainer();

            Session session = awb.TheSession;

            if (session == null)
                return new CookieContainer();

            ApiEdit editor = session.Editor != null
                ? session.Editor.SynchronousEditor
                : null;

            if (editor != null &&
                !string.IsNullOrEmpty(editor.URL) &&
                url.StartsWith(editor.URL))
            {
                return editor.Cookies ?? new CookieContainer();
            }

            return new CookieContainer();
        }





## 6.8. Major Architecture Decisions
### ARCH-001
Date: 2026-07-03

### 6.8.1. Decision
Plugins will not be considered part of the initial .NET 8 migration.

### 6.8.2. Reason
The majority of plugins are unused and increase migration complexity.

### 6.8.3. Impact
- Simplifies migration
- Smaller testing surface
- Plugins may be migrated individually later

### 6.8.4 Status: Approved

## 6.9. Migration Milestones

| Milestone                 | Date | Status |
| ------------------------- | ---- | ------ |
| Planning Complete         |      | ☐      |
| Dependency Audit Complete |      | ☐      |
| Build Clean               |      | ☐      |
| First .NET 8 Compile      |      | ☐      |
| First Successful Launch   |      | ☐      |
| First Successful Edit     |      | ☐      |
| Feature Complete          |      | ☐      |
| Release Candidate         |      | ☐      |

## 6.10. Deferred Decisions

| ID     | Topic            | Reason Deferred                   | Review Phase      |
| ------ | ---------------- | --------------------------------- | ----------------- |
| DD-001 | Plugin migration | Not required for MVP              | TBD	            |
| DD-002 | Avalonia UI      | Evaluate after WinForms migration | GUI Modernization |

## 6.11. Lessons Learned
2026-07-15

The project contained numerous hard-coded paths.

Recommendation:
Centralize configuration before further modernization.

## 6.12. Related Documents
Prerequisites
-------------
00 – Foundation, Discovery & Planning
01 - Migration Strategy
02 – Development Environment

Supporting
----------
03 – Solution Inventory
04 – Dependency Audit
05 – Code Health Assessment

Operational
-----------
07 – Lessons Learned
08 - Migration-Assessment
09 - Project Terminology




