# 03. Solution inventory

## 3.1 Plugin Strategy
Initial assessment: no bundled plugin is considered essential to the core modernization effort.

The plugin architecture itself should be preserved, because future extensions may use it. However, most existing bundled plugins can be excluded from the initial migration path to reduce noise and complexity.

Candidate plugins to retain/review:
- TheTemplator
- TypoScan
- Delinker

All other bundled plugins are considered candidates for retirement or long-term deferral unless a future need is identified.

## 3.2 Initial classification

* Essential bundled plugins: none
* Candidate plugins to retain/review: TBD
* Candidate plugins to remove/defer: most bundled plugins


## 3.3 Migration implication

The first modernization pass should focus on the core application, shared libraries, and plugin interface/host behavior rather than upgrading every bundled plugin project.


## 3.4 Solution Inventory
Current application versions of supporting apps within AWB
AutoWikiBrowser currently uses a legacy non-SDK-style project file targeting .NET Framework 4.8.1. The project will require conversion or recreation as an SDK-style .NET 8 Windows Desktop project before the final migration can be completed.
| Area           | Finding                                                |
| -------------- | ------------------------------------------------------ |
| Project format | Legacy `.csproj`, not SDK-style                        |
| Framework      | Targets `.NET Framework 4.8.1`                         |
| UI             | WinForms app                                           |
| COM/Browser    | Uses `Microsoft.mshtml`                                |
| NuGet          | `Newtonsoft.Json` referenced through `..\packages\...` |
| Platform       | Release builds as `x86`                                |
| Deployment     | Has old ClickOnce-style publish settings               |
| Signing        | Has old manifest/certificate settings                  |
| Designer       | Uses `UseWinFormsOutOfProcDesigner`                    |


## 3.5. Migration Baseline
Primary Solution:

- AutoWikiBrowser no plugins.sln

Secondary Solution:

- AutoWikiBrowser.sln (Reference only)


### 3.5.1. Main applications, core and updater


Project				| Folder		| Output	| Purpose		| Migration Scope
|-------------------------------|-----------------------|---------------|-----------------------|------------------------------------------|
AutoWikiBrowser			| AWB			| EXE		| Main application	| Phase 1
AWBUpdater			| AWBUpdater		| EXE		| Updater		|  Investigate
WikiFunctions			| WikiFunctions		| DLL		| Core library		| Phase 1


### 3.5.2. Utilities and extras

Project			| Folder	| Output	| Purpose		| Migration Scope
|-----------------------|---------------|---------------|-----------------------|------------------------------------------|
CheckPage converter	| Extras	| 		| Utilities		| Investigate
Copy			| Extras	| 		| Utilities		| Investigate
DBScanner		| Extras	| 		| Utilities		| Investigate
Regex Tester		| Extras	| 		| Utilities		| Investigate
Sandbox			| Extras	| 		| Utilities		| Investigate


### 3.5.3. Tests

Project			| Folder	| Output	| Purpose		| Migration Scope
|-----------------------|---------------|---------------|-----------------------|------------------------------------------|
UnitTests		| UnitTests	| DLL		| Tests			| Phase 1


### 3.5.4. Plug-ins (for future work)

Project			| Folder	| Output	| Purpose		| Migration Scope	|
|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|
Delinker		| Plugins	| DLL		| Optional extension	| Retain / Review	| Candidate for post-core migration
TheTemplator		| Plugins	| DLL		| Optional extension	| Retain / Review	| Candidate for post-core migration
TypoScan		| Plugins	| DLL		| Optional extension	| Retain / Review	| Candidate for post-core migration
BingSearch		| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals
CFD			| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals
Fronds			| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals
IFD			| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals
KingbotK Plugin		| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals
NoLimitsPlugin		| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals


### 3.5.5. Project Dependencies

| Project		| 	Depends on 		 |	Project references	 | Referenced By 		 | Notes			 	  |
|-----------------------|--------------------------------|-------------------------------|-------------------------------|----------------------------------------|
| AutoWikiBrowser	| WikiFunctions			 | Wikifunctions		 | None				 | AWBUpdater and UnitTests appear to	  |
|			|				 |				 |				 | be build dependencies only; verify why.|			
| WikiFunctions		| WikiFunctions, AWBUpdater, 	 | TBD				 | AutoWikiBrowser, 		 | Core shared library			  |			
|			| UnitTests			 | 				 | UnitTests			 | 					  |
| AWBUpdater		| None				 | TBD				 | Build dependency of 		 | Standalone utiliy		 	  |
|			|				 |				 | AutoWikiBrowser		 | (verify external dependencies)	  |
| UnitTests		| AutoWikiBrowser, WikiFunctions | TBD				 | Build dependency of		 | Verify whether project		  |
|			|				 |				 | AutoWikiBrowser		 | reference or test reference		  |

