## 2026-06-26

### Project Foundation

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

---

### Decisions

- Bundled plugins are outside the initial modernization scope.
- Preserve the plugin architecture.
- Focus on modernizing the core application first.

## 1. Purpose
This document records all significant decisions, changes, and milestones during the modernization of AutoWikiBrowser to .NET 8 (TWAIN).

Routine Git commits should be used for source-level history. This document captures architectural decisions, planning changes, and major implementation milestones that affect the project as a whole.

## 2. Status
| Property        | Value      |
| --------------- | ---------- |
| Document Status | Active     |
| Project Phase   | Planning   |
| Started         | 2026-06-27 |
| Last Updated    | 2026-06-27 |

## 3. Decision categories
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


## 4. Change log 
| Date       | ID     | Category | Description                             | Decision | Git Commit |
| ---------- | ------ | -------- | --------------------------------------- | -------- | ---------- |
| 2026-06-27 | CC-001 | DOC      | Created project documentation framework | Approved | Commit 9   |
| 2026-06-27 | CC-002 | CODE	 | Added `Tools.IsValidWebUrl()` helper. Provides safer URL validation than the legacy regex and allows callers to migrate gradually. | Approved | Commit |

## 5. Major Architecture Decisions
## ARCH-001

Date: 2026-07-03

Decision

Plugins will not be considered part of the initial .NET 8 migration.

Reason

The majority of plugins are unused and increase migration complexity.

Impact

- Simplifies migration
- Smaller testing surface
- Plugins may be migrated individually later

Status: Approved

## 6. Migration Milestones

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

## 7. Deferred Decisions

| ID     | Topic            | Reason Deferred                   | Review Phase      |
| ------ | ---------------- | --------------------------------- | ----------------- |
| DD-001 | Plugin migration | Not required for MVP              | TBD	            |
| DD-002 | Avalonia UI      | Evaluate after WinForms migration | GUI Modernization |

## 8. Lessons Learned
2026-07-15

The project contained numerous hard-coded paths.

Recommendation:
Centralize configuration before further modernization.
