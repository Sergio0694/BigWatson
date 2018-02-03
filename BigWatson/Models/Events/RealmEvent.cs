using System;
using Newtonsoft.Json;
using Realms;

namespace BigWatsonDotNet.Models.Events
{
    /// <summary>
    /// A class that represents the app events stored in the database
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class RealmEvent : RealmObject
    {
        /// <summary>
        /// Gets the key of the current event
        /// </summary>
        [PrimaryKey]
        public String Uid { get; set; }

        /// <summary>
        /// Gets the priority associated with the log
        /// </summary>
        [JsonProperty(nameof(Priority), Order = 1)]
        public byte Priority { get; set; }

        /// <summary>
        /// Gets the log message
        /// </summary>
        [JsonProperty(nameof(Message), Order = 2)]
        public String Message { get; set; }

        /// <summary>
        /// Gets the timestamp for the current log
        /// </summary>
        [JsonProperty(nameof(Timestamp), Order = 3)]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the app version for the log
        /// </summary>
        [JsonProperty(nameof(AppVersion), Order = 4)]
        public String AppVersion { get; set; }
    }
}
