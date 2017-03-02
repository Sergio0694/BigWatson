using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BigWatson.PCL.Helpers;
using BigWatson.PCL.Misc;
using BigWatson.Shared;
using BigWatson.Shared.Misc;
using BigWatson.Shared.Models;
using JetBrains.Annotations;
using PCLStorage;
using SQLite.Net;
using SQLite.Net.Async;

namespace BigWatson.PCL.Core
{
    /// <summary>
    /// Manages the exceptions database
    /// </summary>
    internal static class BigWatson
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
        public static async Task<ExceptionReport> LogExceptionAsync([NotNull] String type, int hResult, [CanBeNull] String message,
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

        // Loads the groups with the previous exceptions that were thrown by the app
        [Pure]
        [PublicAPI]
        public static async Task<ExceptionsCollection> LoadGroupedExceptionsAsync()
        {
            // Make sure the database is connected
            await EnsureDatabaseConnectionAsync();

            // Get all the app versions and the exceptions
            return await SQLiteReportsExtractor.LoadGroupedExceptionsAsync(ExceptionsTable);
        }

        // Returns a set of data with all the app versions that generated the input Exception type
        [Pure]
        [PublicAPI]
        public static async Task<IEnumerable<VersionExtendedInfo>> LoadAppVersionsInfoAsync([NotNull] String exceptionType)
        {
            // Make sure the database is connected
            await EnsureDatabaseConnectionAsync();

            // Get the exceptions with the same Type
            return await SQLiteReportsExtractor.LoadAppVersionsInfoAsync(ExceptionsTable, exceptionType);
        }

        // Makes sure the number of exception reports in the database isn't too high
        [PublicAPI]
        public static async Task<AsyncOperationResult<IReadOnlyList<ExceptionReport>>> TryTrimAndOptimizeDatabaseAsync(int length, CancellationToken token)
        {
            // Checks
            if (length < 0) throw new ArgumentOutOfRangeException("The length must be a positive number");
            if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

            // Make sure the database is connected
            await EnsureDatabaseConnectionAsync();
            if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

            // Perform the optimization
            return await SQLiteReportsExtractor.TryTrimAndOptimizeDatabaseAsync(ExceptionsTable, DatabaseConnection, length, token);
        }

        #endregion
    }
}
