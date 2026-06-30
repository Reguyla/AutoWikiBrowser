## 07. Lessons Learned
Visual Studio may create an orphaned .resx file for non-resource projects. Deleting and restoring the file refreshed the IDE state and allowed Git to recognize it correctly.

## 7.1 Known Issue
AWB currently depends on SVN-generated revision metadata (SvnInfo.cs). This dependency should be replaced with a Git-based versioning strategy during the .NET 8 migration.

## 7.2 Build notes

`SvnInfo.cs` is a generated build artifact. If deleted, the first rebuild may recreate it but still report errors because dependent projects cannot find `WikiFunctions.dll`. Running rebuild again after `SvnInfo.cs` is regenerated allows `WikiFunctions` and dependent projects to build successfully.

### Upgrade Assistant Findings May Be Inflated Before Project Conversion

The initial Upgrade Assistant analysis was performed against the legacy .NET Framework project format.

Many `Api.0001 (API does not exist)` incidents were associated with Windows Forms types (`System.Windows.Forms.*`), particularly within `*.Designer.cs` files.

These APIs are expected to be available after conversion to SDK-style projects targeting `net8.0-windows` with `UseWindowsForms=true`.

Recommendation:
Treat the initial incident counts as a baseline only. Re-run the Upgrade Assistant after project conversion to obtain a more accurate assessment of true migration issues.

## 7.3. Related Documents
Prerequisites
-------------
00 – Foundation, Discovery & Planning
01 - Migration Strategy
02 – Development Environment

Supporting
----------
03 – Solution Inventory
04 – Dependency Audit
05 – Code Health Assessment

Operational
-----------
06 – Change Control Log
08 - Migration-Assessment
09 - Project Terminology