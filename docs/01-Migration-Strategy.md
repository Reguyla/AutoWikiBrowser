# 01. Migration Strategy

## 1.1. Executive Summary

This document defines the overall strategy for migrating AutoWikiBrowser from the .NET Framework to .NET 8 while preserving existing functionality and minimizing migration risk. It establishes the guiding principles, migration phases, validation approach, and decision-making framework that will be followed throughout the project.

The migration will be performed incrementally, beginning with analysis and planning, followed by dependency assessment, project modernization, feature parity validation, and post-migration enhancements. Optional components, including bundled plugins, will be evaluated separately after the core application has been successfully migrated and stabilized.

This strategy is intended to provide a consistent roadmap for the modernization effort while ensuring that architectural decisions remain well documented, repeatable, and aligned with the long-term objectives of the project.

## 1.2. Migration Objectives
The primary objectives of the migration are:

- Migrate the core AutoWikiBrowser application to .NET 8
Successfully migrate the core application, shared libraries, and supporting projects while maintaining a stable build.

- Maintain feature parity
Preserve existing functionality and user workflows throughout the migration. Functional improvements should generally be deferred until after migration unless required for compatibility.

- Minimize migration risk
Perform the migration incrementally using small, well-defined changes supported by documentation, testing, and source control.

- Reduce technical debt
Identify obsolete APIs, legacy dependencies, and maintainability issues encountered during migration and address them where practical without unnecessarily expanding project scope.

- Preserve extensibility
Maintain the existing plugin architecture while deferring migration of optional bundled plugins until after the core application has been successfully modernized.

- Improve maintainability
Modernize the solution structure, build system, dependency management, and documentation to establish a solid foundation for future development.

- Establish a repeatable migration process
Document decisions, dependencies, risks, and lessons learned throughout the project to support future maintenance and modernization efforts.

## 1.3. Guiding Principles

- Analysis before implementation — Understand the existing system before modifying it.
- Defer optional plugins until after the core migration.
- Evidence-based decisions — Base migration decisions on documented findings rather than assumptions.
- Incremental migration — Make one logical, verifiable change at a time.
- Preserve existing functionality during migration.
- Validate changes continuously.


## 1.4. Migration Phases
The migration will be completed in a series of incremental phases.


| Phase   | Description                                        | Status      |
|---------|----------------------------------------------------|-------------|
| Phase 0 | Foundation, Discovery & Planning       | In Progress |
| Phase 1 | Core Migration       | In Progress |
| Phase 2 | Project Modernization | Planned     |
| Phase 3 | Stabilization       | Planned     |
| Phase 4 | Modernization           | Planned     |
| Phase 5 | Plugin Evaluation       | Planned     |
| Phase 6 | Application Evolution                  | Planned     |


## 1.5. Migration Order
The migration will proceed in the following order:

1. Repository preparation
2. Solution inventory
3. Dependency audit
4. Code health assessment
5. SDK-style project conversion
6. .NET 8 migration
7. Build stabilization
8. Functional validation
9. Performance optimization
10. Post-migration enhancements

The order may be refined as additional dependencies and migration risks are identified.

## 1.6. Risk Mitigation
The following practices will be used to reduce migration risk throughout the project:

- Perform a complete assessment of the existing solution before modifying production code.
- Migrate the solution incrementally using small, well-defined commits.
- Maintain detailed documentation of architecture, dependencies, and migration decisions.
- Preserve existing functionality and user workflows during the migration.
- Investigate high-risk dependencies before attempting replacement or modernization.
- Defer optional plugins and non-essential utilities until the core application has been successfully migrated.
- Continuously validate build stability after each logical migration step.
- Maintain the ability to revert individual migration steps through source control.

## 1.7. Validation Strategy

Each migration phase will be validated before proceeding to the next stage.

Validation activities include:
- Successful solution build with no unexpected errors.
- Verification that existing functionality continues to operate as expected.
- Execution of available unit tests.
- Manual testing of core application workflows.
- Review of compiler warnings and static analysis findings.
- Verification that documentation reflects the current state of the project.
- Confirmation that migration objectives for the current phase have been satisfied before beginning the next phase.


## 1.8. Rollback Strategy

The migration will be managed through incremental source control commits to ensure that individual changes can be reverted if necessary.

Rollback procedures include:

- Limit each commit to a single logical change whenever practical.
- Validate each completed migration step before continuing.
- Use Git history to identify and revert isolated migration changes when required.
- Avoid combining unrelated modifications into a single commit.
- Preserve the original .NET Framework implementation until the .NET 8 migration has been validated.
- Document significant architectural decisions to simplify troubleshooting and recovery.


## 1.9. Success Criteria
The migration will be considered successful when the following objectives have been achieved:

- All Phase 1 projects build successfully under .NET 8.
- Core application functionality is preserved.
- Existing workflows have been validated.
- Critical migration risks have been resolved or mitigated.
- Project documentation accurately reflects the migrated solution.
- Technical debt identified during migration has been documented.
- The solution is ready for continued modernization and future development.
- Documentation accurately reflects the final migrated architecture.


## 1.10. Post-Migration Modernization
Following completion of the .NET 8 migration, modernization efforts may include:

- User interface improvements
- Architecture and code quality improvements
- Performance optimization
- Enhanced testing and automation
- Plugin evaluation and modernization
- Utility review and modernization
- Dependency updates
- Documentation improvements
- Long-term product evolution and rebranding


## 1.11. Future Enhancements

- Default to no assumed wiki.
- Let the user select a wiki/project during setup.
- Allow custom MediaWiki URL entry.
- Auto-detect site info from the wiki API.
- Confirm detected settings with the user.
- Store that profile for future sessions.


### 1. Wiki Profile Selection and Detection
The rebuilt application should not assume that the user is editing English Wikipedia. During modernization, TWAIN should support explicit wiki profile selection and custom MediaWiki site configuration.

Wiki profiles should be portable, allowing users to export, import, and share configurations between installations.

Planned behavior:

| Capability | Description |
|------------|-------------|
| Wiki selection | User can select Wikipedia, Commons, Wikidata, Fandom, or a custom MediaWiki site. |
| Auto-detection | Application can query the MediaWiki API to detect site name, language, namespaces, API endpoint, and capabilities. |
| Confirmation | Detected settings should be shown to the user before being saved. |
| Profiles | Users can save multiple wiki profiles and switch between them. |
| Rule behavior | Wiki-specific features, such as `MultipleIssues`, should be enabled only when supported by the selected profile. |


## 1.22. Related Documents
Prerequisites
-------------
00 – Foundation, Discovery & Planning
02 – Development Environment

Supporting
----------
03 – Solution Inventory
04 – Dependency Audit
05 – Code Health Assessment

Operational
-----------
06 – Change Control Log
07 – Lessons Learned