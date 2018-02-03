using System;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models;
using JetBrains.Annotations;
using Realms;

namespace BigWatsonDotNet.Interfaces
{
    /// <summary>
    /// The base interface for readonly exceptions managers
    /// </summary>
    public interface IReadOnlyLogger : IEquatable<IReadOnlyLogger>
    {
        /// <summary>
        /// Gets the disk size of the underlying database file, in bytes
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync();

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use
        /// for the app versions that generated the specified <see cref="Exception"/> type
        /// </summary>
        /// <typeparam name="TException">The <see cref="Exception"/> type to look for</typeparam>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<TException>() where TException : Exception;

        /// <summary>
        /// Loads the groups with the previous event logs from the <see cref="Realm"/> instance in use
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync();

        /// <summary>
        /// Loads the groups with the previous event logs from the <see cref="Realm"/> instance in use
        /// for the logs with the specified priority level
        /// </summary>
        /// <param name="priority">The target priority of the event logs to retrieve</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority);
    }
}