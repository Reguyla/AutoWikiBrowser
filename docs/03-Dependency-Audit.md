\# 03. Dependency audit



\## 1. Executive Summary



This document inventories and evaluates the external and internal dependencies required to build and operate the AutoWikiBrowser solution. Its purpose is to identify components that may affect the migration from .NET Framework to .NET 8 and to document the strategy for addressing each dependency.



The audit includes:



\* Project-to-project references

\* Framework assemblies

\* Third-party libraries

\* COM/Interop components

\* NuGet packages

\* External build dependencies



Each dependency is classified according to its current status, migration risk, and recommended modernization strategy.



This document will be updated throughout the migration as dependencies are verified, upgraded, replaced, or retired.



\## 2. Project References



| Project         | Reference        |	Status			  | Type                    	   | Notes                          |

| --------------- | ---------------- | -------------------------- | ------------------------------ |------------------------------- |

| AutoWikiBrowser | WikiFunctions    | 	Confirmed		  | Project reference      	   | Core library                   |

| --------------- | ---------------- | -------------------------- | ------------------------------ |------------------------------- |

| AutoWikiBrowser | Microsoft.mshtml | 				  | COM / Interop reference	   | Migration risk                 |

| --------------- | ---------------- | -------------------------- | ------------------------------ |------------------------------- |

| AutoWikiBrowser | Newtonsoft.Json  | 				  | NuGet / package reference? 	   | Verify version                 |

| --------------- | ---------------- | -------------------------- | ------------------------------ |------------------------------- |

| AutoWikiBrowser | System.\*         | 				  | Framework reference		   | Review during .NET 8 migration |

| --------------- | ---------------- | -------------------------- | ------------------------------ |------------------------------- |





\## 3. External Dependencies

| Project         | Dependency           | Type        | Status      | Migration Risk | Notes                              |

| --------------- | -------------------- | ----------- | ----------- | -------------- | ---------------------------------- |

| AutoWikiBrowser | Microsoft.mshtml     | COM/Interop | Confirmed   | High           | Legacy Internet Explorer component |

| --------------- | -------------------- | ----------- | ----------- | -------------- | ---------------------------------- |

| AutoWikiBrowser | Newtonsoft.Json      | Unknown     | Investigate | Medium         | Determine whether NuGet or DLL     |

| --------------- | -------------------- | ----------- | ----------- | -------------- | ---------------------------------- |

| AutoWikiBrowser | System.Windows.Forms | Framework   | Confirmed   | Low            | Supported on .NET 8 Windows        |

| --------------- | -------------------- | ----------- | ----------- | -------------- | ---------------------------------- |





\## 4. Framework Dependencies

| Dependency | Status    | Notes                     |

| ---------- | --------- | ------------------------- |

| System.\*   | Confirmed | .NET Framework assemblies |

| ---------- | --------- | ------------------------- |





\## 5. Third-Party Libraries

| Library         | Current Source | Status      | Notes                                                   |

| --------------- | -------------- | ----------- | ------------------------------------------------------- |

| Newtonsoft.Json | Unknown        | Investigate | Determine whether NuGet package or direct DLL reference |

| --------------- | -------------- | ----------- | ------------------------------------------------------- |







\## 6. COM / Interop Components



| Component        | Status    | Migration Risk | Notes                                |

| ---------------- | --------- | -------------- | ------------------------------------ |

| Microsoft.mshtml | Confirmed | High           | Legacy Internet Explorer COM library |

| ---------------- | --------- | -------------- | ------------------------------------ |



\## 7. NuGet Packages

\### Overview



The project currently contains a mixture of framework references, project references, and external libraries. This section inventories packages managed through NuGet and identifies those requiring updates or replacement during the .NET 8 migration.



| Package | Version | Used By Projects | Directly Installed In | Purpose | Migration Action |

|---------|---------|------------------|------------------------|---------|------------------|

