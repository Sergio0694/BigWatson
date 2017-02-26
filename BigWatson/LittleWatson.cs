using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using BigWatson.Misc;
using BigWatson.Models;
using JetBrains.Annotations;

namespace BigWatson
{
    /// <summary>
    /// Manages the error reports if the app crashes
    /// </summary>
    public class LittleWatson
    {
        // Constants
        private const String LittleWatsonDetails = nameof(LittleWatsonDetails);

        /// <summary>
        /// Gets the app current version in the format "Major.Minor.Build.Revision"
        /// </summary>
        public static String AppVersion
        {
            get
            {
                PackageVersion currentVersion = Package.Current.Id.Version;
                return $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}.{currentVersion.Revision}";
            }
        }

        /// <summary>
        /// Saves the crash report into local storage
        /// </summary>
        /// <param name="ex">Exception that caused the app to crash</param>
        [PublicAPI]
        public static void ReportException([NotNull] Exception ex)
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
            exceptionValues[nameof(ExceptionReport.AppVersion)] = AppVersion;
            exceptionValues[nameof(ExceptionReport.UsedMemory)] = (long)MemoryManager.AppMemoryUsage;
            exceptionValues[nameof(ExceptionReport.CrashDateTime)] = DateTime.Now.ToBinary();
        }

        /// <summary>
        /// If the app crashed the last time if was opened, reports the error
        /// </summary>
        [PublicAPI]
        public static async Task<AsyncOperationResult<ExceptionReport>> CheckForPreviousExceptionAsync()
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
                ExceptionReport report = await SQLiteExceptionsManager.LogExceptionAsync(
                    exceptionValues[nameof(ExceptionReport.ExceptionType)].To<String>(),
                    exceptionValues[nameof(ExceptionReport.HResult)].To<int>(),
                    exceptionValues[nameof(ExceptionReport.Message)].To<String>(),
                    exceptionValues[nameof(ExceptionReport.Source)].To<String>(),
                    exceptionValues[nameof(ExceptionReport.StackTrace)].To<String>(),
                    exceptionValues[nameof(ExceptionReport.AppVersion)].To<String>(),
                    DateTime.FromBinary(exceptionValues[nameof(ExceptionReport.CrashDateTime)].To<long>()),
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
    }
}
