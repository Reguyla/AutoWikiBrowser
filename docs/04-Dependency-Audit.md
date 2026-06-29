# 03. Dependency audit

## 3.1. Executive Summary

This document inventories and evaluates the external and internal dependencies required to build and operate the AutoWikiBrowser solution. Its purpose is to identify components that may affect the migration from .NET Framework to .NET 8 and to document the strategy for addressing each dependency.

The audit includes:
- Project-to-project references
- Framework assemblies
- Third-party libraries
- COM/Interop components
- NuGet packages
- External build dependencies

Each dependency is classified according to its current status, migration risk, and recommended modernization strategy.

This document will be updated throughout the migration as dependencies are verified, upgraded, replaced, or retired.


## 3.2. Dependencies to look for and analyze

| Dependency Type                      | Where to Look                                        | Why It Matters                                        |
| ------------------------------------ | ---------------------------------------------------- | ----------------------------------------------------- |
| **Project references**               | `.csproj`, References node                           | Determines migration order                            |
| **Framework assemblies**             | `System.*`, `Microsoft.*` references                 | Some .NET Framework assemblies do not exist in .NET 8 |
| **Direct DLL references**            | `References`, `HintPath`, `lib/`, `bin/`             | May need NuGet replacements or source updates         |
| **COM references**                   | References node, `.csproj`                           | Often migration blockers                              |
| **WinForms / designer dependencies** | Forms, `.Designer.cs`, `.resx`                       | Needs .NET 8 WinForms compatibility review            |
| **App.config / settings**            | `App.config`, `.settings`, config sections           | Old configuration APIs may need changes               |
| **Resource files**                   | `.resx`, images, icons, embedded files               | Can break during SDK-style conversion                 |
| **Build tools / targets**            | `.targets`, `.props`, pre/post-build events          | Legacy build steps may fail in SDK-style projects     |
| **Test framework references**        | NUnit, test adapters, mocks                          | Tests may need package upgrades                       |
| **Installer/updater dependencies**   | setup projects, ClickOnce, AWBUpdater                | Deployment model may need rework                      |
| **External executables/tools**       | Anything launched via `Process.Start`                | Need path/platform assumptions checked                |
| **Native libraries**                 | `.dll`, `.ocx`, x86/x64-specific files               | Can block AnyCPU/.NET 8 migration                     |
| **Web/API dependencies**             | Wiki API clients, `WebClient`, `HttpWebRequest`      | Old networking patterns may need modernization        |
| **Serialization dependencies**       | XML, binary, JSON, custom config                     | `BinaryFormatter` especially is a red flag            |
| **Registry dependencies**            | `Microsoft.Win32.Registry`                           | Windows-only and may affect portability               |
| **File/path dependencies**           | hardcoded paths, temp folders, user folders          | Migration can expose path and permission issues       |
| **GAC references**                   | Global Assembly Cache references                     | Not a good fit for modern .NET                        |
| **MSBuild/NuGet restore files**      | `.sln`, `.csproj`, `packages.config`, `nuget.config` | Determines how packages/builds restore                |


### 3.2.1. Useful search terms
- HintPath
- Reference Include
- COMReference
- Content Include
- EmbeddedResource
- None Include
- PostBuildEvent
- PreBuildEvent
- TargetFrameworkVersion
- packages.config
- App.config
- WebClient
- HttpWebRequest
- BinaryFormatter
- Registry
- Process.Start
- NuGet


### 3.2.2. Initial priority

1. Project-to-project references
2. Direct DLL references / HintPath references
3. .NET Framework assembly references
4. WinForms/designer/resource dependencies
5. App.config/settings dependencies
6. Build events / .targets / .props files
7. Installer/updater dependencies
8. Web/API dependencies
9. Test framework dependencies
10. Native/COM dependencies

## 3.3. Dependency Inventory

The following table summarizes the major dependency categories identified within the AutoWikiBrowser solution. Detailed analysis and migration recommendations for each dependency type are provided in the sections that follow.

