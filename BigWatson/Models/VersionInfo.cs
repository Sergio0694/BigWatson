using System;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models
{
    /// <summary>
    /// A simple model that wraps an app version number and the number of related logs
    /// </summary>
    public sealed class VersionInfo
    {
        /// <summary>
        /// Gets the number of total logs for the current app version
        /// </summary>
        public int Logs { get; }

        /// <summary>
        /// Gets the app version for the current instance
        /// </summary>
        [NotNull]
        public Version AppVersion { get; }

        // Internal constructor
        internal VersionInfo(int logs, [NotNull] Version version)
        {
            Logs = logs;
            AppVersion = version;
        }
    }
}
