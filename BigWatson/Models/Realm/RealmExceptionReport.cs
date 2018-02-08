using System;
using System.Diagnostics.CodeAnalysis;
using BigWatsonDotNet.Models.Abstract;
using Newtonsoft.Json;
using Realms;

namespace BigWatsonDotNet.Models.Realm
{
    /// <summary>
    /// A class that represents the crash reports stored in the database
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class RealmExceptionReport : RealmObject, ILog
    {
        /// <inheritdoc/>
        [PrimaryKey]
        public string Uid { get; set; }

        /// <summary>
        /// Gets a string representing the Type of the Exception
        /// </summary>
        [JsonProperty(nameof(ExceptionType), Order = 1)]
        public string ExceptionType { get; set; }

        /// <summary>
        /// Gets the HResult associated to the Exception
        /// </summary>
        [JsonProperty(nameof(HResult), Order = 2)]
        public int HResult { get; set; }

        /// <summary>
        /// Gets the message that was generated when the Exception was thrown
        /// </summary>
        [JsonProperty(nameof(Message), Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        /// <summary>
        /// Gets the source of the Exception, if present
        /// </summary>
        [JsonProperty(nameof(Source), Order = 4, NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }

        /// <summary>
        /// Gets the demystified stack trace for the current Exception
        /// </summary>
        [JsonProperty(nameof(StackTrace), Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public string StackTrace { get; set; }

        /// <summary>
        /// Gets the original stack trace for the current Exception
        /// </summary>
        [JsonProperty(nameof(NativeStackTrace), Order = 6, NullValueHandling = NullValueHandling.Ignore)]
        public string NativeStackTrace { get; set; }

        /// <inheritdoc/>
        [JsonProperty(nameof(Timestamp), Order = 7)]
        public DateTimeOffset Timestamp { get; set; }

        /// <inheritdoc/>
        [JsonProperty(nameof(AppVersion), Order = 8)]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public string AppVersion { get; set; }

        /// <summary>
        /// Gets the amount of memory that the app was using when the Exception was thrown
        /// </summary>
        [JsonProperty(nameof(UsedMemory), Order = 9, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long UsedMemory { get; set; }
    }
}
