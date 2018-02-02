﻿using System;
using System.IO;
using System.Threading.Tasks;
using BigWatson.Models;
using JetBrains.Annotations;

namespace BigWatson.Interfaces
{
    /// <summary>
    /// An interface for an exceptions manager instance with write permission
    /// </summary>
    public interface IExceptionsManager : IExceptionsReader
    {
        /// <summary>
        /// Saves the crash report into local storage
        /// </summary>
        /// <param name="e">Exception that caused the app to crash</param>
        [PublicAPI]
        void Log([NotNull] Exception e);

        /// <summary>
        /// Deletes all the existing exception reports present in the database
        /// </summary>
        [PublicAPI]
        Task ResetAsync();

        /// <summary>
        /// Removes all the <see cref="ExceptionReport"/> instances in the databases older than the input <see cref="TimeSpan"/>
        /// </summary>
        [PublicAPI]
        Task TrimAsync(TimeSpan threshold);

        /// <summary>
        /// Copies the content of the current crash reports database into a <see cref="Stream"/>
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        Task<Stream> ExportDatabaseAsync();

        /// <summary>
        /// Copies the content of the current crash reports database into a backup file with the specified path
        /// </summary>
        /// <param name="path">The path to the target backup file</param>
        [PublicAPI]
        Task ExportDatabaseAsync([NotNull] String path);
    }
}