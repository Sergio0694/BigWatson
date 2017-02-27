using Windows.Storage;
using BigWatson.Shared.Models;
using SQLite.Net.Async;

namespace BigWatson.Misc
{
    /// <summary>
    /// A class that wraps useful info for a database file in use on the UWP platform
    /// </summary>
    public sealed class DatabaseInfo : DatabaseInfoBase<StorageFile>
    {
        /// <summary>
        /// Gets whether or not the database retrieved was already existing in the target directory
        /// </summary>
        public bool LoadedExistingDatabase { get; }

        // Internal constructor
        internal DatabaseInfo(StorageFile file, SQLiteAsyncConnection connection, bool loadedExisting)
            : base(file, connection)
        {
            LoadedExistingDatabase = loadedExisting;
        }
    }
}
