﻿using System;
using BigWatsonDotNet.Models.Abstract;
using BigWatsonDotNet.Models.Realm;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models
{
    /// <summary>
    /// A class that represents a standalone crash report
    /// </summary>
    public sealed class ExceptionReport : LogBase
    {
        #region Properties

        /// <summary>
        /// Gets the type of the exception for this crash report
        /// </summary>
        [NotNull]
        public string ExceptionType { get; }

        /// <summary>
        /// Gets the HResult associated to the exception
        /// </summary>
        public int HResult { get; }

        /// <summary>
        /// Gets the message that was generated when the Exception was thrown
        /// </summary>
        [NotNull]
        public string Message { get; }

        /// <summary>
        /// Gets the source of the Exception, if present
        /// </summary>
        [NotNull]
        public string Source { get; }

        /// <summary>
        /// Gets the enhanced staack trace for the current crash report
        /// </summary>
        [NotNull]
        public string StackTrace { get; }

        /// <summary>
        /// Gets the original stack trace for the current crash report
        /// </summary>
        [NotNull]
        public string NativeStackTrace { get; }

        /// <summary>
        /// Gets the amount of memory that the app was using when the Exception was thrown
        /// </summary>
        public long UsedMemory { get; }

        #endregion

        #region Additional parameters

        /// <summary>
        /// Gets the minimum app version that generated this Exception type
        /// </summary>
        [NotNull]
        public Version MinExceptionVersion { get; }

        /// <summary>
        /// Gets the maximum app version that generated this Exception type
        /// </summary>
        [NotNull]
        public Version MaxExceptionVersion { get; }

        /// <summary>
        /// Gets the total number of times this Exception type was generated by the app
        /// </summary>
        public int ExceptionTypeOccurrencies { get; }

        /// <summary>
        /// Gets the most recent crash time for this Exception type
        /// </summary>
        public DateTime MostRecentCrashTime { get; }

        /// <summary>
        /// Gets the first time this Exception type was generated
        /// </summary>
        public DateTime LeastRecentCrashTime { get; }

        #endregion

        internal ExceptionReport(
            [NotNull] RealmExceptionReport report,
            [NotNull] string min, [NotNull] string max, int occurrences,
            DateTimeOffset recent, DateTimeOffset old)
            : base(report.Timestamp.LocalDateTime, Version.Parse(report.AppVersion))
        {
            // Primary
            ExceptionType = report.ExceptionType;
            HResult = report.HResult;
            Message = report.Message ?? string.Empty;
            Source = report.Source ?? string.Empty;
            StackTrace = report.StackTrace ?? string.Empty;
            NativeStackTrace = report.NativeStackTrace ?? string.Empty;
            UsedMemory = report.UsedMemory;

            // Secondary
            MinExceptionVersion = Version.Parse(min);
            MaxExceptionVersion = Version.Parse(max);
            ExceptionTypeOccurrencies = occurrences;
            MostRecentCrashTime = recent.LocalDateTime;
            LeastRecentCrashTime = old.LocalDateTime;
        }
    }
}