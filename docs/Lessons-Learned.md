\##Lessons Learned



Visual Studio may create an orphaned .resx file for non-resource projects. Deleting and restoring the file refreshed the IDE state and allowed Git to recognize it correctly.



\##Known Issue



AWB currently depends on SVN-generated revision metadata (SvnInfo.cs). This dependency should be replaced with a Git-based versioning strategy during the .NET 8 migration.

