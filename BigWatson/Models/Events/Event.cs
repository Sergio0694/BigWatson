using System;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Interfaces;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models.Events
{
    /// <summary>
    /// A class that represents a standalone aapp event
    /// </summary>
    public sealed class Event : ILog
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

        /// <inheritdoc/>
        public DateTime Timestamp { get; }

        /// <inheritdoc/>
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