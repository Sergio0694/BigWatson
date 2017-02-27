using BigWatson.Shared.Models;
using PCLStorage;
using SQLite.Net.Async;

namespace BigWatson.PCL.Misc
{
    /// <summary>
    /// A class that wraps useful info for a database file in use on a generic platform
    /// </summary>
    public sealed class DatabaseInfo : DatabaseInfoBase<IFile>
    {
        // Internal constructor
        internal DatabaseInfo(IFile file, SQLiteAsyncConnection connection) : base(file, connection) { }
    }
}
