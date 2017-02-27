using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using BigWatson.Shared.Misc;
using BigWatson.Shared.Models;
using JetBrains.Annotations;

namespace BigWatson.UWP
{
    /// <summary>
    /// A static class that contains methods to log and save new exception reports
    /// </summary>
    public static class LittleWatson
    {
        #region Constants and parameters

        // Constants
        private const String LittleWatsonDetails = nameof(LittleWatsonDetails);

        /// <summary>
        /// Gets the app current version in the format "Major.Minor.Build.Revision"
        /// </summary>
        private static Version AppVersion
        {
            get
            {
                PackageVersion currentVersion = Package.Current.Id.Version;
                return new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build, currentVersion.Revision);
            }
        }

        #endregion

        #region APIs

        /// <summary>
        /// Saves the crash report into local storage
        /// </summary>
        /// <param name="ex">Exception that caused the app to crash</param>
        [PublicAPI]
        public static void LogException([NotNull] Exception ex)
        {
            // Get the container
            ApplicationData.Current.LocalSettings.CreateContainer(LittleWatsonDetails, ApplicationDataCreateDisposition.Always);
            IPropertySet exceptionValues = ApplicationData.Current.LocalSettings.Containers[LittleWatsonDetails].Values;

            // Save the Exception data
            exceptionValues[nameof(ExceptionReport.ExceptionType)] = ex.GetType().ToString();
            exceptionValues[nameof(ExceptionReport.Source)] = ex.Source;
            exceptionValues[nameof(ExceptionReport.HResult)] = ex.HResult;
            exceptionValues[nameof(ExceptionReport.Message)] = ex.Message;
            exceptionValues[nameof(ExceptionReport.StackTrace)] = ex.StackTrace;
            exceptionValues[nameof(ExceptionReport.AppVersion)] = AppVersion.ToString();
            exceptionValues[nameof(ExceptionReport.UsedMemory)] = (long)MemoryManager.AppMemoryUsage;
            exceptionValues[nameof(ExceptionReport.CrashTime)] = DateTime.Now.ToBinary();
        }

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
            // Get the settings container
            IPropertySet exceptionValues;
            try
            {
                ApplicationDataContainer container = ApplicationData.Current.LocalSettings.CreateContainer(
                    LittleWatsonDetails, ApplicationDataCreateDisposition.Existing);
                exceptionValues = container.Values;
            }
            catch
            {
                // Report not available
                return AsyncOperationStatus.RunToCompletion;
            }

            // Try to get the last Exception data
            try
            {
                // Log the Exception in the database
                ExceptionReport report = await BigWatson.LogExceptionAsync(
                    exceptionValues[nameof(ExceptionReport.ExceptionType)].To<String>(),
                    exceptionValues[nameof(ExceptionReport.HResult)].To<int>(),
                    exceptionValues[nameof(ExceptionReport.Message)].To<String>(),
                    exceptionValues[nameof(ExceptionReport.Source)].To<String>(),
                    exceptionValues[nameof(ExceptionReport.StackTrace)].To<String>(),
                    new Version(exceptionValues[nameof(ExceptionReport.AppVersion)].To<String>()), 
                    DateTime.FromBinary(exceptionValues[nameof(ExceptionReport.CrashTime)].To<long>()),
                    exceptionValues[nameof(ExceptionReport.UsedMemory)].To<long>());

                // Delete the previous report
                ApplicationData.Current.LocalSettings.DeleteContainer(LittleWatsonDetails);
                return report;
            }
            catch
            {
                // Error logging the exception
                return AsyncOperationStatus.Faulted;
            }
        }

        #endregion
    }
}
