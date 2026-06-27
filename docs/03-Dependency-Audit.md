\####################################

&#x20;## 1. Executive Summary

\####################################

\## Executive Summary



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



\####################################

&#x20;## 2. Project References

\####################################



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





\####################################

&#x20;## 3. External Dependencies

\####################################

| Project         | Dependency           | Type        | Status      | Migration Risk | Notes                              |

| --------------- | -------------------- | ----------- | ----------- | -------------- | ---------------------------------- |

| AutoWikiBrowser | Microsoft.mshtml     | COM/Interop | Confirmed   | High           | Legacy Internet Explorer component |

| --------------- | -------------------- | ----------- | ----------- | -------------- | ---------------------------------- |

| AutoWikiBrowser | Newtonsoft.Json      | Unknown     | Investigate | Medium         | Determine whether NuGet or DLL     |

| --------------- | -------------------- | ----------- | ----------- | -------------- | ---------------------------------- |

| AutoWikiBrowser | System.Windows.Forms | Framework   | Confirmed   | Low            | Supported on .NET 8 Windows        |

| --------------- | -------------------- | ----------- | ----------- | -------------- | ---------------------------------- |



\####################################

&#x20;## 4. Framework Dependencies

\####################################

| Dependency | Status    | Notes                     |

| ---------- | --------- | ------------------------- |

| System.\*   | Confirmed | .NET Framework assemblies |

| ---------- | --------- | ------------------------- |



\####################################

&#x20;## 5. Third-Party Libraries

\####################################

| Library         | Current Source | Status      | Notes                                                   |

| --------------- | -------------- | ----------- | ------------------------------------------------------- |

| Newtonsoft.Json | Unknown        | Investigate | Determine whether NuGet package or direct DLL reference |

| --------------- | -------------- | ----------- | ------------------------------------------------------- |



\####################################

&#x20;## 6. COM / Interop Components

\####################################

| Component        | Status    | Migration Risk | Notes                                |

| ---------------- | --------- | -------------- | ------------------------------------ |

| Microsoft.mshtml | Confirmed | High           | Legacy Internet Explorer COM library |

| ---------------- | --------- | -------------- | ------------------------------------ |



\####################################

&#x20;## 7. NuGet Packages

\####################################

\### Overview



The project currently contains a mixture of framework references, project references, and external libraries. This section inventories packages managed through NuGet and identifies those requiring updates or replacement during the .NET 8 migration.



\### Package Inventory



| Project 	  | Package 		| Current Version | Latest Version | Status 	 | Migration Strategy 			| Notes 	|

|-----------------|---------|----------:|----------------:|----------------|-------------|--------------------------------------|---------------|

| AutoWikiBrowser | Newtonsoft.Json 	| TBD 		  | TBD 	   | Investigate | Determine if NuGet or direct DLL	|		|

|-----------------|---------|----------:|----------------:|----------------|-------------|--------------------------------------|---------------|



\####################################

&#x20;## 8. Migration Risks

\####################################

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



\####################################

&#x20;## 9. Recommendations

\####################################

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













































