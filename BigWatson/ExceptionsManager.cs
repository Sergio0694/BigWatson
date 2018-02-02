using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BigWatson.Models;
using JetBrains.Annotations;
using Realms;

namespace BigWatson
{
    /// <summary>
    /// The entry point with all the APIs exposed by the library
    /// </summary>
    public static class ExceptionsManager
    {
        #region Properties

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
                    folder = Path.Combine(data, nameof(ExceptionsManager)),
                    path = Path.Combine(folder, "crashreports.realm");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return new RealmConfiguration(path);
            }
        }

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
            get => _UsedMemoryParser ?? DefaultMemoryParser;
            set => _UsedMemoryParser = value;
        }

        #endregion

        #region Logging

        /// <summary>
        /// Saves the crash report into local storage
        /// </summary>
        /// <param name="e">Exception that caused the app to crash</param>
        [PublicAPI]
        public static void Log([NotNull] Exception e)
        {
            // Save the report into the database
            using (Realm realm = Realm.GetInstance(DefaultConfiguration))
            using (Transaction transaction = realm.BeginWrite())
            {
                RealmExceptionReport report = new RealmExceptionReport
                {
                    Uid = Guid.NewGuid().ToString(),
                    ExceptionType = e.GetType().ToString(),
                    HResult = e.HResult,
                    Message = e.Message,
                    StackTrace = e.StackTrace,
                    AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    UsedMemory = UsedMemoryParser(),
                    CrashTime = DateTime.Now.ToBinary()
                };
                realm.Add(report);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Deletes all the existing exception reports present in the database
        /// </summary>
        [PublicAPI]
        public static async Task ResetAsync()
        {
            using (Realm realm = await Realm.GetInstanceAsync(DefaultConfiguration))
            using (Transaction transaction = realm.BeginWrite())
            {
                realm.RemoveAll<RealmExceptionReport>();
                transaction.Commit();
            }
        }

        #endregion

        #region Reports inspection

        /// <summary>
        /// Loads the groups with the previous exceptions from the built-in <see cref="Realm"/> instance
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        public static Task<ExceptionsCollection> LoadCrashReportsAsync() => LoadCrashReportsAsync(DefaultConfiguration);

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> daatabase located at the specified path
        /// </summary>
        /// <param name="path">The path to the <see cref="Realm"/> database to read</param>
        [PublicAPI]
        [Pure, ItemNotNull]
        public static Task<ExceptionsCollection> LoadCrashReportsAsync([NotNull] String path) => LoadCrashReportsAsync(new RealmConfiguration(path));

        /// <summary>
        /// Loads the groups with the previous exceptions from the <see cref="Realm"/> instance specified by the input <see cref="RealmConfiguration"/>
        /// </summary>
        [Pure, ItemNotNull]
        private static async Task<ExceptionsCollection> LoadCrashReportsAsync([NotNull] RealmConfiguration configuration)
        {
            // Get all the app versions and the exceptions
            using (Realm realm = await Realm.GetInstanceAsync(configuration))
            {
                ExceptionReport[] exceptions =
                    (from entry in realm.All<RealmExceptionReport>().ToArray()
                     select new ExceptionReport(entry)).ToArray();

                // Update the type occurrencies and the other info
                foreach (ExceptionReport exception in exceptions)
                {
                    // Number of times this same Exception was thrown
                    exception.ExceptionTypeOccurrencies = exceptions.Count(item => item.ExceptionType.Equals(exception.ExceptionType));

                    // Exceptions with the same Type
                    ExceptionReport[] sameType =
                        (from item in exceptions
                         where item.ExceptionType.Equals(exception.ExceptionType)
                         orderby item.CrashTime descending
                         select item).ToArray();

                    // Update the crash times for the same Exceptions
                    exception.RecentCrashTime = sameType.First().CrashTime;
                    if (sameType.Length > 1) exception.LessRecentCrashTime = sameType.Last().CrashTime;

                    // Get the app versions for this Exception Type
                    Version[] versions =
                        (from entry in sameType
                         group entry by entry.AppVersion
                         into version
                         orderby version.Key
                         select version.Key).ToArray();

                    // Update the number of occurrencies and the app version interval
                    exception.MinExceptionVersion = versions.First();
                    if (versions.Length > 1) exception.MaxExceptionVersion = versions.Last();
                }

                // List the available app versions
                IEnumerable<Version> appVersions =
                    from exception in exceptions
                    group exception by exception.AppVersion
                    into header
                    orderby header.Key descending
                    select header.Key;

                // Create the output collection
                return new ExceptionsCollection(
                    from version in appVersions
                    let items =
                        (from exception in exceptions
                         where exception.AppVersion.Equals(version)
                         orderby exception.CrashTime descending
                         select exception).ToArray()
                    where items.Length > 0
                    select new GroupedList<VersionExtendedInfo, ExceptionReport>(
                        new VersionExtendedInfo(items.Length, version), items));
            }
        }

        /// <summary>
        /// Loads the groups with the previous exceptions from the built-in <see cref="Realm"/> instance 
        /// for the app versions that generated the specified <see cref="Exception"/> type
        /// </summary>
        /// <typeparam name="T">The <see cref="Exception"/> type to look for</typeparam>
        [PublicAPI]
        [Pure, ItemNotNull]
        public static async Task<ExceptionsCollection> LoadCrashReportsAsync<T>() where T : Exception
        {
            using (Realm realm = await Realm.GetInstanceAsync(DefaultConfiguration))
            {
                // Get the exceptions with the same Type
                String type = typeof(T).ToString();
                ExceptionReport[] exceptions = 
                    (from entry in realm.All<RealmExceptionReport>().Where(entry => entry.ExceptionType.Equals(type))
                     select new ExceptionReport(entry)).ToArray();

                // Group by version
                return new ExceptionsCollection(
                    from exception in exceptions
                    group exception by exception.AppVersion
                    into version
                    orderby version.Key descending
                    let items =
                        (from entry in version
                         orderby entry.CrashTime descending
                         select entry).ToArray()
                    select new GroupedList<VersionExtendedInfo, ExceptionReport>(
                        new VersionExtendedInfo(items.Length, version.Key), items));
            }
        }

        /// <summary>
        /// Removes all the <see cref="ExceptionReport"/> instances in the databases older than the input <see cref="TimeSpan"/>
        /// </summary>
        [PublicAPI]
        public static async Task TrimAsync(TimeSpan threshold)
        {
            using (Realm realm = await Realm.GetInstanceAsync(DefaultConfiguration))
            {
                IQueryable<RealmExceptionReport> old = realm.All<RealmExceptionReport>().Where(entry => DateTime.Now.Subtract(DateTime.FromBinary(entry.CrashTime)) > threshold);
                realm.RemoveRange(old);
            }

            Realm.Compact();
        }

        #endregion

        #region Tools

        /// <summary>
        /// Copies the content of the current crash reports database into a <see cref="Stream"/>
        /// </summary>
        [PublicAPI]
        [Pure, ItemNotNull]
        public static Task<Stream> ExportDatabaseAsync()
        {
            return Task.Run(() =>
            {
                // Copy the current database
                Stream stream = new MemoryStream();
                using (FileStream file = File.OpenRead(DefaultConfiguration.DatabasePath))
                {
                    byte[] buffer = new byte[8192];
                    while (true)
                    {
                        int read = file.Read(buffer, 0, buffer.Length);
                        if (read > 0) stream.Write(buffer, 0, read);
                        else break;
                    }
                }

                // Seek the result stream back to the start
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });
        }

        /// <summary>
        /// Copies the content of the current crash reports database into a backup file with the specified path
        /// </summary>
        /// <param name="path">The path to the target backup file</param>
        [PublicAPI]
        public static Task ExportDatabaseAsync([NotNull] String path)
        {
            return Task.Run(() =>
            {
                using (FileStream
                    source = File.OpenRead(DefaultConfiguration.DatabasePath),
                    destination = File.OpenWrite(path))
                {
                    byte[] buffer = new byte[8192];
                    while (true)
                    {
                        int read = source.Read(buffer, 0, buffer.Length);
                        if (read > 0) destination.Write(buffer, 0, read);
                        else break;
                    }
                }
            });
        }

        #endregion
    }
}
