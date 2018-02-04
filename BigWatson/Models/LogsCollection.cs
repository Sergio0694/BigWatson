using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigWatsonDotNet.Models.Abstract;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models
{
    /// <summary>
    /// A class that wraps a grouped collection of saved logs
    /// </summary>
    public sealed class LogsCollection<TLog> : IReadOnlyCollection<IGrouping<VersionInfo, TLog>> where TLog : LogBase
    {
        // Actual data source
        [NotNull, ItemNotNull]
        private readonly IReadOnlyCollection<IGrouping<VersionInfo, TLog>> Source;
        
        internal LogsCollection([NotNull, ItemNotNull] IReadOnlyCollection<IGrouping<VersionInfo, TLog>> source)
        {
            Source = source;
        }

        #region Public APIs

        /// <summary>
        /// Gets the list of stored logs for the given app <see cref="Version"/>
        /// </summary>
        /// <param name="version">The <see cref="Version"/> to use to retrieve saved logs</param>
        [NotNull, ItemNotNull]
        public IEnumerable<TLog> this[[NotNull] Version version] => this.FirstOrDefault(group => group.Key.AppVersion.Equals(version)) ?? new TLog[0].AsEnumerable();

        private int? _LogsCount;

        /// <summary>
        /// Gets the total number of logs stored in this instance
        /// </summary>
        public int LogsCount => _LogsCount ?? (_LogsCount = Source.Sum(g => g.Key.Logs)).Value;

        /// <summary>
        /// Gets a list of all the available logs stored in this instance
        /// </summary>
        [NotNull, ItemNotNull]
        public IEnumerable<TLog> Logs => Source.SelectMany(g => g);

        /// <summary>
        /// Gets the list of all the app versions with at least a single stored log
        /// </summary>
        [NotNull, ItemNotNull]
        public IEnumerable<Version> AppVersions => Source.Select(g => g.Key.AppVersion);

        #endregion

        #region IReadOnlyCollection

        /// <inheritdoc/>
        public int Count => Source.Count;

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<IGrouping<VersionInfo, TLog>> GetEnumerator() => Source.GetEnumerator();

        #endregion
    }
}
