﻿using System;
using System.Text;
using JetBrains.Annotations;
using Realms;

namespace BigWatson.Models
{
    /// <summary>
    /// A class that maps the database table used to store app crashes
    /// </summary>
    public sealed class ExceptionReport : RealmObject
    {
        /// <summary>
        /// Gets the key of the current Exception
        /// </summary>
        [PrimaryKey]
        public String Uid { get; internal set; }

        /// <summary>
        /// Gets a String representing the Type of the Exception
        /// </summary>
        public String ExceptionType { get; internal set; }

        /// <summary>
        /// Gets the HResult associated to the Exception
        /// </summary>
        public int HResult { get; internal set; }

        /// <summary>
        /// Gets the message that was generated when the Exception was thrown
        /// </summary>
        public String Message { get; internal set; }

        /// <summary>
        /// Gets the source of the Exception, if present
        /// </summary>
        public String Source { get; internal set; }

        /// <summary>
        /// Gets the StackTrace for the current Exception
        /// </summary>
        public String StackTrace { get; internal set; }

        // Internal app version
        internal String InternalAppVersion { get; set; }

        /// <summary>
        /// Gets the version of the app when the Exception was thrown
        /// </summary>
        public Version AppVersion => new Version(InternalAppVersion);

        // Internal binary crash time
        internal long InternalCrashTime { get; set; }

        /// <summary>
        /// Gets the time of the crash
        /// </summary>
        [Ignored]
        public DateTime CrashTime
        {
            get => DateTime.FromBinary(InternalCrashTime);
            private set => InternalCrashTime = value.ToBinary();
        }

        /// <summary>
        /// Gets the amount of memory that the app was using when the Exception was thrown
        /// </summary>
        public long UsedMemory { get; internal set; }

        /// <summary>
        /// Creates a new instance of the ExceptionReport with the given parameters
        /// </summary>
        /// <param name="type">The Type of the Exception</param>
        /// <param name="hResult">The Exception HResult</param>
        /// <param name="message">The Exception message, if present</param>
        /// <param name="source">The source of the Exception, if available</param>
        /// <param name="stackTrace">The StackTrace of the Exception</param>
        /// <param name="version">The app version when the crash happened</param>
        /// <param name="crashTime">The crash time</param>
        /// <param name="usedMemory">The amount of memory that the app was using when it crashed</param>
        internal static ExceptionReport New([NotNull] String type, int hResult, [CanBeNull] String message, [CanBeNull] String source, 
            [CanBeNull] String stackTrace, [NotNull] Version version, DateTime crashTime, long usedMemory)
        {
            return new ExceptionReport
            {
                Uid = Guid.NewGuid().ToString(),
                ExceptionType = type,
                HResult = hResult,
                Source = source ?? String.Empty,
                Message = message ?? String.Empty,
                StackTrace = stackTrace ?? String.Empty,
                CrashTime = crashTime,
                InternalAppVersion = version.ToString(),
                UsedMemory = usedMemory
            };
        }

        #region Additional parameters

        /// <summary>
        /// Gets the minimum app version that generated this Exception type
        /// </summary>
        [Ignored]
        public Version MinExceptionVersion { get; internal set; }

        /// <summary>
        /// Gets the maximum app version that generated this Exception type
        /// </summary>
        [Ignored]
        public Version MaxExceptionVersion { get; internal set; }

        /// <summary>
        /// Gets the total number of times this Exception type was generated by the app
        /// </summary>
        [Ignored]
        public int ExceptionTypeOccurrencies { get; internal set; }

        /// <summary>
        /// Gets the extended info on the crash times for this instance
        /// </summary>
        [Ignored]
        public String CrashVersionsInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"{ExceptionTypeOccurrencies} {(ExceptionTypeOccurrencies == 1 ? "time" : "times")}");
                sb.Append(", ");
                sb.Append(MaxExceptionVersion == null
                    ? $"in version {MinExceptionVersion}" 
                    : $"from {MinExceptionVersion} to {MaxExceptionVersion}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets the most recent crash time for this Exception type
        /// </summary>
        [Ignored]
        public DateTime RecentCrashTime { get; internal set; }

        /// <summary>
        /// Gets the first time this Exception type was generated
        /// </summary>
        [Ignored]
        public DateTime LessRecentCrashTime { get; internal set; }

        #endregion
    }
}