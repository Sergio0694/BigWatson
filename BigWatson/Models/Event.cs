using System;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models.Abstract;
using BigWatsonDotNet.Models.Realm;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models
{
    /// <summary>
    /// A class that represents a standalone aapp event
    /// </summary>
    public sealed class Event : LogBase
    {
        /// <summary>
        /// Gets the priority associated with the log
        /// </summary>
        public EventPriority Priority { get; }

        /// <summary>
        /// Gets the log message
        /// </summary>
        [NotNull]
        public string Message { get; }

        internal Event([NotNull] RealmEvent log) : base(log.Timestamp.LocalDateTime, Version.Parse(log.AppVersion))
        {
            Priority = log.Priority;
            Message = log.Message;
        }
    }
}