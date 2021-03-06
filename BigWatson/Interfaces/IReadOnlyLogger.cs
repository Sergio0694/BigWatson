﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models;
using JetBrains.Annotations;
using Realms;

namespace BigWatsonDotNet.Interfaces
{
    /// <summary>
    /// The base interface for readonly logger instances
    /// </summary>
    public interface IReadOnlyLogger : IEquatable<IReadOnlyLogger>
    {
        /// <summary>
        /// Gets the disk size of the underlying database file, in bytes
        /// </summary>
        [PublicAPI]
        long Size { get; }

        /// <summary>
        /// Checks whether or not the <see cref="Realm"/> instance in use contains at least a crash report
        /// </summary>
        /// <returns></returns>
        [PublicAPI]
        [Pure]
        Task<bool> AnyExceptionsAsync();

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync();

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use, according to the input filter
        /// </summary>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to filter the saved exceptions</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync([NotNull] Predicate<ExceptionReport> predicate);

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use
        /// </summary>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the timestamp of each entry and the current time</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync(TimeSpan threshold);

        /// <summary>
        /// Loads the exceptions for the given app <see cref="Version"/> from the <see cref="Realm"/> instance in use
        /// </summary>
        /// <param name="version">The target <see cref="Version"/> to use to load saved logs</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<IReadOnlyList<ExceptionReport>> LoadExceptionsAsync([NotNull] Version version);

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use
        /// for the app versions that generated the specified <see cref="Exception"/> type
        /// </summary>
        /// <typeparam name="TException">The <see cref="Exception"/> type to look for</typeparam>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<TException>() where TException : Exception;

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use
        /// for the app versions that generated the specified <see cref="Exception"/> type
        /// </summary>
        /// <typeparam name="TException">The <see cref="Exception"/> type to look for</typeparam>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the timestamp of each entry and the current time</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<TException>(TimeSpan threshold) where TException : Exception;

        /// <summary>
        /// Loads the previous exceptions for the input app <see cref="Version"/> from the <see cref="Realm"/> instance in use
        /// for the app versions that generated the specified <see cref="Exception"/> type
        /// </summary>
        /// <typeparam name="TException">The <see cref="Exception"/> type to look for</typeparam>
        /// <param name="version">The target <see cref="Version"/> to use to load saved logs</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<IReadOnlyList<ExceptionReport>> LoadExceptionsAsync<TException>([NotNull] Version version) where TException : Exception;

        /// <summary>
        /// Loads the groups with the previous event logs from the <see cref="Realm"/> instance in use
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync();

        /// <summary>
        /// Loads the groups with the previous event logs from the <see cref="Realm"/> instance in use, according to the input filter
        /// </summary>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to filter the saved logs</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync([NotNull] Predicate<Event> predicate);

        /// <summary>
        /// Loads the groups with the previous event logs from the <see cref="Realm"/> instance in use
        /// </summary>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the timestamp of each entry and the current time</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync(TimeSpan threshold);

        /// <summary>
        /// Loads the previous event logs for the input app <see cref="Version"/> from the <see cref="Realm"/> instance in use
        /// </summary>
        /// <param name="version">The target <see cref="Version"/> to use to load saved logs</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<IReadOnlyList<Event>> LoadEventsAsync([NotNull] Version version);

        /// <summary>
        /// Loads the groups with the previous event logs from the <see cref="Realm"/> instance in use
        /// for the logs with the specified priority level
        /// </summary>
        /// <param name="priority">The target priority of the event logs to retrieve</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority);

        /// <summary>
        /// Loads the groups with the previous event logs from the <see cref="Realm"/> instance in use
        /// for the logs with the specified priority level
        /// </summary>
        /// <param name="priority">The target priority of the event logs to retrieve</param>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the timestamp of each entry and the current time</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority, TimeSpan threshold);

        /// <summary>
        /// Loads the previous event logs for the input app <see cref="Version"/> from the <see cref="Realm"/> instance in use
        /// for the logs with the specified priority level
        /// </summary>
        /// <param name="priority">The target priority of the event logs to retrieve</param>
        /// <param name="version">The target <see cref="Version"/> to use to load saved logs</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<IReadOnlyList<Event>> LoadEventsAsync(EventPriority priority, [NotNull] Version version);
    }
}