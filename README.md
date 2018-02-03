![](http://i.cubeupload.com/XRD8pX.png)
[![NuGet](https://img.shields.io/nuget/v/BigWatson.svg)](https://www.nuget.org/packages/BigWatson/) [![NuGet](https://img.shields.io/nuget/dt/BigWatson.svg)](https://www.nuget.org/stats/packages/BigWatson?groupby=Version) [![AppVeyor](https://img.shields.io/appveyor/ci/Sergio0694/bigwatson.svg)](https://ci.appveyor.com/project/Sergio0694/bigwatson) [![AppVeyor tests](https://img.shields.io/appveyor/tests/Sergio0694/bigwatson.svg)](https://ci.appveyor.com/project/Sergio0694/bigwatson) [![Twitter Follow](https://img.shields.io/twitter/follow/Sergio0694.svg?style=flat&label=Follow)](https://twitter.com/SergioPedri)

A .NET Standard 2.0 library to easily log and review offline exception reports and messages for an app.

# Table of Contents

- [Installing from NuGet](#installing-from-nuget)
- [Quick start](#quick-start)
  - [Setup](#setup) 
  - [Browse reports](#browse-reports)
  - [Event logs](#event-logs)
  - [External databases](#external-databases)
- [Dependencies](#dependencies)
- [Credits](#credits)

# Installing from NuGet

To install **BigWatson**, run the following command in the **Package Manager Console**

```
Install-Package BigWatson
```

More details available [here](https://www.nuget.org/packages/BigWatson).

## Quick start

The library exposes various APIs to easily log exceptions and then manage the logs database.

### Setup

The only thing that's needed to get started with the library is to add an event handler to log the exceptions:

```C#
this.UnhandledException += (s, e) => BigWatson.Instance.Log(e.Exception);
```

And that's it! The library will now automatically log every exception thrown by the app and build a complete database with all the crash reports and their useful info, including the app version and the app memory usage.

### Browse reports

It is possible to load the complete list of previous exception reports, sorted by app versions, using:

```C#
var reports = await BigWatson.Instance.LoadCrashReportsAsync();
```

To load only the reports of a specific type, just use the following overload:

```C#
var reports = await BigWatson.Instance.LoadCrashReportsAsync<InvalidOperationException>();
```

It is also possible to trim the local exceptions database by deleting old reports that are no longer needed:

```C#
await BigWatson.Instance.TrimAsync(TimeSpan.FromDays(30));
```

### Event logs

Using **BigWatson** it is also possible to save event reports, which can be useful for analytics or debugging purposes:

```C#
BigWatson.Instance.Log(EventPriority.Info, "The user used the app 2 times today");
```

## External databases

If you want to share your crash reports database with someone else, or if you'd like your customers to be able to send their reports database to you, the library has some APIs to quickly share a database and load an external database file:

```C#
// Client-side
await BigWatson.Instance.ExportDatabaseAsync(pathToExportDatabase);

// Developer side
IReadOnlyExceptionManager watson = BigWatson.Load(pathToDatabase);
ExceptionsCollection clientReports = await watson.LoadCrashReportsAsync();
```

## Dependencies

The libraries use the following libraries and NuGet packages:

* [Realm.Database](https://www.nuget.org/packages/Realm.Database/)
* [Newtonsoft.Json](https://www.nuget.org/packages/newtonsoft.json/)
* [Ben.Demystifier](https://www.nuget.org/packages/Ben.Demystifier/)
* [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations/)

## Credits

BigWatson UWP is an extension of the [LittleWatson](https://www.alexhardwicke.com/little-watson/) class by Alex Hardwicke. In addition to that, this updated library also includes an internal Realm database to store and retrieve previous logs, and additional APIs.
The icon base image was made by Aldric Rodríguez from [thenounproject.com](https://thenounproject.com/).