| Dependency | Category | Used By | Current Status | Migration Risk | Planned Action | Notes |
|------------|----------|---------|----------------|----------------|----------------|-------|
| WikiFunctions | Project Reference | AutoWikiBrowser | Confirmed | Low | Migrate with core solution | Core shared library |
| Microsoft.mshtml | COM / Interop | AutoWikiBrowser | Confirmed | High | Investigate replacement or removal | Legacy Internet Explorer component |
| Newtonsoft.Json | NuGet Package | AutoWikiBrowser, WikiFunctions | Confirmed | Low | Retain initially; verify .NET 8 compatibility | Currently version 13.0.3 |
| SemanticVersion | NuGet Package | WikiFunctions | Confirmed | Low | Retain initially; verify compatibility | Semantic version helper library |
| System.* | Framework | Multiple projects | Confirmed | Medium | Review each assembly during migration | Some APIs may not exist in .NET 8 |
| System.Windows.Forms | Framework | AutoWikiBrowser | Confirmed | Low | Supported under .NET 8 Windows Desktop | Primary UI framework |
| Build-generated SvnInfo.cs | Build-time dependency | WikiFunctions | Confirmed | Medium | Investigate replacement with Git version metadata | Generated during build |
| Legacy MSBuild Project Format | Build System | Entire solution | Confirmed | High | Convert to SDK-style projects | Required before .NET 8 migration |
| ClickOnce Publish Settings | Deployment | AutoWikiBrowser | Confirmed | Medium | Re-evaluate deployment strategy | Legacy publishing model |
| Manifest / Certificate Signing | Deployment | AutoWikiBrowser | Confirmed | Medium | Review signing requirements | Legacy signing configuration |
| x86 Platform Target | Platform | AutoWikiBrowser | Confirmed | Medium | Reassess AnyCPU/x64 strategy | May affect compatibility |

## 3.4. Framework Dependencies

The project currently targets .NET Framework 4.8.1 and relies primarily on standard System.* assemblies together with Windows Forms.

Areas requiring review include:

- Framework assemblies unavailable in .NET 8
- Obsolete APIs
- Windows-only APIs
- WinForms compatibility

| Dependency Type | Item | Used By | Notes |
|---|---|---|---|
| Dynamic compilation | `Microsoft.CSharp` / `CSharpCodeProvider` | `CSharpEval` | Uses legacy .NET Framework CodeDOM compiler with `CompilerVersion = v4.0`. |

## 3.5. NuGet Packages
The project currently contains a mixture of framework references, project references, and external libraries. This section inventories packages managed through NuGet and identifies those requiring updates or replacement during the .NET 8 migration.

### 3.5.1.NuGet Dependency Summary

| Scope | Count | Notes |
|------|------:|-------|
| Application/runtime packages | 2 | Required by production code |
| Test-only packages | 16 | Used only by UnitTests |
| Total NuGet packages | 18 | Most NuGet usage is test-related |

