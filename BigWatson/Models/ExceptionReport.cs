﻿using System;
using System.Text;
using SQLite.Net.Attributes;
using CanBeNull = JetBrains.Annotations.CanBeNullAttribute;

namespace BigWatson.Models
{
    /// <summary>
    /// A class that maps the database table used to store app crashes
    /// </summary>
    [Table("ExceptionReports")]
    public class ExceptionReport
    {
        /// <summary>
        /// Gets the key of the current Exception
        /// </summary>
        [Column(nameof(Uid)), PrimaryKey]
        public String Uid { get; internal set; }

        /// <summary>
        /// Gets a String representing the Type of the Exception
        /// </summary>
        [Column(nameof(ExceptionType)), NotNull]
        public String ExceptionType { get; internal set; }

        /// <summary>
        /// Gets the HResult associated to the Exception
        /// </summary>
        [Column(nameof(HResult)), NotNull]
        public int HResult { get; internal set; }

        /// <summary>
        /// Gets the message that was generated when the Exception was thrown
        /// </summary>
        [Column(nameof(Message)), NotNull]
        public String Message { get; internal set; }

        /// <summary>
        /// Gets the source of the Exception, if present
        /// </summary>
        [Column(nameof(Source)), NotNull]
        public String Source { get; internal set; }

        /// <summary>
        /// Gets the StackTrace for the current Exception
        /// </summary>
        [Column(nameof(StackTrace))]
        public String StackTrace { get; internal set; }

        /// <summary>
        /// Gets the version of the app when the Exception was thrown
        /// </summary>
        [Column(nameof(AppVersion)), NotNull]
        public String AppVersion { get; internal set; }

        /// <summary>
        /// Gets the number of ticks that represent the crash time
        /// </summary>
        [Column(nameof(CrashTime)), NotNull]
        public long CrashTime { get; internal set; }

        /// <summary>
        /// Gets the amount of memory that the app was using when the Exception was thrown
        /// </summary>
        [Column(nameof(UsedMemory)), NotNull]
        public long UsedMemory { get; internal set; }

        /// <summary>
        /// Gets the time of the crash
        /// </summary>
        [Ignore]
        public DateTime CrashDateTime
        {
            get { return DateTime.FromBinary(CrashTime); }
            private set { CrashTime = value.ToBinary(); }
        }

        /// <summary>
        /// Creates a new instance of the ExceptionReport with the given parameters
        /// </summary>
        /// <param name="type">The Type of the Exception</param>
        /// <param name="hResult">The Exception HResult</param>
        /// <param name="message">The Exception message, if present</param>
        /// <param name="source">The source of the Exception, if available</param>
        /// <param name="stackTrace">The StackTrace of the Exception</param>
        /// <param name="appVersion">The app version when the crash happened</param>
        /// <param name="crashTime">The crash time</param>
        /// <param name="usedMemory">The amount of memory that the app was using when it crashed</param>
        internal static ExceptionReport New(String type, int hResult, [CanBeNull] String message, [CanBeNull] String source, [CanBeNull] String stackTrace, 
            String appVersion, DateTime crashTime, long usedMemory)
        {
            return new ExceptionReport
            {
                Uid = Guid.NewGuid().ToString(),
                ExceptionType = type,
                HResult = hResult,
                Source = source ?? String.Empty,
                Message = message ?? String.Empty,
                StackTrace = stackTrace ?? String.Empty,
                CrashDateTime = crashTime,
                AppVersion = appVersion,
                UsedMemory = usedMemory
            };
        }

        #region Additional parameters

        /// <summary>
        /// Gets the minimum app version that generated this Exception type
        /// </summary>
        [Ignore]
        public String MinExceptionVersion { get; internal set; }

        /// <summary>
        /// Gets the maximum app version that generated this Exception type
        /// </summary>
        [Ignore]
        public String MaxExceptionVersion { get; internal set; }

        /// <summary>
        /// Gets the total number of times this Exception type was generated by the app
        /// </summary>
        [Ignore]
        public int ExceptionTypeOccurrencies { get; internal set; }

        /// <summary>
        /// Gets the extended info on the crash times for this instance
        /// </summary>
        [Ignore]
        public String CrashVersionsInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format("{0} {1}", ExceptionTypeOccurrencies, ExceptionTypeOccurrencies == 1 ? "time" : "times"));
                sb.Append(", ");
                sb.Append(String.IsNullOrEmpty(MaxExceptionVersion) 
                    ? $"in version {MinExceptionVersion}" 
                    : $"from {MinExceptionVersion} to {MaxExceptionVersion}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets the most recent crash time for this Exception type
        /// </summary>
        [Ignore]
        public long RecentCrashTime { get; internal set; }

        /// <summary>
        /// Gets the first time this Exception type was generated
        /// </summary>
        [Ignore]
        public long LessRecentCrashTime { get; internal set; }

        #endregion
    }
}