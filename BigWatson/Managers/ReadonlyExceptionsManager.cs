using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
using JetBrains.Annotations;
using Realms;

namespace BigWatsonDotNet.Managers
{
    /// <summary>
    /// A readonly exceptions manager to provider access to any kind of crash reports database
    /// </summary>
    internal class ReadOnlyExceptionsManager : IReadOnlyExceptionManager
    {
        /// <summary>
        /// Gets the default <see cref="RealmConfiguration"/> instance for the <see cref="Realm"/> used by the library
        /// </summary>
        [NotNull]
        protected RealmConfiguration Configuration { get; }

        public ReadOnlyExceptionsManager([NotNull] RealmConfiguration configuration) => Configuration = configuration;

        /// <inheritdoc/>
        public async Task<ExceptionsCollection> LoadCrashReportsAsync()
        {
            // Get all the app versions and the exceptions
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                ExceptionReport[] exceptions =
                    (from entry in realm.All<RealmExceptionReport>().ToArray()
                     select new ExceptionReport(entry)).ToArray();

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
                return new ExceptionsCollection(
                    from version in appVersions
                    let items =
                        (from exception in exceptions
                         where exception.AppVersion.Equals(version)
                         orderby exception.CrashTime descending
                         select exception).ToArray()
                    where items.Length > 0
                    select new GroupedList<VersionExtendedInfo, ExceptionReport>(
                        new VersionExtendedInfo(items.Length, version), items));
            }
        }

        /// <inheritdoc/>
        public async Task<ExceptionsCollection> LoadCrashReportsAsync<T>() where T : Exception
        {
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                // Get the exceptions with the same Type
                String type = typeof(T).ToString();
                ExceptionReport[] exceptions = 
                    (from entry in realm.All<RealmExceptionReport>().Where(entry => entry.ExceptionType.Equals(type))
                     select new ExceptionReport(entry)).ToArray();

                // Group by version
                return new ExceptionsCollection(
                    from exception in exceptions
                    group exception by exception.AppVersion
                    into version
                    orderby version.Key descending
                    let items =
                        (from entry in version
                         orderby entry.CrashTime descending
                         select entry).ToArray()
                    select new GroupedList<VersionExtendedInfo, ExceptionReport>(
                        new VersionExtendedInfo(items.Length, version.Key), items));
            }
        }
    }
}
