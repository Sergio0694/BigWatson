using SQLite.Net.Async;

namespace BigWatson.Shared.Models
{
    /// <summary>
    /// A class that wraps useful info for a database file in use
    /// </summary>
    /// <typeparam name="T">The type that represents the database file in use</typeparam>
    public abstract class DatabaseInfoBase<T> where T : class
    {
        /// <summary>
        /// Gets the database file
        /// </summary>
        public T File { get; }

        /// <summary>
        /// Gets an open async connection to the database file
        /// </summary>
        public SQLiteAsyncConnection Connection { get; }

        /// <summary>
        /// Gets whether or not the database retrieved was already existing in the target directory
        /// </summary>
        public bool LoadedExistingDatabase { get; }

        // Internal constructor
        internal DatabaseInfoBase(T file, SQLiteAsyncConnection connection, bool loadedExisting)
        {
            File = file;
            Connection = connection;
            LoadedExistingDatabase = loadedExisting;
        }
    }
}