### 3.5.2.Non UnitTest
| Package | Version | Used By Projects | Directly Installed In | Purpose | Migration Action |
|---------|---------|------------------|------------------------|---------|------------------|
| Newtonsoft.Json | 13.0,3 | AutoWikiBrowser, WikiFunctions | AutoWikiBrowser, WikiFunctions | JSON parsing/serialization | Keep initially; verify .NET 8 compatibility |
| SemanticVersion | 2.1.0 | WikiFunctions | WikiFunctions | A portable semantic version class library compliant with the 2.0 SemanticVersion standard (http://semver.org) | Keep initially; verify .NET 8 compatibility |


### 3.5.3.UnitTest-Only NuGet Packages
| Package | Version | Used By Projects | Directly Installed In | Purpose | Migration Action |
|---------|---------|------------------|------------------------|---------|------------------|
| NUnit		| 4.3.2 | UnitTests | UnitTests | NUnit can be used for a wide range of testing | Keep initially; verify .NET 8 compatibility |
| NUnit.Analyzers | 4.6.0 | UnitTests | UnitTests | analyzers and code fixes for test projects using NUnit 3+. The analyzers will mark wrong usages when writing tests, and the code fixes can be used to used to correct these usages. They will also aid in the transition from NUnit 3 to NUnit 4.  | Keep initially; verify .NET 8 compatibility |
| Nunit.Console | 3.19.1 | UnitTests | UnitTests |  the nunit3-console runner and test engine for version 3 of the NUnit unit-testing framework.  | Keep initially; verify .NET 8 compatibility |
| Nunit.ConsoleRunner | 3.19.1 | UnitTests | UnitTests | This package includes the nunit3-console runner and test engine for version 3 of the NUnit unit-testing framework.  | Keep initially; verify .NET 8 compatibility |
| Nunit.Extension.NUnitProjectLoader | 3.8.0 | UnitTests | UnitTests |  This extension allows the engine to run NUnit projects, which have a file extension of '.nunit'. | Keep initially; verify .NET 8 compatibility |
| Nunit.Extension.NUnitV2Driver | 3.9.0 | UnitTests | UnitTests | This extension allows NUnit to load and run tests compiled against earlier versions of the NUnit framework. Versions 2.0 through 2.7 are supported.  | Keep initially; verify .NET 8 compatibility |
| Nunit.Extension.NUnitV2ResultWriter | 3.8.0 | UnitTests | UnitTests |  This extension allows NUnit to create result files in the V2 format, which is used by many CI servers. | Keep initially; verify .NET 8 compatibility |
| Nunit.Extension.TeamCityEventListener | 1.0.9 | UnitTests | UnitTests |  This extension sends specially formatted messages about test progress to TeamCity as each test executes, allowing TeamCity to monitor progress. | Keep initially; verify .NET 8 compatibility |
| Nunit.Extension.VSPRojectLoader | 3.9.0 | UnitTests | UnitTests | This extension allows NUnit to recognize and load solutions and projects in Visual Studio format. It supports files of type .sln, .csproj, .vbproj, .vjsproj, .vcproj and .fsproj.  | Keep initially; verify .NET 8 compatibility |
| Nunit3TestAdapter | 4.6.0 | UnitTests | UnitTests | The NUnit3 TestAdapter for Visual Studio, all versions from 2012 and onwards, and DotNet (incl. .Net core), versions .net framework 4.6.2 or higher, .net core 3.1, .net 5 or higher.   | Keep initially; verify .NET 8 compatibility |
| System.Buffers | 4.6.0 | UnitTests | UnitTests | System.Buffers  | Keep initially; verify .NET 8 compatibility |
| System.Memory | 4.6.0 | UnitTests | UnitTests |  System.Memory | Keep initially; verify .NET 8 compatibility |
| System.Numerics.Vectors |4.6.0 | UnitTests | UnitTests | System.Numerics.Vectors  | Keep initially; verify .NET 8 compatibility |
| System.Runtime.CompilerServices.Unsafe | 6.1.0 | UnitTests | UnitTests | Provides the System.Runtime.CompilerServices.Unsafe class, which provides generic, low-level functionality for manipulating pointers. | Keep initially; verify .NET 8 compatibility |
| System.Threading.Tasks.Extensions | 4.5.4 | UnitTests | UnitTests | Provides additional types that simplify the work of writing concurrent and asynchronous code. | Keep initially; verify .NET 8 compatibility |
| System.ValueTuple | 4.5.0 | UnitTests | UnitTests | rovides the System.ValueTuple structs, which implement the underlying types for tuples in C# and Visual Basic. | Keep initially; verify .NET 8 compatibility |

## 3.6. COM / Interop Components

The dependency inventory currently identifies one known COM dependency.

| Component | Used By | Migration Risk | Planned Action |
|-----------|---------|----------------|----------------|
| Microsoft.mshtml | AutoWikiBrowser | High | Investigate replacement or removal |

## 3.7. Migration Risks

| Risk                                               | Impact | Status      | Mitigation                                                                        |
| -------------------------------------------------- | ------ | ----------- | --------------------------------------------------------------------------------- |
| Legacy COM/Interop dependencies (Microsoft.mshtml) | High   | Investigate | Determine whether replacement or removal is required.                             |
| Unknown external library management                | Medium | Investigate | Determine whether libraries are managed through NuGet or direct DLL references.   |
| Legacy .NET Framework APIs                         | Medium | Ongoing     | Inventory framework dependencies and identify replacement APIs where necessary.   |
| Utility project compatibility                      | Low    | Investigate | Evaluate each utility independently after the core application has been assessed. |
| Plugin compatibility                               | Low    | Deferred    | Plugins are intentionally excluded from the initial migration scope.              |


## 3.8. Recommendations
Based on the current dependency assessment so far:
1. Complete the dependency inventory before making any code changes.
2. Prioritize migration of the four core projects:
	1.  * AutoWikiBrowser
	2.  * WikiFunctions
	3.  * AWBUpdater (subject to investigation)
	4.  * UnitTests

3. Investigate all external libraries to determine how they are managed and whether updates are required.
4. Evaluate legacy COM/Interop components early in the migration to determine replacement strategies.
5. Defer bundled plugins until the core application reaches feature parity under .NET 8.
6. Reassess utilities after the core migration to determine which should be modernized, retained, or retired.

## 3.9. Build time dependencies
| Dependency Type | Item | Used By | Notes |
|---|---|---|---|
| Build-generated source | `WikiFunctions/SvnInfo.cs` | `Variables.Revision`, `Variables.RevisionNumber` | Legacy SVN revision metadata; updated during build. |


## 3.10 Related Documents
Prerequisites
-------------
00 – Foundation, Discovery & Planning
02 - Development Environment

Supporting
----------
03 – Solution Inventory
05 – Code Health Assessment

Operational
-----------
07 – Lessons Learned