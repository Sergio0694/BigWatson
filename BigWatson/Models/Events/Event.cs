using System;
using BigWatsonDotNet.Enums;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models.Events
{
    /// <summary>
    /// A class that represents a standalone aapp event
    /// </summary>
    public sealed class Event
    {
        /// <summary>
        /// Gets the priority associated with the log
        /// </summary>
        public EventPriority Priority { get; }

        /// <summary>
        /// Gets the log message
        /// </summary>
        [NotNull]
        public String Message { get; }

        /// <summary>
        /// Gets the timestamp for the current log
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the app version for the log
        /// </summary>
        [NotNull]
        public Version AppVersion { get; }

        internal Event([NotNull] RealmEvent log)
        {
            Priority = (EventPriority)log.Priority;
            Message = log.Message;
            Timestamp = log.Timestamp.LocalDateTime;
            AppVersion = Version.Parse(log.AppVersion);

        }
    }
}