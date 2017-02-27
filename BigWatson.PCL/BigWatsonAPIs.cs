using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigWatson.PCL.Helpers;
using BigWatson.PCL.Misc;
using BigWatson.Shared.Misc;
using BigWatson.Shared.Models;
using JetBrains.Annotations;
using SQLite.Net.Interop;

namespace BigWatson.PCL
{
    /// <summary>
    /// A wrapper class with all the public TysAPIs exposed by the library
    /// </summary>
    public static class BigWatsonAPIs
    {
        #region Parameters and initialization

        /// <summary>
        /// Gets the current SQLite platform to use
        /// </summary>
        internal static ISQLitePlatform Platform { get; private set; }

        /// <summary>
        /// Gets the current settings manager
        /// </summary>
        internal static ISettingsManager SettingsManager { get; private set; }

        /// <summary>
        /// Gets the current app version
        /// </summary>
        internal static Version AppVersion { get; private set; }

        /// <summary>
        /// Gets the <see cref="MemoryReporter"/> delegate instance to use
        /// </summary>
        internal static MemoryReporter Reporter { get; private set; }

        /// <summary>
        /// Initializes the SQLite platform to use in the PCL and stores the authorization token for the app, this method must be called during startup
        /// </summary>
        /// <param name="platform">The current device platform</param>
        /// <param name="manager">The settings manager used to store the library data</param>
        /// <param name="version">The current app version</param>
        /// <param name="reporter">The <see cref="MemoryReporter"/> delegate instance to use</param>
        [PublicAPI]
        public static void InitializeLibrary(
            [NotNull] ISQLitePlatform platform, [NotNull] ISettingsManager manager, [NotNull] Version version, [NotNull] MemoryReporter reporter)
        {
            if (Platform != null || SettingsManager != null)
            {
                throw new InvalidOperationException("The library has already been initialized");
            }
            Platform = platform;
            SettingsManager = manager;
            AppVersion = version;
            Reporter = reporter;
        }

        #endregion

        #region Logging APIs

        /// <summary>
        /// Saves the crash report into local storage
        /// </summary>
        /// <param name="e">Exception that caused the app to crash</param>
        [PublicAPI]
        public static void LogException([NotNull] Exception e) => LittleWatson.LogException(e, AppVersion);

        /// <summary>
        /// Checks for a previous temporary exception report and flushes it into the internal database if possible
        /// </summary>
        /// <remarks>The status of the returned <see cref="AsyncOperationResult{T}"/> instance will be set to <see cref="AsyncOperationStatus.RunToCompletion"/>
        /// if a report is not found or if one is found and successfully stored to disk. In this case, the saved report will also be returned.
        /// In case of an error, the returned status will be set to <see cref="AsyncOperationStatus.Faulted"/></remarks>
        /// <returns>An <see cref="AsyncOperationResult{T}"/> instance that indicates whether the method execution was successful, 
        /// and eventually a <see cref="ExceptionReport"/> instance that represents the last thrown exception that was just logged into the database</returns>
        [PublicAPI]
        public static async Task<AsyncOperationResult<ExceptionReport>> TryFlushPreviousExceptionAsync()
        {
            // Try to get the last Exception data
            try
            {
                // Log the Exception in the database
                ExceptionReport report = await LittleWatson.TryFlushPreviousExceptionAsync();

                // Return the result
                return report;
            }
            catch
            {
                // Error logging the exception
                return AsyncOperationStatus.Faulted;
            }
        }

        #endregion

        #region Debugging APIs

        /// <summary>
        /// Loads all the exception reports currently stored on the device
        /// </summary>
        public static Task<IEnumerable<JumpListGroup<ExceptionsGroup, ExceptionReportDebugInfo>>> LoadExceptionReportsAsync()
        {
            return SQLiteManager.LoadSavedExceptionReportsAsync();
        }

        #endregion
    }
}
