using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BigWatsonDotNet.Delegates;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models.Abstract;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Interfaces
{
    /// <summary>
    /// An interface for a logger instance with write permission
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
        [PublicAPI]
        void Log(EventPriority priority, [NotNull] string message);

        /// <summary>
        /// Removes all the logs in the databases older than the input <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the <see cref="LogBase.Timestamp"/> property for each entry and the current time</param>
        [PublicAPI]
        Task TrimAsync(TimeSpan threshold);

        /// <summary>
        /// Removes all the logs in the databases for app versions older or equal to the input <see cref="Version"/>
        /// </summary>
        /// <param name="version">The target <see cref="Version"/> value to use to trim the existing logs</param>
        [PublicAPI]
        Task TrimAsync([NotNull] Version version);

        /// <summary>
        /// Removes all the logs of the specified type in the databases older than the input <see cref="TimeSpan"/>
        /// </summary>
        /// <typeparam name="TLog">The type of logs to trim</typeparam>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the <see cref="LogBase.Timestamp"/> property for each entry and the current time</param>
        [PublicAPI]
        Task TrimAsync<TLog>(TimeSpan threshold) where TLog : LogBase;

        /// <summary>
        /// Removes all the logs of the specified type in the databases for app versions older or equal to the input <see cref="Version"/>
        /// </summary>
        /// <typeparam name="TLog">The type of logs to trim</typeparam>
        /// <param name="version">The target <see cref="Version"/> value to use to trim the existing logs</param>
        [PublicAPI]
        Task TrimAsync<TLog>([NotNull] Version version) where TLog : LogBase;

        /// <summary>
        /// Deletes all the existing logs present in the database
        /// </summary>
        [PublicAPI]
        Task ResetAsync();

        /// <summary>
        /// Deletes all the existing logs present in the database according to the input <see cref="Predicate{T}"/>
        /// </summary>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to select the logs to delete</param>
        [PublicAPI]
        Task ResetAsync<TLog>([NotNull] Predicate<TLog> predicate) where TLog : LogBase;

        /// <summary>
        /// Deletes all the existing logs present in the database for the specified app <see cref="Version"/>
        /// </summary>
        /// <param name="version">The target <see cref="Version"/> to use to delete the saved logs</param>
        [PublicAPI]
        Task ResetAsync([NotNull] Version version);

        /// <summary>
        /// Deletes all the existing logs of the specified type from the database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to delete</typeparam>
        [PublicAPI]
        Task ResetAsync<TLog>() where TLog : LogBase;

        /// <summary>
        /// Deletes all the existing logs of the specified type from the database for the specified app <see cref="Version"/>
        /// </summary>
        /// <typeparam name="TLog">The type of logs to delete</typeparam>
        /// <param name="version">The target <see cref="Version"/> to use to delete the saved logs</param>
        [PublicAPI]
        Task ResetAsync<TLog>([NotNull] Version version) where TLog : LogBase;

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
        Task ExportAsync([NotNull] string path);

        /// <summary>
        /// Exports the content of the current logs database as a JSON string
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<string> ExportAsJsonAsync();

        /// <summary>
        /// Exports the content of the current logs database as a JSON string, according to the input <see cref="Predicate{T}"/>
        /// </summary>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to select the logs to export</param>
        Task<string> ExportAsJsonAsync<TLog>([NotNull] Predicate<TLog> predicate) where TLog : LogBase;

        /// <summary>
        /// Exports the content of the current logs database as a JSON string
        /// </summary>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the timestamp of each entry and the current time</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<string> ExportAsJsonAsync(TimeSpan threshold);

        /// <summary>
        /// Exports the content of the current logs database as a JSON string
        /// </summary>
        /// <param name="version">The target app <see cref="Version"/> for the logs to export</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<string> ExportAsJsonAsync([NotNull] Version version);

        /// <summary>
        /// Exports the logs of the specified type as a JSON string
        /// </summary>
        /// <typeparam name="TLog">The type of logs to export</typeparam>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<string> ExportAsJsonAsync<TLog>() where TLog : LogBase;

        /// <summary>
        /// Exports the logs of the specified type as a JSON string
        /// </summary>
        /// <typeparam name="TLog">The type of logs to export</typeparam>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the timestamp of each entry and the current time</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<string> ExportAsJsonAsync<TLog>(TimeSpan threshold) where TLog : LogBase;

        /// <summary>
        /// Exports the logs of the specified type as a JSON string
        /// </summary>
        /// <typeparam name="TLog">The type of logs to export</typeparam>
        /// <param name="version">The target app <see cref="Version"/> for the logs to export</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<string> ExportAsJsonAsync<TLog>([NotNull] Version version) where TLog : LogBase;

        /// <summary>
        /// Exports the content of the current logs database into a JSON file with the specified path
        /// </summary>
        /// <param name="path">The path to the target export file</param>
        [PublicAPI]
        Task ExportAsJsonAsync([NotNull] string path);

        /// <summary>
        /// Exports the content of the current logs database into a JSON file with the specified path, according to the input <see cref="Predicate{T}"/>
        /// </summary>
        /// <param name="path">The path to the target export file</param>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to select the logs to export</param>
        Task ExportAsJsonAsync<TLog>([NotNull] string path, [NotNull] Predicate<TLog> predicate) where TLog : LogBase;

        /// <summary>
        /// Exports the content of the current logs database into a JSON file with the specified path
        /// </summary>
        /// <param name="path">The path to the target export file</param>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the timestamp of each entry and the current time</param>
        [PublicAPI]
        Task ExportAsJsonAsync([NotNull] string path, TimeSpan threshold);

        /// <summary>
        /// Exports the content of the current logs database into a JSON file with the specified path
        /// </summary>
        /// <param name="path">The path to the target export file</param>
        /// <param name="version">The target app <see cref="Version"/> for the logs to export</param>
        [PublicAPI]
        Task ExportAsJsonAsync([NotNull] string path, [NotNull] Version version);

        /// <summary>
        /// Exports the logs of the specified type into a JSON file with the specified path
        /// </summary>
        /// <typeparam name="TLog">The type of logs to export</typeparam>
        /// <param name="path">The path to the target export file</param>
        [PublicAPI]
        Task ExportAsJsonAsync<TLog>([NotNull] string path) where TLog : LogBase;

        /// <summary>
        /// Exports the logs of the specified type into a JSON file with the specified path
        /// </summary>
        /// <typeparam name="TLog">The type of logs to export</typeparam>
        /// <param name="path">The path to the target export file</param>
        /// <param name="threshold">The maximum <see cref="TimeSpan"/> between the timestamp of each entry and the current time</param>
        [PublicAPI]
        Task ExportAsJsonAsync<TLog>([NotNull] string path, TimeSpan threshold) where TLog : LogBase;

        /// <summary>
        /// Exports the logs of the specified type into a JSON file with the specified path
        /// </summary>
        /// <typeparam name="TLog">The type of logs to export</typeparam>
        /// <param name="path">The path to the target export file</param>
        /// <param name="version">The target app <see cref="Version"/> for the logs to export</param>
        [PublicAPI]
        Task ExportAsJsonAsync<TLog>([NotNull] string path, [NotNull] Version version) where TLog : LogBase;

        /// <summary>
        /// Flushes all the logs of the specified type using the input <see cref="LogUploader{TLog}"/> function and deletes them from the local database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to flush</typeparam>
        /// <param name="uploader">The <see cref="LogUploader{TLog}"/> function to use to upload the logs of the input type</param>
        /// <param name="token">A <see cref="CancellationToken"/> for the operation</param>
        /// <returns>The number of logs that have been flushed correctly</returns>
        [PublicAPI]
        Task<int> TryFlushAsync<TLog>([NotNull] LogUploader<TLog> uploader, CancellationToken token) where TLog : LogBase;

        /// <summary>
        /// Flushes all the logs of the specified type according to a given <see cref="Predicate{T}"/> using the input <see cref="LogUploader{TLog}"/> function and deletes them from the local database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to flush</typeparam>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to select the logs to flush</param>
        /// <param name="uploader">The <see cref="LogUploader{TLog}"/> function to use to upload the logs of the input type</param>
        /// <param name="token">A <see cref="CancellationToken"/> for the operation</param>
        /// <returns>The number of logs that have been flushed correctly</returns>
        [PublicAPI]
        Task<int> TryFlushAsync<TLog>([NotNull] Predicate<TLog> predicate, [NotNull] LogUploader<TLog> uploader, CancellationToken token) where TLog : LogBase;

        /// <summary>
        /// Flushes all the logs of the specified type using the input <see cref="LogUploaderWithToken{TLog}"/> function and deletes them from the local database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to flush</typeparam>
        /// <param name="uploader">The <see cref="LogUploaderWithToken{TLog}"/> function to use to upload the logs of the input type</param>
        /// <param name="token">A <see cref="CancellationToken"/> for the operation</param>
        /// <returns>The number of logs that have been flushed correctly</returns>
        [PublicAPI]
        Task<int> TryFlushAsync<TLog>([NotNull] LogUploaderWithToken<TLog> uploader, CancellationToken token) where TLog : LogBase;

        /// <summary>
        /// Flushes all the logs of the specified type according to a given <see cref="Predicate{T}"/> using the input <see cref="LogUploaderWithToken{TLog}"/> function and deletes them from the local database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to flush</typeparam>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to select the logs to flush</param>
        /// <param name="uploader">The <see cref="LogUploaderWithToken{TLog}"/> function to use to upload the logs of the input type</param>
        /// <param name="token">A <see cref="CancellationToken"/> for the operation</param>
        /// <returns>The number of logs that have been flushed correctly</returns>
        [PublicAPI]
        Task<int> TryFlushAsync<TLog>([NotNull] Predicate<TLog> predicate, [NotNull] LogUploaderWithToken<TLog> uploader, CancellationToken token) where TLog : LogBase;

        /// <summary>
        /// Flushes all the logs of the specified type using the input <see cref="LogUploader{TLog}"/> function and deletes them from the local database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to flush</typeparam>
        /// <param name="uploader">The <see cref="LogUploader{TLog}"/> function to use to upload the logs</param>
        /// <param name="token">A <see cref="CancellationToken"/> for the operation</param>
        /// <param name="mode">The desired execution mode. If <see cref="FlushMode.Parallel"/> is selected, the input function should be thread-safe to avoid issues</param>
        /// <returns>The number of logs that have been flushed correctly</returns>
        [PublicAPI]
        Task<int> TryFlushAsync<TLog>([NotNull] LogUploader<TLog> uploader, CancellationToken token, FlushMode mode) where TLog : LogBase;

        /// <summary>
        /// Flushes all the logs of the specified type according to a given <see cref="Predicate{T}"/> using the input <see cref="LogUploader{TLog}"/> function and deletes them from the local database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to flush</typeparam>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to select the logs to flush</param>
        /// <param name="uploader">The <see cref="LogUploader{TLog}"/> function to use to upload the logs</param>
        /// <param name="token">A <see cref="CancellationToken"/> for the operation</param>
        /// <param name="mode">The desired execution mode. If <see cref="FlushMode.Parallel"/> is selected, the input function should be thread-safe to avoid issues</param>
        /// <returns>The number of logs that have been flushed correctly</returns>
        [PublicAPI]
        Task<int> TryFlushAsync<TLog>([NotNull] Predicate<TLog> predicate, [NotNull] LogUploader<TLog> uploader, CancellationToken token, FlushMode mode) where TLog : LogBase;

        /// <summary>
        /// Flushes all the logs of the specified type using the input <see cref="LogUploaderWithToken{TLog}"/> function and deletes them from the local database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to flush</typeparam>
        /// <param name="uploader">The <see cref="LogUploaderWithToken{TLog}"/> function to use to upload the logs</param>
        /// <param name="token">A <see cref="CancellationToken"/> for the operation</param>
        /// <param name="mode">The desired execution mode. If <see cref="FlushMode.Parallel"/> is selected, the input function should be thread-safe to avoid issues</param>
        /// <returns>The number of logs that have been flushed correctly</returns>
        [PublicAPI]
        Task<int> TryFlushAsync<TLog>([NotNull] LogUploaderWithToken<TLog> uploader, CancellationToken token, FlushMode mode) where TLog : LogBase;

        /// <summary>
        /// Flushes all the logs of the specified type according to a given <see cref="Predicate{T}"/> using the input <see cref="LogUploaderWithToken{TLog}"/> function and deletes them from the local database
        /// </summary>
        /// <typeparam name="TLog">The type of logs to flush</typeparam>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to use to select the logs to flush</param>
        /// <param name="uploader">The <see cref="LogUploaderWithToken{TLog}"/> function to use to upload the logs</param>
        /// <param name="token">A <see cref="CancellationToken"/> for the operation</param>
        /// <param name="mode">The desired execution mode. If <see cref="FlushMode.Parallel"/> is selected, the input function should be thread-safe to avoid issues</param>
        /// <returns>The number of logs that have been flushed correctly</returns>
        [PublicAPI]
        Task<int> TryFlushAsync<TLog>([NotNull] Predicate<TLog> predicate, [NotNull] LogUploaderWithToken<TLog> uploader, CancellationToken token, FlushMode mode) where TLog : LogBase;
    }
}