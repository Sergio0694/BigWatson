﻿using System;
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
    [PublicAPI]
    public sealed class LogsCollection<TLog> : IReadOnlyList<ReadOnlyGroupingList<Version, TLog>>
        where TLog : LogBase
    {
        // Actual data source
        [NotNull, ItemNotNull]
        private readonly IReadOnlyList<ReadOnlyGroupingList<Version, TLog>> Source;
        
        internal LogsCollection([NotNull, ItemNotNull] IReadOnlyList<ReadOnlyGroupingList<Version, TLog>> source)
        {
            Source = source;
            LogsCount = source.Sum(g => g.Count);
            Logs = source.SelectMany(g => g).ToArray();
            AppVersions = source.Select(g => g.Key).ToArray();
        }

        #region APIs

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

        /// <summary>
        /// Returns a list of saved logs for the input app <see cref="Version"/>
        /// </summary>
        /// <param name="version">The app <see cref="Version"/> for the logs to return</param>
        [NotNull, ItemNotNull]
        public IReadOnlyList<TLog> this[[NotNull] Version version]
        {
            get
            {
                if (Source.FirstOrDefault(group => group.Key.Equals(version)) is var result && result != null)
                {
                    return result;
                }
                return new TLog[0];
            }
        }

        /// <summary>
        /// Returns an <see cref="IReadOnlyDictionary{TKey,TValue}"/> with the logs stored in the current instance and their relative app <see cref="Version"/>
        /// </summary>
        [Pure, NotNull]
        public IReadOnlyDictionary<Version, IReadOnlyList<TLog>> ToDictionary()
        {
            return Source.ToDictionary<ReadOnlyGroupingList<Version, TLog>, Version, IReadOnlyList<TLog>>(group => group.Key, group => group);
        }

        #endregion

        #region IReadOnlyList

        /// <inheritdoc/>
        public int Count => Source.Count;

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<ReadOnlyGroupingList<Version, TLog>> GetEnumerator() => Source.GetEnumerator();

        /// <inheritdoc/>
        [NotNull, ItemNotNull]
        public ReadOnlyGroupingList<Version, TLog> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Source[index];
        }

        #endregion
    }
}
