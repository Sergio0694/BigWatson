# BigWatson

A UWP library to easily log and review offline exception reports for an app.

This library is written in C# and can be used in any UWP application, with build 10240 as the minimum SDK version.

## Usage

The library exposes various APIs to easily log exceptions and then manage the logs database.

### Setup

The first step is to add an event handler to log the exceptions. The logging method is synchronous because at this point the app will be in an undefined state and it'd be a bad practice to call asynchronous code from here.

```C#
this.UnhandledException += (s, e) =>
{
    LittleWatson.LogException(e.Exception);
};
```

### Flush previous logs

At the next app restart after the crash, it will be possible to try to flush the previous exception report to the database. In case a previous exception report is found, it will be stored to disk and also returned by the method:

```C#
AsyncOperationResult<ExceptionReport> result = await LittleWatson.TryFlushPreviousExceptionAsync();
```

### Load and manage the local reports

It is possible to load the complete list of previous exception reports, sorted by app versions, using:

```C#
IEnumerable<IGrouping<VersionExtendedInfo, ExceptionReport>> reports = await BigWatson.LoadGroupedExceptionsAsync();
```

To load additional versions info for a specific exception type, use this method:

```C#
// Assuming we have an ExceptionReport here
IEnumerable<VersionExtendedInfo>> info = await BigWatson.LoadAppVersionsInfoAsync(report.ExceptionType);
```

It is also possible to trim the local exceptions database by setting a maximum number of reports that can be stored:

```C#
AsyncOperationResult<IReadOnlyList<ExceptionReport>> deletedReports = await BigWatson.TryTrimAndOptimizeDatabaseAsync(100, CancellationToken.None);
if (deletedReports) // Check if the status is set to AsyncOperationStatus.RunToCompletion
{
    // Optionally access the list of deleted reports here...
}
```

## Dependencies

The library uses the following libraries and NuGet packages:

* [SQLite for UWP](https://marketplace.visualstudio.com/items?itemName=SQLiteDevelopmentTeam.SQLiteforUniversalWindowsPlatform)
* [SQLite.Net.Async-PCL](https://www.nuget.org/packages/SQLite.Net.Async-PCL/)
* [SQLite.Net-PCL](https://www.nuget.org/packages/SQLite.Net.Core-PCL/)
* [SQLite.Net-Core-PCL](https://www.nuget.org/packages/SQLite.Net.Core-PCL/)
* [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations)