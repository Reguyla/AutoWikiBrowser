\# 00.  Foundation, Discovery \& Planning

Objective



Establish the technical, organizational, and documentation foundation required to support a successful migration.



Activities



Repository organization

Documentation

Development environment

Solution inventory

Dependency audit

Code health assessment

Migration planning

GitHub workflows/templates

Initial backlog



Deliverables



Complete planning documentation

Defined migration scope

Risk assessment

Migration strategy



Definition of Done



Ready to begin technical migration work.





\## Phase 1 – Core Migration

Objective



Successfully migrate the core application and essential supporting projects to .NET 8 while preserving existing functionality.



Activities



SDK-style conversion

Target framework upgrade

Dependency updates

Build fixes

Compiler errors

Initial validation



Projects



AutoWikiBrowser

WikiFunctions

UnitTests

AWBUpdater (if retained)



Definition of Done



Core application builds and runs successfully under .NET 8.





\## Phase 2 – Project Modernization

Objective



Complete migration of remaining in-scope projects and eliminate legacy build infrastructure.



Activities



Utilities

Shared tooling

Remaining project conversions

Remove obsolete project formats

Modernize build process

CI updates



Definition of Done



All in-scope projects successfully build using the modern toolchain.





\## Phase 3 – Stabilization

Objective



Ensure the migrated application is reliable, maintainable, and functionally equivalent to the original implementation.



Activities



Regression testing

Bug fixes

Performance tuning

Static analysis

Technical debt reduction

Documentation updates



Definition of Done



Feature parity achieved with no known critical regressions.





\## Phase 4 – Modernization

Phase 4 – Modernization



Objective



Improve the application beyond feature parity while preserving its core mission.



Activities



UI improvements

UX improvements

Architecture cleanup

Better logging

Better diagnostics

Better tooling

Regex workbench

Analytics



Definition of Done



Modern architecture established for future development.







\## Phase 5 – Plugin Evaluation

Phase 5 – Plugin Evaluation



Objective



Evaluate each bundled plugin individually and determine its long-term future.



Activities



Plugin inventory

Usage analysis

Compatibility assessment

Migrate

Replace

Retire



Definition of Done



Every plugin has a documented disposition.







\## Phase 6 – Application Evolution

Phase 6 – Application Evolution



Objective



Continue evolving the application beyond the initial migration by introducing new capabilities and long-term architectural improvements.



Activities



Major new features

Additional tooling

Long-term architecture

Branding (if desired)

New workflows

Future plugin ecosystem

Community contributions



Definition of Done



The application has transitioned from a migrated legacy application into an actively evolving modern project.

