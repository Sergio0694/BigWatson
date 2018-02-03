using System;
using System.IO;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Interfaces
{
    /// <summary>
    /// An interface for an exceptions manager instance with write permission
    /// </summary>
    public interface ILogger : IReadOnlyLogger
    {
        /// <summary>
        /// Saves the crash report into local storage
        /// </summary>
        /// <param name="e">Exception that caused the app to crash</param>
        [PublicAPI]
        void Log([NotNull] Exception e);

        /// <summary>
        /// Saves a new event log into local storage
        /// </summary>
        /// <param name="priority">The event priority</param>
        /// <param name="message">The message for the event to log</param>
        void Log(EventPriority priority, [NotNull] String message);

        /// <summary>
        /// Removes all the logs in the databases older than the input <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the <see cref="ILog.Timestamp"/> property for each entry and the current time</param>
        [PublicAPI]
        Task TrimAsync(TimeSpan threshold);

        /// <summary>
        /// Removes all the logs of the specified type in the databases older than the input <see cref="TimeSpan"/>
        /// </summary>
        /// <typeparam name="TLog">The type of logs to trim</typeparam>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the <see cref="ILog.Timestamp"/> property for each entry and the current time</param>
        [PublicAPI]
        Task TrimAsync<TLog>(TimeSpan threshold) where TLog : ILog;

        /// <summary>
        /// Deletes all the existing logs present in the database
        /// </summary>
        [PublicAPI]
        Task ResetAsync();

        /// <summary>
        /// Deletes all the existing logs of the specified type from the database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to delete</typeparam>
        Task ResetAsync<TLog>() where TLog : ILog;

        /// <summary>
        /// Copies the content of the current logs database into a <see cref="Stream"/>
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<Stream> ExportAsync();

        /// <summary>
        /// Copies the content of the current logs database into a backup file with the specified path
        /// </summary>
        /// <param name="path">The path to the target backup file</param>
        [PublicAPI]
        Task ExportAsync([NotNull] String path);

        /// <summary>
        /// Exports the content of the current logs database as a JSON string
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<String> ExportAsJsonAsync();

        /// <summary>
        /// Exports the content of the current logs database into a JSON file with the specified path
        /// </summary>
        /// <param name="path">The path to the target export file</param>
        [PublicAPI]
        [Pure]
        Task ExportAsJsonAsync([NotNull] String path);
    }
}