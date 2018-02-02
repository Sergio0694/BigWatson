using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models
{
    /// <summary>
    /// A class that wraps a grouped collection of saved exception reports
    /// </summary>
    public sealed class ExceptionsCollection : IEnumerable<IGrouping<VersionExtendedInfo, ExceptionReport>>
    {
        #region Initialization

        // Actual source query
        [NotNull, ItemNotNull]
        private readonly IEnumerable<IGrouping<VersionExtendedInfo, ExceptionReport>> Source;

        // Internal constructor
        internal ExceptionsCollection([NotNull, ItemNotNull] IEnumerable<IGrouping<VersionExtendedInfo, ExceptionReport>> source)
        {
            Source = source;
        }

        #endregion

        #region Public APIs

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<IGrouping<VersionExtendedInfo, ExceptionReport>> GetEnumerator() => Source.GetEnumerator();

        private int? _ExceptionsCount;

        /// <summary>
        /// Gets the total number of exceptions stored in this instance
        /// </summary>
        public int ExceptionsCount => _ExceptionsCount ?? (_ExceptionsCount = Source.Sum(g => g.Key.Crashes)).Value;

        /// <summary>
        /// Gets a list of all the available exception reports stored in this instance
        /// </summary>
        public IEnumerable<ExceptionReport> Exceptions => Source.SelectMany(g => g);

        /// <summary>
        /// Gets the list of all the app versions with at least a single stored exception report
        /// </summary>
        public IEnumerable<Version> CrashedAppVersions => Source.Select(g => g.Key.AppVersion);

        /// <summary>
        /// Gets an ordered list of all the types of the exceptions that have been logged so far
        /// </summary>
        public IEnumerable<String> ExceptionTypes => Exceptions.Select(e => e.ExceptionType).Distinct().OrderBy(t => t);

        #endregion
    }
}
