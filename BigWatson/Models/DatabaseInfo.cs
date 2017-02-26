using Windows.Storage;
using SQLite.Net.Async;

namespace BigWatson.Models
{
    /// <summary>
    /// A class that wraps useful info for a database file in use
    /// </summary>
    public sealed class DatabaseInfo
    {
        /// <summary>
        /// Gets the database file
        /// </summary>
        public StorageFile File { get; }

        /// <summary>
        /// Gets an open async connection to the database file
        /// </summary>
        public SQLiteAsyncConnection Connection { get; }

        /// <summary>
        /// Gets whether or not the database retrieved was already existing in the target directory
        /// </summary>
        public bool LoadedExistingDatabase { get; }

        // Internal constructor
        internal DatabaseInfo(StorageFile file, SQLiteAsyncConnection connection, bool loadedExisting)
        {
            File = file;
            Connection = connection;
            LoadedExistingDatabase = loadedExisting;
        }
    }
}
