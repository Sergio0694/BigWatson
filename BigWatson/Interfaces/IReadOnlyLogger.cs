using System;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models;
using BigWatsonDotNet.Models.Events;
using BigWatsonDotNet.Models.Exceptions;
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
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync();

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance in use
        /// for the app versions that generated the specified <see cref="Exception"/> type
        /// </summary>
        /// <typeparam name="T">The <see cref="Exception"/> type to look for</typeparam>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<ExceptionReport>> LoadExceptionsAsync<T>() where T : Exception;

        /// <summary>
        /// 
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<LogsCollection<Event>> LoadEventsAsync(EventPriority priority);
    }
}