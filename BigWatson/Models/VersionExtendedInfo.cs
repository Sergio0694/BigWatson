using System;
using JetBrains.Annotations;

namespace BigWatson.Models
{
    /// <summary>
    /// A simple model that wraps the number of crashes for a given app version number
    /// </summary>
    public sealed class VersionExtendedInfo
    {
        /// <summary>
        /// Gets the number of total crashes for this app version
        /// </summary>
        public int Crashes { get; }

        /// <summary>
        /// Gets the app version associated with the current value
        /// </summary>
        [NotNull]
        public Version AppVersion { get; }

        // Internal constructor
        internal VersionExtendedInfo(int crashes, [NotNull] Version version)
        {
            Crashes = crashes;
            AppVersion = version;
        }
    }
}
