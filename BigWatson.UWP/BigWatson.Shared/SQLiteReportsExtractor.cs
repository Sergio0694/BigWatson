using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BigWatson.Shared.Misc;
using BigWatson.Shared.Models;
using JetBrains.Annotations;
using SQLite.Net.Async;

namespace BigWatson.Shared
{
    /// <summary>
    /// A class that executes queries on the input table to extract <see cref="ExceptionReport"/> instances
    /// </summary>
    internal static class SQLiteReportsExtractor
    {
        /// <summary>
        /// Loads the groups with the previous exceptions that were thrown by the app
        /// </summary>
        /// <param name="table">The source table query to use to read the data</param>
        /// <returns>A sequence of groups that have a <see cref="VersionExtendedInfo"/> key with the app version and the number of
        /// exception reports for that release, and a list of <see cref="ExceptionReport"/> with all the available
        /// reports for each version</returns>
        [Pure]
        [PublicAPI]
        [ItemNotNull]
        public static async Task<ExceptionsCollection> LoadGroupedExceptionsAsync([NotNull] AsyncTableQuery<ExceptionReport> table)
        {
            // Get all the app versions and the exceptions
            List<ExceptionReport> exceptions = await table.ToListAsync();

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
                    (from exception in exceptions
                    where exception.AppVersion.Equals(version)
                    orderby exception.CrashTime descending
                    select exception).ToArray()
                where items.Length > 0
                select new GroupedList<VersionExtendedInfo, ExceptionReport>(
                    new VersionExtendedInfo(items.Length, version), items);

            // Return the exceptions
            return new ExceptionsCollection(groupedList);
        }

        /// <summary>
        /// Returns a set of data with all the app versions that generated the input Exception type
        /// </summary>
        /// <param name="table">The source table query to use to read the data</param>
        /// <param name="exceptionType">The input Exception type to look for</param>
        /// <remarks>The <paramref name="exceptionType"/> parameter can be passed by calling the equivalent string of <see cref="Exception.GetType()"/>,
        /// by manually entering an exception type like "InvalidOperationException" or by passing the type from a loaded <see cref="ExceptionReport"/></remarks>
        /// <returns>A sequence of <see cref="VersionExtendedInfo"/> instances with the number of occurrences of the given exception type
        /// for each previous app version</returns>
        [Pure]
        [PublicAPI]
        [ItemNotNull]
        public static async Task<IEnumerable<VersionExtendedInfo>> LoadAppVersionsInfoAsync(
            [NotNull] AsyncTableQuery<ExceptionReport> table, [NotNull] String exceptionType)
        {
            // Get the exceptions with the same Type
            List<ExceptionReport> sameExceptions = await table.Where(entry => entry.ExceptionType == exceptionType).ToListAsync();

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
        /// <param name="table">The source table query to use to read the data</param>
        /// <param name="connection">The connection to the database in use</param>
        /// <param name="length">The maximum number of items in the database</param>
        /// <param name="token">The cancellation token for the operation</param>
        /// <returns>An <see cref="AsyncOperationResult{T}"/> instance that indicates whether the method execution was successful, 
        /// and eventually a readonly list of <see cref="ExceptionReport"/> instances that represents the reports that were just deleted</returns>
        [PublicAPI]
        public static async Task<AsyncOperationResult<IReadOnlyList<ExceptionReport>>> TryTrimAndOptimizeDatabaseAsync(
            [NotNull] AsyncTableQuery<ExceptionReport> table, [NotNull] SQLiteAsyncConnection connection, int length, CancellationToken token)
        {
            // Checks
            if (length < 0) throw new ArgumentOutOfRangeException("The length must be a positive number");
            if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

            // Make sure the database is connected
            if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

            try
            {
                // Check cleanup required
                int total = await table.CountAsync();
                if (total <= length) return new List<ExceptionReport>();
                if (token.IsCancellationRequested) return AsyncOperationStatus.Canceled;

                // Get all the instances and sort them chronologically
                List<ExceptionReport> reports = await table.OrderBy(entry => entry.CrashTime).ToListAsync();

                // Delete the required items
                int target = total - length;
                if (target <= 0) return AsyncOperationStatus.InternallyAborted; // This shouldn't happen
                List<ExceptionReport> deleted = new List<ExceptionReport>();
                for (int i = 0; i < target; i++)
                {
                    ExceptionReport report = reports[i];
                    await connection.DeleteAsync(report);
                    deleted.Add(report);
                }

                // Execute the VACUUM command
                await connection.ExecuteAsync("VACUUM;");
                return deleted;
            }
            catch
            {
                return AsyncOperationStatus.Faulted;
            }
        }
    }
}
