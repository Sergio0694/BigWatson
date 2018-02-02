using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Managers;
using JetBrains.Annotations;
using Realms;

namespace BigWatsonDotNet
{
    /// <summary>
    /// The entry point with all the APIs exposed by the library
    /// </summary>
    public static class BigWatson
    {
        #region Memory logger

        // The default memory parser
        [Pure]
        private static long DefaultMemoryParser()
        {
            try
            {
                return Process.GetCurrentProcess().PrivateMemorySize64;
            }
            catch (PlatformNotSupportedException)
            {
                // Just ignore
                return 0;
            }
        }

        [CanBeNull]
        private static Func<long> _UsedMemoryParser;

        /// <summary>
        /// Gets or sets a <see cref="Func{TResult}"/> <see langword="delegate"/> that checks the current memory used by the process.
        /// This is needed on some platforms (eg. UWP), where the <see cref="Process"/> APIs are not supported.
        /// </summary>
        [NotNull]
        public static Func<long> UsedMemoryParser
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _UsedMemoryParser ?? DefaultMemoryParser;
            set => _UsedMemoryParser = value;
        }

        #endregion

        /// <summary>
        /// Gets the .realm file extension used by the crash reports databases
        /// </summary>
        [NotNull]
        public const String DatabaseExtension = ".realm";

        /// <summary>
        /// Gets the default <see cref="RealmConfiguration"/> instance for the <see cref="Realm"/> used by the library
        /// </summary>
        [NotNull]
        private static RealmConfiguration DefaultConfiguration
        {
            get
            {
                String
                    data = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    folder = Path.Combine(data, nameof(BigWatson)),
                    path = Path.Combine(folder, $@"crashreports{DatabaseExtension}");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return new RealmConfiguration(path);
            }
        }

        [CanBeNull]
        private static IExceptionsManager _Default;

        /// <summary>
        /// Gets the default <see cref="IExceptionsManager"/> instance to read and write crash reports for the current app
        /// </summary>
        [PublicAPI]
        [NotNull]
        public static IExceptionsManager Default => _Default ?? (_Default = new ReadWriteExceptionsManager(DefaultConfiguration));

        /// <summary>
        /// Gets an <see cref="IReadOnlyExceptionManager"/> instance to access crash reports from an external database
        /// </summary>
        /// <param name="path">The path to the exported crash reports database to open</param>
        [PublicAPI]
        [Pure, NotNull]
        public static IReadOnlyExceptionManager Load([NotNull] String path) => new ReadOnlyExceptionsManager(new RealmConfiguration(path));
    }
}
