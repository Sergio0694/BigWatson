using System;
using System.Diagnostics.CodeAnalysis;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models.Abstract;
using Newtonsoft.Json;
using Realms;

namespace BigWatsonDotNet.Models.Realm
{
    /// <summary>
    /// A class that represents the app events stored in the database
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class RealmEvent : RealmObject, ILog
    {
        /// <summary>
        /// Gets the key of the current event
        /// </summary>
        [PrimaryKey]
        public string Uid { get; set; }

        /// <summary>
        /// Gets the raw <see cref="Priority"/> level value for the current log
        /// </summary>
        public byte Level { get; set; }

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

        /// <inheritdoc/>
        [JsonProperty(nameof(Timestamp), Order = 3)]
        public DateTimeOffset Timestamp { get; set; }

        /// <inheritdoc/>
        [JsonProperty(nameof(AppVersion), Order = 4)]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public string AppVersion { get; set; }
    }
}
