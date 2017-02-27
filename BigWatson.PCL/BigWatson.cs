using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BigWatson.PCL.Helpers;
using BigWatson.PCL.Misc;
using BigWatson.Shared.Misc;
using BigWatson.Shared.Models;
using JetBrains.Annotations;
using PCLStorage;
using SQLite.Net;
using SQLite.Net.Async;

namespace BigWatson.PCL
{
    /// <summary>
    /// Manages the exceptions database
    /// </summary>
    public static class BigWatson
    {
        #region Constants and parameters

        /// <summary>
        /// Gets the name of the folder where the exceptions database is stored
        /// </summary>
        private const String DatabaseFolderName = "BigWatsonData";

        /// <summary>
        /// Gets the name of the local exceptions database file
        /// </summary>
        private const String DatabaseFileName = "ExceptionsData.db";

        /// <summary>
        /// The async connection to the local database in use
        /// </summary>
        private static SQLiteAsyncConnection DatabaseConnection => _DatabaseInfo.Connection;

        /// <summary>
        /// The file and connection for the current database in use
        /// </summary>
        private static DatabaseInfo _DatabaseInfo;

        private static AsyncTableQuery<ExceptionReport> _ExceptionsTable;

        /// <summary>
        /// Gets the table with all the stored exceptions
        /// </summary>
        private static AsyncTableQuery<ExceptionReport> ExceptionsTable => _ExceptionsTable ?? (_ExceptionsTable = DatabaseConnection.Table<ExceptionReport>());

        #endregion

        #region Database initialization

        /// <summary>
        /// Checks if the given Table is present in the database, and it creates it if it doesn't exist
        /// </summary>
        /// <typeparam name="T">The class that represents the database table</typeparam>
        private static async Task<bool> EnsureTablePresent<T>(SQLiteAsyncConnection connection) where T : class, new()
        {
            try
            {
                await connection.Table<T>().FirstOrDefaultAsync();
                return true;
            }
            catch (SQLiteException)
            {
                //The table doesn't exist
                return false;
            }
        }

        /// <summary>
        /// Loads a database up and connects to it, using a backup database if the target one isn't available
        /// </summary>
        private static async Task EnsureDatabaseConnectionAsync()
        {
            // Initial check
            if (_DatabaseInfo != null) return;

            // Try to get the database in use
            IFile database = await FileSystemHelper.TryGetFileAsync(DatabaseFileName, DatabaseFolderName);

            // Connect to the database
            if (BigWatsonAPIs.Platform == null) throw new InvalidOperationException("The SQLite platform hasn't been initialized");
            SQLiteConnectionString connectionString = new SQLiteConnectionString(database.Path, true);
            SQLiteConnectionWithLock lockConnection = new SQLiteConnectionWithLock(BigWatsonAPIs.Platform, connectionString);
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(() => lockConnection);

            // Make sure the table exists
            if (!await EnsureTablePresent<ExceptionReport>(connection)) await connection.CreateTableAsync<ExceptionReport>();

            // Store the reference to the database in use
            _DatabaseInfo = new DatabaseInfo(database, connection);
        }

        #endregion

        #region Tools

        /// <summary>
        /// Logs the given Exception into the local database
        /// </summary>
        /// <param name="type">The type of thee new Exception</param>
        /// <param name="hResult">The HRESULT of the new Exception</param>
        /// <param name="message">The optional message of the new Exception</param>
        /// <param name="source">The optional source of the new Exception</param>
        /// <param name="stackTrace">The optional stack trace of the new Exception</param>
        /// <param name="version">The app version that generated this Exception</param>
        /// <param name="crashTime">The crash time for the new exception to log</param>
        /// <param name="usedMemory">The amount of used memory when the Exception was generated</param>
        [ItemNotNull]
        internal static async Task<ExceptionReport> LogExceptionAsync([NotNull] String type, int hResult, [CanBeNull] String message,
            [CanBeNull] String source, [CanBeNull] String stackTrace, [NotNull] Version version, DateTime crashTime, long usedMemory)
        {
            // Make sure the database is connected
            await EnsureDatabaseConnectionAsync();

            ExceptionReport report = ExceptionReport.New(type, hResult, message,
                source, stackTrace, version, crashTime, usedMemory);
            await DatabaseConnection.InsertAsync(report);
            return report;
        }

        #endregion

        #region APIs

