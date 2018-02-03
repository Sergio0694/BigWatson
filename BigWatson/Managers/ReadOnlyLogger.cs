using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
using BigWatsonDotNet.Models.Events;
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

        #region Crash reports

        /// <inheritdoc/>
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync() => LoadExceptionsAsync(r => r.All<RealmExceptionReport>());

        /// <inheritdoc/>
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<T>() where T : Exception
        {
            String type = typeof(T).ToString();
            return LoadExceptionsAsync(r => r.All<RealmExceptionReport>().Where(entry => entry.ExceptionType.Equals(type)));
        }

        // Loads and prepares an exceptions collection from the input data
        [Pure, ItemNotNull]
        private async Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync([NotNull] Func<Realm, IQueryable<RealmExceptionReport>> loader)
        {
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                RealmExceptionReport[] data = loader(realm).ToArray();

                var query =
                    from grouped in
                        from exception in
                            from raw in data
                            let sameType =
                                (from item in data
                                    where item.ExceptionType.Equals(raw.ExceptionType)
                                    orderby item.Timestamp descending
                                    select item).ToArray()
                            let versions =
                                (from entry in sameType
                                    group entry by entry.AppVersion
                                    into version
                                    orderby version.Key
                                    select version.Key).ToArray()
                            select new ExceptionReport(raw,
                                versions[0], versions[versions.Length - 1], sameType.Length,
                                sameType[0].Timestamp, sameType[sameType.Length - 1].Timestamp)
                        orderby exception.Timestamp descending
                        group exception by exception.AppVersion
                        into header
                        orderby header.Key descending
                        select header
                    let crashes = grouped.ToArray()
                    select new GroupedList<VersionInfo, ExceptionReport>(
                        new VersionInfo(crashes.Length, grouped.Key), crashes);

                return new LogsCollection<ExceptionReport>(query.ToArray());
            }
        }

        #endregion

        #region Event logs

        /// <inheritdoc/>
        public Task<LogsCollection<Event>> LoadEventsAsync() => LoadEventsAsync(r => r.All<RealmEvent>());

        /// <inheritdoc/>
        public Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority) 
            => LoadEventsAsync(r => r.All<RealmEvent>().Where(entry => entry.Priority == priority));

        // Loads and prepares an events collection from the input data
        [Pure, ItemNotNull]
        private async Task<LogsCollection<Event>> LoadEventsAsync([NotNull] Func<Realm, IQueryable<RealmEvent>> loader)
        {
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                RealmEvent[] data = loader(realm).ToArray();

                var query =
                    from grouped in
                        from item in
                            from raw in data
                            select new Event(raw)
                        orderby item.Timestamp descending
                        group item by item.AppVersion
                        into header
                        orderby header.Key descending
                        select header
                    let logs = grouped.ToArray()
                    select new GroupedList<VersionInfo, Event>(
                        new VersionInfo(logs.Length, grouped.Key), logs);

                return new LogsCollection<Event>(query.ToArray());
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
