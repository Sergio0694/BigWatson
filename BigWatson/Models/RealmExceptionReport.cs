using System;
using Realms;

namespace BigWatson.Models
{
    /// <summary>
    /// A class that maps the database table used to store app crashes
    /// </summary>
    internal sealed class RealmExceptionReport : RealmObject
    {
        /// <summary>
        /// Gets the key of the current Exception
        /// </summary>
        [PrimaryKey]
        public String Uid { get; set; }

        /// <summary>
        /// Gets a String representing the Type of the Exception
        /// </summary>
        public String ExceptionType { get; set; }

        /// <summary>
        /// Gets the HResult associated to the Exception
        /// </summary>
        public int HResult { get; set; }

        /// <summary>
        /// Gets the message that was generated when the Exception was thrown
        /// </summary>
        public String Message { get; set; }

        /// <summary>
        /// Gets the source of the Exception, if present
        /// </summary>
        public String Source { get; set; }

        /// <summary>
        /// Gets the StackTrace for the current Exception
        /// </summary>
        public String StackTrace { get; set; }

        /// <summary>
        /// Gets the app version for the report
        /// </summary>
        public String AppVersion { get; set; }

        /// <summary>
        /// Gets the crash time for the report
        /// </summary>
        public long CrashTime { get; set; }

        /// <summary>
        /// Gets the amount of memory that the app was using when the Exception was thrown
        /// </summary>
        public long UsedMemory { get; set; }
    }
}
