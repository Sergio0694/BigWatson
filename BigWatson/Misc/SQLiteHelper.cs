using System;
using System.Threading.Tasks;
using Windows.Storage;
using JetBrains.Annotations;
using SQLite.Net;
using SQLite.Net.Async;
using SQLite.Net.Platform.WinRT;

namespace BigWatson.Misc
{
    /// <summary>
    /// A static class with some helper methods to manage databases and database connections
    /// </summary>
    internal static class SQLiteSharedHelper
    {
        /// <summary>
        /// Creates a function that returns an open connection with the database at the given path
        /// </summary>
        /// <param name="databasePath">The path of the database to open</param>
        [NotNull]
        private static Func<SQLiteConnectionWithLock> PrepareSQLiteConnection([NotNull] String databasePath)
        {
            SQLitePlatformWinRT platform = new SQLitePlatformWinRT();
            SQLiteConnectionString connectionString = new SQLiteConnectionString(databasePath, true);
            SQLiteConnectionWithLock connection = new SQLiteConnectionWithLock(platform, connectionString);
            return () => connection;
        }

        // Replaces the local database with the default one (deletes all user changes)
        private static async Task<StorageFile> RestoreCleanDatabaseAsync([NotNull] String path, [NotNull] String filename)
        {
            // Get a clean database file and copy it
            StorageFile cleanDatabase = await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));

            return await cleanDatabase.CopyAsync(ApplicationData.Current.LocalFolder, filename, NameCollisionOption.ReplaceExisting);
        }

        /// <summary>
        /// Loads a database up and connects to it, using a backup database if the target one isn't available
        /// </summary>
        /// <param name="filename">The filename of the target database to use</param>
        /// <param name="backupPath">The path of the default database file to use in case of failure</param>
        public static async Task<DatabaseInfo> InitializeDatabaseAsync([NotNull] String filename, [NotNull] String backupPath)
        {
            // Get the local database
            bool loadedExisting;
            StorageFile database = await ApplicationData.Current.LocalFolder.TryGetItemAsync<StorageFile>(filename);
            if (database != null) loadedExisting = true;
            else
            {
                database = await RestoreCleanDatabaseAsync(backupPath, filename);
                loadedExisting = false;
            }

            // Open the database connection
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(PrepareSQLiteConnection(database.Path));

            // Return the retrived items
            return new DatabaseInfo(database, connection, loadedExisting);
        }
    }
}
