using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <returns><see langword="true"/> if a previous crash report was found and flushed, <see langword="false"/> if no crash reports were found</returns>
        [PublicAPI]
        public static async Task<bool> TryFlushPreviousExceptionAsync()
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
                    using (Realm realm = await Realm.GetInstanceAsync(RealmConfiguration.DefaultConfiguration))
                    {
                        await realm.WriteAsync(r =>
                        {
                            ExceptionReport report = ExceptionReport.New(type, hResult, message, source, stackTrace, Version.Parse(version), DateTime.FromBinary(time), memory);
                            r.Add(report);
                        });
                    }
                }
                return true;
            }
            catch
            {
                // Previous report not found
                return false;
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
            using (Realm realm = await Realm.GetInstanceAsync(RealmConfiguration.DefaultConfiguration))
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
            using (Realm realm = await Realm.GetInstanceAsync(RealmConfiguration.DefaultConfiguration))
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
            using (Realm realm = await Realm.GetInstanceAsync(RealmConfiguration.DefaultConfiguration))
            {
                IQueryable<ExceptionReport> old = realm.All<ExceptionReport>().Where(entry => DateTime.Now.Subtract(entry.CrashTime) > threshold);
                realm.RemoveRange(old);
            }

            Realm.Compact();
        }

        #endregion
    }
}
