using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PCLStorage;

namespace BigWatson.PCL.Helpers
{
    /// <summary>
    /// A simple class that manages the current file system in use
    /// </summary>
    internal static class FileSystemHelper
    {
        /// <summary>
        /// Gets the desired file in the target folder, creating them if they don't exist
        /// </summary>
        /// <param name="filename">The filename</param>
        /// <param name="folderName">The name of the folder that contains the requested file</param>
        public static async Task<IFile> TryGetFileAsync([NotNull] String filename, [NotNull] String folderName)
        {
            IFolder folder = await FileSystem.Current.LocalStorage.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
            return await folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
        }
    }
}
