using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BigWatsonDotNet.Models.Abstract;
using BigWatsonDotNet.Models.Misc;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models
{
    /// <summary>
    /// A class that wraps a grouped collection of saved logs
    /// </summary>
    public sealed class LogsCollection<TLog> : IReadOnlyList<ReadOnlyGroupingList<VersionInfo, TLog>>, ILookup<Version, TLog>
        where TLog : LogBase
    {
        // Actual data source
        [NotNull, ItemNotNull]
        private readonly IReadOnlyList<ReadOnlyGroupingList<VersionInfo, TLog>> Source;
        
        internal LogsCollection([NotNull, ItemNotNull] IReadOnlyList<ReadOnlyGroupingList<VersionInfo, TLog>> source)
        {
            Source = source;
            LogsCount = source.Sum(g => g.Count);
            Logs = source.SelectMany(g => g).ToArray();
            AppVersions = source.Select(g => g.Key.AppVersion).ToArray();
        }

        #region Public APIs

        /// <summary>
        /// Gets the total number of logs stored in this instance
        /// </summary>
        public int LogsCount { get; }

        /// <summary>
        /// Gets a list of all the available logs stored in this instance
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyList<TLog> Logs { get; }

        /// <summary>
        /// Gets the list of all the app versions with at least a single stored log
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyList<Version> AppVersions { get; }

        /// <summary>
        /// Returns a list of saved logs according to the input selector
        /// </summary>
        /// <param name="predicate">A <see cref="Predicate{T}"/> used to select the logs to return</param>
        [NotNull, ItemNotNull]
        public IReadOnlyList<TLog> this[[NotNull] Predicate<TLog> predicate] => Logs.Where(log => predicate(log)).ToArray();

        #endregion

        #region IReadOnlyList

        /// <inheritdoc cref="IReadOnlyList{T}"/>
        public int Count => Source.Count;

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<ReadOnlyGroupingList<VersionInfo, TLog>> GetEnumerator() => Source.GetEnumerator();

        /// <inheritdoc/>
        [NotNull, ItemNotNull]
        public ReadOnlyGroupingList<VersionInfo, TLog> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Source[index];
        }

        #endregion

        #region ILookup

        /// <inheritdoc/>
        [NotNull, ItemNotNull]
        public IEnumerable<TLog> this[[NotNull] Version version] => Source.FirstOrDefault(group => group.Key.AppVersion.Equals(version)) ?? new TLog[0].AsEnumerable();

        /// <inheritdoc/>
        public bool Contains(Version key) => Source.Any(group => group.Key.AppVersion.Equals(key));

        /// <inheritdoc/>
        IEnumerator<IGrouping<Version, TLog>> IEnumerable<IGrouping<Version, TLog>>.GetEnumerator()
        {
            IEnumerator<IGrouping<Version, TLog>> GetEnumerator()
            {
                foreach (ReadOnlyGroupingList<VersionInfo, TLog> group in Source)
                    yield return new ReadOnlyGroupingList<Version, TLog>(group.Key.AppVersion, group);
            }

            return GetEnumerator();
        }

        #endregion
    }
}