### 3.5.6. External References


The following external references have been identified during the initial inventory. Detailed compatibility analysis is documented in \*\*03-Dependency-Audit.md\*\*.


| Project 		| External Reference Types 					| Status 		|
|-----------------------|---------------------------------------------------------------|-----------------------|
| AutoWikiBrowser 	| Framework assemblies, COM/Interop, Third-party libraries 	| Identified		|
| WikiFunctions 	| Framework assemblies, Third-party libraries 			| Pending review 	|
| AWBUpdater 		| Pending analysis						| Investigate		|
| UnitTests 		| Framework assemblies						| Pending review	|


### 3.5.7. NuGet Packages


The package management strategy for the solution has not yet been fully assessed.

The dependency audit will determine:

- Whether projects use `packages.config`
- Whether projects use `PackageReference`
- Which third-party libraries are managed through NuGet
- Which dependencies are referenced directly as assemblies

Detailed findings will be documented in \*\*03-Dependency-Audit.md\*\*.

### 3.5.8. Build Order


| Order	| 	Project 	 | Reason	 					 | Notes			|
|:-----:|:-----------------------|:------------------------------------------------------|:-----------------------------|
| 1	| WikiFunctions		 | Core library used by other projects			 | Requires generated SvnInfo.cs during the build process. If the file is missing, the initial build/code analysis may fail until it is regenerated.|
| 2	| AutoWikiBrowser	 | Main application					 | Depends on WikiFunctions.dll. |
| 3	| AWBUpdater		 | Updater utility					 | Built after core application components.|
| 4	| UnitTests		 | Tests compiled after application/library projects	 | Depends on successful compilation of WikiFunctions and AutoWikiBrowser. |


#### 3.5.8.1 Build Prerequisites

The legacy build process generates `WikiFunctions/SvnInfo.cs`, which contains SVN revision metadata. If this generated source file is missing, the initial build or code analysis pass may fail because `WikiFunctions.dll` cannot be produced. A subsequent rebuild regenerates the file and allows dependent projects to compile successfully.

This legacy SVN-based versioning mechanism should be evaluated during the .NET 8 migration and replaced with a Git-based versioning strategy or removed if no longer required.


## 3.6. Startup Projects
Documents which project is configured to launch when debugging the solution.

| Solution			 | Startup Project	 | Status	| Notes 						|
|--------------------------------|-----------------------|--------------|-------------------------------------------------------|
| AutoWikiBrowser no plugins.sln | TBD			 | Investigate  | Verify in Visual Studio startup project settings.	|


## 3.7. Shared Resources
Identifies resources shared across projects, such as common images, icons, templates, scripts, or shared data files.

| Resource	 | Location	 | Used By	 | Notes		 |
|----------------|---------------|---------------|-----------------------|
| TBD		 | TBD		 | TBD		 | Pending review.	 |


## 3.8. Resource Files
Inventories project-specific resource files such as `.resx`, icons, embedded images, and UI assets.

| Project	  | Resource Type		 | Location	 | Notes			 |
|-----------------|------------------------------|---------------|-------------------------------|
| AutoWikiBrowser | Icons / images / resources	 | AWB/Resources | Pending detailed review.	 |

## 3.9. Configuration Files
Tracks application, build, package, and runtime configuration files that may affect migration.

| File		  | Location				| Purpose		    | Migration Notes			  |
|-----------------|-------------------------------------|---------------------------|-------------------------------------|
| app.config	  | TBD					| Application configuration | Verify existence and .NET 8 impact. |
| packages.config | TBD					| NuGet package management  | Verify whether used.		  |

## 3.10. Legacy Technologies
Identifies older technologies or platform-specific dependencies that may require special handling during .NET 8 migration.


| Technology			  | Used By		| Risk		 					| Notes					|
|---------------------------------|---------------------|-------------------------------------------------------|---------------------------------------|
| .NET Framework project format	  | All legacy projects	| Medium						| May require SDK-style conversion.	|
| WinForms | AutoWikiBrowser	  | Low / Medium	| Supported on .NET 8 Windows, 				|					|
| COM / Interop | AutoWikiBrowser | High		| Microsoft.mshtml requires investigation.		|					|



## 3.11. Related Documents
Prerequisites
-------------
00 – Foundation, Discovery & Planning
02 - Development Environment

Supporting
----------
04 – Dependency Audit
05 – Code Health Assessment

Operational
-----------
07 – Lessons Learned

























































