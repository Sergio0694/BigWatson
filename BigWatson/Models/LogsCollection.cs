using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigWatsonDotNet.Interfaces;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models
{
    /// <summary>
    /// A class that wraps a grouped collection of saved logs
    /// </summary>
    public sealed class LogsCollection<TLog> : IReadOnlyCollection<IGrouping<VersionInfo, TLog>> where TLog : ILog
    {
        // Actual data source
        [NotNull, ItemNotNull]
        private readonly IReadOnlyCollection<IGrouping<VersionInfo, TLog>> Source;
        
        internal LogsCollection([NotNull, ItemNotNull] IReadOnlyCollection<IGrouping<VersionInfo, TLog>> source)
        {
            Source = source;
        }

        #region Public APIs

        /// <inheritdoc/>
        public int Count => Source.Count;

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<IGrouping<VersionInfo, TLog>> GetEnumerator() => Source.GetEnumerator();

        private int? _LogsCount;

        /// <summary>
        /// Gets the total number of logs stored in this instance
        /// </summary>
        public int LogsCount => _LogsCount ?? (_LogsCount = Source.Sum(g => g.Key.Logs)).Value;

        /// <summary>
        /// Gets a list of all the available logs stored in this instance
        /// </summary>
        public IEnumerable<TLog> Logs => Source.SelectMany(g => g);

        /// <summary>
        /// Gets the list of all the app versions with at least a single stored log
        /// </summary>
        public IEnumerable<Version> AppVersions => Source.Select(g => g.Key.AppVersion);

        #endregion
    }
}
