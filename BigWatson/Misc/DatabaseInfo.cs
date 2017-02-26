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
        // Internal constructor
        internal DatabaseInfo(StorageFile file, SQLiteAsyncConnection connection, bool loadedExisting)
            : base(file, connection, loadedExisting) { }
    }
}
