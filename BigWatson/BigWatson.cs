using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BigWatson.Models;
using JetBrains.Annotations;
using Realms;
using Realms.KeyValueStorage;

namespace BigWatson
{
    /// <summary>
    /// The entry point with all the APIs exposed by the library
    /// </summary>
    public static class BigWatson
    {
        /// <summary>
        /// Gets the default <see cref="RealmConfiguration"/> instance for the <see cref="Realm"/> used by the library
        /// </summary>
        [NotNull]
        private static RealmConfiguration DefaultConfiguration
        {
            get
            {
                String
                    code = Assembly.GetExecutingAssembly().Location,
                    dll = Path.GetFullPath(code),
                    root = Path.GetDirectoryName(dll),
                    folder = Path.Combine(root, nameof(BigWatson)),
                    path = Path.Combine(folder, "crashreports.realm");
                return new RealmConfiguration(path);
            }
        }

        #region Logging

        /// <summary>
        /// Saves the crash report into local storage
        /// </summary>
        /// <param name="e">Exception that caused the app to crash</param>
        [PublicAPI]
        public static void LogException([NotNull] Exception e)
        {
            using (KeyValueRealm realm = KeyValueRealm.GetInstance())
            {
                realm.Set(nameof(ExceptionReport.ExceptionType), e.GetType().ToString());
                realm.Set(nameof(ExceptionReport.Source), e.Source);
                realm.Set(nameof(ExceptionReport.HResult), e.HResult);
                realm.Set(nameof(ExceptionReport.Message), e.Message);
                realm.Set(nameof(ExceptionReport.StackTrace), e.StackTrace);
                realm.Set(nameof(ExceptionReport.AppVersion), Assembly.GetExecutingAssembly().GetName().Version);
                realm.Set(nameof(ExceptionReport.UsedMemory), Process.GetCurrentProcess().VirtualMemorySize64);
                realm.Set(nameof(ExceptionReport.CrashTime), DateTime.Now.ToBinary());
            }
        }

        /// <summary>
        /// Checks for a previous temporary exception report and flushes it into the internal database if possible
        /// </summary>
        [PublicAPI]
        public static void TryFlushPreviousException()
        {
            Task.Run(() =>
            {
                try
                {
                    using (KeyValueRealm keyRealm = KeyValueRealm.GetInstance())
                    {
                        // Extract the previous report data
                        String
                            type = keyRealm.Get<String>(nameof(ExceptionReport.ExceptionType)),
                            message = keyRealm.Get<String>(nameof(ExceptionReport.Message)),
                            source = keyRealm.Get<String>(nameof(ExceptionReport.Source)),
                            stackTrace = keyRealm.Get<String>(nameof(ExceptionReport.StackTrace)),
                            version = keyRealm.Get<String>(nameof(ExceptionReport.AppVersion));
                        int hResult = keyRealm.Get<int>(nameof(ExceptionReport.HResult));
                        long
                            memory = keyRealm.Get<long>(nameof(ExceptionReport.UsedMemory)),
                            time = keyRealm.Get<long>(nameof(ExceptionReport.CrashTime));

                        // Save the report into the database
                        using (Realm realm = Realm.GetInstance(DefaultConfiguration))
                        using (Transaction transaction = realm.BeginWrite())
                        {
                            ExceptionReport report = ExceptionReport.New(type, hResult, message, source, stackTrace, Version.Parse(version), DateTime.FromBinary(time), memory);
                            realm.Add(report);
                            transaction.Commit();
                        }
                    }
                }
                catch
                {
                    // Previous report not found
                }
            });
        }

        /// <summary>
        /// Deletes all the existing exception reports present in the database
        /// </summary>
        [PublicAPI]
        public static async Task ClearDatabaseAsync()
        {
            using (Realm realm = await Realm.GetInstanceAsync(DefaultConfiguration))
            using (Transaction transaction = realm.BeginWrite())
            {
                realm.RemoveAll<ExceptionReport>();
                transaction.Commit();
            }
        }

        #endregion

        #region Reports inspection

