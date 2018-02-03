using System;
using Newtonsoft.Json;
using Realms;

namespace BigWatsonDotNet.Models.Exceptions
{
    /// <summary>
    /// A class that represents the crash reports stored in the database
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
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
        [JsonProperty(nameof(ExceptionType), Order = 1)]
        public String ExceptionType { get; set; }

        /// <summary>
        /// Gets the HResult associated to the Exception
        /// </summary>
        [JsonProperty(nameof(HResult), Order = 2)]
        public int HResult { get; set; }

        /// <summary>
        /// Gets the message that was generated when the Exception was thrown
        /// </summary>
        [JsonProperty(nameof(Message), Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public String Message { get; set; }

        /// <summary>
        /// Gets the source of the Exception, if present
        /// </summary>
        [JsonProperty(nameof(Source), Order = 4, NullValueHandling = NullValueHandling.Ignore)]
        public String Source { get; set; }

        /// <summary>
        /// Gets the StackTrace for the current Exception
        /// </summary>
        [JsonProperty(nameof(StackTrace), Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public String StackTrace { get; set; }

        /// <summary>
        /// Gets the crash time for the report
        /// </summary>
        [JsonProperty(nameof(Timestamp), Order = 6)]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the app version for the report
        /// </summary>
        [JsonProperty(nameof(AppVersion), Order = 7)]
        public String AppVersion { get; set; }

        /// <summary>
        /// Gets the amount of memory that the app was using when the Exception was thrown
        /// </summary>
        [JsonProperty(nameof(UsedMemory), Order = 8, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long UsedMemory { get; set; }
    }
}