        /// <summary>
        /// Loads the groups with the previous exceptions that were thrown by the app
        /// </summary>
        /// <returns>A sequence of groups that have a <see cref="VersionExtendedInfo"/> key with the app version and the number of
        /// exception reports for that release, and a list of <see cref="ExceptionReport"/> with all the available
        /// reports for each version</returns>
        [Pure]
        [PublicAPI]
        public static async Task<IEnumerable<IGrouping<VersionExtendedInfo, ExceptionReport>>> LoadGroupedExceptionsAsync()
        {
            // Make sure the database is connected
            await EnsureDatabaseConnectionAsync();

            // Get all the app versions and the exceptions
            List<ExceptionReport> exceptions = await ExceptionsTable.ToListAsync();

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
            IEnumerable<GroupedList<VersionExtendedInfo, ExceptionReport>> groupedList =
                from version in appVersions
                let items =
                    from exception in exceptions
                    where exception.AppVersion.Equals(version)
                    orderby exception.CrashTime descending
                    select exception
                where items.Any()
                select new GroupedList<VersionExtendedInfo, ExceptionReport>(
                    new VersionExtendedInfo(items.Count(), version), items);

            // Return the exceptions
            return groupedList;
        }

        /// <summary>
        /// Returns a set of data with all the app versions that generated the input Exception type
        /// </summary>
        /// <param name="exceptionType">The input Exception type to look for</param>
        /// <remarks>The <paramref name="exceptionType"/> parameter can be passed by calling the equivalent string of <see cref="Exception.GetType()"/>,
        /// by manually entering an exception type like "InvalidOperationException" or by passing the type from a loaded <see cref="ExceptionReport"/></remarks>
        /// <returns>A sequence of <see cref="VersionExtendedInfo"/> instances with the number of occurrences of the given exception type
        /// for each previous app version</returns>
        [Pure]
        [PublicAPI]
        public static async Task<IEnumerable<VersionExtendedInfo>> LoadAppVersionsInfoAsync([NotNull] String exceptionType)
        {
            // Get the exceptions with the same Type
            List<ExceptionReport> sameExceptions = await ExceptionsTable.Where(entry => entry.ExceptionType == exceptionType).ToListAsync();

            // Group the exceptions with their app version
            IEnumerable<Version> versions =
                from exception in sameExceptions
                group exception by exception.AppVersion
                into version
                orderby version.Key
                select version.Key;

            // Return the chart data
            return
                from version in versions
                let count = sameExceptions.Count(item => item.AppVersion.Equals(version))
                select new VersionExtendedInfo(count, version);
        }

        /// <summary>
        /// Makes sure the number of exception reports in the database isn't too high
        /// </summary>
        /// <param name="length">The maximum number of items in the database</param>
        /// <param name="token">The cancellation token for the operation</param>
        /// <returns>An <see cref="AsyncOperationResult{T}"/> instance that indicates whether the method execution was successful, 
        /// and eventually a readonly list of <see cref="ExceptionReport"/> instances that represents the reports that were just deleted</returns>
        [PublicAPI]
        public static async Task<AsyncOperationResult<IReadOnlyList<ExceptionReport>>> TryTrimAndOptimizeDatabaseAsync(int length, CancellationToken token)
        {
            // Checks
            if (length < 0) throw new ArgumentOutOfRangeException("The length must be a positive number");
            if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

            // Make sure the database is connected
            await EnsureDatabaseConnectionAsync();
            if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

            try
            {
                // Check cleanup required
                int total = await ExceptionsTable.CountAsync();
                if (total <= length) return new List<ExceptionReport>();
                if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

                // Get all the instances and sort them chronologically
                List<ExceptionReport> reports = await ExceptionsTable.OrderBy(entry => entry.CrashTime).ToListAsync();

                // Delete the required items
                int target = total - length;
                if (target <= 0) return AsyncOperationStatus.InternallyAborted; // This shouldn't happen
                List<ExceptionReport> deleted = new List<ExceptionReport>();
                for (int i = 0; i < target; i++)
                {
                    ExceptionReport report = reports[i];
                    await DatabaseConnection.DeleteAsync(report);
                    deleted.Add(report);
                }

                // Execute the VACUUM command
                await DatabaseConnection.ExecuteAsync("VACUUM;");
                return deleted;
            }
            catch
            {
                return AsyncOperationStatus.Faulted;
            }
        }

        /// <summary>
        /// Returns a copy of the local exceptions database that can easily be exported from the app
        /// </summary>
        /// <param name="token">The cancellation token for the operation</param>
        [Pure]
        [PublicAPI]
        public static async Task<AsyncOperationResult<StorageFile>> ExportLogsAsync(CancellationToken token)
        {
            // Connect to the existing database
            await EnsureDatabaseConnectionAsync();
            if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

            // Try to get a copy of the database
            try
            {
                StorageFile copy = await _DatabaseInfo.File.CopyAsync(ApplicationData.Current.TemporaryFolder,
                    $"Exceptions[{DateTime.Now:yy-MM-dd_hh.mm.ss}].db", NameCollisionOption.FailIfExists);
                if (copy != null) return copy;
                return AsyncOperationStatus.InternallyAborted;
            }
            catch
            {
                return AsyncOperationStatus.Faulted;
            }
        }

        #endregion
    }
