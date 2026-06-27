##### \####################################

##### &#x20;# Plugin Strategy 

##### \####################################

Initial assessment: no bundled plugin is considered essential to the core modernization effort.

The plugin architecture itself should be preserved, because future extensions may use it. However, most existing bundled plugins can be excluded from the initial migration path to reduce noise and complexity.



Candidate plugins to retain/review:

\- TheTemplator

\- TypoScan

\- Delinker



All other bundled plugins are considered candidates for retirement or long-term deferral unless a future need is identified.

##### \####################################

##### &#x20;# Initial classification

##### \####################################

* Essential bundled plugins: none
* Candidate plugins to retain/review: TBD
* Candidate plugins to remove/defer: most bundled plugins

##### \####################################

##### &#x20;# Migration implication

##### \####################################

The first modernization pass should focus on the core application, shared libraries, and plugin interface/host behavior rather than upgrading every bundled plugin project.



##### \####################################

##### &#x20;# Solution Inventory

##### \####################################

##### \####################################

##### &#x20;# Migration Baseline

##### \####################################

Primary Solution:

\- AutoWikiBrowser no plugins.sln



Secondary Solution:

\- AutoWikiBrowser.sln (Reference only)



###### \######################################

###### &#x20;# Main applications, core and updater

###### \######################################

###### 

Project				| Folder		| Output	| Purpose		| Migration Scope	

|-------------------------------|-----------------------|---------------|-----------------------|------------------------------------------|

AutoWikiBrowser			| AWB			| EXE		| Main application	| Phase 1

|-------------------------------|-----------------------|---------------|-----------------------|------------------------------------------|

AWBUpdater			| AWBUpdater		| EXE		| Updater		|  Investigate

|-------------------------------|-----------------------|---------------|-----------------------|------------------------------------------|

WikiFunctions			| WikiFunctions		| DLL		| Core library		| Phase 1

|-------------------------------|-----------------------|---------------|-----------------------|------------------------------------------|





###### \####################################

###### &#x20;# Utilities and extras

###### \####################################

Project			| Folder	| Output	| Purpose		| Migration Scope

|-----------------------|---------------|---------------|-----------------------|------------------------------------------|

CheckPage converter	| Extras	| 		| Utilities		| Investigate

|-----------------------|---------------|---------------|-----------------------|------------------------------------------|

Copy			| Extras	| 		| Utilities		| Investigate

|-----------------------|---------------|---------------|-----------------------|------------------------------------------|

DBScanner		| Extras	| 		| Utilities		| Investigate

|-----------------------|---------------|---------------|-----------------------|------------------------------------------|

Regex Tester		| Extras	| 		| Utilities		| Investigate

|-----------------------|---------------|---------------|-----------------------|------------------------------------------|

Sandbox			| Extras	| 		| Utilities		| Investigate

|-----------------------|---------------|---------------|-----------------------|------------------------------------------|





###### \####################################

###### &#x20;# Tests

###### \####################################

Project			| Folder	| Output	| Purpose		| Migration Scope

|-----------------------|---------------|---------------|-----------------------|------------------------------------------|

UnitTests		| UnitTests	| DLL		| Tests			| Phase 1

|-----------------------|---------------|---------------|-----------------------|------------------------------------------|



###### \####################################

###### &#x20;# Plug-ins (for future work)

###### \####################################

Project			| Folder	| Output	| Purpose		| Migration Scope	| 

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

Delinker		| Plugins	| DLL		| Optional extension	| Retain / Review	| Candidate for post-core migration

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

TheTemplator		| Plugins	| DLL		| Optional extension	| Retain / Review	| Candidate for post-core migration

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

TypoScan		| Plugins	| DLL		| Optional extension	| Retain / Review	| Candidate for post-core migration

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

BingSearch		| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

CFD			| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

Fronds			| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

IFD			| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

KingbotK Plugin		| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|

NoLimitsPlugin		| Plugins	| DLL		| Optional extension	| Retire / Deferred	| Not needed for current goals

|-----------------------|---------------|---------------|-----------------------|------------------------------------------------------------|