| Newtonsoft.Json | 13.0,3 | AutoWikiBrowser, WikiFunctions | AutoWikiBrowser, WikiFunctions | JSON parsing/serialization | Keep initially; verify .NET 8 compatibility |

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

| SemanticVersion | 2.1.0 | WikiFunctions | WikiFunctions | A portable semantic version class library compliant with the 2.0 SemanticVersion standard (http://semver.org) | Keep initially; verify .NET 8 compatibility |

| System.Buffers | 4.6.0 | UnitTests | UnitTests | System.Buffers  | Keep initially; verify .NET 8 compatibility |

| System.Memory | 4.6.0 | UnitTests | UnitTests |  System.Memory | Keep initially; verify .NET 8 compatibility |

| System.Numerics.Vectors |4.6.0 | UnitTests | UnitTests | System.Numerics.Vectors  | Keep initially; verify .NET 8 compatibility |

| System.Runtime.CompilerServices.Unsafe | 6.1.0 | UnitTests | UnitTests | Provides the System.Runtime.CompilerServices.Unsafe class, which provides generic, low-level functionality for manipulating pointers. | Keep initially; verify .NET 8 compatibility |

| System.Threading.Tasks.Extensions | 4.5.4 | UnitTests | UnitTests | Provides additional types that simplify the work of writing concurrent and asynchronous code. | Keep initially; verify .NET 8 compatibility |

| System.ValueTuple | 4.5.0 | UnitTests | UnitTests | rovides the System.ValueTuple structs, which implement the underlying types for tuples in C# and Visual Basic. | Keep initially; verify .NET 8 compatibility |





\### Package Inventory



| Project 	  | Package 		| Current Version | Latest Version | Status 	 | Migration Strategy 			| Notes 	|

|-----------------|---------|----------:|----------------:|----------------|-------------|--------------------------------------|---------------|

| AutoWikiBrowser | Newtonsoft.Json 	| TBD 		  | TBD 	   | Investigate | Determine if NuGet or direct DLL	|		|

|-----------------|---------|----------:|----------------:|----------------|-------------|--------------------------------------|---------------|





\## 8. Migration Risks



| Risk                                               | Impact | Status      | Mitigation                                                                        |

| -------------------------------------------------- | ------ | ----------- | --------------------------------------------------------------------------------- |

| Legacy COM/Interop dependencies (Microsoft.mshtml) | High   | Investigate | Determine whether replacement or removal is required.                             |

| -------------------------------------------------- | ------ | ----------- | --------------------------------------------------------------------------------- |

| Unknown external library management                | Medium | Investigate | Determine whether libraries are managed through NuGet or direct DLL references.   |

| -------------------------------------------------- | ------ | ----------- | --------------------------------------------------------------------------------- |

| Legacy .NET Framework APIs                         | Medium | Ongoing     | Inventory framework dependencies and identify replacement APIs where necessary.   |

| -------------------------------------------------- | ------ | ----------- | --------------------------------------------------------------------------------- |

| Utility project compatibility                      | Low    | Investigate | Evaluate each utility independently after the core application has been assessed. |

| -------------------------------------------------- | ------ | ----------- | --------------------------------------------------------------------------------- |

| Plugin compatibility                               | Low    | Deferred    | Plugins are intentionally excluded from the initial migration scope.              |

| -------------------------------------------------- | ------ | ----------- | --------------------------------------------------------------------------------- |





\## 9. Recommendations



Based on the current dependency assessment so far:



1\. Complete the dependency inventory before making any code changes.

2\. Prioritize migration of the four core projects:



&#x20;  \* AutoWikiBrowser

&#x20;  \* WikiFunctions

&#x20;  \* AWBUpdater (subject to investigation)

&#x20;  \* UnitTests

3\. Investigate all external libraries to determine how they are managed and whether updates are required.

4\. Evaluate legacy COM/Interop components early in the migration to determine replacement strategies.

5\. Defer bundled plugins until the core application reaches feature parity under .NET 8.

6\. Reassess utilities after the core migration to determine which should be modernized, retained, or retired.

