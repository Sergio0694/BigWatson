﻿using System;
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
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync() => LoadExceptionsAsync(TimeSpan.MaxValue);

        /// <inheritdoc/>
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync(TimeSpan threshold) => LoadExceptionsAsync(threshold, r => r.All<RealmExceptionReport>());

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<ExceptionReport>> LoadExceptionsAsync(Version version)
        {
            LogsCollection<ExceptionReport> groups = await LoadExceptionsAsync(TimeSpan.MaxValue, r => r.All<RealmExceptionReport>().Where(entry => entry.AppVersion == version.ToString()));
            return groups.Logs.ToArray();
        }

        /// <inheritdoc/>
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<TException>() where TException : Exception => LoadExceptionsAsync<TException>(TimeSpan.MaxValue);

        /// <inheritdoc/>
        public Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<TException>(TimeSpan threshold) where TException : Exception
        {
            string type = typeof(TException).ToString();
            return LoadExceptionsAsync(threshold, r => r.All<RealmExceptionReport>().Where(entry => entry.ExceptionType.Equals(type)));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<ExceptionReport>> LoadExceptionsAsync<TException>(Version version) where TException : Exception
        {
            string type = typeof(TException).ToString();
            LogsCollection<ExceptionReport> groups = await LoadExceptionsAsync(TimeSpan.MaxValue, r => r.All<RealmExceptionReport>().Where(entry => entry.ExceptionType.Equals(type) && entry.AppVersion == version.ToString()));
            return groups.Logs.ToArray();
        }

        // Loads and prepares an exceptions collection from the input data
        [Pure, ItemNotNull]
        private async Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync(TimeSpan threshold, [NotNull] Func<Realm, IQueryable<RealmExceptionReport>> loader)
        {
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                RealmExceptionReport[] data = loader(realm).ToArray().Where(entry => DateTimeOffset.Now.Subtract(entry.Timestamp) < threshold).ToArray();

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
        public Task<LogsCollection<Event>> LoadEventsAsync(TimeSpan threshold)
        {
            return LoadEventsAsync(r => r.All<RealmEvent>().Where(entry => DateTimeOffset.Now.Subtract(entry.Timestamp) < threshold));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<Event>> LoadEventsAsync(Version version)
        {
            LogsCollection<Event> groups = await LoadEventsAsync(r => r.All<RealmEvent>().Where(entry => entry.AppVersion == version.ToString()));
            return groups.Logs.ToArray();
        }

        /// <inheritdoc/>
        public Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority)
        {
            return LoadEventsAsync(r => r.All<RealmEvent>().Where(entry => entry.Priority == priority));
        }

        /// <inheritdoc/>
        public Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority, TimeSpan threshold)
        {
            return LoadEventsAsync(r => r.All<RealmEvent>().Where(entry => entry.Priority == priority && DateTimeOffset.Now.Subtract(entry.Timestamp) < threshold));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<Event>> LoadEventsAsync(EventPriority priority, Version version)
        {
            LogsCollection<Event> groups = await LoadEventsAsync(r => r.All<RealmEvent>().Where(entry => entry.Priority == priority && entry.AppVersion == version.ToString()));
            return groups.Logs.ToArray();
        }

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
