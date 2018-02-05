using System;
using BigWatsonDotNet.Enums;
using Newtonsoft.Json;
using Realms;

namespace BigWatsonDotNet.Models.Realm
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
        public string Uid { get; set; }

        private byte Level { get; set; }

        /// <summary>
        /// Gets the priority associated with the log
        /// </summary>
        [Ignored]
        [JsonProperty(nameof(Priority), Order = 1)]
        public EventPriority Priority
        {
            get => (EventPriority)Level;
            set => Level = (byte)value;
        }

        /// <summary>
        /// Gets the log message
        /// </summary>
        [JsonProperty(nameof(Message), Order = 2)]
        public string Message { get; set; }

        /// <summary>
        /// Gets the timestamp for the current log
        /// </summary>
        [JsonProperty(nameof(Timestamp), Order = 3)]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the app version for the log
        /// </summary>
        [JsonProperty(nameof(AppVersion), Order = 4)]
        public string AppVersion { get; set; }
    }
}
