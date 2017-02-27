using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using BigWatson.Shared.Misc;
using JetBrains.Annotations;

namespace BigWatson.UWP.Misc
{
    /// <summary>
    /// A simple class with some useful extension methods
    /// </summary>
    internal static class UWPExtensions
    {
        // Private core copy method
        private static async Task CopyFileContentAsync([NotNull] Stream source, [NotNull] Stream target)
        {
            byte[] bytes = new byte[1024];
            while (true)
            {
                int read = await source.ReadAsync(bytes, 0, 1024);
                if (read == 0) break;
                await target.WriteAsync(bytes, 0, read);
            }
        }

        /// <summary>
        /// Copies the content of a source file inside a target file
        /// </summary>
        /// <param name="source">The source file</param>
        /// <param name="target">The target file (its content will be overwritten)</param>
        public static async Task CopyFileContentAsync([NotNull] this StorageFile source, [NotNull] StorageFile target)
        {
            // Try to open the files and copy their content
            try
            {
                using (Stream inputStream = await source.OpenStreamForReadAsync())
                using (Stream outputStream = await target.OpenStreamForWriteAsync())
                {
                    await CopyFileContentAsync(inputStream, outputStream);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Create a temporary copy if the first file failed to open
                StorageFile localCopy = await CreateCopyAsync(source, ApplicationData.Current.TemporaryFolder);
                using (Stream inputStream = await localCopy.OpenStreamForReadAsync())
                using (Stream outputStream = await target.OpenStreamForWriteAsync())
                {
                    await CopyFileContentAsync(inputStream, outputStream);
                }

                // Delete the temporary copy
                localCopy.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().Forget();
            }
        }

        /// <summary>
        /// Creates a new copy of the input file in the desired folder
        /// </summary>
        /// <param name="file">The file to copy</param>
        /// <param name="folder">The target folder</param>
        public static IAsyncOperation<StorageFile> CreateCopyAsync([NotNull] this StorageFile file, [NotNull] IStorageFolder folder)
        {
            return file.CopyAsync(folder, $"{Guid.NewGuid()}{file.FileType}");
        }

        /// <summary>
        /// Tries to get the target file and returns null if it isn't found
        /// </summary>
        /// <param name="folder">The source folder</param>
        /// <param name="filename">The filename of the file to retrieve</param>
        [ItemCanBeNull]
        public static async Task<T> TryGetItemAsync<T>([NotNull] this StorageFolder folder, [NotNull] String filename) where T : class, IStorageItem
        {
            return await folder.TryGetItemAsync(filename) as T;
        }
    }
}
