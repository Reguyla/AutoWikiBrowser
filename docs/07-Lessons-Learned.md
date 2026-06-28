## 07. Lessons Learned
Visual Studio may create an orphaned .resx file for non-resource projects. Deleting and restoring the file refreshed the IDE state and allowed Git to recognize it correctly.

## 7.1 Known Issue
AWB currently depends on SVN-generated revision metadata (SvnInfo.cs). This dependency should be replaced with a Git-based versioning strategy during the .NET 8 migration.

## 7.2 Build notes

`SvnInfo.cs` is a generated build artifact. If deleted, the first rebuild may recreate it but still report errors because dependent projects cannot find `WikiFunctions.dll`. Running rebuild again after `SvnInfo.cs` is regenerated allows `WikiFunctions` and dependent projects to build successfully.

## 7.3. Related Documents
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