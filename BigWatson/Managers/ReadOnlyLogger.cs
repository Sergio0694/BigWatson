using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
using BigWatsonDotNet.Models.Exceptions;
using JetBrains.Annotations;
using Realms;

namespace BigWatsonDotNet.Managers
{
    /// <summary>
    /// A readonly exceptions manager to provider access to any kind of crash reports database
    /// </summary>
    internal class ReadOnlyLogger : IReadOnlyLogger
    {
        /// <summary>
        /// Gets the default <see cref="RealmConfiguration"/> instance for the <see cref="Realm"/> used by the library
        /// </summary>
        [NotNull]
        protected RealmConfiguration Configuration { get; }

        public ReadOnlyLogger([NotNull] RealmConfiguration configuration) => Configuration = configuration;

        #region Reports loading

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
                    // Exceptions with the same type
                    ExceptionReport[] sameType =
                        (from item in exceptions
                         where item.ExceptionType.Equals(exception.ExceptionType)
                         orderby item.CrashTime descending
                         select item).ToArray();
                    exception.ExceptionTypeOccurrencies = sameType.Length;

                    // Update the crash times for the same Exceptions
                    exception.RecentCrashTime = sameType[0].CrashTime;
                    exception.LeastRecentCrashTime = sameType[sameType.Length - 1].CrashTime;

                    // Get the app versions for this exception type
                    Version[] versions =
                        (from entry in sameType
                         group entry by entry.AppVersion
                         into version
                         orderby version.Key
                         select version.Key).ToArray();

                    // Update the number of occurrencies and the app version interval
                    exception.MinExceptionVersion = versions[0];
                    exception.MaxExceptionVersion = versions[versions.Length - 1];
                }

                // Create the output collection
                return new ExceptionsCollection(
                    from grouped in 
                        from exception in exceptions
                        orderby exception.CrashTime descending
                        group exception by exception.AppVersion
                        into header
                        orderby header.Key descending
                        select header
                    let crashes = grouped.ToArray()
                    select new GroupedList<VersionInfo, ExceptionReport>(
                        new VersionInfo(crashes.Length, grouped.Key), crashes));
            }
        }

        /// <inheritdoc/>
        public async Task<ExceptionsCollection> LoadCrashReportsAsync<T>() where T : Exception
        {
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                // Get the exceptions with the same type
                String type = typeof(T).ToString();
                ExceptionReport[] exceptions = 
                    (from entry in realm.All<RealmExceptionReport>().Where(entry => entry.ExceptionType.Equals(type)).ToArray()
                     select new ExceptionReport(entry)).ToArray();

                // Update the info
                DateTime
                    oldest = exceptions.OrderBy(entry => entry.CrashTime).First().CrashTime,
                    newest = exceptions.OrderBy(entry => entry.CrashTime).Last().CrashTime;
                Version
                    min = exceptions.OrderBy(entry => entry.AppVersion).First().AppVersion,
                    max = exceptions.OrderBy(entry => entry.AppVersion).Last().AppVersion;
                foreach (ExceptionReport exception in exceptions)
                {
                    exception.ExceptionTypeOccurrencies = exceptions.Length;
                    exception.RecentCrashTime = newest;
                    exception.LeastRecentCrashTime = oldest;
                    exception.MinExceptionVersion = min;
                    exception.MaxExceptionVersion = max;
                }

                // Group by version
                return new ExceptionsCollection(
                    from grouped in 
                        from exception in exceptions
                        orderby exception.CrashTime descending
                        group exception by exception.AppVersion
                        into header
                        orderby header.Key descending
                        select header
                    let crashes = grouped.ToArray()
                    select new GroupedList<VersionInfo, ExceptionReport>(
                        new VersionInfo(crashes.Length, grouped.Key), crashes));
            }
        }

        #endregion

        #region IEquatable

        /// <inheritdoc/>
        public bool Equals(IReadOnlyLogger other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;
            return other.GetType() == GetType() &&
                   other is ReadOnlyLogger manager &&
                   manager.Configuration.DatabasePath.Equals(Configuration.DatabasePath);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as IReadOnlyLogger);

        /// <inheritdoc/>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                // DatabasePath is not readonly, but can't be changed after initialization (so it's fine here)
                return (17 + GetType().GetHashCode()) * 31 + Configuration.DatabasePath.GetHashCode();
            }
        }

        #endregion
    }
}
