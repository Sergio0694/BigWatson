using System;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models.Abstract
{
    /// <summary>
    /// An interface for database logs, needed because the base <see cref="Realms.RealmObject"/> class can't be indirectly inherited
    /// </summary>
    internal interface ILog
    {
        /// <summary>
        /// Gets the key of the current event
        /// </summary>
        string Uid { get; }

        /// <summary>
        /// Gets the timestamp for the current log
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the app version for the log
        /// </summary>
        [NotNull]
        string AppVersion { get; }
    }
}
