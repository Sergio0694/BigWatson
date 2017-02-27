﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using BigWatson.Shared;
using BigWatson.Shared.Misc;
using BigWatson.Shared.Models;
using BigWatson.UWP.Misc;
using JetBrains.Annotations;
using SQLite.Net.Async;

namespace BigWatson.UWP
{
    /// <summary>
    /// Manages the exceptions database
    /// </summary>
    public static class BigWatson
    {
        #region Constants and parameters

        /// <summary>
        /// Gets the name of the local exceptions database file
        /// </summary>
        private const String DatabaseFileName = "ExceptionsData.db";

        /// <summary>
        /// Gets the path of the clean database
        /// </summary>
        private const String CleanDatabaseUri = "ms-appx:///Assets/Exceptions.db";

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

        #region Tools

        /// <summary>
        /// Makes sure the exceptions database is open and connected
        /// </summary>
        private static async Task EnsureDatabaseConnectionAsync()
        {
            if (_DatabaseInfo == null)
            {
                _DatabaseInfo = await SQLiteSharedHelper.InitializeDatabaseAsync(DatabaseFileName, CleanDatabaseUri);
            }
        }

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
            return await SQLiteReportsExtractor.LoadGroupedExceptionsAsync(ExceptionsTable);
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
            // Make sure the database is connected
            await EnsureDatabaseConnectionAsync();

            // Get the exceptions with the same Type
            return await SQLiteReportsExtractor.LoadAppVersionsInfoAsync(ExceptionsTable, exceptionType);
        }

        /// <summary>
        /// Makes sure the number of exception reports in the database isn't too high
        /// </summary>
        /// <param name="length">The maximum number of items in the database</param>
        /// <param name="token">The cancellation token for the operation</param>
        /// <returns>An <see cref="AsyncOperationResult{T}"/> instance that indicates whether the method execution was successful, 
        /// and eventually a readonly list of <see cref="ExceptionReport"/> instances that represents the reports that were just deleted</returns>
        [PublicAPI]
        public static async Task<AsyncOperationResult<IReadOnlyList<ExceptionReport>>>  TryTrimAndOptimizeDatabaseAsync(int length, CancellationToken token)
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
}