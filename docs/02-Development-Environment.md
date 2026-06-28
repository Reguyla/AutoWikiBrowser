# 02. Development Environment
##  2.1. Executive summary
This document records the development environment used throughout the AutoWikiBrowser modernization project. Its purpose is to document the software, tools, configuration, and repository setup required to reproduce the development environment and ensure consistency across contributors.

## 2.2. Objectives
- Establish a reproducible development environment.
- Document required tools and software versions.
- Record repository configuration.
- Provide a baseline for future contributors.

##  2.3. Initial Setup

| Item               | Value       |
| ------------------ | ----------- |
| Initial Setup      | 2026-06-26  |
| Repository         | GitHub Fork |
| Base Branch        | master      |
| Development Branch | development |


## 2.4. Repository

- Forked from the official AutoWikiBrowser repository.
- Created a `development` branch for all active work.
- `master` remains the stable reference branch.

## 2.5. Development Environment

| Component     | Version                                   |
| ------------- | ----------------------------------------- |
| Windows       | Windows 11 Pro (or whatever you're using) |
| Visual Studio | 2022 Community                            |
| Git           | Installed                                 |
| .NET Framework Developer Pack      | 4.8.1                |


## 2.6. Local Repository

Repository cloned to: `C:\Users\<username>\source\repos\AutoWikiBrowser`

## 2.7. Status
- Repository cloned successfully.
- Git integration verified.
- Development branch established.
- Discovery and planning in progress.
- Solution inventory underway.
- Dependency assessment underway.


## 2.8. Development Toolchain

| Component        | Current Configuration        |
| ---------------- | ---------------------------- |
| IDE              | Visual Studio 2022 Community |
| Target Framework | .NET Framework 4.8.1         |
| Project Format   | Legacy MSBuild               |
| Build System     | MSBuild                      |
| Source Control   | Git                          |
| Static Analysis  | Roslynator                   |
| Code Cleanup     | CodeMaid                     |

## 2.9. Project Under Development
| Item               | Value                          |
| ------------------ | ------------------------------ |
| Project            | AutoWikiBrowser                |
| Source Repository  | GitHub Fork                    |
| Primary Solution   | AutoWikiBrowser no plugins.sln |
| Reference Solution | AutoWikiBrowser.sln            |
| Primary Branch     | development                    |
| Base Branch        | master                         |

## 2.10. Required Software

- Visual Studio 2022
- Git for Windows
- .NET Framework 4.8.1 Developer Pack
- Roslynator
- CodeMaid

## 2.11. Optional Software
- Notepad++
- Notepad
- WinMerge
- Powertoys

## 2.12. Project Baseline
| Item               | Value                      |
| ------------------ | -------------------------- |
| Baseline Revision  | SourceForge r13021         |
| Initial Git Commit | (your initial fork commit) |
| Target Platform    | .NET Framework 4.8.1       |
| Migration Target   | .NET 8                     |

## 2.13. Future Environment
The development environment will be updated during the migration to support:

- .NET 8 SDK
- SDK-style projects
- Updated build tooling
- Modern package management

## 2.14. Related Documents
Prerequisites
-------------
00 – Foundation, Discovery & Planning
01 - Migration Strategy

Supporting
----------
03 – Solution Inventory
04 – Dependency Audit
05 – Code Health Assessment

Operational
-----------
07 – Lessons Learned