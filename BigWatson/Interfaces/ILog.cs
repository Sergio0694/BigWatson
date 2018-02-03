using System;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Interfaces
{
    /// <summary>
    /// An interface for all claasses that represent some type of log
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Gets the timestamp for the current log
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets the app version for the log
        /// </summary>
        [NotNull]
        Version AppVersion { get; }
    }
}