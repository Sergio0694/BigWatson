﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
using JetBrains.Annotations;
using Realms;

namespace BigWatsonDotNet.Managers
{
    /// <summary>
    /// A complete exceptions manager with read and write permission
    /// </summary>
    internal sealed class ReadWriteExceptionsManager : ReadOnlyExceptionsManager, IExceptionsManager
    {
        public ReadWriteExceptionsManager([NotNull] RealmConfiguration configuration) : base(configuration) { }

        /// <inheritdoc/>
        public void Log(Exception e)
        {
            // Save the report into the database
            using (Realm realm = Realm.GetInstance(Configuration))
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
                    UsedMemory = BigWatson.UsedMemoryParser(),
                    CrashTime = DateTime.Now.ToBinary()
                };
                realm.Add(report);
                transaction.Commit();
            }
        }

        /// <inheritdoc/>
        public async Task ResetAsync()
        {
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            using (Transaction transaction = realm.BeginWrite())
            {
                realm.RemoveAll<RealmExceptionReport>();
                transaction.Commit();
            }
        }

        /// <inheritdoc/>
        public async Task TrimAsync(TimeSpan threshold)
        {
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                foreach (RealmExceptionReport old in
                    from entry in realm.All<RealmExceptionReport>().ToArray()
                    where DateTime.Now.Subtract(DateTime.FromBinary(entry.CrashTime)) > threshold
                    select entry)
                {
                    realm.Remove(old);
                }
            }

            Realm.Compact();
        }

        /// <inheritdoc/>
        public Task<Stream> ExportDatabaseAsync()
        {
            return Task.Run(() =>
            {
                // Copy the current database
                Stream stream = new MemoryStream();
                using (FileStream file = File.OpenRead(Configuration.DatabasePath))
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

        /// <inheritdoc/>
        public Task ExportDatabaseAsync(String path)
        {
            return Task.Run(() =>
            {
                using (FileStream
                    source = File.OpenRead(Configuration.DatabasePath),
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
    }
}