        /// <summary>
        /// Loads the groups with the previous exceptions that were thrown by the app
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        public static async Task<ExceptionsCollection> LoadGroupedExceptionsAsync()
        {
            // Get all the app versions and the exceptions
            using (Realm realm = await Realm.GetInstanceAsync(DefaultConfiguration))
            {
                ExceptionReport[] exceptions = realm.All<ExceptionReport>().ToArray();

                // Update the type occurrencies and the other info
                foreach (ExceptionReport exception in exceptions)
                {
                    // Number of times this same Exception was thrown
                    exception.ExceptionTypeOccurrencies = exceptions.Count(item => item.ExceptionType.Equals(exception.ExceptionType));

                    // Exceptions with the same Type
                    ExceptionReport[] sameType =
                        (from item in exceptions
                         where item.ExceptionType.Equals(exception.ExceptionType)
                         orderby item.CrashTime descending
                         select item).ToArray();

                    // Update the crash times for the same Exceptions
                    exception.RecentCrashTime = sameType.First().CrashTime;
                    if (sameType.Length > 1) exception.LessRecentCrashTime = sameType.Last().CrashTime;

                    // Get the app versions for this Exception Type
                    Version[] versions =
                        (from entry in sameType
                         group entry by entry.AppVersion
                         into version
                         orderby version.Key
                         select version.Key).ToArray();

                    // Update the number of occurrencies and the app version interval
                    exception.MinExceptionVersion = versions.First();
                    if (versions.Length > 1) exception.MaxExceptionVersion = versions.Last();
                }

                // List the available app versions
                IEnumerable<Version> appVersions =
                    from exception in exceptions
                    group exception by exception.AppVersion
                    into header
                    orderby header.Key descending
                    select header.Key;

                // Create the output collection
                IEnumerable<GroupedList<VersionExtendedInfo, ExceptionReport>> groupedList =
                    from version in appVersions
                    let items =
                        (from exception in exceptions
                        where exception.AppVersion.Equals(version)
                        orderby exception.CrashTime descending
                        select exception).ToArray()
                    where items.Length > 0
                    select new GroupedList<VersionExtendedInfo, ExceptionReport>(
                        new VersionExtendedInfo(items.Length, version), items);

                // Return the exceptions
                return new ExceptionsCollection(groupedList);
            }
        }

        /// <summary>
        /// Returns the sequence of all the app versions that generated the specified <see cref="Exception"/> type
        /// </summary>
        /// <typeparam name="T">The <see cref="Exception"/> type to look for</typeparam>
        [PublicAPI]
        [Pure, ItemNotNull]
        public static async Task<IEnumerable<VersionExtendedInfo>> LoadAppVersionsInfoAsync<T>() where T : Exception
        {
            using (Realm realm = await Realm.GetInstanceAsync(DefaultConfiguration))
            {
                // Get the exceptions with the same Type
                String type = typeof(T).ToString();
                ExceptionReport[] exceptions = realm.All<ExceptionReport>().Where(entry => entry.ExceptionType.Equals(type)).ToArray();

                // Group the exceptions with their app version
                IEnumerable<Version> versions =
                    from exception in exceptions
                    group exception by exception.AppVersion
                    into version
                    orderby version.Key
                    select version.Key;

                // Return the chart data
                return
                    from version in versions
                    let count = exceptions.Count(item => item.AppVersion.Equals(version))
                    select new VersionExtendedInfo(count, version);
            }
        }

        /// <summary>
        /// Removes all the <see cref="ExceptionReport"/> instances in the databases older than the input <see cref="TimeSpan"/>
        /// </summary>
        [PublicAPI]
        public static async Task TrimDatabaseAsync(TimeSpan threshold)
        {
            using (Realm realm = await Realm.GetInstanceAsync(DefaultConfiguration))
            {
                IQueryable<ExceptionReport> old = realm.All<ExceptionReport>().Where(entry => DateTime.Now.Subtract(entry.CrashTime) > threshold);
                realm.RemoveRange(old);
            }

            Realm.Compact();
        }

        #endregion

        #region Tools

        /// <summary>
        /// Copies the content of the current crash reports database into a <see cref="Stream"/>
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        public static Task<Stream> ExportDatabaseAsync()
        {
            return Task.Run(() =>
            {
                // Copy the current database
                Stream stream = new MemoryStream();
                using (FileStream file = File.OpenRead(DefaultConfiguration.DatabasePath))
                {
                    byte[] buffer = new byte[8192];
                    while (true)
                    {
                        int read = file.Read(buffer, 0, buffer.Length);
                        if (read > 0) stream.Write(buffer, 0, read);
                        else break;
                    }
                }

                // Seek the result stream back to the start
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });
        }

        /// <summary>
        /// Copies the content of the current crash reports database into a backup file with the specified path
        /// </summary>
        /// <param name="path">The path to the target backup file</param>
        [PublicAPI]
        public static Task ExportDatabaseAsync([NotNull] String path)
        {
            return Task.Run(() =>
            {
                using (FileStream
                    source = File.OpenRead(DefaultConfiguration.DatabasePath),
                    destination = File.OpenWrite(path))
                {
                    byte[] buffer = new byte[8192];
                    while (true)
                    {
                        int read = source.Read(buffer, 0, buffer.Length);
                        if (read > 0) destination.Write(buffer, 0, read);
                        else break;
                    }
                }
            });
        }

        #endregion
    }
}
