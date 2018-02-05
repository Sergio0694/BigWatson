using System;
using System.IO;
using System.Reflection;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Loggers;
using JetBrains.Annotations;
using Realms;

namespace BigWatsonDotNet
{
    /// <summary>
    /// The entry point to reach all the available APIs in the library
    /// </summary>
    public static class BigWatson
    {
        /// <summary>
        /// Gets the .realm file extension used by the crash reports databases
        /// </summary>
        [NotNull]
        public const string DatabaseExtension = ".realm";

        #region Folder management

        /// <summary>
        /// Gets the path for the library working folder
        /// </summary>
        [NotNull]
        private static string WorkingDirectoryPath
        {
            get
            {
                string
                    data = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    folder = Path.Combine(data, nameof(BigWatson));
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return folder;
            }
        }

        /// <summary>
        /// Gets the path for the cache files used by the library
        /// </summary>
        [NotNull]
        internal static string CacheDirectoryPath
        {
            get
            {
                string folder = Path.Combine(WorkingDirectoryPath, "cache");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return folder;
            }
        }

        /// <summary>
        /// Gets the default <see cref="RealmConfiguration"/> instance for the <see cref="Realm"/> used by the library
        /// </summary>
        [NotNull]
        private static RealmConfiguration DefaultConfiguration => new RealmConfiguration(Path.Combine(WorkingDirectoryPath, $@"crashreports{DatabaseExtension}"));

        #endregion

        #region Public APIs

        /// <summary>
        /// Gets the current <see cref="Version"/> instance for the executing app
        /// </summary>
        [NotNull]
        public static Version CurrentAppVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets or sets a <see cref="Func{TResult}"/> <see langword="delegate"/> that checks the current memory used by the process.
        /// This is needed on some platforms (eg. UWP), where the <see cref="System.Diagnostics.Process"/> APIs are not supported.
        /// </summary>
        [CanBeNull]
        public static Func<long> MemoryParser { get; set; }

        [CanBeNull]
        private static ILogger _Instance;

        /// <summary>
        /// Gets the local <see cref="ILogger"/> instance to read and write logs for the current app
        /// </summary>
        [PublicAPI]
        [NotNull]
        public static ILogger Instance => _Instance ?? (_Instance = new Logger(DefaultConfiguration));

        /// <summary>
        /// Gets an <see cref="IReadOnlyLogger"/> instance to access logs from an external database
        /// </summary>
        /// <param name="path">The path to the exported logs database to open</param>
        [PublicAPI]
        [Pure, NotNull]
        public static IReadOnlyLogger Load([NotNull] string path) => new ReadOnlyLogger(new RealmConfiguration(path));

        /// <summary>
        /// Gets an <see cref="IReadOnlyLogger"/> instance to access logs from an external database
        /// </summary>
        /// <param name="stream">The input <see cref="Stream"/> with the database to read</param>
        /// <remarks>As a <see cref="Realm"/> database connection can't be created directly from a <see cref="Stream"/>, 
        /// the contents will be copied to a local temporary file that will be used to load the external logs
        /// temporary files</remarks>
        [PublicAPI]
        [Pure, NotNull]
        public static IReadOnlyLogger Load([NotNull] Stream stream)
        {
            if (!stream.CanRead) throw new ArgumentException("The input stream can't be read from", nameof(stream));
            String filename = Path.Combine(CacheDirectoryPath, $"{Guid.NewGuid().ToString()}{DatabaseExtension}");
            using (FileStream file = File.OpenWrite(filename)) stream.CopyTo(file);
            return Load(filename);
        }

        #endregion
    }
}
