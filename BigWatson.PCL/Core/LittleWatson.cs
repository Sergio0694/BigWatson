using System;
using System.Threading.Tasks;
using BigWatson.PCL.Helpers;
using BigWatson.Shared.Models;
using JetBrains.Annotations;

namespace BigWatson.PCL.Core
{
    /// <summary>
    /// A static class that contains methods to log and save new exception reports
    /// </summary>
    internal static class LittleWatson
    {
        /// <summary>
        /// Gets the local settings instance to use
        /// </summary>
        private static ISettingsManager AppSettings => BigWatsonAPIs.SettingsManager;

        /// <summary>
        /// Saves the info on the current exception in the local settings
        /// </summary>
        /// <param name="e">The exception that was thrown by the app</param>
        /// <param name="version">The current app version</param>
        public static void LogException([NotNull] Exception e, [NotNull] Version version)
        {
            AppSettings.Clear();
            AppSettings.AddOrUpdateValue(nameof(ExceptionReport.ExceptionType), e.GetType().ToString());
            AppSettings.AddOrUpdateValue(nameof(ExceptionReport.Source), e.Source);
            AppSettings.AddOrUpdateValue(nameof(ExceptionReport.HResult), e.HResult);
            AppSettings.AddOrUpdateValue(nameof(ExceptionReport.Message), e.Message);
            AppSettings.AddOrUpdateValue(nameof(ExceptionReport.StackTrace), e.StackTrace);
            AppSettings.AddOrUpdateValue(nameof(ExceptionReport.AppVersion), version.ToString());
            AppSettings.AddOrUpdateValue(nameof(ExceptionReport.UsedMemory), BigWatsonAPIs.Reporter());
            AppSettings.AddOrUpdateValue(nameof(ExceptionReport.CrashTime), DateTime.Now.ToBinary());
        }

        /// <summary>
        /// Tries to flush to disk and get the last stored exception, if present
        /// </summary>
        public static Task<ExceptionReport> TryFlushPreviousExceptionAsync()
        {
            // Check if there's a pending exception
            if (!AppSettings.ContainsKey(nameof(ExceptionReport.ExceptionType))) return null;

            // Make sure the required info are present
            String
                type = AppSettings.GetValueOrDefault<String>(nameof(ExceptionReport.ExceptionType)),
                version = AppSettings.GetValueOrDefault<String>(nameof(ExceptionReport.AppVersion));
            if (type == null || version == null)
            {
                AppSettings.Clear();
                return null;
            }

            // Parse and return the new instance
            return BigWatson.LogExceptionAsync(
                type,
                AppSettings.GetValueOrDefault<int>(nameof(ExceptionReport.HResult)),
                AppSettings.GetValueOrDefault<String>(nameof(ExceptionReport.Message)),
                AppSettings.GetValueOrDefault<String>(nameof(ExceptionReport.Source)),
                AppSettings.GetValueOrDefault<String>(nameof(ExceptionReport.StackTrace)),
                new Version(AppSettings.GetValueOrDefault<String>(nameof(ExceptionReport.AppVersion))),
                DateTime.FromBinary(AppSettings.GetValueOrDefault<long>(nameof(ExceptionReport.CrashTime))),
                AppSettings.GetValueOrDefault<long>(nameof(ExceptionReport.UsedMemory)));
        }
    }
}
