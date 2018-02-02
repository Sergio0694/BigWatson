# BigWatson

A .NET Standard 2.0 library to easily log and review offline exception reports for an app.

## Usage

The library exposes various APIs to easily log exceptions and then manage the logs database.

### Setup

The first step is to add an event handler to log the exceptions. The logging method is synchronous because at this point the app will be in an undefined state and it'd be dangerous to call asynchronous code from here.

```C#
this.UnhandledException += (s, e) => BigWatson.LogException(e.Exception);
```

### Flush previous logs

At the next app restart after the crash, it will be possible to try to flush the previous exception report to the database. In case a previous exception report is found, it will be added to the database:

```C#
BigWatson.TryFlushPreviousException();
```

And that's it! The library will now automatically log every exception thrown by the app and build a complete database with all the crash reports and their useful info, including the app version and the app memory usage.

### Load and manage local craash reports

It is possible to load the complete list of previous exception reports, sorted by app versions, using:

```C#
ExceptionsCollection reports = await BigWatson.LoadGroupedExceptionsAsync();
```

To load additional versions info for a specific exception type, use this method:

```C#
IEnumerable<VersionExtendedInfo>> info = await BigWatson.LoadAppVersionsInfoAsync<InvalidOperationException>();
```

It is also possible to trim the local exceptions database by deleting old reports that are no longer needed:

```C#
await BigWatson.TrimDatabaseAsync(TimeSpan.FromDays(30));
```

## Dependencies

The libraries use the following libraries and NuGet packages:

* [Realm.Database](https://www.nuget.org/packages/Realm.Database/)
* [Realm.KeyValueStorage](https://www.nuget.org/packages/Realm.KeyValueStorage/)
* [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations/)

## Credits

BigWatson UWP is an extension of the [LittleWatson](https://www.alexhardwicke.com/little-watson/) class by Alex Hardwicke. In addition to that, this updated library also includes an internal Realm database to store and retrieve previous logs, and additional APIs.
