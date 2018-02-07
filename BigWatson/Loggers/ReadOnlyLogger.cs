using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
using BigWatsonDotNet.Models.Misc;
using BigWatsonDotNet.Models.Realm;
using JetBrains.Annotations;
using Realms;

namespace BigWatsonDotNet.Loggers
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
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync()
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                {
                    // Crash reports must never be partially loaded, as some properties depend on all the logs saved in the database*
                    RealmExceptionReport[] data = realm.All<RealmExceptionReport>().ToArray();

                    var query =
                        from grouped in
                            from exception in
                                from raw in data
                                let sameType = (
                                    from item in data
                                    where item.ExceptionType.Equals(raw.ExceptionType)
                                    orderby item.Timestamp descending
                                    select item).ToArray()
                                let versions = (
                                    from entry in sameType
                                    group entry by entry.AppVersion
                                    into version
                                    orderby version.Key
                                    select version.Key).ToArray()
                                select new ExceptionReport(raw,
                                    versions[0], versions[versions.Length - 1], sameType.Length, //*
                                    sameType[0].Timestamp, sameType[sameType.Length - 1].Timestamp)
                            orderby exception.Timestamp descending
                            group exception by exception.AppVersion
                            into header
                            orderby header.Key descending
                            select header
                        let crashes = grouped.ToArray()
                        select new ReadOnlyGroupingList<Version, ExceptionReport>(grouped.Key, crashes);

                    return new LogsCollection<ExceptionReport>(query.ToArray());
                }
            });
        }

        /// <inheritdoc/>
        public async Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync(Predicate<ExceptionReport> predicate)
        {
            var query = 
                from grouped in await LoadExceptionsAsync()
                let items = (
                    from item in grouped
                    where predicate(item)
                    select item).ToArray()
                select new ReadOnlyGroupingList<Version, ExceptionReport>(grouped.Key, items); 

            return new LogsCollection<ExceptionReport>(query.ToArray());
        }

        /// <inheritdoc/>
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync(TimeSpan threshold) => LoadExceptionsAsync(log => DateTimeOffset.Now.Subtract(log.Timestamp) < threshold);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ExceptionReport>> LoadExceptionsAsync(Version version) => (await LoadExceptionsAsync(log => log.AppVersion.Equals(version))).Logs;

        /// <inheritdoc/>
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<TException>() where TException : Exception
        {
            string type = typeof(TException).ToString();
            return LoadExceptionsAsync(log => log.ExceptionType.Equals(type));
        }

        /// <inheritdoc/>
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<TException>(TimeSpan threshold) where TException : Exception
        {
            string type = typeof(TException).ToString();
            return LoadExceptionsAsync(log =>log.ExceptionType.Equals(type) && DateTimeOffset.Now.Subtract(log.Timestamp) < threshold);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ExceptionReport>> LoadExceptionsAsync<TException>(Version version) where TException : Exception
        {
            string type = typeof(TException).ToString();
            return (await LoadExceptionsAsync(log => log.ExceptionType.Equals(type) && log.AppVersion.Equals(version))).Logs;
        }

        #endregion

        #region Event logs

        /// <inheritdoc/>
        public Task<LogsCollection<Event>> LoadEventsAsync() => LoadEventsAsync(r => r.All<RealmEvent>());

        /// <inheritdoc/>
        public async Task<LogsCollection<Event>> LoadEventsAsync(Predicate<Event> predicate)
        {
            var query = 
                from grouped in await LoadEventsAsync(r => r.All<RealmEvent>())
                let items = (
                    from item in grouped
                    where predicate(item)
                    select item).ToArray()
                select new ReadOnlyGroupingList<Version, Event>(grouped.Key, items); 

            return new LogsCollection<Event>(query.ToArray());
        }

        /// <inheritdoc/>
        public Task<LogsCollection<Event>> LoadEventsAsync(TimeSpan threshold)
        {
            return LoadEventsAsync(r =>
                from log in r.All<RealmEvent>().ToArray()
                where DateTimeOffset.Now.Subtract(log.Timestamp) < threshold
                select log);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Event>> LoadEventsAsync(Version version)
        {
            string _version = version.ToString();
            return (await LoadEventsAsync(r =>
                from log in r.All<RealmEvent>()
                where log.AppVersion == _version
                select log)).Logs;
        }

        /// <inheritdoc/>
        public Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority)
        {
            byte _priority = (byte)priority;
            return LoadEventsAsync(r =>
                from log in r.All<RealmEvent>().ToArray()
                where log.Level == _priority
                select log);
        }

        /// <inheritdoc/>
        public Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority, TimeSpan threshold)
        {
            byte _priority = (byte)priority;
            return LoadEventsAsync(r =>
                from item in (
                    from log in r.All<RealmEvent>().ToArray()
                    where log.Level == _priority
                    select log).ToArray()
                where DateTimeOffset.Now.Subtract(item.Timestamp) < threshold
                select item);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Event>> LoadEventsAsync(EventPriority priority, Version version)
        {
            byte _priority = (byte)priority;
            string _version = version.ToString();
            return (await LoadEventsAsync(r =>
                from log in r.All<RealmEvent>()
                where log.Level == _priority && log.AppVersion == _version
                select log)).Logs;
        }

        // Loads and prepares an events collection from the input data
        [Pure, ItemNotNull]
        private Task<LogsCollection<Event>> LoadEventsAsync([NotNull] Func<Realm, IEnumerable<RealmEvent>> loader)
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
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
                        select new ReadOnlyGroupingList<Version, Event>(grouped.Key, logs);

                    return new LogsCollection<Event>(query.ToArray());
                }
            });
        }

        #endregion

        #region Info

        /// <inheritdoc/>
        public long Size
        {
            get
            {
                FileInfo info = new FileInfo(Configuration.DatabasePath);
                return info.Exists ? info.Length : 0;
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
