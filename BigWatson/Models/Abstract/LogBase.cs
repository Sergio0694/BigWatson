using System;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models.Abstract
{
    /// <summary>
    /// An base class for all claasses that represent some type of log
    /// </summary>
    public abstract class LogBase
    {
        /// <summary>
        /// Gets the timestamp for the current log
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the app version for the log
        /// </summary>
        [NotNull]
        public Version AppVersion { get; }

        private protected LogBase(DateTime timestamp, Version version)
        {
            Timestamp = timestamp;
            AppVersion = version;
        }
    }
}
